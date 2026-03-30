using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BlastGame.Core;
using BlastGame.Board;

namespace BlastGame.UI
{
    public class FailPopup : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button tryAgainButton;
        [SerializeField] private RectTransform popupPanel;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI failText;
        [SerializeField] private BoardManager boardManager;

        private bool listenersAdded;

        public void Show()
        {
            gameObject.SetActive(true);

            if (levelText != null && GameManager.Instance != null)
                levelText.text = "Level " + GameManager.Instance.CurrentLevel;

            if (!listenersAdded)
            {
                closeButton.onClick.AddListener(OnClosePressed);
                tryAgainButton.onClick.AddListener(OnTryAgainPressed);
                listenersAdded = true;

                // Press effect overlays on buttons
                ButtonPressEffect.AddOverlay(tryAgainButton);
                ButtonPressEffect.AddOverlay(closeButton);

                // Disable built-in Color Tint
                tryAgainButton.transition = Selectable.Transition.None;
                closeButton.transition = Selectable.Transition.None;
            }

            if (popupPanel != null)
            {
                popupPanel.localScale = Vector3.zero;
                popupPanel.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }
        }

        private void OnClosePressed()
        {
            closeButton.interactable = false;
            tryAgainButton.interactable = false;
            StartCoroutine(ExitAfterDelay(true));
        }

        private void OnTryAgainPressed()
        {
            closeButton.interactable = false;
            tryAgainButton.interactable = false;
            StartCoroutine(ExitAfterDelay(false));
        }

        private IEnumerator ExitAfterDelay(bool loadMainScene)
        {
            // Wait for press effect to show
            yield return new WaitForSeconds(0.1f);
            yield return ExitAndLoad(loadMainScene);
        }

        private IEnumerator ExitAndLoad(bool loadMainScene)
        {
            if (popupPanel != null)
                popupPanel.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);

            yield return new WaitForSeconds(0.2f);
            gameObject.SetActive(false);

            if (boardManager != null)
            {
                if (loadMainScene)
                    boardManager.PlayExitAndLoadMainScene();
                else
                    boardManager.PlayExitAndReloadLevel();
            }
            else
            {
                if (loadMainScene)
                    GameManager.Instance.LoadMainScene();
                else
                    GameManager.Instance.LoadLevel(GameManager.Instance.CurrentLevel);
            }
        }

    }
}
