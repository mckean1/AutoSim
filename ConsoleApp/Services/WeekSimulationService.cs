using AutoSim.Domain.Management.Interfaces;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Management.Services;

namespace ConsoleApp.Services
{
    /// <summary>
    /// Coordinates week simulation and selects the human team's resolved match.
    /// </summary>
    public sealed class WeekSimulationService
    {
        private readonly SeasonProgressionService _seasonProgressionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeekSimulationService"/> class.
        /// </summary>
        /// <param name="matchEngineWrapper">The match engine wrapper.</param>
        public WeekSimulationService(IMatchEngineWrapper? matchEngineWrapper = null)
        {
            _seasonProgressionService = new SeasonProgressionService(matchEngineWrapper);
        }

        /// <summary>
        /// Starts a background simulation session for the current week.
        /// </summary>
        /// <param name="world">The world to simulate.</param>
        /// <param name="humanTeamId">The human team identifier.</param>
        /// <param name="scheduledMatch">The scheduled human match.</param>
        /// <returns>The background simulation session.</returns>
        public WeekSimulationSession StartSession(
            WorldState world,
            string humanTeamId,
            ScheduledMatch scheduledMatch)
        {
            ArgumentNullException.ThrowIfNull(world);
            ArgumentException.ThrowIfNullOrWhiteSpace(humanTeamId);
            ArgumentNullException.ThrowIfNull(scheduledMatch);

            CancellationTokenSource cancellationTokenSource = new();
            Task<WeekSimulationResult> task = Task.Run(
                () => ResolveCurrentWeek(world, humanTeamId, scheduledMatch, cancellationTokenSource.Token),
                cancellationTokenSource.Token);
            return new WeekSimulationSession(task, cancellationTokenSource);
        }

        /// <summary>
        /// Resolves the current week and returns the human team's match result when one was played.
        /// </summary>
        /// <param name="world">The world to simulate.</param>
        /// <param name="humanTeamId">The human team identifier.</param>
        /// <param name="scheduledMatch">The scheduled human match.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The resolved week simulation result.</returns>
        public WeekSimulationResult ResolveCurrentWeek(
            WorldState world,
            string humanTeamId,
            ScheduledMatch scheduledMatch,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(world);
            ArgumentException.ThrowIfNullOrWhiteSpace(humanTeamId);
            ArgumentNullException.ThrowIfNull(scheduledMatch);

            cancellationToken.ThrowIfCancellationRequested();
            int resolvedWeek = world.Season.CurrentWeek;
            SeasonProgressionResult result = _seasonProgressionService.ResolveCurrentWeek(world);
            cancellationToken.ThrowIfCancellationRequested();
            MatchResult? humanResult = result.MatchResults
                .FirstOrDefault(matchResult => IsHumanMatch(matchResult, humanTeamId, result.World));

            return new WeekSimulationResult
            {
                AllMatchResults = result.MatchResults,
                HumanMatchResult = humanResult,
                ResolvedWeek = resolvedWeek,
                ScheduledMatch = scheduledMatch,
                World = result.World
            };
        }

        private static bool IsHumanMatch(MatchResult result, string humanTeamId, WorldState world)
        {
            ScheduledMatch? match = world.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Schedule)
                .FirstOrDefault(scheduledMatch => string.Equals(scheduledMatch.Id, result.MatchId, StringComparison.Ordinal));

            return match is not null
                && (string.Equals(match.HomeTeamId, humanTeamId, StringComparison.Ordinal)
                    || string.Equals(match.AwayTeamId, humanTeamId, StringComparison.Ordinal));
        }
    }
}
