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
            IReadOnlyList<CombatEffect>? attackEffects = null,
            IReadOnlyList<CombatEffect>? abilityCombatEffects = null)
        {
            ChampionDefinition definition = CreateDefinition(
                defaultPosition,
                health,
                attackEffects,
                abilityCombatEffects);

            return ChampionInstanceFactory.Create(definition, playerId);
        }

        public static ChampionDefinition CreateDefinition(
            FormationPosition defaultPosition = FormationPosition.Frontline,
            int health = 1000,
            IReadOnlyList<CombatEffect>? attackEffects = null,
            IReadOnlyList<CombatEffect>? abilityCombatEffects = null) =>
            new ChampionDefinition
            {
                Id = $"test-{Guid.NewGuid():N}",
                Name = "Test Champion",
                Role = ChampionRole.Fighter,
                DefaultPosition = defaultPosition,
                Health = health,
                Power = 100,
                AttackSpeed = 1.0,
                Attack = new ChampionAttack
                {
                    Effects = attackEffects ?? []
                },
                Ability = new ChampionAbility
                {
                    Id = "test-ability",
                    Name = "Test Ability",
                    Cooldown = 5.0,
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
