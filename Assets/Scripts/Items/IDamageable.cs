using DreamGames.Match.Core;

namespace DreamGames.Match.Items
{
    /// <summary>
    /// Anything that can receive damage from an adjacent blast or a special item explosion:
    /// obstacles (Vase/Stone/ChaliceBox) and special items (they "explode when damaged").
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Applies damage. Returns true if this call destroyed/consumed the item
        /// (caller should then remove it from the grid).
        /// </summary>
        bool TakeDamage(DamageContext context);
    }
}
