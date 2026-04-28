using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Models;

namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Generates complete Management Layer world state.
    /// </summary>
    public sealed class WorldGenerationService
    {
        private static readonly CompetitiveTierName[] TierNames =
        [
            CompetitiveTierName.Amateur,
            CompetitiveTierName.Professional,
            CompetitiveTierName.World
        ];

        private static readonly LeagueRegion[] Regions =
        [
            LeagueRegion.Northern,
            LeagueRegion.Southern,
            LeagueRegion.Eastern,
            LeagueRegion.Western
        ];

        private static readonly DivisionName[] Divisions =
        [
            DivisionName.Apex,
            DivisionName.Vanguard,
            DivisionName.Frontier,
            DivisionName.Summit
        ];

        private readonly LeagueGenerationService _leagueGenerationService;
        private readonly ScheduleGenerationService _scheduleGenerationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldGenerationService"/> class.
        /// </summary>
        /// <param name="leagueGenerationService">The league generation service.</param>
        /// <param name="scheduleGenerationService">The schedule generation service.</param>
        public WorldGenerationService(
            LeagueGenerationService? leagueGenerationService = null,
            ScheduleGenerationService? scheduleGenerationService = null)
        {
            _scheduleGenerationService = scheduleGenerationService ?? new ScheduleGenerationService();
            _leagueGenerationService = leagueGenerationService ?? new LeagueGenerationService(_scheduleGenerationService);
        }

        /// <summary>
        /// Creates a new world.
        /// </summary>
        /// <param name="seed">The deterministic seed.</param>
        /// <param name="humanCoachName">The human coach name.</param>
        /// <param name="humanTeamName">The human team name.</param>
        /// <returns>The generated world state.</returns>
        public WorldState CreateWorld(int seed, string? humanCoachName = null, string? humanTeamName = null)
        {
            Random rng = new(seed);
            NameGenerationService nameGenerationService = new(seed);
            string coachName = ResolveCoachName(nameGenerationService, humanCoachName);
            string teamName = ResolveTeamName(nameGenerationService, humanTeamName);
            HumanPlacement humanPlacement = new(
                Regions[rng.Next(Regions.Length)],
                Divisions[rng.Next(Divisions.Length)],
                rng.Next(1, 6));
            List<CompetitiveTier> tiers = [];
            List<Coach> coaches = [];
            List<Player> players = [];

            foreach (CompetitiveTierName tierName in TierNames)
            {
                List<League> leagues = [];
                foreach (LeagueRegion region in Regions)
                {
                    HumanPlacement? placement = tierName == CompetitiveTierName.Amateur ? humanPlacement : null;
                    LeagueGenerationResult result = _leagueGenerationService.GenerateLeague(
                        tierName,
                        region,
                        placement,
                        nameGenerationService,
                        coachName,
                        teamName);
                    leagues.Add(result.League);
                    coaches.AddRange(result.Coaches);
                    players.AddRange(result.Players);
                }

                tiers.Add(new CompetitiveTier
                {
                    Id = tierName.ToString().ToLowerInvariant(),
                    Leagues = leagues,
                    Name = tierName
                });
            }

            return new WorldState
            {
                Coaches = coaches,
                HumanCoachId = "human-coach",
                Players = players,
                Season = new SeasonState
                {
                    CurrentWeek = 1,
                    WorldChampionshipSchedule = _scheduleGenerationService.GenerateWorldChampionshipReservations(),
                    Year = 1
                },
                Seed = seed,
                Tiers = tiers,
                WorldChampionshipHistory = new WorldChampionshipHistory()
            };
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
