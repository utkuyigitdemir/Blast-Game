using UnityEngine;
using BlastGame.Data;

namespace BlastGame.Items
{
    public abstract class BoardItem : MonoBehaviour
    {
        public ItemType Type { get; protected set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        [SerializeField] protected SpriteRenderer spriteRenderer;
        public SpriteRenderer SpriteRenderer => spriteRenderer;

        protected virtual void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetGridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public virtual bool CanFall() => false;

        public virtual bool TakeDamage(DamageSource source) { return false; }

        private System.Action<GameObject> returnToPool;

        public void SetPoolReturnAction(System.Action<GameObject> action)
        {
            returnToPool = action;
        }

        public virtual void OnDestroyed()
        {
            if (returnToPool != null)
                returnToPool(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
