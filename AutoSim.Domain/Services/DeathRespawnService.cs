using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Handles death, respawn, and retreat state transitions.
    /// </summary>
    public sealed class DeathRespawnService
    {
        private readonly ChampionProgressionService _progressionService;
        private readonly RoundSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeathRespawnService"/> class.
        /// </summary>
        /// <param name="settings">The round settings.</param>
        /// <param name="progressionService">The progression service.</param>
        public DeathRespawnService(RoundSettings settings, ChampionProgressionService progressionService)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _progressionService = progressionService ?? throw new ArgumentNullException(nameof(progressionService));
        }

        /// <summary>
        /// Cancels an active ability cast.
        /// </summary>
        /// <param name="champion">The casting champion.</param>
        public static void CancelCast(ChampionInstance champion)
        {
            ArgumentNullException.ThrowIfNull(champion);

            champion.IsCasting = false;
            champion.CastTimer = 0;
            champion.PendingAbility = null;
        }

        /// <summary>
        /// Processes newly dead champions.
        /// </summary>
        /// <param name="state">The round state.</param>
        /// <param name="killer">The possible killing blow source.</param>
        public void ProcessDeaths(RoundState state, ChampionInstance? killer)
        {
            ArgumentNullException.ThrowIfNull(state);

            foreach (ChampionInstance champion in state.AllChampions)
            {
                if (champion.IsAlive || champion.IsDeathProcessed)
                {
                    continue;
                }

                champion.IsDeathProcessed = true;
                champion.Shields.Clear();
                CancelCast(champion);
                champion.FightId = null;
                champion.CurrentFightPosition = null;
                champion.RespawnTimer = _settings.RespawnDurationSeconds;
                GetTeam(state, champion.TeamSide == TeamSide.Blue ? TeamSide.Red : TeamSide.Blue).KillScore++;

                if (killer is not null && killer.IsAlive && killer.TeamSide != champion.TeamSide)
                {
                    _progressionService.AddExperience(killer, _settings.KillXp);
                }
            }
        }

        /// <summary>
        /// Respawns champions whose respawn timer has completed.
        /// </summary>
        /// <param name="champions">The champions to evaluate.</param>
        public static void RespawnReadyChampions(IEnumerable<ChampionInstance> champions)
        {
            ArgumentNullException.ThrowIfNull(champions);

            foreach (ChampionInstance champion in champions.Where(
                champion => !champion.IsAlive && champion.IsDeathProcessed && champion.RespawnTimer <= 0))
            {
                champion.CurrentHealth = champion.MaximumHealth;
                champion.JustRespawned = true;
                champion.IsDeathProcessed = false;
                champion.Shields.Clear();
                champion.Position = champion.Definition.DefaultPosition;
                champion.Intent = ChampionIntent.Laning;
                champion.FightId = null;
                champion.CurrentFightPosition = null;
                CancelCast(champion);
                champion.AbilityCooldown = champion.Definition.Ability.Cooldown;
                champion.AttackTimer = champion.Definition.AttackSpeed > 0 ? 1.0 / champion.Definition.AttackSpeed : 0;
                champion.LanePosition = champion.TeamSide == TeamSide.Blue ? -100.0 : 100.0;
            }
        }

        /// <summary>
        /// Updates retreating and laning intent based on current health.
        /// </summary>
        /// <param name="champions">The champions to evaluate.</param>
        public void UpdateRetreatIntents(IEnumerable<ChampionInstance> champions)
        {
            ArgumentNullException.ThrowIfNull(champions);

            foreach (ChampionInstance champion in champions.Where(champion => champion.IsAlive))
            {
                double healthPercent = champion.MaximumHealth <= 0
                    ? 0
                    : champion.CurrentHealth / (double)champion.MaximumHealth;

                if (champion.Intent == ChampionIntent.Laning && healthPercent < _settings.RetreatHealthThreshold)
                {
                    champion.Intent = ChampionIntent.Retreating;
                    CancelCast(champion);
                }
                else if (champion.Intent == ChampionIntent.Retreating && champion.CurrentHealth >= champion.MaximumHealth)
                {
                    champion.Intent = ChampionIntent.Laning;
                }
            }
        }

        /// <summary>
        /// Clears same-tick respawn markers.
        /// </summary>
        /// <param name="champions">The champions to update.</param>
        public static void ClearRespawnMarkers(IEnumerable<ChampionInstance> champions)
        {
            ArgumentNullException.ThrowIfNull(champions);

            foreach (ChampionInstance champion in champions)
            {
                champion.JustRespawned = false;
            }
        }

        private static TeamRoundState GetTeam(RoundState state, TeamSide side) =>
            side == TeamSide.Blue ? state.BlueTeam : state.RedTeam;
    }
}
