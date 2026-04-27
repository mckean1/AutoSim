using AutoSim.Domain.Enums;

namespace AutoSim.Domain.Management.Services
{
    /// <summary>
    /// Describes where the human coach and team should be generated.
    /// </summary>
    public sealed record HumanPlacement(LeagueRegion Region, DivisionName Division, int Slot);
}
