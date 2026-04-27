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
            IReadOnlyList<ChampionDefinition> catalog = ChampionCatalog.GetDefaultChampions();
            IReadOnlyList<ChampionDefinition> blueRoster = catalog.ToList();
            IReadOnlyList<ChampionDefinition> redRoster = catalog.Reverse().ToList();
            RoundResult result = new RoundEngine().Simulate(blueRoster, redRoster, seed);
            string logPath = _roundLogWriter.WriteEvents(result.Events, seed);
            string summary = _roundSummaryRenderer.Render("Blue Team", "Red Team", result, logPath);

            Console.Write(summary);
        }
    }
}
