using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlastGame.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public int CurrentLevel { get; private set; }

        private const int TotalLevelCount = 10;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            CurrentLevel = PlayerPrefsManager.GetCurrentLevel();
            Debug.Log($"GameManager: Initialized. Current level = {CurrentLevel}");
        }

        public void LoadLevel(int levelNumber)
        {
            CurrentLevel = levelNumber;
            PlayerPrefsManager.SetCurrentLevel(levelNumber);
            SceneManager.LoadScene("LevelScene");
        }

        public void LoadMainScene()
        {
            SceneManager.LoadScene("MainScene");
        }

        public void OnLevelWon()
        {
            CurrentLevel++;
            PlayerPrefsManager.SetCurrentLevel(CurrentLevel);
            Debug.Log($"GameManager: Level won! Next level = {CurrentLevel}");
        }

        public void OnLevelLost()
        {
            Debug.Log($"GameManager: Level {CurrentLevel} lost.");
        }

        public int GetTotalLevelCount()
        {
            return TotalLevelCount;
        }
    }
}
