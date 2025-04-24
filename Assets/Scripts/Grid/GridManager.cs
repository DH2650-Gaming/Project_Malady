using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private Transform tilesParent;
    
    private Dictionary<Vector2, Tile> _tiles = new Dictionary<Vector2, Tile>();
    private int _offsetX = 6;
    private int _offsetY = 3;
    private Vector2 _tileOffset = new Vector3(0.5f, 0.5f);

    void Start()
    {
        GenerateGrid();
    }
    
    // Creates grid with set width and height
    void GenerateGrid()
    {
        for (int x = -_offsetX; x < width - _offsetX; x++)
        {
            for (int y = -_offsetY; y < height - _offsetY;  y++)
            {
                var spawnedTile = Instantiate(tilePrefab, new Vector2(x, y) + _tileOffset, Quaternion.identity);
                spawnedTile.transform.SetParent(tilesParent);
                
                _tiles.Add(new Vector2(x, y), spawnedTile);
            }
        }
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
