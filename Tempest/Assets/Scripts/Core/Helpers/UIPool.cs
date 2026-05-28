using System.Collections.Generic;
using UnityEngine;

namespace Tempest.Core.Helpers
{
    public class UIPool<T> where T : Component
    {
        private readonly Queue<T> q = new();
        private readonly T prefab;
        private readonly Transform parent;

        public UIPool(T prefab, Transform parent, int prewarm = 12)
        {
            this.prefab = prefab;
            this.parent = parent;
            for (int i = 0; i < prewarm; i++)
                q.Enqueue(Object.Instantiate(prefab, parent));
            foreach (var t in q)
                t.gameObject.SetActive(false);
        }

        public T Get() => q.Count > 0 ? Activate(q.Dequeue()) : Activate(Object.Instantiate(prefab, parent));
        public void Release(T t) { t.gameObject.SetActive(false); q.Enqueue(t); }

        private T Activate(T t) { t.transform.SetAsLastSibling(); t.gameObject.SetActive(true); return t; }
    }
}
