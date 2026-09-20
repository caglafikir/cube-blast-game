using System.IO;
using UnityEngine;

namespace DreamGames.Match.Level
{
    /// <summary>
    /// Loads level_XX.json files from StreamingAssets/Levels.
    /// </summary>
    public static class LevelLoader
    {
        private const string LevelsFolder = "Levels";

        public static string GetLevelFileName(int levelNumber) => $"level_{levelNumber:D2}.json";

        /// <summary>Loads and parses a level. Returns null if the file could not be found/parsed.</summary>
        public static LevelData Load(int levelNumber)
        {
            string path = Path.Combine(Application.streamingAssetsPath, LevelsFolder, GetLevelFileName(levelNumber));

            if (!File.Exists(path))
            {
                Debug.LogError($"[LevelLoader] Level file not found: {path}");
                return null;
            }

            string json = File.ReadAllText(path);
            LevelData data = JsonUtility.FromJson<LevelData>(json);

            if (data == null)
            {
                Debug.LogError($"[LevelLoader] Failed to parse level file: {path}");
                return null;
            }

            if (data.grid == null || data.grid.Length != data.grid_width * data.grid_height)
            {
                Debug.LogError($"[LevelLoader] Level {levelNumber} grid size mismatch: " +
                                $"expected {data.grid_width * data.grid_height}, got {data.grid?.Length ?? 0}");
                return null;
            }

            return data;
        }
    }
}
