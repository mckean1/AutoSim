namespace ConsoleApp.Services
{
    /// <summary>
    /// Represents the status of a week simulation session.
    /// </summary>
    public enum WeekSimulationStatus
    {
        /// <summary>
        /// The simulation is currently running.
        /// </summary>
        Running,

        /// <summary>
        /// The simulation completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// The simulation failed with an error.
        /// </summary>
        Failed,

        /// <summary>
        /// The simulation was cancelled.
        /// </summary>
        Cancelled
    }
}
