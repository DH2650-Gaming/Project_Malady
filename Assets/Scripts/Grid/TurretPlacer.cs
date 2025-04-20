using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class TurretPlacer : MonoBehaviour
{
    [SerializeField] private GameObject[] turrets;
    [SerializeField] private TileBase placeholderTilePrefab;
    [SerializeField] private GameMaster gminstance;

    private int _currentTurretIndex = -1;
    private GameObject _currentTurret;
    private Vector2 _offset = new Vector2(0.5f, 0.5f);
    
    void Start()
    {
        if (gminstance == null)
        {
            gminstance = GameMaster.Instance;
        }
        if (gminstance == null)
        {
            Debug.LogError("GameMaster instance not found in the scene!");
            return;
        }
    }
    void Update()
    {
        HandleKeyPress();
        if (_currentTurretIndex != -1)
        {
            PlaceTurret();
        }
    }
    
    // Temporary before adding pressable UI
    private void HandleKeyPress()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (_currentTurretIndex == 0)
            {
                _currentTurretIndex = -1;
            }
            else
            {
                _currentTurretIndex = 0;
                _currentTurret = turrets[_currentTurretIndex];
            }
            
            Debug.Log(_currentTurretIndex);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (_currentTurretIndex == 1)
            {
                _currentTurretIndex = -1;
            }
            else
            {
                _currentTurretIndex = 1;
                _currentTurret = turrets[_currentTurretIndex];
            }
            
        }
    }

    private void PlaceTurret()
    {
        Tilemap _groundTilemap = gminstance.groundTilemap;
        Tilemap _turretTilemap = gminstance.destructibleObstacleTilemaps[0];
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // Ensure Z position doesn't interfere with Tilemap checks
            worldPoint.z = _groundTilemap.transform.position.z;
            Vector3Int bottomLeftCell = _groundTilemap.WorldToCell(worldPoint);

            // Define the 1x1 area cells
            Vector3Int[] placementCells = new Vector3Int[1] {
                bottomLeftCell
            };
            
           if (CanPlaceTurret(placementCells))
            {
                // --- Placement ---
                Vector3 placementPosition = _groundTilemap.GetCellCenterWorld(bottomLeftCell) ;


                GameObject placedTurret = Instantiate(_currentTurret, placementPosition, Quaternion.identity);

                // Mark the cells as occupied
                foreach (var cell in placementCells)
                {
                    _turretTilemap.SetTile(cell, placeholderTilePrefab);
                }

                Debug.Log($"Placed turret at {bottomLeftCell}");
                gminstance.pathfinderInstance.CalculateFlowFields();

            }
            else
            {
                Debug.Log($"Cannot place turret at {bottomLeftCell}: Area blocked or outside ground.");
                // Optional: Add user feedback (e.g., play a sound)
            }
        }
    }
    private bool CanPlaceTurret(Vector3Int[] cellsToCheck)
    {
        Tilemap _groundTilemap = gminstance.groundTilemap;
        Tilemap [] indestructibleObstacleTilemaps = gminstance.indestructibleObstacleTilemaps;
        Tilemap [] destructibleObstacleTilemaps = gminstance.destructibleObstacleTilemaps;

        foreach (var cell in cellsToCheck)
        {
            // 1. Check ground
            if (!_groundTilemap.HasTile(cell))
            {
                return false;
            }
            foreach (var tilemap in indestructibleObstacleTilemaps)
            {
                if (tilemap.HasTile(cell))
                {
                    return false;
                }
            }
            foreach (var tilemap in destructibleObstacleTilemaps)
            {
                if (tilemap.HasTile(cell))
                {
                    return false;
                }
            }
        }
        return true;
    }

}
