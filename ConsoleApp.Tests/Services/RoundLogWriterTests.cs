using System.Text.Json;
using System.Text.Json.Serialization;
using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using ConsoleApp.Services;

namespace ConsoleApp.Tests.Services
{
    internal sealed class RoundLogWriterTests
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        [Test]
        public void WriteEvents_MissingDirectory_CreatesDirectoryAndReturnsWrittenPath()
        {
            string directory = Path.Combine(Path.GetTempPath(), $"autosim-round-logs-{Guid.NewGuid():N}");
            RoundLogWriter writer = new(directory);

            string path = writer.WriteEvents(CreateEvents(), seed: 123);

            Assert.Multiple(() =>
            {
                Assert.That(Directory.Exists(directory), Is.True);
                Assert.That(File.Exists(path), Is.True);
                Assert.That(path, Does.Contain("seed-123"));
            });
        }

        [Test]
        public void WriteEvents_Events_WritesOneJsonObjectPerLineThatCanBeParsed()
        {
            string directory = Path.Combine(Path.GetTempPath(), $"autosim-round-logs-{Guid.NewGuid():N}");
            RoundLogWriter writer = new(directory);

            string path = writer.WriteEvents(CreateEvents());
            string[] lines = File.ReadAllLines(path);
            RoundEvent[] parsedEvents = lines
                .Select(line => JsonSerializer.Deserialize<RoundEvent>(line, JsonSerializerOptions)!)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(lines, Has.Length.EqualTo(2));
                Assert.That(parsedEvents[0].Type, Is.EqualTo(RoundEventType.RoundStarted));
                Assert.That(parsedEvents[1].Message, Is.EqualTo("Round ended."));
                Assert.That(parsedEvents[1].SourceTeamSide, Is.EqualTo(TeamSide.Blue.ToString()));
                Assert.That(lines[1], Does.Contain("sourceTeamSide"));
                Assert.That(lines[1], Does.Contain("fightId"));
            });
        }

        [Test]
        public void WriteEvents_EmptyEventList_CreatesEmptyLogFile()
        {
            string directory = Path.Combine(Path.GetTempPath(), $"autosim-round-logs-{Guid.NewGuid():N}");
            RoundLogWriter writer = new(directory);

            string path = writer.WriteEvents([]);

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(path), Is.True);
                Assert.That(File.ReadAllText(path), Is.Empty);
            });
        }

        [Test]
        public void WriteEvents_RepeatedSeed_ReturnsUniquePaths()
        {
            string directory = Path.Combine(Path.GetTempPath(), $"autosim-round-logs-{Guid.NewGuid():N}");
            RoundLogWriter writer = new(directory);

            string firstPath = writer.WriteEvents(CreateEvents(), seed: 123);
            string secondPath = writer.WriteEvents(CreateEvents(), seed: 123);

            Assert.Multiple(() =>
            {
                Assert.That(secondPath, Is.Not.EqualTo(firstPath));
                Assert.That(File.Exists(firstPath), Is.True);
                Assert.That(File.Exists(secondPath), Is.True);
            });
        }

        private static IReadOnlyList<RoundEvent> CreateEvents() =>
        [
            new RoundEvent
            {
                TimeSeconds = 0,
                Type = RoundEventType.RoundStarted,
                Message = "Round started."
            },
            new RoundEvent
            {
                TimeSeconds = 1,
                Type = RoundEventType.RoundEnded,
                TeamSide = TeamSide.Blue.ToString(),
                SourceTeamSide = TeamSide.Blue.ToString(),
                FightId = Guid.NewGuid(),
                Message = "Round ended."
            }
        ];
    }
}
