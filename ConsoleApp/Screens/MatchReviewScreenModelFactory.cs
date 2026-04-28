using ConsoleApp.Constants;
using ConsoleApp.Presentation;

namespace ConsoleApp.Screens
{
    /// <summary>
    /// Builds completed match review screen render models.
    /// </summary>
    internal sealed class MatchReviewScreenModelFactory
    {
        /// <summary>
        /// Builds the last completed match review screen.
        /// </summary>
        public ScreenRenderModel BuildLastMatchReviewScreen(
            ScreenHeaderModel header,
            MatchReview match,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(match);

            List<string> lines =
            [
                $"Week {match.WeekNumber} | {match.MatchType} | {match.BestOfLabel}",
                string.Empty,
                $"{match.WinnerTeamName} defeated {GetLosingTeamName(match)} {FormatWinnerLoserScore(match)}",
                string.Empty,
                "Round Results",
                "Round   Winner                    Score"
            ];
            lines.AddRange(match.Rounds.Select(round =>
                $"{round.RoundNumber,-7} {round.WinnerTeamName,-25} {round.BlueScore}-{round.RedScore}"));
            lines.Add(string.Empty);
            lines.Add("Key Moments");
            lines.AddRange(GetMatchKeyMoments(match).DefaultIfEmpty("No key moments are available yet."));

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.ViewReplay,
                    ConsoleConstants.ViewRounds,
                    "view round <number>",
                    "show player <player name>",
                    "show team <team name>",
                    ConsoleConstants.Home,
                    ConsoleConstants.ShowLeague,
                    ConsoleConstants.ShowTeam,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = header,
                Message = message,
                Title = "Last Match Review"
            };
        }

        /// <summary>
        /// Builds the completed match round list screen.
        /// </summary>
        public ScreenRenderModel BuildRoundListScreen(
            ScreenHeaderModel header,
            MatchReview match,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(match);

            List<string> lines =
            [
                $"{match.BlueTeamName} vs {match.RedTeamName}",
                "Round   Winner                    Score   Duration   Note"
            ];
            lines.AddRange(match.Rounds.Select(round =>
                $"{round.RoundNumber,-7} {round.WinnerTeamName,-25} {round.BlueScore}-{round.RedScore}     "
                + $"{FormatTimestamp(round.Duration),-8} {round.KeyMoments.FirstOrDefault() ?? "No key moment."}"));

            return new ScreenRenderModel
            {
                Commands =
                [
                    "view round <number>",
                    "show player <player name>",
                    "show team <team name>",
                    ConsoleConstants.MatchSummary,
                    ConsoleConstants.Home,
                    ConsoleConstants.Back,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = header,
                Message = message,
                Title = "Round List"
            };
        }

        /// <summary>
        /// Builds the completed match round review screen.
        /// </summary>
        public ScreenRenderModel BuildRoundReviewScreen(
            ScreenHeaderModel header,
            MatchReview match,
            RoundReview round,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(match);
            ArgumentNullException.ThrowIfNull(round);

            List<string> lines =
            [
                $"{match.BlueTeamName} vs {match.RedTeamName}",
                $"Round {round.RoundNumber}",
                $"Winner: {round.WinnerTeamName}",
                $"Final score: {round.BlueScore}-{round.RedScore}",
                $"Duration: {FormatTimestamp(round.Duration)}",
                $"Blue team score: {round.BlueScore}",
                $"Red team score: {round.RedScore}",
                string.Empty,
                "Champion Stats"
            ];

            if (round.ChampionStats.Count == 0)
            {
                lines.Add("Champion stats are not available for this round yet.");
            }
            else
            {
                lines.Add("Champion              Team        K  D  Dmg  Heal  Shield");
                lines.AddRange(round.ChampionStats.Select(stat =>
                    $"{stat.ChampionName,-21} {stat.TeamName,-10} {stat.Kills,1}  {stat.Deaths,1}  "
                    + $"{stat.DamageDealt,3}  {stat.HealingDone,4}  {stat.ShieldingDone,6}"));
            }

            lines.Add(string.Empty);
            lines.Add("Key Moments");
            lines.AddRange(round.KeyMoments.Take(8).DefaultIfEmpty("No key moments are available yet."));
            lines.Add(string.Empty);
            lines.Add("Replay Preview");
            lines.AddRange(round.ReplayMessages.Take(6).Select(FormatReplayMessage));

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.ViewReplay,
                    ConsoleConstants.NextRound,
                    ConsoleConstants.PreviousRound,
                    "show champion <name>",
                    "show player <player name>",
                    "show team <team name>",
                    ConsoleConstants.ShowOpponent,
                    ConsoleConstants.MatchSummary,
                    ConsoleConstants.Home,
                    ConsoleConstants.Back,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = header,
                Message = message,
                Title = "Round Review"
            };
        }

        /// <summary>
        /// Builds the replay review screen.
        /// </summary>
        public ScreenRenderModel BuildReplayReviewScreen(
            ScreenHeaderModel header,
            MatchReview match,
            ReplayReviewState replayState,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(match);
            ArgumentNullException.ThrowIfNull(replayState);

            IReadOnlyList<ReplayMessage> messages = GetReplayReviewMessages(match, replayState);
            int pageCount = Math.Max(1, GetReplayReviewPageCount(match, replayState));
            int pageIndex = Math.Min(replayState.PageIndex, pageCount - 1);
            IReadOnlyList<string> replayLines = messages
                .Skip(pageIndex * replayState.PageSize)
                .Take(replayState.PageSize)
                .Select(FormatReplayMessage)
                .ToList();
            string roundLabel = replayState.RoundNumber.HasValue
                ? $"Round {replayState.RoundNumber.Value}"
                : "Match Replay";

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.NextPage,
                    ConsoleConstants.PreviousPage,
                    "view round <number>",
                    "show champion <name>",
                    "show player <player name>",
                    "show team <team name>",
                    ConsoleConstants.ShowOpponent,
                    ConsoleConstants.MatchSummary,
                    ConsoleConstants.Home,
                    ConsoleConstants.Back,
                    ConsoleConstants.Help
                ],
                ContentLines =
                [
                    $"{match.BlueTeamName} vs {match.RedTeamName}",
                    roundLabel,
                    $"Page {pageIndex + 1} of {pageCount}",
                    string.Empty,
                    "Replay Messages",
                    .. replayLines.DefaultIfEmpty("No replay messages are available.")
                ],
                Header = header,
                Message = message,
                Title = "Replay Review"
            };
        }

        /// <summary>
        /// Gets the page count for the replay review screen.
        /// </summary>
        public int GetReplayReviewPageCount(MatchReview match, ReplayReviewState replayState)
        {
            ArgumentNullException.ThrowIfNull(match);
            ArgumentNullException.ThrowIfNull(replayState);

            int messageCount = GetReplayReviewMessages(match, replayState).Count;
            return (int)Math.Ceiling(messageCount / (double)replayState.PageSize);
        }

        private static string FormatReplayMessage(ReplayMessage replayMessage) =>
            $"{FormatTimestamp(replayMessage.Timestamp)}  {replayMessage.Text}";

        private static string FormatTimestamp(TimeSpan timestamp) =>
            $"{(int)timestamp.TotalMinutes:00}:{timestamp.Seconds:00}";

        private static IReadOnlyList<string> GetMatchKeyMoments(MatchReview match) =>
            match.Rounds
                .SelectMany(round => round.KeyMoments.Select(moment => $"R{round.RoundNumber} {moment}"))
                .Take(6)
                .ToList();

        private static string GetLosingTeamName(MatchReview match) =>
            string.Equals(match.WinnerTeamName, match.BlueTeamName, StringComparison.Ordinal)
                ? match.RedTeamName
                : match.BlueTeamName;

        private static string FormatWinnerLoserScore(MatchReview match) =>
            string.Equals(match.WinnerTeamName, match.BlueTeamName, StringComparison.Ordinal)
                ? $"{match.BlueRoundWins}-{match.RedRoundWins}"
                : $"{match.RedRoundWins}-{match.BlueRoundWins}";

        private static IReadOnlyList<ReplayMessage> GetReplayReviewMessages(
            MatchReview match,
            ReplayReviewState replayState)
        {
            if (replayState.RoundNumber is null)
            {
                return match.MatchMessages;
            }

            return match.Rounds
                .FirstOrDefault(round => round.RoundNumber == replayState.RoundNumber.Value)
                ?.ReplayMessages
                ?? [];
        }
    }
}
