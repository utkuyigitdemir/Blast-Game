using UnityEngine;
using BlastGame.Data;

namespace BlastGame.Items
{
    public class Rocket : BoardItem
    {
        public RocketDirection Direction { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Type = ItemType.Rocket;
        }

        public override bool CanFall() => true;

        public void Initialize(RocketDirection direction, Sprite sprite)
        {
            Direction = direction;
            spriteRenderer.sprite = sprite;
        }

        public override bool TakeDamage(DamageSource source)
        {
            // Rockets are immune to all TakeDamage calls.
            // Blast adjacency: no effect.
            // Rocket hit: chain explosion is handled externally by RocketExplosionSystem.
            return false;
        }
    }
}
