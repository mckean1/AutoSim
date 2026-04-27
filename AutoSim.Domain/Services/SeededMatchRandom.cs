using AutoSim.Domain.Interfaces;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Provides deterministic match random values from a fixed seed.
    /// </summary>
    public sealed class SeededMatchRandom : IMatchRandom
    {
        private readonly Random _random;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeededMatchRandom"/> class.
        /// </summary>
        /// <param name="seed">The deterministic seed.</param>
        public SeededMatchRandom(int seed)
        {
            _random = new Random(seed);
        }

        /// <inheritdoc />
        public int Next(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMax),
                    exclusiveMax,
                    "Exclusive maximum must be greater than zero.");
            }

            return _random.Next(exclusiveMax);
        }

        /// <inheritdoc />
        public double NextDouble() => _random.NextDouble();
    }
}
