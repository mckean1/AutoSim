using AutoSim.Domain.Enums;
using AutoSim.Domain.Interfaces;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Resolves combat effect targets from active and global champion pools.
    /// </summary>
    public static class CombatTargeting
    {
        /// <summary>
        /// Selects the targets affected by a combat effect.
        /// </summary>
        /// <param name="source">The champion applying the effect.</param>
        /// <param name="effect">The attack effect being applied.</param>
        /// <param name="allChampions">All living and inactive champions in the match.</param>
        /// <param name="activeChampions">Champions currently participating in the active fight.</param>
        /// <param name="rng">The seeded match random source.</param>
        /// <returns>The selected living targets.</returns>
        public static IReadOnlyList<ChampionInstance> SelectTargets(
            ChampionInstance source,
            AttackEffect effect,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng)
        {
            ArgumentNullException.ThrowIfNull(effect);

            return SelectTargets(source, effect.TargetMode, effect.TargetScope, allChampions, activeChampions, rng);
        }

        /// <summary>
        /// Selects the targets affected by an ability effect.
        /// </summary>
        /// <param name="source">The champion applying the effect.</param>
        /// <param name="effect">The ability effect being applied.</param>
        /// <param name="allChampions">All living and inactive champions in the match.</param>
        /// <param name="activeChampions">Champions currently participating in the active fight.</param>
        /// <param name="rng">The seeded match random source.</param>
        /// <returns>The selected living targets.</returns>
        public static IReadOnlyList<ChampionInstance> SelectTargets(
            ChampionInstance source,
            AbilityEffect effect,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng)
        {
            ArgumentNullException.ThrowIfNull(effect);

            return SelectTargets(source, effect.TargetMode, effect.TargetScope, allChampions, activeChampions, rng);
        }

        /// <summary>
        /// Selects all valid candidates for an effect before target-scope random selection.
        /// </summary>
        /// <param name="source">The champion applying the effect.</param>
        /// <param name="effect">The attack effect being evaluated.</param>
        /// <param name="allChampions">All living and inactive champions in the match.</param>
        /// <param name="activeChampions">Champions currently participating in the active fight.</param>
        /// <returns>The full valid living candidate pool.</returns>
        public static IReadOnlyList<ChampionInstance> SelectCandidatePool(
            ChampionInstance source,
            AttackEffect effect,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions)
        {
            ArgumentNullException.ThrowIfNull(effect);
            return SelectCandidatePool(source, effect.TargetMode, allChampions, activeChampions);
        }

        /// <summary>
        /// Selects all valid candidates for an ability effect before target-scope random selection.
        /// </summary>
        /// <param name="source">The champion applying the effect.</param>
        /// <param name="effect">The ability effect being evaluated.</param>
        /// <param name="allChampions">All living and inactive champions in the match.</param>
        /// <param name="activeChampions">Champions currently participating in the active fight.</param>
        /// <returns>The full valid living candidate pool.</returns>
        public static IReadOnlyList<ChampionInstance> SelectCandidatePool(
            ChampionInstance source,
            AbilityEffect effect,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions)
        {
            ArgumentNullException.ThrowIfNull(effect);
            return SelectCandidatePool(source, effect.TargetMode, allChampions, activeChampions);
        }

        private static IReadOnlyList<ChampionInstance> SelectTargets(
            ChampionInstance source,
            TargetMode targetMode,
            TargetScope targetScope,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng)
        {
            ArgumentNullException.ThrowIfNull(rng);

            IReadOnlyList<ChampionInstance> candidates = SelectCandidatePool(
                source,
                targetMode,
                allChampions,
                activeChampions);

            if (targetScope == TargetScope.All || candidates.Count == 0)
            {
                return candidates;
            }

            return [candidates[rng.Next(candidates.Count)]];
        }

        private static IReadOnlyList<ChampionInstance> SelectCandidatePool(
            ChampionInstance source,
            TargetMode targetMode,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(allChampions);
            ArgumentNullException.ThrowIfNull(activeChampions);

            List<ChampionInstance> allLivingChampions = allChampions.Where(champion => champion.IsAlive).ToList();
            List<ChampionInstance> activeLivingChampions = activeChampions.Where(champion => champion.IsAlive).ToList();

            return GetCandidates(source, targetMode, allLivingChampions, activeLivingChampions);
        }

        private static List<ChampionInstance> GetCandidates(
            ChampionInstance source,
            TargetMode targetMode,
            IReadOnlyList<ChampionInstance> allLivingChampions,
            IReadOnlyList<ChampionInstance> activeLivingChampions)
        {
            return targetMode switch
            {
                TargetMode.EnemyFrontline => GetPositionWithFallback(
                    GetEnemies(source, activeLivingChampions),
                    FormationPosition.Frontline,
                    FormationPosition.Backline),
                TargetMode.EnemyBackline => GetPositionWithFallback(
                    GetEnemies(source, activeLivingChampions),
                    FormationPosition.Backline,
                    FormationPosition.Frontline),
                TargetMode.EnemyAny => GetEnemies(source, activeLivingChampions),
                TargetMode.AllyFrontline => GetPositionWithFallback(
                    GetAllies(source, activeLivingChampions),
                    FormationPosition.Frontline,
                    FormationPosition.Backline),
                TargetMode.AllyBackline => GetPositionWithFallback(
                    GetAllies(source, activeLivingChampions),
                    FormationPosition.Backline,
                    FormationPosition.Frontline),
                TargetMode.AllyAny => GetAllies(source, activeLivingChampions),
                TargetMode.GlobalEnemy => GetEnemies(source, allLivingChampions),
                TargetMode.GlobalAlly => GetAllies(source, allLivingChampions),
                TargetMode.GlobalAll => allLivingChampions.ToList(),
                TargetMode.Self => source.IsAlive ? [source] : [],
                _ => throw new ArgumentOutOfRangeException(nameof(targetMode), targetMode, "Unsupported target mode.")
            };
        }

        private static List<ChampionInstance> GetAllies(
            ChampionInstance source,
            IEnumerable<ChampionInstance> champions) =>
            champions
                .Where(champion => champion.TeamSide == source.TeamSide)
                .ToList();

        private static List<ChampionInstance> GetEnemies(
            ChampionInstance source,
            IEnumerable<ChampionInstance> champions) =>
            champions
                .Where(champion => champion.TeamSide != source.TeamSide)
                .ToList();

        private static List<ChampionInstance> GetPositionWithFallback(
            IEnumerable<ChampionInstance> champions,
            FormationPosition preferredPosition,
            FormationPosition fallbackPosition)
        {
            List<ChampionInstance> championList = champions.ToList();
            List<ChampionInstance> preferredChampions = championList
                .Where(champion => GetCurrentPosition(champion) == preferredPosition)
                .ToList();

            return preferredChampions.Count > 0
                ? preferredChampions
                : championList.Where(champion => GetCurrentPosition(champion) == fallbackPosition).ToList();
        }

        private static FormationPosition GetCurrentPosition(ChampionInstance champion)
        {
            if (champion.CurrentFightPosition is not double fightPosition)
            {
                return champion.Position;
            }

            if (champion.TeamSide == TeamSide.Blue)
            {
                return champion.LanePosition <= fightPosition - champion.CurrentBacklineOffset / 2.0
                    ? FormationPosition.Backline
                    : FormationPosition.Frontline;
            }

            return champion.LanePosition >= fightPosition + champion.CurrentBacklineOffset / 2.0
                ? FormationPosition.Backline
                : FormationPosition.Frontline;
        }
    }
}
