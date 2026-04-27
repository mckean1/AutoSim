using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoSim.Domain.Objects;

namespace ConsoleApp.Services
{
    /// <summary>
    /// Writes structured round events to JSON Lines files.
    /// </summary>
    public sealed class RoundLogWriter
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly string _logDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoundLogWriter"/> class.
        /// </summary>
        /// <param name="logDirectory">The directory where round logs are written.</param>
        public RoundLogWriter(string logDirectory = "logs/rounds")
        {
            _logDirectory = string.IsNullOrWhiteSpace(logDirectory)
                ? throw new ArgumentException("Log directory is required.", nameof(logDirectory))
                : logDirectory;
        }

        /// <summary>
        /// Writes events to a JSONL file.
        /// </summary>
        /// <param name="events">The round events to write.</param>
        /// <param name="seed">The optional deterministic seed.</param>
        /// <returns>The written file path.</returns>
        public string WriteEvents(IEnumerable<RoundEvent> events, int? seed = null)
        {
            ArgumentNullException.ThrowIfNull(events);

            Directory.CreateDirectory(_logDirectory);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fffffff", CultureInfo.InvariantCulture);
            string seedSuffix = seed.HasValue ? $"_seed-{seed.Value}" : string.Empty;
            string uniqueSuffix = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            string path = Path.Combine(_logDirectory, $"round_{timestamp}{seedSuffix}_{uniqueSuffix}.jsonl");

            using StreamWriter writer = new(path);
            foreach (RoundEvent roundEvent in events)
            {
                writer.WriteLine(JsonSerializer.Serialize(roundEvent, JsonSerializerOptions));
            }

            return path;
        }
    }
}
