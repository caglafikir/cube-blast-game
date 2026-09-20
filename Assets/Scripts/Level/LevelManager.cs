using System.Collections.Generic;
using DreamGames.Match.Core;
using DreamGames.Match.Grid;
using DreamGames.Match.Items.Obstacles;
using DreamGames.Match.Persistence;
using DreamGames.Match.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamGames.Match.Level
{
    /// <summary>
    /// Drives a single play-through of LevelScene: loads the level file for the player's
    /// current level, tracks moves/obstacles, and reacts to win/lose.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private GridManager grid;
        [SerializeField] private LevelHudController hud;
        [SerializeField] private FailPopupController failPopup;
        [SerializeField] private WinCelebrationController winCelebration;

        private int levelNumber;
        private int movesRemaining;
        private int obstaclesRemaining;
        private int vasesRemaining;
        private int stonesRemaining;
        private int chaliceBoxesRemaining;
        private bool levelOver;

        private void Start()
        {
            levelNumber = Mathf.Clamp(GameProgress.CurrentLevel, 1, GameProgress.TotalLevels);

            LevelData data = LevelLoader.Load(levelNumber);
            if (data == null)
            {
                Debug.LogError($"[LevelManager] Could not load level {levelNumber}, returning to MainScene.");
                SceneManager.LoadScene(SceneNames.Main);
                return;
            }

            movesRemaining = data.move_count;
            grid.BuildFromLevelData(data);
            grid.FitCameraToGrid(Camera.main);
            MatchFinder.RefreshHints(grid, grid.Prefabs);

            grid.Blast.OnMoveUsed += HandleMoveUsed;
            grid.Blast.OnBoardSettled += HandleBoardSettled;

            hud?.SetLevelNumber(levelNumber);
            hud?.SetMoves(movesRemaining);

            obstaclesRemaining = CountObstacles(out vasesRemaining, out stonesRemaining, out chaliceBoxesRemaining);
            hud?.ConfigureObstacleGoals(vasesRemaining > 0, stonesRemaining > 0, chaliceBoxesRemaining > 0);
            hud?.SetObstacleGoals(vasesRemaining, stonesRemaining, chaliceBoxesRemaining);
        }

        private void OnDestroy()
        {
            if (grid != null && grid.Blast != null)
            {
                grid.Blast.OnMoveUsed -= HandleMoveUsed;
                grid.Blast.OnBoardSettled -= HandleBoardSettled;
            }
        }

        private void HandleMoveUsed()
        {
            if (levelOver) return;
            movesRemaining = Mathf.Max(0, movesRemaining - 1);
            hud?.SetMoves(movesRemaining);
        }

        private void HandleBoardSettled()
        {
            if (levelOver) return;

            obstaclesRemaining = CountObstacles(out vasesRemaining, out stonesRemaining, out chaliceBoxesRemaining);
            hud?.SetObstacleGoals(vasesRemaining, stonesRemaining, chaliceBoxesRemaining);

            if (obstaclesRemaining <= 0)
            {
                WinLevel();
            }
            else if (movesRemaining <= 0)
            {
                LoseLevel();
            }
        }

        /// <summary>Scans the live grid for how many of each obstacle type are still standing.</summary>
        private int CountObstacles(out int vases, out int stones, out int chalices)
        {
            vases = 0;
            stones = 0;
            var chaliceSeen = new HashSet<ChaliceBoxState>();

            foreach (var pos in grid.AllCells())
            {
                var occupant = grid.GetItem(pos.x, pos.y);
                if (occupant is Vase) vases++;
                else if (occupant is Stone) stones++;
                else if (occupant is ChaliceBoxPart part) chaliceSeen.Add(part.State);
            }

            chalices = chaliceSeen.Count;
            return vases + stones + chalices;
        }

        private void WinLevel()
        {
            levelOver = true;
            GameProgress.ReportLevelWon(levelNumber);

            if (winCelebration != null)
            {
                winCelebration.Play(() => SceneManager.LoadScene(SceneNames.Main));
            }
            else
            {
                SceneManager.LoadScene(SceneNames.Main);
            }
        }

        private void LoseLevel()
        {
            levelOver = true;

            if (failPopup != null)
            {
                failPopup.Show(
                    onClose: () => SceneManager.LoadScene(SceneNames.Main),
                    onTryAgain: () => SceneManager.LoadScene(SceneNames.Level));
            }
            else
            {
                SceneManager.LoadScene(SceneNames.Main);
            }
        }
    }
}
