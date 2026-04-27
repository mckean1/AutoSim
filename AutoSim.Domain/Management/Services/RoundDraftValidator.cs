using AutoSim.Domain.Management.Interfaces;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Validates drafted champion selections before a round is simulated.
    /// </summary>
    public sealed class RoundDraftValidator : IRoundDraftValidator
    {
        private const int RequiredTeamChampionCount = 5;

        /// <inheritdoc />
        public void Validate(RoundDraft draft, IReadOnlyList<ChampionDefinition> championCatalog)
        {
            ArgumentNullException.ThrowIfNull(draft);
            ArgumentNullException.ThrowIfNull(championCatalog);

            ValidateSide("Blue", draft.BlueChampions);
            ValidateSide("Red", draft.RedChampions);

            Dictionary<string, ChampionDefinition> catalogById = championCatalog
                .ToDictionary(champion => champion.Id, StringComparer.Ordinal);
            List<string> selectedIds = draft.BlueChampions
                .Concat(draft.RedChampions)
                .Select(champion => champion.Id)
                .ToList();
            string? unknownChampionId = selectedIds.FirstOrDefault(id => !catalogById.ContainsKey(id));
            if (unknownChampionId is not null)
            {
                throw new ArgumentException($"Selected champion does not exist in the catalog: {unknownChampionId}.", nameof(draft));
            }

            string? duplicateChampionId = selectedIds
                .GroupBy(id => id, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .FirstOrDefault();
            if (duplicateChampionId is not null)
            {
                throw new ArgumentException($"Duplicate champion selected in round draft: {duplicateChampionId}.", nameof(draft));
            }
        }

        private static void ValidateSide(string sideName, IReadOnlyList<ChampionDefinition> champions)
        {
            if (champions.Count != RequiredTeamChampionCount)
            {
                throw new ArgumentException(
                    $"{sideName} draft must contain exactly {RequiredTeamChampionCount} champions.",
                    nameof(champions));
            }

            string? duplicateChampionId = champions
                .GroupBy(champion => champion.Id, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .FirstOrDefault();
            if (duplicateChampionId is not null)
            {
                throw new ArgumentException(
                    $"{sideName} draft contains duplicate champion: {duplicateChampionId}.",
                    nameof(champions));
            }
        }
    }
}
