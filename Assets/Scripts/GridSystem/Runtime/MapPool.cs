using System.Collections.Generic;
using UnityEngine;

namespace GridSystem.Runtime
{
    public class MapPool
    {
        private GameObject prefab;
        private Transform parent;
        private Queue<GameObject> pool = new Queue<GameObject>();
        private List<GameObject> active = new List<GameObject>();

        public MapPool(GameObject prefab, Transform parent, int initialSize = 10)
        {
            this.prefab = prefab;
            this.parent = parent;

            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        private GameObject CreateNewObject()
        {
            var obj = Object.Instantiate(prefab, parent);
            obj.SetActive(false);
            pool.Enqueue(obj);
            return obj;
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            if (pool.Count == 0)
            {
                CreateNewObject();
            }

            var obj = pool.Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            active.Add(obj);
            return obj;
        }

        public void Return(GameObject obj)
        {
            obj.SetActive(false);
            active.Remove(obj);
            pool.Enqueue(obj);
        }

        public void ReturnAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Return(active[i]);
            }
        }
    }
}