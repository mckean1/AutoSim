using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Interfaces;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Management.Services;
using AutoSim.Domain.Objects;
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
        private bool _isProcessingCommand;
        private readonly RoundAnalysisRenderer _roundAnalysisRenderer;
        private readonly AggregateRoundAnalysisRenderer _aggregateRoundAnalysisRenderer;
        private readonly AggregateRoundAnalyzer _aggregateRoundAnalyzer;
        private readonly string _logDirectory;
        private readonly RoundLogAnalyzer _roundLogAnalyzer;
        private readonly RoundLogReader _roundLogReader;
        private readonly RoundLogWriter _roundLogWriter;
        private readonly RoundReportWriter _roundReportWriter;
        private readonly SeasonProgressionService _seasonProgressionService;
        private readonly Func<int> _seedProvider;
        private readonly WorldGenerationService _worldGenerationService;
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
            _seasonProgressionService = new SeasonProgressionService(matchEngineWrapper);
            _seedProvider = seedProvider ?? (() => Environment.TickCount);
            _worldGenerationService = new WorldGenerationService();
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
            _isProcessingCommand = true;

            try
            {
                if (string.IsNullOrWhiteSpace(_command))
                {
                    _command = _previousCommand;
                }

                if (IsExitCommand())
                {
                    Console.WriteLine("Exiting the application.");
                    Environment.Exit(0);
                }

                if (IsStartGameCommand())
                {
                    Console.Write(StartGame(ReadOptionalValue("Coach Name"), ReadOptionalValue("Team Name")));
                }
                else
                {
                    Console.Write(ExecuteCommand(_command));
                }

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

            if (IsStartGameCommand())
            {
                return StartGame();
            }

            if (IsStartMatchCommand())
            {
                return StartMatch();
            }

            if (IsShowTeamCommand())
            {
                return ShowTeam();
            }

            if (IsShowLeagueCommand())
            {
                return ShowLeague();
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
                return "  start - Starts a new game." + Environment.NewLine
                    + "  show team - Shows your coach, team, league, division, and players." + Environment.NewLine
                    + "  show league - Shows your current league, divisions, teams, and standings." + Environment.NewLine
                    + "  start match - Resolves the current week of scheduled matches." + Environment.NewLine
                    + "  simulate rounds <number> - Simulates many rounds." + Environment.NewLine
                    + "  analyze round <log path> - Analyzes a saved round log." + Environment.NewLine
                    + "  analyze rounds - Analyzes all saved round logs." + Environment.NewLine
                    + "  exit  - Exits AutoSim." + Environment.NewLine;
            }

            return $"Unknown command: {command}" + Environment.NewLine
                + "Type help for available commands." + Environment.NewLine;
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
            Console.SetCursorPosition(0, 0);
            Console.Write(new string(' ', Console.BufferWidth - 1));
            Console.SetCursorPosition(0, 0);
            Console.Write($"{ConsoleConstants.Prompt}{_command}");
        }

        private bool IsStartGameCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.Start, StringComparison.OrdinalIgnoreCase);
        private bool IsStartMatchCommand() =>
            string.Equals(_command.Trim(), ConsoleConstants.StartMatch, StringComparison.OrdinalIgnoreCase);
        private bool IsShowLeagueCommand() => string.Equals(_command, "show league", StringComparison.OrdinalIgnoreCase);
        private bool IsShowTeamCommand() => string.Equals(_command, "show team", StringComparison.OrdinalIgnoreCase);
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
        private void Redraw() => Console.Clear();

        private string StartMatch()
        {
            if (_world is null)
            {
                return "No game has been started. Use start first." + Environment.NewLine;
            }

            bool? wasCursorVisible = HideCursor();
            int? statusLineTop = WriteTransientStatus("Simulating matches for the current week...");

            try
            {
                Team humanTeam = GetHumanTeam(_world);
                int resolvedWeek = _world.Season.CurrentWeek;
                SeasonProgressionResult result = _seasonProgressionService.ResolveCurrentWeek(_world);
                _world = result.World;
                MatchResult? humanResult = result.MatchResults
                    .FirstOrDefault(matchResult => IsHumanMatch(matchResult, humanTeam.Id, _world));

                string humanSummary = humanResult is null
                    ? "Your team did not have a scheduled match this week."
                    : RenderMatchResult(_world, humanResult);

                return $"Resolved week {resolvedWeek}: {result.MatchResults.Count} matches." + Environment.NewLine
                    + humanSummary + Environment.NewLine;
            }
            finally
            {
                ClearTransientStatus(statusLineTop);
                RestoreCursor(wasCursorVisible);
            }
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

        private static int? WriteTransientStatus(string message)
        {
            try
            {
                int statusLineTop = Console.CursorTop;
                Console.WriteLine(message);
                return statusLineTop;
            }
            catch (IOException)
            {
                return null;
            }
        }

        private static void ClearTransientStatus(int? statusLineTop)
        {
            if (statusLineTop is null)
            {
                return;
            }

            try
            {
                Console.SetCursorPosition(0, statusLineTop.Value);
                Console.Write(new string(' ', Console.BufferWidth - 1));
                Console.SetCursorPosition(0, statusLineTop.Value);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (IOException)
            {
            }
        }

        private string StartGame(string? coachName = null, string? teamName = null)
        {
            int seed = _seedProvider();
            _world = _worldGenerationService.CreateWorld(seed, coachName, teamName);
            Team humanTeam = GetHumanTeam(_world);
            Coach humanCoach = GetHumanCoach(_world);
            League humanLeague = GetTeamLeague(_world, humanTeam);
            Division humanDivision = humanLeague.Divisions.First(division => division.Id == humanTeam.DivisionId);

            return "New game started." + Environment.NewLine
                + $"Seed: {seed}" + Environment.NewLine
                + $"Coach: {humanCoach.Name}" + Environment.NewLine
                + $"Team: {humanTeam.Name}" + Environment.NewLine
                + $"League: {humanLeague.TierName} {humanLeague.Region} League" + Environment.NewLine
                + $"Division: {humanDivision.Name} Division" + Environment.NewLine;
        }

        private static bool? HideCursor()
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            try
            {
                bool wasCursorVisible = Console.CursorVisible;
                Console.CursorVisible = false;
                return wasCursorVisible;
            }
            catch (IOException)
            {
                return null;
            }
        }

        private static void RestoreCursor(bool? wasCursorVisible)
        {
            if (wasCursorVisible is null || !OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                Console.CursorVisible = wasCursorVisible.Value;
            }
            catch (IOException)
            {
            }
        }

        private static string? ReadOptionalValue(string label)
        {
            Console.Write($"{label} (blank to generate): ");
            string? value = Console.ReadLine();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private string ShowLeague()
        {
            if (_world is null)
            {
                return "No game has been started. Use start first." + Environment.NewLine;
            }

            Team humanTeam = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, humanTeam);
            List<string> lines =
            [
                $"{league.TierName} {league.Region} League",
                "Divisions:"
            ];

            foreach (Division division in league.Divisions)
            {
                lines.Add($"  {division.Name} Division");
                lines.AddRange(division.TeamIds.Select(teamId => $"    {FormatTeamName(_world, teamId)}"));
            }

            lines.Add("Standings:");
            int rank = 1;
            foreach (LeagueStanding standing in league.Standings)
            {
                lines.Add(
                    $"  {rank}. {FormatTeamName(_world, standing.TeamId)} "
                    + $"{standing.MatchWins}-{standing.MatchLosses}, Points {standing.Points}");
                rank++;
            }

            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        private string ShowTeam()
        {
            if (_world is null)
            {
                return "No game has been started. Use start first." + Environment.NewLine;
            }

            Coach coach = GetHumanCoach(_world);
            Team team = GetHumanTeam(_world);
            League league = GetTeamLeague(_world, team);
            Division division = league.Divisions.First(currentDivision => currentDivision.Id == team.DivisionId);
            IReadOnlyList<Player> players = _world.Players
                .Where(player => team.PlayerIds.Contains(player.Id))
                .OrderBy(player => player.PositionRole)
                .ToList();

            List<string> lines =
            [
                $"Coach: {coach.Name}",
                $"Team: {team.Name}",
                $"League: {league.TierName} {league.Region} League",
                $"Division: {division.Name} Division",
                "Players:"
            ];
            lines.AddRange(players.Select(player => $"  {player.PositionRole}: {player.Name}"));
            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
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
