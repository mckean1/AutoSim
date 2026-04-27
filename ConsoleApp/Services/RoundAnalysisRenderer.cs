using AutoSim.Domain.Enums;
using ConsoleApp.Objects;

namespace ConsoleApp.Services
{
    /// <summary>
    /// Renders round analysis reports for the console.
    /// </summary>
    public sealed class RoundAnalysisRenderer
    {
        /// <summary>
        /// Renders a round analysis report.
        /// </summary>
        /// <param name="logPath">The analyzed log path.</param>
        /// <param name="analysis">The round analysis.</param>
        /// <returns>The rendered report.</returns>
        public string Render(string logPath, RoundAnalysis analysis)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(logPath);
            ArgumentNullException.ThrowIfNull(analysis);

            StringWriter writer = new();
            writer.WriteLine("Round Analysis");
            writer.WriteLine($"Log: {logPath}");

            if (analysis.TotalEvents == 0)
            {
                writer.WriteLine("No events found.");
                return writer.ToString();
            }

            writer.WriteLine($"Winner: {analysis.Winner?.ToString() ?? "Unknown"}");
            writer.WriteLine($"Score: Blue {analysis.BlueKills} - Red {analysis.RedKills}");
            writer.WriteLine($"Duration: {FormatTime(analysis.DurationSeconds)}");
            writer.WriteLine($"Total Events: {analysis.TotalEvents}");
            writer.WriteLine();
            WriteFightSummary(writer, analysis.FightSummary);
            writer.WriteLine();
            WriteFightsByLane(writer, analysis.FightSummary);
            writer.WriteLine();
            WriteTeamTotals(writer, analysis.BlueTeam, analysis.RedTeam);
            writer.WriteLine();
            WriteChampionPerformance(writer, analysis.Champions);
            writer.WriteLine();
            WriteNotable(writer, analysis.NotableEvents);

            return writer.ToString();
        }

        /// <summary>
        /// Formats seconds as MM:SS.s.
        /// </summary>
        /// <param name="seconds">The seconds to format.</param>
        /// <returns>The formatted time.</returns>
        public static string FormatTime(double seconds)
        {
            double safeSeconds = Math.Max(0, seconds);
            int minutes = (int)(safeSeconds / 60);
            double remainderSeconds = safeSeconds - (minutes * 60);
            return $"{minutes:00}:{remainderSeconds:00.0}";
        }

        private static void WriteFightSummary(TextWriter writer, FightAnalysis fight)
        {
            writer.WriteLine("Fight Summary");
            writer.WriteLine($"Total fights: {fight.TotalFights}");
            writer.WriteLine($"Average duration: {fight.AverageDurationSeconds:0.0}s");
            writer.WriteLine($"Longest fight: {fight.LongestFightSeconds:0.0}s");
            writer.WriteLine($"Blue fight wins: {fight.BlueFightWins}");
            writer.WriteLine($"Red fight wins: {fight.RedFightWins}");
            writer.WriteLine($"Fights ended by round end: {fight.FightsEndedByRoundEnd}");
        }

        private static void WriteFightsByLane(TextWriter writer, FightAnalysis fight)
        {
            writer.WriteLine("Fights by Lane");
            foreach (Lane lane in Enum.GetValues<Lane>())
            {
                fight.FightsByLane.TryGetValue(lane, out int count);
                writer.WriteLine($"{lane,-8}{count}");
            }
        }

        private static void WriteTeamTotals(TextWriter writer, TeamAnalysis blue, TeamAnalysis red)
        {
            writer.WriteLine("Team Totals");
            writer.WriteLine(
                $"{"Team",-7}{"Kills",-7}{"Deaths",-8}{"Damage",-8}{"Healing",-9}{"Shielding",-11}{"Retreats",-10}{"Escapes",-9}Respawns");
            WriteTeam(writer, blue);
            WriteTeam(writer, red);
        }

        private static void WriteTeam(TextWriter writer, TeamAnalysis team)
        {
            writer.WriteLine(
                $"{team.TeamSide,-7}{team.Kills,-7}{team.Deaths,-8}{team.DamageDealt,-8}{team.HealingDone,-9}{team.ShieldingDone,-11}{team.Retreats,-10}{team.Escapes,-9}{team.Respawns}");
        }

        private static void WriteChampionPerformance(TextWriter writer, IReadOnlyList<ChampionAnalysis> champions)
        {
            writer.WriteLine("Champion Performance");
            writer.WriteLine(
                $"{"Champion",-18}{"Team",-7}{"K/D",-6}{"Damage",-9}{"Healing",-9}{"Shielding",-11}{"Retreats",-10}{"Escapes",-9}Respawns");

            foreach (ChampionAnalysis champion in champions)
            {
                string killsDeaths = $"{champion.Kills}/{champion.Deaths}";
                writer.WriteLine(
                    $"{champion.ChampionName,-18}{champion.TeamSide,-7}{killsDeaths,-6}{champion.DamageDealt,-9}{champion.HealingDone,-9}{champion.ShieldingDone,-11}{champion.Retreats,-10}{champion.Escapes,-9}{champion.Respawns}");
            }
        }

        private static void WriteNotable(TextWriter writer, IReadOnlyList<string> notableEvents)
        {
            writer.WriteLine("Notable");
            if (notableEvents.Count == 0)
            {
                writer.WriteLine("- None.");
                return;
            }

            foreach (string notableEvent in notableEvents)
            {
                writer.WriteLine($"- {notableEvent}");
            }
        }
    }
}
