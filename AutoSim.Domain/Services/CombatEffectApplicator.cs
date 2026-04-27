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
        public static void ApplyShield(ChampionInstance target, int amount, double duration)
        {
            ArgumentNullException.ThrowIfNull(target);

            if (!target.IsAlive || amount <= 0 || duration <= 0)
            {
                return;
            }

            target.Shields.Add(new ActiveShield
            {
                Amount = amount,
                Duration = duration
            });
        }

        /// <summary>
        /// Applies damage to a living target, consuming shields before health.
        /// </summary>
        /// <param name="target">The damage target.</param>
        /// <param name="amount">The damage amount.</param>
        public static void ApplyDamage(ChampionInstance target, int amount)
        {
            ArgumentNullException.ThrowIfNull(target);

            if (!target.IsAlive || amount <= 0)
            {
                return;
            }

            int remainingDamage = amount;

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
        }

        /// <summary>
        /// Applies healing to a living target without restoring shields.
        /// </summary>
        /// <param name="target">The healing target.</param>
        /// <param name="amount">The healing amount.</param>
        public static void ApplyHeal(ChampionInstance target, int amount)
        {
            ArgumentNullException.ThrowIfNull(target);

            if (!target.IsAlive || amount <= 0)
            {
                return;
            }

            target.CurrentHealth = Math.Min(target.Definition.Health, target.CurrentHealth + amount);
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
