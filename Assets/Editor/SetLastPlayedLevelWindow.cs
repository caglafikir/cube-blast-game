using DreamGames.Match.Persistence;
using UnityEditor;
using UnityEngine;

namespace DreamGames.Match.EditorTools
{
    /// <summary>
    /// Case study requirement: "A Unity editor menu item should be implemented to set the
    /// last played level number." Opens a tiny window with an int field and a Set button;
    /// writes straight through GameProgress (PlayerPrefs), so it works whether or not the
    /// game is in Play Mode.
    /// </summary>
    public class SetLastPlayedLevelWindow : EditorWindow
    {
        private int levelNumber = 1;

        [MenuItem("Dream Games/Set Last Played Level...")]
        private static void Open()
        {
            var window = GetWindow<SetLastPlayedLevelWindow>(true, "Set Last Played Level", true);
            window.levelNumber = GameProgress.CurrentLevel;
            window.minSize = new Vector2(280, 90);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                $"Current persisted level: {GameProgress.CurrentLevel} " +
                $"(1-{GameProgress.TotalLevels}, or {GameProgress.TotalLevels + 1} = all finished).",
                MessageType.Info);

            levelNumber = EditorGUILayout.IntField("Level Number", levelNumber);
            levelNumber = Mathf.Clamp(levelNumber, 1, GameProgress.TotalLevels + 1);

            EditorGUILayout.Space();
            if (GUILayout.Button("Set"))
            {
                GameProgress.CurrentLevel = levelNumber;
                Debug.Log($"[Dream Games] Last played level set to {GameProgress.CurrentLevel}.");
                Close();
            }
        }
    }
}
