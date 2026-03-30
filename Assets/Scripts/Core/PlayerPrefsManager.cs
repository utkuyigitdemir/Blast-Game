using UnityEngine;

namespace BlastGame.Core
{
    public static class PlayerPrefsManager
    {
        private const string CurrentLevelKey = "CurrentLevel";
        private const int DefaultLevel = 1;

        public static int GetCurrentLevel()
        {
            return PlayerPrefs.GetInt(CurrentLevelKey, DefaultLevel);
        }

        public static void SetCurrentLevel(int level)
        {
            PlayerPrefs.SetInt(CurrentLevelKey, level);
            PlayerPrefs.Save();
        }
    }
}
