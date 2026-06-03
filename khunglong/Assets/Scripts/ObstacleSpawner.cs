using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject[] cactusPrefabs;
    public float spawnTime = 1.5f;
    public float groundY = -1.5f; // Y của mặt đất (đường trắng)

    void Start()
    {
        InvokeRepeating(nameof(Spawn), 1f, spawnTime);
    }

    void Spawn()
    {
        if (cactusPrefabs == null || cactusPrefabs.Length == 0)
        {
            Debug.LogError("❌ Chưa gán cactusPrefabs!");
            return;
        }

        int index = Random.Range(0, cactusPrefabs.Length);
        GameObject prefab = cactusPrefabs[index];

        if (prefab == null)
        {
            Debug.LogError($"❌ Prefab tại index {index} bị null!");
            return;
        }

        // 👉 Lấy chiều cao sprite
        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        float spriteHeight = sr.bounds.size.y;

        // 👉 Canh đáy sprite chạm mặt đất
        float spawnY = groundY + spriteHeight / 2f;

        Vector3 spawnPos = new Vector3(transform.position.x, spawnY, 0);
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}
