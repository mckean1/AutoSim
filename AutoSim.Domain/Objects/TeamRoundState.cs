using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Represents mutable team state for one round.
    /// </summary>
    public sealed class TeamRoundState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TeamRoundState"/> class.
        /// </summary>
        public TeamRoundState(TeamSide side, IEnumerable<ChampionInstance> champions)
        {
            Side = side;
            Champions = (champions ?? throw new ArgumentNullException(nameof(champions))).ToList();
        }

        /// <summary>
        /// Gets the team side.
        /// </summary>
        public TeamSide Side { get; }

        /// <summary>
        /// Gets the team's champion instances.
        /// </summary>
        public IList<ChampionInstance> Champions { get; }

        /// <summary>
        /// Gets or sets the team's kill score.
        /// </summary>
        public int KillScore { get; set; }
    }
}
