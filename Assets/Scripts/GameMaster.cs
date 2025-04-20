using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic; // Required for Lists

/// <summary>
/// Manages the overall game state, including pathfinder initialization,
/// enemy spawning, and tracking game progress (like enemies reaching the exit).
/// </summary>
public class GameMaster : MonoBehaviour
{
    // --- Singleton Setup ---
    private static GameMaster _instance;
    public static GameMaster Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameMaster>(); // Use modern API
                if (_instance == null)
                {
                    Debug.LogError("GameMaster instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            // Optional: DontDestroyOnLoad(gameObject); // If GameMaster should persist across scenes
        }
        else if (_instance != this)
        {
            Debug.LogWarning("Duplicate GameMaster instance found, destroying this one.");
            Destroy(gameObject);
        }
    }
    // --- End Singleton Setup ---

    [Header("Pathfinding")]
    [Tooltip("Reference to the Pathfinder instance. Please set to null, will create automatically.")]
    [ReadOnly] public Pathfinder pathfinderInstance;
    [Tooltip("The main tilemap defining walkable ground areas.")]
    public Tilemap groundTilemap;
    [Tooltip("Tilemaps containing obstacles that CANNOT be destroyed.")]
    public Tilemap[] indestructibleObstacleTilemaps;
    [Tooltip("Tilemaps containing obstacles that CAN be destroyed.")]
    public Tilemap[] destructibleObstacleTilemaps;

    [Header("Enemy Spawning")]
    [Tooltip("List of different enemy prefabs to spawn.")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [Tooltip("List of possible locations where enemies can spawn.")]
    [SerializeField] private List<Transform> spawnCells = new List<Transform>();
    [Tooltip("List of all possible exit cells.")]
    [SerializeField] private List<Transform> exitCells = new List<Transform>();
    [Tooltip("Time delay in seconds between spawning enemies.")]
    [SerializeField] private float spawnInterval = 2.0f;
    // Add more variables here for wave logic later (e.g., enemiesPerWave, timeBetweenWaves)

    [Header("Game State")]
    [Tooltip("Counter for how many enemies have successfully reached an exit.")]
    [SerializeField] [ReadOnly] private int enemiesReachedExit = 0; // ReadOnly attribute makes it visible but not editable in inspector

    // --- Private Runtime Variables ---
    private float timeSinceLastSpawn = 0f;
    private bool pathfinderInitialized = false;

    void Start()
    {
        // Ensure we have the Pathfinder instance
        if (pathfinderInstance == null)
        {
            pathfinderInstance = new Pathfinder(groundTilemap, indestructibleObstacleTilemaps, destructibleObstacleTilemaps, exitCells);
        }

        if (pathfinderInstance == null)
        {
            Debug.LogError("GameMaster could not find the Pathfinder instance! Pathfinding will not be initialized.", this);
            enabled = false; // Disable GameMaster if Pathfinder is missing
            return;
        }

        // Initialize the Pathfinder's flow field
        // IMPORTANT: Ensure this runs *after* the level geometry (tilemaps, obstacles) is fully set up.
        // If level generation happens in Start/Awake elsewhere, you might need to delay this call
        // (e.g., using a coroutine with 'yield return null' or calling it from a level setup manager).
        Debug.Log("GameMaster requesting Flow Field calculation...");
        pathfinderInstance.CalculateFlowFields();
        pathfinderInitialized = true; // Assume calculation completes instantly for now

        // Initialize spawn timer (optional: add initial delay)
        timeSinceLastSpawn = 0f; // Or set to -initialDelay to wait before first spawn

        // Reset counter
        enemiesReachedExit = 0;

         // Validate spawn points and prefabs
         if (enemyPrefabs.Count == 0) {
             Debug.LogWarning("GameMaster: No enemy prefabs assigned!", this);
         }
         if (spawnCells.Count == 0) {
             Debug.LogWarning("GameMaster: No spawn points assigned!", this);
         }
    }

    void Update()
    {
        // Don't run update logic if critical components are missing or not ready
        if (!pathfinderInitialized || enemyPrefabs.Count == 0 || spawnCells.Count == 0)
        {
            return;
        }

        // Simple timed spawning logic
        timeSinceLastSpawn += Time.deltaTime;
        if (timeSinceLastSpawn >= spawnInterval)
        {
            SpawnEnemy();
            timeSinceLastSpawn = 0f; // Reset timer
            // Could also subtract spawnInterval for more precise timing:
            // timeSinceLastSpawn -= spawnInterval;
        }

        // Add other game management logic here (e.g., checking win/loss conditions)
    }

    /// <summary>
    /// Spawns a single enemy at a random spawn point.
    /// </summary>
    void SpawnEnemy()
    {
        // --- Select Prefab ---
        // Simple: pick the first prefab. Expand later for variety.
        if (enemyPrefabs.Count == 0) return; // Should be caught in Start, but double-check
        GameObject prefabToSpawn = enemyPrefabs[0]; // TODO: Implement logic for choosing different prefabs

        // --- Select Spawn Point ---
        if (spawnCells.Count == 0) return; // Should be caught in Start, but double-check
        int spawnIndex = Random.Range(0, spawnCells.Count);
        Transform selectedSpawnPoint = spawnCells[spawnIndex];

        if (selectedSpawnPoint == null)
        {
            Debug.LogError("A null spawn point was selected!", this);
            return;
        }

        // --- Instantiate ---
        Debug.Log($"Spawning enemy {prefabToSpawn.name} at {selectedSpawnPoint.name}");
        Instantiate(prefabToSpawn, selectedSpawnPoint.position, selectedSpawnPoint.rotation);

        // TODO: Add logic for tracking spawned enemies, wave counts, etc.
    }

    /// <summary>
    /// Public method to be called by enemies when they reach an exit.
    /// Increments the counter.
    /// </summary>
    public void EnemyReachedExit()
    {
        enemiesReachedExit++;
        Debug.Log($"An enemy reached the exit! Total reached: {enemiesReachedExit}");

        // TODO: Add logic related to enemies reaching the exit (e.g., player loses health, check win/loss)
    }

    void OnDrawGizmos()
    {
        if(!pathfinderInitialized || pathfinderInstance == null)
        {
            return; // Don't draw if pathfinding is not initialized
        }
        Vector3 cellSize = groundTilemap.cellSize;
        // Use the smaller dimension for scaling to fit within the cell
        float arrowBodyScale = 0.4f; // Arrow body length as % of cell size
        float arrowHeadScale = 0.2f;
        float arrowHeadAngle = 25.0f;
        float minCellDim = Mathf.Min(Mathf.Abs(cellSize.x), Mathf.Abs(cellSize.y));
        float bodyLength = minCellDim * arrowBodyScale;
        float headLength = minCellDim * arrowHeadScale;

        foreach (KeyValuePair<Vector3Int, FlowFieldNode> pair in pathfinderInstance.finalFlowField)
        {
            Vector3Int cellPos = pair.Key;
            FlowFieldNode node = pair.Value;
            Vector3 worldPos = groundTilemap.GetCellCenterWorld(cellPos);

            // Skip drawing if direction is essentially zero (e.g., on the target)
            // Allow drawing for cost 0 (target cells themselves) but maybe differently
            // if (node.DirectionToTarget.sqrMagnitude < 0.001f && node.Cost > 0)
            // {
            //     // Potentially draw something else for nodes with cost but no direction (should be rare)
            //     Gizmos.color = Color.magenta; // Indicate an issue?
            //     Gizmos.DrawWireSphere(worldPos, minCellDim * 0.1f);
            //     continue;
            // }

            // Set color based on status
            switch (node.Status)
            {
                case FlowFieldStatus.Blocked:
                    Gizmos.color = Color.red;
                    // Draw a small cross or cube for blocked cells
                    float blockedMarkerSize = minCellDim * 0.15f;
                    Gizmos.DrawLine(worldPos - Vector3.right * blockedMarkerSize, worldPos + Vector3.right * blockedMarkerSize);
                    Gizmos.DrawLine(worldPos - Vector3.up * blockedMarkerSize, worldPos + Vector3.up * blockedMarkerSize);
                    continue; // Don't draw an arrow for blocked

                case FlowFieldStatus.ReachesExit:
                    Gizmos.color = Color.green;
                     // If it's an exit cell itself (cost 0), draw a circle
                    if (node.Cost == 0) {
                        Gizmos.DrawWireSphere(worldPos, minCellDim * 0.2f);
                        continue;
                    }
                    break;

                case FlowFieldStatus.ReachesDestructible:
                    Gizmos.color = Color.yellow;
                     // If it's the target destructible itself (cost 0?), maybe draw a square?
                     // Note: Cost might not be 0 if calculated *from exits*
                     //if (node.Cost == 0) { // This condition might not be right
                     //    Gizmos.DrawWireCube(worldPos, Vector3.one * minCellDim * 0.4f);
                     //    continue;
                     //}
                    break;

                default:
                    Gizmos.color = Color.grey; // Should not happen
                    break;
            }

            // Ensure direction is normalized (it should be from FlowFieldNode constructor)
            Vector3 direction = node.DirectionToTarget; //.normalized; // Already normalized

            // Calculate arrow points
            Vector3 arrowEndPoint = worldPos + direction * bodyLength;

            // Draw arrow body
            Gizmos.DrawLine(worldPos, arrowEndPoint);

            // Calculate arrowhead points using Quaternions for rotation
            // Arrowhead lines point backwards from the arrowEndPoint
            Vector3 rightVec = Quaternion.LookRotation(-direction) * Quaternion.Euler(0, arrowHeadAngle, 0) * Vector3.forward;
            Vector3 leftVec = Quaternion.LookRotation(-direction) * Quaternion.Euler(0, -arrowHeadAngle, 0) * Vector3.forward;

            // Draw arrowhead lines
            Gizmos.DrawLine(arrowEndPoint, arrowEndPoint + rightVec * headLength);
            Gizmos.DrawLine(arrowEndPoint, arrowEndPoint + leftVec * headLength);
        }
    }
    // Simple ReadOnly attribute for inspector display
    public class ReadOnlyAttribute : PropertyAttribute { }

    #if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false; // Disable editing
            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true; // Re-enable editing for subsequent fields
        }
         public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        {
             return UnityEditor.EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
    #endif
}
