using System.Globalization;

namespace ConsoleApp.Services
{
    /// <summary>
    /// Writes round analysis reports to text files.
    /// </summary>
    public sealed class RoundReportWriter
    {
        private readonly string _reportDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoundReportWriter"/> class.
        /// </summary>
        /// <param name="reportDirectory">The directory where reports are written.</param>
        public RoundReportWriter(string reportDirectory = "logs/rounds")
        {
            _reportDirectory = string.IsNullOrWhiteSpace(reportDirectory)
                ? throw new ArgumentException("Report directory is required.", nameof(reportDirectory))
                : reportDirectory;
        }

        /// <summary>
        /// Writes a report to a text file.
        /// </summary>
        /// <param name="prefix">The report filename prefix.</param>
        /// <param name="report">The report content.</param>
        /// <returns>The written report path.</returns>
        public string WriteReport(string prefix, string report)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
            ArgumentNullException.ThrowIfNull(report);

            Directory.CreateDirectory(_reportDirectory);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fffffff", CultureInfo.InvariantCulture);
            string uniqueSuffix = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            string path = Path.Combine(_reportDirectory, $"{prefix}_{timestamp}_{uniqueSuffix}.txt");

            File.WriteAllText(path, report);
            return path;
        }
    }
}
