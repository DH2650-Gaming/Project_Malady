using UnityEngine;
using UnityEngine.Tilemaps; // Required for WorldToCell

/// <summary>
/// Controls a basic enemy unit's health and movement based on the Pathfinder.
/// Designed to be extended for more specific enemy types.
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("Enemy Stats")]
    [Tooltip("Maximum health points.")]
    [SerializeField] private int maxHealth = 100;
    [Tooltip("Movement speed in units per second.")]
    [SerializeField] private float moveSpeed = 2.0f;

    [Header("References")]
    [Tooltip("Reference to the ground tilemap for cell calculations. If null, attempts to get from Pathfinder.")]
    [SerializeField] private Tilemap groundTilemap;

    // --- Private State ---
    private int currentHealth;
    private Pathfinder pathfinder; // Cached reference to the pathfinder

    void Start()
    {
        currentHealth = maxHealth;

        // Get singleton instance
        pathfinder = Pathfinder.Instance;
        if (pathfinder == null)
        {
            Debug.LogError($"Enemy {gameObject.name} cannot find Pathfinder instance!", this);
            enabled = false; // Disable script if pathfinder is missing
            return;
        }

        // Attempt to get ground tilemap reference if not set
        if (groundTilemap == null)
        {
            groundTilemap = pathfinder.groundTilemap;
            if (groundTilemap == null)
            {
                 Debug.LogError($"Enemy {gameObject.name} needs a Ground Tilemap reference, and couldn't get it from Pathfinder!", this);
                 enabled = false; // Disable script if tilemap is missing
                 return;
            }
        }
    }

    void Update()
    {
        // Only handle movement if the pathfinder is ready
        if (pathfinder != null) // Check instance validity
        {
            HandleMovement();
        }
    }

    /// <summary>
    /// Handles querying the flow field and moving the enemy accordingly.
    /// </summary>
    protected virtual void HandleMovement()
    {
        if (groundTilemap == null || pathfinder == null) return; // Safety check

        // 1. Get Current Cell Position
        // Convert world position to the tilemap's cell coordinate
        Vector3Int currentCell = groundTilemap.WorldToCell(transform.position);

        // 2. Query Flow Field
        FlowFieldNode node = pathfinder.GetFlowFieldNode(currentCell);

        // 3. Determine Action based on Flow Field Status
        switch (node.Status)
        {
            case FlowFieldStatus.ReachesExit:
            case FlowFieldStatus.ReachesDestructible:
                // Move towards the direction indicated by the flow field node
                // Note: Assumes node.DirectionToTarget points where the unit should go.
                // For smoother movement, consider moving towards the center of the *next* cell
                // rather than just applying the direction vector directly.
                Vector3 targetDirection = node.DirectionToTarget;

                // Simple movement: move in the calculated direction
                // For grid-based games, moving towards the center of the next cell might be better:
                // Vector3 targetWorldPos = groundTilemap.GetCellCenterWorld(currentCell + Vector3Int.RoundToInt(targetDirection));
                // transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);

                // Simplest implementation: Move along the direction vector
                // Ensure the direction is not zero before normalizing or moving
                 if (targetDirection != Vector3.zero)
                 {
                    // Optional: Rotate to face movement direction
                    // transform.up = targetDirection; // Or transform.forward depending on sprite orientation

                    transform.position += targetDirection.normalized * moveSpeed * Time.deltaTime;
                 }
                 else if (node.Cost == 0) // Reached the target (exit or destructible)
                 {
                     HandleReachedTarget(node.Status);
                 }

                break;

            case FlowFieldStatus.Blocked:
                // Enemy cannot reach any target from this cell
                // Implement specific behavior (e.g., wait, play idle animation, attack nearby if possible)
                // For now, just stop moving.
                 // Debug.Log($"Enemy {gameObject.name} is blocked at {currentCell}.");
                break;
        }
    }

    /// <summary>
    /// Called when the enemy reaches the cell designated as the target (cost 0).
    /// </summary>
    /// <param name="targetStatus">The type of target reached.</param>
    protected virtual void HandleReachedTarget(FlowFieldStatus targetStatus)
    {
        // Example: If it reached an exit, destroy self
         if (targetStatus == FlowFieldStatus.ReachesExit)
         {
             Debug.Log($"Enemy {gameObject.name} reached an exit.");
             Destroy(gameObject); // Or trigger scoring, etc.
         }
         // If it reached a destructible, maybe start attacking it (needs separate logic)
         else if (targetStatus == FlowFieldStatus.ReachesDestructible)
         {
             Debug.Log($"Enemy {gameObject.name} reached a destructible target cell.");
             // Implement attacking logic here if needed
             Destroy(gameObject);
         }
    }


    /// <summary>
    /// Reduces the enemy's health by a specified amount.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public virtual void TakeDamage(int amount)
    {
        if (amount <= 0) return; // No negative damage

        currentHealth -= amount;
        Debug.Log($"Enemy {gameObject.name} took {amount} damage, {currentHealth}/{maxHealth} HP remaining.");

        // Optional: Add visual feedback (flash color, particle effect)

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles the enemy's death sequence.
    /// </summary>
    protected virtual void Die()
    {
        Debug.Log($"Enemy {gameObject.name} has died.");
        // Optional: Play death animation, spawn particle effects, drop loot, notify game manager

        // Remove the enemy from the game
        Destroy(gameObject);
    }

     // --- Gizmos for Debugging ---
     void OnDrawGizmos()
     {
        // Draw the path direction if the pathfinder is initialized
        if (Application.isPlaying && pathfinder != null && groundTilemap != null)
        {
             Vector3Int currentCell = groundTilemap.WorldToCell(transform.position);
             FlowFieldNode node = pathfinder.GetFlowFieldNode(currentCell);

             if(node.Status != FlowFieldStatus.Blocked && node.DirectionToTarget != Vector3.zero)
             {
                 Gizmos.color = (node.Status == FlowFieldStatus.ReachesExit) ? Color.green : Color.yellow;
                 Vector3 startPos = transform.position;
                 Vector3 endPos = startPos + node.DirectionToTarget; // Show the direction vector
                 Gizmos.DrawLine(startPos, endPos);
                 Gizmos.DrawSphere(endPos, 0.1f); // Mark the direction end
             } else if (node.Status == FlowFieldStatus.Blocked)
             {
                 Gizmos.color = Color.red;
                 Gizmos.DrawWireSphere(transform.position, 0.3f); // Indicate blocked status
             }
        }
     }
}
