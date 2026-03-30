using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BlastGame.Core;

namespace BlastGame.UI
{
    public class CelebrationManager : MonoBehaviour
    {
        [SerializeField] private ParticleSystem starParticles;
        [SerializeField] private RectTransform starImage;
        [SerializeField] private float displayDuration = 3.5f;

        private Coroutine confettiCoroutine;
        private float canvasW;
        private float canvasH;
        private GameObjectPool confettiPool;

        private static readonly Color[] confettiColors = {
            new Color(0.93f, 0.20f, 0.25f), // red
            new Color(0.20f, 0.60f, 0.93f), // blue
            new Color(0.30f, 0.85f, 0.35f), // green
            new Color(0.98f, 0.85f, 0.15f), // yellow
            new Color(1.00f, 0.50f, 0.10f), // orange
            new Color(0.85f, 0.25f, 0.80f), // pink
            new Color(0.40f, 0.90f, 0.90f), // cyan
        };

        public void Show()
        {
            gameObject.SetActive(true);

            // Cache canvas size for confetti positioning
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                canvasW = canvasRect.rect.width;
                canvasH = canvasRect.rect.height;
            }

            // Initialize confetti pool on first Show
            if (confettiPool == null)
            {
                confettiPool = new GameObjectPool(() =>
                {
                    GameObject go = new GameObject("Confetti");
                    go.AddComponent<RectTransform>();
                    Image img = go.AddComponent<Image>();
                    img.raycastTarget = false;
                    return go;
                }, transform, "ConfettiPool", 30);
            }

            if (starParticles != null)
                starParticles.Play();

            if (starImage != null)
            {
                starImage.localScale = Vector3.zero;
                starImage.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            }

            confettiCoroutine = StartCoroutine(ContinuousConfetti());
        }

        public float GetDuration()
        {
            return displayDuration;
        }

        public float Hide()
        {
            float duration = 0.3f;

            if (confettiCoroutine != null)
            {
                StopCoroutine(confettiCoroutine);
                confettiCoroutine = null;
            }

            if (starImage != null)
                starImage.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);

            if (starParticles != null)
                starParticles.Stop();

            DOVirtual.DelayedCall(duration, () => gameObject.SetActive(false)).SetLink(gameObject);
            return duration;
        }

        private IEnumerator ContinuousConfetti()
        {
            float elapsed = 0f;

            while (elapsed < displayDuration)
            {
                SpawnConfettiBatch(24);
                yield return new WaitForSeconds(0.3f);
                elapsed += 0.3f;
            }
        }

        private void SpawnConfettiBatch(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject go = confettiPool.Get();
                go.transform.SetParent(transform);

                Image img = go.GetComponent<Image>();
                img.color = confettiColors[Random.Range(0, confettiColors.Length)];

                RectTransform rt = go.GetComponent<RectTransform>();

                // Random small rectangle (confetti piece)
                float w = Random.Range(12f, 28f);
                float h = Random.Range(8f, 20f);
                rt.sizeDelta = new Vector2(w, h);

                // Start from random position above screen
                float startX = Random.Range(-canvasW * 0.45f, canvasW * 0.45f);
                float startY = canvasH * 0.55f;
                rt.anchoredPosition = new Vector2(startX, startY);
                rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                rt.localScale = Vector3.one;

                // Fall down with sway
                float endY = -canvasH * 0.55f;
                float duration = Random.Range(1.8f, 3.2f);
                float swayAmount = Random.Range(-250f, 250f);

                rt.DOAnchorPosY(endY, duration).SetEase(Ease.Linear).SetLink(go);
                rt.DOAnchorPosX(startX + swayAmount, duration)
                    .SetEase(Ease.InOutSine).SetLink(go);
                rt.DORotate(new Vector3(0f, 0f, Random.Range(-540f, 540f)),
                    duration, RotateMode.FastBeyond360).SetLink(go);
                img.DOFade(0f, duration * 0.3f).SetDelay(duration * 0.7f).SetLink(go);

                // Return to pool instead of Destroy
                float d = duration;
                GameObject captured = go;
                DOVirtual.DelayedCall(d + 0.1f, () => ReturnConfetti(captured))
                    .SetLink(go);
            }
        }

        private void ReturnConfetti(GameObject go)
        {
            if (go == null || confettiPool == null) return;
            Image img = go.GetComponent<Image>();
            if (img != null) img.color = Color.white;
            confettiPool.Return(go);
        }
    }
}
