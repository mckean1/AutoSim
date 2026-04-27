using AutoSim.Domain.Management.Interfaces;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Objects;
using CombatRoundResult = AutoSim.Domain.Objects.RoundResult;

namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Adapts the combat RoundEngine to the Management Layer round engine contract.
    /// </summary>
    public sealed class RoundEngineAdapter : IRoundEngine
    {
        private readonly AutoSim.Domain.Services.RoundEngine _roundEngine;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoundEngineAdapter"/> class.
        /// </summary>
        /// <param name="roundEngine">The combat round engine.</param>
        public RoundEngineAdapter(AutoSim.Domain.Services.RoundEngine? roundEngine = null)
        {
            _roundEngine = roundEngine ?? new AutoSim.Domain.Services.RoundEngine();
        }

        /// <inheritdoc />
        public CombatRoundResult Simulate(RoundSetup setup)
        {
            ArgumentNullException.ThrowIfNull(setup);

            return _roundEngine.Simulate(new RoundRoster
            {
                BlueChampions = setup.BlueChampions,
                RedChampions = setup.RedChampions
            }, setup.Seed);
        }
    }
}
