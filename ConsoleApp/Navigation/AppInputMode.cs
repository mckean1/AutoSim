namespace ConsoleApp.Navigation
{
    /// <summary>
    /// Defines how console input should be interpreted.
    /// </summary>
    internal enum AppInputMode
    {
        /// <summary>
        /// Interpret input as normal commands.
        /// </summary>
        Command,

        /// <summary>
        /// Interpret input as new game setup responses.
        /// </summary>
        NewGameSetup
    }
}
