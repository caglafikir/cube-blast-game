using System;
using System.Collections;
using System.Collections.Generic;
using DreamGames.Match.Core;
using DreamGames.Match.Items;
using DreamGames.Match.Items.SpecialItems;
using UnityEngine;

namespace DreamGames.Match.Grid
{
    /// <summary>
    /// Handles a single tap: finds the affected group, spawns special items, applies obstacle
    /// damage, resolves chain reactions/combos, then hands off to FallController to settle the board.
    /// </summary>
    public class BlastController : MonoBehaviour
    {
        [SerializeField] private GridManager grid;
        [SerializeField] private FallController fallController;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float collapseDuration = 0.18f;

        public bool IsBusy { get; private set; }

        public event Action OnMoveUsed;
        public event Action OnBoardSettled;

        private int sourceIdCounter;
        private readonly HashSet<GridItem> damagedObstaclesThisAction = new HashSet<GridItem>();

        private int NextSourceId() => ++sourceIdCounter;

        // ---------------------------------------------------------------
        // Entry points (called by Cube.OnTap / SpecialItem.OnTap)
        // ---------------------------------------------------------------

        public void HandleTapOnCube(Cube cube)
        {
            if (IsBusy) return;
            List<Cube> group = MatchFinder.FindConnectedCubeGroup(grid, cube.Row, cube.Col);
            if (group.Count < 2) return;
            StartCoroutine(ProcessCubeTap(cube, group));
        }

        public void HandleTapOnSpecialItem(SpecialItem item)
        {
            if (IsBusy) return;
            List<SpecialItem> group = MatchFinder.FindConnectedSpecialGroup(grid, item.Row, item.Col);
            StartCoroutine(ProcessSpecialTap(item, group));
        }

        /// <summary>Fallback single-item detonation path used by SpecialItem.TakeDamage.</summary>
        public void DetonateStandalone(SpecialItem item, int sourceId)
        {
            var exploded = new HashSet<SpecialItem> { item };
            Vector3 origin = item.transform.position;
            item.RemoveFromGrid();
            ApplyExplosionToCells(item.GetExplosionCells(), sourceId, exploded, origin);
        }

        // ---------------------------------------------------------------
        // Cube tap
        // ---------------------------------------------------------------

        private IEnumerator ProcessCubeTap(Cube tapped, List<Cube> group)
        {
            IsBusy = true;
            int sourceId = NextSourceId();
            damagedObstaclesThisAction.Clear();

            ApplyAdjacentObstacleDamage(group, sourceId);

            bool createsSpecial = group.Count >= 4;
            Vector2Int spawnCell = new Vector2Int(tapped.Row, tapped.Col);

            if (createsSpecial)
            {
                // Determined before the collapse animation destroys these Cube instances.
                RocketOrientation orientation = MatchFinder.DetermineRocketOrientation(group);

                yield return StartCoroutine(CollapseGroupRoutine(group.ConvertAll(g => (GridItem)g), spawnCell));

                if (group.Count >= 6)
                {
                    SpawnTnt(spawnCell);
                }
                else
                {
                    SpawnRocket(spawnCell, orientation);
                }
            }
            else
            {
                Vector3 centroid = Vector3.zero;
                foreach (Cube cube in group) centroid += grid.CellToWorld(cube.Row, cube.Col);
                centroid /= group.Count;
                Utils.VfxSpawner.Spawn(grid.Prefabs.blastBurstPrefab, centroid);

                foreach (Cube cube in group)
                {
                    cube.DestroyByExplosion();
                }
            }

            OnMoveUsed?.Invoke();
            yield return StartCoroutine(SettleBoard());
            IsBusy = false;
        }

        private void ApplyAdjacentObstacleDamage(List<Cube> group, int sourceId)
        {
            var groupCells = new HashSet<Vector2Int>();
            foreach (var c in group) groupCells.Add(new Vector2Int(c.Row, c.Col));

            Vector2Int[] dirs =
            {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1)
            };

            foreach (var cell in groupCells)
            {
                foreach (var d in dirs)
                {
                    Vector2Int n = cell + d;
                    if (groupCells.Contains(n)) continue;
                    if (!grid.IsInside(n.x, n.y)) continue;

                    GridItem occupant = grid.GetItem(n.x, n.y);
                    if (occupant is IDamageable damageable && !(occupant is SpecialItem) && !(occupant is Cube))
                    {
                        if (damagedObstaclesThisAction.Contains(occupant)) continue;
                        damagedObstaclesThisAction.Add(occupant);
                        damageable.TakeDamage(DamageContext.AdjacentBlast(group.Count, sourceId));
                    }
                }
            }
        }

        // ---------------------------------------------------------------
        // Special item tap (single explosion or combo)
        // ---------------------------------------------------------------

        private IEnumerator ProcessSpecialTap(SpecialItem tapped, List<SpecialItem> group)
        {
            IsBusy = true;
            int sourceId = NextSourceId();
            damagedObstaclesThisAction.Clear();
            var exploded = new HashSet<SpecialItem>();
            Vector2Int center = new Vector2Int(tapped.Row, tapped.Col);

            if (group.Count <= 1)
            {
                exploded.Add(tapped);
                Vector3 origin = tapped.transform.position;
                tapped.RemoveFromGrid();
                ApplyExplosionToCells(tapped.GetExplosionCells(), sourceId, exploded, origin);
            }
            else
            {
                yield return StartCoroutine(CollapseGroupRoutine(group.ConvertAll(g => (GridItem)g), center));
                foreach (var g in group) exploded.Add(g);

                int tntCount = 0;
                foreach (var g in group) if (g is Tnt) tntCount++;
                int rocketCount = group.Count - tntCount;

                IEnumerable<Vector2Int> cells;
                if (tntCount >= 2) cells = ComboShapes.TntTnt(center);
                else if (tntCount >= 1 && rocketCount >= 1) cells = ComboShapes.TntRocket(grid, center);
                else cells = ComboShapes.RocketRocket(grid, center);

                ApplyExplosionToCells(cells, sourceId, exploded, grid.CellToWorld(center.x, center.y));
            }

            OnMoveUsed?.Invoke();
            yield return StartCoroutine(SettleBoard());
            IsBusy = false;
        }

        // ---------------------------------------------------------------
        // Shared explosion application (handles chain reactions into other special items)
        // ---------------------------------------------------------------

        private void ApplyExplosionToCells(IEnumerable<Vector2Int> cells, int sourceId, HashSet<SpecialItem> exploded, Vector3 originWorldPos)
        {
            Utils.VfxSpawner.Spawn(grid.Prefabs.explosionBurstPrefab, originWorldPos);
            if (cameraTransform != null) Utils.TweenUtil.Shake(this, cameraTransform, 0.16f, 0.2f);

            // Copy first: exploding an item can mutate the grid while we're iterating combo shapes.
            var cellList = new List<Vector2Int>(cells);
            foreach (var cell in cellList)
            {
                if (!grid.IsInside(cell.x, cell.y)) continue;
                GridItem occupant = grid.GetItem(cell.x, cell.y);
                if (occupant == null) continue;

                if (occupant is Cube cube)
                {
                    cube.DestroyByExplosion();
                }
                else if (occupant is SpecialItem special)
                {
                    if (exploded.Contains(special)) continue;
                    exploded.Add(special);
                    Vector3 chainOrigin = special.transform.position;
                    special.RemoveFromGrid();
                    ApplyExplosionToCells(special.GetExplosionCells(), sourceId, exploded, chainOrigin);
                }
                else if (occupant is IDamageable damageable)
                {
                    damageable.TakeDamage(DamageContext.SpecialExplosionCell(sourceId));
                }
            }
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private IEnumerator CollapseGroupRoutine(List<GridItem> items, Vector2Int target)
        {
            int pending = items.Count;
            Vector3 targetPos = grid.CellToWorld(target.x, target.y);
            foreach (var item in items)
            {
                grid.ClearCell(item.Row, item.Col, item);
                Utils.TweenUtil.MoveTo(this, item.transform, targetPos, collapseDuration, () =>
                {
                    if (item != null) Destroy(item.gameObject);
                    pending--;
                });
            }
            yield return new WaitUntil(() => pending <= 0);
        }

        private void SpawnRocket(Vector2Int cell, RocketOrientation orientation)
        {
            Rocket rocket = Instantiate(grid.Prefabs.rocketPrefab);
            Sprite sprite = orientation == RocketOrientation.Horizontal
                ? grid.Prefabs.horizontalRocketSprite
                : grid.Prefabs.verticalRocketSprite;
            rocket.Setup(orientation, sprite);
            grid.PlaceItem(rocket, cell.x, cell.y);
            Utils.TweenUtil.Punch(this, rocket.transform, strength: 1.4f);
            Utils.VfxSpawner.Spawn(grid.Prefabs.blastBurstPrefab, rocket.transform.position);
        }

        private void SpawnTnt(Vector2Int cell)
        {
            Tnt tnt = Instantiate(grid.Prefabs.tntPrefab);
            tnt.Setup(grid.Prefabs.tntSprite);
            grid.PlaceItem(tnt, cell.x, cell.y);
            Utils.TweenUtil.Punch(this, tnt.transform, strength: 1.4f);
            Utils.VfxSpawner.Spawn(grid.Prefabs.blastBurstPrefab, tnt.transform.position);
        }

        private IEnumerator SettleBoard()
        {
            bool done = false;
            fallController.ResolveFallAndRefill(grid, () => done = true);
            yield return new WaitUntil(() => done);
            MatchFinder.RefreshHints(grid, grid.Prefabs);
            OnBoardSettled?.Invoke();
        }
    }
}
