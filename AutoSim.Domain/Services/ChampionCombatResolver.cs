using AutoSim.Domain.Interfaces;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Resolves champion attacks and abilities.
    /// </summary>
    public static class ChampionCombatResolver
    {
        /// <summary>
        /// Resolves a champion's basic attack effects in order.
        /// </summary>
        /// <param name="source">The attacking champion.</param>
        /// <param name="allChampions">All champions in the match.</param>
        /// <param name="activeChampions">Champions currently participating in the active fight.</param>
        /// <param name="rng">The seeded match random source.</param>
        public static void ResolveAttack(
            ChampionInstance source,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng)
        {
            ArgumentNullException.ThrowIfNull(source);

            ResolveEffects(source, source.Definition.Attack.Effects, allChampions, activeChampions, rng, source.CurrentPower);
        }

        /// <summary>
        /// Resolves a champion's ability effects in order.
        /// </summary>
        /// <param name="source">The ability source.</param>
        /// <param name="allChampions">All champions in the match.</param>
        /// <param name="activeChampions">Champions currently participating in the active fight.</param>
        /// <param name="rng">The seeded match random source.</param>
        public static void ResolveAbility(
            ChampionInstance source,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng)
        {
            ArgumentNullException.ThrowIfNull(source);

            ResolveEffects(source, source.Definition.Ability.Effects, allChampions, activeChampions, rng, 0);
        }

        private static void ResolveEffects(
            ChampionInstance source,
            IEnumerable<CombatEffect> effects,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng,
            int valueBonus)
        {
            ArgumentNullException.ThrowIfNull(effects);

            foreach (CombatEffect effect in effects)
            {
                CombatEffectResolver.ResolveEffect(source, effect, allChampions, activeChampions, rng, valueBonus);
            }
        }
    }
}
