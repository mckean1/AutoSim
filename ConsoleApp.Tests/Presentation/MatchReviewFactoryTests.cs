using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Models;
using ConsoleApp.Presentation;
using MatchType = AutoSim.Domain.Enums.MatchType;
using ManagementRoundResult = AutoSim.Domain.Management.Models.RoundResult;

namespace ConsoleApp.Tests.Presentation
{
    internal sealed class MatchReviewFactoryTests
    {
        [Test]
        public void Create_PresentedMatch_BuildsMatchReviewWithKeyMoments()
        {
            PresentedMatch presentedMatch = new()
            {
                Result = new MatchResult
                {
                    BestOf = 3,
                    BlueRoundWins = 1,
                    BlueTeamId = "blue",
                    LosingTeamId = "red",
                    MatchId = "match-1",
                    MatchType = MatchType.RegularSeason,
                    RedRoundWins = 0,
                    RedTeamId = "red",
                    RoundResults =
                    [
                        new ManagementRoundResult
                        {
                            BlueTeamId = "blue",
                            LosingTeamId = "red",
                            RedTeamId = "red",
                            RoundNumber = 1,
                            WinningTeamId = "blue"
                        }
                    ],
                    WinningTeamId = "blue"
                },
                Rounds =
                [
                    new PresentedRound
                    {
                        Result = new ManagementRoundResult
                        {
                            BlueTeamId = "blue",
                            LosingTeamId = "red",
                            RedTeamId = "red",
                            RoundNumber = 1,
                            WinningTeamId = "blue"
                        },
                        Messages =
                        [
                            new ReplayMessage
                            {
                                Category = ReplayMessageCategory.RoundStart,
                                Severity = ReplayMessageSeverity.Important,
                                Text = "Round starts.",
                                Timestamp = TimeSpan.Zero
                            },
                            new ReplayMessage
                            {
                                Category = ReplayMessageCategory.Kill,
                                Severity = ReplayMessageSeverity.Critical,
                                Text = "Quickshot defeats Longshot.",
                                Timestamp = TimeSpan.FromSeconds(72)
                            }
                        ]
                    }
                ]
            };

            MatchReview review = new MatchReviewFactory().Create(
                presentedMatch,
                weekNumber: 12,
                teamId => teamId == "blue" ? "Salt Lake Strikers" : "Boise Barrage");

            Assert.Multiple(() =>
            {
                Assert.That(review.WeekNumber, Is.EqualTo(12));
                Assert.That(review.MatchType, Is.EqualTo("Regular Season"));
                Assert.That(review.Rounds.Single().KeyMoments, Has.Some.Contains("Quickshot defeats Longshot."));
                Assert.That(review.MatchMessages.Select(message => message.Text), Has.Some.StartsWith("R1 "));
            });
        }
    }
}
