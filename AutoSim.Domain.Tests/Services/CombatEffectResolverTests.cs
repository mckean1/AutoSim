using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;

namespace AutoSim.Domain.Tests.Services
{
    internal sealed class CombatEffectResolverTests
    {
        [Test]
        public void ResolveAbilityEffect_DamageEffect_DamagesSelectedTarget()
        {
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one");
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");
            AbilityEffect effect = CreateEffect(CombatEffectType.Damage, 150, TargetMode.EnemyAny, TargetScope.One);

            Resolve(source, effect, [source, enemy], [source, enemy]);

            Assert.That(enemy.CurrentHealth, Is.EqualTo(850));
        }

        [Test]
        public void ResolveAbilityEffect_HealEffect_HealsSelectedTarget()
        {
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one");
            ChampionInstance ally = TestChampionFactory.CreateInstance("player-one", FormationPosition.Backline);
            ally.CurrentHealth = 700;
            AbilityEffect effect = CreateEffect(CombatEffectType.Heal, 150, TargetMode.AllyBackline, TargetScope.One);

            Resolve(source, effect, [source, ally], [source, ally]);

            Assert.That(ally.CurrentHealth, Is.EqualTo(850));
        }

        [Test]
        public void ResolveAbilityEffect_ShieldEffect_AddsShield()
        {
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one");
            AbilityEffect effect = CreateEffect(CombatEffectType.Shield, 120, TargetMode.Self, TargetScope.One, 4.0);

            Resolve(source, effect, [source], [source]);

            Assert.That(source.Shields.Single().Amount, Is.EqualTo(120));
        }

        [Test]
        public void ResolveAbilityEffect_ShieldDurationNull_UsesDefaultFiveSecondDuration()
        {
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one");
            AbilityEffect effect = CreateEffect(CombatEffectType.Shield, 120, TargetMode.Self, TargetScope.One);

            Resolve(source, effect, [source], [source]);

            Assert.That(source.Shields.Single().Duration, Is.EqualTo(5.0));
        }

        [Test]
        public void ResolveAbilityEffect_ShieldDurationProvided_UsesExplicitDuration()
        {
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one");
            AbilityEffect effect = CreateEffect(CombatEffectType.Shield, 120, TargetMode.Self, TargetScope.One, 7.5);

            Resolve(source, effect, [source], [source]);

            Assert.That(source.Shields.Single().Duration, Is.EqualTo(7.5));
        }

        [Test]
        public void ResolveAbilityEffect_TargetScopeAll_AppliesEffectToEverySelectedTarget()
        {
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one");
            ChampionInstance firstEnemy = TestChampionFactory.CreateInstance("player-two", FormationPosition.Frontline);
            ChampionInstance secondEnemy = TestChampionFactory.CreateInstance("player-two", FormationPosition.Backline);
            AbilityEffect effect = CreateEffect(CombatEffectType.Damage, 100, TargetMode.EnemyAny, TargetScope.All);

            Resolve(source, effect, [source, firstEnemy, secondEnemy], [source, firstEnemy, secondEnemy]);

            Assert.That(new[] { firstEnemy.CurrentHealth, secondEnemy.CurrentHealth }, Is.EqualTo(new[] { 900, 900 }));
        }

        [Test]
        public void ResolveAbilityEffect_NoValidTargets_DoesNothing()
        {
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one");
            ChampionInstance ally = TestChampionFactory.CreateInstance("player-one", FormationPosition.Backline);
            AbilityEffect effect = CreateEffect(CombatEffectType.Damage, 100, TargetMode.EnemyAny, TargetScope.One);

            Resolve(source, effect, [source, ally], [source, ally]);

            Assert.That(ally.CurrentHealth, Is.EqualTo(1000));
        }

        [Test]
        public void ResolveAbilityEffect_DeadChampionSelectedByMode_DoesNotApplyEffect()
        {
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one");
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");
            enemy.CurrentHealth = 0;
            AbilityEffect effect = CreateEffect(CombatEffectType.Heal, 100, TargetMode.GlobalEnemy, TargetScope.All);

            Resolve(source, effect, [source, enemy], [source]);

            Assert.That(enemy.CurrentHealth, Is.EqualTo(0));
        }

        private static void Resolve(
            ChampionInstance source,
            AbilityEffect effect,
            IReadOnlyList<ChampionInstance> allChampions,
            IReadOnlyList<ChampionInstance> activeChampions) =>
            CombatEffectResolver.ResolveAbilityEffect(source, effect, allChampions, activeChampions, new QueueMatchRandom(0));

        private static AbilityEffect CreateEffect(
            CombatEffectType type,
            int abilityPower,
            TargetMode targetMode,
            TargetScope targetScope,
            double? duration = null) =>
            TestChampionFactory.CreateAbilityEffect(type, abilityPower, targetMode, targetScope, duration);
    }
}
