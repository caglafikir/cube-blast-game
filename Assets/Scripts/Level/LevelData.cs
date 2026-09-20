using System;

namespace DreamGames.Match.Level
{
    /// <summary>
    /// Raw, JSON-serializable representation of a level, matching the file format
    /// described in the case study ("Levels" section).
    /// grid[] starts at the bottom-left cell and reads left-to-right, bottom-to-top.
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public int level_number;
        public int grid_width;
        public int grid_height;
        public int move_count;
        public string[] grid;
    }
}
