using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;

namespace AutoSim.Domain.Tests.Services
{
    internal sealed class RoundEventLoggingTests
    {
        [Test]
        public void Simulate_CompletedRound_IncludesRoundStartedAndRoundEndedEvents()
        {
            RoundSettings settings = new()
            {
                RoundDurationSeconds = 0.2,
                TickRateSeconds = 0.1
            };

            RoundResult result = new RoundEngine(settings).Simulate([CreateDefinition()], [CreateDefinition()], seed: 0);

            Assert.Multiple(() =>
            {
                Assert.That(result.Events, Is.Not.Empty);
                Assert.That(result.Events.Select(roundEvent => roundEvent.Type), Does.Contain(RoundEventType.RoundStarted));
                Assert.That(result.Events.Select(roundEvent => roundEvent.Type), Does.Contain(RoundEventType.RoundEnded));
            });
        }

        [Test]
        public void Tick_FightLifecycle_EmitsFightStartedJoinedEndedRetreatedAndEscapedEvents()
        {
            RoundState state = CreateState(
                [CreateDefinition(), CreateDefinition()],
                [CreateDefinition()]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance joiner = state.BlueTeam.Champions[1];
            ChampionInstance red = state.RedTeam.Champions[0];
            blue.LanePosition = 0;
            red.LanePosition = 1;
            joiner.Lane = Lane.Top;
            joiner.LanePosition = 20;

            Tick(state, 0.1);
            FightState fight = state.ActiveFights.Single();
            joiner.LanePosition = fight.Position + 1;
            Tick(state, 0.1);
            red.CurrentHealth = 1;
            Tick(state, 0.1);
            red.LanePosition = fight.Position + 20;
            Tick(state, 0.1);

            IReadOnlyList<RoundEventType> eventTypes = state.Events.Select(roundEvent => roundEvent.Type).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(eventTypes, Does.Contain(RoundEventType.FightStarted));
                Assert.That(eventTypes, Does.Contain(RoundEventType.FightJoined));
                Assert.That(eventTypes, Does.Contain(RoundEventType.ChampionRetreated));
                Assert.That(eventTypes, Does.Contain(RoundEventType.ChampionEscaped));
                Assert.That(eventTypes, Does.Contain(RoundEventType.FightEnded));
            });
        }

        [Test]
        public void Tick_RetreatingChampionEscapes_EmitsOneEscapeForSameFight()
        {
            RoundSettings settings = new();
            RoundState state = CreateState(
                [CreateDefinition()],
                [CreateDefinition(), CreateDefinition()],
                settings);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance redEscaper = state.RedTeam.Champions[0];
            ChampionInstance redActive = state.RedTeam.Champions[1];
            redActive.Lane = Lane.Top;
            StartFight(state, blue, redEscaper);
            AddParticipant(state.ActiveFights.Single(), redActive);
            FightState fight = state.ActiveFights.Single();
            redEscaper.Intent = ChampionIntent.Retreating;
            redEscaper.CurrentHealth = 100;
            redEscaper.LanePosition = fight.Position + settings.EngageRange + 1;

            Tick(state, 0.1);
            Tick(state, 0.1);

            IReadOnlyList<RoundEvent> escapes = state.Events
                .Where(roundEvent => roundEvent.Type == RoundEventType.ChampionEscaped)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(escapes, Has.Count.EqualTo(1));
                Assert.That(escapes.Single().FightId, Is.EqualTo(fight.Id));
                Assert.That(redEscaper.FightId, Is.Null);
                Assert.That(FightService.GetActiveParticipants(fight, settings), Does.Not.Contain(redEscaper));
            });
        }

        [Test]
        public void Tick_ChampionEscapesDifferentFight_AllowsSecondEscape()
        {
            RoundSettings settings = new();
            RoundState state = CreateState([CreateDefinition()], [CreateDefinition()], settings);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            StartFight(state, blue, red);
            FightState firstFight = state.ActiveFights.Single();
            red.Intent = ChampionIntent.Retreating;
            red.CurrentHealth = 100;
            red.LanePosition = firstFight.Position + settings.EngageRange + 1;

            Tick(state, 0.1);

            red.Intent = ChampionIntent.Laning;
            red.LanePosition = 0;
            StartFight(state, blue, red);
            FightState secondFight = state.ActiveFights.Single();
            red.Intent = ChampionIntent.Retreating;
            red.CurrentHealth = 100;
            red.LanePosition = secondFight.Position + settings.EngageRange + 1;

            Tick(state, 0.1);

            IReadOnlyList<RoundEvent> escapes = state.Events
                .Where(roundEvent => roundEvent.Type == RoundEventType.ChampionEscaped)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(escapes, Has.Count.EqualTo(2));
                Assert.That(escapes.Select(roundEvent => roundEvent.FightId), Is.Unique);
            });
        }

        [Test]
        public void Simulate_ActiveFightAtRoundEnd_EmitsFightEndedBeforeRoundEnded()
        {
            RoundSettings settings = new()
            {
                RoundDurationSeconds = 6,
                TickRateSeconds = 0.1
            };

            RoundResult result = new RoundEngine(settings).Simulate(
                [CreateDefinition(attackEffects: [])],
                [CreateDefinition(attackEffects: [])],
                seed: 0);

            List<RoundEvent> events = result.Events.ToList();
            RoundEvent fightEnded = events.Last(roundEvent => roundEvent.Type == RoundEventType.FightEnded);
            RoundEvent roundEnded = events.Single(roundEvent => roundEvent.Type == RoundEventType.RoundEnded);

            Assert.Multiple(() =>
            {
                Assert.That(result.ActiveFightCount, Is.EqualTo(0));
                Assert.That(events.IndexOf(fightEnded), Is.LessThan(events.IndexOf(roundEnded)));
                Assert.That(fightEnded.Message, Does.Contain("because the round ended"));
                Assert.That(fightEnded.FightId, Is.Not.Null);
                Assert.That(fightEnded.Lane, Is.Not.Null);
            });
        }

        [Test]
        public void Tick_FightStarted_IsNeutralAndIncludesBothChampions()
        {
            RoundState state = CreateState([CreateDefinition()], [CreateDefinition()]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            blue.LanePosition = 0;
            red.LanePosition = 1;

            Tick(state, 0.1);

            RoundEvent fightStarted = state.Events.Single(roundEvent => roundEvent.Type == RoundEventType.FightStarted);
            Assert.Multiple(() =>
            {
                Assert.That(fightStarted.TeamSide, Is.Null);
                Assert.That(fightStarted.Lane, Is.EqualTo(Lane.Top.ToString()));
                Assert.That(fightStarted.FightId, Is.Not.Null);
                Assert.That(fightStarted.Message, Does.Contain("Fight started between"));
                Assert.That(fightStarted.Message, Does.Contain("Blue Test Champion"));
                Assert.That(fightStarted.Message, Does.Contain("Red Test Champion"));
            });
        }

        [Test]
        public void Tick_CombatActionsAndEffects_EmitStructuredEvents()
        {
            AttackEffect attackDamage = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            AbilityEffect heal = CreateAbilityEffect(CombatEffectType.Heal, 20, TargetMode.AllyAny, TargetScope.One);
            AbilityEffect shield = CreateAbilityEffect(CombatEffectType.Shield, 15, TargetMode.AllyAny, TargetScope.One);
            AbilityEffect damage = CreateAbilityEffect(CombatEffectType.Damage, 25, TargetMode.EnemyAny, TargetScope.One);
            RoundState state = CreateState(
                [CreateDefinition(attackEffects: [attackDamage], abilityEffects: [heal, shield, damage])],
                [CreateDefinition(attackEffects: [])]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            blue.AbilityCooldown = 0;
            blue.AttackTimer = 0;
            blue.CurrentHealth -= 30;
            StartFight(state, blue, red);

            Tick(state, 0.1);
            Tick(state, 0.5);
            blue.AbilityCooldown = 10;
            blue.AttackTimer = 0;
            Tick(state, 0.1);

            IReadOnlyList<RoundEventType> eventTypes = state.Events.Select(roundEvent => roundEvent.Type).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(eventTypes, Does.Contain(RoundEventType.AbilityCastStarted));
                Assert.That(eventTypes, Does.Contain(RoundEventType.AbilityResolved));
                Assert.That(eventTypes, Does.Contain(RoundEventType.HealingDone));
                Assert.That(eventTypes, Does.Contain(RoundEventType.ShieldApplied));
                Assert.That(eventTypes, Does.Contain(RoundEventType.DamageDealt));
                Assert.That(eventTypes, Does.Contain(RoundEventType.AttackResolved));
            });
        }

        [Test]
        public void Tick_DamageEvent_IncludesSourceAndTargetSides()
        {
            AttackEffect attackDamage = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            RoundState state = CreateState(
                [CreateDefinition(attackEffects: [attackDamage])],
                [CreateDefinition(attackEffects: [])]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            StartFight(state, blue, red);
            blue.AttackTimer = 0;

            Tick(state, 0.1);

            RoundEvent damage = state.Events.Single(roundEvent => roundEvent.Type == RoundEventType.DamageDealt);
            Assert.Multiple(() =>
            {
                Assert.That(damage.SourceTeamSide, Is.EqualTo(TeamSide.Blue.ToString()));
                Assert.That(damage.TargetTeamSide, Is.EqualTo(TeamSide.Red.ToString()));
                Assert.That(damage.SourcePlayerId, Is.EqualTo(blue.PlayerId));
                Assert.That(damage.TargetPlayerId, Is.EqualTo(red.PlayerId));
            });
        }

        [Test]
        public void Tick_DeathRespawnAndLevelUp_EmitChampionEvents()
        {
            AttackEffect lethal = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            RoundSettings settings = new()
            {
                KillXp = 100,
                RespawnDurationSeconds = 0.2,
                TickRateSeconds = 0.1
            };
            RoundState state = CreateState(
                [CreateDefinition(attackPower: 2000, attackEffects: [lethal])],
                [CreateDefinition()],
                settings);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            StartFight(state, blue, red);
            blue.AttackTimer = 0;

            Tick(state, 0.1);
            Tick(state, 0.3);

            IReadOnlyList<RoundEventType> eventTypes = state.Events.Select(roundEvent => roundEvent.Type).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(eventTypes, Does.Contain(RoundEventType.ChampionKilled));
                Assert.That(eventTypes, Does.Contain(RoundEventType.ChampionRespawned));
                Assert.That(eventTypes, Does.Contain(RoundEventType.ChampionLeveledUp));
            });
        }

        [Test]
        public void Tick_ChampionKilled_IncludesKillerAndKilledChampion()
        {
            AttackEffect lethal = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            RoundState state = CreateState(
                [CreateDefinition(attackPower: 2000, attackEffects: [lethal])],
                [CreateDefinition()]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            StartFight(state, blue, red);
            blue.AttackTimer = 0;

            Tick(state, 0.1);

            RoundEvent killed = state.Events.Single(roundEvent => roundEvent.Type == RoundEventType.ChampionKilled);
            Assert.Multiple(() =>
            {
                Assert.That(killed.SourceChampionId, Is.EqualTo(blue.Definition.Id));
                Assert.That(killed.SourceTeamSide, Is.EqualTo(TeamSide.Blue.ToString()));
                Assert.That(killed.TargetChampionId, Is.EqualTo(red.Definition.Id));
                Assert.That(killed.TargetTeamSide, Is.EqualTo(TeamSide.Red.ToString()));
                Assert.That(killed.FightId, Is.Not.Null);
            });
        }

        [Test]
        public void Simulate_CompletedRound_HasConsistentEventLifecycle()
        {
            RoundSettings settings = new()
            {
                RoundDurationSeconds = 6,
                TickRateSeconds = 0.1
            };

            RoundResult result = new RoundEngine(settings).Simulate(
                [CreateDefinition(attackEffects: [])],
                [CreateDefinition(attackEffects: [])],
                seed: 0);

            IReadOnlyList<RoundEvent> fightStarted = result.Events
                .Where(roundEvent => roundEvent.Type == RoundEventType.FightStarted)
                .ToList();
            IReadOnlyList<RoundEvent> fightEnded = result.Events
                .Where(roundEvent => roundEvent.Type == RoundEventType.FightEnded)
                .ToList();
            IEnumerable<IGrouping<string, RoundEvent>> duplicateEscapes = result.Events
                .Where(roundEvent => roundEvent.Type == RoundEventType.ChampionEscaped)
                .GroupBy(roundEvent => $"{roundEvent.SourcePlayerId}:{roundEvent.SourceChampionId}:{roundEvent.FightId}")
                .Where(group => group.Count() > 1);

            Assert.Multiple(() =>
            {
                Assert.That(result.Events.Count(roundEvent => roundEvent.Type == RoundEventType.RoundStarted), Is.EqualTo(1));
                Assert.That(result.Events.Count(roundEvent => roundEvent.Type == RoundEventType.RoundEnded), Is.EqualTo(1));
                Assert.That(result.Events.First().Type, Is.EqualTo(RoundEventType.RoundStarted));
                Assert.That(result.Events.Last().Type, Is.EqualTo(RoundEventType.RoundEnded));
                Assert.That(result.ActiveFightCount, Is.EqualTo(0));
                Assert.That(fightEnded.Select(roundEvent => roundEvent.FightId), Is.SupersetOf(fightStarted.Select(roundEvent => roundEvent.FightId)));
                Assert.That(fightEnded, Has.Count.EqualTo(fightStarted.Count));
                Assert.That(duplicateEscapes, Is.Empty);
            });
        }

        [Test]
        public void Simulate_RoundResult_IncludesChampionSummariesAndMatchingTeamTotals()
        {
            RoundResult result = new RoundEngine(new RoundSettings
            {
                RoundDurationSeconds = 0.2,
                TickRateSeconds = 0.1
            }).Simulate([CreateDefinition()], [CreateDefinition()], seed: 0);

            ChampionRoundSummary summary = result.ChampionSummaries.First();
            Assert.Multiple(() =>
            {
                Assert.That(result.ChampionSummaries, Has.Count.EqualTo(2));
                Assert.That(summary.Level, Is.GreaterThanOrEqualTo(1));
                Assert.That(summary.Experience, Is.GreaterThanOrEqualTo(0));
                Assert.That(summary.Gold, Is.GreaterThanOrEqualTo(0));
                Assert.That(summary.Kills, Is.GreaterThanOrEqualTo(0));
                Assert.That(summary.Deaths, Is.GreaterThanOrEqualTo(0));
                Assert.That(summary.FinalHealth, Is.GreaterThanOrEqualTo(0));
                Assert.That(summary.MaximumHealth, Is.GreaterThan(0));
                Assert.That(result.BlueGold, Is.EqualTo(result.ChampionSummaries
                    .Where(champion => champion.TeamSide == TeamSide.Blue)
                    .Sum(champion => champion.Gold)));
                Assert.That(result.RedExperience, Is.EqualTo(result.ChampionSummaries
                    .Where(champion => champion.TeamSide == TeamSide.Red)
                    .Sum(champion => champion.Experience)));
            });
        }

        private static RoundState CreateState(
            IReadOnlyList<ChampionDefinition> blue,
            IReadOnlyList<ChampionDefinition> red,
            RoundSettings? settings = null,
            int seed = 0) =>
            new RoundEngine(settings).CreateState(blue, red, seed);

        private static void Tick(RoundState state, double deltaSeconds) =>
            new RoundEngine(state.Settings).Tick(state, deltaSeconds);

        private static ChampionDefinition CreateDefinition(
            FormationPosition defaultPosition = FormationPosition.Frontline,
            int attackPower = 100,
            IReadOnlyList<AttackEffect>? attackEffects = null,
            IReadOnlyList<AbilityEffect>? abilityEffects = null) =>
            TestChampionFactory.CreateDefinition(
                defaultPosition,
                attackPower: attackPower,
                attackEffects: attackEffects,
                abilityEffects: abilityEffects);

        private static AttackEffect CreateAttackEffect(
            CombatEffectType type,
            TargetMode targetMode,
            TargetScope targetScope) =>
            TestChampionFactory.CreateAttackEffect(type, targetMode, targetScope);

        private static AbilityEffect CreateAbilityEffect(
            CombatEffectType type,
            int abilityPower,
            TargetMode targetMode,
            TargetScope targetScope) =>
            TestChampionFactory.CreateAbilityEffect(type, abilityPower, targetMode, targetScope);

        private static void StartFight(RoundState state, ChampionInstance blue, ChampionInstance red)
        {
            FightState fight = new()
            {
                Lane = blue.Lane,
                Position = 0
            };

            AddParticipant(fight, blue);
            AddParticipant(fight, red);
            state.ActiveFights.Add(fight);
        }

        private static void AddParticipant(FightState fight, ChampionInstance champion)
        {
            fight.Participants.Add(champion);
            champion.FightId = fight.Id;
            champion.CurrentFightPosition = fight.Position;
        }
    }
}
