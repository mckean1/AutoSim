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
                attackPower: 123);
            RoundState state = CreateState([definition], [CreateDefinition()]);
            ChampionInstance champion = state.BlueTeam.Champions[0];

            Assert.Multiple(() =>
            {
                Assert.That(champion.Level, Is.EqualTo(1));
                Assert.That(champion.MaximumHealth, Is.EqualTo(900));
                Assert.That(champion.CurrentHealth, Is.EqualTo(champion.MaximumHealth));
                Assert.That(champion.CurrentAttackPower, Is.EqualTo(123));
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

            engine.AwardExperience(champion, 4500);
            CombatEffectApplicator.ApplyHeal(champion, 1000);

            Assert.Multiple(() =>
            {
                Assert.That(champion.Level, Is.EqualTo(10));
                Assert.That(champion.MaximumHealth, Is.EqualTo(190));
                Assert.That(champion.CurrentHealth, Is.EqualTo(190));
                Assert.That(champion.CurrentAttackPower, Is.EqualTo(18));
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
            AbilityEffect damage = CreateAbilityEffect(CombatEffectType.Damage, 100, TargetMode.EnemyAny, TargetScope.One);
            RoundState state = CreateState(
                [CreateDefinition(abilityEffects: [damage])],
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
            AbilityEffect effect = CreateAbilityEffect(CombatEffectType.Damage, 1, TargetMode.EnemyBackline, TargetScope.All);

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
            AttackEffect attackDamage = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            AbilityEffect abilityDamage = CreateAbilityEffect(CombatEffectType.Damage, 100, TargetMode.EnemyAny, TargetScope.One);
            RoundState state = CreateState(
                [CreateDefinition(attackEffects: [attackDamage], abilityEffects: [abilityDamage], abilityCastTime: 0.2)],
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
            AbilityEffect blueAbility = CreateAbilityEffect(CombatEffectType.Damage, 50, TargetMode.EnemyAny, TargetScope.One);
            AttackEffect redAttack = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            RoundState state = CreateState(
                [CreateDefinition(abilityEffects: [blueAbility], abilityCastTime: 1.0)],
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
            AttackEffect lethal = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            RoundState state = CreateState([CreateDefinition(attackEffects: [lethal], attackPower: 2000)], [CreateDefinition()]);
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
            red.CurrentAttackPower = 104;
            Tick(state, 10.0);

            Assert.Multiple(() =>
            {
                Assert.That(red.IsAlive, Is.True);
                Assert.That(red.CurrentHealth, Is.EqualTo(1020));
                Assert.That(red.Gold, Is.EqualTo(7));
                Assert.That(red.Experience, Is.EqualTo(123));
                Assert.That(red.Level, Is.EqualTo(3));
                Assert.That(red.CurrentAttackPower, Is.EqualTo(104));
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

            RoundResult result = engine.Simulate(CreateRoster([CreateDefinition()], [CreateDefinition()]), seed: 0);

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

        [Test]
        public void CreateState_ValidUniqueRoster_CreatesFiveChampionsPerTeam()
        {
            RoundState state = new RoundEngine().CreateState(CreateRoster(CreateDefinitions(5), CreateDefinitions(5)), seed: 0);

            Assert.Multiple(() =>
            {
                Assert.That(state.BlueTeam.Champions, Has.Count.EqualTo(5));
                Assert.That(state.RedTeam.Champions, Has.Count.EqualTo(5));
                Assert.That(state.AllChampions, Has.Count.EqualTo(10));
            });
        }

        [TestCase(4, 5, "Blue roster must contain exactly 5 champions.")]
        [TestCase(6, 5, "Blue roster must contain exactly 5 champions.")]
        [TestCase(5, 4, "Red roster must contain exactly 5 champions.")]
        [TestCase(5, 6, "Red roster must contain exactly 5 champions.")]
        public void CreateState_InvalidRosterSize_ThrowsClearException(
            int blueCount,
            int redCount,
            string expectedMessage)
        {
            RoundRoster roster = CreateRawRoster(CreateDefinitions(blueCount), CreateDefinitions(redCount));

            ArgumentException exception = Assert.Throws<ArgumentException>(() => new RoundEngine().CreateState(roster, seed: 0))!;

            Assert.That(exception.Message, Does.Contain(expectedMessage));
        }

        [Test]
        public void CreateState_DuplicateWithinBlue_ThrowsClearException()
        {
            ChampionDefinition duplicate = CreateDefinition();
            RoundRoster roster = CreateRoster(
                [duplicate, duplicate, .. CreateDefinitions(3)],
                CreateDefinitions(5));

            ArgumentException exception = Assert.Throws<ArgumentException>(() => new RoundEngine().CreateState(roster, seed: 0))!;

            Assert.That(exception.Message, Does.Contain($"Duplicate champion id in round roster: {duplicate.Id}."));
        }

        [Test]
        public void CreateState_DuplicateWithinRed_ThrowsClearException()
        {
            ChampionDefinition duplicate = CreateDefinition();
            RoundRoster roster = CreateRoster(
                CreateDefinitions(5),
                [duplicate, duplicate, .. CreateDefinitions(3)]);

            ArgumentException exception = Assert.Throws<ArgumentException>(() => new RoundEngine().CreateState(roster, seed: 0))!;

            Assert.That(exception.Message, Does.Contain($"Duplicate champion id in round roster: {duplicate.Id}."));
        }

        [Test]
        public void CreateState_DuplicateAcrossTeams_ThrowsClearException()
        {
            ChampionDefinition duplicate = CreateDefinition();
            RoundRoster roster = CreateRoster(
                [duplicate, .. CreateDefinitions(4)],
                [duplicate, .. CreateDefinitions(4)]);

            ArgumentException exception = Assert.Throws<ArgumentException>(() => new RoundEngine().CreateState(roster, seed: 0))!;

            Assert.That(exception.Message, Does.Contain("Champion ids must be unique across both teams."));
        }

        [Test]
        public void CreateState_ValidRoster_AssignsDeterministicLanes()
        {
            RoundState state = new RoundEngine().CreateState(CreateRoster(CreateDefinitions(5), CreateDefinitions(5)), seed: 0);
            Lane[] expectedLanes = [Lane.Top, Lane.Top, Lane.Mid, Lane.Bottom, Lane.Bottom];

            Assert.Multiple(() =>
            {
                Assert.That(state.BlueTeam.Champions.Select(champion => champion.Lane), Is.EqualTo(expectedLanes));
                Assert.That(state.RedTeam.Champions.Select(champion => champion.Lane), Is.EqualTo(expectedLanes));
            });
        }

        [Test]
        public void Simulate_RoundResult_IncludesOnlyActiveRoundChampions()
        {
            RoundResult result = new RoundEngine(new RoundSettings
            {
                RoundDurationSeconds = 0.2,
                TickRateSeconds = 0.1
            }).Simulate(CreateRoster(CreateDefinitions(5), CreateDefinitions(5)), seed: 0);

            Assert.Multiple(() =>
            {
                Assert.That(result.ChampionSummaries, Has.Count.EqualTo(10));
                Assert.That(result.ChampionSummaries.Count(champion => champion.TeamSide == TeamSide.Blue), Is.EqualTo(5));
                Assert.That(result.ChampionSummaries.Count(champion => champion.TeamSide == TeamSide.Red), Is.EqualTo(5));
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
            int seed = 0)
        {
            int activeBlueCount = blue.Count;
            int activeRedCount = red.Count;
            RoundState state = new RoundEngine(settings).CreateState(CreateRoster(blue, red), seed);
            DeactivateFillers(state.BlueTeam.Champions.Skip(activeBlueCount));
            DeactivateFillers(state.RedTeam.Champions.Skip(activeRedCount));
            return state;
        }

        private static void Tick(RoundState state, double deltaSeconds) =>
            new RoundEngine(state.Settings).Tick(state, deltaSeconds);

        private static ChampionDefinition CreateDefinition(
            FormationPosition defaultPosition = FormationPosition.Frontline,
            int attackPower = 100,
            IReadOnlyList<AttackEffect>? attackEffects = null,
            IReadOnlyList<AbilityEffect>? abilityEffects = null,
            double abilityCastTime = 0.5) =>
            TestChampionFactory.CreateDefinition(
                defaultPosition,
                attackPower: attackPower,
                attackEffects: attackEffects,
                abilityEffects: abilityEffects,
                abilityCastTime: abilityCastTime);

        private static RoundRoster CreateRoster(
            IReadOnlyList<ChampionDefinition> blue,
            IReadOnlyList<ChampionDefinition> red) =>
            new()
            {
                BlueChampions = PadRoster(blue),
                RedChampions = PadRoster(red)
            };

        private static RoundRoster CreateRawRoster(
            IReadOnlyList<ChampionDefinition> blue,
            IReadOnlyList<ChampionDefinition> red) =>
            new()
            {
                BlueChampions = blue,
                RedChampions = red
            };

        private static IReadOnlyList<ChampionDefinition> CreateDefinitions(int count) =>
            Enumerable.Range(0, count)
                .Select(_ => CreateDefinition())
                .ToList();

        private static IReadOnlyList<ChampionDefinition> PadRoster(IReadOnlyList<ChampionDefinition> champions)
        {
            List<ChampionDefinition> roster = champions.ToList();
            while (roster.Count < 5)
            {
                roster.Add(CreateDefinition());
            }

            return roster;
        }

        private static void DeactivateFillers(IEnumerable<ChampionInstance> champions)
        {
            foreach (ChampionInstance champion in champions)
            {
                champion.CurrentHealth = 0;
                champion.RespawnTimer = 999;
                champion.IsDeathProcessed = true;
            }
        }

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
