using System;

namespace BlastGame.Data
{
    [Serializable]
    public class LevelData
    {
        public int level_number;
        public int grid_width;
        public int grid_height;
        public int move_count;
        public string[] grid;

        /// <summary>
        /// Returns the grid code at logical position (x, y).
        /// x=0 is left column, y=0 is bottom row.
        /// </summary>
        public string GetCell(int x, int y)
        {
            int index = y * grid_width + x;
            return grid[index];
        }
    }
}
