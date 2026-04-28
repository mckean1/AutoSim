namespace ConsoleApp.Screens
{
    /// <summary>
    /// Adapts <see cref="Console"/> to the frame writer abstraction.
    /// </summary>
    public sealed class SystemConsoleFrameWriter : IConsoleFrameWriter
    {
        private const int DefaultHeight = 25;
        private const int DefaultWidth = 80;

        /// <inheritdoc />
        public int Height => GetHeight();

        /// <inheritdoc />
        public int Width => GetWidth();

        /// <inheritdoc />
        public bool CursorVisible
        {
            set
            {
                try
                {
                    Console.CursorVisible = value;
                }
                catch (IOException)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            try
            {
                Console.Clear();
            }
            catch (IOException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <inheritdoc />
        public void SetCursorPosition(int left, int top)
        {
            try
            {
                Console.SetCursorPosition(left, top);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (IOException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <inheritdoc />
        public void Write(string value)
        {
            try
            {
                Console.Write(value);
            }
            catch (IOException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static int GetHeight()
        {
            try
            {
                return Console.WindowHeight > 0 ? Console.WindowHeight : DefaultHeight;
            }
            catch (IOException)
            {
                return DefaultHeight;
            }
        }

        private static int GetWidth()
        {
            try
            {
                return Console.WindowWidth > 0 ? Console.WindowWidth : DefaultWidth;
            }
            catch (IOException)
            {
                return DefaultWidth;
            }
        }
    }
}
