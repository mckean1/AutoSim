namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Creates runtime champion instances from immutable definitions.
    /// </summary>
    public static class ChampionInstanceFactory
    {
        /// <summary>
        /// Creates a champion instance with initial runtime state.
        /// </summary>
        /// <param name="definition">The champion definition.</param>
        /// <param name="playerId">The owning player identifier.</param>
        /// <returns>A champion instance initialized from its definition.</returns>
        public static ChampionInstance Create(ChampionDefinition definition, string playerId) =>
            new ChampionInstance(definition, playerId);
    }
}
