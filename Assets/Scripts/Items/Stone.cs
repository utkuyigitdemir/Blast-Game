using UnityEngine;
using BlastGame.Data;

namespace BlastGame.Items
{
    public class Stone : BoardItem
    {
        private int health = 1;

        protected override void Awake()
        {
            base.Awake();
            Type = ItemType.Stone;
        }

        public override bool CanFall() => false;

        public void Initialize(Sprite sprite)
        {
            spriteRenderer.sprite = sprite;
        }

        public override bool TakeDamage(DamageSource source)
        {
            // Stone is damaged ONLY by rockets
            if (source != DamageSource.Rocket) return false;

            health--;
            if (health <= 0)
            {
                OnDestroyed();
                return true;
            }
            return false;
        }
    }
}
