using AutoSim.Domain.Enums;
using AutoSim.Domain.Management.Interfaces;
using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Objects;
using AutoSim.Domain.Services;
using ConsoleApp.Objects;
using MatchType = AutoSim.Domain.Enums.MatchType;

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
        public void ExecuteCommand_Help_RendersCommandReferenceScreen()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);

            string output = application.ExecuteCommand("help");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Help"));
                Assert.That(output, Does.Contain("General"));
                Assert.That(output, Does.Contain("Management"));
                Assert.That(output, Does.Contain("Replay"));
                Assert.That(output, Does.Contain("Commands: home | back | help"));
            });
        }

        [Test]
        public void ExecuteCommand_ShowHelp_RendersHelpScreen()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);

            string output = application.ExecuteCommand("show help");

            Assert.That(output, Does.Contain("Help"));
        }

        [Test]
        public void ExecuteCommand_EmptyWorldCommand_ShowsFriendlyMessageInScreenShell()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);

            string output = application.ExecuteCommand("show team");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("No world has been created yet. Use `start` to begin a new game."));
                Assert.That(output, Does.Contain("║"));
            });
        }

        [Test]
        public void ExecuteCommand_InitialState_RendersNoWorldScreen()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);

            string output = application.ExecuteCommand("home");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Welcome to AutoSim"));
                Assert.That(output, Does.Contain("No game world has been created yet."));
                Assert.That(output, Does.Contain("Commands: start | help"));
            });
        }

        [Test]
        public void ExecuteCommand_Start_EntersNewGameSetupCoachStep()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);

            string output = application.ExecuteCommand("start");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("New Game Setup"));
                Assert.That(output, Does.Contain("Create Your Coach"));
                Assert.That(output, Does.Contain("Coach Name: _"));
                Assert.That(output, Does.Contain("Commands: cancel | help"));
            });
        }

        [Test]
        public void ExecuteCommand_SetupCoachName_Invalid_RerendersWithValidationMessage()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            application.ExecuteCommand("start");

            string output = application.ExecuteCommand(" ");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Coach name must be between 2 and 40 characters."));
                Assert.That(output, Does.Contain("Create Your Coach"));
            });
        }

        [Test]
        public void ExecuteCommand_SetupCoachName_Valid_AdvancesToTeamStep()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            application.ExecuteCommand("start");

            string output = application.ExecuteCommand("McKean");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Create Your Team"));
                Assert.That(output, Does.Contain("Coach Name: McKean"));
                Assert.That(output, Does.Contain("Team Name: _"));
                Assert.That(output, Does.Contain("Commands: back | cancel | help"));
            });
        }

        [Test]
        public void ExecuteCommand_SetupBackFromTeamStep_ReturnsToCoachStep()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            application.ExecuteCommand("start");
            application.ExecuteCommand("McKean");

            string output = application.ExecuteCommand("back");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Create Your Coach"));
                Assert.That(output, Does.Contain("Coach Name: McKean"));
            });
        }

        [Test]
        public void ExecuteCommand_SetupCancel_ClearsStateAndReturnsToNoWorldScreen()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            application.ExecuteCommand("start");
            application.ExecuteCommand("McKean");

            string output = application.ExecuteCommand("cancel");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("New game setup cancelled."));
                Assert.That(output, Does.Contain("No game world has been created yet."));
            });
        }

        [Test]
        public void ExecuteCommand_SetupTeamName_Invalid_DoesNotGenerateWorld()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            application.ExecuteCommand("start");
            application.ExecuteCommand("McKean");

            string output = application.ExecuteCommand(" ");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Team name must be between 2 and 50 characters."));
                Assert.That(output, Does.Contain("Create Your Team"));
                Assert.That(output, Does.Contain("Team Name: _"));
            });
        }

        [Test]
        public void ExecuteCommand_SetupComplete_GeneratesWorldUsingEnteredNames()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            application.ExecuteCommand("start");
            application.ExecuteCommand("McKean");

            string output = application.ExecuteCommand("Salt Lake Strikers");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Home"));
                Assert.That(output, Does.Contain("New game created. Coach: McKean | Team: Salt Lake Strikers"));
                Assert.That(output, Does.Contain("Coach: McKean"));
                Assert.That(output, Does.Contain("Team: Salt Lake Strikers"));
            });
        }

        [Test]
        public void ExecuteCommand_StartWhenWorldExists_DoesNotOverwriteWorld()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = CreateStartedApplication(directory);
            string originalTeamOutput = application.ExecuteCommand("show team");

            string output = application.ExecuteCommand("start");
            string afterOutput = application.ExecuteCommand("show team");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("A game world already exists."));
                Assert.That(afterOutput, Is.EqualTo(originalTeamOutput));
            });
        }

        [Test]
        public void ExecuteCommand_HelpDuringSetup_ReturnsToSetupOnBack()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            application.ExecuteCommand("start");
            application.ExecuteCommand("McKean");
            string helpOutput = application.ExecuteCommand("help");

            string output = application.ExecuteCommand("back");

            Assert.Multiple(() =>
            {
                Assert.That(helpOutput, Does.Contain("Begin new game setup."));
                Assert.That(helpOutput, Does.Contain("type a name"));
                Assert.That(output, Does.Contain("Create Your Team"));
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
        public void ExecuteCommand_ManagementCommands_RunNewGameFlow()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);

            string startOutput = application.ExecuteCommand("start");
            string coachOutput = application.ExecuteCommand("Coach Carter");
            string setupCompleteOutput = application.ExecuteCommand("Salt Lake Strikers");
            string teamOutput = application.ExecuteCommand("show team");
            string leagueOutput = application.ExecuteCommand("show league");
            string scheduleOutput = application.ExecuteCommand("show schedule");
            string matchOutput = application.ExecuteCommand("start match");

            Assert.Multiple(() =>
            {
                Assert.That(startOutput, Does.Contain("Create Your Coach"));
                Assert.That(coachOutput, Does.Contain("Create Your Team"));
                Assert.That(setupCompleteOutput, Does.Contain("New game created."));
                Assert.That(setupCompleteOutput, Does.Contain("Coach: Coach Carter"));
                Assert.That(setupCompleteOutput, Does.Contain("Team: Salt Lake Strikers"));
                Assert.That(teamOutput, Does.Contain("Coach:"));
                Assert.That(teamOutput, Does.Contain("Roster"));
                Assert.That(leagueOutput, Does.Contain("Standings"));
                Assert.That(scheduleOutput, Does.Contain("Scheduled matches"));
                Assert.That(matchOutput, Does.Contain("Match Preview"));
                Assert.That(matchOutput, Does.Contain("show champions"));
            });
        }

        [Test]
        public void ExecuteCommand_StartMatchWithDifferentCasing_RendersMatchPreview()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123, new CountingMatchEngineWrapper());
            CompleteNewGameSetup(application);

            string output = application.ExecuteCommand("Start Match");

            Assert.That(output, Does.Contain("Match Preview"));
        }

        [Test]
        public void ExecuteCommand_UnknownCommand_PrintsFriendlyMessage()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);

            string output = application.ExecuteCommand("start macth");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Unknown command: start macth"));
                Assert.That(output, Does.Contain("Commands: start | help"));
            });
        }

        [Test]
        public void ExecuteCommand_MatchPresentationFlow_RoutesThroughMatchEngineWrapper()
        {
            string directory = CreateTempDirectory();
            CountingMatchEngineWrapper matchEngineWrapper = new();
            ConsoleApplication application = new(directory, () => 123, matchEngineWrapper);
            CompleteNewGameSetup(application);
            application.ExecuteCommand("start match");
            string draftOutput = application.ExecuteCommand("continue");
            string draftSummaryOutput = application.ExecuteCommand("auto draft");

            string replayOutput = application.ExecuteCommand("continue");

            Assert.Multiple(() =>
            {
                Assert.That(draftOutput, Does.Contain("Draft"));
                Assert.That(draftSummaryOutput, Does.Contain("Draft Summary"));
                Assert.That(matchEngineWrapper.CallCount, Is.GreaterThan(0));
                Assert.That(replayOutput, Does.Contain("Live Replay"));
                Assert.That(replayOutput, Does.Contain("Recent Events"));
            });
        }

        [Test]
        public void ExecuteCommand_MatchPresentationFlow_AdvancesExactlyOneWeek()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123, new CountingMatchEngineWrapper());
            CompleteNewGameSetup(application);

            application.ExecuteCommand("start match");
            application.ExecuteCommand("continue");
            application.ExecuteCommand("auto draft");
            application.ExecuteCommand("continue");
            application.ExecuteCommand("skip");
            string firstOutput = application.ExecuteCommand("match summary");
            application.ExecuteCommand("continue");
            application.ExecuteCommand("start match");
            application.ExecuteCommand("continue");
            application.ExecuteCommand("auto draft");
            string secondOutput = application.ExecuteCommand("continue");

            Assert.Multiple(() =>
            {
                Assert.That(firstOutput, Does.Contain("Final match score:"));
                Assert.That(secondOutput, Does.Contain("Week 3"));
            });
        }

        [Test]
        public void ExecuteCommand_LiveReplayStep_ShowsRecentReadableMessages()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123, new CountingMatchEngineWrapper());
            CompleteNewGameSetup(application);
            application.ExecuteCommand("start match");
            application.ExecuteCommand("continue");
            application.ExecuteCommand("auto draft");
            application.ExecuteCommand("continue");

            string output = application.ExecuteCommand("step");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Live Replay"));
                Assert.That(output, Does.Contain("Recent Events"));
                Assert.That(output, Does.Contain("hits").Or.Contain("fight"));
                Assert.That(output, Does.Not.Contain("ChampionInstance"));
            });
        }

        [Test]
        public void ExecuteCommand_SkipReplay_ShowsRoundAndMatchSummaries()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123, new CountingMatchEngineWrapper());
            CompleteNewGameSetup(application);
            application.ExecuteCommand("start match");
            application.ExecuteCommand("continue");
            application.ExecuteCommand("auto draft");
            application.ExecuteCommand("continue");

            string roundSummary = application.ExecuteCommand("skip");
            string matchSummary = application.ExecuteCommand("match summary");

            Assert.Multiple(() =>
            {
                Assert.That(roundSummary, Does.Contain("Round Summary"));
                Assert.That(roundSummary, Does.Contain("Key moments"));
                Assert.That(matchSummary, Does.Contain("Match Summary"));
                Assert.That(matchSummary, Does.Contain("Final match score:"));
            });
        }

        [Test]
        public void ExecuteCommand_ViewLastMatchWithoutCompletedMatch_ShowsHelpfulMessage()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);

            string output = application.ExecuteCommand("view last match");

            Assert.That(output, Does.Contain("No completed match is available yet."));
        }

        [Test]
        public void ExecuteCommand_ViewLastMatchAfterCompletedMatch_RendersReview()
        {
            ConsoleApplication application = CreateApplicationWithCompletedMatch();
            application.ExecuteCommand("home");

            string output = application.ExecuteCommand("view last match");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Last Match Review"));
                Assert.That(output, Does.Contain("Round Results"));
                Assert.That(output, Does.Contain("Key Moments"));
            });
        }

        [Test]
        public void ExecuteCommand_ViewRoundsAfterCompletedMatch_RendersRoundList()
        {
            ConsoleApplication application = CreateApplicationWithCompletedMatch();
            application.ExecuteCommand("home");

            string output = application.ExecuteCommand("view rounds");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Round List"));
                Assert.That(output, Does.Contain("Round"));
                Assert.That(output, Does.Contain("Winner"));
            });
        }

        [Test]
        public void ExecuteCommand_ViewRoundAfterCompletedMatch_RendersRoundReview()
        {
            ConsoleApplication application = CreateApplicationWithCompletedMatch();
            application.ExecuteCommand("home");

            string output = application.ExecuteCommand("view round 1");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Round Review"));
                Assert.That(output, Does.Contain("Champion stats are not available for this round yet."));
                Assert.That(output, Does.Contain("Replay Preview"));
            });
        }

        [Test]
        public void ExecuteCommand_ViewRoundInvalid_ShowsHelpfulMessage()
        {
            ConsoleApplication application = CreateApplicationWithCompletedMatch();
            application.ExecuteCommand("view last match");

            string output = application.ExecuteCommand("view round 99");

            Assert.That(output, Does.Contain("Round 99 is not available."));
        }

        [Test]
        public void ExecuteCommand_ViewReplayAfterCompletedMatch_RendersPagedReplayReview()
        {
            ConsoleApplication application = CreateApplicationWithCompletedMatch();
            application.ExecuteCommand("home");

            string output = application.ExecuteCommand("view replay");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Replay Review"));
                Assert.That(output, Does.Contain("Page 1 of"));
                Assert.That(output, Does.Contain("Replay Messages"));
                Assert.That(output, Does.Not.Contain("ChampionInstance"));
            });
        }

        [Test]
        public void ExecuteCommand_ReplayReviewPaging_RerendersReplayReview()
        {
            ConsoleApplication application = CreateApplicationWithCompletedMatch();
            application.ExecuteCommand("view replay");

            string nextPage = application.ExecuteCommand("next page");
            string previousPage = application.ExecuteCommand("previous page");

            Assert.Multiple(() =>
            {
                Assert.That(nextPage, Does.Contain("Replay Review"));
                Assert.That(previousPage, Does.Contain("Replay Review"));
                Assert.That(previousPage, Does.Contain("Page 1 of"));
            });
        }

        [Test]
        public void ExecuteCommand_BackFromReplayReview_ReturnsPreviousReviewScreen()
        {
            ConsoleApplication application = CreateApplicationWithCompletedMatch();
            application.ExecuteCommand("view round 1");
            application.ExecuteCommand("view replay");

            string output = application.ExecuteCommand("back");

            Assert.That(output, Does.Contain("Round Review"));
        }

        [Test]
        public void ExecuteCommand_HomeAfterShowingTeam_RendersHomeScreen()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);
            application.ExecuteCommand("show team");

            string output = application.ExecuteCommand("home");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Home"));
                Assert.That(output, Does.Contain("Next match:"));
                Assert.That(output, Does.Contain("Recommended action:"));
            });
        }

        [Test]
        public void ExecuteCommand_ShowChampions_RendersChampionCatalog()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);

            string output = application.ExecuteCommand("show champions");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Champion Catalog"));
                Assert.That(output, Does.Contain("Champion"));
                Assert.That(output, Does.Contain("Quickshot"));
                Assert.That(output, Does.Contain("Commands: home | back | show champion <name>"));
            });
        }

        [Test]
        public void ExecuteCommand_ShowChampionCaseInsensitive_RendersChampionDetail()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);

            string output = application.ExecuteCommand("show champion quickshot");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Champion Detail"));
                Assert.That(output, Does.Contain("Quickshot"));
                Assert.That(output, Does.Contain("Role: Marksman"));
                Assert.That(output, Does.Contain("Ability"));
            });
        }

        [Test]
        public void ExecuteCommand_ShowChampionPartialMatch_RendersChampionDetail()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);

            string output = application.ExecuteCommand("show champion quick");

            Assert.That(output, Does.Contain("Quickshot"));
        }

        [Test]
        public void ExecuteCommand_ShowChampionAmbiguousMatch_ShowsHelpfulMessage()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            application.ExecuteCommand("show champions");

            string output = application.ExecuteCommand("show champion shot");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Multiple champions match:"));
                Assert.That(output, Does.Contain("Longshot"));
                Assert.That(output, Does.Contain("Quickshot"));
            });
        }

        [Test]
        public void ExecuteCommand_ShowChampionInvalidMatch_ShowsHelpfulMessage()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            application.ExecuteCommand("show champions");

            string output = application.ExecuteCommand("show champion nope");

            Assert.That(output, Does.Contain("No champion found matching: nope"));
        }

        [Test]
        public void ExecuteCommand_ShowPlayerExactMatch_RendersPlayerDetail()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);
            string teamOutput = application.ExecuteCommand("show team");
            string playerName = ExtractRosterPlayerName(teamOutput);

            string output = application.ExecuteCommand($"show player {playerName}");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Player Detail"));
                Assert.That(output, Does.Contain(playerName));
                Assert.That(output, Does.Contain("Contracts are not implemented yet."));
                Assert.That(output, Does.Contain("Recent player performance is not available yet."));
            });
        }

        [Test]
        public void ExecuteCommand_ShowPlayerCaseInsensitive_RendersPlayerDetail()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);
            string playerName = ExtractRosterPlayerName(application.ExecuteCommand("show team"));

            string output = application.ExecuteCommand($"show player {playerName.ToUpperInvariant()}");

            Assert.That(output, Does.Contain("Player Detail"));
        }

        [Test]
        public void ExecuteCommand_ShowPlayerPartialMatch_RendersPlayerDetailWhenUnambiguous()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);
            string playerName = ExtractRosterPlayerName(application.ExecuteCommand("show team"));
            string partial = playerName.Split(' ')[0];

            string output = application.ExecuteCommand($"show player {partial}");

            Assert.That(output, Does.Contain("Player Detail"));
        }

        [Test]
        public void ExecuteCommand_ShowPlayerInvalidMatch_ShowsHelpfulMessage()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);

            string output = application.ExecuteCommand("show player nope");

            Assert.That(output, Does.Contain("No player found matching: nope"));
        }

        [Test]
        public void ExecuteCommand_ShowPlayerAmbiguousMatch_ShowsHelpfulMessage()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);

            string output = application.ExecuteCommand("show player a");

            Assert.That(output, Does.Contain("Multiple players match:").Or.Contain("Player Detail"));
        }

        [Test]
        public void ExecuteCommand_ShowSpecificTeamExactMatch_RendersTeamDetail()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            string startOutput = CompleteNewGameSetup(application);
            string teamName = ExtractFieldValue(startOutput, "Team:");

            string output = application.ExecuteCommand($"show team {teamName}");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Team Detail"));
                Assert.That(output, Does.Contain(teamName));
                Assert.That(output, Does.Contain("Roster"));
            });
        }

        [Test]
        public void ExecuteCommand_ShowSpecificTeamCaseInsensitive_RendersTeamDetail()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            string teamName = ExtractFieldValue(CompleteNewGameSetup(application), "Team:");

            string output = application.ExecuteCommand($"show team {teamName.ToUpperInvariant()}");

            Assert.That(output, Does.Contain("Team Detail"));
        }

        [Test]
        public void ExecuteCommand_ShowSpecificTeamPartialMatch_RendersTeamDetailWhenUnambiguous()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            string teamName = ExtractFieldValue(CompleteNewGameSetup(application), "Team:");
            string partial = teamName.Split(' ')[0];

            string output = application.ExecuteCommand($"show team {partial}");

            Assert.That(output, Does.Contain("Team Detail").Or.Contain("Multiple teams match:"));
        }

        [Test]
        public void ExecuteCommand_ShowSpecificTeamInvalidMatch_ShowsHelpfulMessage()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);

            string output = application.ExecuteCommand("show team nope");

            Assert.That(output, Does.Contain("No team found matching: nope"));
        }

        [Test]
        public void ExecuteCommand_ShowSpecificTeamAmbiguousMatch_ShowsHelpfulMessage()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);

            string output = application.ExecuteCommand("show team a");

            Assert.That(output, Does.Contain("Multiple teams match:").Or.Contain("Team Detail"));
        }

        [Test]
        public void ExecuteCommand_ShowOpponentFromHome_RendersOpponentWhenAvailable()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);

            string output = application.ExecuteCommand("show opponent");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Team Detail"));
                Assert.That(output, Does.Contain("Opponent:"));
            });
        }

        [Test]
        public void ExecuteCommand_ShowOpponentWithoutWorld_ShowsHelpfulMessage()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);

            string output = application.ExecuteCommand("show opponent");

            Assert.That(output, Does.Contain("No world has been created yet. Use `start` to begin a new game."));
        }

        [Test]
        public void ExecuteCommand_ShowPlayoffs_RendersPlaceholderScreen()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);

            string output = application.ExecuteCommand("show playoffs");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Playoff Picture"));
                Assert.That(output, Does.Contain("Regular season lasts 23 weeks."));
                Assert.That(output, Does.Contain("Week 24: League Quarterfinals, best-of-5"));
            });
        }

        [Test]
        public void ExecuteCommand_ShowPlayoffPicture_RendersPlaceholderScreen()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);

            string output = application.ExecuteCommand("show playoff picture");

            Assert.That(output, Does.Contain("Playoff Picture"));
        }

        [Test]
        public void ExecuteCommand_FilterRole_UpdatesChampionCatalog()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            application.ExecuteCommand("show champions");

            string output = application.ExecuteCommand("filter role mage");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Filtered role: Mage"));
                Assert.That(output, Does.Contain("Ember Sage"));
                Assert.That(output, Does.Not.Contain("Quickshot"));
            });
        }

        [Test]
        public void ExecuteCommand_ClearFilter_ReturnsChampionCatalogToAllChampions()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            application.ExecuteCommand("show champions");
            application.ExecuteCommand("filter role support");

            string output = application.ExecuteCommand("clear filter");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("All champions"));
                Assert.That(output, Does.Contain("Quickshot"));
                Assert.That(output, Does.Contain("Lifewarden"));
            });
        }

        [Test]
        public void ExecuteCommand_FilterRoleInvalid_ShowsHelpfulMessage()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            application.ExecuteCommand("show champions");

            string output = application.ExecuteCommand("filter role assassin");

            Assert.That(output, Does.Contain("Unknown role: assassin"));
        }

        [Test]
        public void ExecuteCommand_BackFromChampionDetailOpenedFromCatalog_ReturnsCatalog()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            application.ExecuteCommand("show champions");
            application.ExecuteCommand("show champion quickshot");

            string output = application.ExecuteCommand("back");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Champion Catalog"));
                Assert.That(output, Does.Contain("Quickshot"));
            });
        }

        [Test]
        public void ExecuteCommand_BackFromChampionDetailOpenedFromDraft_ReturnsDraft()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123, new CountingMatchEngineWrapper());
            CompleteNewGameSetup(application);
            application.ExecuteCommand("start match");
            application.ExecuteCommand("continue");
            application.ExecuteCommand("show champion quickshot");

            string output = application.ExecuteCommand("back");

            Assert.That(output, Does.Contain("Draft"));
        }

        [Test]
        public void ExecuteCommand_BackWithoutPreviousScreen_ShowsHelpfulMessage()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);

            string output = application.ExecuteCommand("back");

            Assert.That(output, Does.Contain("No previous screen is available."));
        }

        [Test]
        public void ExecuteCommand_BackAfterHelp_ReturnsPreviousScreen()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application);
            application.ExecuteCommand("show team");
            application.ExecuteCommand("help");

            string output = application.ExecuteCommand("back");

            Assert.That(output, Does.Contain("Team Detail"));
        }

        [Test]
        public void ExecuteCommand_SimulateRounds_WritesRequestedLogsAndPrintsAggregateResults()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 1000);

            string output = application.ExecuteCommand("simulate rounds 2");
            string[] logs = Directory.GetFiles(directory, "*.jsonl");
            string[] reports = Directory.GetFiles(directory, "simulation_summary_*.txt");

            Assert.Multiple(() =>
            {
                Assert.That(logs, Has.Length.EqualTo(2));
                Assert.That(reports, Has.Length.EqualTo(1));
                Assert.That(output, Does.Contain("Simulated 2 rounds."));
                Assert.That(output, Does.Contain("Aggregate report written to:"));
                Assert.That(output, Does.Contain("Analyze all logs with:"));
                Assert.That(output, Does.Contain("analyze rounds"));
                Assert.That(output, Does.Not.Contain("Team Averages"));
                Assert.That(File.ReadAllText(reports.Single()), Does.Contain("Aggregate Results"));
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
            string[] reports = Directory.GetFiles(directory, "aggregate_round_analysis_*.txt");

            Assert.Multiple(() =>
            {
                Assert.That(reports, Has.Length.EqualTo(1));
                Assert.That(output, Does.Contain("Analyzed 1 rounds."));
                Assert.That(output, Does.Contain("Aggregate report written to:"));
                Assert.That(output, Does.Not.Contain("Skipped Logs"));
                string report = File.ReadAllText(reports.Single());
                Assert.That(report, Does.Contain("Aggregate Round Analysis"));
                Assert.That(report, Does.Contain("Rounds analyzed: 1"));
                Assert.That(report, Does.Contain("Skipped Logs"));
                Assert.That(report, Does.Contain("bad.jsonl"));
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

        private static string ExtractFieldValue(string output, string fieldName)
        {
            string normalized = output.Replace('║', '\n');
            string? line = normalized
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .FirstOrDefault(value => value.StartsWith(fieldName, StringComparison.Ordinal));

            Assert.That(line, Is.Not.Null, $"Expected to find field '{fieldName}' in output.");
            return line![fieldName.Length..].Trim();
        }

        private static string ExtractRosterPlayerName(string teamOutput)
        {
            string normalized = teamOutput.Replace('║', '\n');
            string? rosterLine = normalized
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .FirstOrDefault(value => value.StartsWith("Top ", StringComparison.Ordinal)
                    || value.StartsWith("Jungle ", StringComparison.Ordinal)
                    || value.StartsWith("Mid ", StringComparison.Ordinal)
                    || value.StartsWith("Bot ", StringComparison.Ordinal)
                    || value.StartsWith("Support ", StringComparison.Ordinal));

            Assert.That(rosterLine, Is.Not.Null, "Expected a roster line in team output.");
            string[] parts = rosterLine!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', parts.Skip(1));
        }

        private static ConsoleApplication CreateApplicationWithCompletedMatch()
        {
            string directory = CreateTempDirectory();
            ConsoleApplication application = new(directory, () => 123, new CountingMatchEngineWrapper());
            CompleteNewGameSetup(application);
            application.ExecuteCommand("start match");
            application.ExecuteCommand("continue");
            application.ExecuteCommand("auto draft");
            application.ExecuteCommand("continue");
            application.ExecuteCommand("skip");
            application.ExecuteCommand("match summary");
            return application;
        }

        private static string CompleteNewGameSetup(ConsoleApplication application, string coachName = "Coach Carter", string teamName = "Salt Lake Strikers")
        {
            application.ExecuteCommand("start");
            application.ExecuteCommand(coachName);
            return application.ExecuteCommand(teamName);
        }

        private static ConsoleApplication CreateStartedApplication(string directory, string coachName = "Coach Carter", string teamName = "Salt Lake Strikers")
        {
            ConsoleApplication application = new(directory, () => 123);
            CompleteNewGameSetup(application, coachName, teamName);
            return application;
        }

        private sealed class CountingMatchEngineWrapper : IMatchEngineWrapper
        {
            public int CallCount { get; private set; }

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
                _ = blueCoach;
                _ = redCoach;
                _ = players;
                _ = championCatalog;
                _ = seed;
                CallCount++;
                return new MatchResult
                {
                    BestOf = match.BestOf,
                    BlueRoundWins = 2,
                    BlueTeamId = blueTeam.Id,
                    LosingTeamId = redTeam.Id,
                    MatchId = match.Id,
                    MatchType = match.MatchType,
                    RedRoundWins = 0,
                    RedTeamId = redTeam.Id,
                    RoundResults =
                    [
                        new AutoSim.Domain.Management.Models.RoundResult
                        {
                            BlueTeamId = blueTeam.Id,
                            LosingTeamId = redTeam.Id,
                            RedTeamId = redTeam.Id,
                            RoundNumber = 1,
                            WinningTeamId = blueTeam.Id
                        },
                        new AutoSim.Domain.Management.Models.RoundResult
                        {
                            BlueTeamId = blueTeam.Id,
                            LosingTeamId = redTeam.Id,
                            RedTeamId = redTeam.Id,
                            RoundNumber = 2,
                            WinningTeamId = blueTeam.Id
                        }
                    ],
                    WinningTeamId = blueTeam.Id
                };
            }
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
