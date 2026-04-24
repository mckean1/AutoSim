using ConsoleApp.Constants;
using ConsoleApp.Enums;

namespace ConsoleApp.Objects
{
    /// <summary>
    /// Represents the interactive console application loop.
    /// </summary>
    public sealed class ConsoleApplication
    {
        private string _command;
        private string _previousCommand;
        private ScreenState _screenState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleApplication"/> class.
        /// </summary>
        public ConsoleApplication()
        {
            _command = string.Empty;
            _previousCommand = string.Empty;
            _screenState = ScreenState.Initialization;
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

            if (_screenState == ScreenState.Initialization && IsStartCommand())
            {
                _screenState = ScreenState.Main;
            }

            if (_screenState == ScreenState.Initialization && IsHelpCommand())
            {
                Console.WriteLine($"  start - Starts the game.");
                Console.WriteLine($"  exit  - Exits AutoSim.");
            }

            if (_screenState == ScreenState.Main && IsHelpCommand())
            {
                Console.WriteLine($"  exit  - Exits AutoSim.");
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

        private bool IsStartCommand() => string.Equals(_command, ConsoleConstants.Start, StringComparison.Ordinal);
        private bool IsHelpCommand() => string.Equals(_command, ConsoleConstants.Help, StringComparison.Ordinal);
        private bool IsExitCommand() => string.Equals(_command, ConsoleConstants.Exit, StringComparison.Ordinal);
        private void Redraw() => Console.Clear();
    }
}
