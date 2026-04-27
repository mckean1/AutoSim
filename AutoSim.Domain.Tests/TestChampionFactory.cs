using AutoSim.Domain.Enums;
using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Tests
{
    internal static class TestChampionFactory
    {
        public static ChampionInstance CreateInstance(
            string playerId = "player-one",
            FormationPosition defaultPosition = FormationPosition.Frontline,
            int health = 1000,
            int power = 100,
            double attackSpeed = 1.0,
            double abilityCooldown = 5.0,
            double abilityCastTime = 0.5,
            IReadOnlyList<CombatEffect>? attackEffects = null,
            IReadOnlyList<CombatEffect>? abilityCombatEffects = null)
        {
            ChampionDefinition definition = CreateDefinition(
                defaultPosition,
                health,
                power,
                attackSpeed,
                abilityCooldown,
                abilityCastTime,
                attackEffects,
                abilityCombatEffects);

            return ChampionInstanceFactory.Create(definition, playerId);
        }

        public static ChampionDefinition CreateDefinition(
            FormationPosition defaultPosition = FormationPosition.Frontline,
            int health = 1000,
            int power = 100,
            double attackSpeed = 1.0,
            double abilityCooldown = 5.0,
            double abilityCastTime = 0.5,
            IReadOnlyList<CombatEffect>? attackEffects = null,
            IReadOnlyList<CombatEffect>? abilityCombatEffects = null) =>
            new ChampionDefinition
            {
                Id = $"test-{Guid.NewGuid():N}",
                Name = "Test Champion",
                Role = ChampionRole.Fighter,
                DefaultPosition = defaultPosition,
                Health = health,
                Power = power,
                AttackSpeed = attackSpeed,
                Attack = new ChampionAttack
                {
                    Effects = attackEffects ?? []
                },
                Ability = new ChampionAbility
                {
                    Id = "test-ability",
                    Name = "Test Ability",
                    Cooldown = abilityCooldown,
                    CastTime = abilityCastTime,
                    Effects = abilityCombatEffects ?? []
                }
            };

        public static CombatEffect CreateEffect(
            CombatEffectType type,
            int value,
            TargetMode targetMode,
            TargetScope targetScope,
            double? duration = null) =>
            new CombatEffect
            {
                Type = type,
                Value = value,
                TargetMode = targetMode,
                TargetScope = targetScope,
                Duration = duration
            };
    }
}
