using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Interfaces;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Objects;
using CombatRoundResult = AutoSim.Domain.Objects.RoundResult;
using MatchType = AutoSim.Domain.Enums.MatchType;
using ManagementRoundResult = AutoSim.Domain.Management.Models.RoundResult;

namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Coordinates match-level flow between Management Layer matches and the RoundEngine.
    /// </summary>
    public sealed class MatchEngineWrapper : IMatchEngine
    {
        private readonly IRoundDraftService _roundDraftService;
        private readonly IRoundDraftValidator _roundDraftValidator;
        private readonly IRoundEngine _roundEngine;

        /// <summary>
        /// Initializes a new instance of the <see cref="MatchEngineWrapper"/> class.
        /// </summary>
        /// <param name="roundDraftService">The round draft service.</param>
        /// <param name="roundDraftValidator">The round draft validator.</param>
        /// <param name="roundEngine">The round engine.</param>
        public MatchEngineWrapper(
            IRoundDraftService? roundDraftService = null,
            IRoundDraftValidator? roundDraftValidator = null,
            IRoundEngine? roundEngine = null)
        {
            _roundDraftService = roundDraftService ?? new DeterministicRoundDraftService();
            _roundDraftValidator = roundDraftValidator ?? new RoundDraftValidator();
            _roundEngine = roundEngine ?? new RoundEngineAdapter();
        }

        /// <inheritdoc />
        public MatchResult Resolve(
            ScheduledMatch match,
            Team blueTeam,
            Team redTeam,
            Coach blueCoach,
            Coach redCoach,
            IReadOnlyList<Player> players,
            IReadOnlyList<ChampionDefinition> championCatalog,
            int seed)
        {
            ArgumentNullException.ThrowIfNull(match);
            ArgumentNullException.ThrowIfNull(blueTeam);
            ArgumentNullException.ThrowIfNull(redTeam);
            ArgumentNullException.ThrowIfNull(blueCoach);
            ArgumentNullException.ThrowIfNull(redCoach);
            ArgumentNullException.ThrowIfNull(players);
            ArgumentNullException.ThrowIfNull(championCatalog);

            int bestOf = GetBestOf(match);
            int winsRequired = (bestOf / 2) + 1;
            int blueWins = 0;
            int redWins = 0;
            List<ManagementRoundResult> roundResults = [];
            IReadOnlyList<Player> bluePlayers = GetTeamPlayers(players, blueTeam.Id);
            IReadOnlyList<Player> redPlayers = GetTeamPlayers(players, redTeam.Id);

            while (blueWins < winsRequired && redWins < winsRequired)
            {
                int roundNumber = roundResults.Count + 1;
                int roundSeed = CreateRoundSeed(seed, roundNumber);
                RoundDraft draft = _roundDraftService.DraftRound(new RoundDraftContext
                {
                    BlueCoach = blueCoach,
                    BluePlayers = bluePlayers,
                    BlueTeam = blueTeam,
                    ChampionCatalog = championCatalog,
                    Match = match,
                    PreviousRounds = roundResults,
                    RedCoach = redCoach,
                    RedPlayers = redPlayers,
                    RedTeam = redTeam,
                    RoundNumber = roundNumber,
                    Seed = roundSeed
                });
                _roundDraftValidator.Validate(draft, championCatalog);

                CombatRoundResult combatResult = _roundEngine.Simulate(new RoundSetup
                {
                    BlueChampions = draft.BlueChampions,
                    BlueTeamId = blueTeam.Id,
                    RedChampions = draft.RedChampions,
                    RedTeamId = redTeam.Id,
                    RoundNumber = roundNumber,
                    Seed = roundSeed
                });
                bool blueWon = combatResult.WinningSide == TeamSide.Blue;
                blueWins += blueWon ? 1 : 0;
                redWins += blueWon ? 0 : 1;
                string winningTeamId = blueWon ? blueTeam.Id : redTeam.Id;
                string losingTeamId = blueWon ? redTeam.Id : blueTeam.Id;

                roundResults.Add(new ManagementRoundResult
                {
                    BlueChampionIds = draft.BlueChampions.Select(champion => champion.Id).ToList(),
                    BlueTeamId = blueTeam.Id,
                    LosingTeamId = losingTeamId,
                    RedChampionIds = draft.RedChampions.Select(champion => champion.Id).ToList(),
                    RedTeamId = redTeam.Id,
                    RoundNumber = roundNumber,
                    Summary = $"Round {roundNumber}: {winningTeamId} won.",
                    WinningTeamId = winningTeamId
                });
            }

            string matchWinnerId = blueWins > redWins ? blueTeam.Id : redTeam.Id;
            string matchLoserId = blueWins > redWins ? redTeam.Id : blueTeam.Id;
            return new MatchResult
            {
                BestOf = bestOf,
                BlueRoundWins = blueWins,
                BlueTeamId = blueTeam.Id,
                LosingTeamId = matchLoserId,
                MatchId = match.Id,
                MatchType = match.MatchType,
                RedRoundWins = redWins,
                RedTeamId = redTeam.Id,
                RoundResults = roundResults,
                WinningTeamId = matchWinnerId
            };
        }

        private static int GetBestOf(ScheduledMatch match)
        {
            if (match.BestOf > 0)
            {
                return match.BestOf;
            }

            return match.MatchType switch
            {
                MatchType.RegularSeason => 3,
                MatchType.LeagueQuarterfinal => 5,
                MatchType.LeagueSemifinal => 5,
                MatchType.LeagueFinal => 7,
                MatchType.WorldChampionshipSemifinal => 7,
                MatchType.WorldChampionshipFinal => 9,
                _ => 3
            };
        }

        private static int CreateRoundSeed(int matchSeed, int roundNumber) =>
            unchecked((matchSeed * 397) ^ roundNumber);

        private static IReadOnlyList<Player> GetTeamPlayers(IReadOnlyList<Player> players, string teamId) =>
            players.Where(player => string.Equals(player.TeamId, teamId, StringComparison.Ordinal)).ToList();
    }
}
