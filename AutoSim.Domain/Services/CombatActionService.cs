using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Handles champion combat actions inside active fights.
    /// </summary>
    public sealed class CombatActionService
    {
        private readonly DeathRespawnService _deathRespawnService;
        private readonly RoundSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatActionService"/> class.
        /// </summary>
        /// <param name="settings">The round settings.</param>
        /// <param name="deathRespawnService">The death and respawn service.</param>
        public CombatActionService(RoundSettings settings, DeathRespawnService deathRespawnService)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _deathRespawnService = deathRespawnService ?? throw new ArgumentNullException(nameof(deathRespawnService));
        }

        /// <summary>
        /// Advances a champion's active ability cast.
        /// </summary>
        /// <param name="champion">The casting champion.</param>
        /// <param name="deltaSeconds">The elapsed time in seconds.</param>
        /// <param name="state">The round state.</param>
        public void TickCasting(ChampionInstance champion, double deltaSeconds, RoundState state)
        {
            ArgumentNullException.ThrowIfNull(champion);
            ArgumentNullException.ThrowIfNull(state);

            if (!champion.IsAlive || !champion.IsCasting || champion.PendingAbility is null)
            {
                return;
            }

            champion.CastTimer = Math.Max(0, champion.CastTimer - deltaSeconds);

            if (champion.CastTimer > 0)
            {
                return;
            }

            FightState? fight = FightService.GetFight(state, champion.FightId);
            if (fight is not null && champion.Intent != ChampionIntent.Retreating)
            {
                IReadOnlyList<ChampionInstance> activeChampions = FightService.GetActiveParticipants(fight, _settings);
                ChampionCombatResolver.ResolveAbility(champion, state.AllChampions, activeChampions, state.Rng);
                _deathRespawnService.ProcessDeaths(state, champion);
                champion.AbilityCooldown = champion.Definition.Ability.Cooldown;
            }

            DeathRespawnService.CancelCast(champion);
        }

        /// <summary>
        /// Resolves ready attacks and starts ready useful ability casts.
        /// </summary>
        /// <param name="state">The round state.</param>
        public void ResolveCombatActions(RoundState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            foreach (FightState fight in state.ActiveFights.ToList())
            {
                IReadOnlyList<ChampionInstance> activeChampions = FightService.GetActiveParticipants(fight, _settings);
                foreach (ChampionInstance champion in activeChampions)
                {
                    champion.CurrentFightPosition = fight.Position;
                    champion.CurrentBacklineOffset = _settings.BacklineOffset;
                }

                foreach (ChampionInstance champion in activeChampions)
                {
                    if (champion.JustRespawned || !CanAct(champion))
                    {
                        continue;
                    }

                    if (champion.AbilityCooldown <= 0 && IsAbilityUseful(champion, state, activeChampions))
                    {
                        champion.IsCasting = true;
                        champion.CastTimer = champion.Definition.Ability.CastTime;
                        champion.PendingAbility = champion.Definition.Ability;
                        continue;
                    }

                    if (champion.AttackTimer <= 0)
                    {
                        ChampionCombatResolver.ResolveAttack(champion, state.AllChampions, activeChampions, state.Rng);
                        champion.AttackTimer = champion.Definition.AttackSpeed > 0
                            ? 1.0 / champion.Definition.AttackSpeed
                            : 0;
                        _deathRespawnService.ProcessDeaths(state, champion);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether a champion's ability has at least one useful target.
        /// </summary>
        /// <param name="champion">The champion evaluating the ability.</param>
        /// <param name="state">The round state.</param>
        /// <param name="activeChampions">The active fight participants.</param>
        /// <returns>True if the ability is useful; otherwise false.</returns>
        public static bool IsAbilityUseful(
            ChampionInstance champion,
            RoundState state,
            IEnumerable<ChampionInstance> activeChampions)
        {
            ArgumentNullException.ThrowIfNull(champion);
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(activeChampions);

            foreach (CombatEffect effect in champion.Definition.Ability.Effects)
            {
                IReadOnlyList<ChampionInstance> targets = CombatTargeting.SelectCandidatePool(
                    champion,
                    effect,
                    state.AllChampions,
                    activeChampions);

                if (effect.Type == CombatEffectType.Heal)
                {
                    if (targets.Any(target => target.CurrentHealth < target.MaximumHealth))
                    {
                        return true;
                    }

                    continue;
                }

                if (targets.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanAct(ChampionInstance champion) =>
            champion.IsAlive
            && champion.FightId.HasValue
            && champion.Intent != ChampionIntent.Retreating
            && !champion.IsCasting;
    }
}
