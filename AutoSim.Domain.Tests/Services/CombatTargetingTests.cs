using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;

namespace AutoSim.Domain.Tests.Services
{
    internal sealed class CombatTargetingTests
    {
        [Test]
        public void SelectTargets_EnemyFrontlineWithoutFrontline_FallsBackToBackline()
        {
            ChampionInstance source = CreateInstance("player-one", FormationPosition.Frontline);
            ChampionInstance enemy = CreateInstance("player-two", FormationPosition.Backline);
            CombatEffect effect = CreateEffect(TargetMode.EnemyFrontline, TargetScope.One);

            IReadOnlyList<ChampionInstance> targets = CombatTargeting.SelectTargets(
                source,
                effect,
                [source, enemy],
                [source, enemy]);

            Assert.That(targets.Single(), Is.SameAs(enemy));
        }

        [Test]
        public void SelectTargets_EnemyAny_UsesOnlyActiveFightChampions()
        {
            ChampionInstance source = CreateInstance("player-one", FormationPosition.Frontline);
            ChampionInstance activeEnemy = CreateInstance("player-two", FormationPosition.Frontline);
            ChampionInstance inactiveEnemy = CreateInstance("player-two", FormationPosition.Backline);
            CombatEffect effect = CreateEffect(TargetMode.EnemyAny, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = CombatTargeting.SelectTargets(
                source,
                effect,
                [source, activeEnemy, inactiveEnemy],
                [source, activeEnemy]);

            Assert.That(targets, Is.EquivalentTo(new[] { activeEnemy }));
        }

        [Test]
        public void SelectTargets_GlobalEnemy_UsesInactiveLivingChampions()
        {
            ChampionInstance source = CreateInstance("player-one", FormationPosition.Frontline);
            ChampionInstance activeEnemy = CreateInstance("player-two", FormationPosition.Frontline);
            ChampionInstance inactiveEnemy = CreateInstance("player-two", FormationPosition.Backline);
            CombatEffect effect = CreateEffect(TargetMode.GlobalEnemy, TargetScope.All);

            IReadOnlyList<ChampionInstance> targets = CombatTargeting.SelectTargets(
                source,
                effect,
                [source, activeEnemy, inactiveEnemy],
                [source, activeEnemy]);

            Assert.That(targets, Is.EquivalentTo(new[] { activeEnemy, inactiveEnemy }));
        }

        [Test]
        public void SelectTargets_TargetScopeOne_UsesProvidedIndexSelector()
        {
            ChampionInstance source = CreateInstance("player-one", FormationPosition.Frontline);
            ChampionInstance firstEnemy = CreateInstance("player-two", FormationPosition.Frontline);
            ChampionInstance secondEnemy = CreateInstance("player-two", FormationPosition.Backline);
            CombatEffect effect = CreateEffect(TargetMode.EnemyAny, TargetScope.One);

            IReadOnlyList<ChampionInstance> targets = CombatTargeting.SelectTargets(
                source,
                effect,
                [source, firstEnemy, secondEnemy],
                [source, firstEnemy, secondEnemy],
                count => count - 1);

            Assert.That(targets.Single(), Is.SameAs(secondEnemy));
        }

        private static CombatEffect CreateEffect(TargetMode targetMode, TargetScope targetScope) =>
            new CombatEffect
            {
                Type = CombatEffectType.Damage,
                Value = 100,
                TargetMode = targetMode,
                TargetScope = targetScope
            };

        private static ChampionInstance CreateInstance(string playerId, FormationPosition defaultPosition) =>
            ChampionInstanceFactory.Create(
                new ChampionDefinition
                {
                    Id = $"{playerId}-{defaultPosition}",
                    Name = "Test Champion",
                    Role = ChampionRole.Fighter,
                    DefaultPosition = defaultPosition,
                    Health = 1000,
                    Power = 100,
                    AttackSpeed = 1.0,
                    Attack = new ChampionAttack
                    {
                        Effects = []
                    },
                    Ability = new ChampionAbility
                    {
                        Id = "test-ability",
                        Name = "Test Ability",
                        Cooldown = 5.0,
                        Effects = []
                    }
                },
                playerId);
    }
}
