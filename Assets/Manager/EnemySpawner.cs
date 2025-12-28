using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyGroup
    {
        public GameObject enemyPrefab;
        public int baseCount;
        public float baseSpawnRate = 1f;
        public float quantityMultiplier = 1f; // How much this enemy's quantity scales with time
    }

    [Header("Spawn Settings")]
    public float spawnDistanceFromCamera = 12f;
    public float minDistanceBetweenSpawns = 3f;

    [Header("Wave Settings")]
    public List<EnemyGroup> enemyTypes = new List<EnemyGroup>();
    public float timeBetweenWaves = 5f;
    public bool autoStartSpawning = true;

    [Header("Quantity Scaling Settings")]
    public float quantityIncreaseRate = 0.1f; // How much quantity increases per minute
    public float maxQuantityMultiplier = 5f; // Maximum enemies multiplier
    public int baseEnemiesPerWave = 5;
    public float enemiesPerWaveIncrease = 0.3f; // Additional enemies per wave
    public int maxEnemiesPerWave = 100; // Cap to prevent performance issues

    [Header("Current Wave Info")]
    public int currentWaveNumber = 0;
    public int enemiesRemaining = 0;
    public bool isSpawning = false;
    public float currentQuantityMultiplier = 1f;

    public Transform player;
    private Camera mainCamera;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private float gameTime = 0f;
    private Coroutine currentSpawnCoroutine;
    
    // Wave structure for endless mode
    [System.Serializable]
    private class EndlessWave
    {
        public List<EnemySpawnInfo> enemies = new List<EnemySpawnInfo>();
        public float waveDuration = 30f;
        
        [System.Serializable]
        public class EnemySpawnInfo
        {
            public GameObject prefab;
            public int count;
            public float spawnRate;
        }
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        mainCamera = Camera.main;
        
        if (player == null)
            Debug.LogError("Player not found! Make sure player has 'Player' tag.");
        
        if (mainCamera == null)
            Debug.LogError("Main camera not found!");

        if (autoStartSpawning)
            StartSpawning();
    }

    void Update()
    {
        // Clean up destroyed enemies
        activeEnemies.RemoveAll(enemy => enemy == null);
        enemiesRemaining = activeEnemies.Count;
        
        // Update game time for quantity progression
        if (isSpawning)
        {
            gameTime += Time.deltaTime;
            UpdateQuantityMultiplier();
        }
    }

    public void StartSpawning()
    {
        if (enemyTypes.Count == 0)
        {
            Debug.LogWarning("No enemy types configured in EnemySpawner!");
            return;
        }

        isSpawning = true;
        currentWaveNumber = 0;
        currentQuantityMultiplier = 1f;
        gameTime = 0f;
        
        if (currentSpawnCoroutine != null)
            StopCoroutine(currentSpawnCoroutine);
        
        currentSpawnCoroutine = StartCoroutine(SpawnEndlessWaves());
    }

    IEnumerator SpawnEndlessWaves()
    {
        while (isSpawning)
        {
            currentWaveNumber++;
            
            // Generate wave based on current quantity multiplier
            EndlessWave wave = GenerateWave(currentWaveNumber, currentQuantityMultiplier);
            
            Debug.Log($"Starting Wave {currentWaveNumber}");
            Debug.Log($"Quantity Multiplier: {currentQuantityMultiplier:F2}");
            Debug.Log($"Total enemies this wave: {GetTotalEnemiesInWave(wave)}");
            
            float waveTimer = wave.waveDuration;
            int totalToSpawn = GetTotalEnemiesInWave(wave);
            
            // Spawn all enemy groups in this wave
            foreach (EndlessWave.EnemySpawnInfo spawnInfo in wave.enemies)
            {
                StartCoroutine(SpawnEnemyGroup(spawnInfo));
            }
            
            // Wave timer
            while (waveTimer > 0 && enemiesRemaining > 0)
            {
                waveTimer -= Time.deltaTime;
                yield return null;
            }
            
            // Wait for all enemies to be defeated before next wave
            if (enemiesRemaining > 0)
            {
                Debug.Log($"Waiting for {enemiesRemaining} remaining enemies...");
                yield return new WaitUntil(() => enemiesRemaining == 0);
            }
            
            // Brief pause between waves
            if (isSpawning)
            {
                Debug.Log($"Wave {currentWaveNumber} complete! Next wave in {timeBetweenWaves} seconds.");
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }
    }
    
    IEnumerator SpawnEnemyGroup(EndlessWave.EnemySpawnInfo spawnInfo)
    {
        int spawnedCount = 0;
        
        while (spawnedCount < spawnInfo.count && isSpawning)
        {
            if (player != null && mainCamera != null)
            {
                Vector2 spawnPosition = GetOffScreenSpawnPosition();
                if (spawnPosition != Vector2.zero)
                {
                    GameObject enemy = Instantiate(spawnInfo.prefab, spawnPosition, Quaternion.identity);
                    activeEnemies.Add(enemy);
                    
                    // Make enemy face towards player
                    Vector2 directionToPlayer = ((Vector2)player.position - spawnPosition).normalized;
                    float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
                    enemy.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                    
                    spawnedCount++;
                }
            }
            
            float spawnDelay = 1f / spawnInfo.spawnRate;
            yield return new WaitForSeconds(spawnDelay);
        }
    }
    
    EndlessWave GenerateWave(int waveNumber, float quantityMultiplier)
    {
        EndlessWave wave = new EndlessWave();
        
        // Keep wave duration consistent
        wave.waveDuration = 30f;
        
        // Calculate total enemies for this wave
        int totalEnemies = CalculateEnemiesForWave(waveNumber, quantityMultiplier);
        
        // Distribute enemies among available types
        List<EnemyGroup> availableTypes = new List<EnemyGroup>(enemyTypes);
        
        if (availableTypes.Count == 0) return wave;
        
        // Simple distribution: Divide enemies evenly among types
        int enemiesPerType = Mathf.Max(1, totalEnemies / availableTypes.Count);
        int remainder = totalEnemies % availableTypes.Count;
        
        for (int i = 0; i < availableTypes.Count; i++)
        {
            EnemyGroup type = availableTypes[i];
            
            // Calculate count for this type with multiplier
            int countForType = enemiesPerType;
            if (i == 0) countForType += remainder; // Add remainder to first type
            
            // Apply type-specific quantity multiplier
            float typeMultiplier = type.quantityMultiplier;
            countForType = Mathf.RoundToInt(countForType * typeMultiplier * quantityMultiplier);
            
            // Ensure at least 1 enemy of this type
            countForType = Mathf.Max(1, countForType);
            
            wave.enemies.Add(new EndlessWave.EnemySpawnInfo
            {
                prefab = type.enemyPrefab,
                count = countForType,
                spawnRate = type.baseSpawnRate // Keep spawn rate constant
            });
        }
        
        return wave;
    }
    
    int CalculateEnemiesForWave(int waveNumber, float quantityMultiplier)
    {
        // Base enemies plus scaling based on wave number
        float baseEnemies = baseEnemiesPerWave + (waveNumber * enemiesPerWaveIncrease);
        
        // Apply quantity multiplier
        int totalEnemies = Mathf.RoundToInt(baseEnemies * quantityMultiplier);
        
        // Cap at maximum to prevent performance issues
        return Mathf.Min(totalEnemies, maxEnemiesPerWave);
    }
    
    void UpdateQuantityMultiplier()
    {
        // Increase quantity based on game time (in minutes)
        float minutes = gameTime / 60f;
        currentQuantityMultiplier = 1f + (minutes * quantityIncreaseRate);
        currentQuantityMultiplier = Mathf.Min(currentQuantityMultiplier, maxQuantityMultiplier);
    }
    
    int GetTotalEnemiesInWave(EndlessWave wave)
    {
        int total = 0;
        foreach (var spawnInfo in wave.enemies)
        {
            total += spawnInfo.count;
        }
        return total;
    }

    Vector2 GetOffScreenSpawnPosition()
    {
        if (mainCamera == null || player == null) return Vector2.zero;

        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        
        Vector2 cameraCenter = mainCamera.transform.position;
        Vector2 playerPosition = player.position;

        List<Vector2> spawnEdges = new List<Vector2>
        {
            new Vector2(cameraCenter.x - cameraWidth/2 - spawnDistanceFromCamera, 
                       playerPosition.y + Random.Range(-cameraHeight/2, cameraHeight/2)),
            new Vector2(cameraCenter.x + cameraWidth/2 + spawnDistanceFromCamera, 
                       playerPosition.y + Random.Range(-cameraHeight/2, cameraHeight/2)),
            new Vector2(playerPosition.x + Random.Range(-cameraWidth/2, cameraWidth/2), 
                       cameraCenter.y + cameraHeight/2 + spawnDistanceFromCamera),
            new Vector2(playerPosition.x + Random.Range(-cameraWidth/2, cameraWidth/2), 
                       cameraCenter.y - cameraHeight/2 - spawnDistanceFromCamera)
        };

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
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null && Vector2.Distance(spawnPos, enemy.transform.position) < minDistanceBetweenSpawns)
            {
                return false;
            }
        }

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

    public void StopSpawning()
    {
        isSpawning = false;
        if (currentSpawnCoroutine != null)
            StopCoroutine(currentSpawnCoroutine);
    }

    public void ResetSpawning()
    {
        StopSpawning();
        
        // Destroy all active enemies
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        activeEnemies.Clear();
        
        // Reset stats
        currentWaveNumber = 0;
        currentQuantityMultiplier = 1f;
        gameTime = 0f;
    }

    // Helper method to get current spawn info (useful for UI)
    public int GetProjectedNextWaveEnemies()
    {
        int nextWave = currentWaveNumber + 1;
        return CalculateEnemiesForWave(nextWave, currentQuantityMultiplier);
    }

    void OnDrawGizmosSelected()
    {
        if (mainCamera != null)
        {
            float cameraHeight = 2f * mainCamera.orthographicSize;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            Vector2 cameraCenter = mainCamera.transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(cameraCenter, new Vector3(cameraWidth, cameraHeight, 0));

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(cameraCenter, new Vector3(cameraWidth + spawnDistanceFromCamera * 2, 
                                                          cameraHeight + spawnDistanceFromCamera * 2, 0));
        }
    }
}