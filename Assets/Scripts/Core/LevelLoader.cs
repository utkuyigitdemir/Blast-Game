using UnityEngine;
using BlastGame.Data;

namespace BlastGame.Core
{
    public static class LevelLoader
    {
        public static LevelData LoadLevel(int levelNumber)
        {
            string path = $"Levels/level_{levelNumber:D2}";
            TextAsset textAsset = Resources.Load<TextAsset>(path);

            if (textAsset == null)
            {
                Debug.LogError($"LevelLoader: Could not find level file at Resources/{path}");
                return null;
            }

            LevelData levelData = JsonUtility.FromJson<LevelData>(textAsset.text);

            if (levelData == null)
            {
                Debug.LogError($"LevelLoader: Failed to parse JSON for level {levelNumber}");
                return null;
            }

            Debug.Log($"LevelLoader: Loaded level {levelData.level_number} " +
                       $"({levelData.grid_width}x{levelData.grid_height}, " +
                       $"{levelData.move_count} moves, {levelData.grid.Length} cells)");

            return levelData;
        }
    }
}
