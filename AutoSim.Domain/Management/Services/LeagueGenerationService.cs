using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Models;

namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Generates leagues, divisions, teams, coaches, and players for a tier.
    /// </summary>
    public sealed class LeagueGenerationService
    {
        private static readonly DivisionName[] DivisionNames =
        [
            DivisionName.Apex,
            DivisionName.Vanguard,
            DivisionName.Frontier,
            DivisionName.Summit
        ];

        private static readonly PositionRole[] PositionRoles =
        [
            PositionRole.Top,
            PositionRole.Jungle,
            PositionRole.Mid,
            PositionRole.Bot,
            PositionRole.Support
        ];

        private readonly ScheduleGenerationService _scheduleGenerationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="LeagueGenerationService"/> class.
        /// </summary>
        /// <param name="scheduleGenerationService">The schedule generation service.</param>
        public LeagueGenerationService(ScheduleGenerationService? scheduleGenerationService = null)
        {
            _scheduleGenerationService = scheduleGenerationService ?? new ScheduleGenerationService();
        }

        /// <summary>
        /// Generates one league.
        /// </summary>
        /// <param name="tierName">The tier name.</param>
        /// <param name="region">The league region.</param>
        /// <param name="humanPlacement">The human placement, if this league should contain the human coach.</param>
        /// <param name="nameGenerationService">The name generation service.</param>
        /// <param name="humanCoachName">The human coach name.</param>
        /// <param name="humanTeamName">The human team name.</param>
        /// <returns>The generated league bundle.</returns>
        public LeagueGenerationResult GenerateLeague(
            CompetitiveTierName tierName,
            LeagueRegion region,
            HumanPlacement? humanPlacement,
            NameGenerationService? nameGenerationService = null,
            string? humanCoachName = null,
            string? humanTeamName = null)
        {
            NameGenerationService names = nameGenerationService ?? new NameGenerationService((int)tierName * 397 + (int)region);
            string? resolvedHumanCoachName = humanPlacement is null ? null : ResolveCoachName(names, humanCoachName);
            string? resolvedHumanTeamName = humanPlacement is null ? null : ResolveTeamName(names, humanTeamName);
            string leagueId = $"{tierName}-{region}".ToLowerInvariant();
            List<Coach> coaches = [];
            List<Player> players = [];
            List<Team> teams = [];
            List<Division> divisions = [];

            foreach (DivisionName divisionName in DivisionNames)
            {
                string divisionId = $"{leagueId}-{divisionName}".ToLowerInvariant();
                List<string> divisionTeamIds = [];

                for (int slot = 1; slot <= 5; slot++)
                {
                    bool isHumanTeam = humanPlacement is not null
                        && humanPlacement.Region == region
                        && humanPlacement.Division == divisionName
                        && humanPlacement.Slot == slot;
                    string teamId = isHumanTeam ? "human-team" : $"{divisionId}-team-{slot}";
                    string coachId = isHumanTeam ? "human-coach" : $"{teamId}-coach";
                    IReadOnlyList<string> playerIds = PositionRoles
                        .Select(role => $"{teamId}-player-{role}".ToLowerInvariant())
                        .ToList();

                    coaches.Add(new Coach
                    {
                        Id = coachId,
                        IsHuman = isHumanTeam,
                        Name = isHumanTeam ? resolvedHumanCoachName! : names.GenerateCoachName(),
                        TeamId = teamId
                    });
                    players.AddRange(PositionRoles.Select((role, index) => new Player
                    {
                        Id = playerIds[index],
                        Name = names.GeneratePlayerName(),
                        PositionRole = role,
                        TeamId = teamId
                    }));
                    teams.Add(new Team
                    {
                        CoachId = coachId,
                        DivisionId = divisionId,
                        Id = teamId,
                        LeagueId = leagueId,
                        Name = isHumanTeam ? resolvedHumanTeamName! : names.GenerateTeamName(),
                        PlayerIds = playerIds
                    });
                    divisionTeamIds.Add(teamId);
                }

                divisions.Add(new Division
                {
                    Id = divisionId,
                    Name = divisionName,
                    TeamIds = divisionTeamIds
                });
            }

            IReadOnlyList<ScheduledMatch> regularSeasonSchedule = _scheduleGenerationService.GenerateRegularSeasonSchedule(
                leagueId,
                teams,
                divisions);
            IReadOnlyList<ScheduledMatch> schedule = regularSeasonSchedule
                .Concat(_scheduleGenerationService.GenerateLeaguePlayoffReservations(leagueId))
                .ToList();
            IReadOnlyList<LeagueStanding> standings = teams
                .Select(team => new LeagueStanding { TeamId = team.Id })
                .ToList();
            League league = new()
            {
                Id = leagueId,
                Divisions = divisions,
                Region = region,
                Schedule = schedule,
                Standings = standings,
                Teams = teams,
                TierName = tierName
            };

            return new LeagueGenerationResult(league, coaches, players);
        }

        private static string ResolveCoachName(NameGenerationService nameGenerationService, string? humanCoachName)
        {
            if (string.IsNullOrWhiteSpace(humanCoachName))
            {
                return nameGenerationService.GenerateCoachName();
            }

            string coachName = humanCoachName.Trim();
            nameGenerationService.ReservePersonName(coachName);
            return coachName;
        }

        private static string ResolveTeamName(NameGenerationService nameGenerationService, string? humanTeamName)
        {
            if (string.IsNullOrWhiteSpace(humanTeamName))
            {
                return nameGenerationService.GenerateTeamName();
            }

            string teamName = humanTeamName.Trim();
            nameGenerationService.ReserveTeamName(teamName);
            return teamName;
        }
    }
}
