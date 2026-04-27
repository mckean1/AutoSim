using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Handles champion lane and fight movement.
    /// </summary>
    public sealed class ChampionMovementService
    {
        private readonly RoundSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChampionMovementService"/> class.
        /// </summary>
        /// <param name="settings">The round settings.</param>
        public ChampionMovementService(RoundSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Moves all living champions according to their current round state.
        /// </summary>
        /// <param name="state">The round state.</param>
        /// <param name="deltaSeconds">The elapsed time in seconds.</param>
        public void MoveChampions(RoundState state, double deltaSeconds)
        {
            ArgumentNullException.ThrowIfNull(state);

            foreach (ChampionInstance champion in state.AllChampions.Where(champion => champion.IsAlive))
            {
                if (champion.JustRespawned)
                {
                    continue;
                }

                double targetPosition = GetMovementTarget(champion, state);
                double speed = GetMovementSpeed(champion);
                MoveToward(champion, targetPosition, speed * deltaSeconds);
            }
        }

        private double GetMovementTarget(ChampionInstance champion, RoundState state)
        {
            if (champion.Intent == ChampionIntent.Retreating)
            {
                return champion.TeamSide == TeamSide.Blue ? -100.0 : 100.0;
            }

            FightState? fight = FightService.GetFight(state, champion.FightId);
            if (fight is null)
            {
                return champion.TeamSide == TeamSide.Blue ? 100.0 : -100.0;
            }

            if (champion.Position == FormationPosition.Backline)
            {
                return champion.TeamSide == TeamSide.Blue
                    ? fight.Position - state.Settings.BacklineOffset
                    : fight.Position + state.Settings.BacklineOffset;
            }

            return fight.Position;
        }

        private double GetMovementSpeed(ChampionInstance champion)
        {
            if (champion.Intent == ChampionIntent.Retreating)
            {
                return _settings.RetreatMoveSpeed;
            }

            return champion.FightId.HasValue ? _settings.FightMoveSpeed : _settings.LaneMoveSpeed;
        }

        private static void MoveToward(ChampionInstance champion, double targetPosition, double maxDistance)
        {
            double distance = targetPosition - champion.LanePosition;
            double movement = Math.Abs(distance) <= maxDistance
                ? distance
                : Math.Sign(distance) * maxDistance;

            champion.LanePosition = Math.Clamp(champion.LanePosition + movement, -100.0, 100.0);
        }
    }
}
