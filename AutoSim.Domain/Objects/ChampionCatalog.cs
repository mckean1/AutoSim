using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Provides the initial champion seed data.
    /// </summary>
    public static class ChampionCatalog
    {
        private const double StandardShieldDuration = 5.0;

        /// <summary>
        /// Gets the default champion definitions.
        /// </summary>
        /// <returns>The initial champion definitions.</returns>
        public static IReadOnlyList<ChampionDefinition> GetDefaultChampions() =>
        [
            CreateChampion(
                "iron-vanguard",
                "Iron Vanguard",
                "A durable frontline anchor who absorbs pressure with self-shields while wearing down the enemy frontline.",
                ChampionRole.Fighter,
                FormationPosition.Frontline,
                150,
                15,
                0.80,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "bracebreaker",
                "Bracebreaker",
                8.0,
                0.75,
                [
                    CreateAbilityEffect(CombatEffectType.Damage, 35, TargetMode.EnemyFrontline, TargetScope.One),
                    CreateAbilityEffect(CombatEffectType.Shield, 30, TargetMode.Self, TargetScope.One, StandardShieldDuration)
                ]),
            CreateChampion(
                "bloodguard",
                "Bloodguard",
                "A bruising duelist who stays alive through self-healing while trading damage on the frontline.",
                ChampionRole.Fighter,
                FormationPosition.Frontline,
                125,
                20,
                1.05,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "crimson-swing",
                "Crimson Swing",
                7.0,
                0.50,
                [
                    CreateAbilityEffect(CombatEffectType.Damage, 30, TargetMode.EnemyFrontline, TargetScope.One),
                    CreateAbilityEffect(CombatEffectType.Heal, 30, TargetMode.Self, TargetScope.One)
                ]),
            CreateChampion(
                "chain-mauler",
                "Chain Mauler",
                "A disruptive fighter who threatens enemy backliners with heavy targeted damage.",
                ChampionRole.Fighter,
                FormationPosition.Frontline,
                130,
                20,
                0.95,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "chain-drag",
                "Chain Drag",
                8.0,
                0.75,
                [CreateAbilityEffect(CombatEffectType.Damage, 45, TargetMode.EnemyBackline, TargetScope.One)]),
            CreateChampion(
                "stonejaw",
                "Stonejaw",
                "A protective frontline bruiser who shields nearby allies while holding the enemy frontline in place.",
                ChampionRole.Fighter,
                FormationPosition.Frontline,
                145,
                15,
                0.85,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "hold-the-line",
                "Hold the Line",
                9.0,
                0.75,
                [
                    CreateAbilityEffect(
                        CombatEffectType.Shield,
                        30,
                        TargetMode.AllyFrontline,
                        TargetScope.All,
                        StandardShieldDuration),
                    CreateAbilityEffect(CombatEffectType.Damage, 20, TargetMode.EnemyFrontline, TargetScope.One)
                ]),
            CreateChampion(
                "rift-breaker",
                "Rift Breaker",
                "An aggressive fighter who pressures entire fights with area damage instead of single-target control.",
                ChampionRole.Fighter,
                FormationPosition.Frontline,
                120,
                25,
                0.90,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "ground-splitter",
                "Ground Splitter",
                9.0,
                1.00,
                [CreateAbilityEffect(CombatEffectType.Damage, 30, TargetMode.EnemyAny, TargetScope.All)]),
            CreateChampion(
                "quickshot",
                "Quickshot",
                "A fast sustained attacker who overwhelms frontline targets with frequent basic attacks.",
                ChampionRole.Marksman,
                FormationPosition.Backline,
                95,
                20,
                1.60,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "rapid-fire",
                "Rapid Fire",
                4.0,
                0.25,
                [CreateAbilityEffect(CombatEffectType.Damage, 25, TargetMode.EnemyFrontline, TargetScope.One)]),
            CreateChampion(
                "longshot",
                "Longshot",
                "A backline sniper who specializes in picking off fragile enemies behind the frontline.",
                ChampionRole.Marksman,
                FormationPosition.Backline,
                90,
                25,
                1.10,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyBackline, TargetScope.One)],
                "piercing-shot",
                "Piercing Shot",
                8.0,
                1.00,
                [CreateAbilityEffect(CombatEffectType.Damage, 55, TargetMode.EnemyBackline, TargetScope.One)]),
            CreateChampion(
                "volley-hawk",
                "Volley Hawk",
                "An area-focused marksman who spreads damage across all enemies in the active fight.",
                ChampionRole.Marksman,
                FormationPosition.Backline,
                100,
                20,
                1.20,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "arrow-storm",
                "Arrow Storm",
                8.0,
                0.75,
                [CreateAbilityEffect(CombatEffectType.Damage, 25, TargetMode.EnemyAny, TargetScope.All)]),
            CreateChampion(
                "glass-arrow",
                "Glass Arrow",
                "A fragile carry with high attack power and a devastating single-target finishing shot.",
                ChampionRole.Marksman,
                FormationPosition.Backline,
                90,
                30,
                1.00,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "perfect-shot",
                "Perfect Shot",
                10.0,
                1.00,
                [CreateAbilityEffect(CombatEffectType.Damage, 65, TargetMode.EnemyFrontline, TargetScope.One)]),
            CreateChampion(
                "crosswind",
                "Crosswind",
                "A self-protecting marksman who mixes steady damage with personal shielding to survive longer fights.",
                ChampionRole.Marksman,
                FormationPosition.Backline,
                105,
                20,
                1.30,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "evasive-volley",
                "Evasive Volley",
                7.0,
                0.50,
                [
                    CreateAbilityEffect(CombatEffectType.Damage, 30, TargetMode.EnemyFrontline, TargetScope.One),
                    CreateAbilityEffect(CombatEffectType.Shield, 25, TargetMode.Self, TargetScope.One, StandardShieldDuration)
                ]),
            CreateChampion(
                "ember-sage",
                "Ember Sage",
                "A burst mage who focuses on destroying one frontline target with powerful spell damage.",
                ChampionRole.Mage,
                FormationPosition.Backline,
                90,
                15,
                0.90,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "fire-lance",
                "Fire Lance",
                9.0,
                1.00,
                [CreateAbilityEffect(CombatEffectType.Damage, 45, TargetMode.EnemyFrontline, TargetScope.One)]),
            CreateChampion(
                "stormcaller",
                "Stormcaller",
                "A slow-casting mage who punishes grouped enemies with heavy fight-wide damage.",
                ChampionRole.Mage,
                FormationPosition.Backline,
                95,
                15,
                0.80,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "chain-storm",
                "Chain Storm",
                9.0,
                1.50,
                [CreateAbilityEffect(CombatEffectType.Damage, 40, TargetMode.EnemyAny, TargetScope.All)]),
            CreateChampion(
                "frost-oracle",
                "Frost Oracle",
                "A backline-control mage who damages enemy backliners while shielding allied backliners.",
                ChampionRole.Mage,
                FormationPosition.Backline,
                95,
                15,
                0.90,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "cold-star",
                "Cold Star",
                9.0,
                1.00,
                [
                    CreateAbilityEffect(CombatEffectType.Damage, 35, TargetMode.EnemyBackline, TargetScope.All),
                    CreateAbilityEffect(
                        CombatEffectType.Shield,
                        15,
                        TargetMode.AllyBackline,
                        TargetScope.All,
                        StandardShieldDuration)
                ]),
            CreateChampion(
                "starbinder",
                "Starbinder",
                "A global pressure mage who damages enemies across the map, even outside the current fight.",
                ChampionRole.Mage,
                FormationPosition.Backline,
                90,
                15,
                0.85,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "falling-stars",
                "Falling Stars",
                12.0,
                1.50,
                [CreateAbilityEffect(CombatEffectType.Damage, 20, TargetMode.GlobalEnemy, TargetScope.All)]),
            CreateChampion(
                "rune-weaver",
                "Rune Weaver",
                "A hybrid mage who damages the enemy team while shielding allies in the same fight.",
                ChampionRole.Mage,
                FormationPosition.Backline,
                110,
                10,
                1.00,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "runic-pulse",
                "Runic Pulse",
                10.0,
                1.25,
                [
                    CreateAbilityEffect(CombatEffectType.Damage, 25, TargetMode.EnemyAny, TargetScope.All),
                    CreateAbilityEffect(CombatEffectType.Shield, 30, TargetMode.AllyAny, TargetScope.All, StandardShieldDuration)
                ]),
            CreateChampion(
                "lifewarden",
                "Lifewarden",
                "A focused healer who keeps a single ally alive through repeated targeted healing.",
                ChampionRole.Support,
                FormationPosition.Backline,
                105,
                10,
                1.00,
                [CreateAttackEffect(CombatEffectType.Heal, TargetMode.AllyFrontline, TargetScope.One)],
                "mending-light",
                "Mending Light",
                6.0,
                0.75,
                [CreateAbilityEffect(CombatEffectType.Heal, 45, TargetMode.AllyAny, TargetScope.One)]),
            CreateChampion(
                "dawn-keeper",
                "Dawn Keeper",
                "A pure team healer who restores all allies in the active fight during extended engagements.",
                ChampionRole.Support,
                FormationPosition.Backline,
                100,
                5,
                1.00,
                [CreateAttackEffect(CombatEffectType.Heal, TargetMode.AllyAny, TargetScope.One)],
                "morning-light",
                "Morning Light",
                9.0,
                1.00,
                [CreateAbilityEffect(CombatEffectType.Heal, 30, TargetMode.AllyAny, TargetScope.All)]),
            CreateChampion(
                "bulwark-medic",
                "Bulwark Medic",
                "A shield support who protects the active fight team with broad temporary barriers.",
                ChampionRole.Support,
                FormationPosition.Backline,
                115,
                5,
                0.90,
                [
                    CreateAttackEffect(
                        CombatEffectType.Shield,
                        TargetMode.AllyFrontline,
                        TargetScope.One,
                        StandardShieldDuration)
                ],
                "barrier-field",
                "Barrier Field",
                10.0,
                0.75,
                [
                    CreateAbilityEffect(
                        CombatEffectType.Shield,
                        35,
                        TargetMode.AllyAny,
                        TargetScope.All,
                        StandardShieldDuration)
                ]),
            CreateChampion(
                "pact-seer",
                "Pact Seer",
                "A hybrid support who damages enemies while healing allies to swing small fights.",
                ChampionRole.Support,
                FormationPosition.Backline,
                95,
                15,
                1.00,
                [CreateAttackEffect(CombatEffectType.Damage, TargetMode.EnemyFrontline, TargetScope.One)],
                "life-pact",
                "Life Pact",
                7.0,
                0.75,
                [
                    CreateAbilityEffect(CombatEffectType.Damage, 35, TargetMode.EnemyFrontline, TargetScope.One),
                    CreateAbilityEffect(CombatEffectType.Heal, 35, TargetMode.AllyAny, TargetScope.One)
                ]),
            CreateChampion(
                "field-cleric",
                "Field Cleric",
                "A global sustain support who can heal and shield allies anywhere on the map.",
                ChampionRole.Support,
                FormationPosition.Backline,
                90,
                5,
                1.10,
                [CreateAttackEffect(CombatEffectType.Heal, TargetMode.AllyAny, TargetScope.One)],
                "field-prayer",
                "Field Prayer",
                12.0,
                1.50,
                [
                    CreateAbilityEffect(CombatEffectType.Heal, 25, TargetMode.GlobalAlly, TargetScope.All),
                    CreateAbilityEffect(
                        CombatEffectType.Shield,
                        20,
                        TargetMode.GlobalAlly,
                        TargetScope.All,
                        StandardShieldDuration)
                ])
        ];

        private static ChampionDefinition CreateChampion(
            string id,
            string name,
            string description,
            ChampionRole role,
            FormationPosition defaultPosition,
            int health,
            int attackPower,
            double attackSpeed,
            IReadOnlyList<AttackEffect> attackEffects,
            string abilityId,
            string abilityName,
            double abilityCooldown,
            double abilityCastTime,
            IReadOnlyList<AbilityEffect> abilityEffects) =>
            new ChampionDefinition
            {
                Id = id,
                Name = name,
                Description = description,
                Role = role,
                DefaultPosition = defaultPosition,
                Health = health,
                AttackPower = attackPower,
                AttackSpeed = attackSpeed,
                Attack = new ChampionAttack
                {
                    Effects = attackEffects
                },
                Ability = new ChampionAbility
                {
                    Id = abilityId,
                    Name = abilityName,
                    Cooldown = abilityCooldown,
                    CastTime = abilityCastTime,
                    Effects = abilityEffects
                }
            };

        private static AttackEffect CreateAttackEffect(
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

        private static AbilityEffect CreateAbilityEffect(
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
