using AutoSim.Domain.Enums;
using AutoSim.Domain.Interfaces;
using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;

namespace AutoSim.Domain.Tests.Services
{
    internal sealed class CombatTargetingTests
    {
        [Test]
        public void SelectTargets_EnemyFrontline_SelectsLivingEnemyFrontlineActiveFightParticipants()
        {
            TargetScenario scenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.EnemyFrontline, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect);

            Assert.That(targets, Is.EquivalentTo(new[] { scenario.EnemyFrontline }));
        }

        [Test]
        public void SelectTargets_EnemyFrontline_NoFrontline_FallsBackToLivingEnemyBacklineActiveFightParticipants()
        {
            TargetScenario scenario = CreateScenario(activeChampions: []);
            CombatEffect effect = CreateEffect(TargetMode.EnemyFrontline, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(
                scenario,
                effect,
                [scenario.Source, scenario.EnemyBackline]);

            Assert.That(targets, Is.EquivalentTo(new[] { scenario.EnemyBackline }));
        }

        [Test]
        public void SelectTargets_EnemyBackline_SelectsLivingEnemyBacklineActiveFightParticipants()
        {
            TargetScenario scenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.EnemyBackline, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect);

            Assert.That(targets, Is.EquivalentTo(new[] { scenario.EnemyBackline }));
        }

        [Test]
        public void SelectTargets_EnemyBackline_NoBackline_FallsBackToLivingEnemyFrontlineActiveFightParticipants()
        {
            TargetScenario scenario = CreateScenario(activeChampions: []);
            CombatEffect effect = CreateEffect(TargetMode.EnemyBackline, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(
                scenario,
                effect,
                [scenario.Source, scenario.EnemyFrontline]);

            Assert.That(targets, Is.EquivalentTo(new[] { scenario.EnemyFrontline }));
        }

        [Test]
        public void SelectTargets_EnemyAny_OnlyIncludesLivingEnemyActiveFightParticipants()
        {
            TargetScenario scenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.EnemyAny, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect);

            Assert.That(targets, Is.EquivalentTo(new[] { scenario.EnemyFrontline, scenario.EnemyBackline }));
        }

        [Test]
        public void SelectTargets_AllyFrontline_SelectsLivingAlliedFrontlineActiveFightParticipants()
        {
            TargetScenario scenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.AllyFrontline, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect);

            Assert.That(targets, Is.EquivalentTo(new[] { scenario.Source }));
        }

        [Test]
        public void SelectTargets_AllyFrontline_NoFrontline_FallsBackToLivingAlliedBacklineActiveFightParticipants()
        {
            TargetScenario scenario = CreateScenario(activeChampions: []);
            CombatEffect effect = CreateEffect(TargetMode.AllyFrontline, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(
                scenario,
                effect,
                [scenario.AllyBackline, scenario.EnemyFrontline]);

            Assert.That(targets, Is.EquivalentTo(new[] { scenario.AllyBackline }));
        }

        [Test]
        public void SelectTargets_AllyBackline_SelectsLivingAlliedBacklineActiveFightParticipants()
        {
            TargetScenario scenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.AllyBackline, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect);

            Assert.That(targets, Is.EquivalentTo(new[] { scenario.AllyBackline }));
        }

        [Test]
        public void SelectTargets_AllyBackline_NoBackline_FallsBackToLivingAlliedFrontlineActiveFightParticipants()
        {
            TargetScenario scenario = CreateScenario(activeChampions: []);
            CombatEffect effect = CreateEffect(TargetMode.AllyBackline, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(
                scenario,
                effect,
                [scenario.Source, scenario.EnemyFrontline]);

            Assert.That(targets, Is.EquivalentTo(new[] { scenario.Source }));
        }

        [Test]
        public void SelectTargets_AllyAny_OnlyIncludesLivingAlliedActiveFightParticipants()
        {
            TargetScenario scenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.AllyAny, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect);

            Assert.That(targets, Is.EquivalentTo(new[] { scenario.Source, scenario.AllyBackline }));
        }

        [Test]
        public void SelectTargets_GlobalEnemy_IncludesLivingEnemiesOutsideActiveFight()
        {
            TargetScenario scenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.GlobalEnemy, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect);

            Assert.That(
                targets,
                Is.EquivalentTo(new[] { scenario.EnemyFrontline, scenario.EnemyBackline, scenario.InactiveEnemy }));
        }

        [Test]
        public void SelectTargets_GlobalAlly_IncludesLivingAlliesOutsideActiveFight()
        {
            TargetScenario scenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.GlobalAlly, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect);

            Assert.That(
                targets,
                Is.EquivalentTo(new[] { scenario.Source, scenario.AllyBackline, scenario.InactiveAlly }));
        }

        [Test]
        public void SelectTargets_GlobalAll_IncludesLivingChampionsFromBothFullRosters()
        {
            TargetScenario scenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.GlobalAll, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect);

            Assert.That(
                targets,
                Is.EquivalentTo(
                    new[]
                    {
                        scenario.Source,
                        scenario.AllyBackline,
                        scenario.InactiveAlly,
                        scenario.EnemyFrontline,
                        scenario.EnemyBackline,
                        scenario.InactiveEnemy
                    }));
        }

        [Test]
        public void SelectTargets_AllModes_ExcludeDeadChampions()
        {
            TargetScenario scenario = CreateScenario();
            scenario.EnemyFrontline.CurrentHealth = 0;
            scenario.EnemyBackline.CurrentHealth = 0;
            CombatEffect effect = CreateEffect(TargetMode.GlobalEnemy, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect);

            Assert.That(targets, Is.EquivalentTo(new[] { scenario.InactiveEnemy }));
        }

        [Test]
        public void SelectTargets_Self_SourceAlive_ReturnsOnlySourceChampion()
        {
            TargetScenario scenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.Self, TargetScope.One);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect);

            Assert.That(targets, Is.EquivalentTo(new[] { scenario.Source }));
        }

        [Test]
        public void SelectTargets_Self_SourceDead_ReturnsNoTargets()
        {
            TargetScenario scenario = CreateScenario();
            scenario.Source.CurrentHealth = 0;
            CombatEffect effect = CreateEffect(TargetMode.Self, TargetScope.One);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect);

            Assert.That(targets, Is.Empty);
        }

        [Test]
        public void SelectTargets_TargetScopeOne_CandidatesExist_ReturnsExactlyOneTarget()
        {
            TargetScenario scenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.EnemyAny, TargetScope.One);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect, rng: new QueueMatchRandom(0));

            Assert.That(targets, Has.Count.EqualTo(1));
        }

        [Test]
        public void SelectTargets_TargetScopeOne_MultipleCandidates_UsesSeededRandom()
        {
            TargetScenario scenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.EnemyAny, TargetScope.One);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect, rng: new QueueMatchRandom(1));

            Assert.That(targets.Single(), Is.SameAs(scenario.EnemyBackline));
        }

        [Test]
        public void SelectTargets_TargetScopeAll_ReturnsAllValidCandidates()
        {
            TargetScenario scenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.EnemyAny, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(scenario, effect);

            Assert.That(targets, Has.Count.EqualTo(2));
        }

        [Test]
        public void SelectTargets_NoValidCandidates_ReturnsEmptyList()
        {
            TargetScenario scenario = CreateScenario(activeChampions: []);
            CombatEffect effect = CreateEffect(TargetMode.EnemyAny, TargetScope.One);

            IReadOnlyList<ChampionInstance> targets = SelectTargets(
                scenario,
                effect,
                [scenario.Source, scenario.AllyBackline]);

            Assert.That(targets, Is.Empty);
        }

        [Test]
        public void SelectTargets_SameSeedAndInputs_ReturnsSameTargetChoice()
        {
            TargetScenario firstScenario = CreateScenario();
            TargetScenario secondScenario = CreateScenario();
            CombatEffect effect = CreateEffect(TargetMode.EnemyAny, TargetScope.One);

            ChampionInstance firstTarget = SelectTargets(
                firstScenario,
                effect,
                rng: new SeededMatchRandom(77)).Single();
            ChampionInstance secondTarget = SelectTargets(
                secondScenario,
                effect,
                rng: new SeededMatchRandom(77)).Single();

            Assert.That(firstTarget.Position, Is.EqualTo(secondTarget.Position));
        }

        private static IReadOnlyList<ChampionInstance> SelectTargets(
            TargetScenario scenario,
            CombatEffect effect,
            IReadOnlyList<ChampionInstance>? activeChampions = null,
            IMatchRandom? rng = null) =>
            CombatTargeting.SelectTargets(
                scenario.Source,
                effect,
                scenario.AllChampions,
                activeChampions ?? scenario.ActiveChampions,
                rng ?? new QueueMatchRandom(0));

        private static CombatEffect CreateEffect(TargetMode targetMode, TargetScope targetScope) =>
            TestChampionFactory.CreateEffect(CombatEffectType.Damage, 100, targetMode, targetScope);

        private static TargetScenario CreateScenario(IReadOnlyList<ChampionInstance>? activeChampions = null)
        {
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", FormationPosition.Frontline);
            ChampionInstance allyBackline = TestChampionFactory.CreateInstance(
                "player-one",
                FormationPosition.Backline);
            ChampionInstance inactiveAlly = TestChampionFactory.CreateInstance(
                "player-one",
                FormationPosition.Backline);
            ChampionInstance enemyFrontline = TestChampionFactory.CreateInstance(
                "player-two",
                FormationPosition.Frontline);
            ChampionInstance enemyBackline = TestChampionFactory.CreateInstance(
                "player-two",
                FormationPosition.Backline);
            ChampionInstance inactiveEnemy = TestChampionFactory.CreateInstance(
                "player-two",
                FormationPosition.Backline);

            return new TargetScenario(
                source,
                allyBackline,
                inactiveAlly,
                enemyFrontline,
                enemyBackline,
                inactiveEnemy,
                [source, allyBackline, inactiveAlly, enemyFrontline, enemyBackline, inactiveEnemy],
                activeChampions ?? [source, allyBackline, enemyFrontline, enemyBackline]);
        }

        private sealed record TargetScenario(
            ChampionInstance Source,
            ChampionInstance AllyBackline,
            ChampionInstance InactiveAlly,
            ChampionInstance EnemyFrontline,
            ChampionInstance EnemyBackline,
            ChampionInstance InactiveEnemy,
            IReadOnlyList<ChampionInstance> AllChampions,
            IReadOnlyList<ChampionInstance> ActiveChampions);
    }
}
