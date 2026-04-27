using AutoSim.Domain.Enums;
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
        /// <param name="effect">The effect being applied.</param>
        /// <param name="allChampions">All living and inactive champions in the match.</param>
        /// <param name="activeChampions">Champions currently participating in the active fight.</param>
        /// <param name="selectIndex">Optional index selector for single-target selection.</param>
        /// <returns>The selected living targets.</returns>
        public static IReadOnlyList<ChampionInstance> SelectTargets(
            ChampionInstance source,
            CombatEffect effect,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            Func<int, int>? selectIndex = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(effect);
            ArgumentNullException.ThrowIfNull(allChampions);
            ArgumentNullException.ThrowIfNull(activeChampions);

            List<ChampionInstance> allLivingChampions = allChampions.Where(champion => champion.IsAlive).ToList();
            List<ChampionInstance> activeLivingChampions = activeChampions.Where(champion => champion.IsAlive).ToList();
            List<ChampionInstance> candidates = GetCandidates(source, effect.TargetMode, allLivingChampions, activeLivingChampions);

            if (effect.TargetScope == TargetScope.All || candidates.Count <= 1)
            {
                return candidates;
            }

            int selectedIndex = selectIndex?.Invoke(candidates.Count) ?? 0;

            if (selectedIndex < 0 || selectedIndex >= candidates.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(selectIndex),
                    $"Selected target index {selectedIndex} is outside the candidate range.");
            }

            return [candidates[selectedIndex]];
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
            champions.Where(champion => string.Equals(champion.PlayerId, source.PlayerId, StringComparison.Ordinal)).ToList();

        private static List<ChampionInstance> GetEnemies(
            ChampionInstance source,
            IEnumerable<ChampionInstance> champions) =>
            champions.Where(champion => !string.Equals(champion.PlayerId, source.PlayerId, StringComparison.Ordinal)).ToList();

        private static List<ChampionInstance> GetPositionWithFallback(
            IEnumerable<ChampionInstance> champions,
            FormationPosition preferredPosition,
            FormationPosition fallbackPosition)
        {
            List<ChampionInstance> championList = champions.ToList();
            List<ChampionInstance> preferredChampions = championList
                .Where(champion => champion.Position == preferredPosition)
                .ToList();

            return preferredChampions.Count > 0
                ? preferredChampions
                : championList.Where(champion => champion.Position == fallbackPosition).ToList();
        }
    }
}
