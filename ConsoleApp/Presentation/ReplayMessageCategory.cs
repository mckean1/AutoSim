namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Defines player-facing replay message categories.
    /// </summary>
    public enum ReplayMessageCategory
    {
        /// <summary>
        /// The round started.
        /// </summary>
        RoundStart,

        /// <summary>
        /// A fight event occurred.
        /// </summary>
        Fight,

        /// <summary>
        /// Damage was dealt.
        /// </summary>
        Damage,

        /// <summary>
        /// Healing was applied.
        /// </summary>
        Heal,

        /// <summary>
        /// A shield was applied.
        /// </summary>
        Shield,

        /// <summary>
        /// A champion was defeated.
        /// </summary>
        Kill,

        /// <summary>
        /// A champion respawned.
        /// </summary>
        Respawn,

        /// <summary>
        /// A champion retreated.
        /// </summary>
        Retreat,

        /// <summary>
        /// A champion returned to action.
        /// </summary>
        Return,

        /// <summary>
        /// A farming event occurred.
        /// </summary>
        Farm,

        /// <summary>
        /// A champion leveled up.
        /// </summary>
        LevelUp,

        /// <summary>
        /// An objective event occurred.
        /// </summary>
        Objective,

        /// <summary>
        /// The round ended.
        /// </summary>
        RoundEnd,

        /// <summary>
        /// The match ended.
        /// </summary>
        MatchEnd
    }
}
