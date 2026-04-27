namespace AutoSim.Domain.Enums
{
    /// <summary>
    /// Defines the supported combat effect types.
    /// </summary>
    public enum CombatEffectType
    {
        /// <summary>
        /// Reduces a target's shields or health.
        /// </summary>
        Damage,

        /// <summary>
        /// Restores a living target's health.
        /// </summary>
        Heal,

        /// <summary>
        /// Adds temporary damage absorption.
        /// </summary>
        Shield
    }
}
