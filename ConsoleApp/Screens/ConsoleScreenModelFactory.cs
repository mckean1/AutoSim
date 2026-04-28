using ConsoleApp.Constants;
using ConsoleApp.Navigation;

namespace ConsoleApp.Screens
{
    /// <summary>
    /// Builds general console screen render models.
    /// </summary>
    internal sealed class ConsoleScreenModelFactory
    {
        /// <summary>
        /// Builds the welcome screen shown before a world is created.
        /// </summary>
        /// <param name="message">The optional status message.</param>
        /// <returns>The screen render model.</returns>
        public ScreenRenderModel BuildWelcomeScreen(string? message = null) =>
            new()
            {
                Commands = [ConsoleConstants.Start, ConsoleConstants.Help],
                ContentLines =
                [
                    "No game world has been created yet.",
                    "Enter `start` to create a new game.",
                    string.Empty,
                    "Start a new management career to generate the world, league, roster, and schedule."
                ],
                Header = new ScreenHeaderModel
                {
                    PrimaryLeft = "AutoSim",
                    PrimaryRight = "No World"
                },
                Message = message,
                Title = "Welcome to AutoSim"
            };

        /// <summary>
        /// Builds the new-game setup screen.
        /// </summary>
        /// <param name="state">The new-game setup state.</param>
        /// <param name="message">The optional status message.</param>
        /// <returns>The screen render model.</returns>
        public ScreenRenderModel BuildNewGameSetupScreen(NewGameSetupState state, string? message = null)
        {
            ArgumentNullException.ThrowIfNull(state);

            IReadOnlyList<string> commands = state.Step == NewGameSetupStep.CoachName
                ? [ConsoleConstants.Cancel, ConsoleConstants.Help]
                : [ConsoleConstants.Back, ConsoleConstants.Cancel, ConsoleConstants.Help];

            List<string> lines = state.Step == NewGameSetupStep.CoachName
                ?
                [
                    "Create Your Coach",
                    "Enter your coach name.",
                    string.Empty,
                    $"Coach Name: {state.CoachName ?? "_"}",
                    string.Empty,
                    "This name will be used for the human-controlled coach."
                ]
                :
                [
                    "Create Your Team",
                    $"Coach Name: {state.CoachName}",
                    string.Empty,
                    "Enter your team name.",
                    string.Empty,
                    $"Team Name: {state.TeamName ?? "_"}",
                    string.Empty,
                    "Your team will start in the Amateur Tier.",
                    "Region and division will be randomized."
                ];

            return new ScreenRenderModel
            {
                Commands = commands,
                ContentLines = lines,
                Header = new ScreenHeaderModel
                {
                    PrimaryLeft = "AutoSim",
                    PrimaryRight = "New Game Setup"
                },
                Message = message,
                Title = "New Game Setup"
            };
        }

        /// <summary>
        /// Builds the command reference screen.
        /// </summary>
        /// <param name="header">The screen header.</param>
        /// <param name="contextScreen">The screen that opened help.</param>
        /// <param name="contextCommands">The commands available in the previous context.</param>
        /// <param name="message">The optional status message.</param>
        /// <returns>The screen render model.</returns>
        public ScreenRenderModel BuildHelpScreen(
            ScreenHeaderModel header,
            ScreenKind contextScreen,
            IReadOnlyList<string> contextCommands,
            string? message = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(contextCommands);

            List<string> lines =
            [
                "General",
                "start                 Begin new game setup.",
                "home                  Return to the command hub.",
                "back                  Return to the previous screen.",
                "help                  Show this command reference.",
                string.Empty,
                "Setup",
                "type a name           Submit the requested setup field.",
                "cancel                Cancel new game setup.",
                "back                  Return to the previous setup step.",
                string.Empty,
                "Management",
                "show team             View your team.",
                "show league           View league standings.",
                "show schedule         View the current week schedule.",
                "show playoffs         View playoff picture information.",
                string.Empty,
                "Team / Player",
                "show team <team name> View a specific team.",
                "show opponent         View the relevant opponent.",
                "show player <name>    View player details.",
                string.Empty,
                "Champions",
                "show champions        View the champion catalog.",
                "show champion <name>  View champion details.",
                "filter role <role>    Filter champions by role.",
                "clear filter          Clear the champion filter.",
                string.Empty,
                "Match",
                "start match           Open the current match preview.",
                "continue              Advance the active match flow.",
                "cancel                Cancel the active preview/draft.",
                "view last match       Review the last completed match.",
                "view rounds           View completed match rounds.",
                "view round <number>   View a specific round.",
                "view replay           Open replay review.",
                string.Empty,
                "Replay",
                "step                  Advance one replay step.",
                "play                  Start live replay playback.",
                "pause                 Pause live replay playback.",
                "skip                  Skip to the next replay summary.",
                "faster                Increase replay playback speed.",
                "slower                Decrease replay playback speed.",
                "next page             Move replay review forward.",
                "previous page         Move replay review backward.",
                string.Empty,
                $"Current context: {GetScreenTitle(contextScreen)}",
                $"Context commands: {string.Join(" | ", contextCommands)}"
            ];

            IReadOnlyList<string> commands = contextScreen == ScreenKind.NewGameSetup
                ? [ConsoleConstants.Back, ConsoleConstants.Cancel, ConsoleConstants.Help]
                : [ConsoleConstants.Home, ConsoleConstants.Back, ConsoleConstants.Help];

            return new ScreenRenderModel
            {
                Commands = commands,
                ContentLines = lines,
                Header = header,
                Message = message,
                Title = "Help"
            };
        }

        private static string GetScreenTitle(ScreenKind screenKind) =>
            screenKind switch
            {
                ScreenKind.Home => "Home",
                ScreenKind.Team => "Team Detail",
                ScreenKind.PlayerDetail => "Player Detail",
                ScreenKind.League => "League",
                ScreenKind.Playoffs => "Playoff Picture",
                ScreenKind.Schedule => "Schedule",
                ScreenKind.MatchPreview => "Match Preview",
                ScreenKind.ReplayPreparation => "Replay Preparation",
                ScreenKind.Draft => "Draft",
                ScreenKind.DraftSummary => "Draft Summary",
                ScreenKind.LiveReplay => "Live Replay",
                ScreenKind.RoundSummary => "Round Summary",
                ScreenKind.MatchSummary => "Match Summary",
                ScreenKind.ChampionCatalog => "Champion Catalog",
                ScreenKind.ChampionDetail => "Champion Detail",
                ScreenKind.LastMatchReview => "Last Match Review",
                ScreenKind.RoundList => "Round List",
                ScreenKind.RoundReview => "Round Review",
                ScreenKind.ReplayReview => "Replay Review",
                ScreenKind.Help => "Help",
                ScreenKind.NewGameSetup => "New Game Setup",
                _ => screenKind.ToString()
            };
    }
}
