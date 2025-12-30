using System.Collections.Generic;
using UnityEngine;

public class SimpleEndlessMapGenerator : MonoBehaviour
{
    [Header("TILE ASSETS")]
    public Sprite[] tileSprites; // Your 4 PNG sprites
    
    [Header("OBSTACLES")]
    public GameObject[] obstaclePrefabs;
    
    [Header("TILE SIZE SETTINGS")]
    [Tooltip("Base size of each tile in world units")]
    public float tileSize = 2f; // Increased from 1f
    
    [Tooltip("Multiply sprite size by this amount")]
    public float spriteScaleMultiplier = 2f; // New parameter
    
    [Header("GENERATION SETTINGS")]
    public int generateRadius = 10;
    [Range(0f, 1f)] public float obstacleChance = 0.2f;
    
    [Header("COLLISION SETTINGS")]
    public bool makeObstaclesSolid = true;
    public string obstacleTag = "Obstacle";
    
    private Transform player;
    private Dictionary<Vector2Int, GameObject> tiles = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int lastPlayerTile;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogError("No player found! Make sure player has 'Player' tag.");
            enabled = false;
            return;
        }
        
        GenerateMap();
    }
    
    void Update()
    {
        Vector2Int currentTile = GetPlayerTile();
        if (currentTile != lastPlayerTile)
        {
            GenerateMap();
            lastPlayerTile = currentTile;
        }
    }
    
    Vector2Int GetPlayerTile()
    {
        return new Vector2Int(
            Mathf.RoundToInt(player.position.x / tileSize),
            Mathf.RoundToInt(player.position.y / tileSize)
        );
    }
    
    void GenerateMap()
    {
        Vector2Int playerTile = GetPlayerTile();
        HashSet<Vector2Int> neededTiles = new HashSet<Vector2Int>();
        
        // Determine which tiles we need
        for (int x = -generateRadius; x <= generateRadius; x++)
        {
            for (int y = -generateRadius; y <= generateRadius; y++)
            {
                Vector2Int tilePos = new Vector2Int(playerTile.x + x, playerTile.y + y);
                neededTiles.Add(tilePos);
                
                if (!tiles.ContainsKey(tilePos))
                {
                    CreateTile2D(tilePos);
                }
            }
        }
        
        // Remove old tiles
        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var tilePos in tiles.Keys)
        {
            if (!neededTiles.Contains(tilePos))
            {
                toRemove.Add(tilePos);
            }
        }
        
        foreach (var tilePos in toRemove)
        {
            Destroy(tiles[tilePos]);
            tiles.Remove(tilePos);
        }
    }
    
    void CreateTile2D(Vector2Int pos)
    {
        Vector3 worldPos = new Vector3(pos.x * tileSize, pos.y * tileSize, 0);
        
        GameObject tile = new GameObject($"Tile_{pos.x}_{pos.y}");
        tile.transform.position = worldPos;
        tile.transform.parent = transform;
        
        // Add sprite
        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
        if (tileSprites.Length > 0)
        {
            Sprite selectedSprite = tileSprites[Random.Range(0, tileSprites.Length)];
            sr.sprite = selectedSprite;
            
            // Calculate scale to make sprite fit the tileSize
            if (selectedSprite != null)
            {
                // Get the sprite's natural size in world units
                float spriteWidth = selectedSprite.bounds.size.x;
                float spriteHeight = selectedSprite.bounds.size.y;
                
                // Calculate scale needed to make sprite fill the tile
                float scaleX = tileSize / spriteWidth;
                float scaleY = tileSize / spriteHeight;
                
                // Use uniform scaling (keep aspect ratio)
                float uniformScale = Mathf.Min(scaleX, scaleY) * spriteScaleMultiplier;
                
                // Apply scale
                tile.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
                
                Debug.Log($"Tile sprite: {selectedSprite.name}, Size: {spriteWidth}x{spriteHeight}, Scale: {uniformScale}");
            }
        }
        else
        {
            sr.color = Color.gray;
        }
        
        // Set sorting layer to ensure tiles are behind characters
        sr.sortingLayerName = "Background";
        sr.sortingOrder = -10;
        
        // Spawn obstacle
        if (obstaclePrefabs.Length > 0 && Random.value < obstacleChance)
        {
            Vector3 obstaclePos = worldPos + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), -1f); // -1f puts obstacles in front of tiles
            GameObject obstacle = Instantiate(
                obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)], 
                obstaclePos, 
                Quaternion.identity, 
                tile.transform
            );
            
            obstacle.name = "Obstacle";
            
            // FIX: Ensure obstacle collider is NOT a trigger
            FixObstacleCollider(obstacle);
        }
        
        tiles.Add(pos, tile);
    }

    void FixObstacleCollider(GameObject obstacle)
    {
        // Get all Collider2D components
        Collider2D[] colliders = obstacle.GetComponents<Collider2D>();
        
        if (colliders.Length == 0)
        {
            Debug.LogWarning($"Obstacle '{obstacle.name}' has no Collider2D!");
            // Add one if missing
            BoxCollider2D newCollider = obstacle.AddComponent<BoxCollider2D>();
            newCollider.isTrigger = false; // Make sure it's NOT a trigger
        }
        else
        {
            // Fix all colliders
            foreach (Collider2D col in colliders)
            {
                col.isTrigger = false; // MAKE SURE THIS IS FALSE!
                Debug.Log($"Fixed collider on {obstacle.name}: isTrigger = {col.isTrigger}");
            }
        }
        
        // Also check if it needs Rigidbody2D
        Rigidbody2D rb = obstacle.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = obstacle.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static; // Static obstacles don't move
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Debug.Log($"Added Static Rigidbody2D to {obstacle.name}");
        }
        else if (rb.bodyType != RigidbodyType2D.Static)
        {
            rb.bodyType = RigidbodyType2D.Static; // Make sure it's static
        }
    }
}