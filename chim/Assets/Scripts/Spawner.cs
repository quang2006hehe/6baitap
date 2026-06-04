using UnityEngine;

public class Spawner : MonoBehaviour
{
    public Pipes prefab;
    public float spawnRate = 1.5f;
    public float minHeight = -1f;
    public float maxHeight = 2f;
    public float verticalGap = 3f;

    private float spawnTimer;
    private float currentSpawnRate;

    // Challenge pattern variables
    private bool isSpawningChallenge = false;
    private int challengePipesRemaining = 0;
    private float challengeSpawnInterval = 0.38f; // Interval between tunnel pipes
    private float challengeStartHeight = 0f;
    private float challengeLastHeight = 0f;
    private int challengeDirection = 1; // 1 = going up, -1 = going down
    private int challengeType = 0;      // 0 = Ladder, 1 = Wave
    private int challengeStepCount = 0;

    private int cooldownCounter = 0;
    private const int NORMAL_PIPES_COOLDOWN = 5; // Spawn at least 5 normal pipes between challenges

    private void Start()
    {
        ResetSpawner();
    }

    private void OnEnable()
    {
        ResetSpawner();
    }

    public void ResetSpawner()
    {
        spawnTimer = 0f;
        currentSpawnRate = spawnRate;
        isSpawningChallenge = false;
        challengePipesRemaining = 0;
        cooldownCounter = 0;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentSpawnRate) {
            Spawn();
            
            // Determine next spawn rate
            if (isSpawningChallenge && challengePipesRemaining > 0) {
                // If in challenge, next pipe is spawned very quickly to form a tunnel
                currentSpawnRate = challengeSpawnInterval * Random.Range(0.92f, 1.08f);
            } else {
                // Normal spawn rate, gently modified by difficulty
                float difficultyModifier = 1f;
                if (GameManager.Instance != null) {
                    difficultyModifier = GameManager.Instance.GetDifficultySpawnRateModifier();
                }
                currentSpawnRate = spawnRate * difficultyModifier * Random.Range(0.85f, 1.15f);
            }
            spawnTimer = 0f;
        }
    }

    private void Spawn()
    {
        Pipes pipes = Instantiate(prefab, transform.position, Quaternion.identity);
        float spawnY = 0f;

        if (isSpawningChallenge && challengePipesRemaining > 0) {
            // Spawn a challenge pipe (Tunnel corridor)
            if (challengeType == 0) {
                // Ladder pattern (smooth step up or step down diagonal tunnel)
                float stepHeight = 0.35f; // small step to create a smooth slope
                challengeLastHeight += challengeDirection * stepHeight;
                challengeLastHeight = Mathf.Clamp(challengeLastHeight, minHeight, maxHeight);
                spawnY = challengeLastHeight;
            } else {
                // Sine Wave tunnel (winding smoothly up and down)
                spawnY = challengeStartHeight + Mathf.Sin(challengeStepCount * 0.7f) * 1.2f;
                spawnY = Mathf.Clamp(spawnY, minHeight, maxHeight);
                challengeStepCount++;
            }

            challengePipesRemaining--;
            if (challengePipesRemaining <= 0) {
                isSpawningChallenge = false;
                cooldownCounter = NORMAL_PIPES_COOLDOWN; // trigger cooldown
            }
        } else {
            // Spawn a normal pipe
            float rangeModifier = 0f;
            if (GameManager.Instance != null) {
                rangeModifier = GameManager.Instance.GetDifficultyHeightRangeModifier();
            }
            float currentMinHeight = minHeight - rangeModifier;
            float currentMaxHeight = maxHeight + rangeModifier;
            spawnY = Random.Range(currentMinHeight, currentMaxHeight);

            // Tick down cooldown
            if (cooldownCounter > 0) {
                cooldownCounter--;
            }

            // Check if we can trigger a challenge based on actual pipes passed
            int currentPipesPassed = GameManager.Instance != null ? GameManager.Instance.pipesPassed : 0;
            if (cooldownCounter <= 0 && currentPipesPassed >= 20) {
                // 35% chance to trigger a challenge
                if (Random.value < 0.35f) {
                    StartChallenge(currentPipesPassed, spawnY);
                }
            }
        }

        pipes.transform.position += Vector3.up * spawnY;
        
        // Dynamic gap adjustment
        float gapModifier = 1f;
        if (GameManager.Instance != null) {
            gapModifier = GameManager.Instance.GetDifficultyGapModifier();
        }

        // Clamp gap modifier during tunnel challenges so it doesn't become impossible to pass
        if (isSpawningChallenge) {
            gapModifier = Mathf.Max(gapModifier, 0.85f);
        }

        pipes.gap = verticalGap * gapModifier;
    }

    private void StartChallenge(int pipesPassed, float startingHeight)
    {
        isSpawningChallenge = true;
        challengeStartHeight = startingHeight;
        challengeLastHeight = startingHeight;
        challengeStepCount = 0;

        if (pipesPassed >= 100) {
            // Wave Tunnel: 6 to 9 pipes forming a long winding cave
            challengeType = 1;
            challengePipesRemaining = Random.Range(6, 10);
            challengeSpawnInterval = 0.35f; // very close together
        } else if (pipesPassed >= 40) {
            // Ladder Tunnel: 3 to 4 pipes stepping up/down
            challengeType = 0;
            challengePipesRemaining = Random.Range(3, 5);
            challengeDirection = Random.value < 0.5f ? 1 : -1;
            challengeSpawnInterval = 0.38f;
        } else if (pipesPassed >= 20) {
            // Ladder Tunnel: 3 pipes stepping up/down
            challengeType = 0;
            challengePipesRemaining = 3;
            challengeDirection = Random.value < 0.5f ? 1 : -1;
            challengeSpawnInterval = 0.42f;
        }
    }

}

