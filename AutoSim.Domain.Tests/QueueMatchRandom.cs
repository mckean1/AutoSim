using AutoSim.Domain.Interfaces;

namespace AutoSim.Domain.Tests
{
    internal sealed class QueueMatchRandom : IMatchRandom
    {
        private readonly Queue<int> _values;

        public QueueMatchRandom(params int[] values)
        {
            _values = new Queue<int>(values);
        }

        public int Next(int exclusiveMax)
        {
            int value = _values.Count > 0 ? _values.Dequeue() : 0;

            if (value < 0 || value >= exclusiveMax)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMax),
                    value,
                    "Queued random value is out of range.");
            }

            return value;
        }

        public double NextDouble() => Next(10000) / 10000.0;
    }
}
