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
            AttackEffect firstHit = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            AttackEffect secondHit = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance(
                "player-one",
                attackPower: 100,
                attackEffects: [firstHit, secondHit]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");

            ChampionCombatResolver.ResolveAttack(source, [source, enemy], [source, enemy], new QueueMatchRandom(0, 0));

            Assert.That(enemy.CurrentHealth, Is.EqualTo(800));
        }

        [Test]
        public void ResolveAbility_MultipleEffects_ResolvesAllEffectsInOrder()
        {
            AbilityEffect shield = CreateAbilityEffect(CombatEffectType.Shield, 100, TargetMode.Self, TargetScope.One, 5.0);
            AbilityEffect damage = CreateAbilityEffect(CombatEffectType.Damage, 150, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance(
                "player-one",
                abilityEffects: [shield, damage]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");

            ChampionCombatResolver.ResolveAbility(source, [source, enemy], [source, enemy], new QueueMatchRandom(0));

            Assert.That(new[] { source.Shields.Single().Amount, enemy.CurrentHealth }, Is.EqualTo(new[] { 100, 850 }));
        }

        [Test]
        public void ResolveAttack_LaterEffectSeesEarlierKill_DoesNotTargetDeadChampion()
        {
            AttackEffect killFirstTarget = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            AttackEffect hitRemainingTarget = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance(
                "player-one",
                attackPower: 1000,
                attackEffects: [killFirstTarget, hitRemainingTarget]);
            ChampionInstance firstEnemy = TestChampionFactory.CreateInstance("player-two");
            ChampionInstance secondEnemy = TestChampionFactory.CreateInstance("player-two", FormationPosition.Backline);

            ChampionCombatResolver.ResolveAttack(
                source,
                [source, firstEnemy, secondEnemy],
                [source, firstEnemy, secondEnemy],
                new QueueMatchRandom(0, 0));

            Assert.That(new[] { firstEnemy.CurrentHealth, secondEnemy.CurrentHealth }, Is.EqualTo(new[] { 0, 0 }));
        }

        [Test]
        public void ResolveAttack_HybridEffects_DamagesEnemyAndHealsSelf()
        {
            AttackEffect damage = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            AttackEffect heal = CreateAttackEffect(CombatEffectType.Heal, TargetMode.Self, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance(
                "player-one",
                attackPower: 100,
                attackEffects: [damage, heal]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");
            source.CurrentHealth = 600;

            ChampionCombatResolver.ResolveAttack(source, [source, enemy], [source, enemy], new QueueMatchRandom(0));

            Assert.That(new[] { enemy.CurrentHealth, source.CurrentHealth }, Is.EqualTo(new[] { 900, 700 }));
        }

        [Test]
        public void ResolveAbility_HybridEffects_ShieldsAllyAndDamagesEnemy()
        {
            AbilityEffect shield = CreateAbilityEffect(
                CombatEffectType.Shield,
                100,
                TargetMode.AllyBackline,
                TargetScope.One,
                5.0);
            AbilityEffect damage = CreateAbilityEffect(
                CombatEffectType.Damage,
                150,
                TargetMode.EnemyFrontline,
                TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance(
                "player-one",
                abilityEffects: [shield, damage]);
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
            AbilityEffect selfShield = CreateAbilityEffect(CombatEffectType.Shield, 100, TargetMode.Self, TargetScope.One, 5.0);
            AbilityEffect globalDamage = CreateAbilityEffect(
                CombatEffectType.Damage,
                150,
                TargetMode.GlobalEnemy,
                TargetScope.All);
            ChampionInstance source = TestChampionFactory.CreateInstance(
                "player-one",
                abilityEffects: [selfShield, globalDamage]);
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
        public void ResolveAttack_DamageEffect_UsesCurrentAttackPowerExactly()
        {
            AttackEffect damage = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", attackPower: 20, attackEffects: [damage]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two", health: 100);

            ChampionCombatResolver.ResolveAttack(source, [source, enemy], [source, enemy], new QueueMatchRandom(0));

            Assert.That(enemy.CurrentHealth, Is.EqualTo(80));
        }

        [Test]
        public void ResolveAttack_HealEffect_UsesCurrentAttackPowerExactly()
        {
            AttackEffect heal = CreateAttackEffect(CombatEffectType.Heal, TargetMode.Self, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", attackPower: 20, attackEffects: [heal]);
            source.CurrentHealth = 50;

            ChampionCombatResolver.ResolveAttack(source, [source], [source], new QueueMatchRandom(0));

            Assert.That(source.CurrentHealth, Is.EqualTo(70));
        }

        [Test]
        public void ResolveAttack_ShieldEffect_UsesCurrentAttackPowerExactly()
        {
            AttackEffect shield = CreateAttackEffect(CombatEffectType.Shield, TargetMode.Self, TargetScope.One, 5.0);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", attackPower: 20, attackEffects: [shield]);

            ChampionCombatResolver.ResolveAttack(source, [source], [source], new QueueMatchRandom(0));

            Assert.That(source.Shields.Single().Amount, Is.EqualTo(20));
        }

        [Test]
        public void ResolveAbility_DamageEffect_UsesAbilityPowerExactly()
        {
            AbilityEffect damage = CreateAbilityEffect(CombatEffectType.Damage, 35, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", attackPower: 20, abilityEffects: [damage]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two", health: 100);

            ChampionCombatResolver.ResolveAbility(source, [source, enemy], [source, enemy], new QueueMatchRandom(0));

            Assert.That(enemy.CurrentHealth, Is.EqualTo(65));
        }

        [Test]
        public void ResolveAbility_HealEffect_UsesAbilityPowerExactly()
        {
            AbilityEffect heal = CreateAbilityEffect(CombatEffectType.Heal, 35, TargetMode.Self, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", attackPower: 20, abilityEffects: [heal]);
            source.CurrentHealth = 50;

            ChampionCombatResolver.ResolveAbility(source, [source], [source], new QueueMatchRandom(0));

            Assert.That(source.CurrentHealth, Is.EqualTo(85));
        }

        [Test]
        public void ResolveAbility_ShieldEffect_UsesAbilityPowerExactly()
        {
            AbilityEffect shield = CreateAbilityEffect(CombatEffectType.Shield, 35, TargetMode.Self, TargetScope.One, 5.0);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", attackPower: 20, abilityEffects: [shield]);

            ChampionCombatResolver.ResolveAbility(source, [source], [source], new QueueMatchRandom(0));

            Assert.That(source.Shields.Single().Amount, Is.EqualTo(35));
        }

        [Test]
        public void ResolveAttack_AfterLevelUp_UsesIncreasedCurrentAttackPower()
        {
            AttackEffect damage = CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", attackEffects: [damage]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");
            new ChampionProgressionService(new RoundSettings()).AddExperience(source, 100);

            ChampionCombatResolver.ResolveAttack(source, [source, enemy], [source, enemy], new QueueMatchRandom(0));

            Assert.That(enemy.CurrentHealth, Is.EqualTo(998));
        }

        [Test]
        public void ResolveAbility_AfterLevelUp_DoesNotUseIncreasedCurrentAttackPower()
        {
            AbilityEffect damage = CreateAbilityEffect(CombatEffectType.Damage, 35, TargetMode.EnemyAny, TargetScope.One);
            ChampionInstance source = TestChampionFactory.CreateInstance("player-one", abilityEffects: [damage]);
            ChampionInstance enemy = TestChampionFactory.CreateInstance("player-two");
            new ChampionProgressionService(new RoundSettings()).AddExperience(source, 100);

            ChampionCombatResolver.ResolveAbility(source, [source, enemy], [source, enemy], new QueueMatchRandom(0));

            Assert.That(enemy.CurrentHealth, Is.EqualTo(965));
        }

        [Test]
        public void AttackEffect_DoesNotExposeAmountProperties()
        {
            string[] propertyNames = typeof(AttackEffect).GetProperties().Select(property => property.Name).ToArray();

            Assert.That(propertyNames, Does.Not.Contain("AbilityPower"));
            Assert.That(propertyNames, Does.Not.Contain("Value"));
            Assert.That(propertyNames, Does.Not.Contain("AttackPower"));
        }

        [Test]
        public void ChampionDefinition_UsesAttackPowerInsteadOfPower()
        {
            string[] propertyNames = typeof(ChampionDefinition).GetProperties().Select(property => property.Name).ToArray();

            Assert.That(propertyNames, Does.Contain("AttackPower"));
            Assert.That(propertyNames, Does.Not.Contain("Power"));
        }

        private static AttackEffect CreateAttackEffect(
            CombatEffectType type,
            TargetMode targetMode,
            TargetScope targetScope,
            double? duration = null) =>
            TestChampionFactory.CreateAttackEffect(type, targetMode, targetScope, duration);

        private static AbilityEffect CreateAbilityEffect(
            CombatEffectType type,
            int abilityPower,
            TargetMode targetMode,
            TargetScope targetScope,
            double? duration = null) =>
            TestChampionFactory.CreateAbilityEffect(type, abilityPower, targetMode, targetScope, duration);
    }
}
