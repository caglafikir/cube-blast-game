using DreamGames.Match.Core;
using UnityEngine;

namespace DreamGames.Match.Items
{
    /// <summary>
    /// A single-color blockable cube. Cubes are removed directly (no health) whenever they
    /// are part of a tapped group or hit by a special item explosion.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Cube : GridItem
    {
        public CubeColor Color { get; private set; }

        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer hintRenderer; // special item hint icon, hidden by default

        public void Setup(CubeColor color, Sprite bodySprite)
        {
            Color = color;
            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
            bodyRenderer.sprite = bodySprite;
            SetHint(false, null);
        }

        /// <summary>Shows/hides the little icon indicating this cube is part of an eligible group.</summary>
        public void SetHint(bool visible, Sprite hintSprite)
        {
            if (hintRenderer == null) return;
            hintRenderer.enabled = visible;
            if (visible) hintRenderer.sprite = hintSprite;
        }

        public override void OnTap()
        {
            Grid.Blast.HandleTapOnCube(this);
        }

        /// <summary>Directly destroyed by an explosion cell (no health to track).</summary>
        public void DestroyByExplosion()
        {
            RemoveFromGrid();
        }
    }
}
