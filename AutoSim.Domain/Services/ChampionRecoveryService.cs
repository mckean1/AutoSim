using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Handles passive and base health recovery.
    /// </summary>
    public sealed class ChampionRecoveryService
    {
        private readonly RoundSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChampionRecoveryService"/> class.
        /// </summary>
        /// <param name="settings">The round settings.</param>
        public ChampionRecoveryService(RoundSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Applies regeneration to living champions.
        /// </summary>
        /// <param name="champions">The champions to recover.</param>
        /// <param name="deltaSeconds">The elapsed time in seconds.</param>
        public void ApplyRegeneration(IEnumerable<ChampionInstance> champions, double deltaSeconds)
        {
            ArgumentNullException.ThrowIfNull(champions);

            foreach (ChampionInstance champion in champions)
            {
                if (!champion.IsAlive)
                {
                    continue;
                }

                double healing = _settings.PassiveHealthRegenPerSecond * deltaSeconds;
                if (IsNearOwnBase(champion))
                {
                    healing += champion.MaximumHealth * _settings.BaseHealPercentPerSecond * deltaSeconds;
                }

                AddHealing(champion, healing);
            }
        }

        private bool IsNearOwnBase(ChampionInstance champion) =>
            champion.TeamSide == TeamSide.Blue
                ? champion.LanePosition <= -100.0 + _settings.BaseHealRange
                : champion.LanePosition >= 100.0 - _settings.BaseHealRange;

        private static void AddHealing(ChampionInstance champion, double healing)
        {
            champion.HealingProgress += healing;
            int wholeHealing = (int)Math.Floor(champion.HealingProgress);
            if (wholeHealing <= 0)
            {
                return;
            }

            champion.HealingProgress -= wholeHealing;
            CombatEffectApplicator.ApplyHeal(champion, wholeHealing);
        }
    }
}
