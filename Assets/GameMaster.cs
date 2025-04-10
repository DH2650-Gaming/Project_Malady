using UnityEngine;
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
    [Tooltip("Reference to the Pathfinder instance. If null, will attempt to find it.")]
    public Pathfinder pathfinderInstance;

    [Header("Enemy Spawning")]
    [Tooltip("List of different enemy prefabs to spawn.")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [Tooltip("List of possible locations where enemies can spawn.")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
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
            pathfinderInstance = Pathfinder.Instance;
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
         if (spawnPoints.Count == 0) {
             Debug.LogWarning("GameMaster: No spawn points assigned!", this);
         }
    }

    void Update()
    {
        // Don't run update logic if critical components are missing or not ready
        if (!pathfinderInitialized || enemyPrefabs.Count == 0 || spawnPoints.Count == 0)
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
        if (spawnPoints.Count == 0) return; // Should be caught in Start, but double-check
        int spawnIndex = Random.Range(0, spawnPoints.Count);
        Transform selectedSpawnPoint = spawnPoints[spawnIndex];

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
