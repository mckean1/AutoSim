using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Interfaces;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Management.Services;
using AutoSim.Domain.Objects;
using ConsoleApp.Constants;
using ConsoleApp.Navigation;
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
                return ContinueMatch();
            }

            if (IsCancelCommand())
            {
                _pendingMatch = null;
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
        private bool IsCancelCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Cancel, StringComparison.OrdinalIgnoreCase);
        private bool IsContinueCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Continue, StringComparison.OrdinalIgnoreCase);
        private bool IsHomeCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Home, StringComparison.OrdinalIgnoreCase);
        private bool IsShowLeagueCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ShowLeague, StringComparison.OrdinalIgnoreCase);
        private bool IsShowOpponentCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ShowOpponent, StringComparison.OrdinalIgnoreCase);
        private bool IsShowScheduleCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ShowSchedule, StringComparison.OrdinalIgnoreCase);
        private bool IsShowTeamCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.ShowTeam, StringComparison.OrdinalIgnoreCase);
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
        private void Redraw() => _screenRenderer.Render(_currentScreenModel);

        private string ContinueMatch()
        {
            if (_world is null)
            {
                return RenderCurrentScreen("No game has been started. Use start first.");
            }

            if (_pendingMatch is null)
            {
                return RenderCurrentScreen("No match preview is active. Use start match first.");
            }

            Team humanTeam = GetHumanTeam(_world);
            int resolvedWeek = _world.Season.CurrentWeek;
            SeasonProgressionResult result = _seasonProgressionService.ResolveCurrentWeek(_world);
            _world = result.World;
            _pendingMatch = null;
            MatchResult? humanResult = result.MatchResults
                .FirstOrDefault(matchResult => IsHumanMatch(matchResult, humanTeam.Id, _world));

            string message = humanResult is null
                ? $"Resolved week {resolvedWeek}: Your team did not have a scheduled match."
                : $"Resolved week {resolvedWeek}: {FormatMatchResultSummary(_world, humanResult)}";

            return RenderHome(message);
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

            if (_pendingMatch is null)
            {
                return RenderCurrentScreen("No match preview is active.");
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            string opponentId = string.Equals(_pendingMatch.HomeTeamId, humanTeam.Id, StringComparison.Ordinal)
                ? _pendingMatch.AwayTeamId
                : _pendingMatch.HomeTeamId;
            Team opponent = league.Teams.First(team => string.Equals(team.Id, opponentId, StringComparison.Ordinal));
            _screenNavigationState.CurrentScreen = ScreenKind.MatchPreview;
            _currentScreenModel = BuildTeamScreen(_world, opponent, league, $"Opponent: {opponent.Name}");
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
            _screenNavigationState.CurrentScreen = ScreenKind.MatchPreview;
            _currentScreenModel = BuildMatchPreviewScreen(_world, humanTeam, league, match);
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
            return new ScreenRenderModel
            {
                Commands =
                [
                    ConsoleConstants.Continue,
                    ConsoleConstants.Cancel,
                    ConsoleConstants.ShowTeam,
                    ConsoleConstants.ShowOpponent
                ],
                ContentLines =
                [
                    $"Blue team: {FormatTeamName(world, match.HomeTeamId)}",
                    $"Red team: {FormatTeamName(world, match.AwayTeamId)}",
                    $"Match type: {match.MatchType}",
                    $"Best of: {match.BestOf}",
                    $"Week: {match.Week}",
                    string.Empty,
                    "Continue to resolve the current week. Live replay will come in a later pass."
                ],
                Header = BuildHeader(world, humanTeam, league),
                Message = message,
                Title = "Match Preview"
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

        private static string GetRecommendedAction(WorldState world, Team humanTeam) =>
            GetCurrentWeekHumanMatch(world, humanTeam) is null ? ConsoleConstants.ShowSchedule : ConsoleConstants.StartMatch;

        private static string FormatMatchResultSummary(WorldState world, MatchResult result) =>
            $"{FormatTeamName(world, result.WinningTeamId)} won "
            + $"{result.BlueRoundWins}-{result.RedRoundWins} over "
            + $"{FormatTeamName(world, result.LosingTeamId)}.";

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
