namespace AutoSim.Domain.Enums
{
    /// <summary>
    /// Defines the candidate pool for a combat effect.
    /// </summary>
    public enum TargetMode
    {
        EnemyFrontline,
        EnemyBackline,
        EnemyAny,

        AllyFrontline,
        AllyBackline,
        AllyAny,

        GlobalEnemy,
        GlobalAlly,
        GlobalAll,

        Self
    }
}
