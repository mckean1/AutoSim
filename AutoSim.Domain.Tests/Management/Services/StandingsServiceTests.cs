using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Management.Services;
using MatchType = AutoSim.Domain.Enums.MatchType;

namespace AutoSim.Domain.Tests.Management.Services
{
    internal sealed class StandingsServiceTests
    {
        [Test]
        public void ApplyMatchResult_TwoOneRegularSeasonWin_AddsOnePointToWinnerAndMinusOneToLoser()
        {
            League league = CreateLeague();
            ScheduledMatch match = new()
            {
                AwayTeamId = "away",
                BestOf = 3,
                HomeTeamId = "home",
                Id = "match-1",
                LeagueId = "league",
                MatchType = MatchType.RegularSeason,
                Week = 1
            };
            MatchResult result = new()
            {
                BestOf = 3,
                BlueRoundWins = 2,
                BlueTeamId = "home",
                LosingTeamId = "away",
                MatchId = match.Id,
                MatchType = MatchType.RegularSeason,
                RedRoundWins = 1,
                RedTeamId = "away",
                WinningTeamId = "home"
            };

            League updatedLeague = new StandingsService().ApplyMatchResult(league, match, result);
            LeagueStanding homeStanding = updatedLeague.Standings.Single(standing => standing.TeamId == "home");
            LeagueStanding awayStanding = updatedLeague.Standings.Single(standing => standing.TeamId == "away");

            Assert.Multiple(() =>
            {
                Assert.That(homeStanding.MatchWins, Is.EqualTo(1));
                Assert.That(homeStanding.Points, Is.EqualTo(1));
                Assert.That(awayStanding.MatchLosses, Is.EqualTo(1));
                Assert.That(awayStanding.Points, Is.EqualTo(-1));
            });
        }

        private static League CreateLeague() =>
            new()
            {
                Divisions =
                [
                    new Division
                    {
                        Id = "division",
                        Name = DivisionName.Apex,
                        TeamIds = ["home", "away"]
                    }
                ],
                Id = "league",
                Region = LeagueRegion.Northern,
                Standings =
                [
                    new LeagueStanding { TeamId = "home" },
                    new LeagueStanding { TeamId = "away" }
                ],
                Teams =
                [
                    new Team
                    {
                        CoachId = "home-coach",
                        DivisionId = "division",
                        Id = "home",
                        LeagueId = "league",
                        Name = "Home"
                    },
                    new Team
                    {
                        CoachId = "away-coach",
                        DivisionId = "division",
                        Id = "away",
                        LeagueId = "league",
                        Name = "Away"
                    }
                ],
                TierName = CompetitiveTierName.Amateur
            };
    }
}
