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
            Assert.That(target.Shields, Is.Empty);
        }

        [Test]
        public void ApplyDamage_DamageLessThanShield_PartiallyConsumesShield()
        {
            ChampionInstance target = CreateInstance();
            CombatEffectApplicator.ApplyShield(target, 100, 5.0);

            CombatEffectApplicator.ApplyDamage(target, 40);

            Assert.That(target.CurrentHealth, Is.EqualTo(1000));
            Assert.That(target.Shields.Single().Amount, Is.EqualTo(60));
        }

        [Test]
        public void ApplyDamage_DamageGreaterThanShield_ReducesHealthWithRemainingDamage()
        {
            ChampionInstance target = CreateInstance();
            CombatEffectApplicator.ApplyShield(target, 100, 5.0);

            CombatEffectApplicator.ApplyDamage(target, 175);

            Assert.That(target.CurrentHealth, Is.EqualTo(925));
            Assert.That(target.Shields, Is.Empty);
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

            Assert.That(target.CurrentHealth, Is.EqualTo(950));
            Assert.That(target.Shields.Single().Amount, Is.EqualTo(60));
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
        public void TickShields_MultipleShields_StackAndExpireIndependently()
        {
            ChampionInstance target = CreateInstance();
            CombatEffectApplicator.ApplyShield(target, 100, 3.0);
            CombatEffectApplicator.ApplyShield(target, 200, 5.0);

            CombatEffectApplicator.TickShields(target, 3.0);

            Assert.That(target.Shields, Has.Count.EqualTo(1));
            Assert.That(target.Shields.Single().Amount, Is.EqualTo(200));
            Assert.That(target.Shields.Single().Duration, Is.EqualTo(2.0));
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
        public void ApplyShield_DeadChampion_DoesNothing()
        {
            ChampionInstance target = CreateInstance();
            target.CurrentHealth = 0;

            CombatEffectApplicator.ApplyShield(target, 100, 5.0);

            Assert.That(target.Shields, Is.Empty);
        }

        [Test]
        public void Create_DefinitionHasDefaults_InitializesPositionAndCurrentHealth()
        {
            ChampionDefinition definition = CreateDefinition(FormationPosition.Backline);

            ChampionInstance instance = ChampionInstanceFactory.Create(definition, "player-one");

            Assert.That(instance.Position, Is.EqualTo(FormationPosition.Backline));
            Assert.That(instance.CurrentHealth, Is.EqualTo(1000));
        }

        private static ChampionInstance CreateInstance() =>
            ChampionInstanceFactory.Create(CreateDefinition(FormationPosition.Frontline), "player-one");

        private static ChampionDefinition CreateDefinition(FormationPosition defaultPosition) =>
            new ChampionDefinition
            {
                Id = "test-fighter",
                Name = "Test Fighter",
                Role = ChampionRole.Fighter,
                DefaultPosition = defaultPosition,
                Health = 1000,
                Power = 100,
                AttackSpeed = 1.0,
                Attack = new ChampionAttack
                {
                    Effects =
                    [
                        new CombatEffect
                        {
                            Type = CombatEffectType.Damage,
                            Value = 100,
                            TargetMode = TargetMode.EnemyFrontline,
                            TargetScope = TargetScope.One
                        }
                    ]
                },
                Ability = new ChampionAbility
                {
                    Id = "test-ability",
                    Name = "Test Ability",
                    Cooldown = 5.0,
                    Effects =
                    [
                        new CombatEffect
                        {
                            Type = CombatEffectType.Shield,
                            Value = 100,
                            TargetMode = TargetMode.Self,
                            TargetScope = TargetScope.One,
                            Duration = 5.0
                        }
                    ]
                }
            };
    }
}
