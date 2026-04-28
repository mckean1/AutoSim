using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Objects;
using ConsoleApp.Presentation;
using MatchType = AutoSim.Domain.Enums.MatchType;
using ManagementRoundResult = AutoSim.Domain.Management.Models.RoundResult;

namespace ConsoleApp.Tests.Presentation
{
    internal sealed class ReplayPresenterTests
    {
        [Test]
        public void Present_MatchResult_CreatesReadableReplayMessages()
        {
            ChampionDefinition blueChampion = CreateChampion("quickshot", "Quickshot");
            ChampionDefinition redChampion = CreateChampion("stonejaw", "Stonejaw");
            MatchResult result = CreateMatchResult(blueChampion.Id, redChampion.Id);

            PresentedMatch presentedMatch = new ReplayPresenter().Present(
                result,
                [blueChampion, redChampion],
                teamId => teamId == "blue-team" ? "Salt Lake Strikers" : "Boise Barrage");

            IReadOnlyList<ReplayMessage> messages = presentedMatch.Rounds.Single().Messages;

            Assert.Multiple(() =>
            {
                Assert.That(messages, Is.Not.Empty);
                Assert.That(messages.Select(message => message.Text), Has.Some.Contains("Quickshot"));
                Assert.That(messages.Select(message => message.Text), Has.Some.Contains("Stonejaw"));
                Assert.That(messages.Select(message => message.Text), Has.None.Contains("ChampionInstance"));
                Assert.That(messages.Select(message => message.Category), Does.Contain(ReplayMessageCategory.RoundEnd));
            });
        }

        private static MatchResult CreateMatchResult(string blueChampionId, string redChampionId) =>
            new()
            {
                BestOf = 3,
                BlueRoundWins = 1,
                BlueTeamId = "blue-team",
                LosingTeamId = "red-team",
                MatchId = "match-1",
                MatchType = MatchType.RegularSeason,
                RedRoundWins = 0,
                RedTeamId = "red-team",
                RoundResults =
                [
                    new ManagementRoundResult
                    {
                        BlueChampionIds =
                        [
                            blueChampionId,
                            "blue-2",
                            "blue-3",
                            "blue-4",
                            "blue-5"
                        ],
                        BlueTeamId = "blue-team",
                        LosingTeamId = "red-team",
                        RedChampionIds =
                        [
                            redChampionId,
                            "red-2",
                            "red-3",
                            "red-4",
                            "red-5"
                        ],
                        RedTeamId = "red-team",
                        RoundNumber = 1,
                        WinningTeamId = "blue-team"
                    }
                ],
                WinningTeamId = "blue-team"
            };

        private static ChampionDefinition CreateChampion(string id, string name) =>
            new()
            {
                Id = id,
                Name = name,
                Description = "A test champion.",
                Role = ChampionRole.Fighter,
                DefaultPosition = FormationPosition.Frontline,
                Health = 100,
                AttackPower = 10,
                AttackSpeed = 1,
                Attack = new ChampionAttack
                {
                    Effects =
                    [
                        new AttackEffect
                        {
                            Type = CombatEffectType.Damage,
                            TargetMode = TargetMode.EnemyFrontline,
                            TargetScope = TargetScope.One
                        }
                    ]
                },
                Ability = new ChampionAbility
                {
                    Id = $"{id}-ability",
                    Name = "Test Ability",
                    Cooldown = 1,
                    CastTime = 0.1,
                    Effects =
                    [
                        new AbilityEffect
                        {
                            Type = CombatEffectType.Damage,
                            AbilityPower = 10,
                            TargetMode = TargetMode.EnemyFrontline,
                            TargetScope = TargetScope.One
                        }
                    ]
                }
            };
    }
}
