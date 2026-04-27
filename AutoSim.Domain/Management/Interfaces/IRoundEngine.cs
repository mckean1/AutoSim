using AutoSim.Domain.Management.Models;
using CombatRoundResult = AutoSim.Domain.Objects.RoundResult;

namespace AutoSim.Domain.Management.Interfaces
{
    /// <summary>
    /// Simulates one validated drafted round.
    /// </summary>
    public interface IRoundEngine
    {
        /// <summary>
        /// Simulates one round.
        /// </summary>
        /// <param name="setup">The validated round setup.</param>
        /// <returns>The combat round result.</returns>
        CombatRoundResult Simulate(RoundSetup setup);
    }
}
