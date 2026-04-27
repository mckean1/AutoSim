using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Handles fight creation, joining, active participant checks, and ending.
    /// </summary>
    public sealed class FightService
    {
        private readonly ChampionProgressionService _progressionService;
        private readonly RoundSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="FightService"/> class.
        /// </summary>
        /// <param name="settings">The round settings.</param>
        /// <param name="progressionService">The progression service.</param>
        public FightService(RoundSettings settings, ChampionProgressionService progressionService)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _progressionService = progressionService ?? throw new ArgumentNullException(nameof(progressionService));
        }

        /// <summary>
        /// Gets a fight by identifier.
        /// </summary>
        /// <param name="state">The round state.</param>
        /// <param name="fightId">The fight identifier.</param>
        /// <returns>The fight if present; otherwise null.</returns>
        public static FightState? GetFight(RoundState state, Guid? fightId) =>
            fightId.HasValue
                ? state.ActiveFights.FirstOrDefault(fight => fight.Id == fightId.Value)
                : null;

        /// <summary>
        /// Gets living active participants for a fight.
        /// </summary>
        /// <param name="fight">The fight.</param>
        /// <param name="settings">The round settings.</param>
        /// <returns>The active participants.</returns>
        public static IReadOnlyList<ChampionInstance> GetActiveParticipants(FightState fight, RoundSettings settings)
        {
            ArgumentNullException.ThrowIfNull(fight);
            ArgumentNullException.ThrowIfNull(settings);

            return fight.Participants
                .Where(champion => champion.IsAlive)
                .Where(champion => champion.FightId == fight.Id)
                .Where(champion => champion.Intent != ChampionIntent.Retreating
                    || Math.Abs(champion.LanePosition - fight.Position) <= settings.EngageRange)
                .ToList();
        }

        /// <summary>
        /// Creates fights from lane proximity.
        /// </summary>
        /// <param name="state">The round state.</param>
        public void CreateFights(RoundState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            foreach (Lane lane in Enum.GetValues<Lane>())
            {
                if (state.ActiveFights.Any(fight => fight.Lane == lane))
                {
                    continue;
                }

                List<(ChampionInstance Blue, ChampionInstance Red, double Distance)> pairs =
                    state.BlueTeam.Champions
                        .Where(champion => IsEligibleToStartFight(champion, lane))
                        .SelectMany(
                            blue => state.RedTeam.Champions
                                .Where(red => IsEligibleToStartFight(red, lane))
                                .Select(red => (Blue: blue, Red: red, Distance: Math.Abs(blue.LanePosition - red.LanePosition))))
                        .Where(pair => pair.Distance <= _settings.EngageRange)
                        .OrderBy(pair => pair.Distance)
                        .ToList();

                if (pairs.Count == 0)
                {
                    continue;
                }

                (ChampionInstance blue, ChampionInstance red, _) = pairs[0];
                double fightPosition = (blue.LanePosition + red.LanePosition) / 2.0;
                FightState fight = new()
                {
                    Lane = lane,
                    Position = fightPosition
                };

                List<ChampionInstance> initialParticipants = state.AllChampions
                    .Where(champion => IsEligibleToStartFight(champion, lane))
                    .Where(champion => Math.Abs(champion.LanePosition - fightPosition) <= _settings.EngageRange)
                    .ToList();

                if (!initialParticipants.Any(champion => champion.TeamSide == TeamSide.Blue)
                    || !initialParticipants.Any(champion => champion.TeamSide == TeamSide.Red))
                {
                    continue;
                }

                foreach (ChampionInstance participant in initialParticipants)
                {
                    AddParticipant(fight, participant);
                }

                state.ActiveFights.Add(fight);
                state.AddEvent(new RoundEvent
                {
                    TimeSeconds = state.CurrentTime,
                    Type = RoundEventType.FightStarted,
                    Lane = lane.ToString(),
                    FightId = fight.Id,
                    ChampionId = blue.Definition.Id,
                    SourceTeamSide = blue.TeamSide.ToString(),
                    SourceChampionId = blue.Definition.Id,
                    SourceChampionName = blue.Definition.Name,
                    SourcePlayerId = blue.PlayerId,
                    TargetChampionId = red.Definition.Id,
                    TargetTeamSide = red.TeamSide.ToString(),
                    TargetChampionName = red.Definition.Name,
                    TargetPlayerId = red.PlayerId,
                    Message = $"Fight started between {RoundEventFormatter.ChampionName(blue)} and {RoundEventFormatter.ChampionName(red)} in {lane}."
                });
            }
        }

        /// <summary>
        /// Joins eligible champions to active fights.
        /// </summary>
        /// <param name="state">The round state.</param>
        public void JoinFights(RoundState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            foreach (FightState fight in state.ActiveFights)
            {
                foreach (ChampionInstance champion in state.AllChampions)
                {
                    if (!IsEligibleToStartFight(champion, fight.Lane))
                    {
                        continue;
                    }

                    if (Math.Abs(champion.LanePosition - fight.Position) <= _settings.EngageRange)
                    {
                        if (AddParticipant(fight, champion))
                        {
                            state.AddEvent(new RoundEvent
                            {
                                TimeSeconds = state.CurrentTime,
                                Type = RoundEventType.FightJoined,
                                Lane = fight.Lane.ToString(),
                                FightId = fight.Id,
                                TeamSide = champion.TeamSide.ToString(),
                                ChampionId = champion.Definition.Id,
                                SourceTeamSide = champion.TeamSide.ToString(),
                                SourceChampionId = champion.Definition.Id,
                                SourceChampionName = champion.Definition.Name,
                                SourcePlayerId = champion.PlayerId,
                                Message = $"{RoundEventFormatter.ChampionName(champion)} joined the fight in {fight.Lane}."
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Ends fights where one side has no active participants.
        /// </summary>
        /// <param name="state">The round state.</param>
        public void EndInactiveFights(RoundState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            foreach (FightState fight in state.ActiveFights.ToList())
            {
                foreach (ChampionInstance participant in fight.Participants.ToList())
                {
                    if (!participant.IsAlive)
                    {
                        participant.FightId = null;
                        participant.CurrentFightPosition = null;
                        continue;
                    }

                    if (participant.FightId == fight.Id
                        && participant.Intent == ChampionIntent.Retreating
                        && Math.Abs(participant.LanePosition - fight.Position) > _settings.EngageRange)
                    {
                        participant.FightId = null;
                        participant.CurrentFightPosition = null;
                        state.AddEvent(new RoundEvent
                        {
                            TimeSeconds = state.CurrentTime,
                            Type = RoundEventType.ChampionEscaped,
                            Lane = fight.Lane.ToString(),
                            FightId = fight.Id,
                            TeamSide = participant.TeamSide.ToString(),
                            ChampionId = participant.Definition.Id,
                            SourceTeamSide = participant.TeamSide.ToString(),
                            SourceChampionId = participant.Definition.Id,
                            SourceChampionName = participant.Definition.Name,
                            SourcePlayerId = participant.PlayerId,
                            Message = $"{RoundEventFormatter.ChampionName(participant)} escaped the fight."
                        });
                    }
                }

                IReadOnlyList<ChampionInstance> activeParticipants = GetActiveParticipants(fight, _settings);
                // TODO: Make fight sides with no active damage-capable participants retreat to prevent sustain-only stalls.
                bool hasBlue = activeParticipants.Any(champion => champion.TeamSide == TeamSide.Blue);
                bool hasRed = activeParticipants.Any(champion => champion.TeamSide == TeamSide.Red);

                if (hasBlue && hasRed)
                {
                    continue;
                }

                if (hasBlue || hasRed)
                {
                    TeamSide winningSide = hasBlue ? TeamSide.Blue : TeamSide.Red;
                    foreach (ChampionInstance winner in activeParticipants.Where(champion => champion.TeamSide == winningSide))
                    {
                        _progressionService.AddExperience(winner, _settings.FightWinXp, state);
                    }
                }

                foreach (ChampionInstance participant in fight.Participants)
                {
                    if (participant.FightId == fight.Id)
                    {
                        participant.FightId = null;
                    }

                    participant.CurrentFightPosition = null;
                }

                state.ActiveFights.Remove(fight);
                state.AddEvent(new RoundEvent
                {
                    TimeSeconds = state.CurrentTime,
                    Type = RoundEventType.FightEnded,
                    Lane = fight.Lane.ToString(),
                    FightId = fight.Id,
                    TeamSide = hasBlue == hasRed ? null : (hasBlue ? TeamSide.Blue : TeamSide.Red).ToString(),
                    SourceTeamSide = hasBlue == hasRed ? null : (hasBlue ? TeamSide.Blue : TeamSide.Red).ToString(),
                    Message = hasBlue == hasRed
                        ? $"The fight in {fight.Lane} ended with no winner."
                        : $"{(hasBlue ? TeamSide.Blue : TeamSide.Red)} won the fight in {fight.Lane}."
                });
            }
        }

        /// <summary>
        /// Closes all active fights because the round ended.
        /// </summary>
        /// <param name="state">The round state.</param>
        public void CloseActiveFightsForRoundEnd(RoundState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            foreach (FightState fight in state.ActiveFights.ToList())
            {
                foreach (ChampionInstance participant in fight.Participants)
                {
                    if (participant.FightId == fight.Id)
                    {
                        participant.FightId = null;
                    }

                    participant.CurrentFightPosition = null;
                }

                state.ActiveFights.Remove(fight);
                state.AddEvent(new RoundEvent
                {
                    TimeSeconds = state.CurrentTime,
                    Type = RoundEventType.FightEnded,
                    Lane = fight.Lane.ToString(),
                    FightId = fight.Id,
                    Message = $"Fight in {fight.Lane} ended because the round ended."
                });
            }
        }

        private bool AddParticipant(FightState fight, ChampionInstance champion)
        {
            if (!fight.Participants.Contains(champion))
            {
                fight.Participants.Add(champion);
                champion.FightId = fight.Id;
                champion.CurrentFightPosition = fight.Position;
                champion.CurrentBacklineOffset = _settings.BacklineOffset;
                return true;
            }

            champion.FightId = fight.Id;
            champion.CurrentFightPosition = fight.Position;
            champion.CurrentBacklineOffset = _settings.BacklineOffset;
            return false;
        }

        private static bool IsEligibleToStartFight(ChampionInstance champion, Lane lane) =>
            champion.IsAlive
            && !champion.JustRespawned
            && champion.Lane == lane
            && champion.Intent == ChampionIntent.Laning
            && !champion.FightId.HasValue;
    }
}
