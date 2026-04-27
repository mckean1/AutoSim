using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Simulates one complete autobattler round.
    /// </summary>
    public sealed class RoundEngine
    {
        private const int TeamRosterSize = 5;

        private static readonly Lane[] LaneAssignments =
        [
            Lane.Top,
            Lane.Top,
            Lane.Mid,
            Lane.Bottom,
            Lane.Bottom
        ];

        private readonly CombatActionService _combatActionService;
        private readonly ChampionMovementService _movementService;
        private readonly ChampionProgressionService _progressionService;
        private readonly ChampionRecoveryService _recoveryService;
        private readonly DeathRespawnService _deathRespawnService;
        private readonly FightService _fightService;
        private readonly RoundSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoundEngine"/> class.
        /// </summary>
        /// <param name="settings">The round settings.</param>
        public RoundEngine(RoundSettings? settings = null)
        {
            _settings = settings ?? new RoundSettings();
            _progressionService = new ChampionProgressionService(_settings);
            _deathRespawnService = new DeathRespawnService(_settings, _progressionService);
            _combatActionService = new CombatActionService(_settings, _deathRespawnService);
            _movementService = new ChampionMovementService(_settings);
            _recoveryService = new ChampionRecoveryService(_settings);
            _fightService = new FightService(_settings, _progressionService);
        }

        /// <summary>
        /// Creates initial round state for two teams.
        /// </summary>
        /// <param name="blueRoster">The blue team roster.</param>
        /// <param name="redRoster">The red team roster.</param>
        /// <param name="seed">The deterministic round seed.</param>
        /// <returns>The initialized round state.</returns>
        public RoundState CreateState(
            IEnumerable<ChampionDefinition> blueRoster,
            IEnumerable<ChampionDefinition> redRoster,
            int seed)
        {
            ArgumentNullException.ThrowIfNull(blueRoster);
            ArgumentNullException.ThrowIfNull(redRoster);

            return CreateState(
                new RoundRoster
                {
                    BlueChampions = blueRoster.ToList(),
                    RedChampions = redRoster.ToList()
                },
                seed);
        }

        /// <summary>
        /// Creates initial round state for the selected round roster.
        /// </summary>
        /// <param name="roster">The selected round roster.</param>
        /// <param name="seed">The deterministic round seed.</param>
        /// <returns>The initialized round state.</returns>
        public RoundState CreateState(RoundRoster roster, int seed)
        {
            ArgumentNullException.ThrowIfNull(roster);
            ValidateRoster(roster);

            TeamRoundState blueTeam = CreateTeam(TeamSide.Blue, roster.BlueChampions, "blue");
            TeamRoundState redTeam = CreateTeam(TeamSide.Red, roster.RedChampions, "red");

            return new RoundState(blueTeam, redTeam, new SeededMatchRandom(seed), _settings);
        }

        /// <summary>
        /// Simulates one round to completion.
        /// </summary>
        /// <param name="blueRoster">The blue team roster.</param>
        /// <param name="redRoster">The red team roster.</param>
        /// <param name="seed">The deterministic round seed.</param>
        /// <returns>The round result.</returns>
        public RoundResult Simulate(
            IEnumerable<ChampionDefinition> blueRoster,
            IEnumerable<ChampionDefinition> redRoster,
            int seed)
        {
            ArgumentNullException.ThrowIfNull(blueRoster);
            ArgumentNullException.ThrowIfNull(redRoster);

            return Simulate(
                new RoundRoster
                {
                    BlueChampions = blueRoster.ToList(),
                    RedChampions = redRoster.ToList()
                },
                seed);
        }

        /// <summary>
        /// Simulates one round to completion.
        /// </summary>
        /// <param name="roster">The selected round roster.</param>
        /// <param name="seed">The deterministic round seed.</param>
        /// <returns>The round result.</returns>
        public RoundResult Simulate(RoundRoster roster, int seed)
        {
            RoundState state = CreateState(roster, seed);
            state.AddEvent(new RoundEvent
            {
                TimeSeconds = 0,
                Type = RoundEventType.RoundStarted,
                Message = "Round started."
            });

            while (state.CurrentTime < state.Settings.RoundDurationSeconds)
            {
                double deltaSeconds = Math.Min(
                    state.Settings.TickRateSeconds,
                    state.Settings.RoundDurationSeconds - state.CurrentTime);

                Tick(state, deltaSeconds);
            }

            _fightService.CloseActiveFightsForRoundEnd(state);
            TeamSide winningSide = DetermineWinner(state);
            state.AddEvent(new RoundEvent
            {
                TimeSeconds = state.CurrentTime,
                Type = RoundEventType.RoundEnded,
                TeamSide = winningSide.ToString(),
                SourceTeamSide = winningSide.ToString(),
                Message = $"Round ended. Winner: {winningSide}."
            });

            return CreateResultCore(state, winningSide);
        }

        /// <summary>
        /// Advances the round by a single simulation tick.
        /// </summary>
        /// <param name="state">The round state.</param>
        /// <param name="deltaSeconds">The elapsed time in seconds.</param>
        public void Tick(RoundState state, double deltaSeconds)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (deltaSeconds <= 0)
            {
                return;
            }

            IReadOnlyList<ChampionInstance> allChampions = state.AllChampions;
            foreach (ChampionInstance champion in allChampions)
            {
                ChampionRuntimeTicker.TickChampion(champion, deltaSeconds);
                _combatActionService.TickCasting(champion, deltaSeconds, state);
            }

            _deathRespawnService.ProcessDeaths(state, null);
            _deathRespawnService.RespawnReadyChampions(state);
            _deathRespawnService.UpdateRetreatIntents(allChampions, state);
            _movementService.MoveChampions(state, deltaSeconds);
            _recoveryService.ApplyRegeneration(allChampions, deltaSeconds);
            _progressionService.ApplyFarming(allChampions, deltaSeconds);
            _deathRespawnService.UpdateRetreatIntents(allChampions, state);
            _fightService.CreateFights(state);
            _fightService.JoinFights(state);
            _combatActionService.ResolveCombatActions(state);
            _fightService.EndInactiveFights(state);
            DeathRespawnService.ClearRespawnMarkers(allChampions);

            state.CurrentTime = Math.Min(state.Settings.RoundDurationSeconds, state.CurrentTime + deltaSeconds);
        }

        /// <summary>
        /// Awards experience and applies any resulting level ups.
        /// </summary>
        /// <param name="champion">The champion receiving experience.</param>
        /// <param name="experience">The experience to award.</param>
        public void AwardExperience(ChampionInstance champion, int experience) =>
            _progressionService.AddExperience(champion, experience);

        private static TeamRoundState CreateTeam(
            TeamSide side,
            IReadOnlyList<ChampionDefinition> roster,
            string playerId)
        {
            List<ChampionInstance> champions = roster
                .Select((definition, index) => CreateChampion(side, definition, playerId, index))
                .ToList();

            return new TeamRoundState(side, champions);
        }

        private static ChampionInstance CreateChampion(
            TeamSide side,
            ChampionDefinition definition,
            string playerId,
            int index)
        {
            ChampionInstance champion = ChampionInstanceFactory.Create(definition, playerId);
            champion.TeamSide = side;
            champion.Lane = LaneAssignments[Math.Min(index, LaneAssignments.Length - 1)];
            champion.LanePosition = side == TeamSide.Blue ? -20.0 : 20.0;
            champion.Intent = ChampionIntent.Laning;
            champion.FightId = null;
            champion.CurrentFightPosition = null;
            champion.Position = definition.DefaultPosition;
            return champion;
        }

        private static void ValidateRoster(RoundRoster roster)
        {
            if (roster.BlueChampions.Count != TeamRosterSize)
            {
                throw new ArgumentException("Blue roster must contain exactly 5 champions.", nameof(roster));
            }

            if (roster.RedChampions.Count != TeamRosterSize)
            {
                throw new ArgumentException("Red roster must contain exactly 5 champions.", nameof(roster));
            }

            string? duplicateBlueId = FindDuplicateChampionId(roster.BlueChampions);
            if (duplicateBlueId is not null)
            {
                throw new ArgumentException($"Duplicate champion id in round roster: {duplicateBlueId}.", nameof(roster));
            }

            string? duplicateRedId = FindDuplicateChampionId(roster.RedChampions);
            if (duplicateRedId is not null)
            {
                throw new ArgumentException($"Duplicate champion id in round roster: {duplicateRedId}.", nameof(roster));
            }

            HashSet<string> blueIds = roster.BlueChampions.Select(champion => champion.Id).ToHashSet(StringComparer.Ordinal);
            if (roster.RedChampions.Any(champion => blueIds.Contains(champion.Id)))
            {
                throw new ArgumentException("Champion ids must be unique across both teams.", nameof(roster));
            }
        }

        private static string? FindDuplicateChampionId(IReadOnlyList<ChampionDefinition> champions)
        {
            HashSet<string> ids = new(StringComparer.Ordinal);
            foreach (ChampionDefinition champion in champions)
            {
                if (!ids.Add(champion.Id))
                {
                    return champion.Id;
                }
            }

            return null;
        }

        private RoundResult CreateResult(RoundState state) => CreateResultCore(state, null);

        private RoundResult CreateResultCore(RoundState state, TeamSide? knownWinningSide)
        {
            int blueGold = state.BlueTeam.Champions.Sum(champion => champion.Gold);
            int redGold = state.RedTeam.Champions.Sum(champion => champion.Gold);
            int blueExperience = state.BlueTeam.Champions.Sum(champion => champion.Experience);
            int redExperience = state.RedTeam.Champions.Sum(champion => champion.Experience);
            TeamSide winningSide = knownWinningSide
                ?? DetermineWinner(state, blueGold, redGold, blueExperience, redExperience);

            return new RoundResult
            {
                WinningSide = winningSide,
                BlueKills = state.BlueTeam.KillScore,
                RedKills = state.RedTeam.KillScore,
                BlueGold = blueGold,
                RedGold = redGold,
                BlueExperience = blueExperience,
                RedExperience = redExperience,
                Duration = state.CurrentTime,
                ActiveFightCount = state.ActiveFights.Count,
                ChampionSummaries = state.AllChampions.Select(CreateChampionSummary).ToList(),
                Events = state.Events.ToList()
            };
        }

        private static ChampionRoundSummary CreateChampionSummary(ChampionInstance champion) =>
            new()
            {
                ChampionId = champion.Definition.Id,
                ChampionName = champion.Definition.Name,
                TeamSide = champion.TeamSide,
                Lane = champion.Lane,
                Level = champion.Level,
                Experience = champion.Experience,
                Gold = champion.Gold,
                Kills = champion.Kills,
                Deaths = champion.Deaths,
                FinalHealth = champion.CurrentHealth,
                MaximumHealth = champion.MaximumHealth
            };

        private static TeamSide DetermineWinner(RoundState state)
        {
            int blueGold = state.BlueTeam.Champions.Sum(champion => champion.Gold);
            int redGold = state.RedTeam.Champions.Sum(champion => champion.Gold);
            int blueExperience = state.BlueTeam.Champions.Sum(champion => champion.Experience);
            int redExperience = state.RedTeam.Champions.Sum(champion => champion.Experience);

            return DetermineWinner(state, blueGold, redGold, blueExperience, redExperience);
        }

        private static TeamSide DetermineWinner(
            RoundState state,
            int blueGold,
            int redGold,
            int blueExperience,
            int redExperience)
        {
            if (state.BlueTeam.KillScore != state.RedTeam.KillScore)
            {
                return state.BlueTeam.KillScore > state.RedTeam.KillScore ? TeamSide.Blue : TeamSide.Red;
            }

            if (blueGold != redGold)
            {
                return blueGold > redGold ? TeamSide.Blue : TeamSide.Red;
            }

            if (blueExperience != redExperience)
            {
                return blueExperience > redExperience ? TeamSide.Blue : TeamSide.Red;
            }

            return state.Rng.Next(2) == 0 ? TeamSide.Blue : TeamSide.Red;
        }
    }
}
