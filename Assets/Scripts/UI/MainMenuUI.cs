using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BlastGame.Core;

namespace BlastGame.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button levelButton;
        [SerializeField] private TextMeshProUGUI levelButtonText;

        private RectTransform buttonRect;
        private float originalButtonY;
        private bool isTransitioning;

        private void Start()
        {
            UpdateButtonText();
            levelButton.onClick.AddListener(OnLevelButtonPressed);

            // Add press effect overlay — covers entire button, captures pointer events
            ButtonPressEffect.AddOverlay(levelButton);

            // Disable Button's built-in Color Tint (we handle visuals ourselves)
            levelButton.transition = Selectable.Transition.None;

            // Enter animation: button slides up from below screen
            buttonRect = levelButton.GetComponent<RectTransform>();
            originalButtonY = buttonRect.anchoredPosition.y;

            buttonRect.anchoredPosition = new Vector2(
                buttonRect.anchoredPosition.x,
                originalButtonY - 1500f);

            buttonRect.DOAnchorPosY(originalButtonY, 0.5f)
                .SetEase(Ease.InOutCubic);
        }

        private void UpdateButtonText()
        {
            int currentLevel = GameManager.Instance.CurrentLevel;
            int totalLevels = GameManager.Instance.GetTotalLevelCount();

            if (currentLevel > totalLevels)
            {
                levelButtonText.text = "Finished";
            }
            else
            {
                levelButtonText.text = "Level " + currentLevel;
            }
        }

        private void OnLevelButtonPressed()
        {
            if (isTransitioning) return;

            int currentLevel = GameManager.Instance.CurrentLevel;
            int totalLevels = GameManager.Instance.GetTotalLevelCount();

            if (currentLevel > totalLevels)
            {
                Debug.Log("All levels completed!");
                return;
            }

            isTransitioning = true;
            levelButton.interactable = false;
            StartCoroutine(ExitAndLoadLevel(currentLevel));
        }

        private IEnumerator ExitAndLoadLevel(int level)
        {
            // Brief wait for press effect to show
            yield return new WaitForSeconds(0.1f);

            // Button slides down off screen
            buttonRect.DOAnchorPosY(originalButtonY - 1500f, 0.4f)
                .SetEase(Ease.InOutCubic);

            yield return new WaitForSeconds(0.4f);

            GameManager.Instance.LoadLevel(level);
        }

    }
}
