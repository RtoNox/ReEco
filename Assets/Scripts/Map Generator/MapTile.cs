using UnityEngine;

[CreateAssetMenu(fileName = "NewMapTile", menuName = "Map/Map Tile")]
public class MapTile : ScriptableObject
{
    public Texture2D tileTexture;
    public GameObject obstaclePrefab;
    public float obstacleChance = 0.3f;
    public bool canRotate = true;
    public bool walkable = true;
    public float moveSpeedMultiplier = 1f;
}