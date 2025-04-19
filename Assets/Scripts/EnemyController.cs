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
    
    private Tilemap groundTilemap;

    // --- Private State ---
    private int currentHealth;
    private GameMaster gm;

    void Start()
    {
        currentHealth = maxHealth;

        // Get singleton instance
        gm = GameMaster.Instance;
        if (gm == null)
        {
            Debug.LogError($"Enemy {gameObject.name} cannot find GameMaster instance!", this);
            enabled = false;
            return;
        }

        groundTilemap = gm.groundTilemap;
        if (groundTilemap == null)
        {
            Debug.LogError($"Enemy {gameObject.name} needs a Ground Tilemap reference, and couldn't get it from GameMaster!", this);
            enabled = false; // Disable script if tilemap is missing
            return;
        }
    }

    void Update()
    {
        if (gm != null) // Check instance validity
        {
            HandleMovement();
        }
    }

    /// <summary>
    /// Handles querying the flow field and moving the enemy accordingly.
    /// </summary>
    protected virtual void HandleMovement()
    {
        if (groundTilemap == null || gm == null) return; // Safety check

        Vector3Int currentCell = groundTilemap.WorldToCell(transform.position);
        FlowFieldNode node = gm.pathfinderInstance.GetFlowFieldNode(currentCell);

        switch (node.Status)
        {
            case FlowFieldStatus.ReachesExit:
            case FlowFieldStatus.ReachesDestructible:
                // Check if we are already at the target (cost 0)
                if (node.Cost == 0) {
                    HandleReachedTarget(node.Status);
                    return; // Stop moving if already at target
                }

                // Ensure there's a valid direction to move
                if (node.DirectionToTarget != Vector3.zero)
                {
                    // --- Refined Movement: Move towards next cell center ---
                    // Calculate the next cell based on the direction vector
                    // Assumes DirectionToTarget points from currentCell towards the next cell
                    Vector3Int nextCell = currentCell + Vector3Int.RoundToInt(node.DirectionToTarget);

                    // Get the world position of the center of the next cell
                    Vector3 targetWorldPos = groundTilemap.GetCellCenterWorld(nextCell);

                    // Move the enemy's transform towards that target position
                    transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);

                    // Optional: Rotate to face the target position (if needed)
                    // Vector3 moveDirection = (targetWorldPos - transform.position).normalized;
                    // if (moveDirection != Vector3.zero) {
                    //    Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, moveDirection); // Adjust axis based on 2D/3D setup
                    //    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                    // }
                    // --- End Refined Movement ---
                }
                // else: Direction is zero, but cost is not 0? Should not happen with correct CombineFields logic.
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
        if (Application.isPlaying && gm != null && groundTilemap != null)
        {
             Vector3Int currentCell = groundTilemap.WorldToCell(transform.position);
             FlowFieldNode node = gm.pathfinderInstance.GetFlowFieldNode(currentCell);

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
