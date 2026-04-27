using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;
using ConsoleApp.Constants;
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
        private readonly RoundAnalysisRenderer _roundAnalysisRenderer;
        private readonly AggregateRoundAnalysisRenderer _aggregateRoundAnalysisRenderer;
        private readonly AggregateRoundAnalyzer _aggregateRoundAnalyzer;
        private readonly string _logDirectory;
        private readonly RoundLogAnalyzer _roundLogAnalyzer;
        private readonly RoundLogReader _roundLogReader;
        private readonly RoundLogWriter _roundLogWriter;
        private readonly RoundReportWriter _roundReportWriter;
        private readonly RoundSummaryRenderer _roundSummaryRenderer;
        private readonly Func<int> _seedProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleApplication"/> class.
        /// </summary>
        public ConsoleApplication(string logDirectory = "logs/rounds", Func<int>? seedProvider = null)
        {
            _command = string.Empty;
            _previousCommand = string.Empty;
            _logDirectory = logDirectory;
            _roundAnalysisRenderer = new RoundAnalysisRenderer();
            _aggregateRoundAnalysisRenderer = new AggregateRoundAnalysisRenderer();
            _aggregateRoundAnalyzer = new AggregateRoundAnalyzer();
            _roundLogAnalyzer = new RoundLogAnalyzer();
            _roundLogReader = new RoundLogReader();
            _roundLogWriter = new RoundLogWriter(logDirectory);
            _roundReportWriter = new RoundReportWriter(logDirectory);
            _roundSummaryRenderer = new RoundSummaryRenderer();
            _seedProvider = seedProvider ?? (() => Environment.TickCount);
        }

        /// <summary>
        /// Runs the interactive console loop.
        /// </summary>
        public void Run()
        {
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
            Console.SetCursorPosition(0, 1);

            if (string.IsNullOrWhiteSpace(_command))
            {
                _command = _previousCommand;
            }

            if (IsExitCommand())
            {
                Console.WriteLine("Exiting the application.");
                Environment.Exit(0);
            }

            Console.Write(ExecuteCommand(_command));

            _previousCommand = _command;
            _command = string.Empty;
        }

        /// <summary>
        /// Executes a command and returns console output.
        /// </summary>
        /// <param name="command">The command text.</param>
        /// <returns>The command output.</returns>
        public string ExecuteCommand(string command)
        {
            _command = command ?? string.Empty;

            if (IsStartCommand())
            {
                return StartMatch();
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
                return "  start match - Starts a match." + Environment.NewLine
                    + "  simulate rounds <number> - Simulates many rounds." + Environment.NewLine
                    + "  analyze round <log path> - Analyzes a saved round log." + Environment.NewLine
                    + "  analyze rounds - Analyzes all saved round logs." + Environment.NewLine
                    + "  exit  - Exits AutoSim." + Environment.NewLine;
            }

            return string.Empty;
        }

        private void HandleBackspace()
        {
            if (_command.Length > 0)
            {
                _command = _command[..^1];
            }
        }

        private void HandleCharacterInput(ConsoleKeyInfo key)
        {
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
            Console.SetCursorPosition(0, 0);
            Console.Write(new string(' ', Console.BufferWidth - 1));
            Console.SetCursorPosition(0, 0);
            Console.Write($"{ConsoleConstants.Prompt}{_command}");
        }

        private bool IsStartCommand() =>
            string.Equals(_command, ConsoleConstants.Start, StringComparison.Ordinal)
            || string.Equals(_command, ConsoleConstants.StartMatch, StringComparison.Ordinal);
        private bool IsAnalyzeRoundCommand() =>
            _command.StartsWith("analyze round ", StringComparison.OrdinalIgnoreCase);
        private bool IsAnalyzeRoundsCommand() =>
            string.Equals(_command, "analyze rounds", StringComparison.OrdinalIgnoreCase);
        private bool IsSimulateRoundsCommand() =>
            _command.StartsWith("simulate rounds", StringComparison.OrdinalIgnoreCase);
        private bool IsHelpCommand() => string.Equals(_command, ConsoleConstants.Help, StringComparison.Ordinal);
        private bool IsExitCommand() => string.Equals(_command, ConsoleConstants.Exit, StringComparison.Ordinal);
        private void Redraw() => Console.Clear();

        private string StartMatch()
        {
            int seed = _seedProvider();
            RoundRoster roster = CreateTemporaryRoundRoster(ChampionCatalog.GetDefaultChampions(), seed);
            RoundResult result = new RoundEngine().Simulate(roster, seed);
            string logPath = _roundLogWriter.WriteEvents(result.Events, seed);
            return _roundSummaryRenderer.Render("Blue Team", "Red Team", result, logPath);
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
