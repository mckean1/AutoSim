using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using ConsoleApp.Constants;

namespace ConsoleApp.Screens
{
    /// <summary>
    /// Builds champion catalog and detail screen render models.
    /// </summary>
    internal sealed class ChampionScreenModelFactory
    {
        /// <summary>
        /// Builds the champion catalog screen.
        /// </summary>
        /// <param name="header">The screen header.</param>
        /// <param name="roleFilter">The optional role filter.</param>
        /// <param name="message">The optional status message.</param>
        /// <returns>The screen render model.</returns>
        public ScreenRenderModel BuildChampionCatalogScreen(
            ScreenHeaderModel header,
            ChampionRole? roleFilter,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);

            IReadOnlyList<ChampionDefinition> champions = ChampionCatalog.GetDefaultChampions()
                .OrderBy(champion => champion.Role)
                .ThenBy(champion => champion.Name, StringComparer.Ordinal)
                .Where(champion => roleFilter is null || champion.Role == roleFilter)
                .ToList();
            List<string> lines =
            [
                roleFilter is null ? "All champions" : $"Filtered role: {roleFilter}",
                "Champion          Role      HP   AP   Pwr  Speed  CD    Description"
            ];
            lines.AddRange(champions.Select(FormatChampionCatalogRow));

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Home,
                    ConsoleConstants.Back,
                    "show champion <name>",
                    "filter role <role>",
                    ConsoleConstants.ClearFilter,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = header,
                Message = message,
                Title = "Champion Catalog"
            };
        }

        /// <summary>
        /// Builds the champion detail screen.
        /// </summary>
        /// <param name="header">The screen header.</param>
        /// <param name="champion">The selected champion.</param>
        /// <param name="message">The optional status message.</param>
        /// <returns>The screen render model.</returns>
        public ScreenRenderModel BuildChampionDetailScreen(
            ScreenHeaderModel header,
            ChampionDefinition champion,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(champion);

            List<string> lines =
            [
                champion.Name,
                string.Empty,
                $"Role: {champion.Role}",
                $"Description: {champion.Description}",
                string.Empty,
                "Stats",
                $"Health: {champion.Health}",
                $"Attack Power: {champion.AttackPower}",
                $"Ability Power: {GetAbilityPower(champion)}",
                $"Action Speed: {champion.AttackSpeed:0.##} attacks/sec",
                string.Empty,
                "Ability",
                $"Name: {champion.Ability.Name}",
                $"Cooldown: {champion.Ability.Cooldown:0.##}s",
                $"Cast Time: {champion.Ability.CastTime:0.##}s"
            ];

            AbilityEffect? firstEffect = champion.Ability.Effects.FirstOrDefault();
            if (firstEffect is not null)
            {
                lines.Add($"Target Mode: {firstEffect.TargetMode}");
                lines.Add($"Target Scope: {firstEffect.TargetScope}");
            }

            lines.Add(string.Empty);
            lines.Add("Effects");
            lines.AddRange(champion.Ability.Effects.Select(FormatAbilityEffect));
            lines.Add(string.Empty);
            lines.Add("Basic Attack");
            lines.AddRange(champion.Attack.Effects.Select(FormatAttackEffect));

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Back,
                    ConsoleConstants.ShowChampions,
                    "show champion <name>",
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = header,
                Message = message,
                Title = "Champion Detail"
            };
        }

        private static string FormatChampionCatalogRow(ChampionDefinition champion) =>
            $"{champion.Name,-17} {champion.Role,-9} {champion.Health,3}  {champion.AttackPower,3}  "
            + $"{GetAbilityPower(champion),3}  {champion.AttackSpeed,5:0.##}  {champion.Ability.Cooldown,4:0.#}  "
            + champion.Description;

        private static int GetAbilityPower(ChampionDefinition champion) =>
            champion.Ability.Effects.Sum(effect => effect.AbilityPower);

        private static string FormatAbilityEffect(AbilityEffect effect)
        {
            string amount = effect.AbilityPower > 0 ? $": {effect.AbilityPower}" : string.Empty;
            string duration = effect.Duration.HasValue ? $" for {effect.Duration.Value:0.##}s" : string.Empty;
            return $"{effect.Type}{amount} | Target: {effect.TargetMode} | Scope: {effect.TargetScope}{duration}";
        }

        private static string FormatAttackEffect(AttackEffect effect)
        {
            string duration = effect.Duration.HasValue ? $" for {effect.Duration.Value:0.##}s" : string.Empty;
            return $"{effect.Type} | Target: {effect.TargetMode} | Scope: {effect.TargetScope}{duration}";
        }
    }
}
