namespace AutoSim.Domain.Enums
{
    /// <summary>
    /// Defines a champion's current round-level behavior.
    /// </summary>
    public enum ChampionIntent
    {
        /// <summary>
        /// The champion is advancing and farming in lane.
        /// </summary>
        Laning,

        /// <summary>
        /// The champion is returning to base to recover.
        /// </summary>
        Retreating
    }
}
