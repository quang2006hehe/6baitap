using UnityEngine;

public class PipeSpawner : MonoBehaviour
{
    public GameObject pipePrefab; // Ô này để kéo Prefab ống vào
    public float spawnRate = 2f;  // Cứ 2 giây tạo 1 ống
    private float timer = 0f;
    public float heightOffset = 2.5f; // Độ cao ngẫu nhiên lên xuống

    void Start()
    {
        spawnPipe(); // Tạo 1 ống ngay khi bắt đầu
    }

    void Update()
    {
        if (timer < spawnRate)
        {
            timer += Time.deltaTime;
        }
        else
        {
            spawnPipe();
            timer = 0;
        }
    }

    void spawnPipe()
    {
        float lowestPoint = transform.position.y - heightOffset;
        float highestPoint = transform.position.y + heightOffset;

        // Tạo ống tại vị trí X của Spawner, nhưng Y ngẫu nhiên
        Instantiate(pipePrefab, new Vector3(transform.position.x, Random.Range(lowestPoint, highestPoint), 0), transform.rotation);
    }
}