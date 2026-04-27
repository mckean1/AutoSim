using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Interfaces;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Provides deterministic coach-controlled MVP champion drafting.
    /// </summary>
    public sealed class DeterministicRoundDraftService : IRoundDraftService
    {
        /// <inheritdoc />
        public RoundDraft DraftRound(RoundDraftContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            List<ChampionDefinition> availableChampions = context.ChampionCatalog
                .OrderBy(champion => CreateStableDraftScore(context.Seed, champion.Id))
                .ToList();
            List<ChampionDefinition> blueChampions = SelectComposition(
                availableChampions,
                new HashSet<string>(StringComparer.Ordinal));
            HashSet<string> blueIds = blueChampions.Select(champion => champion.Id).ToHashSet(StringComparer.Ordinal);
            List<ChampionDefinition> redChampions = SelectComposition(availableChampions, blueIds);

            return new RoundDraft
            {
                BlueChampions = blueChampions,
                RedChampions = redChampions
            };
        }

        private static List<ChampionDefinition> SelectComposition(
            IReadOnlyList<ChampionDefinition> availableChampions,
            ISet<string> excludedChampionIds) =>
        [
            .. SelectByRole(availableChampions, ChampionRole.Fighter, 2, excludedChampionIds),
            .. SelectByRole(availableChampions, ChampionRole.Mage, 1, excludedChampionIds),
            .. SelectByRole(availableChampions, ChampionRole.Marksman, 1, excludedChampionIds),
            .. SelectByRole(availableChampions, ChampionRole.Support, 1, excludedChampionIds)
        ];

        private static IReadOnlyList<ChampionDefinition> SelectByRole(
            IReadOnlyList<ChampionDefinition> availableChampions,
            ChampionRole role,
            int count,
            ISet<string> excludedChampionIds) =>
            availableChampions
                .Where(champion => champion.Role == role && !excludedChampionIds.Contains(champion.Id))
                .Take(count)
                .ToList();

        private static int CreateStableDraftScore(int seed, string championId)
        {
            int score = seed;
            foreach (char value in championId)
            {
                score = unchecked((score * 397) ^ value);
            }

            return score;
        }
    }
}
