using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using ConsoleApp.Objects;

namespace ConsoleApp.Services
{
    /// <summary>
    /// Computes round analysis metrics from structured events.
    /// </summary>
    public sealed class RoundLogAnalyzer
    {
        /// <summary>
        /// Analyzes parsed round events.
        /// </summary>
        /// <param name="events">The parsed round events.</param>
        /// <returns>The computed round analysis.</returns>
        public RoundAnalysis Analyze(IReadOnlyList<RoundEvent> events)
        {
            ArgumentNullException.ThrowIfNull(events);

            TeamAccumulator blueTeam = new(TeamSide.Blue);
            TeamAccumulator redTeam = new(TeamSide.Red);
            Dictionary<string, ChampionAccumulator> champions = new(StringComparer.Ordinal);

            foreach (RoundEvent roundEvent in events)
            {
                ApplyTeamMetrics(roundEvent, blueTeam, redTeam);
                ApplyChampionMetrics(roundEvent, champions);
            }

            FightAnalysis fightAnalysis = AnalyzeFights(events);
            IReadOnlyList<ChampionAnalysis> championAnalyses = champions.Values
                .Select(accumulator => accumulator.ToAnalysis())
                .OrderBy(analysis => analysis.TeamSide)
                .ThenByDescending(analysis => analysis.Kills)
                .ThenByDescending(analysis => analysis.DamageDealt)
                .ThenBy(analysis => analysis.ChampionName, StringComparer.Ordinal)
                .ToList();
            int blueKills = blueTeam.Kills;
            int redKills = redTeam.Kills;

            return new RoundAnalysis
            {
                TotalEvents = events.Count,
                DurationSeconds = GetDurationSeconds(events),
                Winner = GetWinner(events, blueKills, redKills),
                BlueKills = blueKills,
                RedKills = redKills,
                FightSummary = fightAnalysis,
                BlueTeam = blueTeam.ToAnalysis(),
                RedTeam = redTeam.ToAnalysis(),
                Champions = championAnalyses,
                NotableEvents = CreateNotableEvents(championAnalyses, fightAnalysis, blueTeam, redTeam)
            };
        }

        private static void ApplyTeamMetrics(RoundEvent roundEvent, TeamAccumulator blueTeam, TeamAccumulator redTeam)
        {
            TeamSide? sourceSide = ParseTeamSide(roundEvent.SourceTeamSide);
            TeamSide? targetSide = ParseTeamSide(roundEvent.TargetTeamSide);

            switch (roundEvent.Type)
            {
                case RoundEventType.ChampionKilled:
                    TeamSide? killingSide = sourceSide ?? GetOpposingSide(targetSide);
                    if (killingSide is TeamSide killSide)
                    {
                        GetTeam(blueTeam, redTeam, killSide).Kills++;
                    }

                    if (targetSide is TeamSide deathSide)
                    {
                        GetTeam(blueTeam, redTeam, deathSide).Deaths++;
                    }

                    break;
                case RoundEventType.DamageDealt:
                    AddAmount(blueTeam, redTeam, sourceSide, roundEvent.Amount, MetricKind.Damage);
                    break;
                case RoundEventType.HealingDone:
                    AddAmount(blueTeam, redTeam, sourceSide, roundEvent.Amount, MetricKind.Healing);
                    break;
                case RoundEventType.ShieldApplied:
                    AddAmount(blueTeam, redTeam, sourceSide, roundEvent.Amount, MetricKind.Shielding);
                    break;
                case RoundEventType.ChampionRetreated:
                    if (sourceSide is TeamSide retreatSide)
                    {
                        GetTeam(blueTeam, redTeam, retreatSide).Retreats++;
                    }

                    break;
                case RoundEventType.ChampionEscaped:
                    if (sourceSide is TeamSide escapeSide)
                    {
                        GetTeam(blueTeam, redTeam, escapeSide).Escapes++;
                    }

                    break;
                case RoundEventType.ChampionRespawned:
                    if (sourceSide is TeamSide respawnSide)
                    {
                        GetTeam(blueTeam, redTeam, respawnSide).Respawns++;
                    }

                    break;
            }
        }

        private static void ApplyChampionMetrics(
            RoundEvent roundEvent,
            Dictionary<string, ChampionAccumulator> champions)
        {
            ChampionAccumulator? source = GetSourceChampion(roundEvent, champions);
            ChampionAccumulator? target = GetTargetChampion(roundEvent, champions);

            switch (roundEvent.Type)
            {
                case RoundEventType.ChampionKilled:
                    if (source is not null)
                    {
                        source.Kills++;
                    }

                    if (target is not null)
                    {
                        target.Deaths++;
                    }

                    break;
                case RoundEventType.DamageDealt:
                    if (source is not null)
                    {
                        source.DamageDealt += roundEvent.Amount ?? 0;
                    }

                    break;
                case RoundEventType.HealingDone:
                    if (source is not null)
                    {
                        source.HealingDone += roundEvent.Amount ?? 0;
                    }

                    break;
                case RoundEventType.ShieldApplied:
                    if (source is not null)
                    {
                        source.ShieldingDone += roundEvent.Amount ?? 0;
                    }

                    break;
                case RoundEventType.ChampionRetreated:
                    if (source is not null)
                    {
                        source.Retreats++;
                    }

                    break;
                case RoundEventType.ChampionEscaped:
                    if (source is not null)
                    {
                        source.Escapes++;
                    }

                    break;
                case RoundEventType.ChampionRespawned:
                    if (source is not null)
                    {
                        source.Respawns++;
                    }

                    break;
                case RoundEventType.ChampionLeveledUp:
                    if (source is not null)
                    {
                        source.LevelsGained++;
                    }

                    break;
            }
        }

        private static FightAnalysis AnalyzeFights(IReadOnlyList<RoundEvent> events)
        {
            List<RoundEvent> starts = events.Where(roundEvent => roundEvent.Type == RoundEventType.FightStarted).ToList();
            List<RoundEvent> ends = events.Where(roundEvent => roundEvent.Type == RoundEventType.FightEnded).ToList();
            Dictionary<Lane, int> fightsByLane = Enum.GetValues<Lane>().ToDictionary(lane => lane, _ => 0);
            foreach (RoundEvent start in starts)
            {
                if (ParseLane(start.Lane) is Lane lane)
                {
                    fightsByLane[lane]++;
                }
            }

            List<(double Duration, Lane? Lane)> durations = [];
            HashSet<RoundEvent> matchedEnds = [];
            foreach (RoundEvent start in starts.Where(start => start.FightId.HasValue))
            {
                RoundEvent? end = ends.FirstOrDefault(fightEnd => fightEnd.FightId == start.FightId);
                if (end is not null)
                {
                    AddDuration(start, end, durations);
                    matchedEnds.Add(end);
                }
            }

            foreach (RoundEvent start in starts.Where(start => !start.FightId.HasValue))
            {
                Lane? lane = ParseLane(start.Lane);
                RoundEvent? end = ends
                    .Where(fightEnd => !matchedEnds.Contains(fightEnd))
                    .FirstOrDefault(fightEnd => ParseLane(fightEnd.Lane) == lane);

                if (end is not null)
                {
                    AddDuration(start, end, durations);
                    matchedEnds.Add(end);
                }
            }

            int blueFightWins = ends.Count(fightEnd => ParseTeamSide(fightEnd.SourceTeamSide) == TeamSide.Blue
                && !IsRoundEndFightEnd(fightEnd));
            int redFightWins = ends.Count(fightEnd => ParseTeamSide(fightEnd.SourceTeamSide) == TeamSide.Red
                && !IsRoundEndFightEnd(fightEnd));
            (double longestFight, Lane? longestLane) = durations
                .OrderByDescending(duration => duration.Duration)
                .FirstOrDefault();

            return new FightAnalysis
            {
                TotalFights = starts.Count,
                AverageDurationSeconds = durations.Count == 0 ? 0 : durations.Average(duration => duration.Duration),
                LongestFightSeconds = longestFight,
                LongestFightLane = longestLane,
                FightsByLane = fightsByLane,
                BlueFightWins = blueFightWins,
                RedFightWins = redFightWins,
                FightsEndedByRoundEnd = ends.Count(IsRoundEndFightEnd)
            };
        }

        private static void AddDuration(
            RoundEvent start,
            RoundEvent end,
            List<(double Duration, Lane? Lane)> durations)
        {
            double duration = end.TimeSeconds - start.TimeSeconds;
            if (duration >= 0)
            {
                durations.Add((duration, ParseLane(start.Lane) ?? ParseLane(end.Lane)));
            }
        }

        private static IReadOnlyList<string> CreateNotableEvents(
            IReadOnlyList<ChampionAnalysis> champions,
            FightAnalysis fightAnalysis,
            TeamAccumulator blueTeam,
            TeamAccumulator redTeam)
        {
            List<string> notable = [];
            AddChampionNotable(notable, "Most damage", champions, champion => champion.DamageDealt);
            AddChampionNotable(notable, "Most healing", champions, champion => champion.HealingDone);
            AddChampionNotable(notable, "Most shielding", champions, champion => champion.ShieldingDone);
            AddChampionNotable(notable, "Most kills", champions, champion => champion.Kills);
            AddChampionNotable(notable, "Most deaths", champions, champion => champion.Deaths);

            if (fightAnalysis.LongestFightSeconds > 0)
            {
                string lane = fightAnalysis.LongestFightLane?.ToString() ?? "Unknown lane";
                notable.Add($"Longest fight: {lane}, {fightAnalysis.LongestFightSeconds:0.0}s.");
            }

            if (blueTeam.Retreats != redTeam.Retreats)
            {
                TeamSide side = blueTeam.Retreats > redTeam.Retreats ? TeamSide.Blue : TeamSide.Red;
                int value = Math.Max(blueTeam.Retreats, redTeam.Retreats);
                notable.Add($"More retreats: {side}, {value}.");
            }

            if (blueTeam.Escapes != redTeam.Escapes)
            {
                TeamSide side = blueTeam.Escapes > redTeam.Escapes ? TeamSide.Blue : TeamSide.Red;
                int value = Math.Max(blueTeam.Escapes, redTeam.Escapes);
                notable.Add($"More escapes: {side}, {value}.");
            }

            return notable.Take(8).ToList();
        }

        private static void AddChampionNotable(
            ICollection<string> notable,
            string label,
            IReadOnlyList<ChampionAnalysis> champions,
            Func<ChampionAnalysis, int> selector)
        {
            ChampionAnalysis? champion = champions
                .Where(candidate => selector(candidate) > 0)
                .OrderByDescending(selector)
                .FirstOrDefault();

            if (champion is not null)
            {
                notable.Add($"{label}: {champion.TeamSide} {champion.ChampionName}, {selector(champion)}.");
            }
        }

        private static double GetDurationSeconds(IReadOnlyList<RoundEvent> events)
        {
            RoundEvent? roundEnded = events.LastOrDefault(roundEvent => roundEvent.Type == RoundEventType.RoundEnded);
            return roundEnded?.TimeSeconds ?? (events.Count == 0 ? 0 : events.Max(roundEvent => roundEvent.TimeSeconds));
        }

        private static TeamSide? GetWinner(IReadOnlyList<RoundEvent> events, int blueKills, int redKills)
        {
            RoundEvent? roundEnded = events.LastOrDefault(roundEvent => roundEvent.Type == RoundEventType.RoundEnded);
            TeamSide? winner = ParseTeamSide(roundEnded?.SourceTeamSide ?? roundEnded?.TeamSide);
            if (winner is not null)
            {
                return winner;
            }

            if (blueKills != redKills)
            {
                return blueKills > redKills ? TeamSide.Blue : TeamSide.Red;
            }

            return null;
        }

        private static bool IsRoundEndFightEnd(RoundEvent fightEnd) =>
            fightEnd.Message.Contains("round ended", StringComparison.OrdinalIgnoreCase);

        private static ChampionAccumulator? GetSourceChampion(
            RoundEvent roundEvent,
            Dictionary<string, ChampionAccumulator> champions)
        {
            TeamSide? side = ParseTeamSide(roundEvent.SourceTeamSide ?? roundEvent.TeamSide);
            string? championId = roundEvent.SourceChampionId ?? roundEvent.ChampionId;
            string? playerId = roundEvent.SourcePlayerId;
            string? championName = roundEvent.SourceChampionName;

            return GetChampion(champions, side, championId, playerId, championName);
        }

        private static ChampionAccumulator? GetTargetChampion(
            RoundEvent roundEvent,
            Dictionary<string, ChampionAccumulator> champions)
        {
            TeamSide? side = ParseTeamSide(roundEvent.TargetTeamSide);
            return GetChampion(
                champions,
                side,
                roundEvent.TargetChampionId,
                roundEvent.TargetPlayerId,
                roundEvent.TargetChampionName);
        }

        private static ChampionAccumulator? GetChampion(
            Dictionary<string, ChampionAccumulator> champions,
            TeamSide? side,
            string? championId,
            string? playerId,
            string? championName)
        {
            if (side is not TeamSide teamSide || string.IsNullOrWhiteSpace(championId))
            {
                return null;
            }

            string key = $"{teamSide}:{playerId ?? string.Empty}:{championId}";
            if (!champions.TryGetValue(key, out ChampionAccumulator? champion))
            {
                champion = new ChampionAccumulator(teamSide, championId, championName ?? championId);
                champions.Add(key, champion);
            }
            else if (!string.IsNullOrWhiteSpace(championName))
            {
                champion.ChampionName = championName;
            }

            return champion;
        }

        private static void AddAmount(
            TeamAccumulator blueTeam,
            TeamAccumulator redTeam,
            TeamSide? side,
            int? amount,
            MetricKind metricKind)
        {
            if (side is not TeamSide teamSide)
            {
                return;
            }

            TeamAccumulator team = GetTeam(blueTeam, redTeam, teamSide);
            switch (metricKind)
            {
                case MetricKind.Damage:
                    team.DamageDealt += amount ?? 0;
                    break;
                case MetricKind.Healing:
                    team.HealingDone += amount ?? 0;
                    break;
                case MetricKind.Shielding:
                    team.ShieldingDone += amount ?? 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(metricKind), metricKind, "Unsupported metric kind.");
            }
        }

        private static TeamAccumulator GetTeam(TeamAccumulator blueTeam, TeamAccumulator redTeam, TeamSide side) =>
            side == TeamSide.Blue ? blueTeam : redTeam;

        private static TeamSide? GetOpposingSide(TeamSide? side) =>
            side switch
            {
                TeamSide.Blue => TeamSide.Red,
                TeamSide.Red => TeamSide.Blue,
                _ => null
            };

        private static TeamSide? ParseTeamSide(string? value) =>
            Enum.TryParse(value, ignoreCase: true, out TeamSide side) ? side : null;

        private static Lane? ParseLane(string? value) =>
            Enum.TryParse(value, ignoreCase: true, out Lane lane) ? lane : null;

        private enum MetricKind
        {
            Damage,
            Healing,
            Shielding
        }

        private sealed class TeamAccumulator
        {
            public TeamAccumulator(TeamSide teamSide)
            {
                TeamSide = teamSide;
            }

            public TeamSide TeamSide { get; }
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int DamageDealt { get; set; }
            public int HealingDone { get; set; }
            public int ShieldingDone { get; set; }
            public int Retreats { get; set; }
            public int Escapes { get; set; }
            public int Respawns { get; set; }

            public TeamAnalysis ToAnalysis() =>
                new()
                {
                    TeamSide = TeamSide,
                    Kills = Kills,
                    Deaths = Deaths,
                    DamageDealt = DamageDealt,
                    HealingDone = HealingDone,
                    ShieldingDone = ShieldingDone,
                    Retreats = Retreats,
                    Escapes = Escapes,
                    Respawns = Respawns
                };
        }

        private sealed class ChampionAccumulator
        {
            public ChampionAccumulator(TeamSide teamSide, string championId, string championName)
            {
                TeamSide = teamSide;
                ChampionId = championId;
                ChampionName = championName;
            }

            public TeamSide TeamSide { get; }
            public string ChampionId { get; }
            public string ChampionName { get; set; }
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int DamageDealt { get; set; }
            public int HealingDone { get; set; }
            public int ShieldingDone { get; set; }
            public int Retreats { get; set; }
            public int Escapes { get; set; }
            public int Respawns { get; set; }
            public int LevelsGained { get; set; }

            public ChampionAnalysis ToAnalysis() =>
                new()
                {
                    ChampionId = ChampionId,
                    ChampionName = ChampionName,
                    TeamSide = TeamSide,
                    Kills = Kills,
                    Deaths = Deaths,
                    DamageDealt = DamageDealt,
                    HealingDone = HealingDone,
                    ShieldingDone = ShieldingDone,
                    Retreats = Retreats,
                    Escapes = Escapes,
                    Respawns = Respawns,
                    LevelsGained = LevelsGained
                };
        }
    }
}
