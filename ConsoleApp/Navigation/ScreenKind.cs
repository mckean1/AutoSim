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
        /// The player details screen.
        /// </summary>
        PlayerDetail,

        /// <summary>
        /// The league standings screen.
        /// </summary>
        League,

        /// <summary>
        /// The playoff picture screen.
        /// </summary>
        Playoffs,

        /// <summary>
        /// The weekly schedule screen.
        /// </summary>
        Schedule,

        /// <summary>
        /// The match preview screen.
        /// </summary>
        MatchPreview,

        /// <summary>
        /// The live replay preparation screen.
        /// </summary>
        ReplayPreparation,

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
        MatchSummary,

        /// <summary>
        /// The champion catalog screen.
        /// </summary>
        ChampionCatalog,

        /// <summary>
        /// The champion detail screen.
        /// </summary>
        ChampionDetail,

        /// <summary>
        /// The last match review screen.
        /// </summary>
        LastMatchReview,

        /// <summary>
        /// The round list review screen.
        /// </summary>
        RoundList,

        /// <summary>
        /// The detailed round review screen.
        /// </summary>
        RoundReview,

        /// <summary>
        /// The paged replay review screen.
        /// </summary>
        ReplayReview,

        /// <summary>
        /// The help and command reference screen.
        /// </summary>
        Help,

        /// <summary>
        /// The new game setup screen.
        /// </summary>
        NewGameSetup
    }
}
