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
        private static readonly TimeSpan ReplayPollDelay = TimeSpan.FromMilliseconds(25);
        private DateTime _replayPlaybackStartedAtUtc;

        private string _command;
        private string _previousCommand;
        private bool _isProcessingCommand;
        private DateTime _nextReplayAdvanceAtUtc;
        private readonly RoundAnalysisRenderer _roundAnalysisRenderer;
        private readonly AggregateRoundAnalysisRenderer _aggregateRoundAnalysisRenderer;
        private readonly AggregateRoundAnalyzer _aggregateRoundAnalyzer;
        private readonly string _logDirectory;
        private readonly RoundLogAnalyzer _roundLogAnalyzer;
        private readonly RoundLogReader _roundLogReader;
        private readonly RoundLogWriter _roundLogWriter;
        private readonly RoundReportWriter _roundReportWriter;
        private readonly MatchPresentationState _matchPresentationState;
        private readonly MatchReviewFactory _matchReviewFactory;
        private readonly MatchReviewStore _matchReviewStore;
        private readonly ReplayReviewState _replayReviewState;
        private readonly ReplayPresenter _replayPresenter;
        private readonly RoundDraftValidator _roundDraftValidator;
        private readonly DeterministicRoundDraftService _roundDraftService;
        private readonly ScreenNavigationState _screenNavigationState;
        private readonly ConsoleScreenRenderer _screenRenderer;
        private readonly SeasonProgressionService _seasonProgressionService;
        private readonly Func<int> _seedProvider;
        private readonly WorldGenerationService _worldGenerationService;
        private ChampionDefinition? _selectedChampion;
        private ChampionRole? _championCatalogFilter;
        private ScreenKind _championCatalogBackScreen;
        private ScreenKind _championDetailBackScreen;
        private Player? _selectedPlayer;
        private Team? _selectedTeam;
        private IReadOnlyList<string> _helpContextCommands;
        private ScreenKind _helpContextScreen;
        private ScreenKind _reviewBackScreen;
        private int _selectedReviewRoundNumber;
        private ScheduledMatch? _pendingMatch;
        private ScreenRenderModel _currentScreenModel;
        private AppInputMode _inputMode;
        private NewGameSetupState? _newGameSetupState;
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
            _nextReplayAdvanceAtUtc = DateTime.MinValue;
            _replayPlaybackStartedAtUtc = DateTime.MinValue;
            _logDirectory = logDirectory;
            _roundAnalysisRenderer = new RoundAnalysisRenderer();
            _aggregateRoundAnalysisRenderer = new AggregateRoundAnalysisRenderer();
            _aggregateRoundAnalyzer = new AggregateRoundAnalyzer();
            _roundLogAnalyzer = new RoundLogAnalyzer();
            _roundLogReader = new RoundLogReader();
            _roundLogWriter = new RoundLogWriter(logDirectory);
            _roundReportWriter = new RoundReportWriter(logDirectory);
            _matchPresentationState = new MatchPresentationState();
            _matchReviewFactory = new MatchReviewFactory();
            _matchReviewStore = new MatchReviewStore();
            _replayReviewState = new ReplayReviewState();
            _replayPresenter = new ReplayPresenter();
            _roundDraftValidator = new RoundDraftValidator();
            _roundDraftService = new DeterministicRoundDraftService();
            _screenNavigationState = new ScreenNavigationState();
            _screenRenderer = new ConsoleScreenRenderer();
            _seasonProgressionService = new SeasonProgressionService(matchEngineWrapper);
            _seedProvider = seedProvider ?? (() => Environment.TickCount);
            _worldGenerationService = new WorldGenerationService();
            _championCatalogBackScreen = ScreenKind.Home;
            _championDetailBackScreen = ScreenKind.ChampionCatalog;
            _helpContextCommands = [ConsoleConstants.Start, ConsoleConstants.Help];
            _helpContextScreen = ScreenKind.Home;
            _reviewBackScreen = ScreenKind.Home;
            _selectedReviewRoundNumber = 1;
            _inputMode = AppInputMode.Command;
            _newGameSetupState = null;
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
                if (IsLiveReplayPlaying())
                {
                    HandleLiveReplayPlayback();
                    continue;
                }

                RenderPrompt();
                HandleInteractiveKey(Console.ReadKey(intercept: true), allowReplayHotkeys: false);
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
                if (_inputMode == AppInputMode.Command && string.IsNullOrWhiteSpace(_command))
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

            if (_inputMode == AppInputMode.NewGameSetup)
            {
                return HandleNewGameSetupInput(_command);
            }

            string normalizedCommand = _command.Trim();

            if (IsStartGameCommand())
            {
                return BeginNewGameSetup();
            }

            if (IsHomeCommand())
            {
                return RenderHome();
            }

            if (IsStartMatchCommand())
            {
                return RenderMatchPreview();
            }

            if (IsViewLastMatchCommand())
            {
                return ViewLastMatch();
            }

            if (IsViewRoundCommand())
            {
                return ViewRound();
            }

            if (IsNextPageCommand())
            {
                return NextReplayReviewPage();
            }

            if (IsPreviousPageCommand())
            {
                return PreviousReplayReviewPage();
            }

            if (IsPreviousRoundCommand())
            {
                return PreviousReviewRound();
            }

            if (IsBackCommand())
            {
                return Back();
            }

            if (IsClearFilterCommand())
            {
                return ClearChampionFilter();
            }

            if (IsFilterRoleCommand())
            {
                return FilterChampionRole();
            }

            if (IsShowChampionCommand())
            {
                return ShowChampion();
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

            if (IsPlayCommand())
            {
                return PlayReplay();
            }

            if (IsPauseCommand())
            {
                return PauseReplay();
            }

            if (IsFasterCommand())
            {
                return IncreaseReplaySpeed();
            }

            if (IsSlowerCommand())
            {
                return DecreaseReplaySpeed();
            }

            if (IsDetailsCommand())
            {
                return ShowReplayDetails();
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
                return MatchSummaryCommand();
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

            if (IsShowSpecificTeamCommand())
            {
                return ShowSpecificTeam();
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

            if (IsShowPlayerCommand())
            {
                return ShowPlayer();
            }

            if (IsShowPlayoffsCommand())
            {
                return ShowPlayoffs();
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
        private bool IsBackCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Back, StringComparison.OrdinalIgnoreCase);
        private bool IsCancelCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Cancel, StringComparison.OrdinalIgnoreCase);
        private bool IsClearFilterCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ClearFilter, StringComparison.OrdinalIgnoreCase);
        private bool IsContinueCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Continue, StringComparison.OrdinalIgnoreCase);
        private bool IsDetailsCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Details, StringComparison.OrdinalIgnoreCase);
        private bool IsFilterRoleCommand() =>
            _command.Trim().StartsWith($"{ConsoleConstants.FilterRole} ", StringComparison.OrdinalIgnoreCase);
        private bool IsFasterCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Faster, StringComparison.OrdinalIgnoreCase);
        private bool IsHomeCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Home, StringComparison.OrdinalIgnoreCase);
        private bool IsMatchSummaryCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.MatchSummary, StringComparison.OrdinalIgnoreCase);
        private bool IsNextRoundCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.NextRound, StringComparison.OrdinalIgnoreCase);
        private bool IsNextPageCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.NextPage, StringComparison.OrdinalIgnoreCase);
        private bool IsPauseCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Pause, StringComparison.OrdinalIgnoreCase);
        private bool IsPlayCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Play, StringComparison.OrdinalIgnoreCase);
        private bool IsPreviousPageCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.PreviousPage, StringComparison.OrdinalIgnoreCase);
        private bool IsPreviousRoundCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.PreviousRound, StringComparison.OrdinalIgnoreCase);
        private bool IsQuitReplayCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.QuitReplay, StringComparison.OrdinalIgnoreCase);
        private bool IsShowChampionsCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ShowChampions, StringComparison.OrdinalIgnoreCase);
        private bool IsShowChampionCommand() =>
            _command.Trim().StartsWith("show champion ", StringComparison.OrdinalIgnoreCase);
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
        private bool IsViewLastMatchCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ViewLastMatch, StringComparison.OrdinalIgnoreCase);
        private bool IsViewRoundCommand() =>
            _command.Trim().StartsWith("view round ", StringComparison.OrdinalIgnoreCase);
        private bool IsViewRoundsCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ViewRounds, StringComparison.OrdinalIgnoreCase);
        private bool IsAnalyzeRoundCommand() =>
            _command.StartsWith("analyze round ", StringComparison.OrdinalIgnoreCase);
        private bool IsAnalyzeRoundsCommand() =>
            string.Equals(_command, "analyze rounds", StringComparison.OrdinalIgnoreCase);
        private bool IsSimulateRoundsCommand() =>
            _command.StartsWith("simulate rounds", StringComparison.OrdinalIgnoreCase);
        private bool IsHelpCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Help, StringComparison.OrdinalIgnoreCase)
            || string.Equals(_command.Trim(), ConsoleConstants.ShowHelp, StringComparison.OrdinalIgnoreCase);
        private bool IsExitCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Exit, StringComparison.OrdinalIgnoreCase);
        private bool IsShowPlayerCommand() =>
            _command.Trim().StartsWith($"{ConsoleConstants.ShowPlayer} ", StringComparison.OrdinalIgnoreCase);
        private bool IsShowPlayoffsCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ShowPlayoffs, StringComparison.OrdinalIgnoreCase)
            || string.Equals(_command.Trim(), ConsoleConstants.ShowPlayoffPicture, StringComparison.OrdinalIgnoreCase);
        private bool IsShowSpecificTeamCommand() =>
            _command.Trim().StartsWith($"{ConsoleConstants.ShowTeam} ", StringComparison.OrdinalIgnoreCase);
        private bool IsDraftPlaceholderCommand() =>
            _command.Trim().StartsWith("pick ", StringComparison.OrdinalIgnoreCase)
            || _command.Trim().StartsWith("ban ", StringComparison.OrdinalIgnoreCase);
        private void Redraw() => _screenRenderer.Render(_currentScreenModel);

        private void HandleInteractiveKey(ConsoleKeyInfo key, bool allowReplayHotkeys)
        {
            if (allowReplayHotkeys && TryHandleReplayHotkey(key))
            {
                return;
            }

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

        private void HandleLiveReplayPlayback()
        {
            if (Console.KeyAvailable)
            {
                HandleInteractiveKey(Console.ReadKey(intercept: true), allowReplayHotkeys: true);
                return;
            }

            if (DateTime.UtcNow < _nextReplayAdvanceAtUtc)
            {
                Thread.Sleep(ReplayPollDelay);
                return;
            }

            AdvanceReplayPlayback();
        }

        private bool TryHandleReplayHotkey(ConsoleKeyInfo key)
        {
            string? command = key.Key switch
            {
                ConsoleKey.Spacebar => ConsoleConstants.Pause,
                ConsoleKey.Add or ConsoleKey.OemPlus => ConsoleConstants.Faster,
                ConsoleKey.Subtract or ConsoleKey.OemMinus => ConsoleConstants.Slower,
                _ => key.KeyChar switch
                {
                    'p' or 'P' => ConsoleConstants.Pause,
                    's' or 'S' => ConsoleConstants.Skip,
                    'q' or 'Q' => ConsoleConstants.QuitReplay,
                    '?' => ConsoleConstants.Help,
                    '+' => ConsoleConstants.Faster,
                    '-' => ConsoleConstants.Slower,
                    _ => null
                }
            };

            if (command is null)
            {
                return false;
            }

            string previousCommand = _command;
            _command = command;
            try
            {
                ProcessCommand();
            }
            finally
            {
                if (_inputMode == AppInputMode.Command)
                {
                    _command = previousCommand;
                }
            }

            return true;
        }

        private bool IsLiveReplayPlaying() =>
            _screenNavigationState.CurrentScreen == ScreenKind.LiveReplay
            && _matchPresentationState.PresentedMatch is not null
            && _matchPresentationState.LiveReplay.PlaybackState == ReplayPlaybackState.Playing;

        private string ContinueMatchFlow()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No world has been created yet. Use `start` to begin a new game.");
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
            _matchReviewStore.LastMatch = _matchReviewFactory.Create(
                _matchPresentationState.PresentedMatch,
                resolvedWeek,
                teamId => FormatTeamName(_world, teamId));
            _matchPresentationState.RoundIndex = 0;
            _matchPresentationState.LiveReplay.Reset();
            _matchPresentationState.LiveReplay.CurrentEventIndex = 1;
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

            _matchPresentationState.LiveReplay.PlaybackState = ReplayPlaybackState.Paused;
            if (!TryAdvanceReplay())
            {
                return TransitionAfterReplayComplete("Round replay complete.");
            }

            return ViewLiveReplay();
        }

        private string SkipReplay()
        {
            if (_matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No active replay.");
            }

            _matchPresentationState.LiveReplay.PlaybackState = ReplayPlaybackState.Complete;
            _matchPresentationState.LiveReplay.CurrentEventIndex = _matchPresentationState.PresentedMatch
                .Rounds[_matchPresentationState.RoundIndex]
                .Messages
                .Count;
            return TransitionAfterReplayComplete("Replay skipped to the round result.");
        }

        private string NextRound()
        {
            if (_screenNavigationState.CurrentScreen == ScreenKind.RoundReview && _matchReviewStore.LastMatch is not null)
            {
                return RenderRoundReview(_selectedReviewRoundNumber + 1);
            }

            if (_matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No round summaries are available.");
            }

            if (_matchPresentationState.RoundIndex + 1 >= _matchPresentationState.PresentedMatch.Rounds.Count)
            {
                return RenderMatchSummary();
            }

            _matchPresentationState.RoundIndex++;
            _matchPresentationState.LiveReplay.Reset();
            _matchPresentationState.LiveReplay.CurrentEventIndex = 1;
            return ViewLiveReplay();
        }

        private string ViewReplay()
        {
            if (_matchReviewStore.LastMatch is null)
            {
                return RenderCurrentScreen("No completed match is available yet.");
            }

            return RenderReplayReview();
        }

        private string ViewLiveReplay(string? message = null)
        {
            if (_world is null || _matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No replay is available.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            _screenNavigationState.CurrentScreen = ScreenKind.LiveReplay;
            _currentScreenModel = BuildLiveReplayScreen(_world, humanTeam, league, _matchPresentationState, message);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ViewRounds()
        {
            if (_matchReviewStore.LastMatch is null)
            {
                return RenderCurrentScreen("No completed match is available yet.");
            }

            SetReviewBackScreen();
            _screenNavigationState.NavigateTo(ScreenKind.RoundList);
            _currentScreenModel = BuildRoundListScreen(_world, _matchReviewStore.LastMatch);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ViewActiveRoundSummary()
        {
            if (_matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No round summaries are available.");
            }

            _matchPresentationState.RoundIndex = 0;
            return RenderRoundSummary();
        }

        private string ViewLastMatch()
        {
            if (_matchReviewStore.LastMatch is null)
            {
                return RenderCurrentScreen("No completed match is available yet.");
            }

            SetReviewBackScreen();
            _screenNavigationState.NavigateTo(ScreenKind.LastMatchReview);
            _currentScreenModel = BuildLastMatchReviewScreen(_world, _matchReviewStore.LastMatch);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ViewRound()
        {
            if (_matchReviewStore.LastMatch is null)
            {
                return RenderCurrentScreen("No completed match is available yet.");
            }

            string value = _command.Trim()["view round ".Length..].Trim();
            if (!int.TryParse(value, out int roundNumber))
            {
                return RenderCurrentScreen($"Round {value} is not available.");
            }

            return RenderRoundReview(roundNumber);
        }

        private string PreviousReviewRound()
        {
            if (_matchReviewStore.LastMatch is null)
            {
                return RenderCurrentScreen("No completed match is available yet.");
            }

            return RenderRoundReview(_selectedReviewRoundNumber - 1);
        }

        private string NextReplayReviewPage()
        {
            if (_matchReviewStore.LastMatch is null)
            {
                return RenderCurrentScreen("No completed match is available yet.");
            }

            int pageCount = GetReplayReviewPageCount(_matchReviewStore.LastMatch, _replayReviewState);
            _replayReviewState.PageIndex = Math.Min(_replayReviewState.PageIndex + 1, Math.Max(0, pageCount - 1));
            return RenderReplayReview(preserveBackScreen: true);
        }

        private string PreviousReplayReviewPage()
        {
            if (_matchReviewStore.LastMatch is null)
            {
                return RenderCurrentScreen("No completed match is available yet.");
            }

            _replayReviewState.PageIndex = Math.Max(0, _replayReviewState.PageIndex - 1);
            return RenderReplayReview(preserveBackScreen: true);
        }

        private string RenderRoundReview(int roundNumber, string? message = null)
        {
            MatchReview? matchReview = _matchReviewStore.LastMatch;
            if (matchReview is null)
            {
                return RenderCurrentScreen("No completed match is available yet.");
            }

            RoundReview? roundReview = matchReview.Rounds.FirstOrDefault(round => round.RoundNumber == roundNumber);
            if (roundReview is null)
            {
                return RenderCurrentScreen($"Round {roundNumber} is not available.");
            }

            SetReviewBackScreen(includeReviewScreen: true);
            _selectedReviewRoundNumber = roundNumber;
            _screenNavigationState.NavigateTo(ScreenKind.RoundReview);
            _currentScreenModel = BuildRoundReviewScreen(_world, matchReview, roundReview, message);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string MatchSummaryCommand()
        {
            if (_matchPresentationState.PresentedMatch is not null)
            {
                return RenderMatchSummary();
            }

            return ViewLastMatch();
        }

        private string RenderReplayReview(string? message = null, bool preserveBackScreen = false)
        {
            MatchReview? matchReview = _matchReviewStore.LastMatch;
            if (matchReview is null)
            {
                return RenderCurrentScreen("No completed match is available yet.");
            }

            if (!preserveBackScreen)
            {
                SetReviewBackScreen(includeReviewScreen: true);
                int? roundNumber = _screenNavigationState.CurrentScreen == ScreenKind.RoundReview
                    ? _selectedReviewRoundNumber
                    : null;
                _replayReviewState.Reset(roundNumber);
            }

            _screenNavigationState.NavigateTo(ScreenKind.ReplayReview);
            _currentScreenModel = BuildReplayReviewScreen(_world, matchReview, _replayReviewState, message);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string CompleteMatchFlow()
        {
            _matchPresentationState.Clear();
            return RenderHome("Match flow complete.");
        }

        private string PlayReplay()
        {
            if (_matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No active replay.");
            }

            PresentedRound round = _matchPresentationState.PresentedMatch.Rounds[_matchPresentationState.RoundIndex];
            if (_matchPresentationState.LiveReplay.CurrentEventIndex >= round.Messages.Count)
            {
                _matchPresentationState.LiveReplay.PlaybackState = ReplayPlaybackState.Complete;
                return TransitionAfterReplayComplete("Round replay complete.");
            }

            _replayPlaybackStartedAtUtc = DateTime.UtcNow - _matchPresentationState.LiveReplay.CurrentPlaybackTime;
            _matchPresentationState.LiveReplay.PlaybackState = ReplayPlaybackState.Playing;
            ScheduleNextReplayAdvance(immediate: true);
            return ViewLiveReplay();
        }

        private string PauseReplay()
        {
            if (_matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No active replay.");
            }

            UpdateReplayPlaybackClock();
            _matchPresentationState.LiveReplay.PlaybackState = ReplayPlaybackState.Paused;
            return ViewLiveReplay();
        }

        private string IncreaseReplaySpeed()
        {
            if (_matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No active replay.");
            }

            _matchPresentationState.LiveReplay.ReplaySpeed = _matchPresentationState.LiveReplay.ReplaySpeed switch
            {
                ReplaySpeed.Slow => ReplaySpeed.Normal,
                ReplaySpeed.Normal => ReplaySpeed.Fast,
                ReplaySpeed.Fast => ReplaySpeed.VeryFast,
                _ => ReplaySpeed.VeryFast
            };

            if (_matchPresentationState.LiveReplay.PlaybackState == ReplayPlaybackState.Playing)
            {
                ScheduleNextReplayAdvance(immediate: false);
            }

            return ViewLiveReplay();
        }

        private string DecreaseReplaySpeed()
        {
            if (_matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No active replay.");
            }

            _matchPresentationState.LiveReplay.ReplaySpeed = _matchPresentationState.LiveReplay.ReplaySpeed switch
            {
                ReplaySpeed.VeryFast => ReplaySpeed.Fast,
                ReplaySpeed.Fast => ReplaySpeed.Normal,
                ReplaySpeed.Normal => ReplaySpeed.Slow,
                _ => ReplaySpeed.Slow
            };

            if (_matchPresentationState.LiveReplay.PlaybackState == ReplayPlaybackState.Playing)
            {
                ScheduleNextReplayAdvance(immediate: false);
            }

            return ViewLiveReplay();
        }

        private string ShowReplayDetails()
        {
            if (_matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No active replay.");
            }

            PresentedRound round = _matchPresentationState.PresentedMatch.Rounds[_matchPresentationState.RoundIndex];
            int revealed = Math.Min(_matchPresentationState.LiveReplay.CurrentEventIndex, round.Messages.Count);
            return ViewLiveReplay($"Replay details: {revealed} of {round.Messages.Count} events revealed.");
        }

        private void AdvanceReplayPlayback()
        {
            if (_matchPresentationState.PresentedMatch is null)
            {
                return;
            }

            UpdateReplayPlaybackClock();

            TryAdvanceReplayToCurrentTime();

            if (_matchPresentationState.LiveReplay.PlaybackState == ReplayPlaybackState.Complete)
            {
                TransitionAfterReplayComplete();
                _screenRenderer.Render(_currentScreenModel, _command);
                return;
            }

            ViewLiveReplay();
            _screenRenderer.Render(_currentScreenModel, _command);
            ScheduleNextReplayAdvance(immediate: false);
        }

        private void UpdateReplayPlaybackClock()
        {
            if (_matchPresentationState.LiveReplay.PlaybackState != ReplayPlaybackState.Playing)
            {
                return;
            }

            _matchPresentationState.LiveReplay.CurrentPlaybackTime = _replayPlaybackStartedAtUtc == DateTime.MinValue
                ? TimeSpan.Zero
                : DateTime.UtcNow - _replayPlaybackStartedAtUtc;
        }

        private bool TryAdvanceReplayToCurrentTime()
        {
            if (_matchPresentationState.PresentedMatch is null)
            {
                return false;
            }

            PresentedRound round = _matchPresentationState.PresentedMatch.Rounds[_matchPresentationState.RoundIndex];
            int currentIndex = _matchPresentationState.LiveReplay.CurrentEventIndex;
            bool advanced = false;

            while (currentIndex < round.Messages.Count
                && round.Messages[currentIndex].Timestamp <= _matchPresentationState.LiveReplay.CurrentPlaybackTime)
            {
                currentIndex++;
                advanced = true;
            }

            if (advanced)
            {
                _matchPresentationState.LiveReplay.CurrentEventIndex = currentIndex;
            }

            if (_matchPresentationState.LiveReplay.CurrentEventIndex >= round.Messages.Count)
            {
                _matchPresentationState.LiveReplay.CurrentEventIndex = round.Messages.Count;
                _matchPresentationState.LiveReplay.CurrentPlaybackTime = round.Messages[^1].Timestamp;
                _matchPresentationState.LiveReplay.PlaybackState = ReplayPlaybackState.Complete;
            }

            return advanced;
        }

        private bool TryAdvanceReplay()
        {
            if (_matchPresentationState.PresentedMatch is null)
            {
                return false;
            }

            PresentedRound round = _matchPresentationState.PresentedMatch.Rounds[_matchPresentationState.RoundIndex];
            if (_matchPresentationState.LiveReplay.CurrentEventIndex >= round.Messages.Count)
            {
                _matchPresentationState.LiveReplay.CurrentEventIndex = round.Messages.Count;
                _matchPresentationState.LiveReplay.PlaybackState = ReplayPlaybackState.Complete;
                return false;
            }

            _matchPresentationState.LiveReplay.CurrentEventIndex++;
            _matchPresentationState.LiveReplay.CurrentPlaybackTime = round.Messages[_matchPresentationState.LiveReplay.CurrentEventIndex - 1].Timestamp;
            if (_matchPresentationState.LiveReplay.CurrentEventIndex >= round.Messages.Count)
            {
                _matchPresentationState.LiveReplay.CurrentEventIndex = round.Messages.Count;
                _matchPresentationState.LiveReplay.CurrentPlaybackTime = round.Messages[^1].Timestamp;
                _matchPresentationState.LiveReplay.PlaybackState = ReplayPlaybackState.Complete;
            }

            return true;
        }

        private string TransitionAfterReplayComplete(string? message = null)
        {
            _matchPresentationState.LiveReplay.PlaybackState = ReplayPlaybackState.Complete;
            _replayPlaybackStartedAtUtc = DateTime.MinValue;
            _nextReplayAdvanceAtUtc = DateTime.MinValue;

            if (_matchPresentationState.PresentedMatch is null)
            {
                return RenderCurrentScreen("No replay is available.");
            }

            return _matchPresentationState.RoundIndex + 1 < _matchPresentationState.PresentedMatch.Rounds.Count
                ? RenderRoundSummary(message)
                : RenderMatchSummary(message ?? "Replay complete.");
        }

        private void ScheduleNextReplayAdvance(bool immediate)
        {
            _nextReplayAdvanceAtUtc = immediate
                ? DateTime.UtcNow
                : DateTime.UtcNow.Add(GetReplayDelay(_matchPresentationState.LiveReplay.ReplaySpeed));
        }

        private static TimeSpan GetReplayDelay(ReplaySpeed replaySpeed) =>
            replaySpeed switch
            {
                ReplaySpeed.Slow => TimeSpan.FromMilliseconds(1500),
                ReplaySpeed.Fast => TimeSpan.FromMilliseconds(400),
                ReplaySpeed.VeryFast => TimeSpan.FromMilliseconds(150),
                _ => TimeSpan.FromMilliseconds(900)
            };

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

        private string BeginNewGameSetup()
        {
            if (_world is not null)
            {
                return RenderCurrentScreen("A game world already exists.");
            }

            _inputMode = AppInputMode.NewGameSetup;
            _newGameSetupState = new NewGameSetupState
            {
                Step = NewGameSetupStep.CoachName
            };
            _screenNavigationState.ResetTo(ScreenKind.NewGameSetup);
            _currentScreenModel = BuildNewGameSetupScreen(_newGameSetupState);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string HandleNewGameSetupInput(string input)
        {
            string normalizedInput = (input ?? string.Empty).Trim();

            if (_screenNavigationState.CurrentScreen == ScreenKind.Help)
            {
                if (string.Equals(normalizedInput, ConsoleConstants.Back, StringComparison.OrdinalIgnoreCase))
                {
                    return Back();
                }

                if (string.Equals(normalizedInput, ConsoleConstants.Cancel, StringComparison.OrdinalIgnoreCase))
                {
                    return CancelNewGameSetup();
                }

                if (string.Equals(normalizedInput, ConsoleConstants.Help, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(normalizedInput, ConsoleConstants.ShowHelp, StringComparison.OrdinalIgnoreCase))
                {
                    return RenderHelp();
                }

                return RenderCurrentScreen("Use back to return to new game setup.");
            }

            if (_newGameSetupState is null)
            {
                _inputMode = AppInputMode.Command;
                return RenderHome();
            }

            if (string.Equals(normalizedInput, ConsoleConstants.Cancel, StringComparison.OrdinalIgnoreCase))
            {
                return CancelNewGameSetup();
            }

            if (string.Equals(normalizedInput, ConsoleConstants.Help, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedInput, ConsoleConstants.ShowHelp, StringComparison.OrdinalIgnoreCase))
            {
                return RenderHelp();
            }

            if (string.Equals(normalizedInput, ConsoleConstants.Back, StringComparison.OrdinalIgnoreCase))
            {
                return BackInNewGameSetup();
            }

            return _newGameSetupState.Step switch
            {
                NewGameSetupStep.CoachName => SubmitCoachName(normalizedInput),
                NewGameSetupStep.TeamName => SubmitTeamName(normalizedInput),
                _ => RenderCurrentScreen()
            };
        }

        private string CancelNewGameSetup()
        {
            _inputMode = AppInputMode.Command;
            _newGameSetupState = null;
            return RenderHome("New game setup cancelled.");
        }

        private string BackInNewGameSetup()
        {
            if (_newGameSetupState is null)
            {
                _inputMode = AppInputMode.Command;
                return RenderHome();
            }

            if (_newGameSetupState.Step == NewGameSetupStep.TeamName)
            {
                _newGameSetupState.Step = NewGameSetupStep.CoachName;
                return RenderNewGameSetupScreen();
            }

            return RenderNewGameSetupScreen("You are already on the first setup step.");
        }

        private string SubmitCoachName(string input)
        {
            if (input.Length < 2 || input.Length > 40)
            {
                return RenderNewGameSetupScreen("Coach name must be between 2 and 40 characters.");
            }

            _newGameSetupState!.CoachName = input;
            _newGameSetupState.Step = NewGameSetupStep.TeamName;
            return RenderNewGameSetupScreen();
        }

        private string SubmitTeamName(string input)
        {
            if (input.Length < 2 || input.Length > 50)
            {
                return RenderNewGameSetupScreen("Team name must be between 2 and 50 characters.");
            }

            _newGameSetupState!.TeamName = input;

            return StartGame(new NewGameOptions
            {
                CoachName = _newGameSetupState.CoachName!,
                TeamName = _newGameSetupState.TeamName
            });
        }

        private string StartGame(NewGameOptions options)
        {
            int seed = _seedProvider();
            _world = _worldGenerationService.CreateWorld(seed, options.CoachName, options.TeamName);
            _pendingMatch = null;
            _inputMode = AppInputMode.Command;
            _newGameSetupState = null;
            _screenNavigationState.ResetTo(ScreenKind.Home);
            return RenderHome($"New game created. Coach: {options.CoachName} | Team: {options.TeamName}");
        }

        private string ShowLeague()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No world has been created yet. Use `start` to begin a new game.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            _selectedTeam = humanTeam;
            _screenNavigationState.NavigateTo(ScreenKind.League);
            _currentScreenModel = BuildLeagueScreen(_world, humanTeam, league);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ShowTeam()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No world has been created yet. Use `start` to begin a new game.");
            }

            Team team = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, team);
            _selectedTeam = team;
            _screenNavigationState.NavigateTo(ScreenKind.Team);
            _currentScreenModel = BuildTeamScreen(_world, team, league);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ShowSpecificTeam()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No world has been created yet. Use `start` to begin a new game.");
            }

            string query = _command.Trim()[$"{ConsoleConstants.ShowTeam} ".Length..].Trim();
            IReadOnlyList<Team> matches = FindTeamMatches(_world, query);
            if (matches.Count == 0)
            {
                return RenderCurrentScreen($"No team found matching: {query}");
            }

            if (matches.Count > 1)
            {
                return RenderCurrentScreen($"Multiple teams match: {string.Join(", ", matches.Select(team => team.Name))}");
            }

            Team team = matches.Single();
            League league = GetTeamLeague(_world, team);
            _selectedTeam = team;
            _screenNavigationState.NavigateTo(ScreenKind.Team);
            _currentScreenModel = BuildTeamScreen(_world, team, league);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ShowSchedule()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No world has been created yet. Use `start` to begin a new game.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            _screenNavigationState.NavigateTo(ScreenKind.Schedule);
            _currentScreenModel = BuildScheduleScreen(_world, humanTeam, league);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ShowOpponent()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No world has been created yet. Use `start` to begin a new game.");
            }

            Team humanTeam = GetHumanTeam(_world);
            ScheduledMatch? activeMatch = GetRelevantOpponentMatch(_world, humanTeam);
            if (activeMatch is null)
            {
                return RenderCurrentScreen("No opponent is available right now.");
            }

            League league = GetTeamLeague(_world, humanTeam);
            string opponentId = string.Equals(activeMatch.HomeTeamId, humanTeam.Id, StringComparison.Ordinal)
                ? activeMatch.AwayTeamId
                : activeMatch.HomeTeamId;
            Team opponent = league.Teams.First(team => string.Equals(team.Id, opponentId, StringComparison.Ordinal));
            _selectedTeam = opponent;
            _screenNavigationState.NavigateTo(ScreenKind.Team);
            _currentScreenModel = BuildTeamScreen(_world, opponent, league, $"Opponent: {opponent.Name}");
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ShowPlayer()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No world has been created yet. Use `start` to begin a new game.");
            }

            string query = _command.Trim()[$"{ConsoleConstants.ShowPlayer} ".Length..].Trim();
            IReadOnlyList<Player> matches = FindPlayerMatches(_world, query, _selectedTeam);
            if (matches.Count == 0)
            {
                return RenderCurrentScreen($"No player found matching: {query}");
            }

            if (matches.Count > 1)
            {
                return RenderCurrentScreen($"Multiple players match: {string.Join(", ", matches.Select(player => player.Name))}");
            }

            Player player = matches.Single();
            Team? team = player.TeamId is null ? null : FindTeamById(_world, player.TeamId);
            League? league = team is null ? null : GetTeamLeague(_world, team);
            _selectedPlayer = player;
            _screenNavigationState.NavigateTo(ScreenKind.PlayerDetail);
            _currentScreenModel = BuildPlayerScreen(_world, player, team, league);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ShowPlayoffs()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No world has been created yet. Use `start` to begin a new game.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            _screenNavigationState.NavigateTo(ScreenKind.Playoffs);
            _currentScreenModel = BuildPlayoffPictureScreen(_world, humanTeam, league);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string ShowChampions()
        {
            if (_world is null)
            {
                return RenderChampionCatalog();
            }

            if (_screenNavigationState.CurrentScreen is not ScreenKind.ChampionCatalog and not ScreenKind.ChampionDetail)
            {
                _championCatalogBackScreen = _screenNavigationState.CurrentScreen;
            }

            return RenderChampionCatalog();
        }

        private string ShowChampion()
        {
            string query = _command.Trim()["show champion ".Length..].Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                return RenderCurrentScreen("No champion found matching: ");
            }

            IReadOnlyList<ChampionDefinition> matches = FindChampionMatches(query);
            if (matches.Count == 0)
            {
                return RenderCurrentScreen($"No champion found matching: {query}");
            }

            if (matches.Count > 1)
            {
                return RenderCurrentScreen($"Multiple champions match: {string.Join(", ", matches.Select(champion => champion.Name))}");
            }

            if (_screenNavigationState.CurrentScreen is not ScreenKind.ChampionCatalog and not ScreenKind.ChampionDetail)
            {
                _championDetailBackScreen = _screenNavigationState.CurrentScreen;
            }
            else if (_screenNavigationState.CurrentScreen == ScreenKind.ChampionCatalog)
            {
                _championDetailBackScreen = ScreenKind.ChampionCatalog;
            }

            _selectedChampion = matches.Single();
            _screenNavigationState.NavigateTo(ScreenKind.ChampionDetail);
            _currentScreenModel = BuildChampionDetailScreen(_world, _selectedChampion);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string FilterChampionRole()
        {
            string roleText = _command.Trim()[$"{ConsoleConstants.FilterRole} ".Length..].Trim();
            if (!Enum.TryParse(roleText, ignoreCase: true, out ChampionRole role))
            {
                return RenderCurrentScreen($"Unknown role: {roleText}");
            }

            _championCatalogFilter = role;
            return RenderChampionCatalog($"Filter applied: {role}");
        }

        private string ClearChampionFilter()
        {
            _championCatalogFilter = null;
            return RenderChampionCatalog("Champion filter cleared.");
        }

        private string Back()
        {
            if (_screenNavigationState.CurrentScreen is ScreenKind.ChampionDetail
                && _championDetailBackScreen == ScreenKind.ChampionCatalog)
            {
                _screenNavigationState.CurrentScreen = ScreenKind.ChampionCatalog;
                return RenderChampionCatalog();
            }

            if (!_screenNavigationState.TryGoBack(out _))
            {
                return RenderCurrentScreen("No previous screen is available.");
            }

            _selectedChampion = null;
            if (_screenNavigationState.CurrentScreen != ScreenKind.PlayerDetail)
            {
                _selectedPlayer = null;
            }

            if (_screenNavigationState.CurrentScreen != ScreenKind.Team)
            {
                _selectedTeam = null;
            }

            return RenderCurrentScreen();
        }

        private string RenderChampionCatalog(string? message = null)
        {
            _selectedChampion = null;
            _screenNavigationState.NavigateTo(ScreenKind.ChampionCatalog);
            _currentScreenModel = BuildChampionCatalogScreen(_world, _championCatalogFilter, message);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private void SetReviewBackScreen(bool includeReviewScreen = false)
        {
            if (!IsReviewScreen(_screenNavigationState.CurrentScreen) || includeReviewScreen)
            {
                _reviewBackScreen = _screenNavigationState.CurrentScreen;
            }
        }

        private static bool IsReviewScreen(ScreenKind screenKind) =>
            screenKind is ScreenKind.LastMatchReview
                or ScreenKind.RoundList
                or ScreenKind.RoundReview
                or ScreenKind.ReplayReview;

        private string RenderHome(string? message = null)
        {
            if (_world is null)
            {
                _screenNavigationState.ResetTo(ScreenKind.Home);
                _currentScreenModel = BuildWelcomeScreen(message);
                return _screenRenderer.RenderToString(_currentScreenModel);
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            _screenNavigationState.ResetTo(ScreenKind.Home);
            _currentScreenModel = BuildHomeScreen(_world, humanTeam, league, message);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string RenderMatchPreview()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No world has been created yet. Use `start` to begin a new game.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            ScheduledMatch? match = GetCurrentWeekHumanMatch(_world, humanTeam);
            if (match is null)
            {
                _pendingMatch = null;
                return RenderCurrentScreen("No scheduled match is available this week.");
            }

            _pendingMatch = match;
            _matchPresentationState.Clear();
            _matchPresentationState.ScheduledMatch = match;
            _screenNavigationState.NavigateTo(ScreenKind.MatchPreview);
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
            _screenNavigationState.NavigateTo(ScreenKind.Draft);
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
            _helpContextScreen = _screenNavigationState.CurrentScreen;
            _helpContextCommands = GetCurrentScreenCommands();
            _screenNavigationState.NavigateTo(ScreenKind.Help);
            _currentScreenModel = BuildHelpScreen(_world, _helpContextScreen, _helpContextCommands);
            return _screenRenderer.RenderToString(_currentScreenModel);
        }

        private string RenderNewGameSetupScreen(string? message = null)
        {
            if (_newGameSetupState is null)
            {
                _inputMode = AppInputMode.Command;
                return RenderHome(message);
            }

            _screenNavigationState.CurrentScreen = ScreenKind.NewGameSetup;
            _currentScreenModel = BuildNewGameSetupScreen(_newGameSetupState, message);
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
                return _screenNavigationState.CurrentScreen switch
                {
                    ScreenKind.Help => BuildHelpScreen(null, _helpContextScreen, _helpContextCommands, message),
                    ScreenKind.NewGameSetup when _newGameSetupState is not null =>
                        BuildNewGameSetupScreen(_newGameSetupState, message),
                    ScreenKind.ChampionCatalog => BuildChampionCatalogScreen(null, _championCatalogFilter, message),
                    ScreenKind.ChampionDetail when _selectedChampion is not null =>
                        BuildChampionDetailScreen(null, _selectedChampion, message),
                    ScreenKind.LastMatchReview when _matchReviewStore.LastMatch is not null =>
                        BuildLastMatchReviewScreen(null, _matchReviewStore.LastMatch, message),
                    ScreenKind.RoundList when _matchReviewStore.LastMatch is not null =>
                        BuildRoundListScreen(null, _matchReviewStore.LastMatch, message),
                    ScreenKind.RoundReview when _matchReviewStore.LastMatch is not null =>
                        BuildRoundReviewScreen(
                            null,
                            _matchReviewStore.LastMatch,
                            GetReviewRound(_matchReviewStore.LastMatch, _selectedReviewRoundNumber),
                            message),
                    ScreenKind.ReplayReview when _matchReviewStore.LastMatch is not null =>
                        BuildReplayReviewScreen(null, _matchReviewStore.LastMatch, _replayReviewState, message),
                    _ => BuildWelcomeScreen(message)
                };
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            return _screenNavigationState.CurrentScreen switch
            {
                ScreenKind.Team => BuildTeamScreen(_world, _selectedTeam ?? humanTeam, GetTeamLeague(_world, _selectedTeam ?? humanTeam), message),
                ScreenKind.PlayerDetail when _selectedPlayer is not null =>
                    BuildPlayerScreen(_world, _selectedPlayer, _selectedPlayer.TeamId is null ? null : FindTeamById(_world, _selectedPlayer.TeamId), _selectedPlayer.TeamId is null ? null : GetTeamLeague(_world, FindTeamById(_world, _selectedPlayer.TeamId)!), message),
                ScreenKind.League => BuildLeagueScreen(_world, humanTeam, league, message),
                ScreenKind.Playoffs => BuildPlayoffPictureScreen(_world, humanTeam, league, message),
                ScreenKind.Help => BuildHelpScreen(_world, _helpContextScreen, _helpContextCommands, message),
                ScreenKind.Schedule => BuildScheduleScreen(_world, humanTeam, league, message),
                ScreenKind.ChampionCatalog => BuildChampionCatalogScreen(_world, _championCatalogFilter, message),
                ScreenKind.ChampionDetail when _selectedChampion is not null =>
                    BuildChampionDetailScreen(_world, _selectedChampion, message),
                ScreenKind.LastMatchReview when _matchReviewStore.LastMatch is not null =>
                    BuildLastMatchReviewScreen(_world, _matchReviewStore.LastMatch, message),
                ScreenKind.RoundList when _matchReviewStore.LastMatch is not null =>
                    BuildRoundListScreen(_world, _matchReviewStore.LastMatch, message),
                ScreenKind.RoundReview when _matchReviewStore.LastMatch is not null =>
                    BuildRoundReviewScreen(
                        _world,
                        _matchReviewStore.LastMatch,
                        GetReviewRound(_matchReviewStore.LastMatch, _selectedReviewRoundNumber),
                        message),
                ScreenKind.ReplayReview when _matchReviewStore.LastMatch is not null =>
                    BuildReplayReviewScreen(_world, _matchReviewStore.LastMatch, _replayReviewState, message),
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

        private static ScreenRenderModel BuildNewGameSetupScreen(
            NewGameSetupState state,
            string? message = null)
        {
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
                    ConsoleConstants.ShowChampions,
                    ConsoleConstants.StartMatch,
                    ConsoleConstants.ViewLastMatch,
                    ConsoleConstants.ViewReplay,
                    ConsoleConstants.ViewRounds,
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
                    ConsoleConstants.ShowChampions,
                    ConsoleConstants.StartMatch,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = BuildHeader(world, team, league),
                Message = message,
                Title = "Team Detail"
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
                    ConsoleConstants.ShowChampions,
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
                    ConsoleConstants.ShowChampions,
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
                $"Round {round.Result.RoundNumber} | {FormatTimestamp(currentTime)} / 05:00 | {FormatReplayPlaybackState(replay.PlaybackState)} | Speed: {replay.ReplaySpeed}",
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

        private static ScreenRenderModel BuildPlayerScreen(
            WorldState? world,
            Player player,
            Team? team,
            League? league,
            string? message = null)
        {
            List<string> lines =
            [
                player.Name,
                string.Empty,
                $"Team: {team?.Name ?? "Free Agent"}",
                $"Position: {player.PositionRole}",
                $"Rating: {GetPlayerRating(player)}",
                $"Traits: {GetPlayerTraits(player)}",
                string.Empty,
                "Contract",
                "Contracts are not implemented yet.",
                string.Empty,
                "Recent Performance",
                "Recent player performance is not available yet.",
                string.Empty,
                "Notes",
                "Future decision-making stats will appear here when player systems expand."
            ];

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Back,
                    ConsoleConstants.ShowTeam,
                    ConsoleConstants.ShowSchedule,
                    ConsoleConstants.Home,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = world is not null && team is not null && league is not null
                    ? BuildHeader(world, team, league)
                    : BuildChampionHeader(world),
                Message = message,
                Title = "Player Detail"
            };
        }

        private static ScreenRenderModel BuildPlayoffPictureScreen(
            WorldState world,
            Team humanTeam,
            League league,
            string? message = null)
        {
            IReadOnlyList<string> divisionLeaderLines = league.Divisions
                .OrderBy(division => division.Name)
                .Select(division =>
                {
                    LeagueStanding? leader = GetStandingsForDivision(league, division).FirstOrDefault();
                    return leader is null
                        ? $"{division.Name}: standings unavailable"
                        : $"{division.Name}: {FormatTeamName(world, leader.TeamId)} ({FormatRecord(leader)}, {FormatPoints(leader.Points)})";
                })
                .ToList();

            IReadOnlyList<string> wildcardLines = league.Standings
                .Where(standing => !league.Divisions
                    .Select(division => GetStandingsForDivision(league, division).FirstOrDefault()?.TeamId)
                    .Where(teamId => teamId is not null)
                    .Contains(standing.TeamId))
                .OrderByDescending(standing => standing.MatchWins)
                .ThenByDescending(standing => standing.Points)
                .ThenBy(standing => FormatTeamName(world, standing.TeamId), StringComparer.Ordinal)
                .Take(4)
                .Select(standing => $"{FormatTeamName(world, standing.TeamId)} ({FormatRecord(standing)}, {FormatPoints(standing.Points)})")
                .ToList();

            IReadOnlyList<string> bubbleLines = league.Standings
                .Where(standing => !wildcardLines.Any(line => line.StartsWith(FormatTeamName(world, standing.TeamId), StringComparison.Ordinal)))
                .OrderByDescending(standing => standing.MatchWins)
                .ThenByDescending(standing => standing.Points)
                .ThenBy(standing => FormatTeamName(world, standing.TeamId), StringComparer.Ordinal)
                .Take(2)
                .Select(standing => $"{FormatTeamName(world, standing.TeamId)} ({FormatRecord(standing)}, {FormatPoints(standing.Points)})")
                .ToList();

            List<string> lines =
            [
                $"Current league: {FormatLeagueName(league)}",
                $"Current week: {world.Season.CurrentWeek}",
                $"Season status: {GetSeasonStatus(world.Season.CurrentWeek)}",
                string.Empty,
                "Format",
                "Regular season lasts 23 weeks.",
                "Playoffs: 8 teams total.",
                "- 4 division winners",
                "- 4 wildcard teams",
                "Week 24: League Quarterfinals, best-of-5",
                "Week 25: League Semifinals, best-of-5",
                "Week 26: League Finals, best-of-7",
                "World Tier league champions advance to World Championship.",
                "Week 27: World Championship Semifinals, best-of-7",
                "Week 28: World Championship Final, best-of-9",
                string.Empty,
                "Division Leaders",
                .. divisionLeaderLines
            ];

            if (league.Standings.Count == 0)
            {
                lines.Add(string.Empty);
                lines.Add("Playoff picture is not available until standings data is available.");
            }
            else
            {
                lines.Add(string.Empty);
                lines.Add("Wildcard Candidates");
                lines.AddRange(wildcardLines.DefaultIfEmpty("Playoff picture is not available until standings data is available."));
                lines.Add(string.Empty);
                lines.Add("Bubble Teams");
                lines.AddRange(bubbleLines.DefaultIfEmpty("Playoff picture is not available until standings data is available."));
            }

            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Home,
                    ConsoleConstants.ShowLeague,
                    ConsoleConstants.ShowSchedule,
                    ConsoleConstants.Back,
                    ConsoleConstants.Help
                ],
                ContentLines = lines,
                Header = BuildHeader(world, humanTeam, league),
                Message = message,
                Title = "Playoff Picture"
            };
        }

        private static ScreenRenderModel BuildHelpScreen(
            WorldState? world,
            ScreenKind contextScreen,
            IReadOnlyList<string> contextCommands,
            string? message = null)
        {
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
                Header = BuildChampionHeader(world),
                Message = message,
                Title = "Help"
            };
        }

        private static ScreenRenderModel BuildChampionCatalogScreen(
            WorldState? world,
            ChampionRole? roleFilter,
            string? message = null)
        {
            IReadOnlyList<ChampionDefinition> champions = GetOrderedChampions()
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
                Header = BuildChampionHeader(world),
                Message = message,
                Title = "Champion Catalog"
            };
        }

        private static ScreenRenderModel BuildChampionDetailScreen(
            WorldState? world,
            ChampionDefinition champion,
            string? message = null)
        {
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
                Header = BuildChampionHeader(world),
                Message = message,
                Title = "Champion Detail"
            };
        }

        private static ScreenRenderModel BuildLastMatchReviewScreen(
            WorldState? world,
            MatchReview match,
            string? message = null)
        {
            List<string> lines =
            [
                $"Week {match.WeekNumber} | {match.MatchType} | {match.BestOfLabel}",
                string.Empty,
                $"{match.WinnerTeamName} defeated {GetLosingTeamName(match)} {match.BlueRoundWins}-{match.RedRoundWins}",
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
                Header = BuildChampionHeader(world),
                Message = message,
                Title = "Last Match Review"
            };
        }

        private static ScreenRenderModel BuildRoundListScreen(
            WorldState? world,
            MatchReview match,
            string? message = null)
        {
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
                Header = BuildChampionHeader(world),
                Message = message,
                Title = "Round List"
            };
        }

        private static ScreenRenderModel BuildRoundReviewScreen(
            WorldState? world,
            MatchReview match,
            RoundReview round,
            string? message = null)
        {
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
                Header = BuildChampionHeader(world),
                Message = message,
                Title = "Round Review"
            };
        }

        private static ScreenRenderModel BuildReplayReviewScreen(
            WorldState? world,
            MatchReview match,
            ReplayReviewState replayState,
            string? message = null)
        {
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
                Header = BuildChampionHeader(world),
                Message = message,
                Title = "Replay Review"
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

        private static ScreenHeaderModel BuildChampionHeader(WorldState? world)
        {
            if (world is null)
            {
                return new ScreenHeaderModel
                {
                    PrimaryLeft = "AutoSim",
                    PrimaryRight = "Champion Reference"
                };
            }

            Team humanTeam = GetHumanTeam(world);
            League league = GetTeamLeague(world, humanTeam);
            return BuildHeader(world, humanTeam, league);
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

        private static IReadOnlyList<ChampionDefinition> GetOrderedChampions() =>
            ChampionCatalog.GetDefaultChampions()
                .OrderBy(champion => champion.Role)
                .ThenBy(champion => champion.Name, StringComparer.Ordinal)
                .ToList();

        private static IReadOnlyList<ChampionDefinition> FindChampionMatches(string query)
        {
            IReadOnlyList<ChampionDefinition> champions = GetOrderedChampions();
            List<ChampionDefinition> exactMatches = champions
                .Where(champion => string.Equals(champion.Name, query, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (exactMatches.Count > 0)
            {
                return exactMatches;
            }

            return champions
                .Where(champion => champion.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static Team? FindTeamById(WorldState world, string teamId) =>
            world.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Teams)
                .FirstOrDefault(team => string.Equals(team.Id, teamId, StringComparison.Ordinal));

        private static IReadOnlyList<Team> FindTeamMatches(WorldState world, string query)
        {
            IReadOnlyList<Team> teams = world.Tiers
                .SelectMany(tier => tier.Leagues)
                .SelectMany(league => league.Teams)
                .OrderBy(team => team.Name, StringComparer.Ordinal)
                .ToList();

            List<Team> exactMatches = teams
                .Where(team => string.Equals(team.Name, query, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (exactMatches.Count > 0)
            {
                return exactMatches;
            }

            List<Team> partialMatches = teams
                .Where(team => team.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return partialMatches.Count == 1 ? partialMatches : partialMatches;
        }

        private static IReadOnlyList<Player> FindPlayerMatches(WorldState world, string query, Team? contextTeam)
        {
            IReadOnlyList<Player> allPlayers = world.Players
                .OrderBy(player => player.Name, StringComparer.Ordinal)
                .ToList();
            Team humanTeam = GetHumanTeam(world);
            HashSet<string> prioritizedTeamIds = [humanTeam.Id];
            if (contextTeam is not null)
            {
                prioritizedTeamIds.Add(contextTeam.Id);
            }

            IReadOnlyList<Player> prioritizedPlayers = allPlayers
                .Where(player => player.TeamId is not null && prioritizedTeamIds.Contains(player.TeamId))
                .ToList();

            List<Player> exactPriorityMatches = prioritizedPlayers
                .Where(player => string.Equals(player.Name, query, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (exactPriorityMatches.Count > 0)
            {
                return exactPriorityMatches;
            }

            List<Player> exactMatches = allPlayers
                .Where(player => string.Equals(player.Name, query, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (exactMatches.Count > 0)
            {
                return exactMatches;
            }

            List<Player> partialPriorityMatches = prioritizedPlayers
                .Where(player => player.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (partialPriorityMatches.Count == 1)
            {
                return partialPriorityMatches;
            }

            List<Player> partialMatches = allPlayers
                .Where(player => player.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return partialMatches;
        }

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

        private ScheduledMatch? GetRelevantOpponentMatch(WorldState world, Team humanTeam)
        {
            if (_pendingMatch is not null)
            {
                return _pendingMatch;
            }

            if (_matchPresentationState.ScheduledMatch is not null)
            {
                return _matchPresentationState.ScheduledMatch;
            }

            if (_matchPresentationState.PresentedMatch is not null)
            {
                return world.Tiers
                    .SelectMany(tier => tier.Leagues)
                    .SelectMany(league => league.Schedule)
                    .FirstOrDefault(match => string.Equals(match.Id, _matchPresentationState.PresentedMatch.Result.MatchId, StringComparison.Ordinal));
            }

            return _screenNavigationState.CurrentScreen is ScreenKind.Home or ScreenKind.Schedule or ScreenKind.Team or ScreenKind.League
                ? GetNextHumanMatch(world, humanTeam)
                : null;
        }

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

        private static string GetSeasonStatus(int week) =>
            week <= 23
                ? "Regular Season"
                : week switch
                {
                    24 => "League Quarterfinals",
                    25 => "League Semifinals",
                    26 => "League Finals",
                    27 => "World Championship Semifinals",
                    28 => "World Championship Final",
                    _ => "Offseason / Future Phase"
                };

        private static int GetPlayerRating(Player player) =>
            45 + ((Math.Abs(StringComparer.Ordinal.GetHashCode(player.Name)) % 25));

        private static string GetPlayerTraits(Player player)
        {
            string[] traits = ["Clutch", "Steady", "Aggressive", "Disciplined", "Flexible"];
            return traits[Math.Abs(StringComparer.Ordinal.GetHashCode(player.Name + player.PositionRole)) % traits.Length];
        }

        private IReadOnlyList<string> GetCurrentScreenCommands() =>
            _currentScreenModel.Commands;

        private static IReadOnlyList<string> GetCommandsForScreen(ScreenKind screenKind) =>
            screenKind switch
            {
                ScreenKind.Home => [ConsoleConstants.ShowTeam, ConsoleConstants.ShowLeague, ConsoleConstants.ShowSchedule, ConsoleConstants.ShowPlayoffs, ConsoleConstants.ShowChampions, ConsoleConstants.ViewLastMatch, ConsoleConstants.Help],
                ScreenKind.Team => [ConsoleConstants.Back, "show player <name>", ConsoleConstants.ShowOpponent, ConsoleConstants.ShowSchedule, ConsoleConstants.ShowLeague, ConsoleConstants.Home, ConsoleConstants.Help],
                ScreenKind.PlayerDetail => [ConsoleConstants.Back, ConsoleConstants.ShowTeam, ConsoleConstants.ShowSchedule, ConsoleConstants.Home, ConsoleConstants.Help],
                ScreenKind.League => ["show team <team name>", ConsoleConstants.ShowPlayoffs, ConsoleConstants.ShowSchedule, ConsoleConstants.Home, ConsoleConstants.Back, ConsoleConstants.Help],
                ScreenKind.Playoffs => [ConsoleConstants.Home, ConsoleConstants.ShowLeague, ConsoleConstants.ShowSchedule, ConsoleConstants.Back, ConsoleConstants.Help],
                ScreenKind.Schedule => ["show team <team name>", ConsoleConstants.ShowOpponent, ConsoleConstants.ShowPlayoffs, ConsoleConstants.StartMatch, ConsoleConstants.Home, ConsoleConstants.Back, ConsoleConstants.Help],
                ScreenKind.MatchPreview => [ConsoleConstants.ShowOpponent, ConsoleConstants.ShowTeam, "show team <team name>", "show player <player name>", ConsoleConstants.Continue, ConsoleConstants.Cancel, ConsoleConstants.Help],
                ScreenKind.LiveReplay => ["show champion <name>", "show player <player name>", "show team <team name>", ConsoleConstants.ShowOpponent, ConsoleConstants.Home, ConsoleConstants.Back, ConsoleConstants.Help],
                ScreenKind.RoundSummary => ["show champion <name>", "show player <player name>", "show team <team name>", ConsoleConstants.ShowOpponent, ConsoleConstants.Home, ConsoleConstants.Back, ConsoleConstants.Help],
                ScreenKind.MatchSummary => ["show player <player name>", "show team <team name>", ConsoleConstants.ShowOpponent, ConsoleConstants.Home, ConsoleConstants.Help],
                ScreenKind.LastMatchReview => ["show player <player name>", "show team <team name>", ConsoleConstants.Home, ConsoleConstants.Help],
                ScreenKind.RoundList => ["show player <player name>", "show team <team name>", ConsoleConstants.Home, ConsoleConstants.Back, ConsoleConstants.Help],
                ScreenKind.RoundReview => ["show champion <name>", "show player <player name>", "show team <team name>", ConsoleConstants.ShowOpponent, ConsoleConstants.Home, ConsoleConstants.Back, ConsoleConstants.Help],
                ScreenKind.ReplayReview => ["show champion <name>", "show player <player name>", "show team <team name>", ConsoleConstants.ShowOpponent, ConsoleConstants.Home, ConsoleConstants.Back, ConsoleConstants.Help],
                _ => [ConsoleConstants.Help]
            };

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

        private static string FormatReplayPlaybackState(ReplayPlaybackState playbackState) =>
            playbackState switch
            {
                ReplayPlaybackState.Playing => "Playing",
                ReplayPlaybackState.Complete => "Complete",
                _ => "Paused"
            };

        private static string FormatTimestamp(TimeSpan timestamp) =>
            $"{(int)timestamp.TotalMinutes:00}:{timestamp.Seconds:00}";

        private static RoundReview GetReviewRound(MatchReview matchReview, int roundNumber) =>
            matchReview.Rounds.FirstOrDefault(round => round.RoundNumber == roundNumber)
            ?? matchReview.Rounds.First();

        private static IReadOnlyList<string> GetMatchKeyMoments(MatchReview match) =>
            match.Rounds
                .SelectMany(round => round.KeyMoments.Select(moment => $"R{round.RoundNumber} {moment}"))
                .Take(6)
                .ToList();

        private static string GetLosingTeamName(MatchReview match) =>
            string.Equals(match.WinnerTeamName, match.BlueTeamName, StringComparison.Ordinal)
                ? match.RedTeamName
                : match.BlueTeamName;

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

        private static int GetReplayReviewPageCount(MatchReview match, ReplayReviewState replayState)
        {
            int messageCount = GetReplayReviewMessages(match, replayState).Count;
            return (int)Math.Ceiling(messageCount / (double)replayState.PageSize);
        }

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
