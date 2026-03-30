using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace BlastGame.UI
{
    /// <summary>
    /// Adds press-down/release scale feedback to any UI button.
    /// Attach to the button GameObject. Scales the transform on pointer down,
    /// bounces back on pointer up. Works on the entire button area including children.
    /// </summary>
    public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float pressScale = 0.9f;
        [SerializeField] private float downDuration = 0.05f;
        [SerializeField] private float upDuration = 0.1f;

        private Transform scaleTarget;

        /// <summary>
        /// Set which transform to scale. If not set, scales parent.
        /// </summary>
        public void SetScaleTarget(Transform target)
        {
            scaleTarget = target;
        }

        private Transform Target => scaleTarget != null ? scaleTarget : transform.parent;

        public void OnPointerDown(PointerEventData eventData)
        {
            Target.DOKill(true);
            Target.DOScale(pressScale, downDuration).SetEase(Ease.InQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Target.DOKill(true);
            Target.DOScale(1f, upDuration).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// Creates a transparent overlay as last child of the button.
        /// The overlay captures all pointer events and scales the button on press.
        /// </summary>
        public static void AddOverlay(Button button)
        {
            GameObject overlay = new GameObject("PressOverlay");
            overlay.transform.SetParent(button.transform);

            Image img = overlay.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;

            RectTransform rt = overlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            overlay.AddComponent<ButtonPressEffect>();
        }
    }
}
