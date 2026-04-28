namespace ConsoleApp.Screens
{
    /// <summary>
    /// Defines the shared screen header values.
    /// </summary>
    internal sealed record ScreenHeaderModel
    {
        /// <summary>
        /// Gets the first left-aligned header value.
        /// </summary>
        public string PrimaryLeft { get; init; } = "AutoSim";

        /// <summary>
        /// Gets the first right-aligned header value.
        /// </summary>
        public string PrimaryRight { get; init; } = string.Empty;

        /// <summary>
        /// Gets the second left-aligned header value.
        /// </summary>
        public string SecondaryLeft { get; init; } = string.Empty;

        /// <summary>
        /// Gets the second right-aligned header value.
        /// </summary>
        public string SecondaryRight { get; init; } = string.Empty;
    }
}
