using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;

namespace AutoSim.Domain.Tests.Services
{
    internal sealed class RoundEngineTests
    {
        [Test]
        public void CreateState_ChampionRuntimeInitialization_UsesRoundDefaults()
        {
            ChampionDefinition definition = TestChampionFactory.CreateDefinition(
                FormationPosition.Backline,
                health: 900,
                power: 123);
            RoundState state = CreateState([definition], [CreateDefinition()]);
            ChampionInstance champion = state.BlueTeam.Champions[0];

            Assert.Multiple(() =>
            {
                Assert.That(champion.Level, Is.EqualTo(1));
                Assert.That(champion.MaximumHealth, Is.EqualTo(900));
                Assert.That(champion.CurrentHealth, Is.EqualTo(champion.MaximumHealth));
                Assert.That(champion.CurrentPower, Is.EqualTo(123));
                Assert.That(champion.Position, Is.EqualTo(FormationPosition.Backline));
                Assert.That(champion.Intent, Is.EqualTo(ChampionIntent.Laning));
                Assert.That(champion.TeamSide, Is.EqualTo(TeamSide.Blue));
                Assert.That(champion.Lane, Is.EqualTo(Lane.Top));
            });
        }

        [Test]
        public void AwardExperience_EnoughExperience_LevelsChampionAndUsesRuntimeHealth()
        {
            RoundEngine engine = new();
            ChampionInstance champion = TestChampionFactory.CreateInstance(health: 100);
            champion.CurrentHealth = 50;

            engine.AwardExperience(champion, 2000);
            CombatEffectApplicator.ApplyHeal(champion, 1000);

            Assert.Multiple(() =>
            {
                Assert.That(champion.Level, Is.EqualTo(10));
                Assert.That(champion.MaximumHealth, Is.EqualTo(190));
                Assert.That(champion.CurrentHealth, Is.EqualTo(190));
                Assert.That(champion.CurrentPower, Is.EqualTo(118));
            });
        }

        [Test]
        public void Tick_LaneMovement_MovesBySideIntentAndClamps()
        {
            RoundState state = CreateState([CreateDefinition()], [CreateDefinition()]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            red.LanePosition = -90;

            Tick(state, 1.0);
            Assert.Multiple(() =>
            {
                Assert.That(blue.LanePosition, Is.GreaterThan(-20));
                Assert.That(red.LanePosition, Is.GreaterThanOrEqualTo(-100));
            });

            blue.Intent = ChampionIntent.Retreating;
            red.Intent = ChampionIntent.Retreating;
            blue.CurrentHealth = 100;
            red.CurrentHealth = 100;
            double blueBefore = blue.LanePosition;
            double redBefore = red.LanePosition;

            Tick(state, 1.0);

            Assert.Multiple(() =>
            {
                Assert.That(blue.LanePosition, Is.LessThan(blueBefore));
                Assert.That(red.LanePosition, Is.GreaterThan(redBefore));
            });

            blue.CurrentHealth = 0;
            blue.RespawnTimer = 5;
            blue.LanePosition = 10;

            Tick(state, 1.0);

            Assert.That(blue.LanePosition, Is.EqualTo(10));
        }

        [Test]
        public void Tick_Farming_OnlyLivingLaningNonFightingChampionsFarm()
        {
            RoundState state = CreateState([CreateDefinition(), CreateDefinition()], [CreateDefinition()]);
            ChampionInstance farmer = state.BlueTeam.Champions[0];
            ChampionInstance fighter = state.BlueTeam.Champions[1];
            ChampionInstance retreating = state.RedTeam.Champions[0];
            fighter.FightId = Guid.NewGuid();
            retreating.Intent = ChampionIntent.Retreating;
            retreating.CurrentHealth = 100;

            Tick(state, 1.0);

            Assert.Multiple(() =>
            {
                Assert.That(farmer.Gold, Is.EqualTo(1));
                Assert.That(farmer.Experience, Is.EqualTo(5));
                Assert.That(fighter.Gold, Is.EqualTo(0));
                Assert.That(retreating.Gold, Is.EqualTo(0));
            });

            farmer.CurrentHealth = 0;
            farmer.RespawnTimer = 5;
            Tick(state, 1.0);

            Assert.That(farmer.Gold, Is.EqualTo(1));
        }

        [Test]
        public void Tick_Regeneration_AppliesPassiveAndBaseHealingWithoutRestoringShields()
        {
            RoundState state = CreateState([CreateDefinition()], [CreateDefinition()]);
            ChampionInstance champion = state.BlueTeam.Champions[0];
            champion.LanePosition = -100;
            champion.CurrentHealth = 50;
            champion.Shields.Add(new ActiveShield { Amount = 12, Duration = 10 });

            Tick(state, 1.0);

            Assert.Multiple(() =>
            {
                Assert.That(champion.CurrentHealth, Is.EqualTo(151));
                Assert.That(champion.Shields.Single().Amount, Is.EqualTo(12));
            });

            champion.CurrentHealth = 0;
            champion.RespawnTimer = 5;
            Tick(state, 1.0);

            Assert.That(champion.CurrentHealth, Is.EqualTo(0));
        }

        [Test]
        public void Tick_Retreating_CancelsCastPreventsActionsAndReturnsToLaningAtFullHealth()
        {
            CombatEffect damage = CreateEffect(CombatEffectType.Damage, 100, TargetMode.EnemyAny, TargetScope.One);
            RoundState state = CreateState(
                [CreateDefinition(abilityCombatEffects: [damage])],
                [CreateDefinition()]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            StartFight(state, blue, red);
            blue.CurrentHealth = 300;
            blue.IsCasting = true;
            blue.CastTimer = 1;
            blue.PendingAbility = blue.Definition.Ability;
            blue.AttackTimer = 0;
            blue.AbilityCooldown = 0;

            Tick(state, 0.1);

            Assert.Multiple(() =>
            {
                Assert.That(blue.Intent, Is.EqualTo(ChampionIntent.Retreating));
                Assert.That(blue.IsCasting, Is.False);
                Assert.That(red.CurrentHealth, Is.EqualTo(red.MaximumHealth));
            });

            blue.CurrentHealth = blue.MaximumHealth;
            Tick(state, 0.1);

            Assert.That(blue.Intent, Is.EqualTo(ChampionIntent.Laning));
        }

        [Test]
        public void Tick_FightCreation_UsesLaneProximityAndInitialParticipants()
        {
            RoundState state = CreateState(
                [CreateDefinition(), CreateDefinition()],
                [CreateDefinition()]);
            ChampionInstance blueFront = state.BlueTeam.Champions[0];
            ChampionInstance blueNearby = state.BlueTeam.Champions[1];
            ChampionInstance red = state.RedTeam.Champions[0];
            blueFront.LanePosition = 0;
            blueNearby.LanePosition = 1;
            red.LanePosition = 8;

            Tick(state, 0.1);

            FightState fight = state.ActiveFights.Single();
            Assert.Multiple(() =>
            {
                Assert.That(fight.Position, Is.EqualTo(4.0).Within(0.5));
                Assert.That(fight.Participants, Has.Count.EqualTo(3));
                Assert.That(blueNearby.FightId, Is.EqualTo(fight.Id));
            });
        }

        [Test]
        public void Tick_FightCreation_DoesNotStartForDifferentLaneDistanceOrExistingFight()
        {
            RoundState state = CreateState([CreateDefinition()], [CreateDefinition()]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            blue.Lane = Lane.Top;
            red.Lane = Lane.Mid;
            blue.LanePosition = 0;
            red.LanePosition = 1;

            Tick(state, 0.1);
            Assert.That(state.ActiveFights, Is.Empty);

            red.Lane = Lane.Top;
            red.LanePosition = 50;
            Tick(state, 0.1);
            Assert.That(state.ActiveFights, Is.Empty);

            red.LanePosition = blue.LanePosition + 1;
            Tick(state, 0.1);
            Tick(state, 0.1);
            Assert.That(state.ActiveFights, Has.Count.EqualTo(1));
        }

        [Test]
        public void Tick_FightJoining_OnlyEligibleLaningChampionsJoin()
        {
            RoundState state = CreateState(
                [CreateDefinition(), CreateDefinition(), CreateDefinition(), CreateDefinition()],
                [CreateDefinition()]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance joiner = state.BlueTeam.Champions[1];
            ChampionInstance retreating = state.BlueTeam.Champions[2];
            ChampionInstance occupied = state.BlueTeam.Champions[3];
            ChampionInstance red = state.RedTeam.Champions[0];
            StartFight(state, blue, red, position: 0);
            joiner.Lane = Lane.Top;
            joiner.LanePosition = 2;
            retreating.Lane = Lane.Top;
            retreating.LanePosition = 2;
            retreating.Intent = ChampionIntent.Retreating;
            retreating.CurrentHealth = 100;
            occupied.Lane = Lane.Top;
            occupied.LanePosition = 2;
            occupied.FightId = Guid.NewGuid();

            Tick(state, 0.1);

            Assert.Multiple(() =>
            {
                Assert.That(joiner.FightId, Is.EqualTo(state.ActiveFights.Single().Id));
                Assert.That(retreating.FightId, Is.Null);
                Assert.That(occupied.FightId, Is.Not.EqualTo(state.ActiveFights.Single().Id));
            });

            joiner.CurrentHealth = 0;
            joiner.FightId = null;
            Tick(state, 0.1);
            Assert.That(joiner.FightId, Is.Null);
        }

        [Test]
        public void Tick_FightPositioning_MovesTowardFormationAnchors()
        {
            RoundState state = CreateState(
                [CreateDefinition(FormationPosition.Frontline), CreateDefinition(FormationPosition.Backline)],
                [CreateDefinition(FormationPosition.Backline)]);
            ChampionInstance blueFront = state.BlueTeam.Champions[0];
            ChampionInstance blueBack = state.BlueTeam.Champions[1];
            ChampionInstance redBack = state.RedTeam.Champions[0];
            StartFight(state, blueFront, redBack, position: 0);
            AddParticipant(state.ActiveFights.Single(), blueBack);
            blueFront.LanePosition = -10;
            blueBack.LanePosition = -10;
            redBack.LanePosition = 10;

            Tick(state, 0.5);

            Assert.Multiple(() =>
            {
                Assert.That(blueFront.LanePosition, Is.EqualTo(-7.5).Within(0.01));
                Assert.That(blueBack.LanePosition, Is.EqualTo(-7.5).Within(0.01));
                Assert.That(redBack.LanePosition, Is.EqualTo(7.5).Within(0.01));
            });
        }

        [Test]
        public void SelectTargets_FightLocalPosition_UsesLanePositionClassification()
        {
            ChampionInstance source = TestChampionFactory.CreateInstance("blue", FormationPosition.Frontline);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("red", FormationPosition.Frontline);
            source.TeamSide = TeamSide.Blue;
            enemy.TeamSide = TeamSide.Red;
            source.CurrentFightPosition = 0;
            enemy.CurrentFightPosition = 0;
            enemy.LanePosition = 5;
            CombatEffect effect = CreateEffect(CombatEffectType.Damage, 1, TargetMode.EnemyBackline, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = CombatTargeting.SelectTargets(
                source,
                effect,
                [source, enemy],
                [source, enemy],
                new QueueMatchRandom(0));

            Assert.That(targets, Is.EqualTo(new[] { enemy }));
        }

        [Test]
        public void Tick_FightEnd_AwardsWinnerOnlyWhenOneSideRemains()
        {
            RoundState state = CreateState([CreateDefinition()], [CreateDefinition()]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            StartFight(state, blue, red);

            Tick(state, 1.0);
            Assert.That(state.ActiveFights, Has.Count.EqualTo(1));

            red.CurrentHealth = 0;
            Tick(state, 0.1);

            Assert.Multiple(() =>
            {
                Assert.That(state.ActiveFights, Is.Empty);
                Assert.That(blue.Experience, Is.EqualTo(25));
            });
        }

        [Test]
        public void Tick_FightEnd_RetreatingParticipantLeavesOnlyOutsideEngageRange()
        {
            RoundState state = CreateState([CreateDefinition()], [CreateDefinition()]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            StartFight(state, blue, red, position: 0);
            red.Intent = ChampionIntent.Retreating;
            red.CurrentHealth = 100;
            red.LanePosition = 5;

            Tick(state, 0.1);
            Assert.That(red.FightId, Is.Not.Null);

            red.LanePosition = 11;
            Tick(state, 0.1);

            Assert.Multiple(() =>
            {
                Assert.That(red.FightId, Is.Null);
                Assert.That(state.ActiveFights, Is.Empty);
            });
        }

        [Test]
        public void Tick_FightEnd_NoXpWhenBothSidesHaveNoParticipants()
        {
            RoundState state = CreateState([CreateDefinition()], [CreateDefinition()]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            StartFight(state, blue, red);
            blue.CurrentHealth = 0;
            red.CurrentHealth = 0;

            Tick(state, 0.1);

            Assert.Multiple(() =>
            {
                Assert.That(state.ActiveFights, Is.Empty);
                Assert.That(blue.Experience, Is.EqualTo(0));
                Assert.That(red.Experience, Is.EqualTo(0));
            });
        }

        [Test]
        public void Tick_Combat_AttacksAndAbilitiesUseIndependentTimersAndCastingRules()
        {
            CombatEffect attackDamage = CreateEffect(CombatEffectType.Damage, 10, TargetMode.EnemyAny, TargetScope.One);
            CombatEffect abilityDamage = CreateEffect(CombatEffectType.Damage, 100, TargetMode.EnemyAny, TargetScope.One);
            RoundState state = CreateState(
                [CreateDefinition(attackEffects: [attackDamage], abilityCombatEffects: [abilityDamage], abilityCastTime: 0.2)],
                [CreateDefinition()]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            StartFight(state, blue, red);
            blue.AttackTimer = 0;
            blue.AbilityCooldown = 0;

            Tick(state, 0.1);
            Assert.Multiple(() =>
            {
                Assert.That(blue.IsCasting, Is.True);
                Assert.That(red.CurrentHealth, Is.EqualTo(red.MaximumHealth));
                Assert.That(blue.AttackTimer, Is.EqualTo(0));
            });

            blue.AttackTimer = 10;
            Tick(state, 0.2);

            Assert.Multiple(() =>
            {
                Assert.That(blue.IsCasting, Is.False);
                Assert.That(red.CurrentHealth, Is.EqualTo(red.MaximumHealth - 100));
                Assert.That(blue.AbilityCooldown, Is.EqualTo(5.0));
            });
        }

        [Test]
        public void Tick_Combat_ChampionCanMoveAndTakeDamageWhileCasting()
        {
            CombatEffect blueAbility = CreateEffect(CombatEffectType.Damage, 50, TargetMode.EnemyAny, TargetScope.One);
            CombatEffect redAttack = CreateEffect(CombatEffectType.Damage, 20, TargetMode.EnemyAny, TargetScope.One);
            RoundState state = CreateState(
                [CreateDefinition(abilityCombatEffects: [blueAbility], abilityCastTime: 1.0)],
                [CreateDefinition(attackEffects: [redAttack])]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            StartFight(state, blue, red, position: 0);
            blue.LanePosition = -10;
            blue.AbilityCooldown = 0;
            red.AttackTimer = 0;

            Tick(state, 0.1);
            double afterStart = blue.LanePosition;
            Tick(state, 0.1);

            Assert.Multiple(() =>
            {
                Assert.That(blue.IsCasting, Is.True);
                Assert.That(blue.LanePosition, Is.GreaterThan(afterStart));
                Assert.That(blue.CurrentHealth, Is.LessThan(blue.MaximumHealth));
            });
        }

        [Test]
        public void Tick_DeathAndRespawn_AppliesDeathRulesRewardsAndRespawnRules()
        {
            CombatEffect lethal = CreateEffect(CombatEffectType.Damage, 2000, TargetMode.EnemyAny, TargetScope.One);
            RoundState state = CreateState([CreateDefinition(attackEffects: [lethal])], [CreateDefinition()]);
            ChampionInstance blue = state.BlueTeam.Champions[0];
            ChampionInstance red = state.RedTeam.Champions[0];
            StartFight(state, blue, red);
            blue.AttackTimer = 0;
            red.Shields.Add(new ActiveShield { Amount = 1, Duration = 5 });
            red.IsCasting = true;
            red.PendingAbility = red.Definition.Ability;
            red.CastTimer = 5;

            Tick(state, 0.1);

            Assert.Multiple(() =>
            {
                Assert.That(red.IsAlive, Is.False);
                Assert.That(red.Shields, Is.Empty);
                Assert.That(red.IsCasting, Is.False);
                Assert.That(red.FightId, Is.Null);
                Assert.That(red.RespawnTimer, Is.EqualTo(10.0));
                Assert.That(state.BlueTeam.KillScore, Is.EqualTo(1));
                Assert.That(blue.Experience, Is.EqualTo(75));
            });

            red.Gold = 7;
            red.Experience = 123;
            red.Level = 3;
            red.MaximumHealth = 1020;
            red.CurrentPower = 104;
            Tick(state, 10.0);

            Assert.Multiple(() =>
            {
                Assert.That(red.IsAlive, Is.True);
                Assert.That(red.CurrentHealth, Is.EqualTo(1020));
                Assert.That(red.Gold, Is.EqualTo(7));
                Assert.That(red.Experience, Is.EqualTo(123));
                Assert.That(red.Level, Is.EqualTo(3));
                Assert.That(red.CurrentPower, Is.EqualTo(104));
                Assert.That(red.LanePosition, Is.EqualTo(100));
                Assert.That(red.AbilityCooldown, Is.EqualTo(red.Definition.Ability.Cooldown));
                Assert.That(red.AttackTimer, Is.EqualTo(1.0 / red.Definition.AttackSpeed));
            });
        }

        [Test]
        public void Simulate_RoundResult_EndsAtDurationAndUsesTieBreakers()
        {
            RoundSettings settings = new()
            {
                RoundDurationSeconds = 1,
                TickRateSeconds = 0.5
            };
            RoundEngine engine = new(settings);

            RoundResult result = engine.Simulate([CreateDefinition()], [CreateDefinition()], seed: 0);

            Assert.Multiple(() =>
            {
                Assert.That(result.Duration, Is.EqualTo(1));
                Assert.That(result.WinningSide, Is.EqualTo(TeamSide.Red));
                Assert.That(result.BlueKills, Is.EqualTo(0));
                Assert.That(result.RedKills, Is.EqualTo(0));
            });
        }

        [Test]
        public void Simulate_RoundResult_MoreKillsWinsBeforeGoldAndXp()
        {
            RoundSettings settings = new()
            {
                RoundDurationSeconds = 0.1,
                TickRateSeconds = 0.1
            };
            RoundState state = CreateState([CreateDefinition()], [CreateDefinition()], settings);
            state.BlueTeam.KillScore = 1;
            state.RedTeam.Champions[0].Gold = 100;
            state.RedTeam.Champions[0].Experience = 100;

            RoundResult result = InvokeResult(state);

            Assert.That(result.WinningSide, Is.EqualTo(TeamSide.Blue));
        }

        [Test]
        public void Simulate_RoundResult_TiedKillsUseGoldThenXpThenSeededRandom()
        {
            RoundState goldState = CreateState([CreateDefinition()], [CreateDefinition()]);
            goldState.RedTeam.Champions[0].Gold = 1;
            Assert.That(InvokeResult(goldState).WinningSide, Is.EqualTo(TeamSide.Red));

            RoundState xpState = CreateState([CreateDefinition()], [CreateDefinition()]);
            xpState.BlueTeam.Champions[0].Experience = 1;
            Assert.That(InvokeResult(xpState).WinningSide, Is.EqualTo(TeamSide.Blue));

            RoundState randomState = CreateState([CreateDefinition()], [CreateDefinition()], seed: 0);
            Assert.That(InvokeResult(randomState).WinningSide, Is.EqualTo(TeamSide.Red));
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
            IReadOnlyList<CombatEffect>? attackEffects = null,
            IReadOnlyList<CombatEffect>? abilityCombatEffects = null,
            double abilityCastTime = 0.5) =>
            TestChampionFactory.CreateDefinition(
                defaultPosition,
                attackEffects: attackEffects,
                abilityCombatEffects: abilityCombatEffects,
                abilityCastTime: abilityCastTime);

        private static CombatEffect CreateEffect(
            CombatEffectType type,
            int value,
            TargetMode targetMode,
            TargetScope targetScope) =>
            TestChampionFactory.CreateEffect(type, value, targetMode, targetScope);

        private static void StartFight(
            RoundState state,
            ChampionInstance blue,
            ChampionInstance red,
            double position = 0)
        {
            FightState fight = new()
            {
                Lane = blue.Lane,
                Position = position
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

        private static RoundResult InvokeResult(RoundState state)
        {
            RoundEngine engine = new(state.Settings);
            return (RoundResult)typeof(RoundEngine)
                .GetMethod("CreateResult", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(engine, [state])!;
        }
    }
}
