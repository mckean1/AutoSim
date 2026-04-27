using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Management.Services;

namespace AutoSim.Domain.Tests.Management.Services
{
    internal sealed class PlayoffSeedingServiceTests
    {
        [Test]
        public void GetLeaguePlayoffSeeds_CompletedStandings_ReturnsEightSeeds()
        {
            WorldState world = new WorldGenerationService().CreateWorld(seed: 123);
            League league = world.Tiers.First().Leagues.First();
            IReadOnlyList<string> seeds = new PlayoffSeedingService().GetLeaguePlayoffSeeds(league);

            Assert.Multiple(() =>
            {
                Assert.That(seeds, Has.Count.EqualTo(8));
                Assert.That(seeds, Is.Unique);
                Assert.That(seeds.Take(4), Is.SubsetOf(league.Teams.Select(team => team.Id).ToList()));
            });
        }
    }
}
