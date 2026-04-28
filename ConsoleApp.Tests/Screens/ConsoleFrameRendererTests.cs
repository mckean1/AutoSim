using ConsoleApp.Screens;

namespace ConsoleApp.Tests.Screens
{
    internal sealed class ConsoleFrameRendererTests
    {
        [Test]
        public void Render_FirstFrame_WritesAllLines()
        {
            FakeConsoleFrameWriter writer = new(width: 12, height: 3);
            ConsoleFrameRenderer renderer = new(writer);
            ConsoleFrame frame = new(["alpha", "beta", "gamma"], 12, 3);

            renderer.Render(frame);

            Assert.Multiple(() =>
            {
                Assert.That(writer.ClearCount, Is.EqualTo(1));
                Assert.That(writer.Writes, Has.Count.EqualTo(3));
                Assert.That(writer.Writes[0], Is.EqualTo((0, 0, "alpha       ")));
                Assert.That(writer.Writes[1], Is.EqualTo((0, 1, "beta        ")));
                Assert.That(writer.Writes[2], Is.EqualTo((0, 2, "gamma       ")));
            });
        }

        [Test]
        public void Render_IdenticalSecondFrame_WritesNothing()
        {
            FakeConsoleFrameWriter writer = new(width: 12, height: 3);
            ConsoleFrameRenderer renderer = new(writer);
            ConsoleFrame frame = new(["alpha", "beta", "gamma"], 12, 3);
            renderer.Render(frame);
            writer.ResetWrites();

            renderer.Render(frame);

            Assert.That(writer.Writes, Is.Empty);
        }

        [Test]
        public void Render_ChangedLine_RewritesOnlyChangedLine()
        {
            FakeConsoleFrameWriter writer = new(width: 12, height: 3);
            ConsoleFrameRenderer renderer = new(writer);
            renderer.Render(new ConsoleFrame(["alpha", "beta", "gamma"], 12, 3));
            writer.ResetWrites();

            renderer.Render(new ConsoleFrame(["alpha", "delta", "gamma"], 12, 3));

            Assert.That(writer.Writes, Is.EqualTo(new[] { (0, 1, "delta       ") }));
        }

        [Test]
        public void Render_ShorterChangedLine_ClearsLeftoverCharacters()
        {
            FakeConsoleFrameWriter writer = new(width: 12, height: 2);
            ConsoleFrameRenderer renderer = new(writer);
            renderer.Render(new ConsoleFrame(["long-value", "stable"], 12, 2));
            writer.ResetWrites();

            renderer.Render(new ConsoleFrame(["short", "stable"], 12, 2));

            Assert.That(writer.Writes, Is.EqualTo(new[] { (0, 0, "short       ") }));
        }

        [Test]
        public void Render_ResizeTriggersFullRedraw()
        {
            FakeConsoleFrameWriter writer = new(width: 12, height: 2);
            ConsoleFrameRenderer renderer = new(writer);
            renderer.Render(new ConsoleFrame(["alpha", "beta"], 12, 2));
            writer.ResetWrites();
            writer.Width = 14;

            renderer.Render(new ConsoleFrame(["alpha", "beta"], 14, 2));

            Assert.Multiple(() =>
            {
                Assert.That(writer.ClearCount, Is.EqualTo(2));
                Assert.That(writer.Writes, Has.Count.EqualTo(2));
                Assert.That(writer.Writes[0], Is.EqualTo((0, 0, "alpha         ")));
                Assert.That(writer.Writes[1], Is.EqualTo((0, 1, "beta          ")));
            });
        }

        private sealed class FakeConsoleFrameWriter : IConsoleFrameWriter
        {
            private int _left;
            private int _top;

            public FakeConsoleFrameWriter(int width, int height)
            {
                Width = width;
                Height = height;
            }

            public int ClearCount { get; private set; }

            public int Height { get; set; }

            public int Width { get; set; }

            public bool CursorVisible { get; set; } = true;

            public List<(int Left, int Top, string Value)> Writes { get; } = [];

            public void Clear() => ClearCount++;

            public void ResetWrites() => Writes.Clear();

            public void SetCursorPosition(int left, int top)
            {
                _left = left;
                _top = top;
            }

            public void Write(string value) => Writes.Add((_left, _top, value));
        }
    }
}
