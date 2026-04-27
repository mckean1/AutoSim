namespace AutoSim.Domain.Interfaces
{
    /// <summary>
    /// Provides deterministic random values for match resolution.
    /// </summary>
    public interface IMatchRandom
    {
        /// <summary>
        /// Returns a non-negative random integer less than the specified maximum.
        /// </summary>
        /// <param name="exclusiveMax">The exclusive upper bound.</param>
        /// <returns>A random integer in the valid range.</returns>
        int Next(int exclusiveMax);
    }
}
