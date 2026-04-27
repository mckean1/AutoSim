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
        private readonly RoundLogWriter _roundLogWriter;
        private readonly RoundSummaryRenderer _roundSummaryRenderer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleApplication"/> class.
        /// </summary>
        public ConsoleApplication()
        {
            _command = string.Empty;
            _previousCommand = string.Empty;
            _roundLogWriter = new RoundLogWriter();
            _roundSummaryRenderer = new RoundSummaryRenderer();
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

            if (IsStartCommand())
            {
                StartMatch();
                _previousCommand = _command;
                _command = string.Empty;
                return;
            }

            if (IsHelpCommand())
            {
                Console.WriteLine("  start match - Starts a match.");
                Console.WriteLine("  exit  - Exits AutoSim.");
            }

            _previousCommand = _command;
            _command = string.Empty;
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
        private bool IsHelpCommand() => string.Equals(_command, ConsoleConstants.Help, StringComparison.Ordinal);
        private bool IsExitCommand() => string.Equals(_command, ConsoleConstants.Exit, StringComparison.Ordinal);
        private void Redraw() => Console.Clear();

        private void StartMatch()
        {
            int seed = Environment.TickCount;
            RoundRoster roster = CreateTemporaryRoundRoster(ChampionCatalog.GetDefaultChampions(), seed);
            RoundResult result = new RoundEngine().Simulate(roster, seed);
            string logPath = _roundLogWriter.WriteEvents(result.Events, seed);
            string summary = _roundSummaryRenderer.Render("Blue Team", "Red Team", result, logPath);

            Console.Write(summary);
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
