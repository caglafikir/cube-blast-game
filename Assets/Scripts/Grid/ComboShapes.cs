using System.Collections.Generic;
using UnityEngine;

namespace DreamGames.Match.Grid
{
    /// <summary>Cell patterns for adjacent-special-item combos, centered on the tapped cell.</summary>
    public static class ComboShapes
    {
        public static IEnumerable<Vector2Int> RocketRocket(GridManager grid, Vector2Int center)
        {
            for (int c = 0; c < grid.Width; c++) yield return new Vector2Int(center.x, c);
            for (int r = 0; r < grid.Height; r++) yield return new Vector2Int(r, center.y);
        }

        public static IEnumerable<Vector2Int> TntRocket(GridManager grid, Vector2Int center)
        {
            for (int dr = -1; dr <= 1; dr++)
            {
                int row = center.x + dr;
                for (int c = 0; c < grid.Width; c++) yield return new Vector2Int(row, c);
            }
            for (int dc = -1; dc <= 1; dc++)
            {
                int col = center.y + dc;
                for (int r = 0; r < grid.Height; r++) yield return new Vector2Int(r, col);
            }
        }

        public static IEnumerable<Vector2Int> TntTnt(Vector2Int center)
        {
            const int radius = 3; // 3 -> 7x7
            for (int dr = -radius; dr <= radius; dr++)
                for (int dc = -radius; dc <= radius; dc++)
                    yield return new Vector2Int(center.x + dr, center.y + dc);
        }
    }
}
