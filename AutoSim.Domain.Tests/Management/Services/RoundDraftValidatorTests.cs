using AutoSim.Domain.Management.Models;
using AutoSim.Domain.Management.Services;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Tests.Management.Services
{
    internal sealed class RoundDraftValidatorTests
    {
        [Test]
        public void Validate_BlueHasFewerThanFiveChampions_Throws()
        {
            IReadOnlyList<ChampionDefinition> catalog = ChampionCatalog.GetDefaultChampions();
            RoundDraft draft = new()
            {
                BlueChampions = catalog.Take(4).ToList(),
                RedChampions = catalog.Skip(5).Take(5).ToList()
            };

            ArgumentException exception = Assert.Throws<ArgumentException>(
                () => new RoundDraftValidator().Validate(draft, catalog))!;

            Assert.That(exception.Message, Does.Contain("Blue draft must contain exactly 5 champions"));
        }

        [Test]
        public void Validate_BlueHasMoreThanFiveChampions_Throws()
        {
            IReadOnlyList<ChampionDefinition> catalog = ChampionCatalog.GetDefaultChampions();
            RoundDraft draft = new()
            {
                BlueChampions = catalog.Take(6).ToList(),
                RedChampions = catalog.Skip(6).Take(5).ToList()
            };

            ArgumentException exception = Assert.Throws<ArgumentException>(
                () => new RoundDraftValidator().Validate(draft, catalog))!;

            Assert.That(exception.Message, Does.Contain("Blue draft must contain exactly 5 champions"));
        }

        [Test]
        public void Validate_BlueHasDuplicateChampion_Throws()
        {
            IReadOnlyList<ChampionDefinition> catalog = ChampionCatalog.GetDefaultChampions();
            RoundDraft draft = new()
            {
                BlueChampions = [catalog[0], catalog[0], .. catalog.Skip(1).Take(3)],
                RedChampions = catalog.Skip(5).Take(5).ToList()
            };

            ArgumentException exception = Assert.Throws<ArgumentException>(
                () => new RoundDraftValidator().Validate(draft, catalog))!;

            Assert.That(exception.Message, Does.Contain("Blue draft contains duplicate champion"));
        }

        [Test]
        public void Validate_DuplicateChampionAcrossSides_Throws()
        {
            IReadOnlyList<ChampionDefinition> catalog = ChampionCatalog.GetDefaultChampions();
            RoundDraft draft = new()
            {
                BlueChampions = catalog.Take(5).ToList(),
                RedChampions = [catalog[0], .. catalog.Skip(5).Take(4)]
            };

            ArgumentException exception = Assert.Throws<ArgumentException>(
                () => new RoundDraftValidator().Validate(draft, catalog))!;

            Assert.That(exception.Message, Does.Contain("Duplicate champion selected"));
        }
    }
}
