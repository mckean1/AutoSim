namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Represents a temporary shield on a runtime champion instance.
    /// </summary>
    public sealed class ActiveShield
    {
        /// <summary>
        /// Gets or sets the remaining damage absorption amount.
        /// </summary>
        public required int Amount { get; set; }

        /// <summary>
        /// Gets or sets the remaining shield duration in seconds.
        /// </summary>
        public required double Duration { get; set; }
    }
}
