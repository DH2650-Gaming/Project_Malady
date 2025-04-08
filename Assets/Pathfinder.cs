using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq; // Used for OrderBy/ThenBy in Priority Queue simulation
using System;     // Used for IComparable

/// <summary>
/// Calculates and stores Flow Field pathfinding data for a Tilemap environment.
/// Prioritizes paths to the nearest exit, with a fallback to the nearest
/// destructible obstacle for cells that cannot reach an exit.
/// </summary>
public class Pathfinder : MonoBehaviour
{
    // --- Singleton Setup ---
    private static Pathfinder _instance;
    public static Pathfinder Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<Pathfinder>();
                if (_instance == null)
                {
                    Debug.LogError("Pathfinder instance not found in the scene!");
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
            // Optional: DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning("Duplicate Pathfinder instance found, destroying this one.");
            Destroy(gameObject);
        }
    }
    // --- End Singleton Setup ---

    // --- Inspector References ---
    [Header("Tilemap References")]
    [Tooltip("The main tilemap defining walkable ground areas.")]
    public Tilemap groundTilemap;
    [Tooltip("Tilemaps containing obstacles that CANNOT be destroyed.")]
    public Tilemap[] indestructibleObstacleTilemaps;
    [Tooltip("Tilemaps containing obstacles that CAN be destroyed.")]
    public Tilemap[] destructibleObstacleTilemaps;

    [Header("Pathfinding Targets")]
    [Tooltip("List of all possible exit cell coordinates.")]
    public List<Transform> exitCells = new List<Transform>();
    private List<Vector3Int> exitCellPositions = new List<Vector3Int>();
    // --- Flow Field Data ---
    // Stores the final calculated data for each cell
    private Dictionary<Vector3Int, FlowFieldNode> finalFlowField;
    // Stores the cost to reach the nearest exit (used for tie-breaking)
    private Dictionary<Vector3Int, int> costToExit;
    private List<Vector3Int> debug_checkedCells = new List<Vector3Int>(); // For debugging purposes
    private bool isInitialized = false;

    // --- Public Access ---

    /// <summary>
    /// Call this to perform the pre-computation of the flow fields.
    /// Should be called after level setup and whenever static obstacles change significantly.
    /// </summary>
    [ContextMenu("Calculate Flow Fields")]
    public void CalculateFlowFields()
    {
        if (groundTilemap == null || exitCells == null || exitCells.Count == 0)
        {
            Debug.LogError("Cannot calculate Flow Fields: Ground Tilemap or Exit Cells are not set!");
            isInitialized = false;
            return;
        }
        // Convert exit cell transforms to Vector3Int positions
        exitCellPositions.Clear();
        foreach (var exit in exitCells)
        {
            Vector3Int cellPosition = groundTilemap.WorldToCell(exit.position);
            if (groundTilemap.HasTile(cellPosition))
            {
                exitCellPositions.Add(cellPosition);
            }
            else
            {
                Debug.LogWarning($"Exit cell {exit.name} is not on the ground tilemap. Skipping.");
            }
        }
        if (exitCellPositions.Count == 0)
        {
            Debug.LogError("No valid exit cells found on the ground tilemap!");
            isInitialized = false;
            return;
        }

        Debug.Log("Starting Flow Field Calculation...");
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // --- Phase 1: Calculate Flow Field to Nearest Exit ---
        costToExit = new Dictionary<Vector3Int, int>(); // Store costs separately for tie-breaking
        Dictionary<Vector3Int, Vector3Int> cameFromExit = new Dictionary<Vector3Int, Vector3Int>(); // Track path for direction calculation
        CalculateDijkstra(exitCellPositions, costToExit, cameFromExit, true); // Avoids ALL obstacles

        // --- Phase 2: Identify Unreached Cells and Destructible Targets ---
        HashSet<Vector3Int> cellsWithoutExitPath = new HashSet<Vector3Int>();
        List<Vector3Int> destructibleCells = new List<Vector3Int>();
        FindUnreachedAndDestructibles(cellsWithoutExitPath, destructibleCells);

        // --- Phase 3: Calculate Flow Field to Nearest Destructible (for unreached cells) ---
        Dictionary<Vector3Int, int> costToDestructible = new Dictionary<Vector3Int, int>();
        Dictionary<Vector3Int, Vector3Int> cameFromDestructible = new Dictionary<Vector3Int, Vector3Int>();

        // Use the costToExit data for tie-breaking in the Dijkstra calculation
        CalculateDijkstra(destructibleCells, costToDestructible, cameFromDestructible, false, costToExit); // Avoids only INDESTRUCTIBLE obstacles

        // --- Phase 4: Combine Results into Final Flow Field ---
        finalFlowField = new Dictionary<Vector3Int, FlowFieldNode>();
        CombineFlowFields(costToExit, cameFromExit, costToDestructible, cameFromDestructible, cellsWithoutExitPath);

        stopwatch.Stop();
        Debug.Log($"Flow Field Calculation finished in {stopwatch.ElapsedMilliseconds} ms. Processed {finalFlowField.Count} cells.");
        isInitialized = true;
    }

    /// <summary>
    /// Gets the calculated flow field data for a specific cell.
    /// </summary>
    /// <param name="cellPosition">The cell coordinate to query.</param>
    /// <returns>FlowFieldNode containing direction, cost, and status, or a default Node if invalid/uncalculated.</returns>
    public FlowFieldNode GetFlowFieldNode(Vector3Int cellPosition)
    {
        if (!isInitialized || finalFlowField == null)
        {
            Debug.LogWarning("Flow Field not initialized. Call CalculateFlowFields() first.");
            return FlowFieldNode.BlockedNode; // Return a default blocked node
        }

        if (finalFlowField.TryGetValue(cellPosition, out FlowFieldNode node))
        {
            return node;
        }
        else
        {
            // Cell might be outside the calculated ground area or truly blocked
            return FlowFieldNode.BlockedNode;
        }
    }

    /// <summary>
    /// Clears the calculated flow field data. Call this if dynamic obstacles are added/removed frequently
    /// before recalculating.
    /// </summary>
    public void InvalidateField()
    {
        finalFlowField = null;
        costToExit = null;
        isInitialized = false;
        Debug.Log("Flow Field data invalidated.");
    }


    // --- Core Calculation Methods ---

    /// <summary>
    /// Represents a node during Dijkstra calculation. Includes tie-breaking cost.
    /// </summary>
    private class DijkstraNode : IComparable<DijkstraNode>
    {
        public Vector3Int Position;
        public int Cost; // Primary cost (distance from source)
        public int TieBreakerCost; // Secondary cost (e.g., distance to exit for destructibles)
        public Vector3Int CameFrom;

        public DijkstraNode(Vector3Int pos, int cost, int tieBreaker, Vector3Int cameFrom)
        {
            Position = pos;
            Cost = cost;
            TieBreakerCost = tieBreaker;
            CameFrom = cameFrom;
        }

        // Compare based on primary cost, then tie-breaker cost
        public int CompareTo(DijkstraNode other)
        {
            int costComparison = Cost.CompareTo(other.Cost);
            if (costComparison == 0)
            {
                return TieBreakerCost.CompareTo(other.TieBreakerCost);
            }
            return costComparison;
        }
    }

    /// <summary>
    /// Performs Dijkstra's algorithm to calculate costs and paths from multiple sources.
    /// </summary>
    /// <param name="startCells">The list of source cells to start from.</param>
    /// <param name="costSoFar">Dictionary to store the calculated minimum cost to reach each cell.</param>
    /// <param name="cameFrom">Dictionary to store the preceding cell in the shortest path found so far.</param>
    /// <param name="avoidAllObstacles">If true, avoids both destructible and indestructible. If false, avoids only indestructible.</param>
    /// <param name="exitCostsForTieBreaking">Optional: Pre-calculated costs to exit, used for tie-breaking when calculating destructible field.</param>
    private void CalculateDijkstra(
        List<Vector3Int> startCells,
        Dictionary<Vector3Int, int> costSoFar,
        Dictionary<Vector3Int, Vector3Int> cameFrom,
        bool avoidAllObstacles,
        Dictionary<Vector3Int, int> exitCostsForTieBreaking = null)
    {
        // Simple Priority Queue simulation using SortedSet for reasonable performance.
        // For extreme performance, a custom Min-Heap implementation would be better.
        SortedSet<DijkstraNode> frontier = new SortedSet<DijkstraNode>();

        // Initialize starting nodes
        foreach (var startCell in startCells)
        {
            if (!groundTilemap.HasTile(startCell)) continue; // Skip starts outside ground

            // Check if start cell itself is blocked according to rules
             bool startBlocked;
             if (avoidAllObstacles) startBlocked = IsCellBlockedByAnyObstacle(startCell);
             else startBlocked = IsCellBlockedByIndestructible(startCell);
             if (startBlocked && !startCells.Contains(startCell)) // Allow starting *on* a target even if blocked
             {
                 Debug.LogWarning($"Dijkstra start cell {startCell} is blocked by relevant obstacles. Skipping.");
                 continue;
             }


            costSoFar[startCell] = 0;
            cameFrom[startCell] = startCell; // Mark start node
            int tieBreaker = 0;
            if (exitCostsForTieBreaking != null)
            {
                // If calculating destructible field, use cost to exit as tie-breaker
                tieBreaker = exitCostsForTieBreaking.TryGetValue(startCell, out int exitCost) ? exitCost : int.MaxValue;
            }

            frontier.Add(new DijkstraNode(startCell, 0, tieBreaker, startCell));
        }

        // Dijkstra Loop
        while (frontier.Count > 0)
        {
            DijkstraNode currentNode = frontier.Min; // Get node with lowest cost (and tie-breaker)
            frontier.Remove(currentNode);
            debug_checkedCells.Add(currentNode.Position); // For debugging
            foreach (Vector3Int neighborCell in GetNeighbors(currentNode.Position))
            {
                // --- Neighbor Validation ---
                if (!groundTilemap.HasTile(neighborCell)) continue; // Must be on ground
                // Check obstacles based on calculation type
                bool blocked;
                if (avoidAllObstacles) blocked = IsCellBlockedByAnyObstacle(neighborCell);
                else blocked = IsCellBlockedByIndestructible(neighborCell); // Allow moving onto/through destructibles

                if (blocked) continue; // Skip blocked neighbors

                // --- Cost Calculation ---
                int newCost = currentNode.Cost + 1; // Assuming uniform cost grid

                // --- Update Neighbor ---
                if (!costSoFar.ContainsKey(neighborCell) || newCost < costSoFar[neighborCell])
                {
                    costSoFar[neighborCell] = newCost;
                    cameFrom[neighborCell] = currentNode.Position;

                    int tieBreaker = 0;
                    if (exitCostsForTieBreaking != null)
                    {
                        tieBreaker = exitCostsForTieBreaking.TryGetValue(neighborCell, out int exitCost) ? exitCost : int.MaxValue;
                    }

                    // Add/Update neighbor in frontier. SortedSet handles uniqueness based on CompareTo.
                    // If a node with the same Position exists but higher cost, removing/re-adding is needed
                    // for sets that don't automatically update priority based on value changes.
                    // Let's find if it exists first. (This adds overhead, proper heap is better)
                    DijkstraNode existingNode = frontier.FirstOrDefault(n => n.Position == neighborCell);
                    if (existingNode != null)
                    {
                        // If new path is better (lower cost or same cost but better tie-breaker)
                        if (newCost < existingNode.Cost || (newCost == existingNode.Cost && tieBreaker < existingNode.TieBreakerCost))
                        {
                            frontier.Remove(existingNode); // Remove old entry
                            frontier.Add(new DijkstraNode(neighborCell, newCost, tieBreaker, currentNode.Position)); // Add new entry
                        }
                    }
                    else
                    {
                         frontier.Add(new DijkstraNode(neighborCell, newCost, tieBreaker, currentNode.Position));
                    }
                }
            }
        }
    }
    void OnDrawGizmos()
    {
        
        DrawDebugGizmosForCell(debug_checkedCells, 0.5f);
    }

    /// <summary>
    /// Finds all ground cells that couldn't reach an exit and identifies all destructible obstacle locations.
    /// </summary>
    private void FindUnreachedAndDestructibles(HashSet<Vector3Int> cellsWithoutExitPath, List<Vector3Int> destructibleCells)
    {
        if (groundTilemap == null) return;

        BoundsInt bounds = groundTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, groundTilemap.origin.z); // Use Z from tilemap origin
                if (groundTilemap.HasTile(cell))
                {
                    // Check if it reached an exit
                    if (!costToExit.ContainsKey(cell)) // If no cost was calculated for it
                    {
                        Debug.Log($"Cell {cell} cannot reach any exit.");
                        cellsWithoutExitPath.Add(cell);
                    }

                    // Check if it's a destructible obstacle
                    if (IsCellDestructible(cell))
                    {
                        destructibleCells.Add(cell);
                    }
                }else
                {
                    Debug.LogWarning($"Cell {cell} is not on the ground tilemap. Skipping.");
                }
            }
        }
         Debug.Log($"Found {cellsWithoutExitPath.Count} cells without exit path and {destructibleCells.Count} destructible cells.");
    }


    /// <summary>
    /// Combines the results of the exit and destructible Dijkstra runs into the final flow field data.
    /// </summary>
    private void CombineFlowFields(
        Dictionary<Vector3Int, int> costToExit, Dictionary<Vector3Int, Vector3Int> cameFromExit,
        Dictionary<Vector3Int, int> costToDestructible, Dictionary<Vector3Int, Vector3Int> cameFromDestructible,
        HashSet<Vector3Int> cellsWithoutExitPath)
    {
        finalFlowField = new Dictionary<Vector3Int, FlowFieldNode>();

        // Iterate through all cells we calculated *any* cost for
        HashSet<Vector3Int> allProcessedCells = new HashSet<Vector3Int>(costToExit.Keys);
        allProcessedCells.UnionWith(costToDestructible.Keys);

        foreach (var cell in allProcessedCells)
        {
             // Skip if not actually on ground (might happen if start/target was slightly off)
            if (!groundTilemap.HasTile(cell)) continue;

            FlowFieldNode node;

            // Priority 1: Can it reach an exit?
            if (costToExit.ContainsKey(cell) && cameFromExit.ContainsKey(cell))
            {
                Vector3Int directionCell = cameFromExit[cell];
                // Direction points FROM neighbor TO current cell for flow field
                Vector3 directionVector = (Vector3)(directionCell - cell); // Keep as vector between cell centers
                node = new FlowFieldNode(directionVector.normalized, costToExit[cell], FlowFieldStatus.ReachesExit);
            }
            // Priority 2: Can it reach a destructible (and couldn't reach an exit)?
            else if (cellsWithoutExitPath.Contains(cell) && costToDestructible.ContainsKey(cell) && cameFromDestructible.ContainsKey(cell))
            {
                 Vector3Int directionCell = cameFromDestructible[cell];
                 Vector3 directionVector = (Vector3)(directionCell - cell);
                 node = new FlowFieldNode(directionVector.normalized, costToDestructible[cell], FlowFieldStatus.ReachesDestructible);
            }
            // Priority 3: It's blocked from both
            else
            {
                node = FlowFieldNode.BlockedNode; // Use the static blocked node
            }

            finalFlowField[cell] = node;
        }

         // Ensure cells identified as unreachable but never processed by destructible search are marked blocked
         foreach(var unreachableCell in cellsWithoutExitPath)
         {
            if(!finalFlowField.ContainsKey(unreachableCell))
            {
                 finalFlowField[unreachableCell] = FlowFieldNode.BlockedNode;
            }
         }
    }


    // --- Helper Methods ---

    private Vector3Int[] GetNeighbors(Vector3Int cell)
    {
        // Add diagonals if desired, but adjust cost calculation in Dijkstra if diagonal cost != cardinal cost
        return new Vector3Int[] {
            cell + Vector3Int.up,
            cell + Vector3Int.down,
            cell + Vector3Int.left,
            cell + Vector3Int.right
        };
    }

    private bool IsCellBlockedByIndestructible(Vector3Int cell)
    {
        if (indestructibleObstacleTilemaps == null) return false;
        foreach (Tilemap map in indestructibleObstacleTilemaps)
        {
            if (map != null && map.HasTile(cell)) return true;
        }
        return false;
    }

    private bool IsCellDestructible(Vector3Int cell)
    {
        if (destructibleObstacleTilemaps == null) return false;
        foreach (Tilemap map in destructibleObstacleTilemaps)
        {
            if (map != null && map.HasTile(cell)) return true;
        }
        return false;
    }

    private bool IsCellBlockedByAnyObstacle(Vector3Int cell)
    {
        return IsCellBlockedByIndestructible(cell) || IsCellDestructible(cell);
    }
    private void DrawDebugGizmosForCell(List<Vector3Int> cells, float gizmoScale = 0.5f)
    {

        // Calculate Gizmo size based on tilemap cell size and scale factor
        Vector3 gizmoSize = groundTilemap.cellSize * Mathf.Clamp01(gizmoScale);
        foreach (Vector3Int cell in cells)
        {
            Vector3 worldPos = groundTilemap.GetCellCenterWorld(cell);
            Gizmos.DrawWireCube(worldPos, gizmoSize);
        }
        


    }
}

// --- Supporting Structures ---

/// <summary>
/// Represents the calculated flow field data for a single cell.
/// </summary>
[System.Serializable] // Make visible in Inspector if needed elsewhere
public struct FlowFieldNode
{
    public Vector3 DirectionToTarget; // Normalized direction vector towards the next cell on the path
    public int Cost;                  // Cost (e.g., distance) to reach the target (exit or destructible)
    public FlowFieldStatus Status;    // Indicates what type of target this node path leads to

    public FlowFieldNode(Vector3 direction, int cost, FlowFieldStatus status)
    {
        DirectionToTarget = direction;
        Cost = cost;
        Status = status;
    }

    // Static property for a default blocked node
    public static readonly FlowFieldNode BlockedNode = new FlowFieldNode(Vector3.zero, int.MaxValue, FlowFieldStatus.Blocked);
}

/// <summary>
/// Indicates the status of a cell within the flow field.
/// </summary>
public enum FlowFieldStatus
{
    Blocked,             // Cannot reach any target
    ReachesExit,         // Path leads towards the nearest exit
    ReachesDestructible  // Path leads towards the nearest destructible (only if exit is unreachable)
}
