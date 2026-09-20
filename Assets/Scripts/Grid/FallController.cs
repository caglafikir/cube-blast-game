using System;
using System.Collections;
using System.Collections.Generic;
using DreamGames.Match.Items;
using UnityEngine;

namespace DreamGames.Match.Grid
{
    /// <summary>
    /// Hand-implemented gravity: after cells are cleared, compacts each column downward and
    /// spawns new cubes to fill empty cells at the top. Fixed obstacles (CanFall == false)
    /// never move and split a column into independent segments.
    /// </summary>
    public class FallController : MonoBehaviour
    {
        [SerializeField] private float fallDurationPerCell = 0.09f;
        [SerializeField] private float minFallDuration = 0.08f;
        [SerializeField] private float spawnRowGap = 1f;

        public void ResolveFallAndRefill(GridManager grid, Action onSettled)
        {
            StartCoroutine(ResolveRoutine(grid, onSettled));
        }

        private IEnumerator ResolveRoutine(GridManager grid, Action onSettled)
        {
            int pending = 0;
            bool anyStarted = false;

            for (int col = 0; col < grid.Width; col++)
            {
                int writeRow = 0;

                for (int readRow = 0; readRow < grid.Height; readRow++)
                {
                    GridItem item = grid.GetItem(readRow, col);
                    if (item == null) continue;

                    if (!item.CanFall)
                    {
                        // Fixed obstacle: stays exactly where it is, resets compaction above it.
                        writeRow = readRow + 1;
                        continue;
                    }

                    if (writeRow < readRow)
                    {
                        int fromRow = readRow;
                        int toRow = writeRow;
                        grid.MoveItem(item, fromRow, col, toRow, col);
                        int distance = fromRow - toRow;
                        float duration = Mathf.Max(minFallDuration, distance * fallDurationPerCell);
                        pending++;
                        anyStarted = true;
                        item.AnimateToCell(toRow, col, duration, () => pending--);
                    }
                    writeRow++;
                }

                // Spawn new cubes for every empty cell left in this column's topmost segment.
                int spawnOffset = 0;
                for (int row = writeRow; row < grid.Height; row++)
                {
                    Cube cube = UnityEngine.Object.Instantiate(grid.Prefabs.cubePrefab);
                    var color = grid.RandomColor();
                    cube.Setup(color, grid.Prefabs.SpriteFor(color));

                    // Start above the grid, stacked so multiple new cubes fall in sequence.
                    grid.PlaceItem(cube, row, col, snapInstantly: false);
                    spawnOffset++;
                    Vector3 spawnWorldPos = grid.CellToWorld(grid.Height - 1, col) +
                                             new Vector3(0f, spawnRowGap * spawnOffset, 0f);
                    cube.transform.position = spawnWorldPos;

                    int distance = (grid.Height - row) + spawnOffset;
                    float duration = Mathf.Max(minFallDuration, distance * fallDurationPerCell);
                    pending++;
                    anyStarted = true;
                    cube.AnimateToCell(row, col, duration, () => pending--);
                }
            }

            // Wait a frame so all pending++ calls above are registered before we start polling.
            yield return null;
            while (anyStarted && pending > 0)
            {
                yield return null;
            }

            onSettled?.Invoke();
        }
    }
}
