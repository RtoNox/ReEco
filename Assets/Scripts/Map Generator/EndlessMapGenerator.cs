using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessMapGenerator : MonoBehaviour
{
    [Header("Tile Settings")]
    public MapTile[] tilePrefabs; // Assign your 4 tiles here
    public GameObject[] obstaclePrefabs; // Assign your 2 obstacle prefabs
    
    [Header("Map Settings")]
    public int chunkSize = 10; // Tiles per chunk
    public int visibleChunks = 3; // Chunks loaded around player
    public float tileSize = 1f; // Size of each tile
    
    [Header("Generation Settings")]
    [Range(0f, 1f)] public float obstacleSpawnChance = 0.2f;
    public int minObstaclesPerChunk = 1;
    public int maxObstaclesPerChunk = 5;
    
    [Header("Performance")]
    public bool useObjectPooling = true;
    public int poolSize = 50;
    
    // Current map data
    private Dictionary<Vector2Int, GameObject> activeTiles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, List<GameObject>> activeObstacles = new Dictionary<Vector2Int, List<GameObject>>();
    
    // Player tracking
    private Transform player;
    private Vector2Int currentPlayerChunk;
    
    // Object pooling
    private Queue<GameObject> tilePool = new Queue<GameObject>();
    private Queue<GameObject> obstaclePool = new Queue<GameObject>();
    
    void Start()
    {
        // Find player
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogError("No player found! Make sure player has 'Player' tag.");
            return;
        }
        
        // Initialize object pools
        if (useObjectPooling)
        {
            InitializePools();
        }
        
        // Generate initial chunks
        currentPlayerChunk = GetChunkPosition(player.position);
        GenerateInitialChunks();
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Check if player moved to new chunk
        Vector2Int playerChunk = GetChunkPosition(player.position);
        if (playerChunk != currentPlayerChunk)
        {
            currentPlayerChunk = playerChunk;
            UpdateVisibleChunks();
        }
    }
    
    void InitializePools()
    {
        // Create tile pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject tile = new GameObject("PooledTile");
            tile.SetActive(false);
            tilePool.Enqueue(tile);
        }
        
        // Create obstacle pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obstacle = new GameObject("PooledObstacle");
            obstacle.SetActive(false);
            obstaclePool.Enqueue(obstacle);
        }
    }
    
    GameObject GetPooledTile()
    {
        if (tilePool.Count > 0)
        {
            GameObject tile = tilePool.Dequeue();
            tile.SetActive(true);
            return tile;
        }
        return new GameObject("Tile");
    }
    
    GameObject GetPooledObstacle()
    {
        if (obstaclePool.Count > 0)
        {
            GameObject obstacle = obstaclePool.Dequeue();
            obstacle.SetActive(true);
            return obstacle;
        }
        return new GameObject("Obstacle");
    }
    
    void ReturnToPool(GameObject obj, bool isObstacle = false)
    {
        obj.SetActive(false);
        if (isObstacle)
            obstaclePool.Enqueue(obj);
        else
            tilePool.Enqueue(obj);
    }
    
    Vector2Int GetChunkPosition(Vector3 worldPosition)
    {
        int chunkX = Mathf.FloorToInt(worldPosition.x / (chunkSize * tileSize));
        int chunkY = Mathf.FloorToInt(worldPosition.z / (chunkSize * tileSize));
        return new Vector2Int(chunkX, chunkY);
    }
    
    void GenerateInitialChunks()
    {
        for (int x = -visibleChunks; x <= visibleChunks; x++)
        {
            for (int y = -visibleChunks; y <= visibleChunks; y++)
            {
                Vector2Int chunkPos = currentPlayerChunk + new Vector2Int(x, y);
                GenerateChunk(chunkPos);
            }
        }
    }
    
    void UpdateVisibleChunks()
    {
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        
        // Find chunks that are too far away
        foreach (var chunkPos in activeTiles.Keys)
        {
            int distanceX = Mathf.Abs(chunkPos.x - currentPlayerChunk.x);
            int distanceY = Mathf.Abs(chunkPos.y - currentPlayerChunk.y);
            
            if (distanceX > visibleChunks || distanceY > visibleChunks)
            {
                chunksToRemove.Add(chunkPos);
            }
        }
        
        // Remove old chunks
        foreach (var chunkPos in chunksToRemove)
        {
            RemoveChunk(chunkPos);
        }
        
        // Generate new chunks around player
        for (int x = -visibleChunks; x <= visibleChunks; x++)
        {
            for (int y = -visibleChunks; y <= visibleChunks; y++)
            {
                Vector2Int chunkPos = currentPlayerChunk + new Vector2Int(x, y);
                if (!activeTiles.ContainsKey(chunkPos))
                {
                    GenerateChunk(chunkPos);
                }
            }
        }
    }
    
    void GenerateChunk(Vector2Int chunkPos)
    {
        // Generate each tile in the chunk
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector2Int tilePos = new Vector2Int(
                    chunkPos.x * chunkSize + x,
                    chunkPos.y * chunkSize + y
                );
                
                GenerateTile(tilePos);
                
                // Randomly spawn obstacles
                if (Random.value < obstacleSpawnChance)
                {
                    SpawnObstacle(tilePos);
                }
            }
        }
    }
    
    void GenerateTile(Vector2Int tilePos)
    {
        // Randomly select a tile
        if (tilePrefabs.Length == 0) return;
        
        MapTile selectedTile = tilePrefabs[Random.Range(0, tilePrefabs.Length)];
        
        // Calculate world position
        Vector3 worldPos = new Vector3(
            tilePos.x * tileSize,
            0f,
            tilePos.y * tileSize
        );
        
        GameObject tileObject;
        
        if (useObjectPooling)
        {
            tileObject = GetPooledTile();
            tileObject.transform.position = worldPos;
        }
        else
        {
            tileObject = new GameObject($"Tile_{tilePos.x}_{tilePos.y}");
            tileObject.transform.position = worldPos;
        }
        
        // Add sprite renderer or mesh renderer
        if (selectedTile.tileTexture != null)
        {
            // For 2D sprites
            SpriteRenderer sr = tileObject.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(
                selectedTile.tileTexture,
                new Rect(0, 0, selectedTile.tileTexture.width, selectedTile.tileTexture.height),
                new Vector2(0.5f, 0.5f)
            );
        }
        
        // Add collider if needed
        BoxCollider collider = tileObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(tileSize, 0.1f, tileSize);
        
        // Store tile
        if (!activeTiles.ContainsKey(tilePos))
        {
            activeTiles.Add(tilePos, tileObject);
        }
    }
    
    void SpawnObstacle(Vector2Int tilePos)
    {
        if (obstaclePrefabs.Length == 0) return;
        
        // Get tile position
        Vector3 tileWorldPos = new Vector3(
            tilePos.x * tileSize,
            0f,
            tilePos.y * tileSize
        );
        
        // Random offset within tile
        Vector3 offset = new Vector3(
            Random.Range(-tileSize/2, tileSize/2),
            0f,
            Random.Range(-tileSize/2, tileSize/2)
        );
        
        Vector3 obstaclePos = tileWorldPos + offset;
        
        // Select random obstacle
        GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        
        GameObject obstacleObject;
        
        if (useObjectPooling)
        {
            obstacleObject = GetPooledObstacle();
            obstacleObject.transform.position = obstaclePos;
            
            // Copy prefab components
            CopyPrefabComponents(obstaclePrefab, obstacleObject);
        }
        else
        {
            obstacleObject = Instantiate(obstaclePrefab, obstaclePos, Quaternion.identity);
        }
        
        // Random rotation
        obstacleObject.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        
        // Store obstacle
        if (!activeObstacles.ContainsKey(tilePos))
        {
            activeObstacles[tilePos] = new List<GameObject>();
        }
        activeObstacles[tilePos].Add(obstacleObject);
    }
    
    void CopyPrefabComponents(GameObject prefab, GameObject target)
    {
        // This is a simplified version - in practice you might need more robust copying
        if (prefab.GetComponent<MeshFilter>())
        {
            MeshFilter mf = target.AddComponent<MeshFilter>();
            mf.mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
        }
        
        if (prefab.GetComponent<MeshRenderer>())
        {
            MeshRenderer mr = target.AddComponent<MeshRenderer>();
            mr.materials = prefab.GetComponent<MeshRenderer>().sharedMaterials;
        }
        
        if (prefab.GetComponent<Collider>())
        {
            // Copy collider properties based on type
            BoxCollider prefabBox = prefab.GetComponent<BoxCollider>();
            if (prefabBox)
            {
                BoxCollider box = target.AddComponent<BoxCollider>();
                box.center = prefabBox.center;
                box.size = prefabBox.size;
            }
            
            SphereCollider prefabSphere = prefab.GetComponent<SphereCollider>();
            if (prefabSphere)
            {
                SphereCollider sphere = target.AddComponent<SphereCollider>();
                sphere.center = prefabSphere.center;
                sphere.radius = prefabSphere.radius;
            }
        }
    }
    
    void RemoveChunk(Vector2Int chunkPos)
    {
        // Remove all tiles in this chunk
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector2Int tilePos = new Vector2Int(
                    chunkPos.x * chunkSize + x,
                    chunkPos.y * chunkSize + y
                );
                
                if (activeTiles.ContainsKey(tilePos))
                {
                    if (useObjectPooling)
                    {
                        ReturnToPool(activeTiles[tilePos]);
                    }
                    else
                    {
                        Destroy(activeTiles[tilePos]);
                    }
                    activeTiles.Remove(tilePos);
                }
                
                // Remove obstacles
                if (activeObstacles.ContainsKey(tilePos))
                {
                    foreach (var obstacle in activeObstacles[tilePos])
                    {
                        if (useObjectPooling)
                        {
                            ReturnToPool(obstacle, true);
                        }
                        else
                        {
                            Destroy(obstacle);
                        }
                    }
                    activeObstacles.Remove(tilePos);
                }
            }
        }
    }
    
    // Cleanup
    void OnDestroy()
    {
        foreach (var tile in activeTiles.Values)
        {
            Destroy(tile);
        }
        activeTiles.Clear();
        
        foreach (var obstacleList in activeObstacles.Values)
        {
            foreach (var obstacle in obstacleList)
            {
                Destroy(obstacle);
            }
        }
        activeObstacles.Clear();
    }
}