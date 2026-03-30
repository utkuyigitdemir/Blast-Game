using UnityEngine;
using BlastGame.Data;

namespace BlastGame.Items
{
    public class Vase : BoardItem
    {
        private int health = 2;
        private Sprite fullSprite;
        private Sprite damagedSprite;
        private bool damagedByBlastThisTurn;

        protected override void Awake()
        {
            base.Awake();
            Type = ItemType.Vase;
        }

        public override bool CanFall() => true;

        public void Initialize(Sprite fullSpr, Sprite damagedSpr)
        {
            fullSprite = fullSpr;
            damagedSprite = damagedSpr;
            spriteRenderer.sprite = fullSprite;
        }

        public override bool TakeDamage(DamageSource source)
        {
            if (source == DamageSource.Blast)
            {
                // Max 1 blast damage per turn
                if (damagedByBlastThisTurn) return false;
                damagedByBlastThisTurn = true;
            }

            health--;

            if (health <= 0)
            {
                OnDestroyed();
                return true;
            }
            else
            {
                spriteRenderer.sprite = damagedSprite;
                return false;
            }
        }

        public void ResetTurnFlag()
        {
            damagedByBlastThisTurn = false;
        }
    }
}
