namespace ConsoleApp.Navigation
{
    /// <summary>
    /// Stores in-progress values for new game setup.
    /// </summary>
    internal sealed class NewGameSetupState
    {
        /// <summary>
        /// Gets or sets the current setup step.
        /// </summary>
        public NewGameSetupStep Step { get; set; }

        /// <summary>
        /// Gets or sets the coach name.
        /// </summary>
        public string? CoachName { get; set; }

        /// <summary>
        /// Gets or sets the team name.
        /// </summary>
        public string? TeamName { get; set; }
    }
}
