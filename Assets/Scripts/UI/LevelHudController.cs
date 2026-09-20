using UnityEngine;
using UnityEngine.UI;

namespace DreamGames.Match.UI
{
    /// <summary>HUD in LevelScene: level number, moves remaining, and a per-obstacle-type goal row.</summary>
    public class LevelHudController : MonoBehaviour
    {
        [SerializeField] private Text levelText;
        [SerializeField] private Text movesText;

        [Header("Obstacle Goals")]
        [SerializeField] private GameObject vaseGoalRoot;
        [SerializeField] private Text vaseGoalText;
        [SerializeField] private GameObject stoneGoalRoot;
        [SerializeField] private Text stoneGoalText;
        [SerializeField] private GameObject chaliceGoalRoot;
        [SerializeField] private Text chaliceGoalText;

        public void SetLevelNumber(int level)
        {
            if (levelText != null) levelText.text = $"Level {level}";
        }

        public void SetMoves(int moves)
        {
            if (movesText != null) movesText.text = $"Moves: {moves}";
        }

        /// <summary>Hides the goal icon for any obstacle type this level doesn't contain.</summary>
        public void ConfigureObstacleGoals(bool hasVases, bool hasStones, bool hasChalices)
        {
            if (vaseGoalRoot != null) vaseGoalRoot.SetActive(hasVases);
            if (stoneGoalRoot != null) stoneGoalRoot.SetActive(hasStones);
            if (chaliceGoalRoot != null) chaliceGoalRoot.SetActive(hasChalices);
        }

        /// <summary>Updates each remaining-count label.</summary>
        public void SetObstacleGoals(int vasesRemaining, int stonesRemaining, int chalicesRemaining)
        {
            if (vaseGoalText != null) vaseGoalText.text = $"x{Mathf.Max(0, vasesRemaining)}";
            if (stoneGoalText != null) stoneGoalText.text = $"x{Mathf.Max(0, stonesRemaining)}";
            if (chaliceGoalText != null) chaliceGoalText.text = $"x{Mathf.Max(0, chalicesRemaining)}";
        }
    }
}
