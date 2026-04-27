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
            int attackPower = 0,
            double attackSpeed = 1.0,
            double abilityCooldown = 5.0,
            double abilityCastTime = 0.5,
            IReadOnlyList<AttackEffect>? attackEffects = null,
            IReadOnlyList<AbilityEffect>? abilityEffects = null)
        {
            ChampionDefinition definition = CreateDefinition(
                defaultPosition,
                health,
                attackPower,
                attackSpeed,
                abilityCooldown,
                abilityCastTime,
                attackEffects,
                abilityEffects);

            ChampionInstance champion = ChampionInstanceFactory.Create(definition, playerId);
            champion.TeamSide = string.Equals(playerId, "player-two", StringComparison.Ordinal)
                || string.Equals(playerId, "red", StringComparison.Ordinal)
                    ? TeamSide.Red
                    : TeamSide.Blue;

            return champion;
        }

        public static ChampionDefinition CreateDefinition(
            FormationPosition defaultPosition = FormationPosition.Frontline,
            int health = 1000,
            int attackPower = 100,
            double attackSpeed = 1.0,
            double abilityCooldown = 5.0,
            double abilityCastTime = 0.5,
            IReadOnlyList<AttackEffect>? attackEffects = null,
            IReadOnlyList<AbilityEffect>? abilityEffects = null) =>
            new ChampionDefinition
            {
                Id = $"test-{Guid.NewGuid():N}",
                Name = "Test Champion",
                Description = "A test champion used by unit tests.",
                Role = ChampionRole.Fighter,
                DefaultPosition = defaultPosition,
                Health = health,
                AttackPower = attackPower,
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
                    Effects = abilityEffects ?? []
                }
            };

        public static AttackEffect CreateAttackEffect(
            CombatEffectType type,
            TargetMode targetMode,
            TargetScope targetScope,
            double? duration = null) =>
            new AttackEffect
            {
                Type = type,
                TargetMode = targetMode,
                TargetScope = targetScope,
                Duration = duration
            };

        public static AbilityEffect CreateAbilityEffect(
            CombatEffectType type,
            int abilityPower,
            TargetMode targetMode,
            TargetScope targetScope,
            double? duration = null) =>
            new AbilityEffect
            {
                Type = type,
                AbilityPower = abilityPower,
                TargetMode = targetMode,
                TargetScope = targetScope,
                Duration = duration
            };
    }
}
