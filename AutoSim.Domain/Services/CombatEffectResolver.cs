using AutoSim.Domain.Enums;
using AutoSim.Domain.Interfaces;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Resolves combat effects against selected targets.
    /// </summary>
    public static class CombatEffectResolver
    {
        private const double DefaultShieldDuration = 5.0;

        /// <summary>
        /// Resolves an attack effect from a source champion.
        /// </summary>
        /// <param name="source">The champion applying the effect.</param>
        /// <param name="effect">The attack effect to resolve.</param>
        /// <param name="allChampions">All champions in the match.</param>
        /// <param name="activeChampions">Champions currently participating in the active fight.</param>
        /// <param name="rng">The seeded match random source.</param>
        /// <param name="state">The optional round state used for event logging.</param>
        public static void ResolveAttackEffect(
            ChampionInstance source,
            AttackEffect effect,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng,
            RoundState? state = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(effect);
            ArgumentNullException.ThrowIfNull(allChampions);
            ArgumentNullException.ThrowIfNull(activeChampions);
            ArgumentNullException.ThrowIfNull(rng);

            IReadOnlyList<ChampionInstance> targets = CombatTargeting.SelectTargets(
                source,
                effect,
                allChampions,
                activeChampions,
                rng);

            foreach (ChampionInstance target in targets)
            {
                ApplyEffect(effect.Type, source.CurrentAttackPower, effect.Duration, source, target, state);
            }
        }

        /// <summary>
        /// Resolves an ability effect from a source champion.
        /// </summary>
        /// <param name="source">The champion applying the effect.</param>
        /// <param name="effect">The ability effect to resolve.</param>
        /// <param name="allChampions">All champions in the match.</param>
        /// <param name="activeChampions">Champions currently participating in the active fight.</param>
        /// <param name="rng">The seeded match random source.</param>
        /// <param name="state">The optional round state used for event logging.</param>
        public static void ResolveAbilityEffect(
            ChampionInstance source,
            AbilityEffect effect,
            IEnumerable<ChampionInstance> allChampions,
            IEnumerable<ChampionInstance> activeChampions,
            IMatchRandom rng,
            RoundState? state = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(effect);
            ArgumentNullException.ThrowIfNull(allChampions);
            ArgumentNullException.ThrowIfNull(activeChampions);
            ArgumentNullException.ThrowIfNull(rng);

            IReadOnlyList<ChampionInstance> targets = CombatTargeting.SelectTargets(
                source,
                effect,
                allChampions,
                activeChampions,
                rng);

            foreach (ChampionInstance target in targets)
            {
                ApplyEffect(effect.Type, effect.AbilityPower, effect.Duration, source, target, state);
            }
        }

        private static void ApplyEffect(
            CombatEffectType type,
            int amount,
            double? duration,
            ChampionInstance source,
            ChampionInstance target,
            RoundState? state)
        {
            switch (type)
            {
                case CombatEffectType.Damage:
                    int damageDone = CombatEffectApplicator.ApplyDamage(target, amount);
                    if (damageDone > 0 && state is not null)
                    {
                        state.AddEvent(new RoundEvent
                        {
                            TimeSeconds = state.CurrentTime,
                            Type = RoundEventType.DamageDealt,
                            Lane = source.Lane.ToString(),
                            FightId = source.FightId,
                            TeamSide = source.TeamSide.ToString(),
                            ChampionId = source.Definition.Id,
                            SourceTeamSide = source.TeamSide.ToString(),
                            SourceChampionId = source.Definition.Id,
                            SourceChampionName = source.Definition.Name,
                            SourcePlayerId = source.PlayerId,
                            TargetChampionId = target.Definition.Id,
                            TargetTeamSide = target.TeamSide.ToString(),
                            TargetChampionName = target.Definition.Name,
                            TargetPlayerId = target.PlayerId,
                            Message = $"{RoundEventFormatter.ChampionName(source)} dealt {damageDone} damage to {RoundEventFormatter.ChampionName(target)}."
                        });
                    }

                    break;
                case CombatEffectType.Heal:
                    int healingDone = CombatEffectApplicator.ApplyHeal(target, amount);
                    if (healingDone > 0 && state is not null)
                    {
                        state.AddEvent(new RoundEvent
                        {
                            TimeSeconds = state.CurrentTime,
                            Type = RoundEventType.HealingDone,
                            Lane = source.Lane.ToString(),
                            FightId = source.FightId,
                            TeamSide = source.TeamSide.ToString(),
                            ChampionId = source.Definition.Id,
                            SourceTeamSide = source.TeamSide.ToString(),
                            SourceChampionId = source.Definition.Id,
                            SourceChampionName = source.Definition.Name,
                            SourcePlayerId = source.PlayerId,
                            TargetChampionId = target.Definition.Id,
                            TargetTeamSide = target.TeamSide.ToString(),
                            TargetChampionName = target.Definition.Name,
                            TargetPlayerId = target.PlayerId,
                            Message = $"{RoundEventFormatter.ChampionName(source)} healed {RoundEventFormatter.ChampionName(target)} for {healingDone} health."
                        });
                    }

                    break;
                case CombatEffectType.Shield:
                    int shieldApplied = CombatEffectApplicator.ApplyShield(target, amount, duration ?? DefaultShieldDuration);
                    if (shieldApplied > 0 && state is not null)
                    {
                        state.AddEvent(new RoundEvent
                        {
                            TimeSeconds = state.CurrentTime,
                            Type = RoundEventType.ShieldApplied,
                            Lane = source.Lane.ToString(),
                            FightId = source.FightId,
                            TeamSide = source.TeamSide.ToString(),
                            ChampionId = source.Definition.Id,
                            SourceTeamSide = source.TeamSide.ToString(),
                            SourceChampionId = source.Definition.Id,
                            SourceChampionName = source.Definition.Name,
                            SourcePlayerId = source.PlayerId,
                            TargetChampionId = target.Definition.Id,
                            TargetTeamSide = target.TeamSide.ToString(),
                            TargetChampionName = target.Definition.Name,
                            TargetPlayerId = target.PlayerId,
                            Message = $"{RoundEventFormatter.ChampionName(source)} shielded {RoundEventFormatter.ChampionName(target)} for {shieldApplied}."
                        });
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type),
                        type,
                        "Unsupported combat effect type.");
            }
        }
    }
}
