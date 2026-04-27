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
        /// Resolves a combat effect from a source champion.
        /// </summary>
        /// <param name="source">The champion applying the effect.</param>
        /// <param name="effect">The effect to resolve.</param>
        /// <param name="allChampions">All champions in the match.</param>
        /// <param name="activeChampions">Champions currently participating in the active fight.</param>
        /// <param name="rng">The seeded match random source.</param>
        public static void ResolveEffect(
            ChampionInstance source,
            CombatEffect effect,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng,
            int valueBonus = 0)
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
                ApplyEffect(effect, target, valueBonus);
            }
        }

        private static void ApplyEffect(CombatEffect effect, ChampionInstance target, int valueBonus)
        {
            int value = effect.Value + valueBonus;

            switch (effect.Type)
            {
                case CombatEffectType.Damage:
                    CombatEffectApplicator.ApplyDamage(target, value);
                    break;
                case CombatEffectType.Heal:
                    CombatEffectApplicator.ApplyHeal(target, value);
                    break;
                case CombatEffectType.Shield:
                    CombatEffectApplicator.ApplyShield(target, value, effect.Duration ?? DefaultShieldDuration);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(effect),
                        effect.Type,
                        "Unsupported combat effect type.");
            }
        }
    }
}
