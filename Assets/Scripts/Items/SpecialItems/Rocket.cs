using System.Collections.Generic;
using DreamGames.Match.Core;
using UnityEngine;

namespace DreamGames.Match.Items.SpecialItems
{
    public enum RocketOrientation { Horizontal, Vertical }

    /// <summary>
    /// Splits into two parts moving in opposite directions, damaging every cell of its
    /// row (Horizontal) or column (Vertical).
    /// </summary>
    public class Rocket : SpecialItem
    {
        public RocketOrientation Orientation { get; private set; }
        public override SpecialItemType Type =>
            Orientation == RocketOrientation.Horizontal ? SpecialItemType.HorizontalRocket : SpecialItemType.VerticalRocket;

        public void Setup(RocketOrientation orientation, Sprite sprite)
        {
            Orientation = orientation;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = sprite;
        }

        public override IEnumerable<Vector2Int> GetExplosionCells()
        {
            if (Orientation == RocketOrientation.Horizontal)
            {
                for (int c = 0; c < Grid.Width; c++)
                    yield return new Vector2Int(Row, c);
            }
            else
            {
                for (int r = 0; r < Grid.Height; r++)
                    yield return new Vector2Int(r, Col);
            }
        }
    }
}
