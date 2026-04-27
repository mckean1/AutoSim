using AutoSim.Domain.Management.Interfaces;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Advances the Management Layer through scheduled weeks.
    /// </summary>
    public sealed class SeasonProgressionService
    {
        private readonly IMatchEngineWrapper _matchEngineWrapper;
        private readonly StandingsService _standingsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonProgressionService"/> class.
        /// </summary>
        /// <param name="matchEngineWrapper">The match engine wrapper.</param>
        /// <param name="standingsService">The standings service.</param>
        public SeasonProgressionService(
            IMatchEngineWrapper? matchEngineWrapper = null,
            StandingsService? standingsService = null)
        {
            _matchEngineWrapper = matchEngineWrapper ?? new MatchEngineWrapper();
            _standingsService = standingsService ?? new StandingsService();
        }

        /// <summary>
        /// Resolves all scheduled matches for the current week.
        /// </summary>
        /// <param name="world">The world state.</param>
        /// <returns>The updated world and resolved match results.</returns>
        public SeasonProgressionResult ResolveCurrentWeek(WorldState world)
        {
            ArgumentNullException.ThrowIfNull(world);

            int week = world.Season.CurrentWeek;
            List<MatchResult> results = [];
            List<CompetitiveTier> tiers = [];

            foreach (CompetitiveTier tier in world.Tiers)
            {
                List<League> leagues = [];
                foreach (League league in tier.Leagues)
                {
                    League updatedLeague = ResolveLeagueWeek(league, week, world, results);
                    leagues.Add(updatedLeague);
                }

                tiers.Add(tier with { Leagues = leagues });
            }

            WorldState updatedWorld = world with
            {
                Season = world.Season with { CurrentWeek = week + 1 },
                Tiers = tiers
            };
            return new SeasonProgressionResult(updatedWorld, results);
        }

        /// <summary>
        /// Resolves the next scheduled match involving a team.
        /// </summary>
        /// <param name="world">The world state.</param>
        /// <param name="teamId">The team identifier.</param>
        /// <returns>The updated world and resolved match result when one is available.</returns>
        public SeasonProgressionResult ResolveNextMatchForTeam(WorldState world, string teamId)
        {
            ArgumentNullException.ThrowIfNull(world);
            ArgumentException.ThrowIfNullOrWhiteSpace(teamId);

            ScheduledMatch? nextMatch = world.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Schedule)
                .Where(match => match.Result is null
                    && (string.Equals(match.HomeTeamId, teamId, StringComparison.Ordinal)
                        || string.Equals(match.AwayTeamId, teamId, StringComparison.Ordinal)))
                .OrderBy(match => match.Week)
                .ThenBy(match => match.Id, StringComparer.Ordinal)
                .FirstOrDefault();

            if (nextMatch is null)
            {
                return new SeasonProgressionResult(world, []);
            }

            List<MatchResult> results = [];
            List<CompetitiveTier> tiers = [];
            foreach (CompetitiveTier tier in world.Tiers)
            {
                List<League> leagues = [];
                foreach (League league in tier.Leagues)
                {
                    League updatedLeague = league.Id == nextMatch.LeagueId
                        ? ResolveSpecificLeagueMatch(league, nextMatch.Id, world, results)
                        : league;
                    leagues.Add(updatedLeague);
                }

                tiers.Add(tier with { Leagues = leagues });
            }

            WorldState updatedWorld = world with
            {
                Season = world.Season with { CurrentWeek = Math.Max(world.Season.CurrentWeek, nextMatch.Week + 1) },
                Tiers = tiers
            };
            return new SeasonProgressionResult(updatedWorld, results);
        }

        private League ResolveLeagueWeek(League league, int week, WorldState world, List<MatchResult> results)
        {
            Dictionary<string, Team> teamsById = league.Teams.ToDictionary(team => team.Id, StringComparer.Ordinal);
            Dictionary<string, Coach> coachesById = world.Coaches.ToDictionary(coach => coach.Id, StringComparer.Ordinal);
            IReadOnlyList<ChampionDefinition> championCatalog = ChampionCatalog.GetDefaultChampions();
            League updatedLeague = league;
            List<ScheduledMatch> schedule = [];

            foreach (ScheduledMatch match in league.Schedule)
            {
                if (match.Week != week || match.Result is not null)
                {
                    schedule.Add(match);
                    continue;
                }

                if (!teamsById.ContainsKey(match.HomeTeamId) || !teamsById.ContainsKey(match.AwayTeamId))
                {
                    schedule.Add(match);
                    continue;
                }

                Team homeTeam = teamsById[match.HomeTeamId];
                Team awayTeam = teamsById[match.AwayTeamId];
                MatchResult result = _matchEngineWrapper.Resolve(
                    match,
                    homeTeam,
                    awayTeam,
                    coachesById[homeTeam.CoachId],
                    coachesById[awayTeam.CoachId],
                    world.Players,
                    championCatalog,
                    CreateMatchSeed(world.Seed, match.Id));
                ScheduledMatch resolvedMatch = match with { Result = result };
                schedule.Add(resolvedMatch);
                updatedLeague = _standingsService.ApplyMatchResult(updatedLeague with { Schedule = schedule }, resolvedMatch, result);
                results.Add(result);
            }

            return updatedLeague with { Schedule = schedule };
        }

        private League ResolveSpecificLeagueMatch(
            League league,
            string matchId,
            WorldState world,
            List<MatchResult> results)
        {
            Dictionary<string, Team> teamsById = league.Teams.ToDictionary(team => team.Id, StringComparer.Ordinal);
            Dictionary<string, Coach> coachesById = world.Coaches.ToDictionary(coach => coach.Id, StringComparer.Ordinal);
            IReadOnlyList<ChampionDefinition> championCatalog = ChampionCatalog.GetDefaultChampions();
            League updatedLeague = league;
            List<ScheduledMatch> schedule = [];

            foreach (ScheduledMatch match in league.Schedule)
            {
                if (!string.Equals(match.Id, matchId, StringComparison.Ordinal) || match.Result is not null)
                {
                    schedule.Add(match);
                    continue;
                }

                Team homeTeam = teamsById[match.HomeTeamId];
                Team awayTeam = teamsById[match.AwayTeamId];
                MatchResult result = _matchEngineWrapper.Resolve(
                    match,
                    homeTeam,
                    awayTeam,
                    coachesById[homeTeam.CoachId],
                    coachesById[awayTeam.CoachId],
                    world.Players,
                    championCatalog,
                    CreateMatchSeed(world.Seed, match.Id));
                ScheduledMatch resolvedMatch = match with { Result = result };
                schedule.Add(resolvedMatch);
                updatedLeague = _standingsService.ApplyMatchResult(updatedLeague with { Schedule = schedule }, resolvedMatch, result);
                results.Add(result);
            }

            return updatedLeague with { Schedule = schedule };
        }

        private static int CreateMatchSeed(int worldSeed, string matchId)
        {
            int seed = worldSeed;
            foreach (char value in matchId)
            {
                seed = unchecked((seed * 397) ^ value);
            }

            return seed;
        }
    }
}
