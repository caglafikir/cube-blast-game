using System.Collections.Generic;
using DreamGames.Match.Core;
using DreamGames.Match.Items;
using DreamGames.Match.Items.Obstacles;
using DreamGames.Match.Items.SpecialItems;
using DreamGames.Match.Level;
using UnityEngine;

namespace DreamGames.Match.Grid
{
    /// <summary>
    /// Owns the logical width/height grid, converts between grid coordinates and world
    /// positions, spawns items from LevelData, and exposes lookups used by matching,
    /// falling and blast/explosion code.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Transform boardRoot;
        [SerializeField] private ItemPrefabRegistry prefabs = new ItemPrefabRegistry();
        [SerializeField] private BlastController blastController;
        [SerializeField] private FallController fallController;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public BlastController Blast => blastController;
        public FallController Fall => fallController;
        public ItemPrefabRegistry Prefabs => prefabs;

        private GridItem[,] items;

        /// <summary>Row 0 = bottom row, Col 0 = leftmost column (matches the level file spec).</summary>
        public GridItem GetItem(int row, int col) => IsInside(row, col) ? items[row, col] : null;

        public bool IsInside(int row, int col) => row >= 0 && row < Height && col >= 0 && col < Width;

        public bool IsEmpty(int row, int col) => IsInside(row, col) && items[row, col] == null;

        public Vector3 CellToWorld(int row, int col)
        {
            float x = (col - (Width - 1) / 2f) * cellSize;
            float y = (row - (Height - 1) / 2f) * cellSize;
            return boardRoot.TransformPoint(new Vector3(x, y, 0f));
        }

        /// <summary>Inverse of CellToWorld - used by InputController to turn a tap/click into a cell.</summary>
        public bool TryWorldToCell(Vector3 worldPos, out int row, out int col)
        {
            Vector3 local = boardRoot.InverseTransformPoint(worldPos);
            col = Mathf.RoundToInt(local.x / cellSize + (Width - 1) / 2f);
            row = Mathf.RoundToInt(local.y / cellSize + (Height - 1) / 2f);
            return IsInside(row, col);
        }

        /// <summary>Sizes an orthographic camera so the whole board plus margin fits, regardless of screen aspect.</summary>
        public void FitCameraToGrid(Camera cam, float margin = 1.2f)
        {
            if (cam == null || !cam.orthographic) return;

            float boardHalfWidth = Width * cellSize * 0.5f + margin;
            float boardHalfHeight = Height * cellSize * 0.5f + margin;

            float sizeToFitHeight = boardHalfHeight;
            float sizeToFitWidth = boardHalfWidth / cam.aspect;

            cam.orthographicSize = Mathf.Max(sizeToFitHeight, sizeToFitWidth);
        }

        /// <summary>Places an already-instantiated item at (row, col) and snaps it to that world position.</summary>
        public void PlaceItem(GridItem item, int row, int col, bool snapInstantly = true)
        {
            items[row, col] = item;
            item.Initialize(this, row, col);
            item.transform.SetParent(boardRoot, worldPositionStays: false);
            if (snapInstantly) item.transform.position = CellToWorld(row, col);
        }

        /// <summary>Clears (row,col) only if it currently points at the given item (avoids stale clears).</summary>
        public void ClearCell(int row, int col, GridItem expected)
        {
            if (IsInside(row, col) && items[row, col] == expected)
            {
                items[row, col] = null;
            }
        }

        /// <summary>Moves the bookkeeping for an item from its old cell to a new (currently empty) one.</summary>
        public void MoveItem(GridItem item, int fromRow, int fromCol, int toRow, int toCol)
        {
            if (IsInside(fromRow, fromCol) && items[fromRow, fromCol] == item)
            {
                items[fromRow, fromCol] = null;
            }
            items[toRow, toCol] = item;
        }

        public IEnumerable<Vector2Int> AllCells()
        {
            for (int r = 0; r < Height; r++)
                for (int c = 0; c < Width; c++)
                    yield return new Vector2Int(r, c);
        }

        public CubeColor RandomColor() => (CubeColor)Random.Range(0, 4);

        // ---------------------------------------------------------------
        // Level construction
        // ---------------------------------------------------------------

        /// <summary>Builds the board from parsed level data. Returns the number of obstacles spawned.</summary>
        public int BuildFromLevelData(LevelData data)
        {
            Width = data.grid_width;
            Height = data.grid_height;
            items = new GridItem[Height, Width];

            int obstacleCount = 0;
            var chaliceStates = new Dictionary<string, ChaliceBoxState>();

            for (int i = 0; i < data.grid.Length; i++)
            {
                int row = i / Width;
                int col = i % Width;
                string token = data.grid[i];

                switch (token)
                {
                    case LevelTokens.Red: SpawnCube(row, col, CubeColor.Red); break;
                    case LevelTokens.Green: SpawnCube(row, col, CubeColor.Green); break;
                    case LevelTokens.Blue: SpawnCube(row, col, CubeColor.Blue); break;
                    case LevelTokens.Yellow: SpawnCube(row, col, CubeColor.Yellow); break;
                    case LevelTokens.RandomColor: SpawnCube(row, col, RandomColor()); break;

                    case LevelTokens.HorizontalRocket: SpawnRocket(row, col, RocketOrientation.Horizontal); break;
                    case LevelTokens.VerticalRocket: SpawnRocket(row, col, RocketOrientation.Vertical); break;
                    case LevelTokens.Tnt: SpawnTnt(row, col); break;

                    case LevelTokens.Stone: SpawnStone(row, col); obstacleCount++; break;
                    case LevelTokens.Vase: SpawnVase(row, col); obstacleCount++; break;

                    case LevelTokens.ChaliceTopLeft:
                    case LevelTokens.ChaliceTopRight:
                    case LevelTokens.ChaliceBottomLeft:
                    case LevelTokens.ChaliceBottomRight:
                        obstacleCount += SpawnChaliceCorner(row, col, token, chaliceStates);
                        break;

                    default:
                        Debug.LogWarning($"[GridManager] Unknown level token '{token}' at index {i}");
                        break;
                }
            }

            return obstacleCount;
        }

        private void SpawnCube(int row, int col, CubeColor color)
        {
            Cube cube = Instantiate(prefabs.cubePrefab);
            cube.Setup(color, prefabs.SpriteFor(color));
            PlaceItem(cube, row, col);
        }

        private void SpawnRocket(int row, int col, RocketOrientation orientation)
        {
            Rocket rocket = Instantiate(prefabs.rocketPrefab);
            Sprite sprite = orientation == RocketOrientation.Horizontal
                ? prefabs.horizontalRocketSprite
                : prefabs.verticalRocketSprite;
            rocket.Setup(orientation, sprite);
            PlaceItem(rocket, row, col);
        }

        private void SpawnTnt(int row, int col)
        {
            Tnt tnt = Instantiate(prefabs.tntPrefab);
            tnt.Setup(prefabs.tntSprite);
            PlaceItem(tnt, row, col);
        }

        private void SpawnVase(int row, int col)
        {
            Vase vase = Instantiate(prefabs.vasePrefab);
            PlaceItem(vase, row, col);
        }

        private void SpawnStone(int row, int col)
        {
            Stone stone = Instantiate(prefabs.stonePrefab);
            PlaceItem(stone, row, col);
        }

        /// <summary>
        /// Chalice box corners can appear in any order in the grid list. We key pending
        /// boxes by their bottom-left cell (derived from whichever corner we see first)
        /// so all four tokens of the same box resolve to one shared ChaliceBoxState.
        /// </summary>
        private int SpawnChaliceCorner(int row, int col, string token, Dictionary<string, ChaliceBoxState> pending)
        {
            ChaliceBoxCorner corner;
            int blRow, blCol; // bottom-left cell of this box
            switch (token)
            {
                case LevelTokens.ChaliceBottomLeft: corner = ChaliceBoxCorner.BottomLeft; blRow = row; blCol = col; break;
                case LevelTokens.ChaliceBottomRight: corner = ChaliceBoxCorner.BottomRight; blRow = row; blCol = col - 1; break;
                case LevelTokens.ChaliceTopLeft: corner = ChaliceBoxCorner.TopLeft; blRow = row - 1; blCol = col; break;
                case LevelTokens.ChaliceTopRight: corner = ChaliceBoxCorner.TopRight; blRow = row - 1; blCol = col - 1; break;
                default: return 0;
            }

            string key = $"{blRow}_{blCol}";
            bool isNewBox = !pending.TryGetValue(key, out ChaliceBoxState state);
            if (isNewBox)
            {
                state = new ChaliceBoxState();
                pending[key] = state;
            }

            ChaliceBoxPart part = Instantiate(prefabs.chaliceBoxPartPrefab);
            part.Setup(corner, state);
            var sr = part.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = prefabs.chaliceDoorSprite;
            PlaceItem(part, row, col);

            // Only counts as one obstacle towards level obstacle-count bookkeeping the first time.
            return isNewBox ? 1 : 0;
        }
    }
}
