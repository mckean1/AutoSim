using ConsoleApp.Constants;

namespace ConsoleApp.Navigation
{
    internal static class ConsoleCommandParser
    {
        public static ConsoleCommand Parse(string? command)
        {
            string text = command?.Trim() ?? string.Empty;

            return text switch
            {
                _ when IsExact(text, ConsoleConstants.Start) => new ConsoleCommand(ConsoleCommandKind.StartGame, text),
                _ when IsExact(text, ConsoleConstants.Home) => new ConsoleCommand(ConsoleCommandKind.Home, text),
                _ when IsExact(text, ConsoleConstants.StartMatch) => new ConsoleCommand(ConsoleCommandKind.StartMatch, text),
                _ when IsExact(text, ConsoleConstants.Status) => new ConsoleCommand(ConsoleCommandKind.Status, text),
                _ when IsExact(text, ConsoleConstants.ViewLastMatch) => new ConsoleCommand(ConsoleCommandKind.ViewLastMatch, text),
                _ when HasArgument(text, "view round") => new ConsoleCommand(ConsoleCommandKind.ViewRound, text),
                _ when IsExact(text, ConsoleConstants.NextPage) => new ConsoleCommand(ConsoleCommandKind.NextPage, text),
                _ when IsExact(text, ConsoleConstants.PreviousPage) => new ConsoleCommand(ConsoleCommandKind.PreviousPage, text),
                _ when IsExact(text, ConsoleConstants.PreviousRound) => new ConsoleCommand(ConsoleCommandKind.PreviousRound, text),
                _ when IsExact(text, ConsoleConstants.Back) => new ConsoleCommand(ConsoleCommandKind.Back, text),
                _ when IsExact(text, ConsoleConstants.ClearFilter) => new ConsoleCommand(ConsoleCommandKind.ClearFilter, text),
                _ when HasArgument(text, ConsoleConstants.FilterRole) => new ConsoleCommand(ConsoleCommandKind.FilterRole, text),
                _ when HasArgument(text, "show champion") => new ConsoleCommand(ConsoleCommandKind.ShowChampion, text),
                _ when IsExact(text, ConsoleConstants.Continue) => new ConsoleCommand(ConsoleCommandKind.Continue, text),
                _ when IsExact(text, ConsoleConstants.AutoDraft) => new ConsoleCommand(ConsoleCommandKind.AutoDraft, text),
                _ when IsExact(text, ConsoleConstants.Step) => new ConsoleCommand(ConsoleCommandKind.Step, text),
                _ when IsExact(text, ConsoleConstants.Skip) => new ConsoleCommand(ConsoleCommandKind.Skip, text),
                _ when IsExact(text, ConsoleConstants.Play) => new ConsoleCommand(ConsoleCommandKind.Play, text),
                _ when IsExact(text, ConsoleConstants.Pause) => new ConsoleCommand(ConsoleCommandKind.Pause, text),
                _ when IsExact(text, ConsoleConstants.Faster) => new ConsoleCommand(ConsoleCommandKind.Faster, text),
                _ when IsExact(text, ConsoleConstants.Slower) => new ConsoleCommand(ConsoleCommandKind.Slower, text),
                _ when IsExact(text, ConsoleConstants.Details) => new ConsoleCommand(ConsoleCommandKind.Details, text),
                _ when IsExact(text, ConsoleConstants.Exit) => new ConsoleCommand(ConsoleCommandKind.Exit, text),
                _ when HasArgument(text, "pick") || HasArgument(text, "ban") => new ConsoleCommand(ConsoleCommandKind.DraftPlaceholder, text),
                _ when IsExact(text, ConsoleConstants.QuitReplay) => new ConsoleCommand(ConsoleCommandKind.QuitReplay, text),
                _ when IsExact(text, ConsoleConstants.NextRound) => new ConsoleCommand(ConsoleCommandKind.NextRound, text),
                _ when IsExact(text, ConsoleConstants.ViewReplay) => new ConsoleCommand(ConsoleCommandKind.ViewReplay, text),
                _ when IsExact(text, ConsoleConstants.MatchSummary) => new ConsoleCommand(ConsoleCommandKind.MatchSummary, text),
                _ when IsExact(text, ConsoleConstants.ViewRounds) => new ConsoleCommand(ConsoleCommandKind.ViewRounds, text),
                _ when IsExact(text, ConsoleConstants.Cancel) => new ConsoleCommand(ConsoleCommandKind.Cancel, text),
                _ when HasArgument(text, ConsoleConstants.ShowTeam) => new ConsoleCommand(ConsoleCommandKind.ShowSpecificTeam, text),
                _ when IsExact(text, ConsoleConstants.ShowTeam) => new ConsoleCommand(ConsoleCommandKind.ShowTeam, text),
                _ when IsExact(text, ConsoleConstants.ShowLeague) => new ConsoleCommand(ConsoleCommandKind.ShowLeague, text),
                _ when IsExact(text, ConsoleConstants.ShowSchedule) => new ConsoleCommand(ConsoleCommandKind.ShowSchedule, text),
                _ when IsExact(text, ConsoleConstants.ShowOpponent) => new ConsoleCommand(ConsoleCommandKind.ShowOpponent, text),
                _ when HasArgument(text, ConsoleConstants.ShowPlayer) => new ConsoleCommand(ConsoleCommandKind.ShowPlayer, text),
                _ when IsExact(text, ConsoleConstants.ShowPlayoffs) => new ConsoleCommand(ConsoleCommandKind.ShowPlayoffs, text),
                _ when IsExact(text, ConsoleConstants.ShowPlayoffPicture) => new ConsoleCommand(ConsoleCommandKind.ShowPlayoffs, text),
                _ when IsExact(text, ConsoleConstants.ShowChampions) => new ConsoleCommand(ConsoleCommandKind.ShowChampions, text),
                _ when HasArgument(text, "simulate rounds") => new ConsoleCommand(ConsoleCommandKind.SimulateRounds, text),
                _ when IsExact(text, "simulate rounds") => new ConsoleCommand(ConsoleCommandKind.SimulateRounds, text),
                _ when IsExact(text, "analyze rounds") => new ConsoleCommand(ConsoleCommandKind.AnalyzeRounds, text),
                _ when HasArgument(text, "analyze round") => new ConsoleCommand(ConsoleCommandKind.AnalyzeRound, text),
                _ when IsExact(text, ConsoleConstants.Help) => new ConsoleCommand(ConsoleCommandKind.Help, text),
                _ when IsExact(text, ConsoleConstants.ShowHelp) => new ConsoleCommand(ConsoleCommandKind.Help, text),
                _ => ConsoleCommand.Unknown(text)
            };
        }

        private static bool HasArgument(string text, string command) =>
            text.StartsWith($"{command} ", StringComparison.OrdinalIgnoreCase);

        private static bool IsExact(string text, string command) =>
            string.Equals(text, command, StringComparison.OrdinalIgnoreCase);
    }
}
