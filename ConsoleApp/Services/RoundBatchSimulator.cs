using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;
using ConsoleApp.Objects;

namespace ConsoleApp.Services
{
    /// <summary>
    /// Runs batches of independent round simulations.
    /// </summary>
    public sealed class RoundBatchSimulator
    {
        private readonly AggregateRoundAnalyzer _aggregateAnalyzer;
        private readonly RoundLogAnalyzer _roundLogAnalyzer;
        private readonly RoundLogWriter _roundLogWriter;
        private readonly TemporaryRoundRosterFactory _temporaryRoundRosterFactory;

        public RoundBatchSimulator(RoundLogWriter roundLogWriter)
        {
            _roundLogWriter = roundLogWriter ?? throw new ArgumentNullException(nameof(roundLogWriter));
            _roundLogAnalyzer = new RoundLogAnalyzer();
            _aggregateAnalyzer = new AggregateRoundAnalyzer();
            _temporaryRoundRosterFactory = new TemporaryRoundRosterFactory();
        }

        public AggregateRoundAnalysis Simulate(int count, int baseSeed)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Round count must be greater than zero.");
            }

            IReadOnlyList<ChampionDefinition> catalog = ChampionCatalog.GetDefaultChampions();
            List<RoundAnalysis> analyses = [];
            for (int index = 0; index < count; index++)
            {
                int seed = baseSeed + index;
                RoundRoster roster = _temporaryRoundRosterFactory.Create(catalog, seed);
                RoundResult result = new RoundEngine().Simulate(roster, seed);
                _roundLogWriter.WriteEvents(result.Events, seed);
                analyses.Add(_roundLogAnalyzer.Analyze(result.Events));
            }

            return _aggregateAnalyzer.Analyze(analyses, count);
        }
    }
}
