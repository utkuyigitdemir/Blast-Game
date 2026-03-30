using UnityEngine;
using DG.Tweening;
using BlastGame.Data;

namespace BlastGame.Items
{
    public class Cube : BoardItem
    {
        public CubeColor Color { get; private set; }

        private Sprite defaultSprite;
        private Sprite rocketHintSprite;
        private bool showingRocketHint;
        private SpriteRenderer crossfadeRenderer;

        protected override void Awake()
        {
            base.Awake();
            Type = ItemType.Cube;
        }

        public override bool CanFall() => true;

        public void Initialize(CubeColor color, Sprite defaultSpr, Sprite rocketHintSpr)
        {
            Color = color;
            defaultSprite = defaultSpr;
            rocketHintSprite = rocketHintSpr;
            spriteRenderer.sprite = defaultSprite;
        }

        public void ResetForPool()
        {
            transform.DOKill(true);
            showingRocketHint = false;
            if (defaultSprite != null)
                spriteRenderer.sprite = defaultSprite;
            spriteRenderer.color = UnityEngine.Color.white;
            if (crossfadeRenderer != null)
            {
                crossfadeRenderer.DOKill();
                crossfadeRenderer.gameObject.SetActive(false);
            }
        }

        public void SetRocketHint(bool show, bool immediate = false)
        {
            if (showingRocketHint == show) return;
            showingRocketHint = show;

            Sprite targetSprite = show ? rocketHintSprite : defaultSprite;

            // Skip crossfade on initial board setup
            if (immediate)
            {
                spriteRenderer.sprite = targetSprite;
                return;
            }

            // Ensure crossfade child exists
            if (crossfadeRenderer == null)
            {
                GameObject go = new GameObject("Crossfade");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.identity;
                crossfadeRenderer = go.AddComponent<SpriteRenderer>();
                go.SetActive(false);
            }

            // Kill any running crossfade
            crossfadeRenderer.DOKill();

            // Setup crossfade: target sprite on top, starts transparent
            crossfadeRenderer.gameObject.SetActive(true);
            crossfadeRenderer.sprite = targetSprite;
            crossfadeRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            crossfadeRenderer.color = new UnityEngine.Color(1f, 1f, 1f, 0f);

            // Crossfade: overlay fades in over the current sprite
            crossfadeRenderer.DOFade(1f, 0.15f)
                .SetEase(Ease.Linear)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    // Swap main sprite, hide overlay
                    spriteRenderer.sprite = targetSprite;
                    crossfadeRenderer.gameObject.SetActive(false);
                });
        }

        public override bool TakeDamage(DamageSource source)
        {
            // Cubes are only destroyed by direct group blast or rocket pass,
            // not by blast adjacency damage (that's for obstacles only).
            if (source == DamageSource.Blast)
                return false;

            OnDestroyed();
            return true;
        }
    }
}
