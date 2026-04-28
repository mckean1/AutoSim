namespace ConsoleApp.Screens
{
    /// <summary>
    /// Represents a fixed-size visible console frame.
    /// </summary>
    public sealed class ConsoleFrame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleFrame"/> class.
        /// </summary>
        /// <param name="lines">The visible frame lines.</param>
        /// <param name="width">The frame width.</param>
        /// <param name="height">The frame height.</param>
        public ConsoleFrame(IReadOnlyList<string> lines, int width, int height)
        {
            Lines = lines ?? throw new ArgumentNullException(nameof(lines));
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets the visible frame height.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the visible frame lines.
        /// </summary>
        public IReadOnlyList<string> Lines { get; }

        /// <summary>
        /// Gets the visible frame width.
        /// </summary>
        public int Width { get; }
    }
}
