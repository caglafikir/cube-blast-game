using DreamGames.Match.Core;
using UnityEngine;

namespace DreamGames.Match.Items.SpecialItems
{
    /// <summary>
    /// Base class for Rocket and Tnt. Explodes when tapped directly or damaged by another
    /// explosion; adjacent special items combine into a combo explosion instead.
    /// </summary>
    public abstract class SpecialItem : GridItem, IDamageable
    {
        public abstract SpecialItemType Type { get; }

        public override void OnTap()
        {
            Grid.Blast.HandleTapOnSpecialItem(this);
        }

        /// <summary>A single explosion cell hit (from another special item) always detonates this one.</summary>
        public bool TakeDamage(DamageContext context)
        {
            Grid.Blast.DetonateStandalone(this, context.SourceId);
            return true;
        }

        /// <summary>Cells this item damages when it explodes on its own (not as part of a combo).</summary>
        public abstract System.Collections.Generic.IEnumerable<Vector2Int> GetExplosionCells();
    }
}
