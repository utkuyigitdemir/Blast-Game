using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BlastGame.Core;
using BlastGame.Data;

namespace BlastGame.Board
{
    public class VFXManager : MonoBehaviour
    {
        [Header("Cube Particles")]
        [SerializeField] private Sprite particleRed;
        [SerializeField] private Sprite particleBlue;
        [SerializeField] private Sprite particleGreen;
        [SerializeField] private Sprite particleYellow;

        [Header("Rocket Particles")]
        [SerializeField] private Sprite particleSmoke;
        [SerializeField] private Sprite particleStar;

        [Header("Box Particles")]
        [SerializeField] private Sprite particleBox01;
        [SerializeField] private Sprite particleBox02;
        [SerializeField] private Sprite particleBox03;

        [Header("Stone Particles")]
        [SerializeField] private Sprite particleStone01;
        [SerializeField] private Sprite particleStone02;
        [SerializeField] private Sprite particleStone03;

        [Header("Vase Particles")]
        [SerializeField] private Sprite particleVase01;
        [SerializeField] private Sprite particleVase02;
        [SerializeField] private Sprite particleVase03;

        [Header("Celebration")]
        [SerializeField] private Sprite additiveParticleStar;

        private float cellSize;
        private Transform parentTransform;
        private Image darkOverlayImage;
        private GameObjectPool particlePool;
        private GameObjectPool firePool;

        private void OnDestroy()
        {
            // Kill only tweens parented to this manager, not ALL tweens globally
            DOTween.Kill(transform);
            if (darkOverlayImage != null)
                DOTween.Kill(darkOverlayImage);
        }

        public void Initialize(float cellSize, Transform parent)
        {
            this.cellSize = cellSize;
            this.parentTransform = parent;

            DOTween.SetTweensCapacity(3000, 1000);

            // Initialize VFX pools
            particlePool = new GameObjectPool(() =>
            {
                GameObject go = new GameObject("VFX_Particle");
                go.AddComponent<SpriteRenderer>();
                return go;
            }, transform, "ParticlePool", 10);

            firePool = new GameObjectPool(() =>
            {
                GameObject go = new GameObject("VFX_Fire");
                go.AddComponent<SpriteRenderer>();
                return go;
            }, transform, "FirePool", 5);
        }

        // ───────────────────────── Public Effects ─────────────────────────

        public void PlayCubeBlast(Vector3 position, CubeColor color, bool isBigGroup)
        {
            Sprite sprite = GetCubeParticle(color);
            if (sprite == null) return;

            Sprite[] sprites = { sprite };

            if (isBigGroup)
                SpawnBurst(position, sprites, 12, 2.0f, 0.5f, 0.6f, 1.1f);
            else
                SpawnBurst(position, sprites, 6, 1.5f, 0.45f, 0.5f, 0.9f);
        }

        public void PlayObstacleBreak(Vector3 position, ItemType type)
        {
            Sprite[] sprites = GetObstacleParticles(type);
            if (sprites == null || sprites.Length == 0) return;

            SpawnBurst(position, sprites, 8, 1.6f, 0.5f, 0.5f, 1.0f);
        }

        public void PlayDamageShake(Transform target)
        {
            if (target == null) return;
            target.DOShakePosition(0.25f, cellSize * 0.12f, 20, 90f)
                .SetLink(target.gameObject);
        }

        public void PlayRocketTrail(Vector3 position)
        {
            // 2 smoke + 1 spark per cell
            if (particleSmoke != null)
            {
                for (int i = 0; i < 2; i++)
                    SpawnFireParticle(position, particleSmoke, 0.6f, 0.8f, 0.5f, 0.9f);
            }
            if (particleStar != null)
                SpawnFireParticle(position, particleStar, 0.4f, 0.5f, 0.2f, 0.4f);
        }

        /// <summary>
        /// Big explosion burst at the rocket's origin when it detonates.
        /// </summary>
        public void PlayRocketExplosion(Vector3 position)
        {
            // Massive fire → smoke burst
            if (particleSmoke != null)
            {
                for (int i = 0; i < 10; i++)
                    SpawnFireParticle(position, particleSmoke, 1.8f, 0.9f, 0.7f, 1.3f);
            }
            if (particleStar != null)
            {
                for (int i = 0; i < 6; i++)
                    SpawnFireParticle(position, particleStar, 1.2f, 0.7f, 0.35f, 0.7f);
            }
        }

        public void PlayRocketCreationPop(Transform rocketTransform)
        {
            if (rocketTransform == null) return;

            // Scale pop: 0 → normal with OutBack
            Vector3 normalScale = rocketTransform.localScale;
            rocketTransform.localScale = Vector3.zero;
            rocketTransform.DOScale(normalScale, 0.25f).SetEase(Ease.OutBack);

            // Star particles around the rocket
            Sprite[] sprites = GetValidSprites(particleStar);
            if (sprites.Length > 0)
                SpawnBurst(rocketTransform.position, sprites, 4, 0.8f, 0.3f, 0.3f, 0.5f);
        }

        public float PlayTapPunch(Transform target)
        {
            if (target == null) return 0f;
            target.DOPunchScale(Vector3.one * 0.15f, 0.12f, 8, 0.5f);
            return 0.12f;
        }

        // ───────────────────────── Dark Overlay ─────────────────────────

        /// <summary>
        /// UI Image overlay inside the Canvas. Renders above TopBar/HUD,
        /// below FailPopup/CelebrationManager. Covers everything including UI.
        /// </summary>
        public void ShowDarkOverlay(float targetAlpha, float duration)
        {
            CreateDarkOverlayIfNeeded();
            if (darkOverlayImage == null) return;

            darkOverlayImage.gameObject.SetActive(true);
            darkOverlayImage.color = new Color(0f, 0f, 0f, 0f);
            darkOverlayImage.DOFade(targetAlpha, duration)
                .SetLink(darkOverlayImage.gameObject);
        }

        public void HideDarkOverlay(float duration)
        {
            if (darkOverlayImage == null) return;
            darkOverlayImage.DOFade(0f, duration)
                .SetLink(darkOverlayImage.gameObject)
                .OnComplete(() => darkOverlayImage.gameObject.SetActive(false));
        }

        private void CreateDarkOverlayIfNeeded()
        {
            if (darkOverlayImage != null) return;

            // Find canvas in scene
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject go = new GameObject("DarkOverlay");
            go.transform.SetParent(canvas.transform);

            darkOverlayImage = go.AddComponent<Image>();
            darkOverlayImage.color = new Color(0f, 0f, 0f, 0f);
            darkOverlayImage.raycastTarget = false;

            // Stretch to fill entire canvas
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Ensure FailPopup and CelebrationManager render ABOVE the overlay
            Transform failT = canvas.transform.Find("FailPopup");
            Transform celebT = canvas.transform.Find("CelebrationManager");
            if (failT != null) failT.SetAsLastSibling();
            if (celebT != null) celebT.SetAsLastSibling();

            go.SetActive(false);
        }

        // ───────────────────────── Celebration ─────────────────────────

        /// <summary>
        /// Board-level celebration: colorful star bursts across the board.
        /// </summary>
        public void PlayCelebration(Vector3 boardCenter, float boardWidth, float boardHeight)
        {
            // Colorful confetti: use cube particle sprites (red, blue, green, yellow) + stars
            Sprite[] confetti = GetValidSprites(
                particleRed, particleBlue, particleGreen, particleYellow);
            Sprite[] stars = GetValidSprites(particleStar);

            // Colored confetti bursts from random positions
            for (int burst = 0; burst < 10; burst++)
            {
                Vector3 pos = boardCenter + new Vector3(
                    Random.Range(-boardWidth * 0.5f, boardWidth * 0.5f),
                    Random.Range(-boardHeight * 0.5f, boardHeight * 0.5f),
                    0f);

                if (confetti.Length > 0)
                    SpawnBurst(pos, confetti, 4, 2.0f, 1.2f, 0.3f, 0.6f);
            }

            // Smaller white/gold star sparkles
            if (stars.Length > 0)
            {
                Color[] sparkleColors = {
                    new Color(1f, 0.9f, 0.2f),  // gold
                    new Color(1f, 1f, 0.7f),     // light yellow
                    Color.white
                };

                for (int burst = 0; burst < 8; burst++)
                {
                    Vector3 pos = boardCenter + new Vector3(
                        Random.Range(-boardWidth * 0.5f, boardWidth * 0.5f),
                        Random.Range(-boardHeight * 0.5f, boardHeight * 0.5f),
                        0f);

                    Color color = sparkleColors[burst % sparkleColors.Length];
                    SpawnBurst(pos, stars, 3, 1.5f, 1.0f, 0.2f, 0.4f, color);
                }
            }
        }

        // ───────────────────────── Core Helper ─────────────────────────

        private void SpawnBurst(Vector3 center, Sprite[] sprites, int count,
            float spread, float duration, float scaleMin, float scaleMax,
            Color? tint = null)
        {
            for (int i = 0; i < count; i++)
            {
                Sprite sprite = sprites[Random.Range(0, sprites.Length)];
                if (sprite == null) continue;

                GameObject go = particlePool.Get();
                go.transform.position = center;
                if (parentTransform != null)
                    go.transform.SetParent(parentTransform);

                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 200;
                sr.color = tint ?? Color.white;

                // Scale relative to cellSize, normalized by sprite size
                float spriteWidth = sr.sprite.bounds.size.x;
                float desiredSize = cellSize * Random.Range(scaleMin, scaleMax);
                float scale = desiredSize / spriteWidth;
                go.transform.localScale = new Vector3(scale, scale, 1f);

                // Random rotation
                go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                // Random direction outward
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = spread * cellSize * Random.Range(0.5f, 1f);
                Vector3 target = center + new Vector3(
                    Mathf.Cos(angle) * dist,
                    Mathf.Sin(angle) * dist,
                    0f
                );

                // Animate: move outward + fade returns to pool on complete
                go.transform.DOMove(target, duration).SetEase(Ease.OutQuad).SetLink(go);
                GameObject captured = go;
                sr.DOFade(0f, duration).SetEase(Ease.InQuad).SetLink(go)
                    .OnComplete(() => ReturnParticle(captured));
            }
        }

        private void ReturnParticle(GameObject go)
        {
            if (go == null || particlePool == null) return;
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) { sr.sprite = null; sr.color = Color.white; }
            particlePool.Return(go);
        }

        /// <summary>
        /// Spawns a single particle that transitions: bright yellow fire → dark orange → gray smoke → fade out.
        /// Also grows slightly to simulate smoke expanding.
        /// </summary>
        private void SpawnFireParticle(Vector3 center, Sprite sprite,
            float spread, float duration, float scaleMin, float scaleMax)
        {
            if (sprite == null) return;

            GameObject go = firePool.Get();
            go.transform.position = center;
            if (parentTransform != null)
                go.transform.SetParent(parentTransform);

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 200;

            // Start as bright fire
            Color fireStart = new Color(1f, 0.85f, 0.15f, 1f);
            Color smokeMid = new Color(0.7f, 0.35f, 0.08f, 0.85f);
            Color smokeEnd = new Color(0.35f, 0.35f, 0.35f, 0f);
            sr.color = fireStart;

            // Scale normalized by sprite size
            float spriteWidth = sr.sprite.bounds.size.x;
            float desiredSize = cellSize * Random.Range(scaleMin, scaleMax);
            float scale = desiredSize / spriteWidth;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            // Random direction outward
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = spread * cellSize * Random.Range(0.5f, 1f);
            Vector3 target = center + new Vector3(
                Mathf.Cos(angle) * dist,
                Mathf.Sin(angle) * dist,
                0f
            );

            // Move outward + grow (smoke expands)
            go.transform.DOMove(target, duration).SetEase(Ease.OutQuad).SetLink(go);
            go.transform.DOScale(go.transform.localScale * 1.6f, duration).SetEase(Ease.OutQuad).SetLink(go);

            // Color sequence: fire → smoke → fade, returns to pool on complete
            GameObject captured = go;
            Sequence colorSeq = DOTween.Sequence().SetLink(go);
            colorSeq.Append(sr.DOColor(smokeMid, duration * 0.35f).SetEase(Ease.Linear));
            colorSeq.Append(sr.DOColor(smokeEnd, duration * 0.65f).SetEase(Ease.InQuad));
            colorSeq.OnComplete(() => ReturnFire(captured));
        }

        private void ReturnFire(GameObject go)
        {
            if (go == null || firePool == null) return;
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) { sr.sprite = null; sr.color = Color.white; }
            firePool.Return(go);
        }

        // ───────────────────────── Sprite Helpers ─────────────────────────

        private Sprite GetCubeParticle(CubeColor color)
        {
            switch (color)
            {
                case CubeColor.Red: return particleRed;
                case CubeColor.Blue: return particleBlue;
                case CubeColor.Green: return particleGreen;
                case CubeColor.Yellow: return particleYellow;
                default: return particleRed;
            }
        }

        private Sprite[] GetObstacleParticles(ItemType type)
        {
            switch (type)
            {
                case ItemType.Box:
                    return GetValidSprites(particleBox01, particleBox02, particleBox03);
                case ItemType.Stone:
                    return GetValidSprites(particleStone01, particleStone02, particleStone03);
                case ItemType.Vase:
                    return GetValidSprites(particleVase01, particleVase02, particleVase03);
                default:
                    return null;
            }
        }

        private Sprite[] GetValidSprites(params Sprite[] candidates)
        {
            int count = 0;
            foreach (var s in candidates)
                if (s != null) count++;

            Sprite[] result = new Sprite[count];
            int idx = 0;
            foreach (var s in candidates)
                if (s != null) result[idx++] = s;

            return result;
        }
    }
}
