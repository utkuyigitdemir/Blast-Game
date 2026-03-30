using UnityEngine;
using BlastGame.Data;

namespace BlastGame.Items
{
    public class Box : BoardItem
    {
        private int health = 1;

        protected override void Awake()
        {
            base.Awake();
            Type = ItemType.Box;
        }

        public override bool CanFall() => false;

        public void Initialize(Sprite sprite)
        {
            spriteRenderer.sprite = sprite;
        }

        public override bool TakeDamage(DamageSource source)
        {
            // Box is damaged by blast adjacency and rockets
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
