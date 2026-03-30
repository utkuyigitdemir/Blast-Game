using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlastGame.Data;

namespace BlastGame.UI
{
    public class GameplayHUD : MonoBehaviour
    {
        [Header("Moves")]
        [SerializeField] private TextMeshProUGUI movesText;

        [Header("Goals")]
        [SerializeField] private Transform goalsContainer;
        [SerializeField] private GoalItemUI goalItemPrefab;

        [Header("Obstacle Sprites")]
        [SerializeField] private Sprite boxSprite;
        [SerializeField] private Sprite stoneSprite;
        [SerializeField] private Sprite vaseSprite;

        // Layout constants (dynamic — depends on goal count)
        private const float ICON_SIZE_1 = 120f;
        private const float ICON_SIZE_MULTI = 80f;
        private const float H_GAP = 16f;
        private const float V_GAP = 8f;
        private const float COUNT_FONT_1 = 72f;
        private const float COUNT_FONT_MULTI = 54f;
        private const float CHECK_RATIO = 0.4f;

        private Dictionary<ItemType, GoalItemUI> goalItems = new Dictionary<ItemType, GoalItemUI>();

        public void Initialize(int totalMoves, Dictionary<ItemType, int> goals)
        {
            UpdateMoves(totalMoves);

            // Clear existing
            foreach (Transform child in goalsContainer)
            {
                Destroy(child.gameObject);
            }
            goalItems.Clear();

            // Collect active goals
            List<KeyValuePair<ItemType, int>> activeGoals = new List<KeyValuePair<ItemType, int>>();
            foreach (var kvp in goals)
            {
                if (kvp.Value > 0) activeGoals.Add(kvp);
            }
            int goalCount = activeGoals.Count;
            if (goalCount == 0) return;

            float iconSize = goalCount == 1 ? ICON_SIZE_1 : ICON_SIZE_MULTI;
            float fontSize = goalCount == 1 ? COUNT_FONT_1 : COUNT_FONT_MULTI;

            // Calculate positions based on goal count
            List<Vector2> positions = new List<Vector2>();
            float containerW, containerH;

            if (goalCount == 1)
            {
                containerW = iconSize;
                containerH = iconSize;
                positions.Add(Vector2.zero);
            }
            else if (goalCount == 2)
            {
                containerW = iconSize * 2 + H_GAP;
                containerH = iconSize;
                float startX = -(iconSize + H_GAP) / 2f;
                positions.Add(new Vector2(startX, 0f));
                positions.Add(new Vector2(startX + iconSize + H_GAP, 0f));
            }
            else // 3 goals: 2 top + 1 centered bottom
            {
                containerW = iconSize * 2 + H_GAP;
                containerH = iconSize * 2 + V_GAP;
                float topY = (iconSize + V_GAP) / 2f;
                float botY = -(iconSize + V_GAP) / 2f;
                float startX = -(iconSize + H_GAP) / 2f;
                positions.Add(new Vector2(startX, topY));
                positions.Add(new Vector2(startX + iconSize + H_GAP, topY));
                positions.Add(new Vector2(0f, botY));
            }

            // Resize container
            var containerRect = goalsContainer.GetComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(containerW, containerH);

            // Instantiate goal items from prefab
            for (int i = 0; i < goalCount; i++)
            {
                GoalItemUI item = Instantiate(goalItemPrefab, goalsContainer);
                item.gameObject.SetActive(true);

                // Dynamic layout: size and position
                var rt = item.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(iconSize, iconSize);
                rt.anchoredPosition = positions[i];

                // Dynamic sizing: font size varies by goal count
                item.CountText.fontSize = fontSize;

                // Dynamic sizing: check icon scales with icon size
                var checkRt = item.CheckImage.GetComponent<RectTransform>();
                float checkSize = iconSize * CHECK_RATIO;
                checkRt.sizeDelta = new Vector2(checkSize, checkSize);

                // Data binding: sprite and count
                item.SetIcon(GetObstacleSprite(activeGoals[i].Key));
                item.SetCount(activeGoals[i].Value);

                goalItems[activeGoals[i].Key] = item;
            }
        }

        public void UpdateMoves(int remaining)
        {
            movesText.text = remaining.ToString();
        }

        public void UpdateGoals(Dictionary<ItemType, int> remaining)
        {
            foreach (var kvp in goalItems)
            {
                ItemType type = kvp.Key;
                GoalItemUI ui = kvp.Value;

                if (remaining.ContainsKey(type) && remaining[type] > 0)
                {
                    ui.ShowCount(remaining[type]);
                }
                else
                {
                    ui.ShowCompleted();
                }
            }
        }

        private Sprite GetObstacleSprite(ItemType type)
        {
            switch (type)
            {
                case ItemType.Box: return boxSprite;
                case ItemType.Stone: return stoneSprite;
                case ItemType.Vase: return vaseSprite;
                default: return null;
            }
        }
    }
}
