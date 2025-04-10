using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private Camera cam;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap collisionTilemap;
    [SerializeField] private Tile tilePrefab;
    
    private Dictionary<Vector2, Tile> _tiles = new Dictionary<Vector2, Tile>();
    private const float Offset = 0.5f;

    void Start()
    {
        GenerateGrid();
    }
    
    // Creates grid with set width and height (create generalized function?)
    void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var spawnedTile = Instantiate(tilePrefab, new Vector2(x, y) + new Vector2(Offset, Offset), Quaternion.identity);
                
                _tiles.Add(new Vector2(x, y), spawnedTile);
            }
        }
        cam.transform.position = new Vector3((float)width/2, (float)height/2, -10);
        groundTilemap.transform.position = new Vector3((float)width/2, (float)height/2 - 1);
        collisionTilemap.transform.position = new Vector3((float)width/2, (float)height/2 - 1);
    }

    public Tile GetTile(Vector2 pos)
    {
        if (_tiles.ContainsKey(pos))
        {
            return _tiles[pos];
        }
        return null;
    }
}
