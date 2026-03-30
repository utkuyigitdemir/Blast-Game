using UnityEngine;
using UnityEditor;
using BlastGame.Core;

namespace BlastGame.Editor
{
    public class LevelEditorMenu : EditorWindow
    {
        private int levelNumber = 1;

        [MenuItem("Tools/Set Level")]
        public static void ShowWindow()
        {
            GetWindow<LevelEditorMenu>("Set Level");
        }

        private void OnGUI()
        {
            GUILayout.Label("Set Starting Level", EditorStyles.boldLabel);
            GUILayout.Space(10);

            levelNumber = EditorGUILayout.IntField("Level Number", levelNumber);

            GUILayout.Space(10);

            if (GUILayout.Button("Set"))
            {
                PlayerPrefsManager.SetCurrentLevel(levelNumber);
                Debug.Log($"Starting level set to {levelNumber}");
            }
        }
    }
}
