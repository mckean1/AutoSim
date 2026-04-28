using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Objects;
using ConsoleApp.Constants;
using ConsoleApp.Presentation;

namespace ConsoleApp.Screens
{
    /// <summary>
    /// Builds screen render models for live replay screens.
    /// </summary>
    internal sealed class LiveReplayScreenModelFactory
    {
        public ScreenRenderModel BuildLiveReplayScreen(
            ScreenHeaderModel header,
            WorldState world,
            MatchPresentationState state,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(state.PresentedMatch);

            PresentedMatch presentedMatch = state.PresentedMatch;
            PresentedRound round = presentedMatch.Rounds[state.RoundIndex];
            LiveReplayState replay = state.LiveReplay;
            int revealedCount = Math.Min(replay.CurrentEventIndex, round.Messages.Count);
            IReadOnlyList<ReplayMessage> visibleMessages = round.Messages
                .Take(revealedCount)
                .Reverse()
                .Take(replay.VisibleMessageCount)
                .ToList();
            TimeSpan currentTime = replay.PlaybackState == ReplayPlaybackState.Complete && round.Messages.Count > 0
                ? round.Messages[^1].Timestamp
                : replay.CurrentPlaybackTime;
            int blueRoundWins = presentedMatch.Rounds
                .Take(state.RoundIndex)
                .Count(presentedRound => string.Equals(
                    presentedRound.Result.WinningTeamId,
                    presentedMatch.Result.BlueTeamId,
                    StringComparison.Ordinal));
            int redRoundWins = state.RoundIndex - blueRoundWins;

            List<string> lines =
            [
                $"{FormatTeamName(world, presentedMatch.Result.BlueTeamId)} vs {FormatTeamName(world, presentedMatch.Result.RedTeamId)}",
                $"Round {round.Result.RoundNumber} | {FormatTimestamp(currentTime)} / 05:00 | "
                + $"{FormatReplayPlaybackState(replay.PlaybackState)} | Speed: {replay.ReplaySpeed}",
                $"Match score: {blueRoundWins}-{redRoundWins}",
                $"{FormatTeamName(world, presentedMatch.Result.BlueTeamId)} 0 | {FormatTeamName(world, presentedMatch.Result.RedTeamId)} 0",
                string.Empty,
                "Champion State",
                "Champion                 Team   HP   Status"
            ];
            lines.AddRange(FormatChampionState(world, presentedMatch.Result, round.Result));
            lines.Add(string.Empty);
            lines.Add("Recent Events");
            lines.AddRange(visibleMessages.Select(FormatReplayMessage));
            lines.Add(string.Empty);
            lines.Add($"Playback state: {FormatReplayPlaybackState(replay.PlaybackState)}");

            return new ScreenRenderModel
            {
                Commands = replay.PlaybackState == ReplayPlaybackState.Playing
                    ?
                    [
                        "Space/p pause",
                        "s skip",
                        "+ faster",
                        "- slower",
                        "q quit",
                        "? help"
                    ]
                    :
                    [
                        ConsoleConstants.Step,
                        ConsoleConstants.Play,
                        ConsoleConstants.Pause,
                        ConsoleConstants.Skip,
                        ConsoleConstants.Faster,
                        ConsoleConstants.Slower,
                        ConsoleConstants.Details,
                        ConsoleConstants.ShowChampions,
                        ConsoleConstants.QuitReplay,
                        ConsoleConstants.Help
                    ],
                ContentLines = lines,
                Header = header,
                Message = message,
                Title = "Live Replay"
            };
        }

        private static string FormatTeamName(WorldState world, string teamId) =>
            world.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Teams)
                .FirstOrDefault(team => team.Id == teamId)?.Name ?? teamId;

        private static IReadOnlyList<string> FormatChampionState(
            WorldState world,
            MatchResult match,
            AutoSim.Domain.Management.Models.RoundResult round)
        {
            IReadOnlyDictionary<string, string> championNames = ChampionCatalog.GetDefaultChampions()
                .ToDictionary(champion => champion.Id, champion => champion.Name, StringComparer.Ordinal);
            string blueTeam = FormatTeamName(world, match.BlueTeamId);
            string redTeam = FormatTeamName(world, match.RedTeamId);

            return round.BlueChampionIds
                .Select(championId => FormatChampionStateLine(championNames, championId, blueTeam))
                .Concat(round.RedChampionIds.Select(championId => FormatChampionStateLine(championNames, championId, redTeam)))
                .ToList();
        }

        private static string FormatChampionStateLine(
            IReadOnlyDictionary<string, string> championNames,
            string championId,
            string teamName) =>
            $"{GetChampionName(championNames, championId),-24} {TrimTeamName(teamName),-6} 100  Active";

        private static string GetChampionName(IReadOnlyDictionary<string, string> championNames, string championId) =>
            championNames.TryGetValue(championId, out string? name) ? name : championId;

        private static string TrimTeamName(string teamName) =>
            teamName.Length <= 6 ? teamName : teamName[..6];

        private static string FormatReplayMessage(ReplayMessage replayMessage) =>
            $"{FormatTimestamp(replayMessage.Timestamp)}  {replayMessage.Text}";

        private static string FormatReplayPlaybackState(ReplayPlaybackState playbackState) =>
            playbackState switch
            {
                ReplayPlaybackState.Playing => "Playing",
                ReplayPlaybackState.Complete => "Complete",
                _ => "Paused"
            };

        private static string FormatTimestamp(TimeSpan timestamp) =>
            $"{(int)timestamp.TotalMinutes:00}:{timestamp.Seconds:00}";
    }
}
