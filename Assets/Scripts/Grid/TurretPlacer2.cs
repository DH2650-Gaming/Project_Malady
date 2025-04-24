using UnityEngine;

public class TurretPlacer2 : MonoBehaviour
{
    [SerializeField] private GameObject[] turrets;
    [SerializeField] private GridManager gridManager;

    private int _currentTurretIndex = -1;
    private GameObject _currentTurret;
    private Vector2 _offset = new Vector2(0.5f, 0.5f);
    
    void Update()
    {
        if (_currentTurretIndex != -1)
        {
            PlaceTurret();
        }
    }
    
    public void SelectTurret(int turretIndex)
    {
        // If already selected, deselect
        if (_currentTurretIndex == turretIndex)
        {
            _currentTurretIndex = -1;
            _currentTurret = null;
        }
        // Set turret selection index
        else
        {
            _currentTurretIndex = turretIndex;
            _currentTurret = turrets[turretIndex];
        }
    }
    
    private void PlaceTurret()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            Vector2 gridPos = new Vector2(Mathf.FloorToInt(worldPoint.x), Mathf.FloorToInt(worldPoint.y));
            Tile tile = gridManager.GetTile(gridPos);
            
            if (tile != null && tile.CompareTag("Occupied") == false)
            {
                Instantiate(_currentTurret, gridPos + _offset, Quaternion.identity);
                tile.gameObject.tag = "Occupied";
            }
            
            _currentTurretIndex = -1;
            _currentTurret = null;
        }
    }
}
