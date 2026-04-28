using System.Security.Cryptography;
using System.Text;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Objects;

namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Creates completed match review models from presentation results.
    /// </summary>
    public sealed class MatchReviewFactory
    {
        /// <summary>
        /// Creates a match review.
        /// </summary>
        /// <param name="presentedMatch">The presented match.</param>
        /// <param name="weekNumber">The match week.</param>
        /// <param name="teamNameResolver">Resolves team names.</param>
        /// <returns>The match review.</returns>
        public MatchReview Create(
            PresentedMatch presentedMatch,
            int weekNumber,
            Func<string, string> teamNameResolver)
        {
            ArgumentNullException.ThrowIfNull(presentedMatch);
            ArgumentNullException.ThrowIfNull(teamNameResolver);

            MatchResult result = presentedMatch.Result;
            List<RoundReview> rounds = [];
            foreach (PresentedRound presentedRound in presentedMatch.Rounds.OrderBy(round => round.Result.RoundNumber))
            {
                bool blueWon = string.Equals(
                    presentedRound.Result.WinningTeamId,
                    result.BlueTeamId,
                    StringComparison.Ordinal);
                rounds.Add(new RoundReview
                {
                    BlueScore = blueWon ? 1 : 0,
                    Duration = GetDuration(presentedRound.Messages),
                    KeyMoments = GetKeyMoments(presentedRound.Messages, 8),
                    RedScore = blueWon ? 0 : 1,
                    ReplayMessages = presentedRound.Messages,
                    RoundNumber = presentedRound.Result.RoundNumber,
                    WinnerTeamName = teamNameResolver(presentedRound.Result.WinningTeamId)
                });
            }

            IReadOnlyList<ReplayMessage> matchMessages = rounds
                .SelectMany(round => round.ReplayMessages.Select(message => message with
                {
                    Text = $"R{round.RoundNumber} {message.Text}"
                }))
                .ToList();

            return new MatchReview
            {
                BestOfLabel = $"Best of {result.BestOf}",
                BlueRoundWins = result.BlueRoundWins,
                BlueTeamName = teamNameResolver(result.BlueTeamId),
                MatchId = CreateStableGuid(result.MatchId),
                MatchMessages = matchMessages,
                MatchType = result.MatchType == AutoSim.Domain.Enums.MatchType.RegularSeason
                    ? "Regular Season"
                    : result.MatchType.ToString(),
                RedRoundWins = result.RedRoundWins,
                RedTeamName = teamNameResolver(result.RedTeamId),
                Rounds = rounds,
                WeekNumber = weekNumber,
                WinnerTeamName = teamNameResolver(result.WinningTeamId)
            };
        }

        /// <summary>
        /// Gets key moments from replay messages.
        /// </summary>
        /// <param name="messages">The replay messages.</param>
        /// <param name="limit">The maximum moments to return.</param>
        /// <returns>Readable key moment strings.</returns>
        public static IReadOnlyList<string> GetKeyMoments(IReadOnlyList<ReplayMessage> messages, int limit) =>
            messages
                .Where(IsKeyMoment)
                .Take(limit)
                .Select(message => $"{FormatTimestamp(message.Timestamp)} {message.Text}")
                .ToList();

        private static bool IsKeyMoment(ReplayMessage message) =>
            message.Severity != ReplayMessageSeverity.Normal
            || message.Category is ReplayMessageCategory.Kill
                or ReplayMessageCategory.Fight
                or ReplayMessageCategory.RoundEnd
                or ReplayMessageCategory.MatchEnd
                or ReplayMessageCategory.Objective;

        private static TimeSpan GetDuration(IReadOnlyList<ReplayMessage> messages) =>
            messages.Count == 0 ? TimeSpan.Zero : messages.Max(message => message.Timestamp);

        private static Guid CreateStableGuid(string value)
        {
            byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(value));
            return new Guid(hash);
        }

        private static string FormatTimestamp(TimeSpan timestamp) =>
            $"{(int)timestamp.TotalMinutes:00}:{timestamp.Seconds:00}";
    }
}
