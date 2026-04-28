using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Interfaces;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Management.Services;
using AutoSim.Domain.Objects;
using ConsoleApp.Constants;
using ConsoleApp.Navigation;
using ConsoleApp.Presentation;
using ConsoleApp.Screens;
using ConsoleApp.Services;

namespace ConsoleApp.Objects
{
    /// <summary>
    /// Represents the interactive console application loop.
    /// </summary>
    public sealed class ConsoleApplication
    {
        private const int TeamRosterSize = 5;
        private const int RequiredFighterCount = 4;
        private const int RequiredMageCount = 2;
        private const int RequiredMarksmanCount = 2;
        private const int RequiredSupportCount = 2;

        private string _command;
        private string _previousCommand;
        private bool _isProcessingCommand;
        private readonly RoundAnalysisRenderer _roundAnalysisRenderer;
        private readonly AggregateRoundAnalysisRenderer _aggregateRoundAnalysisRenderer;
        private readonly AggregateRoundAnalyzer _aggregateRoundAnalyzer;
        private readonly string _logDirectory;
        private readonly RoundLogAnalyzer _roundLogAnalyzer;
        private readonly RoundLogReader _roundLogReader;
        private readonly RoundLogWriter _roundLogWriter;
        private readonly RoundReportWriter _roundReportWriter;
        private readonly MatchPresentationState _matchPresentationState;
        private readonly ReplayPresenter _replayPresenter;
        private readonly RoundDraftValidator _roundDraftValidator;
        private readonly DeterministicRoundDraftService _roundDraftService;
        private readonly ScreenNavigationState _screenNavigationState;
        private readonly ConsoleScreenRenderer _screenRenderer;
        private readonly SeasonProgressionService _seasonProgressionService;
        private readonly Func<int> _seedProvider;
        private readonly WorldGenerationService _worldGenerationService;
        private ScheduledMatch? _pendingMatch;
        private ScreenRenderModel _currentScreenModel;
        private WorldState? _world;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleApplication"/> class.
        /// </summary>
        public ConsoleApplication(
            string logDirectory = "logs/rounds",
            Func<int>? seedProvider = null,
            IMatchEngineWrapper? matchEngineWrapper = null)
        {
            _command = string.Empty;
            _previousCommand = string.Empty;
            _isProcessingCommand = false;
            _logDirectory = logDirectory;
            _roundAnalysisRenderer = new RoundAnalysisRenderer();
            _aggregateRoundAnalysisRenderer = new AggregateRoundAnalysisRenderer();
            _aggregateRoundAnalyzer = new AggregateRoundAnalyzer();
            _roundLogAnalyzer = new RoundLogAnalyzer();
            _roundLogReader = new RoundLogReader();
            _roundLogWriter = new RoundLogWriter(logDirectory);
            _roundReportWriter = new RoundReportWriter(logDirectory);
            _matchPresentationState = new MatchPresentationState();
            _replayPresenter = new ReplayPresenter();
            _roundDraftValidator = new RoundDraftValidator();
            _roundDraftService = new DeterministicRoundDraftService();
            _screenNavigationState = new ScreenNavigationState();
            _screenRenderer = new ConsoleScreenRenderer();
            _seasonProgressionService = new SeasonProgressionService(matchEngineWrapper);
            _seedProvider = seedProvider ?? (() => Environment.TickCount);
            _worldGenerationService = new WorldGenerationService();
            _currentScreenModel = BuildWelcomeScreen();
        }

        /// <summary>
        /// Runs the interactive console loop.
        /// </summary>
        public void Run()
        {
            _screenRenderer.Render(_currentScreenModel);

            while (true)
            {
                RenderPrompt();
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        Redraw();
                        ProcessCommand();
                        break;
                    case ConsoleKey.Backspace:
                        HandleBackspace();
                        break;
                    default:
                        HandleCharacterInput(key);
                        break;
                }
            }
        }

        /// <summary>
        /// Processes the current command entered by the user.
        /// </summary>
        private void ProcessCommand()
        {
            _isProcessingCommand = true;

            try
            {
                if (string.IsNullOrWhiteSpace(_command))
                {
                    _command = _previousCommand;
                }

                if (IsExitCommand())
                {
                    _currentScreenModel = BuildCurrentScreen("Exiting the application.");
                    _screenRenderer.Render(_currentScreenModel);
                    Environment.Exit(0);
                }

                ExecuteCommand(_command);
                _screenRenderer.Render(_currentScreenModel);

                _previousCommand = _command;
            }
            finally
            {
                _isProcessingCommand = false;
                _command = string.Empty;
                ClearPendingInput();
            }
        }

        /// <summary>
        /// Executes a command and returns console output.
        /// </summary>
        /// <param name="command">The command text.</param>
        /// <returns>The command output.</returns>
        public string ExecuteCommand(string command)
        {
            _command = command ?? string.Empty;
            string normalizedCommand = _command.Trim();

            if (IsStartGameCommand())
            {
                return StartGame();
            }

            if (IsHomeCommand())
            {
                return RenderHome();
            }

            if (IsStartMatchCommand())
            {
                return RenderMatchPreview();
            }

            if (IsContinueCommand())
            {
                return ContinueMatchFlow();
            }

            if (IsAutoDraftCommand())
            {
                return AutoDraft();
            }

            if (IsStepCommand())
            {
                return StepReplay();
            }

            if (IsSkipCommand())
            {
                return SkipReplay();
            }

            if (IsPlayCommand() || IsPauseCommand() || IsFasterCommand() || IsSlowerCommand() || IsDetailsCommand())
            {
                return RenderCurrentScreen($"{normalizedCommand} is a replay placeholder for now. Use step or skip.");
            }

            if (IsDraftPlaceholderCommand())
            {
                return RenderCurrentScreen($"{normalizedCommand} is not implemented yet. Use auto draft for this MVP.");
            }

            if (IsQuitReplayCommand())
            {
                return RenderMatchSummary("Replay closed.");
            }

            if (IsNextRoundCommand())
            {
                return NextRound();
            }

            if (IsViewReplayCommand())
            {
                return ViewReplay();
            }

            if (IsMatchSummaryCommand())
            {
                return RenderMatchSummary();
            }

            if (IsViewRoundsCommand())
            {
                return ViewRounds();
            }

            if (IsCancelCommand())
            {
                if (_screenNavigationState.CurrentScreen is ScreenKind.LiveReplay
                    or ScreenKind.RoundSummary
                    or ScreenKind.MatchSummary)
                {
                    return RenderCurrentScreen("The match has already started. Use continue or match summary.");
                }

                _pendingMatch = null;
                _matchPresentationState.Clear();
                return RenderHome("Match preview cancelled.");
            }

            if (IsShowTeamCommand())
            {
                return ShowTeam();
            }

            if (IsShowLeagueCommand())
            {
                return ShowLeague();
            }

            if (IsShowScheduleCommand())
            {
                return ShowSchedule();
            }

            if (IsShowOpponentCommand())
            {
                return ShowOpponent();
            }

            if (IsShowChampionsCommand())
            {
                return ShowChampions();
            }

            if (IsSimulateRoundsCommand())
            {
                return SimulateRounds();
            }

            if (IsAnalyzeRoundsCommand())
            {
                return AnalyzeRounds();
            }

            if (IsAnalyzeRoundCommand())
            {
                return AnalyzeRound();
            }

            if (IsHelpCommand())
            {
                return RenderHelp();
            }

            return RenderCurrentScreen($"Unknown command: {normalizedCommand}");
        }

        private void HandleBackspace()
        {
            if (_isProcessingCommand)
            {
                return;
            }

            if (_command.Length > 0)
            {
                _command = _command[..^1];
            }
        }

        private void HandleCharacterInput(ConsoleKeyInfo key)
        {
            if (_isProcessingCommand)
            {
                return;
            }

            if (!char.IsControl(key.KeyChar))
            {
                _command += key.KeyChar;
            }
        }

        /// <summary>
        /// Clears and redraws the current prompt and command text.
        /// </summary>
        private void RenderPrompt()
        {
            _screenRenderer.Render(_currentScreenModel, _command);
        }

        private bool IsStartGameCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Start, StringComparison.OrdinalIgnoreCase);
        private bool IsStartMatchCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.StartMatch, StringComparison.OrdinalIgnoreCase);
        private bool IsAutoDraftCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.AutoDraft, StringComparison.OrdinalIgnoreCase);
        private bool IsCancelCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Cancel, StringComparison.OrdinalIgnoreCase);
        private bool IsContinueCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Continue, StringComparison.OrdinalIgnoreCase);
        private bool IsDetailsCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Details, StringComparison.OrdinalIgnoreCase);
        private bool IsFasterCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Faster, StringComparison.OrdinalIgnoreCase);
        private bool IsHomeCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Home, StringComparison.OrdinalIgnoreCase);
        private bool IsMatchSummaryCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.MatchSummary, StringComparison.OrdinalIgnoreCase);
        private bool IsNextRoundCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.NextRound, StringComparison.OrdinalIgnoreCase);
        private bool IsPauseCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Pause, StringComparison.OrdinalIgnoreCase);
        private bool IsPlayCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Play, StringComparison.OrdinalIgnoreCase);
        private bool IsQuitReplayCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.QuitReplay, StringComparison.OrdinalIgnoreCase);
        private bool IsShowChampionsCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ShowChampions, StringComparison.OrdinalIgnoreCase);
        private bool IsShowLeagueCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ShowLeague, StringComparison.OrdinalIgnoreCase);
        private bool IsShowOpponentCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ShowOpponent, StringComparison.OrdinalIgnoreCase);
        private bool IsShowScheduleCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ShowSchedule, StringComparison.OrdinalIgnoreCase);
        private bool IsShowTeamCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ShowTeam, StringComparison.OrdinalIgnoreCase);
        private bool IsSkipCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Skip, StringComparison.OrdinalIgnoreCase);
        private bool IsSlowerCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Slower, StringComparison.OrdinalIgnoreCase);
        private bool IsStepCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Step, StringComparison.OrdinalIgnoreCase);
        private bool IsViewReplayCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ViewReplay, StringComparison.OrdinalIgnoreCase);
        private bool IsViewRoundsCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ViewRounds, StringComparison.OrdinalIgnoreCase);
        private bool IsAnalyzeRoundCommand() =>
            _command.StartsWith("analyze round ", StringComparison.OrdinalIgnoreCase);
        private bool IsAnalyzeRoundsCommand() =>
            string.Equals(_command, "analyze rounds", StringComparison.OrdinalIgnoreCase);
        private bool IsSimulateRoundsCommand() =>
            _command.StartsWith("simulate rounds", StringComparison.OrdinalIgnoreCase);
        private bool IsHelpCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Help, StringComparison.OrdinalIgnoreCase);
        private bool IsExitCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Exit, StringComparison.OrdinalIgnoreCase);
        private bool IsDraftPlaceholderCommand() =>
            _command.Trim().StartsWith("pick ", StringComparison.OrdinalIgnoreCase)
            || _command.Trim().StartsWith("ban ", StringComparison.OrdinalIgnoreCase);
        private void Redraw() => _screenRenderer.Render(_currentScreenModel);

        private string ContinueMatchFlow()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No game has been started. Use start first.");
            }

            return _screenNavigationState.CurrentScreen switch
            {
                ScreenKind.MatchPreview => RenderDraft(),
                ScreenKind.Draft => AutoDraft(),
                ScreenKind.DraftSummary => StartLiveReplay(),
                ScreenKind.LiveReplay => StepReplay(),
                ScreenKind.RoundSummary => NextRound(),
                ScreenKind.MatchSummary => CompleteMatchFlow(),
                _ => RenderCurrentScreen("No match flow is active. Use start match first.")
            };
        }

        private string AutoDraft()
        {
            if (_world is null || _matchPresentationState.ScheduledMatch is null)
            {
                return RenderCurrentScreen("No active draft. Use start match first.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            RoundDraft draft = CreateRoundDraft(_world, league, _matchPresentationState.ScheduledMatch, roundNumber: 1);
            _roundDraftValidator.Validate(draft, ChampionCatalog.GetDefaultChampions());
            _matchPresentationState.RoundDraft = draft;
            _matchPresentationState.IsDraftComplete = true;
            _screenNavigationState.CurrentScreen = ScreenKind.DraftSummary;
            _currentScreenModel = BuildDraftSummaryScreen(_world, humanTeam, league, _matchPresentationState.ScheduledMatch, draft);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string StartLiveReplay()
        {
            if (_world is null || _matchPresentationState.ScheduledMatch is null)
            {
                return RenderCurrentScreen("No active match. Use start match first.");
            }

            Team humanTeam = GetHumanTeam(_world);
            int resolvedWeek = _world.Season.CurrentWeek;
            SeasonProgressionResult result = _seasonProgressionService.ResolveCurrentWeek(_world);
            _world = result.World;
            MatchResult? humanResult = result.MatchResults
                .FirstOrDefault(matchResult => IsHumanMatch(matchResult, humanTeam.Id, _world));

            if (humanResult is null)
            {
                _matchPresentationState.Clear();
                _pendingMatch = null;
                return RenderHome($"Resolved week {resolvedWeek}: Your team did not have a scheduled match.");
            }

            _matchPresentationState.PresentedMatch = _replayPresenter.Present(
                humanResult,
                ChampionCatalog.GetDefaultChampions(),
                teamId => FormatTeamName(_world, teamId));
            _matchPresentationState.RoundIndex = 0;
            _matchPresentationState.ReplayIndex = 1;
            _pendingMatch = null;

            League league = GetTeamLeague(_world, GetHumanTeam(_world));
            _screenNavigationState.CurrentScreen = ScreenKind.LiveReplay;
            _currentScreenModel = BuildLiveReplayScreen(_world, GetHumanTeam(_world), league, _matchPresentationState);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string StepReplay()
        {
            if (_world is null || _matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No active replay.");
            }

            PresentedRound round = _matchPresentationState.PresentedMatch.Rounds[_matchPresentationState.RoundIndex];
            if (_matchPresentationState.ReplayIndex < round.Messages.Count)
            {
                _matchPresentationState.ReplayIndex++;
            }
            else
            {
                return RenderRoundSummary("Round replay complete.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            _screenNavigationState.CurrentScreen = ScreenKind.LiveReplay;
            _currentScreenModel = BuildLiveReplayScreen(_world, humanTeam, league, _matchPresentationState);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string SkipReplay()
        {
            if (_matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No active replay.");
            }

            _matchPresentationState.ReplayIndex = _matchPresentationState.PresentedMatch
                .Rounds[_matchPresentationState.RoundIndex]
                .Messages
                .Count;
            return RenderRoundSummary("Replay skipped to the round result.");
        }

        private string NextRound()
        {
            if (_matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No round summaries are available.");
            }

            if (_matchPresentationState.RoundIndex + 1 >= _matchPresentationState.PresentedMatch.Rounds.Count)
            {
                return RenderMatchSummary();
            }

            _matchPresentationState.RoundIndex++;
            _matchPresentationState.ReplayIndex = 1;
            return ViewReplay();
        }

        private string ViewReplay()
        {
            if (_world is null || _matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No replay is available.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            _screenNavigationState.CurrentScreen = ScreenKind.LiveReplay;
            _currentScreenModel = BuildLiveReplayScreen(_world, humanTeam, league, _matchPresentationState);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ViewRounds()
        {
            if (_matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No round summaries are available.");
            }

            _matchPresentationState.RoundIndex = 0;
            return RenderRoundSummary();
        }

        private string CompleteMatchFlow()
        {
            _matchPresentationState.Clear();
            return RenderHome("Match flow complete.");
        }

        private string RenderRoundSummary(string? message = null)
        {
            if (_world is null || _matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No round summary is available.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            _screenNavigationState.CurrentScreen = ScreenKind.RoundSummary;
            _currentScreenModel = BuildRoundSummaryScreen(_world, humanTeam, league, _matchPresentationState, message);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string RenderMatchSummary(string? message = null)
        {
            if (_world is null || _matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No match summary is available.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            _screenNavigationState.CurrentScreen = ScreenKind.MatchSummary;
            _currentScreenModel = BuildMatchSummaryScreen(_world, humanTeam, league, _matchPresentationState.PresentedMatch, message);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private static void ClearPendingInput()
        {
            try
            {
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(intercept: true);
                }
            }
            catch (InvalidOperationException)
            {
                // KeyAvailable throws when input is redirected, such as during test runs.
            }
        }

        private string StartGame(string? coachName = null, string? teamName = null)
        {
            int seed = _seedProvider();
            _world = _worldGenerationService.CreateWorld(seed, coachName, teamName);
            _pendingMatch = null;
            _screenNavigationState.CurrentScreen = ScreenKind.Home;
            return RenderHome($"New game started. Seed: {seed}");
        }

        private string ShowLeague()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No game has been started. Use start first.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            _screenNavigationState.CurrentScreen = ScreenKind.League;
            _currentScreenModel = BuildLeagueScreen(_world, humanTeam, league);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ShowTeam()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No game has been started. Use start first.");
            }

            Team team = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, team);
            _screenNavigationState.CurrentScreen = ScreenKind.Team;
            _currentScreenModel = BuildTeamScreen(_world, team, league);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ShowSchedule()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No game has been started. Use start first.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            _screenNavigationState.CurrentScreen = ScreenKind.Schedule;
            _currentScreenModel = BuildScheduleScreen(_world, humanTeam, league);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ShowOpponent()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No game has been started. Use start first.");
            }

            ScheduledMatch? activeMatch = _pendingMatch ?? _matchPresentationState.ScheduledMatch;
            if (activeMatch is null)
            {
                return RenderCurrentScreen("No match preview is active.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            string opponentId = string.Equals(activeMatch.HomeTeamId, humanTeam.Id, StringComparison.Ordinal)
                ? activeMatch.AwayTeamId
                : activeMatch.HomeTeamId;
            Team opponent = league.Teams.First(team => string.Equals(team.Id, opponentId, StringComparison.Ordinal));
            _screenNavigationState.CurrentScreen = ScreenKind.MatchPreview;
            _currentScreenModel = BuildTeamScreen(_world, opponent, league, $"Opponent: {opponent.Name}");
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ShowChampions()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No game has been started. Use start first.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            IReadOnlyList<string> championNames = ChampionCatalog.GetDefaultChampions()
                .OrderBy(champion => champion.Role)
                .ThenBy(champion => champion.Name, StringComparer.Ordinal)
                .Select(champion => $"{champion.Role,-9} {champion.Name}")
                .ToList();
            _currentScreenModel = new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Continue,
                    ConsoleConstants.AutoDraft,
                    ConsoleConstants.Cancel,
                    ConsoleConstants.Help
                ],
                ContentLines =
                [
                    "Available champions",
                    "Role      Champion",
                    .. championNames.Take(24)
                ],
                Header = BuildHeader(_world, humanTeam, league),
                Title = "Champions"
            };
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string RenderHome(string? message = null)
        {
            if (_world is null)
            {
                _screenNavigationState.CurrentScreen = ScreenKind.Home;
                _currentScreenModel = BuildWelcomeScreen(message);
                return _screenRenderer.RenderToString(_currentScreenModel);
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            _screenNavigationState.CurrentScreen = ScreenKind.Home;
            _currentScreenModel = BuildHomeScreen(_world, humanTeam, league, message);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string RenderMatchPreview()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No game has been started. Use start first.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            ScheduledMatch? match = GetCurrentWeekHumanMatch(_world, humanTeam);
            if (match is null)
            {
                _pendingMatch = null;
                return RenderCurrentScreen("Your team does not have a scheduled match this week.");
            }

            _pendingMatch = match;
            _matchPresentationState.Clear();
            _matchPresentationState.ScheduledMatch = match;
            _screenNavigationState.CurrentScreen = ScreenKind.MatchPreview;
            _currentScreenModel = BuildMatchPreviewScreen(_world, humanTeam, league, match);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string RenderDraft(string? message = null)
        {
            if (_world is null || _matchPresentationState.ScheduledMatch is null)
            {
                return RenderCurrentScreen("No active match. Use start match first.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            RoundDraft draft = _matchPresentationState.RoundDraft
                ?? CreateRoundDraft(_world, league, _matchPresentationState.ScheduledMatch, roundNumber: 1);
            _matchPresentationState.RoundDraft = draft;
            _screenNavigationState.CurrentScreen = ScreenKind.Draft;
            _currentScreenModel = BuildDraftScreen(
                _world,
                humanTeam,
                league,
                _matchPresentationState.ScheduledMatch,
                draft,
                message);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string RenderHelp()
        {
            _currentScreenModel = BuildCurrentScreen(
                "Available commands are listed in the footer. Use start to begin a new game.");
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string RenderCurrentScreen(string? message = null)
        {
            _currentScreenModel = BuildCurrentScreen(message);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private ScreenRenderModel BuildCurrentScreen(string? message = null)
        {
            if (_world is null)
            {
                return BuildWelcomeScreen(message);
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            return _screenNavigationState.CurrentScreen switch
            {
                ScreenKind.Team => BuildTeamScreen(_world, humanTeam, league, message),
                ScreenKind.League => BuildLeagueScreen(_world, humanTeam, league, message),
                ScreenKind.Schedule => BuildScheduleScreen(_world, humanTeam, league, message),
                ScreenKind.MatchPreview when _pendingMatch is not null =>
                    BuildMatchPreviewScreen(_world, humanTeam, league, _pendingMatch, message),
                ScreenKind.Draft when _matchPresentationState.ScheduledMatch is not null
                    && _matchPresentationState.RoundDraft is not null =>
                    BuildDraftScreen(
                        _world,
                        humanTeam,
                        league,
                        _matchPresentationState.ScheduledMatch,
                        _matchPresentationState.RoundDraft,
                        message),
                ScreenKind.DraftSummary when _matchPresentationState.ScheduledMatch is not null
                    && _matchPresentationState.RoundDraft is not null =>
                    BuildDraftSummaryScreen(
                        _world,
                        humanTeam,
                        league,
                        _matchPresentationState.ScheduledMatch,
                        _matchPresentationState.RoundDraft,
                        message),
                ScreenKind.LiveReplay when _matchPresentationState.PresentedMatch is not null =>
                    BuildLiveReplayScreen(_world, humanTeam, league, _matchPresentationState, message),
                ScreenKind.RoundSummary when _matchPresentationState.PresentedMatch is not null =>
                    BuildRoundSummaryScreen(_world, humanTeam, league, _matchPresentationState, message),
                ScreenKind.MatchSummary when _matchPresentationState.PresentedMatch is not null =>
                    BuildMatchSummaryScreen(_world, humanTeam, league, _matchPresentationState.PresentedMatch, message),
                _ => BuildHomeScreen(_world, humanTeam, league, message)
            };
        }

        private static ScreenRenderModel BuildWelcomeScreen(string? message = null) =>
            new()
            {
                Commands = [ConsoleConstants.Start, ConsoleConstants.Help],
                ContentLines =
                [
                    "No active game.",
                    "Start a new management career to generate the world, league, roster, and schedule.",
                    string.Empty,
                    "Recommended action: start"
                ],
                Header = new ScreenHeaderModel
                {
                    PrimaryLeft = "AutoSim",
                    PrimaryRight = "No Active Game"
                },
                Message = message,
                Title = "Home"
            };

        private static ScreenRenderModel BuildHomeScreen(
            WorldState world,
            Team humanTeam,
            League league,
            string? message = null)
        {
            LeagueStanding standing = GetStanding(league, humanTeam.Id);
            Division division = GetDivision(league, humanTeam);
            ScheduledMatch? nextMatch = GetNextHumanMatch(world, humanTeam);

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.ShowTeam,
                    ConsoleConstants.ShowLeague,
                    ConsoleConstants.ShowSchedule,
                    ConsoleConstants.StartMatch,
                    ConsoleConstants.Help
                ],
                ContentLines =
                [
                    $"Next match: {FormatMatch(world, nextMatch)}",
                    string.Empty,
                    "Team snapshot",
                    $"  Coach: {GetHumanCoach(world).Name}",
                    $"  Team: {humanTeam.Name}",
                    $"  Record: {standing.MatchWins}-{standing.MatchLosses}",
                    $"  Points: {FormatPoints(standing.Points)}",
                    string.Empty,
                    $"Current league: {FormatLeagueName(league)}",
                    $"Division: {division.Name} Division",
                    string.Empty,
                    $"Recommended action: {GetRecommendedAction(world, humanTeam)}"
                ],
                Header = BuildHeader(world, humanTeam, league),
                Message = message,
                Title = "Home"
            };
        }

        private static ScreenRenderModel BuildTeamScreen(
            WorldState world,
            Team team,
            League league,
            string? message = null)
        {
            Coach coach = world.Coaches.First(currentCoach => string.Equals(currentCoach.Id, team.CoachId, StringComparison.Ordinal));
            Division division = GetDivision(league, team);
            IReadOnlyList<Player> players = world.Players
                .Where(player => team.PlayerIds.Contains(player.Id))
                .OrderBy(player => player.PositionRole)
                .ThenBy(player => player.Name, StringComparer.Ordinal)
                .ToList();

            List<string> lines =
            [
                $"Coach: {coach.Name}",
                $"Team: {team.Name}",
                $"League: {FormatLeagueName(league)}",
                $"Division: {division.Name} Division",
                string.Empty,
                "Roster",
                "Role      Player"
            ];
            lines.AddRange(players.Select(player => $"{player.PositionRole,-9} {player.Name}"));
            lines.Add(string.Empty);
            lines.Add($"Next match: {FormatMatch(world, GetNextHumanMatch(world, team))}");

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Home,
                    ConsoleConstants.ShowLeague,
                    ConsoleConstants.ShowSchedule,
                    ConsoleConstants.StartMatch,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = BuildHeader(world, team, league),
                Message = message,
                Title = "Team"
            };
        }

        private static ScreenRenderModel BuildLeagueScreen(
            WorldState world,
            Team humanTeam,
            League league,
            string? message = null)
        {
            Division division = GetDivision(league, humanTeam);
            IReadOnlyList<LeagueStanding> standings = GetStandingsForDivision(league, division);
            string standingsTitle = standings.Count > 0 ? $"{division.Name} Division Standings" : "Overall Standings";
            standings = standings.Count > 0 ? standings : league.Standings;

            List<string> lines =
            [
                $"League: {FormatLeagueName(league)}",
                $"Your record: {FormatRecord(GetStanding(league, humanTeam.Id))}",
                $"Your points: {FormatPoints(GetStanding(league, humanTeam.Id).Points)}",
                string.Empty,
                standingsTitle,
                "Rank  Team                         Record  Points"
            ];
            int rank = 1;
            lines.AddRange(standings.Select(standing =>
                $"{rank++,4}  {FormatTeamName(world, standing.TeamId),-27} {FormatRecord(standing),-7} {FormatPoints(standing.Points)}"));

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Home,
                    ConsoleConstants.ShowTeam,
                    ConsoleConstants.ShowSchedule,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = BuildHeader(world, humanTeam, league),
                Message = message,
                Title = "League"
            };
        }

        private static ScreenRenderModel BuildScheduleScreen(
            WorldState world,
            Team humanTeam,
            League league,
            string? message = null)
        {
            IReadOnlyList<ScheduledMatch> matches = league.Schedule
                .Where(match => match.Week == world.Season.CurrentWeek)
                .OrderBy(match => match.MatchType)
                .ThenBy(match => FormatTeamName(world, match.HomeTeamId), StringComparer.Ordinal)
                .ThenBy(match => FormatTeamName(world, match.AwayTeamId), StringComparer.Ordinal)
                .ToList();
            ScheduledMatch? humanMatch = matches.FirstOrDefault(match => IsTeamMatch(match, humanTeam.Id));

            List<string> lines =
            [
                $"Current week: {world.Season.CurrentWeek}",
                $"Human team match: {FormatMatch(world, humanMatch)}",
                string.Empty,
                "Scheduled matches",
                "Match                                      Status"
            ];
            lines.AddRange(matches
                .Take(12)
                .Select(match => $"{FormatMatch(world, match),-42} {FormatMatchStatus(match)}"));

            if (matches.Count > 12)
            {
                lines.Add($"... {matches.Count - 12} more matches");
            }

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Home,
                    ConsoleConstants.StartMatch,
                    ConsoleConstants.ShowTeam,
                    ConsoleConstants.ShowLeague,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = BuildHeader(world, humanTeam, league),
                Message = message,
                Title = "Schedule"
            };
        }

        private static ScreenRenderModel BuildMatchPreviewScreen(
            WorldState world,
            Team humanTeam,
            League league,
            ScheduledMatch match,
            string? message = null)
        {
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
                Header = BuildHeader(world, humanTeam, league),
                Message = message,
                Title = "Match Preview"
            };
        }

        private static ScreenRenderModel BuildDraftScreen(
            WorldState world,
            Team humanTeam,
            League league,
            ScheduledMatch match,
            RoundDraft draft,
            string? message = null) =>
            new()
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
                Header = BuildHeader(world, humanTeam, league),
                Message = message,
                Title = "Draft"
            };

        private static ScreenRenderModel BuildDraftSummaryScreen(
            WorldState world,
            Team humanTeam,
            League league,
            ScheduledMatch match,
            RoundDraft draft,
            string? message = null) =>
            new()
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
                Header = BuildHeader(world, humanTeam, league),
                Message = message,
                Title = "Draft Summary"
            };

        private static ScreenRenderModel BuildLiveReplayScreen(
            WorldState world,
            Team humanTeam,
            League league,
            MatchPresentationState state,
            string? message = null)
        {
            PresentedMatch presentedMatch = state.PresentedMatch!;
            PresentedRound round = presentedMatch.Rounds[state.RoundIndex];
            IReadOnlyList<ReplayMessage> visibleMessages = round.Messages
                .Take(Math.Max(1, state.ReplayIndex))
                .TakeLast(10)
                .ToList();
            ReplayMessage currentMessage = visibleMessages.Last();
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
                $"Round {round.Result.RoundNumber} | {FormatTimestamp(currentMessage.Timestamp)} / 05:00",
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
            lines.Add(state.ReplayIndex >= round.Messages.Count
                ? "Playback state: round complete"
                : "Playback state: paused");

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Step,
                    ConsoleConstants.Play,
                    ConsoleConstants.Pause,
                    ConsoleConstants.Skip,
                    ConsoleConstants.Faster,
                    ConsoleConstants.Slower,
                    ConsoleConstants.Details,
                    ConsoleConstants.QuitReplay,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = BuildHeader(world, humanTeam, league),
                Message = message,
                Title = "Live Replay"
            };
        }

        private static ScreenRenderModel BuildRoundSummaryScreen(
            WorldState world,
            Team humanTeam,
            League league,
            MatchPresentationState state,
            string? message = null)
        {
            PresentedRound round = state.PresentedMatch!.Rounds[state.RoundIndex];
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
                    ConsoleConstants.Continue,
                    ConsoleConstants.MatchSummary,
                    ConsoleConstants.Help
                ],
                ContentLines =
                [
                    $"Round winner: {FormatTeamName(world, round.Result.WinningTeamId)}",
                    $"Final round score: {FormatTeamName(world, round.Result.WinningTeamId)} wins",
                    "Duration: 05:00",
                    $"Blue team score: {FormatTeamName(world, round.Result.BlueTeamId)}",
                    $"Red team score: {FormatTeamName(world, round.Result.RedTeamId)}",
                    string.Empty,
                    "Champion stats",
                    .. FormatRoundChampionSummary(world, round.Result),
                    string.Empty,
                    "Key moments",
                    .. keyMoments.Select(FormatReplayMessage)
                ],
                Header = BuildHeader(world, humanTeam, league),
                Message = message,
                Title = "Round Summary"
            };
        }

        private static ScreenRenderModel BuildMatchSummaryScreen(
            WorldState world,
            Team humanTeam,
            League league,
            PresentedMatch presentedMatch,
            string? message = null)
        {
            MatchResult result = presentedMatch.Result;
            LeagueStanding standing = GetStanding(league, humanTeam.Id);

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Continue,
                    ConsoleConstants.ViewRounds,
                    ConsoleConstants.ShowLeague,
                    ConsoleConstants.ShowTeam,
                    ConsoleConstants.Home,
                    ConsoleConstants.Help
                ],
                ContentLines =
                [
                    $"Match winner: {FormatTeamName(world, result.WinningTeamId)}",
                    $"Final match score: {result.BlueRoundWins}-{result.RedRoundWins}",
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
                Header = BuildHeader(world, humanTeam, league),
                Message = message,
                Title = "Match Summary"
            };
        }

        private string AnalyzeRound()
        {
            string logPath = _command["analyze round ".Length..].Trim();

            try
            {
                IReadOnlyList<RoundEvent> events = _roundLogReader.ReadEvents(logPath);
                RoundAnalysis analysis = _roundLogAnalyzer.Analyze(events);
                return _roundAnalysisRenderer.Render(logPath, analysis);
            }
            catch (RoundLogReadException exception)
            {
                return exception.Message + Environment.NewLine;
            }
        }

        private string SimulateRounds()
        {
            string value = _command["simulate rounds".Length..].Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Usage: simulate rounds <number>" + Environment.NewLine;
            }

            if (!int.TryParse(value, out int count))
            {
                return "Round count must be a positive whole number." + Environment.NewLine;
            }

            if (count <= 0)
            {
                return "Round count must be greater than zero." + Environment.NewLine;
            }

            int baseSeed = _seedProvider();
            AggregateRoundAnalysis analysis = new RoundBatchSimulator(_roundLogWriter).Simulate(count, baseSeed);
            string report = _aggregateRoundAnalysisRenderer.Render("Aggregate Results", _logDirectory, analysis);
            string reportPath = _roundReportWriter.WriteReport("simulation_summary", report);
            return $"Simulated {count} rounds." + Environment.NewLine
                + $"Logs written to: {_logDirectory}" + Environment.NewLine
                + $"Aggregate report written to: {reportPath}" + Environment.NewLine
                + "Analyze all logs with:" + Environment.NewLine
                + "analyze rounds" + Environment.NewLine;
        }

        private string AnalyzeRounds()
        {
            if (!Directory.Exists(_logDirectory))
            {
                return $"Round log folder was not found: {_logDirectory}" + Environment.NewLine;
            }

            IReadOnlyList<string> logPaths = Directory.GetFiles(_logDirectory, "*.jsonl").OrderBy(path => path).ToList();
            if (logPaths.Count == 0)
            {
                return $"No round logs found in {_logDirectory}." + Environment.NewLine;
            }

            List<RoundAnalysis> analyses = [];
            List<string> skippedLogs = [];
            foreach (string logPath in logPaths)
            {
                try
                {
                    IReadOnlyList<RoundEvent> events = _roundLogReader.ReadEvents(logPath);
                    if (events.Count == 0)
                    {
                        skippedLogs.Add($"{Path.GetFileName(logPath)}: No events found.");
                        continue;
                    }

                    analyses.Add(_roundLogAnalyzer.Analyze(events));
                }
                catch (RoundLogReadException exception)
                {
                    skippedLogs.Add($"{Path.GetFileName(logPath)}: {exception.Message}");
                }
            }

            if (analyses.Count == 0)
            {
                AggregateRoundAnalysis emptyAnalysis = _aggregateRoundAnalyzer.Analyze([], logPaths.Count, skippedLogs);
                string emptyReport = _aggregateRoundAnalysisRenderer.Render(
                    "Aggregate Round Analysis",
                    _logDirectory,
                    emptyAnalysis);
                string emptyReportPath = _roundReportWriter.WriteReport("aggregate_round_analysis", emptyReport);
                return "No valid round logs were found." + Environment.NewLine
                    + $"Aggregate report written to: {emptyReportPath}" + Environment.NewLine;
            }

            AggregateRoundAnalysis analysis = _aggregateRoundAnalyzer.Analyze(analyses, logPaths.Count, skippedLogs);
            string report = _aggregateRoundAnalysisRenderer.Render("Aggregate Round Analysis", _logDirectory, analysis);
            string reportPath = _roundReportWriter.WriteReport("aggregate_round_analysis", report);
            return $"Analyzed {analysis.RoundsAnalyzed} rounds." + Environment.NewLine
                + $"Aggregate report written to: {reportPath}" + Environment.NewLine;
        }

        /// <summary>
        /// Creates the deterministic temporary 5v5 roster used before draft exists.
        /// </summary>
        /// <param name="catalog">The available champion catalog.</param>
        /// <param name="seed">The deterministic round seed.</param>
        /// <returns>The selected temporary round roster.</returns>
        public static RoundRoster CreateTemporaryRoundRoster(IReadOnlyList<ChampionDefinition> catalog, int seed)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            if (catalog.Count < TeamRosterSize * 2)
            {
                throw new ArgumentException("Catalog must contain at least 10 champions to create a temporary round roster.", nameof(catalog));
            }

            List<ChampionDefinition> fighters = GetShuffledRoleChampions(
                catalog,
                ChampionRole.Fighter,
                RequiredFighterCount,
                seed);
            List<ChampionDefinition> mages = GetShuffledRoleChampions(catalog, ChampionRole.Mage, RequiredMageCount, seed);
            List<ChampionDefinition> marksmen = GetShuffledRoleChampions(
                catalog,
                ChampionRole.Marksman,
                RequiredMarksmanCount,
                seed);
            List<ChampionDefinition> supports = GetShuffledRoleChampions(
                catalog,
                ChampionRole.Support,
                RequiredSupportCount,
                seed);

            return new RoundRoster
            {
                BlueChampions = CreateRoleBalancedTeam(
                    fighters.Take(2),
                    mages.Take(1),
                    marksmen.Take(1),
                    supports.Take(1)),
                RedChampions = CreateRoleBalancedTeam(
                    fighters.Skip(2).Take(2),
                    mages.Skip(1).Take(1),
                    marksmen.Skip(1).Take(1),
                    supports.Skip(1).Take(1))
            };
        }

        private static Coach GetHumanCoach(WorldState world) =>
            world.Coaches.Single(coach => coach.IsHuman);

        private static Team GetHumanTeam(WorldState world)
        {
            Coach coach = GetHumanCoach(world);
            return world.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Teams)
                .Single(team => team.Id == coach.TeamId);
        }

        private static League GetTeamLeague(WorldState world, Team team) =>
            world.Tiers
                .SelectMany(tier => tier.Leagues)
                .Single(league => league.Id == team.LeagueId);

        private static ScreenHeaderModel BuildHeader(WorldState world, Team humanTeam, League league)
        {
            LeagueStanding standing = GetStanding(league, humanTeam.Id);
            return new ScreenHeaderModel
            {
                PrimaryLeft = "AutoSim",
                PrimaryRight = $"Week {world.Season.CurrentWeek} | {FormatLeagueName(league)}",
                SecondaryLeft = humanTeam.Name,
                SecondaryRight = $"Record {standing.MatchWins}-{standing.MatchLosses} | Points {FormatPoints(standing.Points)}"
            };
        }

        private static Division GetDivision(League league, Team team) =>
            league.Divisions.First(division => string.Equals(division.Id, team.DivisionId, StringComparison.Ordinal));

        private static LeagueStanding GetStanding(League league, string teamId) =>
            league.Standings.FirstOrDefault(standing => string.Equals(standing.TeamId, teamId, StringComparison.Ordinal))
            ?? new LeagueStanding
            {
                TeamId = teamId
            };

        private static IReadOnlyList<LeagueStanding> GetStandingsForDivision(League league, Division division)
        {
            HashSet<string> divisionTeamIds = division.TeamIds.ToHashSet(StringComparer.Ordinal);
            return league.Standings
                .Where(standing => divisionTeamIds.Contains(standing.TeamId))
                .OrderByDescending(standing => standing.MatchWins)
                .ThenByDescending(standing => standing.Points)
                .ThenBy(standing => FormatTeamName(league, standing.TeamId), StringComparer.Ordinal)
                .ToList();
        }

        private RoundDraft CreateRoundDraft(WorldState world, League league, ScheduledMatch match, int roundNumber)
        {
            Team blueTeam = GetTeamById(league, match.HomeTeamId);
            Team redTeam = GetTeamById(league, match.AwayTeamId);
            IReadOnlyList<Player> bluePlayers = GetTeamPlayers(world.Players, blueTeam.Id);
            IReadOnlyList<Player> redPlayers = GetTeamPlayers(world.Players, redTeam.Id);

            return _roundDraftService.DraftRound(new RoundDraftContext
            {
                BlueCoach = GetCoachById(world, blueTeam.CoachId),
                BluePlayers = bluePlayers,
                BlueTeam = blueTeam,
                ChampionCatalog = ChampionCatalog.GetDefaultChampions(),
                Match = match,
                PreviousRounds = [],
                RedCoach = GetCoachById(world, redTeam.CoachId),
                RedPlayers = redPlayers,
                RedTeam = redTeam,
                RoundNumber = roundNumber,
                Seed = CreateRoundSeed(CreateMatchSeed(world.Seed, match.Id), roundNumber)
            });
        }

        private static Team GetTeamById(League league, string teamId) =>
            league.Teams.Single(team => string.Equals(team.Id, teamId, StringComparison.Ordinal));

        private static Coach GetCoachById(WorldState world, string coachId) =>
            world.Coaches.Single(coach => string.Equals(coach.Id, coachId, StringComparison.Ordinal));

        private static IReadOnlyList<Player> GetTeamPlayers(IReadOnlyList<Player> players, string teamId) =>
            players
                .Where(player => string.Equals(player.TeamId, teamId, StringComparison.Ordinal))
                .OrderBy(player => player.PositionRole)
                .ThenBy(player => player.Name, StringComparer.Ordinal)
                .ToList();

        private static int CreateRoundSeed(int matchSeed, int roundNumber) =>
            unchecked((matchSeed * 397) ^ roundNumber);

        private static int CreateMatchSeed(int worldSeed, string matchId)
        {
            int seed = worldSeed;
            foreach (char value in matchId)
            {
                seed = unchecked((seed * 397) ^ value);
            }

            return seed;
        }

        private static ScheduledMatch? GetCurrentWeekHumanMatch(WorldState world, Team humanTeam) =>
            GetTeamLeague(world, humanTeam).Schedule
                .Where(match => match.Week == world.Season.CurrentWeek)
                .Where(match => IsTeamMatch(match, humanTeam.Id))
                .OrderBy(match => match.MatchType)
                .ThenBy(match => match.Id, StringComparer.Ordinal)
                .FirstOrDefault();

        private static ScheduledMatch? GetNextHumanMatch(WorldState world, Team humanTeam) =>
            GetTeamLeague(world, humanTeam).Schedule
                .Where(match => match.Week >= world.Season.CurrentWeek)
                .Where(match => match.Result is null)
                .Where(match => IsTeamMatch(match, humanTeam.Id))
                .OrderBy(match => match.Week)
                .ThenBy(match => match.MatchType)
                .ThenBy(match => match.Id, StringComparer.Ordinal)
                .FirstOrDefault();

        private static bool IsTeamMatch(ScheduledMatch match, string teamId) =>
            string.Equals(match.HomeTeamId, teamId, StringComparison.Ordinal)
            || string.Equals(match.AwayTeamId, teamId, StringComparison.Ordinal);

        private static bool IsHumanMatch(MatchResult result, string humanTeamId, WorldState world)
        {
            ScheduledMatch? match = world.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Schedule)
                .FirstOrDefault(scheduledMatch => scheduledMatch.Id == result.MatchId);

            return match is not null
                && (string.Equals(match.HomeTeamId, humanTeamId, StringComparison.Ordinal)
                    || string.Equals(match.AwayTeamId, humanTeamId, StringComparison.Ordinal));
        }

        private static string FormatTeamName(WorldState world, string teamId) =>
            world.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Teams)
                .FirstOrDefault(team => team.Id == teamId)?.Name ?? teamId;

        private static string FormatTeamName(League league, string teamId) =>
            league.Teams.FirstOrDefault(team => team.Id == teamId)?.Name ?? teamId;

        private static string FormatLeagueName(League league) =>
            $"{league.TierName} {league.Region} League";

        private static string FormatMatchType(AutoSim.Domain.Enums.MatchType matchType) =>
            matchType == AutoSim.Domain.Enums.MatchType.RegularSeason ? "Regular Season" : matchType.ToString();

        private static string FormatMatch(WorldState world, ScheduledMatch? match) =>
            match is null
                ? "None scheduled"
                : $"Week {match.Week}: {FormatTeamName(world, match.HomeTeamId)} vs {FormatTeamName(world, match.AwayTeamId)}";

        private static string FormatMatchStatus(ScheduledMatch match) =>
            match.Result is null ? "Scheduled" : "Complete";

        private static string FormatPoints(int points) =>
            points >= 0 ? $"+{points}" : points.ToString();

        private static string FormatRecord(LeagueStanding standing) =>
            $"{standing.MatchWins}-{standing.MatchLosses}";

        private static string FormatHumanMarker(Team humanTeam, string teamId) =>
            string.Equals(humanTeam.Id, teamId, StringComparison.Ordinal) ? " (You)" : string.Empty;

        private static string GetRecommendedAction(WorldState world, Team humanTeam) =>
            GetCurrentWeekHumanMatch(world, humanTeam) is null ? ConsoleConstants.ShowSchedule : ConsoleConstants.StartMatch;

        private static string FormatMatchResultSummary(WorldState world, MatchResult result) =>
            $"{FormatTeamName(world, result.WinningTeamId)} won "
            + $"{result.BlueRoundWins}-{result.RedRoundWins} over "
            + $"{FormatTeamName(world, result.LosingTeamId)}.";

        private static IReadOnlyList<string> FormatLineup(IReadOnlyList<ChampionDefinition> champions)
        {
            string[] positions = ["Top", "Jungle", "Mid", "Bot", "Support"];
            return champions
                .Select((champion, index) => $"{positions.ElementAtOrDefault(index) ?? "Flex",-9} {champion.Name}")
                .ToList();
        }

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

        private static string RenderMatchResult(WorldState world, MatchResult result)
        {
            List<string> lines =
            [
                $"Match: {FormatTeamName(world, result.BlueTeamId)} vs {FormatTeamName(world, result.RedTeamId)}",
                $"Type: {result.MatchType}",
                $"Best of: {result.BestOf}",
                "Rounds:"
            ];
            lines.AddRange(result.RoundResults.Select(round =>
                $"  Round {round.RoundNumber}: {FormatTeamName(world, round.WinningTeamId)}"));
            lines.Add(
                $"Final score: {result.BlueRoundWins}-{result.RedRoundWins}");
            lines.Add($"Winner: {FormatTeamName(world, result.WinningTeamId)}");
            return string.Join(Environment.NewLine, lines);
        }

        private static List<ChampionDefinition> GetShuffledRoleChampions(
            IEnumerable<ChampionDefinition> catalog,
            ChampionRole role,
            int requiredCount,
            int seed)
        {
            List<ChampionDefinition> champions = catalog
                .Where(champion => champion.Role == role)
                .ToList();

            if (champions.Count < requiredCount)
            {
                throw new ArgumentException(
                    $"Catalog must contain at least {requiredCount} {role} champions to create a temporary round roster.",
                    nameof(catalog));
            }

            Shuffle(champions, seed + ((int)role * 397));
            return champions;
        }

        private static IReadOnlyList<ChampionDefinition> CreateRoleBalancedTeam(
            IEnumerable<ChampionDefinition> fighters,
            IEnumerable<ChampionDefinition> mages,
            IEnumerable<ChampionDefinition> marksmen,
            IEnumerable<ChampionDefinition> supports) =>
        [
            .. fighters,
            .. mages,
            .. marksmen,
            .. supports
        ];

        private static void Shuffle(IList<ChampionDefinition> champions, int seed)
        {
            Random rng = new(seed);
            for (int index = champions.Count - 1; index > 0; index--)
            {
                int swapIndex = rng.Next(index + 1);
                (champions[index], champions[swapIndex]) = (champions[swapIndex], champions[index]);
            }
        }
    }
}
