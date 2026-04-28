namespace ConsoleApp.Navigation
{
    /// <summary>
    /// Defines validated values used to create a new game.
    /// </summary>
    internal sealed class NewGameOptions
    {
        /// <summary>
        /// Gets the coach name.
        /// </summary>
        public required string CoachName { get; init; }

        /// <summary>
        /// Gets the team name.
        /// </summary>
        public required string TeamName { get; init; }
    }
}
