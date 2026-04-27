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
        /// <param name="state">The optional round state used for event logging.</param>
        public static void ResolveAttack(
            ChampionInstance source,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng,
            RoundState? state = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            ResolveAttackEffects(source, source.Definition.Attack.Effects, allChampions, activeChampions, rng, state);
        }

        /// <summary>
        /// Resolves a champion's ability effects in order.
        /// </summary>
        /// <param name="source">The ability source.</param>
        /// <param name="allChampions">All champions in the match.</param>
        /// <param name="activeChampions">Champions currently participating in the active fight.</param>
        /// <param name="rng">The seeded match random source.</param>
        /// <param name="state">The optional round state used for event logging.</param>
        public static void ResolveAbility(
            ChampionInstance source,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng,
            RoundState? state = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            ResolveAbilityEffects(source, source.Definition.Ability.Effects, allChampions, activeChampions, rng, state);
        }

        private static void ResolveAttackEffects(
            ChampionInstance source,
            IEnumerable<AttackEffect> effects,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng,
            RoundState? state)
        {
            ArgumentNullException.ThrowIfNull(effects);

            foreach (AttackEffect effect in effects)
            {
                CombatEffectResolver.ResolveAttackEffect(source, effect, allChampions, activeChampions, rng, state);
            }
        }

        private static void ResolveAbilityEffects(
            ChampionInstance source,
            IEnumerable<AbilityEffect> effects,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng,
            RoundState? state)
        {
            ArgumentNullException.ThrowIfNull(effects);

            foreach (AbilityEffect effect in effects)
            {
                CombatEffectResolver.ResolveAbilityEffect(source, effect, allChampions, activeChampions, rng, state);
            }
        }
    }
}
