using DreamGames.Match.Core;
using DreamGames.Match.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DreamGames.Match.UI
{
    /// <summary>
    /// MainScene's LevelButton: shows the player's current level, or a "finished" message
    /// once every level has been completed. Tapping it loads LevelScene.
    /// </summary>
    public class MainSceneController : MonoBehaviour
    {
        [SerializeField] private Button levelButton;
        [SerializeField] private Text levelButtonText;
        [SerializeField] private string finishedText = "All levels finished!";

        private void Awake()
        {
            levelButton?.onClick.AddListener(OnLevelButtonClicked);
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (levelButtonText == null) return;

            if (GameProgress.AllLevelsFinished)
            {
                levelButtonText.text = finishedText;
            }
            else
            {
                levelButtonText.text = $"Level {GameProgress.CurrentLevel}";
            }
        }

        private void OnLevelButtonClicked()
        {
            if (GameProgress.AllLevelsFinished) return;
            SceneManager.LoadScene(SceneNames.Level);
        }
    }
}
