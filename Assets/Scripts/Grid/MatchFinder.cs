using System.Collections.Generic;
using DreamGames.Match.Items;
using DreamGames.Match.Items.SpecialItems;
using UnityEngine;

namespace DreamGames.Match.Grid
{
    /// <summary>
    /// Flood-fill helpers for finding connected groups of same-color cubes and connected
    /// groups of special items (for combos). 4-directional adjacency only, as specified.
    /// </summary>
    public static class MatchFinder
    {
        private static readonly Vector2Int[] Neighbors4 =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        /// <summary>All cubes directly/transitively connected to (row,col) that share its color.</summary>
        public static List<Cube> FindConnectedCubeGroup(GridManager grid, int row, int col)
        {
            var result = new List<Cube>();
            if (!(grid.GetItem(row, col) is Cube start)) return result;

            var visited = new HashSet<Vector2Int>();
            var stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(row, col));
            visited.Add(new Vector2Int(row, col));

            while (stack.Count > 0)
            {
                Vector2Int pos = stack.Pop();
                if (grid.GetItem(pos.x, pos.y) is Cube cube && cube.Color == start.Color)
                {
                    result.Add(cube);
                    foreach (var d in Neighbors4)
                    {
                        Vector2Int next = pos + d;
                        if (visited.Contains(next)) continue;
                        if (!grid.IsInside(next.x, next.y)) continue;
                        if (grid.GetItem(next.x, next.y) is Cube n && n.Color == start.Color)
                        {
                            visited.Add(next);
                            stack.Push(next);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>All special items (any type) directly/transitively adjacent to (row,col) - used for combos.</summary>
        public static List<SpecialItem> FindConnectedSpecialGroup(GridManager grid, int row, int col)
        {
            var result = new List<SpecialItem>();
            if (!(grid.GetItem(row, col) is SpecialItem)) return result;

            var visited = new HashSet<Vector2Int>();
            var stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(row, col));
            visited.Add(new Vector2Int(row, col));

            while (stack.Count > 0)
            {
                Vector2Int pos = stack.Pop();
                if (grid.GetItem(pos.x, pos.y) is SpecialItem item)
                {
                    result.Add(item);
                    foreach (var d in Neighbors4)
                    {
                        Vector2Int next = pos + d;
                        if (visited.Contains(next)) continue;
                        if (!grid.IsInside(next.x, next.y)) continue;
                        if (grid.GetItem(next.x, next.y) is SpecialItem)
                        {
                            visited.Add(next);
                            stack.Push(next);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>Scans the whole board and refreshes each cube's "eligible for special" hint icon.</summary>
        public static void RefreshHints(GridManager grid, ItemPrefabRegistry prefabs)
        {
            var handled = new HashSet<Vector2Int>();
            foreach (var pos in grid.AllCells())
            {
                if (handled.Contains(pos)) continue;
                if (!(grid.GetItem(pos.x, pos.y) is Cube)) continue;

                List<Cube> group = FindConnectedCubeGroup(grid, pos.x, pos.y);
                bool eligible = group.Count >= 4;
                Sprite hint = null;
                if (eligible)
                {
                    if (group.Count >= 6)
                    {
                        hint = prefabs.tntHintSprite;
                    }
                    else
                    {
                        hint = DetermineRocketOrientation(group) == RocketOrientation.Horizontal
                            ? prefabs.hRocketHintSprite
                            : prefabs.vRocketHintSprite;
                    }
                }

                foreach (var cube in group)
                {
                    cube.SetHint(eligible, hint);
                    handled.Add(new Vector2Int(cube.Row, cube.Col));
                }
            }
        }

        /// <summary>
        /// Deterministic orientation for the rocket a 4-5 cube group would create, based on the
        /// group's bounding box (wider than tall -> horizontal, taller than wide -> vertical).
        /// Used by both the hint icon and the actual spawn so they always agree.
        /// </summary>
        public static RocketOrientation DetermineRocketOrientation(List<Cube> group)
        {
            int minRow = int.MaxValue, maxRow = int.MinValue, minCol = int.MaxValue, maxCol = int.MinValue;
            foreach (var cube in group)
            {
                minRow = Mathf.Min(minRow, cube.Row);
                maxRow = Mathf.Max(maxRow, cube.Row);
                minCol = Mathf.Min(minCol, cube.Col);
                maxCol = Mathf.Max(maxCol, cube.Col);
            }

            int width = maxCol - minCol;
            int height = maxRow - minRow;
            return width >= height ? RocketOrientation.Horizontal : RocketOrientation.Vertical;
        }
    }
}
