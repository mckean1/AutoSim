using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Management.Models
{
    /// <summary>
    /// Represents a player who may be contracted to a team or remain a free agent.
    /// </summary>
    public sealed record Player
    {
        /// <summary>
        /// Gets the player identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the player display name.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Gets the player's position role.
        /// </summary>
        public required PositionRole PositionRole { get; init; }

        /// <summary>
        /// Gets the contracted team identifier, or null when the player is a free agent.
        /// </summary>
        public string? TeamId { get; init; }
    }
}
