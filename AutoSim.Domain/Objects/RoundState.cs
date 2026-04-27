using AutoSim.Domain.Interfaces;

namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Represents mutable state for one simulated round.
    /// </summary>
    public sealed class RoundState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoundState"/> class.
        /// </summary>
        public RoundState(
            TeamRoundState blueTeam,
            TeamRoundState redTeam,
            IMatchRandom rng,
            RoundSettings settings)
        {
            BlueTeam = blueTeam ?? throw new ArgumentNullException(nameof(blueTeam));
            RedTeam = redTeam ?? throw new ArgumentNullException(nameof(redTeam));
            Rng = rng ?? throw new ArgumentNullException(nameof(rng));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Gets or sets the current round time.
        /// </summary>
        public double CurrentTime { get; set; }

        /// <summary>
        /// Gets the blue team state.
        /// </summary>
        public TeamRoundState BlueTeam { get; }

        /// <summary>
        /// Gets the red team state.
        /// </summary>
        public TeamRoundState RedTeam { get; }

        /// <summary>
        /// Gets the active fights.
        /// </summary>
        public IList<FightState> ActiveFights { get; } = [];

        /// <summary>
        /// Gets the seeded random source.
        /// </summary>
        public IMatchRandom Rng { get; }

        /// <summary>
        /// Gets the round settings.
        /// </summary>
        public RoundSettings Settings { get; }

        /// <summary>
        /// Gets the optional event log.
        /// </summary>
        public IList<string> EventLog { get; } = [];

        /// <summary>
        /// Gets all champions in the round.
        /// </summary>
        public IReadOnlyList<ChampionInstance> AllChampions =>
            BlueTeam.Champions.Concat(RedTeam.Champions).ToList();
    }
}
