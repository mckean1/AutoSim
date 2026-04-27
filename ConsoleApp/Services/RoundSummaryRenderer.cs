using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;

namespace ConsoleApp.Services
{
    /// <summary>
    /// Renders readable round summaries for the console.
    /// </summary>
    public sealed class RoundSummaryRenderer
    {
        /// <summary>
        /// Renders the round result and log path.
        /// </summary>
        /// <param name="blueTeamName">The blue team name.</param>
        /// <param name="redTeamName">The red team name.</param>
        /// <param name="result">The round result.</param>
        /// <param name="logPath">The event log path.</param>
        /// <returns>The rendered summary.</returns>
        public string Render(string blueTeamName, string redTeamName, RoundResult result, string logPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(blueTeamName);
            ArgumentException.ThrowIfNullOrWhiteSpace(redTeamName);
            ArgumentNullException.ThrowIfNull(result);
            ArgumentException.ThrowIfNullOrWhiteSpace(logPath);

            StringWriter writer = new();
            writer.WriteLine($"Match: {blueTeamName} vs {redTeamName}");
            writer.WriteLine($"Round Duration: {result.Duration:0.0}s");
            writer.WriteLine($"Winner: {result.WinningSide}");
            writer.WriteLine();
            writer.WriteLine("Final Score");
            writer.WriteLine($"Blue: {result.BlueKills} kills");
            writer.WriteLine($"Red: {result.RedKills} kills");
            writer.WriteLine();
            writer.WriteLine("Team Totals");
            writer.WriteLine($"Blue: {result.BlueGold} gold, {result.BlueExperience} XP");
            writer.WriteLine($"Red: {result.RedGold} gold, {result.RedExperience} XP");
            writer.WriteLine();
            WriteChampionTable(writer, "Blue Champions", result.ChampionSummaries, TeamSide.Blue);
            writer.WriteLine();
            WriteChampionTable(writer, "Red Champions", result.ChampionSummaries, TeamSide.Red);
            writer.WriteLine();
            writer.WriteLine("Round log written to:");
            writer.WriteLine(logPath);

            return writer.ToString();
        }

        private static void WriteChampionTable(
            TextWriter writer,
            string title,
            IEnumerable<ChampionRoundSummary> summaries,
            TeamSide teamSide)
        {
            writer.WriteLine(title);
            writer.WriteLine($"{"Name",-16}{"Lane",-8}{"Lv",-5}{"XP",-7}{"Gold",-7}{"K/D",-6}HP");

            foreach (ChampionRoundSummary summary in summaries.Where(summary => summary.TeamSide == teamSide))
            {
                string killsDeaths = $"{summary.Kills}/{summary.Deaths}";
                string health = $"{summary.FinalHealth}/{summary.MaximumHealth}";
                writer.WriteLine(
                    $"{summary.ChampionName,-16}{summary.Lane,-8}{summary.Level,-5}{summary.Experience,-7}{summary.Gold,-7}{killsDeaths,-6}{health}");
            }
        }
    }
}
