namespace AutoSim.Domain.Enums
{
    /// <summary>
    /// Defines the season context for a scheduled match.
    /// </summary>
    public enum MatchType
    {
        /// <summary>
        /// A regular-season league match.
        /// </summary>
        RegularSeason,

        /// <summary>
        /// A league quarterfinal playoff match.
        /// </summary>
        LeagueQuarterfinal,

        /// <summary>
        /// A league semifinal playoff match.
        /// </summary>
        LeagueSemifinal,

        /// <summary>
        /// A league final playoff match.
        /// </summary>
        LeagueFinal,

        /// <summary>
        /// A world championship semifinal match.
        /// </summary>
        WorldChampionshipSemifinal,

        /// <summary>
        /// A world championship final match.
        /// </summary>
        WorldChampionshipFinal,

        /// <summary>
        /// A non-season testing match.
        /// </summary>
        Exhibition
    }
}
