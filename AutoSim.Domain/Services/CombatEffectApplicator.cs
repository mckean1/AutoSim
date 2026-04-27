using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Applies runtime combat effects to champion instances.
    /// </summary>
    public static class CombatEffectApplicator
    {
        /// <summary>
        /// Applies a new shield to a living target.
        /// </summary>
        /// <param name="target">The shield target.</param>
        /// <param name="amount">The damage absorption amount.</param>
        /// <param name="duration">The shield duration in seconds.</param>
        public static int ApplyShield(ChampionInstance target, int amount, double duration)
        {
            ArgumentNullException.ThrowIfNull(target);

            if (!target.IsAlive || amount <= 0 || duration <= 0)
            {
                return 0;
            }

            target.Shields.Add(new ActiveShield
            {
                Amount = amount,
                Duration = duration
            });

            return amount;
        }

        /// <summary>
        /// Applies damage to a living target, consuming shields before health.
        /// </summary>
        /// <param name="target">The damage target.</param>
        /// <param name="amount">The damage amount.</param>
        public static int ApplyDamage(ChampionInstance target, int amount)
        {
            ArgumentNullException.ThrowIfNull(target);

            if (!target.IsAlive || amount <= 0)
            {
                return 0;
            }

            int remainingDamage = amount;
            int healthBefore = target.CurrentHealth;
            int shieldBefore = target.Shields.Sum(shield => shield.Amount);

            foreach (ActiveShield shield in target.Shields)
            {
                if (remainingDamage <= 0)
                {
                    break;
                }

                int absorbedDamage = Math.Min(shield.Amount, remainingDamage);
                shield.Amount -= absorbedDamage;
                remainingDamage -= absorbedDamage;
            }

            target.Shields.RemoveAll(shield => shield.Amount <= 0);

            if (remainingDamage > 0)
            {
                target.CurrentHealth = Math.Max(0, target.CurrentHealth - remainingDamage);
            }

            int shieldAfter = target.Shields.Sum(shield => shield.Amount);
            return shieldBefore - shieldAfter + healthBefore - target.CurrentHealth;
        }

        /// <summary>
        /// Applies healing to a living target without restoring shields.
        /// </summary>
        /// <param name="target">The healing target.</param>
        /// <param name="amount">The healing amount.</param>
        public static int ApplyHeal(ChampionInstance target, int amount)
        {
            ArgumentNullException.ThrowIfNull(target);

            if (!target.IsAlive || amount <= 0)
            {
                return 0;
            }

            int healthBefore = target.CurrentHealth;
            target.CurrentHealth = Math.Min(target.MaximumHealth, target.CurrentHealth + amount);
            return target.CurrentHealth - healthBefore;
        }

        /// <summary>
        /// Advances shield durations and removes expired or depleted shields.
        /// </summary>
        /// <param name="target">The champion whose shields should tick.</param>
        /// <param name="deltaSeconds">The elapsed time in seconds.</param>
        public static void TickShields(ChampionInstance target, double deltaSeconds)
        {
            ArgumentNullException.ThrowIfNull(target);

            if (deltaSeconds <= 0)
            {
                target.Shields.RemoveAll(shield => shield.Amount <= 0 || shield.Duration <= 0);
                return;
            }

            foreach (ActiveShield shield in target.Shields)
            {
                shield.Duration -= deltaSeconds;
            }

            target.Shields.RemoveAll(shield => shield.Amount <= 0 || shield.Duration <= 0);
        }
    }
}
