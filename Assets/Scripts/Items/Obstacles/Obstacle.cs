using DreamGames.Match.Core;
using UnityEngine;

namespace DreamGames.Match.Items.Obstacles
{
    /// <summary>
    /// Base class for single-cell obstacles (Vase, Stone). ChaliceBox is handled separately
    /// because it spans a 2x2 area instead of a single grid cell.
    /// </summary>
    public abstract class Obstacle : GridItem, IDamageable
    {
        [SerializeField] protected int health = 1;

        /// <summary>Obstacles are never tap-targets themselves; tapping them does nothing.</summary>
        public override void OnTap() { }

        protected abstract bool AcceptsAdjacentBlastDamage { get; }
        protected abstract bool AcceptsSpecialExplosionDamage { get; }

        public virtual bool TakeDamage(DamageContext context)
        {
            bool accepted = context.SourceType == DamageSourceType.AdjacentBlast
                ? AcceptsAdjacentBlastDamage
                : AcceptsSpecialExplosionDamage;

            if (!accepted) return false;

            health -= 1;
            OnDamaged();

            if (health <= 0)
            {
                RemoveFromGrid();
                return true;
            }

            Utils.TweenUtil.Punch(this, transform, strength: 1.15f, duration: 0.14f);
            return false;
        }

        protected virtual void OnDamaged() { }
    }
}
