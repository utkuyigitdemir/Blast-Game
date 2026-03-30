using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace BlastGame.Core
{
    /// <summary>
    /// Lightweight GameObject pool. Avoids GC allocations from Instantiate/Destroy cycles.
    /// Uses a factory delegate so it works with both prefabs and runtime-created objects.
    /// </summary>
    public class GameObjectPool
    {
        private readonly Queue<GameObject> pool = new Queue<GameObject>();
        private readonly System.Func<GameObject> createFunc;
        private readonly Transform container;

        public GameObjectPool(System.Func<GameObject> createFunc, Transform parent,
            string name = "Pool", int prewarmCount = 0)
        {
            this.createFunc = createFunc;

            container = new GameObject(name).transform;
            container.SetParent(parent);

            for (int i = 0; i < prewarmCount; i++)
            {
                GameObject go = createFunc();
                go.transform.SetParent(container);
                go.SetActive(false);
                pool.Enqueue(go);
            }
        }

        /// <summary>
        /// Get an object from the pool. Creates a new one if pool is empty.
        /// </summary>
        public GameObject Get()
        {
            GameObject go;

            if (pool.Count > 0)
            {
                go = pool.Dequeue();
                if (go == null)
                    go = createFunc();
            }
            else
            {
                go = createFunc();
            }

            go.SetActive(true);
            return go;
        }

        /// <summary>
        /// Return an object to the pool. Kills tweens, resets transform, deactivates.
        /// </summary>
        public void Return(GameObject go)
        {
            if (go == null) return;

            DOTween.Kill(go.transform);
            go.transform.SetParent(container);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            go.transform.rotation = Quaternion.identity;
            go.SetActive(false);

            pool.Enqueue(go);
        }

        public int CountInactive => pool.Count;
    }
}
