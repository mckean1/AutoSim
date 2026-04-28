namespace ConsoleApp.Navigation
{
    /// <summary>
    /// Defines the available management screens in the console client.
    /// </summary>
    internal enum ScreenKind
    {
        /// <summary>
        /// The home dashboard screen.
        /// </summary>
        Home,

        /// <summary>
        /// The team details screen.
        /// </summary>
        Team,

        /// <summary>
        /// The league standings screen.
        /// </summary>
        League,

        /// <summary>
        /// The weekly schedule screen.
        /// </summary>
        Schedule,

        /// <summary>
        /// The match preview screen.
        /// </summary>
        MatchPreview,

        /// <summary>
        /// The draft screen.
        /// </summary>
        Draft,

        /// <summary>
        /// The draft summary screen.
        /// </summary>
        DraftSummary,

        /// <summary>
        /// The live replay screen.
        /// </summary>
        LiveReplay,

        /// <summary>
        /// The round summary screen.
        /// </summary>
        RoundSummary,

        /// <summary>
        /// The match summary screen.
        /// </summary>
        MatchSummary
    }
}
