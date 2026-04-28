using System.Text;
using ConsoleApp.Constants;

namespace ConsoleApp.Screens
{
    /// <summary>
    /// Renders full-screen console layouts for management screens.
    /// </summary>
    internal sealed class ConsoleScreenRenderer
    {
        private const int DefaultWidth = 80;
        private const int MinimumWidth = 40;

        /// <summary>
        /// Renders a screen directly to the console.
        /// </summary>
        /// <param name="model">The screen render model.</param>
        /// <param name="command">The current command text.</param>
        public void Render(ScreenRenderModel model, string command = "")
        {
            ArgumentNullException.ThrowIfNull(model);

            Console.Clear();
            Console.Write(RenderToString(model, GetConsoleWidth(), command));
        }

        /// <summary>
        /// Renders a screen model to text.
        /// </summary>
        /// <param name="model">The screen render model.</param>
        /// <param name="width">The target console width.</param>
        /// <param name="command">The current command text.</param>
        /// <returns>The rendered screen text.</returns>
        public string RenderToString(ScreenRenderModel model, int width = DefaultWidth, string command = "")
        {
            ArgumentNullException.ThrowIfNull(model);

            int safeWidth = Math.Max(MinimumWidth, width);
            int innerWidth = safeWidth - 2;
            StringBuilder builder = new();

            AppendBorder(builder, '╔', '═', '╗', innerWidth);
            AppendContentRow(
                builder,
                BuildSplitLine(model.Header.PrimaryLeft, model.Header.PrimaryRight, GetContentWidth(innerWidth)),
                innerWidth);
            AppendContentRow(
                builder,
                BuildSplitLine(model.Header.SecondaryLeft, model.Header.SecondaryRight, GetContentWidth(innerWidth)),
                innerWidth);
            AppendBorder(builder, '╠', '═', '╣', innerWidth);

            AppendContentRow(builder, model.Title, innerWidth);
            if (!string.IsNullOrWhiteSpace(model.Message))
            {
                AppendContentRow(builder, model.Message, innerWidth);
                AppendContentRow(builder, string.Empty, innerWidth);
            }

            foreach (string line in model.ContentLines)
            {
                AppendContentRow(builder, line, innerWidth);
            }

            AppendBorder(builder, '╠', '═', '╣', innerWidth);
            AppendContentRow(builder, $"Commands: {string.Join(" | ", model.Commands)}", innerWidth);
            AppendContentRow(builder, $"{ConsoleConstants.Prompt}{command}", innerWidth);
            AppendBorder(builder, '╚', '═', '╝', innerWidth);

            return builder.ToString();
        }

        private static void AppendBorder(StringBuilder builder, char left, char fill, char right, int innerWidth) =>
            builder.Append(left).Append(new string(fill, innerWidth)).Append(right).AppendLine();

        private static void AppendContentRow(StringBuilder builder, string value, int innerWidth) =>
            AppendRow(builder, FitWithPadding(value, innerWidth));

        private static void AppendRow(StringBuilder builder, string value) =>
            builder.Append('║').Append(value).Append('║').AppendLine();

        private static string BuildSplitLine(string left, string right, int innerWidth)
        {
            string safeLeft = left ?? string.Empty;
            string safeRight = right ?? string.Empty;

            if (safeRight.Length == 0)
            {
                return Fit(safeLeft, innerWidth);
            }

            int gap = innerWidth - safeLeft.Length - safeRight.Length;
            if (gap < 1)
            {
                int leftWidth = Math.Max(0, innerWidth - safeRight.Length - 1);
                safeLeft = Truncate(safeLeft, leftWidth);
                gap = innerWidth - safeLeft.Length - safeRight.Length;
            }

            if (gap < 1)
            {
                safeRight = Truncate(safeRight, Math.Max(0, innerWidth - safeLeft.Length - 1));
                gap = innerWidth - safeLeft.Length - safeRight.Length;
            }

            return Fit($"{safeLeft}{new string(' ', Math.Max(1, gap))}{safeRight}", innerWidth);
        }

        private static string Fit(string value, int width)
        {
            string safeValue = Truncate(value ?? string.Empty, width);
            return safeValue.PadRight(width);
        }

        private static string FitWithPadding(string value, int width)
        {
            if (width <= 2)
            {
                return Fit(value, width);
            }

            return $" {Fit(value, width - 2)} ";
        }

        private static int GetContentWidth(int innerWidth) =>
            Math.Max(0, innerWidth - 2);

        private static string Truncate(string value, int width)
        {
            if (width <= 0)
            {
                return string.Empty;
            }

            if (value.Length <= width)
            {
                return value;
            }

            return width == 1 ? value[..1] : $"{value[..(width - 1)]}…";
        }

        private static int GetConsoleWidth()
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
