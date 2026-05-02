namespace AGNIDAWN.Gameplay.MetaProgression
{
    /// <summary>
    /// Run difficulty levels, unlocked progressively via shrines.
    /// Normal is available from the start; Tandav and Pralaya require shrine unlocks.
    /// Linear: FAI-13
    /// </summary>
    public enum DifficultyLevel
    {
        Normal  = 0,   // Default — always unlocked
        Tandav  = 1,   // Hard — unlocked via Shiva shrine tier 2
        Pralaya = 2    // Extreme — unlocked via Shiva shrine tier 3
    }
}
