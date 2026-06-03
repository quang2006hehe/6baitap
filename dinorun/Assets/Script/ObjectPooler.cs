using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolLookup; // Dùng để tìm prefab nhanh khi cần mở rộng pool

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolLookup = new Dictionary<string, Pool>();

        foreach (Pool pool in pools)
        {
            poolLookup[pool.tag] = pool;
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.transform.SetParent(this.transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    // Lấy một vật thể ra từ Pool dựa vào Tag
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool với tag " + tag + " không tồn tại!");
            return null;
        }

        Queue<GameObject> queue = poolDictionary[tag];
        GameObject objectToSpawn = null;

        // Tìm một vật thể chưa active trong queue
        int checkCount = queue.Count;
        for (int i = 0; i < checkCount; i++)
        {
            GameObject obj = queue.Dequeue();
            queue.Enqueue(obj);

            if (!obj.activeSelf)
            {
                objectToSpawn = obj;
                break;
            }
        }

        // Nếu tất cả các vật thể đều đang hoạt động, tiến hành tự động mở rộng Pool
        if (objectToSpawn == null)
        {
            if (poolLookup.ContainsKey(tag))
            {
                objectToSpawn = Instantiate(poolLookup[tag].prefab);
                objectToSpawn.transform.SetParent(this.transform);
                queue.Enqueue(objectToSpawn);
                Debug.Log("Mở rộng Pool '" + tag + "' thêm 1 phần tử mới.");
            }
            else
            {
                // Backup case: Lấy bừa phần tử đầu tiên đang chạy để dùng lại (recycle)
                objectToSpawn = queue.Dequeue();
                queue.Enqueue(objectToSpawn);
            }
        }

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        return objectToSpawn;
    }
}