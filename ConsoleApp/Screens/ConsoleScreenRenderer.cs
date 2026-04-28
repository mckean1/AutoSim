using System.Text;
using ConsoleApp.Constants;

namespace ConsoleApp.Screens
{
    /// <summary>
    /// Renders full-screen console layouts for management screens.
    /// </summary>
    internal sealed class ConsoleScreenRenderer
    {
        private const int DefaultHeight = 25;
        private const int DefaultWidth = 80;
        private const int MinimumWidth = 40;
        private const string BottomLeft = "\u255A";
        private const string BottomRight = "\u255D";
        private const string Horizontal = "\u2550";
        private const string MiddleLeft = "\u2560";
        private const string MiddleRight = "\u2563";
        private const string TopLeft = "\u2554";
        private const string TopRight = "\u2557";
        private const string Vertical = "\u2551";

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
        /// Builds a fixed-size console frame for in-place rendering.
        /// </summary>
        /// <param name="model">The screen render model.</param>
        /// <param name="width">The target console width.</param>
        /// <param name="height">The target console height.</param>
        /// <param name="command">The current command text.</param>
        /// <returns>The fixed-size frame.</returns>
        public ConsoleFrame BuildFrame(ScreenRenderModel model, int width, int height, string command = "")
        {
            ArgumentNullException.ThrowIfNull(model);

            int safeWidth = Math.Max(MinimumWidth, width);
            int safeHeight = Math.Max(1, height);
            int innerWidth = safeWidth - 2;
            int headerLineCount = 4;
            int footerLineCount = 4;
            int bodyLineCount = Math.Max(0, safeHeight - headerLineCount - footerLineCount);
            List<string> bodyLines = BuildBodyLines(model, bodyLineCount);
            List<string> lines = [];

            AppendBorder(lines, TopLeft, Horizontal, TopRight, innerWidth);
            AppendContentRow(
                lines,
                BuildSplitLine(model.Header.PrimaryLeft, model.Header.PrimaryRight, GetContentWidth(innerWidth)),
                innerWidth);
            AppendContentRow(
                lines,
                BuildSplitLine(model.Header.SecondaryLeft, model.Header.SecondaryRight, GetContentWidth(innerWidth)),
                innerWidth);
            AppendBorder(lines, MiddleLeft, Horizontal, MiddleRight, innerWidth);

            foreach (string line in bodyLines)
            {
                AppendContentRow(lines, line, innerWidth);
            }

            AppendBorder(lines, MiddleLeft, Horizontal, MiddleRight, innerWidth);
            AppendContentRow(lines, $"Commands: {string.Join(" | ", model.Commands)}", innerWidth);
            AppendContentRow(lines, $"{ConsoleConstants.Prompt}{command}", innerWidth);
            AppendBorder(lines, BottomLeft, Horizontal, BottomRight, innerWidth);

            while (lines.Count < safeHeight)
            {
                lines.Add(new string(' ', safeWidth));
            }

            return new ConsoleFrame(lines.Take(safeHeight).Select(line => Fit(line, safeWidth)).ToList(), safeWidth, safeHeight);
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

            AppendLine(builder, BuildBorder(TopLeft, Horizontal, TopRight, innerWidth));
            AppendLine(
                builder,
                BuildRow(BuildSplitLine(model.Header.PrimaryLeft, model.Header.PrimaryRight, GetContentWidth(innerWidth))));
            AppendLine(
                builder,
                BuildRow(BuildSplitLine(model.Header.SecondaryLeft, model.Header.SecondaryRight, GetContentWidth(innerWidth))));
            AppendLine(builder, BuildBorder(MiddleLeft, Horizontal, MiddleRight, innerWidth));

            AppendLine(builder, BuildRow(FitWithPadding(model.Title, innerWidth)));
            if (!string.IsNullOrWhiteSpace(model.Message))
            {
                AppendLine(builder, BuildRow(FitWithPadding(model.Message, innerWidth)));
                AppendLine(builder, BuildRow(FitWithPadding(string.Empty, innerWidth)));
            }

            foreach (string line in model.ContentLines)
            {
                AppendLine(builder, BuildRow(FitWithPadding(line, innerWidth)));
            }

            AppendLine(builder, BuildBorder(MiddleLeft, Horizontal, MiddleRight, innerWidth));
            AppendLine(builder, BuildRow(FitWithPadding($"Commands: {string.Join(" | ", model.Commands)}", innerWidth)));
            AppendLine(builder, BuildRow(FitWithPadding($"{ConsoleConstants.Prompt}{command}", innerWidth)));
            AppendLine(builder, BuildBorder(BottomLeft, Horizontal, BottomRight, innerWidth));
            return builder.ToString();
        }

        private static List<string> BuildBodyLines(ScreenRenderModel model, int bodyLineCount)
        {
            List<string> lines = [model.Title];
            if (!string.IsNullOrWhiteSpace(model.Message))
            {
                lines.Add(model.Message);
                lines.Add(string.Empty);
            }

            int remainingContentLines = Math.Max(0, bodyLineCount - lines.Count);
            lines.AddRange(model.ContentLines.Take(remainingContentLines));

            while (lines.Count < bodyLineCount)
            {
                lines.Add(string.Empty);
            }

            return lines;
        }

        private static void AppendBorder(List<string> lines, string left, string fill, string right, int innerWidth) =>
            lines.Add(BuildBorder(left, fill, right, innerWidth));

        private static void AppendContentRow(List<string> lines, string value, int innerWidth) =>
            lines.Add(BuildRow(FitWithPadding(value, innerWidth)));

        private static string BuildRow(string value) =>
            $"{Vertical}{value}{Vertical}";

        private static string BuildBorder(string left, string fill, string right, int innerWidth) =>
            $"{left}{Repeat(fill, innerWidth)}{right}";

        private static void AppendLine(StringBuilder builder, string line) =>
            builder.AppendLine(line);

        private static string Repeat(string value, int count) =>
            string.Concat(Enumerable.Repeat(value, count));

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

            if (width <= 3)
            {
                return value[..width];
            }

            return $"{value[..(width - 3)]}...";
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
