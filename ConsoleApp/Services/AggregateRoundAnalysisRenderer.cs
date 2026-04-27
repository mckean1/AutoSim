using AutoSim.Domain.Enums;
using ConsoleApp.Objects;

namespace ConsoleApp.Services
{
    /// <summary>
    /// Renders aggregate round analysis reports.
    /// </summary>
    public sealed class AggregateRoundAnalysisRenderer
    {
        public string Render(string title, string logsFolder, AggregateRoundAnalysis analysis)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(logsFolder);
            ArgumentNullException.ThrowIfNull(analysis);

            StringWriter writer = new();
            writer.WriteLine(title);
            writer.WriteLine($"Logs folder: {logsFolder}");
            writer.WriteLine($"Logs found: {analysis.TotalLogsFound}");
            writer.WriteLine($"Rounds analyzed: {analysis.RoundsAnalyzed}");
            writer.WriteLine($"Skipped logs: {analysis.LogsSkipped}");

            if (analysis.RoundsAnalyzed == 0)
            {
                WriteSkippedLogs(writer, analysis.SkippedLogs);
                return writer.ToString();
            }

            writer.WriteLine();
            writer.WriteLine("Win Rate");
            writer.WriteLine($"Blue: {analysis.BlueWins} wins ({WinRate(analysis.BlueWins, analysis.RoundsAnalyzed):0.0}%)");
            writer.WriteLine($"Red: {analysis.RedWins} wins ({WinRate(analysis.RedWins, analysis.RoundsAnalyzed):0.0}%)");
            writer.WriteLine($"Unknown: {analysis.UnknownWinners}");
            writer.WriteLine();
            writer.WriteLine("Average Score");
            writer.WriteLine($"Blue kills: {analysis.AverageBlueKills:0.0}");
            writer.WriteLine($"Red kills: {analysis.AverageRedKills:0.0}");
            writer.WriteLine($"Average round duration: {analysis.AverageRoundDurationSeconds:0.0}s");
            writer.WriteLine($"Average total events: {analysis.AverageTotalEvents:0.0}");
            writer.WriteLine();
            writer.WriteLine("Fight Summary");
            writer.WriteLine($"Average fights per round: {analysis.AverageFightsPerRound:0.0}");
            writer.WriteLine($"Average fight duration: {analysis.AverageFightDurationSeconds:0.0}s");
            writer.WriteLine($"Longest fight seen: {analysis.LongestFightSeconds:0.0}s");
            writer.WriteLine($"Fights ended by round end avg: {analysis.AverageFightsEndedByRoundEnd:0.0}");
            writer.WriteLine();
            WriteFightsByLane(writer, analysis);
            writer.WriteLine();
            WriteTeamAverages(writer, analysis.BlueTeam, analysis.RedTeam);
            writer.WriteLine();
            WriteChampionAverages(writer, analysis.Champions);
            writer.WriteLine();
            WriteNotable(writer, analysis.NotableFindings);
            WriteSkippedLogs(writer, analysis.SkippedLogs);

            return writer.ToString();
        }

        private static double WinRate(int wins, int rounds) => rounds == 0 ? 0 : wins * 100.0 / rounds;

        private static void WriteFightsByLane(TextWriter writer, AggregateRoundAnalysis analysis)
        {
            writer.WriteLine("Fights by Lane");
            foreach (Lane lane in Enum.GetValues<Lane>())
            {
                analysis.AverageFightsByLane.TryGetValue(lane, out double average);
                writer.WriteLine($"{lane,-8}{average:0.0} avg");
            }
        }

        private static void WriteTeamAverages(TextWriter writer, TeamAggregateAnalysis blue, TeamAggregateAnalysis red)
        {
            writer.WriteLine("Team Averages");
            writer.WriteLine($"{"Team",-7}{"Kills",-7}{"Deaths",-8}{"Damage",-9}{"Healing",-9}{"Shielding",-11}{"Retreats",-10}{"Escapes",-9}Respawns");
            WriteTeam(writer, blue);
            WriteTeam(writer, red);
        }

        private static void WriteTeam(TextWriter writer, TeamAggregateAnalysis team)
        {
            writer.WriteLine(
                $"{team.TeamSide,-7}{team.AverageKills,-7:0.0}{team.AverageDeaths,-8:0.0}{team.AverageDamageDealt,-9:0.0}{team.AverageHealingDone,-9:0.0}{team.AverageShieldingDone,-11:0.0}{team.AverageRetreats,-10:0.0}{team.AverageEscapes,-9:0.0}{team.AverageRespawns:0.0}");
        }

        private static void WriteChampionAverages(TextWriter writer, IReadOnlyList<ChampionAggregateAnalysis> champions)
        {
            writer.WriteLine("Champion Averages");
            writer.WriteLine($"{"Champion",-18}{"Games",-7}{"Win%",-7}{"Kills",-7}{"Deaths",-8}{"Damage",-9}{"Healing",-9}{"Shielding",-11}Retreats");
            foreach (ChampionAggregateAnalysis champion in champions)
            {
                writer.WriteLine(
                    $"{champion.ChampionName,-18}{champion.Games,-7}{champion.WinRate,-7:0.0}{champion.AverageKills,-7:0.0}{champion.AverageDeaths,-8:0.0}{champion.AverageDamageDealt,-9:0.0}{champion.AverageHealingDone,-9:0.0}{champion.AverageShieldingDone,-11:0.0}{champion.AverageRetreats:0.0}");
            }
        }

        private static void WriteNotable(TextWriter writer, IReadOnlyList<string> findings)
        {
            writer.WriteLine("Notable");
            if (findings.Count == 0)
            {
                writer.WriteLine("- None.");
                return;
            }

            foreach (string finding in findings)
            {
                writer.WriteLine($"- {finding}");
            }
        }

        private static void WriteSkippedLogs(TextWriter writer, IReadOnlyList<string> skippedLogs)
        {
            if (skippedLogs.Count == 0)
            {
                return;
            }

            writer.WriteLine();
            writer.WriteLine("Skipped Logs");
            foreach (string skippedLog in skippedLogs)
            {
                writer.WriteLine($"- {skippedLog}");
            }
        }
    }
}
