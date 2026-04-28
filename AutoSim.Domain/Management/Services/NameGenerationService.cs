namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Generates unique coach, player, and team names for generated worlds.
    /// </summary>
    public sealed class NameGenerationService
    {
        private static readonly string[] FirstNames =
        [
            "Alden",
            "Aria",
            "Briar",
            "Caden",
            "Calla",
            "Darian",
            "Elian",
            "Ember",
            "Galen",
            "Hana",
            "Iris",
            "Jalen",
            "Kara",
            "Lena",
            "Maren",
            "Niko",
            "Orin",
            "Petra",
            "Quinn",
            "Rhea",
            "Silas",
            "Talia",
            "Vera",
            "Wren",
            "Zane"
        ];

        private static readonly string[] LastNames =
        [
            "Ashford",
            "Bennett",
            "Cross",
            "Dawes",
            "Ellis",
            "Frost",
            "Graves",
            "Hale",
            "Ivers",
            "Jensen",
            "Kade",
            "Lowell",
            "Morrow",
            "Nash",
            "Osborne",
            "Pryce",
            "Quill",
            "Reed",
            "Sato",
            "Vale",
            "Wells",
            "Yates",
            "Zeller",
            "Stone",
            "Rivers",
            "Black",
            "Hart",
            "Fox",
            "Mason",
            "Ward",
            "Knight",
            "Shaw",
            "Blake",
            "Rowe",
            "Page",
            "West",
            "Cole",
            "North",
            "Chase",
            "Lane",
            "Gray",
            "Fields",
            "Hayes",
            "Bishop",
            "Pierce",
            "Rhodes",
            "Vaughn",
            "Flynn",
            "Dean",
            "Brooks",
            "Holt",
            "Reyes",
            "Park",
            "Young",
            "Kim",
            "Singh",
            "Patel",
            "Morgan",
            "Bell",
            "King"
        ];

        private static readonly string[] TeamPrefixes =
        [
            "Aegis",
            "Anchor",
            "Arc",
            "Astral",
            "Beacon",
            "Binary",
            "Cascade",
            "Circuit",
            "Comet",
            "Crown",
            "Drift",
            "Echo",
            "Ember",
            "Forge",
            "Harbor",
            "Helix",
            "Ion",
            "Keystone",
            "Lattice",
            "Lunar",
            "Meridian",
            "Neon",
            "Nova",
            "Obsidian",
            "Onyx",
            "Pulse",
            "Quartz",
            "Radiant",
            "Relay",
            "Rift",
            "Solar",
            "Summit",
            "Tempest",
            "Titan",
            "Vector",
            "Vertex",
            "Vivid",
            "Zenith",
            "Zero",
            "Zephyr"
        ];

        private static readonly string[] TeamSuffixes =
        [
            "Aces",
            "Arrows",
            "Blazers",
            "Bolts",
            "Breakers",
            "Catalysts",
            "Chargers",
            "Comets",
            "Defenders",
            "Dynasty",
            "Eclipse",
            "Engine",
            "Falcons",
            "Force",
            "Guard",
            "Helix",
            "Horizon",
            "Knights",
            "Legion",
            "Meteors",
            "Monarchs",
            "Nomads",
            "Outriders",
            "Phantoms",
            "Pilots",
            "Rangers",
            "Ravens",
            "Reign",
            "Sentinels",
            "Shields",
            "Sparks",
            "Storm",
            "Strikers",
            "Titans",
            "Valkyries",
            "Vanguard",
            "Velocity",
            "Voyagers",
            "Wardens",
            "Wolves"
        ];

        private readonly Random _rng;
        private readonly HashSet<string> _personNames;
        private readonly HashSet<string> _teamNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="NameGenerationService"/> class.
        /// </summary>
        /// <param name="seed">The deterministic seed.</param>
        public NameGenerationService(int seed)
        {
            _rng = new Random(seed);
            _personNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _teamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Generates a unique coach name.
        /// </summary>
        /// <returns>The generated coach name.</returns>
        public string GenerateCoachName() => GeneratePersonName();

        /// <summary>
        /// Generates a unique player name.
        /// </summary>
        /// <returns>The generated player name.</returns>
        public string GeneratePlayerName() => GeneratePersonName();

        /// <summary>
        /// Generates a unique team name.
        /// </summary>
        /// <returns>The generated team name.</returns>
        public string GenerateTeamName() => GenerateUniqueName(TeamPrefixes, TeamSuffixes, _teamNames);

        /// <summary>
        /// Reserves a person name so generated coach and player names cannot duplicate it.
        /// </summary>
        /// <param name="name">The person name.</param>
        public void ReservePersonName(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            _personNames.Add(name.Trim());
        }

        /// <summary>
        /// Reserves a team name so generated team names cannot duplicate it.
        /// </summary>
        /// <param name="name">The team name.</param>
        public void ReserveTeamName(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            _teamNames.Add(name.Trim());
        }

        private string GeneratePersonName() => GenerateUniqueName(FirstNames, LastNames, _personNames);

        private string GenerateUniqueName(string[] prefixes, string[] suffixes, HashSet<string> usedNames)
        {
            int maximumNameCount = prefixes.Length * suffixes.Length;
            if (usedNames.Count >= maximumNameCount)
            {
                throw new InvalidOperationException("The generated name pool has been exhausted.");
            }

            while (true)
            {
                string name = $"{prefixes[_rng.Next(prefixes.Length)]} {suffixes[_rng.Next(suffixes.Length)]}";
                if (usedNames.Add(name))
                {
                    return name;
                }
            }
        }
    }
}
