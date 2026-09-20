using UnityEngine;

namespace DreamGames.Match.Persistence
{
    /// <summary>
    /// Thin wrapper around PlayerPrefs that persists the player's last played level.
    /// Plain C# (no MonoBehaviour) so it can be used from both runtime code and
    /// editor tooling (see Assets/Editor/SetLastPlayedLevelWindow.cs).
    /// </summary>
    public static class GameProgress
    {
        private const string LastPlayedLevelKey = "DreamGames.LastPlayedLevel";

        /// <summary>Total number of levels shipped with the game.</summary>
        public const int TotalLevels = 10;

        /// <summary>
        /// 1-based index of the level the player should play next.
        /// Clamped between 1 and TotalLevels + 1 (TotalLevels + 1 means "all levels finished").
        /// </summary>
        public static int CurrentLevel
        {
            get
            {
                int stored = PlayerPrefs.GetInt(LastPlayedLevelKey, 1);
                return Mathf.Clamp(stored, 1, TotalLevels + 1);
            }
            set
            {
                int clamped = Mathf.Clamp(value, 1, TotalLevels + 1);
                PlayerPrefs.SetInt(LastPlayedLevelKey, clamped);
                PlayerPrefs.Save();
            }
        }

        public static bool AllLevelsFinished => CurrentLevel > TotalLevels;

        /// <summary>Advances progress after winning the given level.</summary>
        public static void ReportLevelWon(int levelNumber)
        {
            if (levelNumber >= CurrentLevel)
            {
                CurrentLevel = levelNumber + 1;
            }
        }

        public static void ResetProgress()
        {
            CurrentLevel = 1;
        }
    }
}
