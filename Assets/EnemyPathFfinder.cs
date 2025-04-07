using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections; // Required for Coroutines

public class EnemyPathFInder : MonoBehaviour
{
    [Header("Pathfinding Settings")]
    [SerializeField] private Tilemap groundTilemap; // Assign your Ground Tilemap
    [SerializeField] private Tilemap obstacleTilemap; // Assign your Obstacle Tilemap (Optional)
    [SerializeField] private Transform target;      // Assign the player or target object to follow
    [SerializeField] private float moveSpeed = 3f;
    // How often to recalculate the path (in seconds)
    [SerializeField] private float pathRecalculationRate = 0.5f;
     // How close the enemy needs to be to a path node (cell center) to move to the next one
    [SerializeField] private float waypointReachedThreshold = 0.05f;


    private List<Vector3> currentPath;
    private int currentPathIndex = 0;
    private bool isMoving = false;
    private Coroutine followPathCoroutine;
    private Coroutine recalculatePathCoroutine;


    void Start()
    {
        if (groundTilemap == null || target == null)
        {
            Debug.LogError("Ground Tilemap or Target not assigned!", this);
            enabled = false;
            return;
        }
         // Start recalculating the path periodically
         recalculatePathCoroutine = StartCoroutine(RecalculatePath());
    }

    // Coroutine to periodically recalculate the path
    IEnumerator RecalculatePath()
    {
        while (true)
        {
            CalculateAndFollowPath();
            yield return new WaitForSeconds(pathRecalculationRate);
        }
    }


    void CalculateAndFollowPath()
    {
        // Convert world positions to cell positions
        Vector3Int startCell = groundTilemap.WorldToCell(transform.position);
        Vector3Int targetCell = groundTilemap.WorldToCell(target.position);

        // Find the path using the static Pathfinder class
        currentPath = Pathfinder.FindPath(groundTilemap, obstacleTilemap, startCell, targetCell);

         // Stop any existing movement coroutine before starting a new one
        if (followPathCoroutine != null)
        {
            StopCoroutine(followPathCoroutine);
            isMoving = false;
        }

        if (currentPath != null && currentPath.Count > 0)
        {
            currentPathIndex = 0;
             // Start following the new path
            followPathCoroutine = StartCoroutine(FollowPath());
        }
         else {
             // No path found or path is empty, stop moving
             isMoving = false;
         }
    }


    // Coroutine to move the enemy along the calculated path
    IEnumerator FollowPath()
    {
        isMoving = true;

        while (currentPathIndex < currentPath.Count)
        {
            Vector3 targetPosition = currentPath[currentPathIndex];

            // Move towards the next node (cell center) in the path
            while (Vector3.Distance(transform.position, targetPosition) > waypointReachedThreshold)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                 // Optional: Face direction (ensure sprite isn't rotated strangely if using rotation)
                 Vector3 direction = (targetPosition - transform.position).normalized;
                 // Simple sprite flipping:
                 // if (direction.x > 0.1f) GetComponent<SpriteRenderer>().flipX = false;
                 // else if (direction.x < -0.1f) GetComponent<SpriteRenderer>().flipX = true;

                yield return null; // Wait for the next frame
            }

            // Reached the vicinity of the current path node, snap to exact position (optional but good for grid alignment)
            transform.position = targetPosition;

            // Move to the next node in the path
            currentPathIndex++;
        }

         // Reached the end of the path
         Debug.Log("Path finished!");
         isMoving = false;
         // Optional: Trigger recalculation immediately if target might have moved significantly
         // CalculateAndFollowPath();
    }

     // Optional: Visualize the calculated path in the Scene view
    void OnDrawGizmos()
    {
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.green;
             // Draw line from current position to first node
             Gizmos.DrawLine(transform.position, currentPath[0]);
            // Draw lines connecting path nodes
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
             // Draw spheres at each node
             Gizmos.color = Color.blue;
             foreach(Vector3 node in currentPath) {
                 Gizmos.DrawWireSphere(node, 0.1f);
             }
        }
    }

     // Stop coroutines when the object is disabled or destroyed
    void OnDisable() {
        if(followPathCoroutine != null) StopCoroutine(followPathCoroutine);
        if(recalculatePathCoroutine != null) StopCoroutine(recalculatePathCoroutine);
        isMoving = false;
    }
}