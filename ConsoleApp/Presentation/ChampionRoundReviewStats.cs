namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Represents available champion stats for a reviewed round.
    /// </summary>
    public sealed record ChampionRoundReviewStats
    {
        /// <summary>
        /// Gets the champion display name.
        /// </summary>
        public required string ChampionName { get; init; }

        /// <summary>
        /// Gets the team display name.
        /// </summary>
        public required string TeamName { get; init; }

        /// <summary>
        /// Gets kills credited to the champion.
        /// </summary>
        public int Kills { get; init; }

        /// <summary>
        /// Gets deaths credited to the champion.
        /// </summary>
        public int Deaths { get; init; }

        /// <summary>
        /// Gets damage dealt by the champion.
        /// </summary>
        public int DamageDealt { get; init; }

        /// <summary>
        /// Gets healing done by the champion.
        /// </summary>
        public int HealingDone { get; init; }

        /// <summary>
        /// Gets shielding done by the champion.
        /// </summary>
        public int ShieldingDone { get; init; }
    }
}
