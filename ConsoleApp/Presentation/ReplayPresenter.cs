using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Objects;
using ManagementRoundResult = AutoSim.Domain.Management.Models.RoundResult;

namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Converts match results into player-facing replay messages.
    /// </summary>
    public sealed class ReplayPresenter
    {
        /// <summary>
        /// Creates a presented match from a management match result.
        /// </summary>
        /// <param name="result">The match result.</param>
        /// <param name="championCatalog">The champion catalog.</param>
        /// <param name="teamNameResolver">Resolves team names by identifier.</param>
        /// <returns>The presented match.</returns>
        public PresentedMatch Present(
            MatchResult result,
            IReadOnlyList<ChampionDefinition> championCatalog,
            Func<string, string> teamNameResolver)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(championCatalog);
            ArgumentNullException.ThrowIfNull(teamNameResolver);

            Dictionary<string, string> championNames = championCatalog.ToDictionary(
                champion => champion.Id,
                champion => champion.Name,
                StringComparer.Ordinal);
            int blueWins = 0;
            int redWins = 0;
            List<PresentedRound> rounds = [];

            foreach (ManagementRoundResult round in result.RoundResults.OrderBy(round => round.RoundNumber))
            {
                if (string.Equals(round.WinningTeamId, result.BlueTeamId, StringComparison.Ordinal))
                {
                    blueWins++;
                }
                else
                {
                    redWins++;
                }

                rounds.Add(new PresentedRound
                {
                    Result = round,
                    Messages = BuildMessages(result, round, championNames, teamNameResolver, blueWins, redWins)
                });
            }

            return new PresentedMatch
            {
                Result = result,
                Rounds = rounds
            };
        }

        private static IReadOnlyList<ReplayMessage> BuildMessages(
            MatchResult match,
            ManagementRoundResult round,
            IReadOnlyDictionary<string, string> championNames,
            Func<string, string> teamNameResolver,
            int blueWins,
            int redWins)
        {
            List<string> blueChampions = round.BlueChampionIds.Select(id => GetChampionName(championNames, id)).ToList();
            List<string> redChampions = round.RedChampionIds.Select(id => GetChampionName(championNames, id)).ToList();
            string blueTeam = teamNameResolver(round.BlueTeamId);
            string redTeam = teamNameResolver(round.RedTeamId);
            string winningTeam = teamNameResolver(round.WinningTeamId);
            string firstBlue = blueChampions.ElementAtOrDefault(0) ?? blueTeam;
            string secondBlue = blueChampions.ElementAtOrDefault(1) ?? blueTeam;
            string firstRed = redChampions.ElementAtOrDefault(0) ?? redTeam;
            string secondRed = redChampions.ElementAtOrDefault(1) ?? redTeam;

            return
            [
                Message(0, ReplayMessageCategory.RoundStart, ReplayMessageSeverity.Important,
                    $"Round {round.RoundNumber} begins. {blueTeam} faces {redTeam}."),
                Message(18, ReplayMessageCategory.Fight, ReplayMessageSeverity.Normal,
                    $"A fight breaks out as {firstBlue} contests {firstRed}."),
                Message(42, ReplayMessageCategory.Damage, ReplayMessageSeverity.Normal,
                    $"{firstBlue} hits {firstRed} for 20 damage."),
                Message(66, ReplayMessageCategory.Shield, ReplayMessageSeverity.Normal,
                    $"{secondBlue} shields an ally for 25."),
                Message(92, ReplayMessageCategory.Retreat, ReplayMessageSeverity.Normal,
                    $"{secondRed} retreats toward base."),
                Message(128, ReplayMessageCategory.Kill, ReplayMessageSeverity.Important,
                    $"{firstBlue} defeats {firstRed}. {blueTeam} pressures the map."),
                Message(171, ReplayMessageCategory.Objective, ReplayMessageSeverity.Normal,
                    $"{redTeam} answers with a side-lane objective."),
                Message(224, ReplayMessageCategory.Fight, ReplayMessageSeverity.Important,
                    $"{blueTeam} and {redTeam} collapse for the deciding fight."),
                Message(263, ReplayMessageCategory.RoundEnd, ReplayMessageSeverity.Critical,
                    $"{winningTeam} wins round {round.RoundNumber}. Match score: {blueWins}-{redWins}."),
                Message(300, ReplayMessageCategory.MatchEnd, ReplayMessageSeverity.Critical,
                    IsMatchComplete(match, blueWins, redWins)
                        ? $"{teamNameResolver(match.WinningTeamId)} wins the match {FormatWinnerLoserScore(match)}."
                        : $"Round {round.RoundNumber} complete.")
            ];
        }

        private static ReplayMessage Message(
            int seconds,
            ReplayMessageCategory category,
            ReplayMessageSeverity severity,
            string text) =>
            new()
            {
                Category = category,
                Severity = severity,
                Timestamp = TimeSpan.FromSeconds(seconds),
                Text = text
            };

        private static bool IsMatchComplete(MatchResult match, int blueWins, int redWins) =>
            blueWins == match.BlueRoundWins && redWins == match.RedRoundWins;

        private static string FormatWinnerLoserScore(MatchResult match) =>
            string.Equals(match.WinningTeamId, match.BlueTeamId, StringComparison.Ordinal)
                ? $"{match.BlueRoundWins}-{match.RedRoundWins}"
                : $"{match.RedRoundWins}-{match.BlueRoundWins}";

        private static string GetChampionName(IReadOnlyDictionary<string, string> championNames, string championId) =>
            championNames.TryGetValue(championId, out string? name) ? name : championId;
    }
}
