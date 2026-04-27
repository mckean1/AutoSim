using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Handles champion gold, experience, and level progression.
    /// </summary>
    public sealed class ChampionProgressionService
    {
        private readonly RoundSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChampionProgressionService"/> class.
        /// </summary>
        /// <param name="settings">The round settings.</param>
        public ChampionProgressionService(RoundSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Gets the total earned experience required to reach a level.
        /// </summary>
        /// <param name="level">The target level.</param>
        /// <returns>The cumulative experience threshold.</returns>
        public static int GetTotalExperienceRequiredForLevel(int level)
        {
            if (level <= 1)
            {
                return 0;
            }

            return ((level - 1) * level / 2) * 100;
        }

        /// <summary>
        /// Adds whole experience and applies resulting level ups.
        /// </summary>
        /// <param name="champion">The champion receiving experience.</param>
        /// <param name="experience">The experience to add.</param>
        public void AddExperience(ChampionInstance champion, int experience)
        {
            ArgumentNullException.ThrowIfNull(champion);

            if (experience <= 0)
            {
                return;
            }

            champion.Experience += experience;
            champion.ExperienceProgress += experience;
            ApplyLevelUps(champion);
        }

        /// <summary>
        /// Adds fractional experience and applies resulting level ups.
        /// </summary>
        /// <param name="champion">The champion receiving experience.</param>
        /// <param name="experience">The experience to add.</param>
        public void AddExperience(ChampionInstance champion, double experience)
        {
            ArgumentNullException.ThrowIfNull(champion);

            if (experience <= 0)
            {
                return;
            }

            champion.ExperienceProgress += experience;
            int wholeExperience = (int)Math.Floor(champion.ExperienceProgress);
            if (wholeExperience > champion.Experience)
            {
                champion.Experience = wholeExperience;
                ApplyLevelUps(champion);
            }
        }

        /// <summary>
        /// Adds fractional gold.
        /// </summary>
        /// <param name="champion">The champion receiving gold.</param>
        /// <param name="gold">The gold to add.</param>
        public static void AddGold(ChampionInstance champion, double gold)
        {
            ArgumentNullException.ThrowIfNull(champion);

            if (gold <= 0)
            {
                return;
            }

            champion.GoldProgress += gold;
            int wholeGold = (int)Math.Floor(champion.GoldProgress);
            if (wholeGold > champion.Gold)
            {
                champion.Gold = wholeGold;
            }
        }

        /// <summary>
        /// Applies all level ups earned by the champion's total experience.
        /// </summary>
        /// <param name="champion">The champion to level.</param>
        public void ApplyLevelUps(ChampionInstance champion)
        {
            ArgumentNullException.ThrowIfNull(champion);

            while (champion.Level < _settings.MaxLevel
                && champion.Experience >= GetTotalExperienceRequiredForLevel(champion.Level + 1))
            {
                champion.Level++;
                champion.MaximumHealth += _settings.HealthPerLevel;
                champion.CurrentHealth = Math.Min(
                    champion.MaximumHealth,
                    champion.CurrentHealth + _settings.HealthPerLevel);
                champion.CurrentPower += _settings.PowerPerLevel;
            }
        }

        /// <summary>
        /// Applies farming rewards to eligible champions.
        /// </summary>
        /// <param name="champions">The champions to evaluate.</param>
        /// <param name="deltaSeconds">The elapsed time in seconds.</param>
        public void ApplyFarming(IEnumerable<ChampionInstance> champions, double deltaSeconds)
        {
            ArgumentNullException.ThrowIfNull(champions);

            foreach (ChampionInstance champion in champions)
            {
                if (!champion.IsAlive
                    || champion.JustRespawned
                    || champion.Intent != Enums.ChampionIntent.Laning
                    || champion.FightId.HasValue)
                {
                    continue;
                }

                AddGold(champion, _settings.FarmGoldPerSecond * deltaSeconds);
                AddExperience(champion, _settings.FarmXpPerSecond * deltaSeconds);
            }
        }
    }
}
