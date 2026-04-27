using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Evaluates whether active fight sides can damage each other.
    /// </summary>
    public sealed class FightDamageCapabilityService
    {
        /// <summary>
        /// Determines whether a side has at least one active participant that can damage an opposing participant.
        /// </summary>
        /// <param name="fight">The fight being evaluated.</param>
        /// <param name="side">The team side being evaluated.</param>
        /// <param name="state">The round state.</param>
        /// <param name="settings">The round settings.</param>
        /// <returns>
        /// <c>true</c> when the side has a living, non-retreating active participant with a damage effect that can
        /// target at least one living opposing active participant; otherwise, <c>false</c>.
        /// </returns>
        public bool CanSideDamageEnemy(FightState fight, TeamSide side, RoundState state, RoundSettings settings)
        {
            ArgumentNullException.ThrowIfNull(fight);
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(settings);

            IReadOnlyList<ChampionInstance> activeParticipants = FightService.GetActiveParticipants(fight, settings);
            return HasDamageCapableParticipant(side, activeParticipants);
        }

        private static bool HasDamageCapableParticipant(
            TeamSide side,
            IReadOnlyList<ChampionInstance> activeParticipants)
        {
            IReadOnlyList<ChampionInstance> activeFightParticipants = activeParticipants
                .Where(champion => champion.FightId.HasValue)
                .ToList();

            return activeFightParticipants
                .Where(champion => champion.TeamSide == side)
                .Where(champion => champion.Intent != ChampionIntent.Retreating)
                .Any(champion => CanDamageEnemy(champion, activeFightParticipants));
        }

        private static bool CanDamageEnemy(
            ChampionInstance source,
            IReadOnlyList<ChampionInstance> activeFightParticipants) =>
            source.Definition.Attack.Effects.Any(effect => CanDamageEnemy(source, effect, activeFightParticipants))
            || source.Definition.Ability.Effects.Any(effect => CanDamageEnemy(source, effect, activeFightParticipants));

        private static bool CanDamageEnemy(
            ChampionInstance source,
            AttackEffect effect,
            IReadOnlyList<ChampionInstance> activeFightParticipants) =>
            effect.Type == CombatEffectType.Damage
            && CombatTargeting.SelectCandidatePool(source, effect, activeFightParticipants, activeFightParticipants)
                .Any(target => target.TeamSide != source.TeamSide);

        private static bool CanDamageEnemy(
            ChampionInstance source,
            AbilityEffect effect,
            IReadOnlyList<ChampionInstance> activeFightParticipants) =>
            effect.Type == CombatEffectType.Damage
            && CombatTargeting.SelectCandidatePool(source, effect, activeFightParticipants, activeFightParticipants)
                .Any(target => target.TeamSide != source.TeamSide);
    }
}
