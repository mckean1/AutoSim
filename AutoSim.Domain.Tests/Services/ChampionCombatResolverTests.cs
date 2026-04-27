using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;

namespace AutoSim.Domain.Tests.Services
{
    internal sealed class ChampionCombatResolverTests
    {
        [Test]
        public void ResolveAttack_MultipleEffects_ResolvesAllEffectsInOrder()
        {
            CombatEffect firstHit = CreateEffect(CombatEffectType.Damage, 100, TargetMode.EnemyAny, TargetScope.One);
            CombatEffect secondHit = CreateEffect(CombatEffectType.Damage, 200, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance(
                "player-one",
                attackEffects: [firstHit, secondHit]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");

            ChampionCombatResolver.ResolveAttack(source, [source, enemy], [source, enemy], new QueueMatchRandom(0, 0));

            Assert.That(enemy.CurrentHealth, Is.EqualTo(700));
        }

        [Test]
        public void ResolveAbility_MultipleEffects_ResolvesAllEffectsInOrder()
        {
            CombatEffect shield = CreateEffect(CombatEffectType.Shield, 100, TargetMode.Self, TargetScope.One, 5.0);
            CombatEffect damage = CreateEffect(CombatEffectType.Damage, 150, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance(
                "player-one",
                abilityCombatEffects: [shield, damage]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");

            ChampionCombatResolver.ResolveAbility(source, [source, enemy], [source, enemy], new QueueMatchRandom(0));

            Assert.That(new[] { source.Shields.Single().Amount, enemy.CurrentHealth }, Is.EqualTo(new[] { 100, 850 }));
        }

        [Test]
        public void ResolveAttack_LaterEffectSeesEarlierKill_DoesNotTargetDeadChampion()
        {
            CombatEffect killFirstTarget = CreateEffect(
                CombatEffectType.Damage,
                1000,
                TargetMode.EnemyAny,
                TargetScope.One);
            CombatEffect hitRemainingTarget = CreateEffect(
                CombatEffectType.Damage,
                100,
                TargetMode.EnemyAny,
                TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance(
                "player-one",
                attackEffects: [killFirstTarget, hitRemainingTarget]);
            ChampionInstance firstEnemy = TestChampionFactory.CreateInstance("player-two");
            ChampionInstance secondEnemy = TestChampionFactory.CreateInstance("player-two", FormationPosition.Backline);

            ChampionCombatResolver.ResolveAttack(
                source,
                [source, firstEnemy, secondEnemy],
                [source, firstEnemy, secondEnemy],
                new QueueMatchRandom(0, 0));

            Assert.That(new[] { firstEnemy.CurrentHealth, secondEnemy.CurrentHealth }, Is.EqualTo(new[] { 0, 900 }));
        }

        [Test]
        public void ResolveAttack_HybridEffects_DamagesEnemyAndHealsSelf()
        {
            CombatEffect damage = CreateEffect(CombatEffectType.Damage, 100, TargetMode.EnemyAny, TargetScope.One);
            CombatEffect heal = CreateEffect(CombatEffectType.Heal, 200, TargetMode.Self, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", attackEffects: [damage, heal]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");
            source.CurrentHealth = 600;

            ChampionCombatResolver.ResolveAttack(source, [source, enemy], [source, enemy], new QueueMatchRandom(0));

            Assert.That(new[] { enemy.CurrentHealth, source.CurrentHealth }, Is.EqualTo(new[] { 900, 800 }));
        }

        [Test]
        public void ResolveAbility_HybridEffects_ShieldsAllyAndDamagesEnemy()
        {
            CombatEffect shield = CreateEffect(
                CombatEffectType.Shield,
                100,
                TargetMode.AllyBackline,
                TargetScope.One,
                5.0);
            CombatEffect damage = CreateEffect(
                CombatEffectType.Damage,
                150,
                TargetMode.EnemyFrontline,
                TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance(
                "player-one",
                abilityCombatEffects: [shield, damage]);
            ChampionInstance ally = TestChampionFactory.CreateInstance("player-one", FormationPosition.Backline);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two", FormationPosition.Frontline);

            ChampionCombatResolver.ResolveAbility(
                source,
                [source, ally, enemy],
                [source, ally, enemy],
                new QueueMatchRandom(0, 0));

            Assert.That(new[] { ally.Shields.Single().Amount, enemy.CurrentHealth }, Is.EqualTo(new[] { 100, 850 }));
        }

        [Test]
        public void ResolveAbility_DifferentEffects_UseDifferentTargetModes()
        {
            CombatEffect selfShield = CreateEffect(CombatEffectType.Shield, 100, TargetMode.Self, TargetScope.One, 5.0);
            CombatEffect globalDamage = CreateEffect(
                CombatEffectType.Damage,
                150,
                TargetMode.GlobalEnemy,
                TargetScope.All);
            ChampionInstance source = TestChampionFactory.CreateInstance(
                "player-one",
                abilityCombatEffects: [selfShield, globalDamage]);
            ChampionInstance activeEnemy = TestChampionFactory.CreateInstance("player-two");
            ChampionInstance inactiveEnemy = TestChampionFactory.CreateInstance(
                "player-two",
                FormationPosition.Backline);

            ChampionCombatResolver.ResolveAbility(
                source,
                [source, activeEnemy, inactiveEnemy],
                [source, activeEnemy],
                new QueueMatchRandom(0));

            Assert.That(
                new[] { source.Shields.Single().Amount, activeEnemy.CurrentHealth, inactiveEnemy.CurrentHealth },
                Is.EqualTo(new[] { 100, 850, 850 }));
        }

        [Test]
        public void ResolveAttack_DamageEffect_AddsCurrentPower()
        {
            CombatEffect damage = CreateEffect(CombatEffectType.Damage, 5, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", power: 20, attackEffects: [damage]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");

            ChampionCombatResolver.ResolveAttack(source, [source, enemy], [source, enemy], new QueueMatchRandom(0));

            Assert.That(enemy.CurrentHealth, Is.EqualTo(975));
        }

        [Test]
        public void ResolveAttack_HealEffect_AddsCurrentPower()
        {
            CombatEffect heal = CreateEffect(CombatEffectType.Heal, 5, TargetMode.Self, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", power: 20, attackEffects: [heal]);
            source.CurrentHealth = 900;

            ChampionCombatResolver.ResolveAttack(source, [source], [source], new QueueMatchRandom(0));

            Assert.That(source.CurrentHealth, Is.EqualTo(925));
        }

        [Test]
        public void ResolveAttack_ShieldEffect_AddsCurrentPower()
        {
            CombatEffect shield = CreateEffect(CombatEffectType.Shield, 5, TargetMode.Self, TargetScope.One, 5.0);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", power: 20, attackEffects: [shield]);

            ChampionCombatResolver.ResolveAttack(source, [source], [source], new QueueMatchRandom(0));

            Assert.That(source.Shields.Single().Amount, Is.EqualTo(25));
        }

        [Test]
        public void ResolveAbility_DamageEffect_DoesNotAddCurrentPower()
        {
            CombatEffect damage = CreateEffect(CombatEffectType.Damage, 35, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", power: 20, abilityCombatEffects: [damage]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");

            ChampionCombatResolver.ResolveAbility(source, [source, enemy], [source, enemy], new QueueMatchRandom(0));

            Assert.That(enemy.CurrentHealth, Is.EqualTo(965));
        }

        [Test]
        public void ResolveAbility_HealEffect_DoesNotAddCurrentPower()
        {
            CombatEffect heal = CreateEffect(CombatEffectType.Heal, 35, TargetMode.Self, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", power: 20, abilityCombatEffects: [heal]);
            source.CurrentHealth = 900;

            ChampionCombatResolver.ResolveAbility(source, [source], [source], new QueueMatchRandom(0));

            Assert.That(source.CurrentHealth, Is.EqualTo(935));
        }

        [Test]
        public void ResolveAbility_ShieldEffect_DoesNotAddCurrentPower()
        {
            CombatEffect shield = CreateEffect(CombatEffectType.Shield, 35, TargetMode.Self, TargetScope.One, 5.0);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", power: 20, abilityCombatEffects: [shield]);

            ChampionCombatResolver.ResolveAbility(source, [source], [source], new QueueMatchRandom(0));

            Assert.That(source.Shields.Single().Amount, Is.EqualTo(35));
        }

        [Test]
        public void ResolveAttack_AfterLevelUp_UsesIncreasedCurrentPower()
        {
            CombatEffect damage = CreateEffect(CombatEffectType.Damage, 5, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", attackEffects: [damage]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");
            new ChampionProgressionService(new RoundSettings()).AddExperience(source, 100);

            ChampionCombatResolver.ResolveAttack(source, [source, enemy], [source, enemy], new QueueMatchRandom(0));

            Assert.That(enemy.CurrentHealth, Is.EqualTo(993));
        }

        [Test]
        public void ResolveAbility_AfterLevelUp_DoesNotUseIncreasedCurrentPower()
        {
            CombatEffect damage = CreateEffect(CombatEffectType.Damage, 35, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", abilityCombatEffects: [damage]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");
            new ChampionProgressionService(new RoundSettings()).AddExperience(source, 100);

            ChampionCombatResolver.ResolveAbility(source, [source, enemy], [source, enemy], new QueueMatchRandom(0));

            Assert.That(enemy.CurrentHealth, Is.EqualTo(965));
        }

        private static CombatEffect CreateEffect(
            CombatEffectType type,
            int value,
            TargetMode targetMode,
            TargetScope targetScope,
            double? duration = null) =>
            TestChampionFactory.CreateEffect(type, value, targetMode, targetScope, duration);
    }
}
