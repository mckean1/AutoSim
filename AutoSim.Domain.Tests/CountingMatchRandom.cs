using AutoSim.Domain.Interfaces;

namespace AutoSim.Domain.Tests
{
    internal sealed class CountingMatchRandom : IMatchRandom
    {
        public int NextCalls { get; private set; }

        public int NextDoubleCalls { get; private set; }

        public int Next(int exclusiveMax)
        {
            NextCalls++;
            return 0;
        }

        public double NextDouble()
        {
            NextDoubleCalls++;
            return 0;
        }
    }
}
