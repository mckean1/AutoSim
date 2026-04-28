namespace ConsoleApp.Screens
{
    /// <summary>
    /// Renders console frames in place by rewriting only changed lines.
    /// </summary>
    public sealed class ConsoleFrameRenderer
    {
        private readonly IConsoleFrameWriter _writer;
        private ConsoleFrame? _previousFrame;
        private int _previousHeight;
        private int _previousWidth;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleFrameRenderer"/> class.
        /// </summary>
        /// <param name="writer">The console frame writer.</param>
        public ConsoleFrameRenderer(IConsoleFrameWriter? writer = null)
        {
            _writer = writer ?? new SystemConsoleFrameWriter();
        }

        /// <summary>
        /// Gets the current console height.
        /// </summary>
        public int Height => _writer.Height;

        /// <summary>
        /// Gets the current console width.
        /// </summary>
        public int Width => _writer.Width;

        /// <summary>
        /// Renders a frame, forcing a full repaint after resize.
        /// </summary>
        /// <param name="frame">The frame to render.</param>
        public void Render(ConsoleFrame frame)
        {
            ArgumentNullException.ThrowIfNull(frame);

            if (_previousFrame is null
                || _previousWidth != _writer.Width
                || _previousHeight != _writer.Height
                || _previousFrame.Width != frame.Width
                || _previousFrame.Height != frame.Height)
            {
                ForceRender(frame);
                return;
            }

            int height = Math.Min(frame.Height, _writer.Height);
            for (int index = 0; index < height; index++)
            {
                string previousLine = GetLine(_previousFrame, index, frame.Width);
                string line = GetLine(frame, index, frame.Width);
                if (!string.Equals(previousLine, line, StringComparison.Ordinal))
                {
                    WriteLine(index, line, frame.Width);
                }
            }

            _previousFrame = frame;
        }

        /// <summary>
        /// Clears the viewport and renders every visible line.
        /// </summary>
        /// <param name="frame">The frame to render.</param>
        public void ForceRender(ConsoleFrame frame)
        {
            ArgumentNullException.ThrowIfNull(frame);

            _writer.Clear();
            int height = Math.Min(frame.Height, _writer.Height);
            for (int index = 0; index < height; index++)
            {
                WriteLine(index, GetLine(frame, index, frame.Width), frame.Width);
            }

            _previousFrame = frame;
            _previousWidth = _writer.Width;
            _previousHeight = _writer.Height;
        }

        /// <summary>
        /// Clears the previous frame so the next render is full.
        /// </summary>
        public void Reset()
        {
            _previousFrame = null;
            _previousWidth = 0;
            _previousHeight = 0;
        }

        /// <summary>
        /// Sets cursor visibility.
        /// </summary>
        /// <param name="isVisible">A value indicating whether the cursor is visible.</param>
        public void SetCursorVisible(bool isVisible) => _writer.CursorVisible = isVisible;

        private static string GetLine(ConsoleFrame frame, int index, int width) =>
            index < frame.Lines.Count ? Fit(frame.Lines[index], width) : new string(' ', width);

        private static string Fit(string value, int width)
        {
            if (width <= 0)
            {
                return string.Empty;
            }

            string safeValue = value ?? string.Empty;
            if (safeValue.Length > width)
            {
                safeValue = safeValue[..width];
            }

            return safeValue.PadRight(width);
        }

        private void WriteLine(int index, string line, int frameWidth)
        {
            int width = Math.Min(Math.Max(0, frameWidth), _writer.Width);
            if (width == 0 || index < 0 || index >= _writer.Height)
            {
                return;
            }

            _writer.SetCursorPosition(0, index);
            _writer.Write(Fit(line, width));
        }
    }
}
