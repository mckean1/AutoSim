using AutoSim.Domain.Enums;
using AutoSim.Domain.Interfaces;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Resolves combat effects against selected targets.
    /// </summary>
    public static class CombatEffectResolver
    {
        private const double DefaultShieldDuration = 5.0;

        /// <summary>
        /// Resolves an attack effect from a source champion.
        /// </summary>
        /// <param name="source">The champion applying the effect.</param>
        /// <param name="effect">The attack effect to resolve.</param>
        /// <param name="allChampions">All champions in the match.</param>
        /// <param name="activeChampions">Champions currently participating in the active fight.</param>
        /// <param name="rng">The seeded match random source.</param>
        public static void ResolveAttackEffect(
            ChampionInstance source,
            AttackEffect effect,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(effect);
            ArgumentNullException.ThrowIfNull(allChampions);
            ArgumentNullException.ThrowIfNull(activeChampions);
            ArgumentNullException.ThrowIfNull(rng);

            IReadOnlyList<ChampionInstance> targets = CombatTargeting.SelectTargets(
                source,
                effect,
                allChampions,
                activeChampions,
                rng);

            foreach (ChampionInstance target in targets)
            {
                ApplyEffect(effect.Type, source.CurrentAttackPower, effect.Duration, target);
            }
        }

        /// <summary>
        /// Resolves an ability effect from a source champion.
        /// </summary>
        /// <param name="source">The champion applying the effect.</param>
        /// <param name="effect">The ability effect to resolve.</param>
        /// <param name="allChampions">All champions in the match.</param>
        /// <param name="activeChampions">Champions currently participating in the active fight.</param>
        /// <param name="rng">The seeded match random source.</param>
        public static void ResolveAbilityEffect(
            ChampionInstance source,
            AbilityEffect effect,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(effect);
            ArgumentNullException.ThrowIfNull(allChampions);
            ArgumentNullException.ThrowIfNull(activeChampions);
            ArgumentNullException.ThrowIfNull(rng);

            IReadOnlyList<ChampionInstance> targets = CombatTargeting.SelectTargets(
                source,
                effect,
                allChampions,
                activeChampions,
                rng);

            foreach (ChampionInstance target in targets)
            {
                ApplyEffect(effect.Type, effect.AbilityPower, effect.Duration, target);
            }
        }

        private static void ApplyEffect(
            CombatEffectType type,
            int amount,
            double? duration,
            ChampionInstance target)
        {
            switch (type)
            {
                case CombatEffectType.Damage:
                    CombatEffectApplicator.ApplyDamage(target, amount);
                    break;
                case CombatEffectType.Heal:
                    CombatEffectApplicator.ApplyHeal(target, amount);
                    break;
                case CombatEffectType.Shield:
                    CombatEffectApplicator.ApplyShield(target, amount, duration ?? DefaultShieldDuration);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type),
                        type,
                        "Unsupported combat effect type.");
            }
        }
    }
}
