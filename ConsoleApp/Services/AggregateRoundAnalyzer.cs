using AutoSim.Domain.Enums;
using ConsoleApp.Objects;

namespace ConsoleApp.Services
{
    /// <summary>
    /// Combines single-round analyses into aggregate metrics.
    /// </summary>
    public sealed class AggregateRoundAnalyzer
    {
        public AggregateRoundAnalysis Analyze(
            IReadOnlyList<RoundAnalysis> rounds,
            int totalLogsFound,
            IReadOnlyList<string>? skippedLogs = null)
        {
            ArgumentNullException.ThrowIfNull(rounds);
            IReadOnlyList<string> skipped = skippedLogs ?? [];
            int count = rounds.Count;

            Dictionary<Lane, int> totalFightsByLane = Enum.GetValues<Lane>().ToDictionary(lane => lane, _ => 0);
            foreach (RoundAnalysis round in rounds)
            {
                foreach (Lane lane in Enum.GetValues<Lane>())
                {
                    totalFightsByLane[lane] += round.FightSummary.FightsByLane.TryGetValue(lane, out int value) ? value : 0;
                }
            }

            IReadOnlyList<ChampionAggregateAnalysis> champions = CreateChampionAggregates(rounds);

            return new AggregateRoundAnalysis
            {
                TotalLogsFound = totalLogsFound,
                RoundsAnalyzed = count,
                LogsSkipped = skipped.Count,
                BlueWins = rounds.Count(round => round.Winner == TeamSide.Blue),
                RedWins = rounds.Count(round => round.Winner == TeamSide.Red),
                UnknownWinners = rounds.Count(round => round.Winner is null),
                AverageBlueKills = Average(rounds, round => round.BlueKills),
                AverageRedKills = Average(rounds, round => round.RedKills),
                AverageRoundDurationSeconds = Average(rounds, round => round.DurationSeconds),
                AverageTotalEvents = Average(rounds, round => round.TotalEvents),
                AverageFightsPerRound = Average(rounds, round => round.FightSummary.TotalFights),
                AverageFightDurationSeconds = Average(rounds, round => round.FightSummary.AverageDurationSeconds),
                LongestFightSeconds = count == 0 ? 0 : rounds.Max(round => round.FightSummary.LongestFightSeconds),
                AverageFightsEndedByRoundEnd = Average(rounds, round => round.FightSummary.FightsEndedByRoundEnd),
                AverageFightsByLane = totalFightsByLane.ToDictionary(pair => pair.Key, pair => count == 0 ? 0 : pair.Value / (double)count),
                TotalFightsByLane = totalFightsByLane,
                BlueTeam = CreateTeamAggregate(TeamSide.Blue, rounds.Select(round => round.BlueTeam).ToList(), count),
                RedTeam = CreateTeamAggregate(TeamSide.Red, rounds.Select(round => round.RedTeam).ToList(), count),
                Champions = champions,
                NotableFindings = CreateNotableFindings(rounds, champions, totalFightsByLane),
                SkippedLogs = skipped
            };
        }

        private static TeamAggregateAnalysis CreateTeamAggregate(TeamSide side, IReadOnlyList<TeamAnalysis> teams, int rounds) =>
            new()
            {
                TeamSide = side,
                AverageKills = Average(teams, team => team.Kills, rounds),
                AverageDeaths = Average(teams, team => team.Deaths, rounds),
                AverageDamageDealt = Average(teams, team => team.DamageDealt, rounds),
                AverageHealingDone = Average(teams, team => team.HealingDone, rounds),
                AverageShieldingDone = Average(teams, team => team.ShieldingDone, rounds),
                AverageRetreats = Average(teams, team => team.Retreats, rounds),
                AverageEscapes = Average(teams, team => team.Escapes, rounds),
                AverageRespawns = Average(teams, team => team.Respawns, rounds)
            };

        private static IReadOnlyList<ChampionAggregateAnalysis> CreateChampionAggregates(IReadOnlyList<RoundAnalysis> rounds) =>
            rounds
                .SelectMany(round => round.Champions.Select(champion => (Round: round, Champion: champion)))
                .GroupBy(item => item.Champion.ChampionId, StringComparer.Ordinal)
                .Select(group =>
                {
                    List<(RoundAnalysis Round, ChampionAnalysis Champion)> games = group.ToList();
                    string name = games.Last().Champion.ChampionName;
                    int wins = games.Count(item => item.Round.Winner == item.Champion.TeamSide);
                    return new ChampionAggregateAnalysis
                    {
                        ChampionId = group.Key,
                        ChampionName = name,
                        Games = games.Count,
                        Wins = wins,
                        WinRate = games.Count == 0 ? 0 : wins * 100.0 / games.Count,
                        AverageKills = games.Average(item => item.Champion.Kills),
                        AverageDeaths = games.Average(item => item.Champion.Deaths),
                        AverageDamageDealt = games.Average(item => item.Champion.DamageDealt),
                        AverageHealingDone = games.Average(item => item.Champion.HealingDone),
                        AverageShieldingDone = games.Average(item => item.Champion.ShieldingDone),
                        AverageRetreats = games.Average(item => item.Champion.Retreats),
                        AverageEscapes = games.Average(item => item.Champion.Escapes)
                    };
                })
                .OrderByDescending(champion => champion.Games)
                .ThenByDescending(champion => champion.AverageDamageDealt + champion.AverageHealingDone + champion.AverageShieldingDone)
                .ThenBy(champion => champion.ChampionName, StringComparer.Ordinal)
                .ToList();

        private static IReadOnlyList<string> CreateNotableFindings(
            IReadOnlyList<RoundAnalysis> rounds,
            IReadOnlyList<ChampionAggregateAnalysis> champions,
            IReadOnlyDictionary<Lane, int> totalFightsByLane)
        {
            List<string> findings = [];
            int blueWins = rounds.Count(round => round.Winner == TeamSide.Blue);
            int redWins = rounds.Count(round => round.Winner == TeamSide.Red);
            int knownWins = blueWins + redWins;
            if (knownWins > 0)
            {
                double blueRate = blueWins * 100.0 / knownWins;
                double redRate = redWins * 100.0 / knownWins;
                findings.Add($"Blue/Red win rate: {blueRate:0.0}% / {redRate:0.0}%.");
            }

            AddChampionFinding(findings, "Most damaging champion", champions, champion => champion.AverageDamageDealt, "avg damage");
            AddChampionFinding(findings, "Most healing champion", champions, champion => champion.AverageHealingDone, "avg healing");
            AddChampionFinding(findings, "Most shielding champion", champions, champion => champion.AverageShieldingDone, "avg shielding");

            if (totalFightsByLane.Count > 0 && totalFightsByLane.Values.Any(value => value > 0))
            {
                Lane lane = totalFightsByLane.OrderByDescending(pair => pair.Value).First().Key;
                findings.Add($"{lane} has the most fights.");
            }

            return findings;
        }

        private static void AddChampionFinding(
            ICollection<string> findings,
            string label,
            IReadOnlyList<ChampionAggregateAnalysis> champions,
            Func<ChampionAggregateAnalysis, double> selector,
            string suffix)
        {
            ChampionAggregateAnalysis? champion = champions
                .Where(candidate => selector(candidate) > 0)
                .OrderByDescending(selector)
                .FirstOrDefault();

            if (champion is not null)
            {
                findings.Add($"{label}: {champion.ChampionName}, {selector(champion):0.0} {suffix}.");
            }
        }

        private static double Average<T>(IReadOnlyList<T> values, Func<T, double> selector) =>
            values.Count == 0 ? 0 : values.Average(selector);

        private static double Average<T>(IReadOnlyList<T> values, Func<T, double> selector, int denominator) =>
            denominator == 0 ? 0 : values.Sum(selector) / denominator;
    }
}
