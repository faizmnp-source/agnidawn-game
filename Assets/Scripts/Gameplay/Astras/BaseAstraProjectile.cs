using AGNIDAWN.Player;

namespace AGNIDAWN.Gameplay.Astras
{
    /// <summary>
    /// Intermediate base for all divine Astra projectiles.
    /// Inherits Projectile's pooling, movement, and hit framework.
    /// Each of the 10 divine weapons overrides virtual hooks for unique behaviour.
    /// Assembly: AGNIDAWN.Gameplay  |  Linear: FAI-11
    /// </summary>
    public abstract class BaseAstraProjectile : Projectile
    {
        // No additional state — subclasses bring their own.
        // Exists to give the 10 astras a shared type for editor filtering
        // and future cross-astra logic (e.g. synergy detection).
    }
}
