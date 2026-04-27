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
        /// <returns>The generated league bundle.</returns>
        public LeagueGenerationResult GenerateLeague(
            CompetitiveTierName tierName,
            LeagueRegion region,
            HumanPlacement? humanPlacement)
        {
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
                        Name = isHumanTeam ? "Human Coach" : $"{FormatName(teamId)} Coach",
                        TeamId = teamId
                    });
                    players.AddRange(PositionRoles.Select((role, index) => new Player
                    {
                        Id = playerIds[index],
                        Name = $"{FormatName(teamId)} {role}",
                        PositionRole = role,
                        TeamId = teamId
                    }));
                    teams.Add(new Team
                    {
                        CoachId = coachId,
                        DivisionId = divisionId,
                        Id = teamId,
                        LeagueId = leagueId,
                        Name = isHumanTeam ? "AutoSim United" : FormatName(teamId),
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

        private static string FormatName(string id) =>
            string.Join(' ', id.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(ToTitleCase));

        private static string ToTitleCase(string value) =>
            string.IsNullOrWhiteSpace(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];
    }
}
