using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Simulates one complete autobattler round.
    /// </summary>
    public sealed class RoundEngine
    {
        private static readonly Lane[] LaneAssignments =
        [
            Lane.Top,
            Lane.Top,
            Lane.Mid,
            Lane.Bottom,
            Lane.Bottom
        ];

        private readonly RoundSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoundEngine"/> class.
        /// </summary>
        /// <param name="settings">The round settings.</param>
        public RoundEngine(RoundSettings? settings = null)
        {
            _settings = settings ?? new RoundSettings();
        }

        /// <summary>
        /// Creates initial round state for two teams.
        /// </summary>
        public RoundState CreateState(
            IEnumerable<ChampionDefinition> blueRoster,
            IEnumerable<ChampionDefinition> redRoster,
            int seed)
        {
            ArgumentNullException.ThrowIfNull(blueRoster);
            ArgumentNullException.ThrowIfNull(redRoster);

            TeamRoundState blueTeam = CreateTeam(TeamSide.Blue, blueRoster, "blue");
            TeamRoundState redTeam = CreateTeam(TeamSide.Red, redRoster, "red");

            return new RoundState(blueTeam, redTeam, new SeededMatchRandom(seed), _settings);
        }

        /// <summary>
        /// Simulates one round to completion.
        /// </summary>
        public RoundResult Simulate(
            IEnumerable<ChampionDefinition> blueRoster,
            IEnumerable<ChampionDefinition> redRoster,
            int seed)
        {
            RoundState state = CreateState(blueRoster, redRoster, seed);

            while (state.CurrentTime < state.Settings.RoundDurationSeconds)
            {
                double deltaSeconds = Math.Min(
                    state.Settings.TickRateSeconds,
                    state.Settings.RoundDurationSeconds - state.CurrentTime);

                Tick(state, deltaSeconds);
            }

            return CreateResult(state);
        }

        /// <summary>
        /// Advances the round by a single simulation tick.
        /// </summary>
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
                TickCasting(champion, deltaSeconds, state);
            }

            HandleDeaths(state, null);
            RespawnReadyChampions(allChampions);
            UpdateRetreatIntents(allChampions);
            MoveChampions(state, deltaSeconds);
            ApplyFarmingAndRegeneration(allChampions, state, deltaSeconds);
            UpdateRetreatIntents(allChampions);
            CreateFights(state);
            JoinFights(state);
            ResolveCombatActions(state);
            EndInactiveFights(state);
            ClearRespawnMarkers(allChampions);

            state.CurrentTime = Math.Min(state.Settings.RoundDurationSeconds, state.CurrentTime + deltaSeconds);
        }

        /// <summary>
        /// Awards experience and applies any resulting level ups.
        /// </summary>
        public void AwardExperience(ChampionInstance champion, int experience)
        {
            ArgumentNullException.ThrowIfNull(champion);

            if (experience <= 0)
            {
                return;
            }

            champion.Experience += experience;
            champion.ExperienceProgress += experience;
            ApplyLevelUps(champion);
        }

        private static TeamRoundState CreateTeam(
            TeamSide side,
            IEnumerable<ChampionDefinition> roster,
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

        private static double ClampLanePosition(double lanePosition) => Math.Clamp(lanePosition, -100.0, 100.0);

        private void TickCasting(ChampionInstance champion, double deltaSeconds, RoundState state)
        {
            if (!champion.IsAlive || !champion.IsCasting || champion.PendingAbility is null)
            {
                return;
            }

            champion.CastTimer = Math.Max(0, champion.CastTimer - deltaSeconds);

            if (champion.CastTimer > 0)
            {
                return;
            }

            FightState? fight = GetFight(state, champion.FightId);
            if (fight is not null && champion.Intent != ChampionIntent.Retreating)
            {
                IReadOnlyList<ChampionInstance> activeChampions = GetActiveParticipants(fight, state.Settings);
                ChampionCombatResolver.ResolveAbility(champion, state.AllChampions, activeChampions, state.Rng);
                HandleDeaths(state, champion);
                champion.AbilityCooldown = champion.Definition.Ability.Cooldown;
            }

            CancelCast(champion);
        }

        private static void CancelCast(ChampionInstance champion)
        {
            champion.IsCasting = false;
            champion.CastTimer = 0;
            champion.PendingAbility = null;
        }

        private static void RespawnReadyChampions(IEnumerable<ChampionInstance> champions)
        {
            foreach (ChampionInstance champion in champions.Where(
                champion => !champion.IsAlive && champion.IsDeathProcessed && champion.RespawnTimer <= 0))
            {
                champion.CurrentHealth = champion.MaximumHealth;
                champion.JustRespawned = true;
                champion.IsDeathProcessed = false;
                champion.Shields.Clear();
                champion.Position = champion.Definition.DefaultPosition;
                champion.Intent = ChampionIntent.Laning;
                champion.FightId = null;
                champion.CurrentFightPosition = null;
                CancelCast(champion);
                champion.AbilityCooldown = champion.Definition.Ability.Cooldown;
                champion.AttackTimer = champion.Definition.AttackSpeed > 0 ? 1.0 / champion.Definition.AttackSpeed : 0;
                champion.LanePosition = champion.TeamSide == TeamSide.Blue ? -100.0 : 100.0;
            }
        }

        private void UpdateRetreatIntents(IEnumerable<ChampionInstance> champions)
        {
            foreach (ChampionInstance champion in champions.Where(champion => champion.IsAlive))
            {
                double healthPercent = champion.MaximumHealth <= 0
                    ? 0
                    : champion.CurrentHealth / (double)champion.MaximumHealth;

                if (champion.Intent == ChampionIntent.Laning && healthPercent < _settings.RetreatHealthThreshold)
                {
                    champion.Intent = ChampionIntent.Retreating;
                    CancelCast(champion);
                }
                else if (champion.Intent == ChampionIntent.Retreating && champion.CurrentHealth >= champion.MaximumHealth)
                {
                    champion.Intent = ChampionIntent.Laning;
                }
            }
        }

        private void MoveChampions(RoundState state, double deltaSeconds)
        {
            foreach (ChampionInstance champion in state.AllChampions.Where(champion => champion.IsAlive))
            {
                if (champion.JustRespawned)
                {
                    continue;
                }

                double targetPosition = GetMovementTarget(champion, state);
                double speed = GetMovementSpeed(champion);
                MoveToward(champion, targetPosition, speed * deltaSeconds);
            }
        }

        private double GetMovementTarget(ChampionInstance champion, RoundState state)
        {
            if (champion.Intent == ChampionIntent.Retreating)
            {
                return champion.TeamSide == TeamSide.Blue ? -100.0 : 100.0;
            }

            FightState? fight = GetFight(state, champion.FightId);
            if (fight is null)
            {
                return champion.TeamSide == TeamSide.Blue ? 100.0 : -100.0;
            }

            if (champion.Position == FormationPosition.Backline)
            {
                return champion.TeamSide == TeamSide.Blue
                    ? fight.Position - state.Settings.BacklineOffset
                    : fight.Position + state.Settings.BacklineOffset;
            }

            return fight.Position;
        }

        private double GetMovementSpeed(ChampionInstance champion)
        {
            if (champion.Intent == ChampionIntent.Retreating)
            {
                return _settings.RetreatMoveSpeed;
            }

            return champion.FightId.HasValue ? _settings.FightMoveSpeed : _settings.LaneMoveSpeed;
        }

        private static void MoveToward(ChampionInstance champion, double targetPosition, double maxDistance)
        {
            double distance = targetPosition - champion.LanePosition;
            double movement = Math.Abs(distance) <= maxDistance
                ? distance
                : Math.Sign(distance) * maxDistance;

            champion.LanePosition = ClampLanePosition(champion.LanePosition + movement);
        }

        private void ApplyFarmingAndRegeneration(
            IEnumerable<ChampionInstance> champions,
            RoundState state,
            double deltaSeconds)
        {
            foreach (ChampionInstance champion in champions)
            {
                if (!champion.IsAlive)
                {
                    continue;
                }

                double healing = state.Settings.PassiveHealthRegenPerSecond * deltaSeconds;
                if (IsNearOwnBase(champion, state.Settings))
                {
                    healing += champion.MaximumHealth * state.Settings.BaseHealPercentPerSecond * deltaSeconds;
                }

                AddHealing(champion, healing);

                if (champion.JustRespawned || champion.Intent != ChampionIntent.Laning || champion.FightId.HasValue)
                {
                    continue;
                }

                AddGold(champion, state.Settings.FarmGoldPerSecond * deltaSeconds);
                AddExperience(champion, state.Settings.FarmXpPerSecond * deltaSeconds);
            }
        }

        private static bool IsNearOwnBase(ChampionInstance champion, RoundSettings settings) =>
            champion.TeamSide == TeamSide.Blue
                ? champion.LanePosition <= -100.0 + settings.BaseHealRange
                : champion.LanePosition >= 100.0 - settings.BaseHealRange;

        private static void AddHealing(ChampionInstance champion, double healing)
        {
            champion.HealingProgress += healing;
            int wholeHealing = (int)Math.Floor(champion.HealingProgress);
            if (wholeHealing <= 0)
            {
                return;
            }

            champion.HealingProgress -= wholeHealing;
            CombatEffectApplicator.ApplyHeal(champion, wholeHealing);
        }

        private void AddGold(ChampionInstance champion, double gold)
        {
            champion.GoldProgress += gold;
            int wholeGold = (int)Math.Floor(champion.GoldProgress);
            if (wholeGold <= champion.Gold)
            {
                return;
            }

            champion.Gold = wholeGold;
        }

        private void AddExperience(ChampionInstance champion, double experience)
        {
            champion.ExperienceProgress += experience;
            int wholeExperience = (int)Math.Floor(champion.ExperienceProgress);
            if (wholeExperience > champion.Experience)
            {
                champion.Experience = wholeExperience;
                ApplyLevelUps(champion);
            }
        }

        private void ApplyLevelUps(ChampionInstance champion)
        {
            while (champion.Level < _settings.MaxLevel && champion.Experience >= champion.Level * 100)
            {
                champion.Level++;
                champion.MaximumHealth += _settings.HealthPerLevel;
                champion.CurrentHealth = Math.Min(
                    champion.MaximumHealth,
                    champion.CurrentHealth + _settings.HealthPerLevel);
                champion.CurrentPower += _settings.PowerPerLevel;
            }
        }

        private void CreateFights(RoundState state)
        {
            foreach (Lane lane in Enum.GetValues<Lane>())
            {
                if (state.ActiveFights.Any(fight => fight.Lane == lane))
                {
                    continue;
                }

                List<(ChampionInstance Blue, ChampionInstance Red, double Distance)> pairs =
                    state.BlueTeam.Champions
                        .Where(champion => IsEligibleToStartFight(champion, lane))
                        .SelectMany(
                            blue => state.RedTeam.Champions
                                .Where(red => IsEligibleToStartFight(red, lane))
                                .Select(red => (Blue: blue, Red: red, Distance: Math.Abs(blue.LanePosition - red.LanePosition))))
                        .Where(pair => pair.Distance <= state.Settings.EngageRange)
                        .OrderBy(pair => pair.Distance)
                        .ToList();

                if (pairs.Count == 0)
                {
                    continue;
                }

                (ChampionInstance blue, ChampionInstance red, _) = pairs[0];
                double fightPosition = (blue.LanePosition + red.LanePosition) / 2.0;
                FightState fight = new()
                {
                    Lane = lane,
                    Position = fightPosition
                };

                List<ChampionInstance> initialParticipants = state.AllChampions
                    .Where(champion => IsEligibleToStartFight(champion, lane))
                    .Where(champion => Math.Abs(champion.LanePosition - fightPosition) <= state.Settings.EngageRange)
                    .ToList();

                if (!initialParticipants.Any(champion => champion.TeamSide == TeamSide.Blue)
                    || !initialParticipants.Any(champion => champion.TeamSide == TeamSide.Red))
                {
                    continue;
                }

                foreach (ChampionInstance participant in initialParticipants)
                {
                    AddParticipant(fight, participant, state.Settings);
                }

                state.ActiveFights.Add(fight);
            }
        }

        private static bool IsEligibleToStartFight(ChampionInstance champion, Lane lane) =>
            champion.IsAlive
            && !champion.JustRespawned
            && champion.Lane == lane
            && champion.Intent == ChampionIntent.Laning
            && !champion.FightId.HasValue;

        private void JoinFights(RoundState state)
        {
            foreach (FightState fight in state.ActiveFights)
            {
                foreach (ChampionInstance champion in state.AllChampions)
                {
                    if (!IsEligibleToStartFight(champion, fight.Lane))
                    {
                        continue;
                    }

                    if (Math.Abs(champion.LanePosition - fight.Position) <= state.Settings.EngageRange)
                    {
                        AddParticipant(fight, champion, state.Settings);
                    }
                }
            }
        }

        private static void AddParticipant(FightState fight, ChampionInstance champion, RoundSettings settings)
        {
            if (!fight.Participants.Contains(champion))
            {
                fight.Participants.Add(champion);
            }

            champion.FightId = fight.Id;
            champion.CurrentFightPosition = fight.Position;
            champion.CurrentBacklineOffset = settings.BacklineOffset;
        }

        private void ResolveCombatActions(RoundState state)
        {
            foreach (FightState fight in state.ActiveFights.ToList())
            {
                IReadOnlyList<ChampionInstance> activeChampions = GetActiveParticipants(fight, state.Settings);
                foreach (ChampionInstance champion in activeChampions)
                {
                    champion.CurrentFightPosition = fight.Position;
                    champion.CurrentBacklineOffset = state.Settings.BacklineOffset;
                }

                foreach (ChampionInstance champion in activeChampions)
                {
                    if (champion.JustRespawned)
                    {
                        continue;
                    }

                    if (!CanAct(champion))
                    {
                        continue;
                    }

                    if (champion.AbilityCooldown <= 0 && IsAbilityUseful(champion, state, activeChampions))
                    {
                        champion.IsCasting = true;
                        champion.CastTimer = champion.Definition.Ability.CastTime;
                        champion.PendingAbility = champion.Definition.Ability;
                        continue;
                    }

                    if (champion.AttackTimer <= 0)
                    {
                        ChampionCombatResolver.ResolveAttack(champion, state.AllChampions, activeChampions, state.Rng);
                        champion.AttackTimer = champion.Definition.AttackSpeed > 0
                            ? 1.0 / champion.Definition.AttackSpeed
                            : 0;
                        HandleDeaths(state, champion);
                    }
                }
            }
        }

        private static bool CanAct(ChampionInstance champion) =>
            champion.IsAlive
            && champion.FightId.HasValue
            && champion.Intent != ChampionIntent.Retreating
            && !champion.IsCasting;

        private bool IsAbilityUseful(
            ChampionInstance champion,
            RoundState state,
            IEnumerable<ChampionInstance> activeChampions)
        {
            foreach (CombatEffect effect in champion.Definition.Ability.Effects)
            {
                IReadOnlyList<ChampionInstance> targets = CombatTargeting.SelectTargets(
                    champion,
                    effect,
                    state.AllChampions,
                    activeChampions,
                    new SeededMatchRandom(0));

                if (effect.Type == CombatEffectType.Heal)
                {
                    if (targets.Any(target => target.CurrentHealth < target.MaximumHealth))
                    {
                        return true;
                    }

                    continue;
                }

                if (targets.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleDeaths(RoundState state, ChampionInstance? killer)
        {
            foreach (ChampionInstance champion in state.AllChampions)
            {
                if (champion.IsAlive || champion.IsDeathProcessed)
                {
                    continue;
                }

                champion.IsDeathProcessed = true;
                champion.Shields.Clear();
                CancelCast(champion);
                champion.FightId = null;
                champion.CurrentFightPosition = null;
                champion.RespawnTimer = state.Settings.RespawnDurationSeconds;
                GetTeam(state, champion.TeamSide == TeamSide.Blue ? TeamSide.Red : TeamSide.Blue).KillScore++;

                if (killer is not null && killer.IsAlive && killer.TeamSide != champion.TeamSide)
                {
                    AwardExperience(killer, state.Settings.KillXp);
                }
            }
        }

        private void EndInactiveFights(RoundState state)
        {
            foreach (FightState fight in state.ActiveFights.ToList())
            {
                foreach (ChampionInstance participant in fight.Participants.ToList())
                {
                    if (!participant.IsAlive)
                    {
                        participant.FightId = null;
                        participant.CurrentFightPosition = null;
                        continue;
                    }

                    if (participant.Intent == ChampionIntent.Retreating
                        && Math.Abs(participant.LanePosition - fight.Position) > state.Settings.EngageRange)
                    {
                        participant.FightId = null;
                        participant.CurrentFightPosition = null;
                    }
                }

                IReadOnlyList<ChampionInstance> activeParticipants = GetActiveParticipants(fight, state.Settings);
                bool hasBlue = activeParticipants.Any(champion => champion.TeamSide == TeamSide.Blue);
                bool hasRed = activeParticipants.Any(champion => champion.TeamSide == TeamSide.Red);

                if (hasBlue && hasRed)
                {
                    continue;
                }

                if (hasBlue || hasRed)
                {
                    TeamSide winningSide = hasBlue ? TeamSide.Blue : TeamSide.Red;
                    foreach (ChampionInstance winner in activeParticipants.Where(champion => champion.TeamSide == winningSide))
                    {
                        AwardExperience(winner, state.Settings.FightWinXp);
                    }
                }

                foreach (ChampionInstance participant in fight.Participants)
                {
                    if (participant.FightId == fight.Id)
                    {
                        participant.FightId = null;
                    }

                    participant.CurrentFightPosition = null;
                }

                state.ActiveFights.Remove(fight);
            }
        }

        private static IReadOnlyList<ChampionInstance> GetActiveParticipants(FightState fight, RoundSettings settings) =>
            fight.Participants
                .Where(champion => champion.IsAlive)
                .Where(champion => champion.FightId == fight.Id)
                .Where(champion => champion.Intent != ChampionIntent.Retreating
                    || Math.Abs(champion.LanePosition - fight.Position) <= settings.EngageRange)
                .ToList();

        private static FightState? GetFight(RoundState state, Guid? fightId) =>
            fightId.HasValue
                ? state.ActiveFights.FirstOrDefault(fight => fight.Id == fightId.Value)
                : null;

        private static TeamRoundState GetTeam(RoundState state, TeamSide side) =>
            side == TeamSide.Blue ? state.BlueTeam : state.RedTeam;

        private static void ClearRespawnMarkers(IEnumerable<ChampionInstance> champions)
        {
            foreach (ChampionInstance champion in champions)
            {
                champion.JustRespawned = false;
            }
        }

        private RoundResult CreateResult(RoundState state)
        {
            int blueGold = state.BlueTeam.Champions.Sum(champion => champion.Gold);
            int redGold = state.RedTeam.Champions.Sum(champion => champion.Gold);
            int blueExperience = state.BlueTeam.Champions.Sum(champion => champion.Experience);
            int redExperience = state.RedTeam.Champions.Sum(champion => champion.Experience);
            TeamSide winningSide = DetermineWinner(state, blueGold, redGold, blueExperience, redExperience);

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
                EventLog = state.EventLog.ToList()
            };
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
