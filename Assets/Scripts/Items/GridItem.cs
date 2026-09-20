using DreamGames.Match.Grid;
using UnityEngine;

namespace DreamGames.Match.Items
{
    /// <summary>Base class for anything occupying a single grid cell. Handles coordinates and the fall animation.</summary>
    public abstract class GridItem : MonoBehaviour
    {
        public int Row { get; private set; }
        public int Col { get; private set; }
        public GridManager Grid { get; private set; }

        public bool IsFalling { get; private set; }

        /// <summary>Can this item fall down into empty cells below it?</summary>
        public virtual bool CanFall => true;

        /// <summary>Does this item block other items from falling through its cell?</summary>
        public virtual bool BlocksFall => true;

        public virtual void Initialize(GridManager grid, int row, int col)
        {
            Grid = grid;
            Row = row;
            Col = col;
        }

        /// <summary>Instantly updates logical coordinates (used by fall/refill bookkeeping).</summary>
        public void SetCoordinates(int row, int col)
        {
            Row = row;
            Col = col;
        }

        /// <summary>Animates from current world position down/into the given cell.</summary>
        public void AnimateToCell(int row, int col, float duration, System.Action onComplete = null)
        {
            SetCoordinates(row, col);
            IsFalling = true;
            Vector3 target = Grid.CellToWorld(row, col);
            Utils.TweenUtil.MoveTo(this, transform, target, duration, () =>
            {
                IsFalling = false;
                Utils.TweenUtil.SquashLand(this, transform);
                onComplete?.Invoke();
            });
        }

        /// <summary>Called by BlastController when the player taps directly on this item.</summary>
        public abstract void OnTap();

        /// <summary>Frees the cell immediately, then plays a pop+fade before destroying the GameObject.</summary>
        public virtual void RemoveFromGrid()
        {
            if (Grid != null) Grid.ClearCell(Row, Col, this);
            PlayRemovalAnimation();
        }

        protected virtual void PlayRemovalAnimation()
        {
            if (this == null) return;
            if (!gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }
            Utils.TweenUtil.PopOut(this, transform, 0.16f, () =>
            {
                if (this != null) Destroy(gameObject);
            });
        }
    }
}
