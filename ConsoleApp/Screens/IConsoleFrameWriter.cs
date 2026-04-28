namespace ConsoleApp.Screens
{
    /// <summary>
    /// Writes positioned frame updates to a console-like target.
    /// </summary>
    public interface IConsoleFrameWriter
    {
        /// <summary>
        /// Gets the current viewport height.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the current viewport width.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Sets a value indicating whether the cursor is visible.
        /// </summary>
        public bool CursorVisible { set; }

        /// <summary>
        /// Clears the visible console area.
        /// </summary>
        public void Clear();

        /// <summary>
        /// Moves the cursor to the given position.
        /// </summary>
        /// <param name="left">The column.</param>
        /// <param name="top">The row.</param>
        public void SetCursorPosition(int left, int top);

        /// <summary>
        /// Writes text at the current cursor position.
        /// </summary>
        /// <param name="value">The text to write.</param>
        public void Write(string value);
    }
}
