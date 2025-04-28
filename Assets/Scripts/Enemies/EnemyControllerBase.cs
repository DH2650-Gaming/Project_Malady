using UnityEngine;
using UnityEngine.Tilemaps; // Required for WorldToCell

/// <summary>
/// Controls a basic enemy unit's health and movement based on the Pathfinder.
/// Designed to be extended for more specific enemy types.
/// </summary>
public class EnemyController : UnitBase
{
    public float attackDamage = 10f;
    public float attactFrequency = 1f;
    public float attackRange = 0.05f;
    public float aggroRange = 2f;
    public int bounty = 1;
    // --- Private State ---
    private GameMaster gm;
    private Vector2 targetPosition;
    private Tilemap groundTilemap;


    void Start()
    {
        currentHealth = maxHealth;
        unitType = UnitType.enemyunit;
        _unitBody = GetComponent<Rigidbody2D>();
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
        targetPosition = _unitBody.position;
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

        Vector2 currentWorldPos = _unitBody.position;
        Vector2 direction = targetPosition - currentWorldPos;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = _unitBody.rotation;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, 180f * Time.fixedDeltaTime);
        _unitBody.MoveRotation(newAngle);
        if ((targetPosition - currentWorldPos).sqrMagnitude > 0.01f)
        {
            _unitBody.MovePosition(Vector2.MoveTowards(currentWorldPos, targetPosition, moveSpeed * Time.deltaTime));
            return;
        }
        Vector3Int currentCell = groundTilemap.WorldToCell(currentWorldPos);
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
                    Vector3Int nextCell = currentCell + Vector3Int.RoundToInt(node.DirectionToTarget);
                    Vector2 targetWorldPos = (Vector3)currentWorldPos + groundTilemap.GetCellCenterWorld(nextCell) - groundTilemap.GetCellCenterWorld(currentCell);
                    targetPosition = targetWorldPos;
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
            gm.EnemyReachedExit();
            // Do not call Die() cuz we don't want to play death animation
            gm.currentGold += bounty;
            Destroy(gameObject);
         }
         // If it reached a destructible, maybe start attacking it (needs separate logic)
         else if (targetStatus == FlowFieldStatus.ReachesDestructible)
         {
             Debug.Log($"Enemy {gameObject.name} reached a destructible target cell.");
             Destroy(gameObject);
         }
    }

    public virtual void TakeDamage(float damage, float missileAngle, DamageType damageType)
    {
        if (damage <= 0) return; // No negative damage

        currentHealth -= damage;

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
        gm.currentGold += bounty;
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
