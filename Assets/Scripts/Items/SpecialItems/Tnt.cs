using System.Collections.Generic;
using DreamGames.Match.Core;
using UnityEngine;

namespace DreamGames.Match.Items.SpecialItems
{
    /// <summary>Explodes in a 5x5 area centered on itself.</summary>
    public class Tnt : SpecialItem
    {
        public override SpecialItemType Type => SpecialItemType.Tnt;
        private const int Radius = 2; // 2 -> 5x5

        public void Setup(Sprite sprite)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = sprite;
        }

        public override IEnumerable<Vector2Int> GetExplosionCells()
        {
            for (int dr = -Radius; dr <= Radius; dr++)
                for (int dc = -Radius; dc <= Radius; dc++)
                    yield return new Vector2Int(Row + dr, Col + dc);
        }
    }
}
