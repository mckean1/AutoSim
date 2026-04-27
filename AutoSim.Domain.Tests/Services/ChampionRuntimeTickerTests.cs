using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;

namespace AutoSim.Domain.Tests.Services
{
    internal sealed class ChampionRuntimeTickerTests
    {
        [Test]
        public void TickChampion_AliveChampion_ReducesAttackTimerWithoutGoingBelowZero()
        {
            ChampionInstance champion = CreateInstance();
            champion.AttackTimer = 1.0;

            ChampionRuntimeTicker.TickChampion(champion, 2.0);

            Assert.That(champion.AttackTimer, Is.EqualTo(0));
        }

        [Test]
        public void TickChampion_AliveChampion_ReducesAbilityCooldownWithoutGoingBelowZero()
        {
            ChampionInstance champion = CreateInstance();
            champion.AbilityCooldown = 1.0;

            ChampionRuntimeTicker.TickChampion(champion, 2.0);

            Assert.That(champion.AbilityCooldown, Is.EqualTo(0));
        }

        [Test]
        public void TickChampion_DeadChampion_ReducesRespawnTimerWithoutGoingBelowZero()
        {
            ChampionInstance champion = CreateInstance();
            champion.CurrentHealth = 0;
            champion.RespawnTimer = 1.0;

            ChampionRuntimeTicker.TickChampion(champion, 2.0);

            Assert.That(champion.RespawnTimer, Is.EqualTo(0));
        }

        [Test]
        public void TickChampion_DeadChampion_DoesNotReduceAttackTimerOrAbilityCooldown()
        {
            ChampionInstance champion = CreateInstance();
            champion.CurrentHealth = 0;
            champion.AttackTimer = 3.0;
            champion.AbilityCooldown = 4.0;

            ChampionRuntimeTicker.TickChampion(champion, 1.0);

            Assert.That(new[] { champion.AttackTimer, champion.AbilityCooldown }, Is.EqualTo(new[] { 3.0, 4.0 }));
        }

        [Test]
        public void TickChampion_AliveChampion_ReducesShieldDuration()
        {
            ChampionInstance champion = CreateInstance();
            CombatEffectApplicator.ApplyShield(champion, 100, 5.0);

            ChampionRuntimeTicker.TickChampion(champion, 2.0);

            Assert.That(champion.Shields.Single().Duration, Is.EqualTo(3.0));
        }

        [Test]
        public void TickChampion_ExpiredShield_RemovesShield()
        {
            ChampionInstance champion = CreateInstance();
            CombatEffectApplicator.ApplyShield(champion, 100, 1.0);

            ChampionRuntimeTicker.TickChampion(champion, 1.0);

            Assert.That(champion.Shields, Is.Empty);
        }

        [Test]
        public void TickChampion_DeadChampion_ClearsShields()
        {
            ChampionInstance champion = CreateInstance();
            CombatEffectApplicator.ApplyShield(champion, 100, 5.0);
            champion.CurrentHealth = 0;

            ChampionRuntimeTicker.TickChampion(champion, 1.0);

            Assert.That(champion.Shields, Is.Empty);
        }

        private static ChampionInstance CreateInstance() => TestChampionFactory.CreateInstance();
    }
}
