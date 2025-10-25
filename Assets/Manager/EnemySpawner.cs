using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public List<EnemyGroup> enemyGroups;
        public int waveDuration = 30; // seconds
        [HideInInspector] public float waveTimer;
    }

    [System.Serializable]
    public class EnemyGroup
    {
        public GameObject enemyPrefab;
        public int count;
        public float spawnRate = 1f;
    }

    [Header("Spawn Settings")]
    public float spawnDistanceFromCamera = 12f;
    public float minDistanceBetweenSpawns = 3f;

    [Header("Wave Settings")]
    public List<Wave> waves = new List<Wave>();
    public float timeBetweenWaves = 5f;
    public bool autoStartSpawning = true;

    [Header("Current Wave Info")]
    public int currentWaveIndex = 0;
    public int enemiesRemaining = 0;
    public bool isSpawning = false;

    public Transform player;
    private Camera mainCamera;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool allWavesComplete = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        mainCamera = Camera.main;
        
        if (player == null)
        {
            Debug.LogError("Player not found! Make sure player has 'Player' tag.");
        }

        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found!");
        }

        if (autoStartSpawning)
        {
            StartSpawning();
        }
    }

    void Update()
    {
        // Clean up destroyed enemies
        activeEnemies.RemoveAll(enemy => enemy == null);

        // Update enemies remaining count
        enemiesRemaining = activeEnemies.Count;

        // Wave timer and completion check
        if (isSpawning && !allWavesComplete && currentWaveIndex < waves.Count)
        {
            Wave currentWave = waves[currentWaveIndex];
            currentWave.waveTimer -= Time.deltaTime;

            // Check if wave is complete (time expired or all enemies defeated)
            if (currentWave.waveTimer <= 0 || (enemiesRemaining == 0 && !IsSpawningInProgress()))
            {
                CompleteWave();
            }
        }
    }

    public void StartSpawning()
    {
        if (waves.Count == 0)
        {
            Debug.LogWarning("No waves configured in EnemySpawner!");
            return;
        }

        isSpawning = true;
        currentWaveIndex = 0;
        allWavesComplete = false;
        StartCoroutine(SpawnWaves());
    }

    IEnumerator SpawnWaves()
    {
        while (currentWaveIndex < waves.Count && !allWavesComplete)
        {
            Wave currentWave = waves[currentWaveIndex];
            currentWave.waveTimer = currentWave.waveDuration;

            Debug.Log($"Starting Wave {currentWaveIndex + 1}: {currentWave.waveName}");

            // Spawn all enemy groups in this wave
            foreach (EnemyGroup group in currentWave.enemyGroups)
            {
                StartCoroutine(SpawnEnemyGroup(group));
            }

            // Wait for wave to complete
            yield return new WaitUntil(() => currentWave.waveTimer <= 0 || (enemiesRemaining == 0 && !IsSpawningInProgress()));

            // Brief pause between waves
            if (currentWaveIndex < waves.Count - 1)
            {
                Debug.Log($"Wave {currentWaveIndex + 1} complete! Next wave in {timeBetweenWaves} seconds.");
                yield return new WaitForSeconds(timeBetweenWaves);
            }

            currentWaveIndex++;
        }

        allWavesComplete = true;
        isSpawning = false;
        Debug.Log("All waves complete!");
    }

    IEnumerator SpawnEnemyGroup(EnemyGroup group)
    {
        int spawnedCount = 0;

        while (spawnedCount < group.count && waves[currentWaveIndex].waveTimer > 0)
        {
            if (player != null && mainCamera != null)
            {
                Vector2 spawnPosition = GetOffScreenSpawnPosition();
                if (spawnPosition != Vector2.zero)
                {
                    GameObject enemy = Instantiate(group.enemyPrefab, spawnPosition, Quaternion.identity);
                    activeEnemies.Add(enemy);

                    // Make enemy face towards player
                    Vector2 directionToPlayer = ((Vector2)player.position - spawnPosition).normalized;
                    float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
                    enemy.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

                    spawnedCount++;
                    Debug.Log($"Spawned {group.enemyPrefab.name} ({spawnedCount}/{group.count}) at {spawnPosition}");
                }
            }

            yield return new WaitForSeconds(1f / group.spawnRate);
        }
    }

    Vector2 GetOffScreenSpawnPosition()
    {
        if (mainCamera == null || player == null) return Vector2.zero;

        // Get camera bounds
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        
        Vector2 cameraCenter = mainCamera.transform.position;
        Vector2 playerPosition = player.position;

        // Try different spawn edges
        List<Vector2> spawnEdges = new List<Vector2>
        {
            // Left edge
            new Vector2(cameraCenter.x - cameraWidth/2 - spawnDistanceFromCamera, 
                       playerPosition.y + Random.Range(-cameraHeight/2, cameraHeight/2)),
            // Right edge
            new Vector2(cameraCenter.x + cameraWidth/2 + spawnDistanceFromCamera, 
                       playerPosition.y + Random.Range(-cameraHeight/2, cameraHeight/2)),
            // Top edge
            new Vector2(playerPosition.x + Random.Range(-cameraWidth/2, cameraWidth/2), 
                       cameraCenter.y + cameraHeight/2 + spawnDistanceFromCamera),
            // Bottom edge
            new Vector2(playerPosition.x + Random.Range(-cameraWidth/2, cameraWidth/2), 
                       cameraCenter.y - cameraHeight/2 - spawnDistanceFromCamera)
        };

        // Try each spawn position
        foreach (Vector2 spawnPos in spawnEdges)
        {
            if (IsSpawnPositionValid(spawnPos))
            {
                return spawnPos;
            }
        }

        return Vector2.zero;
    }

    bool IsSpawnPositionValid(Vector2 spawnPos)
    {
        // Check if too close to other enemies
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null && Vector2.Distance(spawnPos, enemy.transform.position) < minDistanceBetweenSpawns)
            {
                return false;
            }
        }

        // Check if position is not obstructed
        Collider2D[] colliders = Physics2D.OverlapCircleAll(spawnPos, 1f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Wall") || collider.CompareTag("Obstacle"))
            {
                return false;
            }
        }

        return true;
    }

    bool IsSpawningInProgress()
    {
        // Check if any spawn coroutines are still running
        // This is a simple implementation - you might want to track this more precisely
        return isSpawning && currentWaveIndex < waves.Count;
    }

    void CompleteWave()
    {
        Debug.Log($"Wave {currentWaveIndex + 1} completed!");
        // Wave completion handled in coroutine
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
        isSpawning = false;
    }

    public void ForceNextWave()
    {
        if (isSpawning && currentWaveIndex < waves.Count)
        {
            waves[currentWaveIndex].waveTimer = 0;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (mainCamera != null)
        {
            // Draw camera bounds
            float cameraHeight = 2f * mainCamera.orthographicSize;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            Vector2 cameraCenter = mainCamera.transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(cameraCenter, new Vector3(cameraWidth, cameraHeight, 0));

            // Draw spawn area outside camera
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(cameraCenter, new Vector3(cameraWidth + spawnDistanceFromCamera * 2, 
                                                          cameraHeight + spawnDistanceFromCamera * 2, 0));
        }
    }
}