using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Provides the MVP champion seed data.
    /// </summary>
    public static class ChampionCatalog
    {
        private const double StandardShieldDuration = 5.0;

        /// <summary>
        /// Gets the default champion definitions.
        /// </summary>
        /// <returns>The MVP champion definitions.</returns>
        public static IReadOnlyList<ChampionDefinition> GetDefaultChampions() =>
        [
            new ChampionDefinition
            {
                Id = "iron-vanguard",
                Name = "Iron Vanguard",
                Role = ChampionRole.Fighter,
                DefaultPosition = FormationPosition.Frontline,
                Health = 1200,
                AttackPower = 80,
                AttackSpeed = 0.8,
                Attack = new ChampionAttack
                {
                    Effects =
                    [
                        new AttackEffect
                        {
                            Type = CombatEffectType.Damage,
                            TargetMode = TargetMode.EnemyFrontline,
                            TargetScope = TargetScope.One
                        }
                    ]
                },
                Ability = new ChampionAbility
                {
                    Id = "shield-breaker",
                    Name = "Shield Breaker",
                    Cooldown = 8.0,
                    CastTime = 0.75,
                    Effects =
                    [
                        new AbilityEffect
                        {
                            Type = CombatEffectType.Damage,
                            AbilityPower = 140,
                            TargetMode = TargetMode.EnemyFrontline,
                            TargetScope = TargetScope.One
                        },
                        new AbilityEffect
                        {
                            Type = CombatEffectType.Shield,
                            AbilityPower = 180,
                            TargetMode = TargetMode.Self,
                            TargetScope = TargetScope.One,
                            Duration = StandardShieldDuration
                        }
                    ]
                }
            },
            new ChampionDefinition
            {
                Id = "quickshot",
                Name = "Quickshot",
                Role = ChampionRole.Marksman,
                DefaultPosition = FormationPosition.Backline,
                Health = 760,
                AttackPower = 120,
                AttackSpeed = 1.25,
                Attack = new ChampionAttack
                {
                    Effects =
                    [
                        new AttackEffect
                        {
                            Type = CombatEffectType.Damage,
                            TargetMode = TargetMode.EnemyFrontline,
                            TargetScope = TargetScope.One
                        }
                    ]
                },
                Ability = new ChampionAbility
                {
                    Id = "piercing-shot",
                    Name = "Piercing Shot",
                    Cooldown = 6.0,
                    CastTime = 0.5,
                    Effects =
                    [
                        new AbilityEffect
                        {
                            Type = CombatEffectType.Damage,
                            AbilityPower = 180,
                            TargetMode = TargetMode.EnemyBackline,
                            TargetScope = TargetScope.One
                        }
                    ]
                }
            },
            new ChampionDefinition
            {
                Id = "ember-sage",
                Name = "Ember Sage",
                Role = ChampionRole.Mage,
                DefaultPosition = FormationPosition.Backline,
                Health = 700,
                AttackPower = 150,
                AttackSpeed = 0.7,
                Attack = new ChampionAttack
                {
                    Effects =
                    [
                        new AttackEffect
                        {
                            Type = CombatEffectType.Damage,
                            TargetMode = TargetMode.EnemyFrontline,
                            TargetScope = TargetScope.One
                        }
                    ]
                },
                Ability = new ChampionAbility
                {
                    Id = "meteor-rain",
                    Name = "Meteor Rain",
                    Cooldown = 10.0,
                    CastTime = 1.25,
                    Effects =
                    [
                        new AbilityEffect
                        {
                            Type = CombatEffectType.Damage,
                            AbilityPower = 110,
                            TargetMode = TargetMode.EnemyAny,
                            TargetScope = TargetScope.All
                        }
                    ]
                }
            },
            new ChampionDefinition
            {
                Id = "dawn-keeper",
                Name = "Dawn Keeper",
                Role = ChampionRole.Support,
                DefaultPosition = FormationPosition.Backline,
                Health = 820,
                AttackPower = 60,
                AttackSpeed = 0.85,
                Attack = new ChampionAttack
                {
                    Effects =
                    [
                        new AttackEffect
                        {
                            Type = CombatEffectType.Damage,
                            TargetMode = TargetMode.EnemyFrontline,
                            TargetScope = TargetScope.One
                        }
                    ]
                },
                Ability = new ChampionAbility
                {
                    Id = "guarding-light",
                    Name = "Guarding Light",
                    Cooldown = 7.0,
                    CastTime = 0.75,
                    Effects =
                    [
                        new AbilityEffect
                        {
                            Type = CombatEffectType.Heal,
                            AbilityPower = 150,
                            TargetMode = TargetMode.AllyAny,
                            TargetScope = TargetScope.One
                        },
                        new AbilityEffect
                        {
                            Type = CombatEffectType.Shield,
                            AbilityPower = 120,
                            TargetMode = TargetMode.AllyFrontline,
                            TargetScope = TargetScope.One,
                            Duration = StandardShieldDuration
                        }
                    ]
                }
            }
        ];
    }
}
