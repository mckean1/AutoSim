using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;

namespace AutoSim.Domain.Tests.Services
{
    internal sealed class CombatEffectApplicatorTests
    {
        [Test]
        public void ApplyDamage_NoShields_ReducesHealth()
        {
            ChampionInstance target = CreateInstance();

            CombatEffectApplicator.ApplyDamage(target, 250);

            Assert.That(target.CurrentHealth, Is.EqualTo(750));
        }

        [Test]
        public void ApplyDamage_ShieldExists_ConsumesShieldBeforeHealth()
        {
            ChampionInstance target = CreateInstance();
            CombatEffectApplicator.ApplyShield(target, 100, 5.0);

            CombatEffectApplicator.ApplyDamage(target, 100);

            Assert.That(target.CurrentHealth, Is.EqualTo(1000));
        }

        [Test]
        public void ApplyDamage_DamageLessThanShield_PartiallyConsumesShield()
        {
            ChampionInstance target = CreateInstance();
            CombatEffectApplicator.ApplyShield(target, 100, 5.0);

            CombatEffectApplicator.ApplyDamage(target, 40);

            Assert.That(target.Shields.Single().Amount, Is.EqualTo(60));
        }

        [Test]
        public void ApplyDamage_DamageGreaterThanShield_ReducesHealthWithRemainingDamage()
        {
            ChampionInstance target = CreateInstance();
            CombatEffectApplicator.ApplyShield(target, 100, 5.0);

            CombatEffectApplicator.ApplyDamage(target, 175);

            Assert.That(target.CurrentHealth, Is.EqualTo(925));
        }

        [Test]
        public void ApplyDamage_DepletedShield_RemovesShield()
        {
            ChampionInstance target = CreateInstance();
            CombatEffectApplicator.ApplyShield(target, 100, 5.0);

            CombatEffectApplicator.ApplyDamage(target, 100);

            Assert.That(target.Shields, Is.Empty);
        }

        [Test]
        public void ApplyDamage_DeadChampion_DoesNothing()
        {
            ChampionInstance target = CreateInstance();
            target.CurrentHealth = 0;

            CombatEffectApplicator.ApplyDamage(target, 100);

            Assert.That(target.CurrentHealth, Is.EqualTo(0));
        }

        [Test]
        public void ApplyHeal_DamagedChampion_RestoresHealth()
        {
            ChampionInstance target = CreateInstance();
            target.CurrentHealth = 700;

            CombatEffectApplicator.ApplyHeal(target, 150);

            Assert.That(target.CurrentHealth, Is.EqualTo(850));
        }

        [Test]
        public void ApplyHeal_HealingAboveMaximumHealth_CapsAtMaximumHealth()
        {
            ChampionInstance target = CreateInstance();
            target.CurrentHealth = 900;

            CombatEffectApplicator.ApplyHeal(target, 200);

            Assert.That(target.CurrentHealth, Is.EqualTo(1000));
        }

        [Test]
        public void ApplyHeal_ShieldExists_DoesNotRestoreShield()
        {
            ChampionInstance target = CreateInstance();
            target.CurrentHealth = 900;
            CombatEffectApplicator.ApplyShield(target, 100, 5.0);
            CombatEffectApplicator.ApplyDamage(target, 40);

            CombatEffectApplicator.ApplyHeal(target, 50);

            Assert.That(target.Shields.Single().Amount, Is.EqualTo(60));
        }

        [Test]
        public void ApplyHeal_DeadChampion_DoesNothing()
        {
            ChampionInstance target = CreateInstance();
            target.CurrentHealth = 0;

            CombatEffectApplicator.ApplyHeal(target, 100);

            Assert.That(target.CurrentHealth, Is.EqualTo(0));
        }

        [Test]
        public void ApplyShield_LivingChampion_AddsActiveShield()
        {
            ChampionInstance target = CreateInstance();

            CombatEffectApplicator.ApplyShield(target, 100, 5.0);

            Assert.That(target.Shields.Single().Amount, Is.EqualTo(100));
        }

        [Test]
        public void ApplyShield_DeadChampion_DoesNothing()
        {
            ChampionInstance target = CreateInstance();
            target.CurrentHealth = 0;

            CombatEffectApplicator.ApplyShield(target, 100, 5.0);

            Assert.That(target.Shields, Is.Empty);
        }

        [Test]
        public void ApplyShield_MultipleApplications_StacksShields()
        {
            ChampionInstance target = CreateInstance();

            CombatEffectApplicator.ApplyShield(target, 100, 5.0);
            CombatEffectApplicator.ApplyShield(target, 200, 5.0);

            Assert.That(target.Shields, Has.Count.EqualTo(2));
        }

        [Test]
        public void ApplyDamage_MultipleShields_ConsumesOldestShieldFirst()
        {
            ChampionInstance target = CreateInstance();
            CombatEffectApplicator.ApplyShield(target, 100, 5.0);
            CombatEffectApplicator.ApplyShield(target, 200, 5.0);

            CombatEffectApplicator.ApplyDamage(target, 150);

            Assert.That(target.Shields.First().Amount, Is.EqualTo(150));
        }

        [Test]
        public void TickShields_MultipleShields_DurationsTickDownIndependently()
        {
            ChampionInstance target = CreateInstance();
            CombatEffectApplicator.ApplyShield(target, 100, 3.0);
            CombatEffectApplicator.ApplyShield(target, 200, 5.0);

            CombatEffectApplicator.TickShields(target, 1.0);

            Assert.That(target.Shields.Select(shield => shield.Duration), Is.EqualTo(new[] { 2.0, 4.0 }));
        }

        [Test]
        public void TickShields_DurationExpires_RemovesShield()
        {
            ChampionInstance target = CreateInstance();
            CombatEffectApplicator.ApplyShield(target, 100, 5.0);

            CombatEffectApplicator.TickShields(target, 5.0);

            Assert.That(target.Shields, Is.Empty);
        }

        [Test]
        public void TickShields_DepletedShieldExists_RemovesShield()
        {
            ChampionInstance target = CreateInstance();
            target.Shields.Add(new ActiveShield
            {
                Amount = 0,
                Duration = 5.0
            });

            CombatEffectApplicator.TickShields(target, 0);

            Assert.That(target.Shields, Is.Empty);
        }

        [Test]
        public void TickShields_MultipleShields_StackAndExpireIndependently()
        {
            ChampionInstance target = CreateInstance();
            CombatEffectApplicator.ApplyShield(target, 100, 3.0);
            CombatEffectApplicator.ApplyShield(target, 200, 5.0);

            CombatEffectApplicator.TickShields(target, 3.0);

            Assert.That(target.Shields.Single().Amount, Is.EqualTo(200));
        }

        [Test]
        public void Create_DefinitionHasDefaults_InitializesPositionAndCurrentHealth()
        {
            ChampionDefinition definition = TestChampionFactory.CreateDefinition(FormationPosition.Backline);

            ChampionInstance instance = ChampionInstanceFactory.Create(definition, "player-one");

            Assert.That(instance.Position, Is.EqualTo(FormationPosition.Backline));
            Assert.That(instance.CurrentHealth, Is.EqualTo(1000));
        }

        private static ChampionInstance CreateInstance() => TestChampionFactory.CreateInstance();
    }
}
