using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Objects;
using ConsoleApp.Constants;
using ConsoleApp.Presentation;

namespace ConsoleApp.Screens
{
    /// <summary>
    /// Builds screen render models for match flow screens.
    /// </summary>
    internal sealed class MatchScreenModelFactory
    {
        public ScreenRenderModel BuildMatchPreviewScreen(
            ScreenHeaderModel header,
            WorldState world,
            Team humanTeam,
            League league,
            ScheduledMatch match,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(humanTeam);
            ArgumentNullException.ThrowIfNull(league);
            ArgumentNullException.ThrowIfNull(match);

            LeagueStanding blueStanding = GetStanding(league, match.HomeTeamId);
            LeagueStanding redStanding = GetStanding(league, match.AwayTeamId);
            string blueTeamName = FormatTeamName(world, match.HomeTeamId);
            string redTeamName = FormatTeamName(world, match.AwayTeamId);

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Continue,
                    ConsoleConstants.Cancel,
                    ConsoleConstants.ShowTeam,
                    ConsoleConstants.ShowOpponent,
                    ConsoleConstants.ShowChampions,
                    ConsoleConstants.Help
                ],
                ContentLines =
                [
                    $"{blueTeamName} vs {redTeamName}",
                    $"Week {match.Week} | {FormatMatchType(match.MatchType)} | Best of {match.BestOf}",
                    string.Empty,
                    $"{blueTeamName}{FormatHumanMarker(humanTeam, match.HomeTeamId)}",
                    $"Record: {FormatRecord(blueStanding)}",
                    $"Points: {FormatPoints(blueStanding.Points)}",
                    string.Empty,
                    $"{redTeamName}{FormatHumanMarker(humanTeam, match.AwayTeamId)}",
                    $"Record: {FormatRecord(redStanding)}",
                    $"Points: {FormatPoints(redStanding.Points)}",
                    string.Empty,
                    $"First team to {(match.BestOf / 2) + 1} round wins takes the match."
                ],
                Header = header,
                Message = message,
                Title = "Match Preview"
            };
        }

        public ScreenRenderModel BuildReplayPreparationScreen(
            ScreenHeaderModel header,
            WorldState world,
            ScheduledMatch match,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(match);

            string blueTeamName = FormatTeamName(world, match.HomeTeamId);
            string redTeamName = FormatTeamName(world, match.AwayTeamId);

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Play,
                    ConsoleConstants.Pause,
                    ConsoleConstants.Status,
                    ConsoleConstants.Help,
                    ConsoleConstants.QuitReplay
                ],
                ContentLines =
                [
                    "Preparing live replay...",
                    $"Resolving Season {world.Season.Year}, Week {world.Season.CurrentWeek}...",
                    $"{blueTeamName} vs {redTeamName}",
                    $"Week {match.Week} | {FormatMatchType(match.MatchType)} | Best of {match.BestOf}",
                    string.Empty,
                    "The match and the rest of the week are simulating in the background.",
                    "Replay messages will reveal from their timestamps once preparation completes.",
                    string.Empty,
                    "Commands: play, pause, status, help, quit replay"
                ],
                Header = header,
                Message = message,
                Title = "Live Replay"
            };
        }

        public ScreenRenderModel BuildDraftScreen(
            ScreenHeaderModel header,
            WorldState world,
            ScheduledMatch match,
            RoundDraft draft,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(match);
            ArgumentNullException.ThrowIfNull(draft);

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.AutoDraft,
                    ConsoleConstants.Continue,
                    ConsoleConstants.Cancel,
                    ConsoleConstants.ShowChampions,
                    ConsoleConstants.Help
                ],
                ContentLines =
                [
                    "Current round: 1",
                    $"Blue team: {FormatTeamName(world, match.HomeTeamId)}",
                    $"Red team: {FormatTeamName(world, match.AwayTeamId)}",
                    "Ban/pick order: automated MVP draft, no bans yet",
                    string.Empty,
                    "Blue bans: None",
                    "Red bans: None",
                    string.Empty,
                    "Blue picks by position",
                    .. FormatLineup(draft.BlueChampions),
                    string.Empty,
                    "Red picks by position",
                    .. FormatLineup(draft.RedChampions),
                    string.Empty,
                    $"Available champion count: {ChampionCatalog.GetDefaultChampions().Count}",
                    "Current draft status: Ready to auto draft"
                ],
                Header = header,
                Message = message,
                Title = "Draft"
            };
        }

        public ScreenRenderModel BuildDraftSummaryScreen(
            ScreenHeaderModel header,
            WorldState world,
            ScheduledMatch match,
            RoundDraft draft,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(match);
            ArgumentNullException.ThrowIfNull(draft);

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Continue,
                    ConsoleConstants.ShowChampions,
                    ConsoleConstants.Cancel,
                    ConsoleConstants.Help
                ],
                ContentLines =
                [
                    "Round 1 draft complete.",
                    $"Blue team: {FormatTeamName(world, match.HomeTeamId)}",
                    .. FormatLineup(draft.BlueChampions),
                    string.Empty,
                    $"Red team: {FormatTeamName(world, match.AwayTeamId)}",
                    .. FormatLineup(draft.RedChampions),
                    string.Empty,
                    "Bans: None",
                    "Draft note: 10 unique champions selected across both teams."
                ],
                Header = header,
                Message = message,
                Title = "Draft Summary"
            };
        }

        public ScreenRenderModel BuildRoundSummaryScreen(
            ScreenHeaderModel header,
            WorldState world,
            PresentedRound round,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(round);

            IReadOnlyList<ReplayMessage> keyMoments = round.Messages
                .Where(replayMessage => replayMessage.Severity != ReplayMessageSeverity.Normal)
                .TakeLast(5)
                .ToList();

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.NextRound,
                    ConsoleConstants.ViewReplay,
                    "view round <number>",
                    ConsoleConstants.PreviousRound,
                    ConsoleConstants.Continue,
                    ConsoleConstants.MatchSummary,
                    ConsoleConstants.Home,
                    ConsoleConstants.Back,
                    ConsoleConstants.Help
                ],
                ContentLines =
                [
                    $"Round winner: {FormatTeamName(world, round.Result.WinningTeamId)}",
                    $"Final round score: {FormatTeamName(world, round.Result.WinningTeamId)} wins",
                    "Duration: 05:00",
                    $"Blue team: {FormatTeamName(world, round.Result.BlueTeamId)}",
                    $"Red team: {FormatTeamName(world, round.Result.RedTeamId)}",
                    string.Empty,
                    "Champion stats",
                    .. FormatRoundChampionSummary(world, round.Result),
                    string.Empty,
                    "Key moments",
                    .. keyMoments.Select(FormatReplayMessage)
                ],
                Header = header,
                Message = message,
                Title = "Round Summary"
            };
        }

        public ScreenRenderModel BuildMatchSummaryScreen(
            ScreenHeaderModel header,
            WorldState world,
            Team humanTeam,
            League league,
            PresentedMatch presentedMatch,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(world);
            ArgumentNullException.ThrowIfNull(humanTeam);
            ArgumentNullException.ThrowIfNull(league);
            ArgumentNullException.ThrowIfNull(presentedMatch);

            MatchResult result = presentedMatch.Result;
            LeagueStanding standing = GetStanding(league, humanTeam.Id);

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Continue,
                    ConsoleConstants.ViewReplay,
                    ConsoleConstants.ViewRounds,
                    "view round <number>",
                    ConsoleConstants.ShowLeague,
                    ConsoleConstants.ShowTeam,
                    ConsoleConstants.ShowChampions,
                    ConsoleConstants.Home,
                    ConsoleConstants.Help
                ],
                ContentLines =
                [
                    $"Match winner: {FormatTeamName(world, result.WinningTeamId)}",
                    $"Final match score: {FormatTeamName(world, result.BlueTeamId)} {result.BlueRoundWins} - "
                    + $"{FormatTeamName(world, result.RedTeamId)} {result.RedRoundWins}",
                    string.Empty,
                    "Round results",
                    .. result.RoundResults
                        .OrderBy(round => round.RoundNumber)
                        .Select(round => $"Round {round.RoundNumber}: {FormatTeamName(world, round.WinningTeamId)}"),
                    string.Empty,
                    $"Current record: {FormatRecord(standing)}",
                    $"Current points: {FormatPoints(standing.Points)}",
                    "Points change: reflected in standings",
                    string.Empty,
                    $"Key note: {FormatTeamName(world, result.WinningTeamId)} controlled the best-of-{result.BestOf}."
                ],
                Header = header,
                Message = message,
                Title = "Match Summary"
            };
        }

        private static LeagueStanding GetStanding(League league, string teamId) =>
            league.Standings.FirstOrDefault(standing => string.Equals(standing.TeamId, teamId, StringComparison.Ordinal))
            ?? new LeagueStanding
            {
                TeamId = teamId
            };

        private static string FormatTeamName(WorldState world, string teamId) =>
            world.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Teams)
                .FirstOrDefault(team => team.Id == teamId)?.Name ?? teamId;

        private static string FormatMatchType(AutoSim.Domain.Enums.MatchType matchType) =>
            matchType == AutoSim.Domain.Enums.MatchType.RegularSeason ? "Regular Season" : matchType.ToString();

        private static string FormatPoints(int points) =>
            points >= 0 ? $"+{points}" : points.ToString();

        private static string FormatRecord(LeagueStanding standing) =>
            $"{standing.MatchWins}-{standing.MatchLosses}";

        private static string FormatHumanMarker(Team humanTeam, string teamId) =>
            string.Equals(humanTeam.Id, teamId, StringComparison.Ordinal) ? " (You)" : string.Empty;

        private static IReadOnlyList<string> FormatLineup(IReadOnlyList<ChampionDefinition> champions)
        {
            string[] positions = ["Top", "Jungle", "Mid", "Bot", "Support"];
            return champions
                .Select((champion, index) => $"{positions.ElementAtOrDefault(index) ?? "Flex",-9} {champion.Name}")
                .ToList();
        }

        private static IReadOnlyList<string> FormatRoundChampionSummary(
            WorldState world,
            AutoSim.Domain.Management.Models.RoundResult round)
        {
            IReadOnlyDictionary<string, string> championNames = ChampionCatalog.GetDefaultChampions()
                .ToDictionary(champion => champion.Id, champion => champion.Name, StringComparer.Ordinal);
            string blueTeam = TrimTeamName(FormatTeamName(world, round.BlueTeamId));
            string redTeam = TrimTeamName(FormatTeamName(world, round.RedTeamId));

            return round.BlueChampionIds
                .Select(championId => $"{GetChampionName(championNames, championId),-24} {blueTeam} participated")
                .Concat(round.RedChampionIds.Select(championId =>
                    $"{GetChampionName(championNames, championId),-24} {redTeam} participated"))
                .ToList();
        }

        private static string GetChampionName(IReadOnlyDictionary<string, string> championNames, string championId) =>
            championNames.TryGetValue(championId, out string? name) ? name : championId;

        private static string TrimTeamName(string teamName) =>
            teamName.Length <= 6 ? teamName : teamName[..6];

        private static string FormatReplayMessage(ReplayMessage replayMessage) =>
            $"{FormatTimestamp(replayMessage.Timestamp)}  {replayMessage.Text}";

        private static string FormatTimestamp(TimeSpan timestamp) =>
            $"{(int)timestamp.TotalMinutes:00}:{timestamp.Seconds:00}";
    }
}
