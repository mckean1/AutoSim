using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;
using ConsoleApp.Objects;

namespace ConsoleApp.Tests.Objects
{
    internal sealed class ConsoleApplicationTests
    {
        [Test]
        public void CreateTemporaryRoundRoster_DefaultCatalog_ReturnsLegalUniqueFiveVersusFiveRoster()
        {
            RoundRoster roster = ConsoleApplication.CreateTemporaryRoundRoster(ChampionCatalog.GetDefaultChampions(), seed: 123);
            IReadOnlyList<string> championIds = roster.BlueChampions
                .Concat(roster.RedChampions)
                .Select(champion => champion.Id)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(roster.BlueChampions, Has.Count.EqualTo(5));
                Assert.That(roster.RedChampions, Has.Count.EqualTo(5));
                Assert.That(championIds, Is.Unique);
            });
        }

        [Test]
        public void CreateTemporaryRoundRoster_DefaultCatalog_ReturnsRoleBalancedTeams()
        {
            RoundRoster roster = ConsoleApplication.CreateTemporaryRoundRoster(ChampionCatalog.GetDefaultChampions(), seed: 123);

            Assert.Multiple(() =>
            {
                AssertRoleBalance(roster.BlueChampions);
                AssertRoleBalance(roster.RedChampions);
            });
        }

        [Test]
        public void CreateTemporaryRoundRoster_DefaultCatalog_OrdersRolesForLaneAssignment()
        {
            RoundRoster roster = ConsoleApplication.CreateTemporaryRoundRoster(ChampionCatalog.GetDefaultChampions(), seed: 123);

            Assert.Multiple(() =>
            {
                AssertRoleOrder(roster.BlueChampions);
                AssertRoleOrder(roster.RedChampions);
            });
        }

        [Test]
        public void CreateTemporaryRoundRoster_DefaultCatalog_AssignsRoleBalancedLanes()
        {
            RoundRoster roster = ConsoleApplication.CreateTemporaryRoundRoster(ChampionCatalog.GetDefaultChampions(), seed: 123);
            RoundState state = new RoundEngine().CreateState(roster, seed: 123);

            Assert.Multiple(() =>
            {
                AssertLaneRoleAssignments(state.BlueTeam.Champions);
                AssertLaneRoleAssignments(state.RedTeam.Champions);
            });
        }

        [Test]
        public void CreateTemporaryRoundRoster_SameSeed_ReturnsSameRoster()
        {
            IReadOnlyList<ChampionDefinition> catalog = ChampionCatalog.GetDefaultChampions();

            RoundRoster firstRoster = ConsoleApplication.CreateTemporaryRoundRoster(catalog, seed: 123);
            RoundRoster secondRoster = ConsoleApplication.CreateTemporaryRoundRoster(catalog, seed: 123);

            Assert.Multiple(() =>
            {
                Assert.That(
                    secondRoster.BlueChampions.Select(champion => champion.Id),
                    Is.EqualTo(firstRoster.BlueChampions.Select(champion => champion.Id)));
                Assert.That(
                    secondRoster.RedChampions.Select(champion => champion.Id),
                    Is.EqualTo(firstRoster.RedChampions.Select(champion => champion.Id)));
            });
        }

        [Test]
        public void CreateTemporaryRoundRoster_DifferentSeeds_ReturnsDifferentRoster()
        {
            IReadOnlyList<ChampionDefinition> catalog = ChampionCatalog.GetDefaultChampions();

            RoundRoster firstRoster = ConsoleApplication.CreateTemporaryRoundRoster(catalog, seed: 123);
            RoundRoster secondRoster = ConsoleApplication.CreateTemporaryRoundRoster(catalog, seed: 456);
            IReadOnlyList<string> firstChampionIds = firstRoster.BlueChampions
                .Concat(firstRoster.RedChampions)
                .Select(champion => champion.Id)
                .ToList();
            IReadOnlyList<string> secondChampionIds = secondRoster.BlueChampions
                .Concat(secondRoster.RedChampions)
                .Select(champion => champion.Id)
                .ToList();

            Assert.That(secondChampionIds, Is.Not.EqualTo(firstChampionIds));
        }

        [Test]
        public void CreateTemporaryRoundRoster_DefaultCatalog_DoesNotUseStaticFirstFiveThenNextFive()
        {
            IReadOnlyList<ChampionDefinition> catalog = ChampionCatalog.GetDefaultChampions();

            RoundRoster roster = ConsoleApplication.CreateTemporaryRoundRoster(catalog, seed: 123);

            Assert.Multiple(() =>
            {
                Assert.That(
                    roster.BlueChampions.Select(champion => champion.Id),
                    Is.Not.EqualTo(catalog.Take(5).Select(champion => champion.Id)));
                Assert.That(
                    roster.RedChampions.Select(champion => champion.Id),
                    Is.Not.EqualTo(catalog.Skip(5).Take(5).Select(champion => champion.Id)));
            });
        }

        [Test]
        public void CreateTemporaryRoundRoster_CatalogHasFewerThanTenChampions_ThrowsClearException()
        {
            IReadOnlyList<ChampionDefinition> catalog = Enumerable.Range(0, 9)
                .Select(index => CreateDefinition($"test-{index}"))
                .ToList();

            ArgumentException exception = Assert.Throws<ArgumentException>(
                () => ConsoleApplication.CreateTemporaryRoundRoster(catalog, seed: 123))!;

            Assert.That(exception.Message, Does.Contain("Catalog must contain at least 10 champions"));
        }

        [TestCase("simulate rounds", "Usage: simulate rounds <number>")]
        [TestCase("simulate rounds nope", "positive whole number")]
        [TestCase("simulate rounds 0", "greater than zero")]
        [TestCase("simulate rounds -1", "greater than zero")]
        public void ExecuteCommand_InvalidSimulateRoundsInput_PrintsFriendlyError(string command, string expected)
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 1000);

            string output = application.ExecuteCommand(command);

            Assert.That(output, Does.Contain(expected));
        }

        [Test]
        public void ExecuteCommand_SimulateRounds_WritesRequestedLogsAndPrintsAggregateResults()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 1000);

            string output = application.ExecuteCommand("simulate rounds 2");
            string[] logs = Directory.GetFiles(directory, "*.jsonl");

            Assert.Multiple(() =>
            {
                Assert.That(logs, Has.Length.EqualTo(2));
                Assert.That(output, Does.Contain("Simulated 2 rounds."));
                Assert.That(output, Does.Contain("Aggregate Results"));
                Assert.That(output, Does.Contain("Analyze all logs with:"));
                Assert.That(output, Does.Contain("analyze rounds"));
            });
        }

        [Test]
        public void ExecuteCommand_AnalyzeRounds_MissingFolder_PrintsFriendlyMessage()
        {
            string directory = Path.Combine(Path.GetTempPath(), $"autosim-missing-{Guid.NewGuid():N}");
            ConsoleApplication application = new(directory, () => 1000);

            string output = application.ExecuteCommand("analyze rounds");

            Assert.That(output, Does.Contain("Round log folder was not found"));
        }

        [Test]
        public void ExecuteCommand_AnalyzeRounds_EmptyFolder_PrintsFriendlyMessage()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 1000);

            string output = application.ExecuteCommand("analyze rounds");

            Assert.That(output, Does.Contain("No round logs found"));
        }

        [Test]
        public void ExecuteCommand_AnalyzeRounds_ValidAndMalformedLogs_PrintsAggregateAndSkippedLogs()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 1000);
            application.ExecuteCommand("simulate rounds 1");
            File.WriteAllText(Path.Combine(directory, "bad.jsonl"), "{bad json");

            string output = application.ExecuteCommand("analyze rounds");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Aggregate Round Analysis"));
                Assert.That(output, Does.Contain("Rounds analyzed: 1"));
                Assert.That(output, Does.Contain("Skipped Logs"));
                Assert.That(output, Does.Contain("bad.jsonl"));
            });
        }

        private static ChampionDefinition CreateDefinition(string id) =>
            new()
            {
                Id = id,
                Name = id,
                Description = "A test champion used by unit tests.",
                Role = ChampionRole.Fighter,
                DefaultPosition = FormationPosition.Frontline,
                Health = 100,
                AttackPower = 10,
                AttackSpeed = 1.0,
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
                    Cooldown = 1.0,
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

        private static string CreateTempDirectory()
        {
            string directory = Path.Combine(Path.GetTempPath(), $"autosim-rounds-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static void AssertRoleBalance(IReadOnlyList<ChampionDefinition> champions)
        {
            Assert.That(champions.Count(champion => champion.Role == ChampionRole.Fighter), Is.EqualTo(2));
            Assert.That(champions.Count(champion => champion.Role == ChampionRole.Mage), Is.EqualTo(1));
            Assert.That(champions.Count(champion => champion.Role == ChampionRole.Marksman), Is.EqualTo(1));
            Assert.That(champions.Count(champion => champion.Role == ChampionRole.Support), Is.EqualTo(1));
        }

        private static void AssertRoleOrder(IReadOnlyList<ChampionDefinition> champions)
        {
            Assert.That(champions[0].Role, Is.EqualTo(ChampionRole.Fighter));
            Assert.That(champions[1].Role, Is.EqualTo(ChampionRole.Fighter));
            Assert.That(champions[2].Role, Is.EqualTo(ChampionRole.Mage));
            Assert.That(champions[3].Role, Is.EqualTo(ChampionRole.Marksman));
            Assert.That(champions[4].Role, Is.EqualTo(ChampionRole.Support));
        }

        private static void AssertLaneRoleAssignments(IList<ChampionInstance> champions)
        {
            Assert.That(champions.Select(champion => champion.Lane), Is.EqualTo(
                new[] { Lane.Top, Lane.Top, Lane.Mid, Lane.Bottom, Lane.Bottom }));
            Assert.That(champions.Take(2).Select(champion => champion.Definition.Role), Is.All.EqualTo(ChampionRole.Fighter));
            Assert.That(champions[2].Definition.Role, Is.EqualTo(ChampionRole.Mage));
            Assert.That(champions.Skip(3).Select(champion => champion.Definition.Role), Is.EquivalentTo(
                new[] { ChampionRole.Marksman, ChampionRole.Support }));
        }
    }
}
