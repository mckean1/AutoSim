using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using ConsoleApp.Services;

namespace ConsoleApp.Tests.Services
{
    internal sealed class RoundLogReaderTests
    {
        [Test]
        public void ReadEvents_ValidJsonLines_ReturnsEventsInOrder()
        {
            string path = CreateTempLog(
                """{"timeSeconds":0,"type":"RoundStarted","message":"Round started."}""",
                """{"timeSeconds":1,"type":"RoundEnded","message":"Round ended.","sourceTeamSide":"Blue"}""");

            IReadOnlyList<RoundEvent> events = new RoundLogReader().ReadEvents(path);

            Assert.Multiple(() =>
            {
                Assert.That(events, Has.Count.EqualTo(2));
                Assert.That(events[0].Type, Is.EqualTo(RoundEventType.RoundStarted));
                Assert.That(events[1].Type, Is.EqualTo(RoundEventType.RoundEnded));
            });
        }

        [Test]
        public void ReadEvents_BlankLines_IgnoresBlankLines()
        {
            string path = CreateTempLog(
                string.Empty,
                """{"timeSeconds":0,"type":"RoundStarted","message":"Round started."}""",
                "   ");

            IReadOnlyList<RoundEvent> events = new RoundLogReader().ReadEvents(path);

            Assert.That(events, Has.Count.EqualTo(1));
        }

        [Test]
        public void ReadEvents_EmptyFile_ReturnsEmptyEvents()
        {
            string path = CreateTempLog();

            IReadOnlyList<RoundEvent> events = new RoundLogReader().ReadEvents(path);

            Assert.That(events, Is.Empty);
        }

        [Test]
        public void ReadEvents_MalformedJson_ThrowsFriendlyLineError()
        {
            string path = CreateTempLog(
                """{"timeSeconds":0,"type":"RoundStarted","message":"Round started."}""",
                "{bad json");

            RoundLogReadException exception = Assert.Throws<RoundLogReadException>(
                () => new RoundLogReader().ReadEvents(path))!;

            Assert.That(exception.Message, Does.Contain("line 2"));
        }

        [Test]
        public void ReadEvents_MissingFile_ThrowsFriendlyError()
        {
            string path = Path.Combine(Path.GetTempPath(), $"missing-round-{Guid.NewGuid():N}.jsonl");

            RoundLogReadException exception = Assert.Throws<RoundLogReadException>(
                () => new RoundLogReader().ReadEvents(path))!;

            Assert.That(exception.Message, Does.Contain("was not found"));
        }

        private static string CreateTempLog(params string[] lines)
        {
            string path = Path.Combine(Path.GetTempPath(), $"autosim-round-log-{Guid.NewGuid():N}.jsonl");
            File.WriteAllLines(path, lines);
            return path;
        }
    }
}
