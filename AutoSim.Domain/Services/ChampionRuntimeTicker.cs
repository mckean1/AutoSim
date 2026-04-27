using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Advances mutable champion runtime timers.
    /// </summary>
    public static class ChampionRuntimeTicker
    {
        /// <summary>
        /// Advances a champion's runtime timers and shield durations.
        /// </summary>
        /// <param name="champion">The champion to tick.</param>
        /// <param name="deltaSeconds">The elapsed time in seconds.</param>
        public static void TickChampion(ChampionInstance champion, double deltaSeconds)
        {
            ArgumentNullException.ThrowIfNull(champion);

            if (!champion.IsAlive)
            {
                champion.Shields.Clear();

                if (deltaSeconds > 0)
                {
                    champion.RespawnTimer = Math.Max(0, champion.RespawnTimer - deltaSeconds);
                }

                return;
            }

            if (deltaSeconds > 0)
            {
                champion.AttackTimer = Math.Max(0, champion.AttackTimer - deltaSeconds);
                champion.AbilityCooldown = Math.Max(0, champion.AbilityCooldown - deltaSeconds);
            }

            CombatEffectApplicator.TickShields(champion, deltaSeconds);
        }
    }
}
