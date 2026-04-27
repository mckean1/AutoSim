using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Objects
{
    /// <summary>
    /// Represents a temporary skirmish inside a round.
    /// </summary>
    public sealed class FightState
    {
        /// <summary>
        /// Gets or sets the stable fight identifier.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the fight lane.
        /// </summary>
        public Lane Lane { get; set; }

        /// <summary>
        /// Gets or sets the fight midpoint.
        /// </summary>
        public double Position { get; set; }

        /// <summary>
        /// Gets the champions that have participated in this fight.
        /// </summary>
        public IList<ChampionInstance> Participants { get; } = [];
    }
}
