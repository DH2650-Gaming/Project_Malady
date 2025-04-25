using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic; // Required for Lists

/// <summary>
/// Manages the overall game state, including pathfinder initialization,
/// enemy spawning, and tracking game progress (like enemies reaching the exit).
/// </summary>

[System.Serializable]
public class EnemyData{
    public GameObject enemyPrefab; // Reference the prefab

    public GameObject spawnPoint;

    public Vector3 spawnOffset;

    public bool randomSpawnOffset = false;

    public int totalCount = 1;

    public int spawnedCount = 0;

    public float initialDelay = 0.0f;

    public float spawnInterval = 0.0f;
}
[System.Serializable]
public class EnemyGroup
{
    public List<EnemyData> enemies = new List<EnemyData>();

    public float spawnDelay = 0f;
}
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
    [Tooltip("List of all possible exit cells.")]
    [SerializeField] private List<Transform> exitCells = new List<Transform>();
    [Tooltip("Time delay in seconds between spawning enemies.")]
    [SerializeField] private List<EnemyGroup> spawnTable = new List<EnemyGroup>();
    // Add more variables here for wave logic later (e.g., enemiesPerWave, timeBetweenWaves)

    [Header("Game State")]
    [Tooltip("Counter for how many enemies have successfully reached an exit.")]
    [SerializeField] [ReadOnly] private int enemiesReachedExit = 0; // ReadOnly attribute makes it visible but not editable in inspector
    [ReadOnly] public HashSet<GameObject> spawnedEnemies;
    public GameObject playerhero;
    public Camera gameCamera;


    // --- Private Runtime Variables ---
    private float timeSinceLastSpawn = 0f;
    private bool pathfinderInitialized = false;
    private int spawngroupIndex = 0;

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

        Debug.Log("GameMaster requesting Flow Field calculation...");
        pathfinderInstance.CalculateFlowFields();
        pathfinderInitialized = true; // Assume calculation completes instantly for now

        
        timeSinceLastSpawn = 0f;
        spawnedEnemies = new HashSet<GameObject>();
        enemiesReachedExit = 0;

         // Validate spawn points and prefabs
         if (spawnTable == null || spawnTable.Count == 0)
         {
             Debug.LogWarning("GameMaster: No spawn table assigned or empty!", this);
         }
    }

    void Update()
    {
        // Don't run update logic if critical components are missing or not ready
        if (!pathfinderInitialized || spawnTable == null || spawnTable.Count == 0)
        {
            return;
        }

        if (spawnedEnemies.Count > 0){
            // Check if they are still valid
            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy == null)
                {
                    spawnedEnemies.Remove(enemy);
                }
            }
        }

        if (spawngroupIndex >= spawnTable.Count)
        {
            if(spawnedEnemies.Count == 0)
            {
                // Victory and end the game
                Debug.Log("All enemies have been spawned and reached the exit! You win!");
                //TODO: Implement victory screen
                return;
            }
        }else{
            EnemyGroup currentGroup = spawnTable[spawngroupIndex];
            if (currentGroup.spawnDelay > 0)
            {
                currentGroup.spawnDelay -= Time.deltaTime;
                if (currentGroup.spawnDelay <= 0)
                {
                    timeSinceLastSpawn = 0f;
                }
            }else
            {
                timeSinceLastSpawn += Time.deltaTime;
                // Spawn enemies in the current group
                bool allSpawned = true;
                foreach (EnemyData enemyData in currentGroup.enemies)
                {
                    if (enemyData.spawnedCount < enemyData.totalCount)
                    {
                        allSpawned = false;
                        while (enemyData.spawnedCount < enemyData.totalCount && timeSinceLastSpawn >= enemyData.initialDelay + enemyData.spawnInterval * enemyData.spawnedCount)
                        {
                            if(enemyData.randomSpawnOffset)
                            {
                                enemyData.spawnOffset = randomSpawnOffset(enemyData.enemyPrefab.transform.localScale);
                            }
                            SpawnEnemy(enemyData.enemyPrefab, enemyData.spawnPoint.transform.position, enemyData.spawnOffset);
                            enemyData.spawnedCount++;
                        }

                    }
                }
                if (allSpawned)
                {
                    // Move to the next spawn group
                    spawngroupIndex++;
                    timeSinceLastSpawn = 0f;
                }
            }
        }
    }

    Vector3 randomSpawnOffset(Vector3 scale)
    {
        Vector3 cellSize = groundTilemap.cellSize;
        float xOffset = Random.Range(0, cellSize.x-scale.x);
        float yOffset = Random.Range(0, cellSize.y-scale.y);
        return new Vector3(xOffset, yOffset, 0);
    }

    /// <summary>
    /// Spawns a single enemy at a random spawn point.
    /// </summary>
    void SpawnEnemy(GameObject enemyPrefab, Vector3 spawnPoint, Vector3 spawnOffset)
    {
        Vector3 scale = enemyPrefab.transform.localScale;
        Vector3 cellSize = groundTilemap.cellSize;
        if (scale.x > cellSize.x || scale.y > cellSize.y)
        {
            Debug.LogWarning("Enemy prefab is too large to spawn safely!");
            return;
        }

        Vector3Int cellPos = groundTilemap.WorldToCell(spawnPoint);
        if (!groundTilemap.HasTile(cellPos))
        {
            Debug.LogWarning("Spawn point is not valid! No tile at the spawn location.");
            return;
        }
        if(spawnOffset.x + scale.x > cellSize.x || spawnOffset.y + scale.y > cellSize.y)
        {
            Debug.LogWarning("Spawn offset is too large! Randomizing offset.");
            spawnOffset = randomSpawnOffset(scale);
        }
        Vector3 spawnPosition = groundTilemap.GetCellCenterWorld(cellPos) + spawnOffset - new Vector3(cellSize.x/2, cellSize.y/2, 0) + new Vector3(scale.x/2, scale.y/2, 0);
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        spawnedEnemies.Add(enemy);
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
