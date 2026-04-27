using System.Text.Json;
using System.Text.Json.Serialization;
using AutoSim.Domain.Objects;

namespace ConsoleApp.Services
{
    /// <summary>
    /// Reads JSON Lines round event logs.
    /// </summary>
    public sealed class RoundLogReader
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Reads round events from a JSONL file.
        /// </summary>
        /// <param name="path">The round log path.</param>
        /// <returns>The parsed round events in file order.</returns>
        public IReadOnlyList<RoundEvent> ReadEvents(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new RoundLogReadException("Round log path is required.");
            }

            if (!File.Exists(path))
            {
                throw new RoundLogReadException($"Round log was not found: {path}");
            }

            List<RoundEvent> events = [];
            int lineNumber = 0;

            foreach (string line in File.ReadLines(path))
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    RoundEvent? roundEvent = JsonSerializer.Deserialize<RoundEvent>(line, JsonSerializerOptions);
                    if (roundEvent is null)
                    {
                        throw new RoundLogReadException($"Could not parse round log at line {lineNumber}.");
                    }

                    events.Add(roundEvent);
                }
                catch (JsonException exception)
                {
                    throw new RoundLogReadException(
                        $"Could not parse round log at line {lineNumber}.",
                        exception);
                }
            }

            return events;
        }
    }
}
