namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Defines a champion's basic attack.
    /// </summary>
    public sealed record ChampionAttack
    {
        /// <summary>
        /// Gets the effects applied when the attack is used.
        /// </summary>
        public required IReadOnlyList<AttackEffect> Effects { get; init; }
    }
}
