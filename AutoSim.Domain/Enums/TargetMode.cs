namespace AutoSim.Domain.Enums
{
    /// <summary>
    /// Defines the candidate pool for a combat effect.
    /// </summary>
    public enum TargetMode
    {
        /// <summary>
        /// Targets active enemy frontline champions, falling back to backline champions.
        /// </summary>
        EnemyFrontline,

        /// <summary>
        /// Targets active enemy backline champions, falling back to frontline champions.
        /// </summary>
        EnemyBackline,

        /// <summary>
        /// Targets any active enemy champion.
        /// </summary>
        EnemyAny,

        /// <summary>
        /// Targets active allied frontline champions, falling back to backline champions.
        /// </summary>
        AllyFrontline,

        /// <summary>
        /// Targets active allied backline champions, falling back to frontline champions.
        /// </summary>
        AllyBackline,

        /// <summary>
        /// Targets any active allied champion.
        /// </summary>
        AllyAny,

        /// <summary>
        /// Targets any living enemy champion in the full roster.
        /// </summary>
        GlobalEnemy,

        /// <summary>
        /// Targets any living allied champion in the full roster.
        /// </summary>
        GlobalAlly,

        /// <summary>
        /// Targets any living champion in the full roster.
        /// </summary>
        GlobalAll,

        /// <summary>
        /// Targets only the source champion.
        /// </summary>
        Self
    }
}
