using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;
using UnityEngine.UI; // Required for WorldToCell

/// <summary>
/// Controls a basic enemy unit's health and movement based on the Pathfinder.
/// Designed to be extended for more specific enemy types.
/// </summary>
public class EnemyController : UnitBase
{
    public float attackDamage = 10f;
    public DamageType attackDamageType = DamageType.Physical;
    public float attactFrequency = 1f;
    public float attackRange = 0.1f;
    public float aggroRange = 2f;
    public int bounty = 1;
    // --- Private State ---
    private GameMaster gm;
    private Vector2 currentWorldPos;
    private Vector2 targetPosition;
    private Tilemap groundTilemap;
    private Tilemap towerTilemap;
    private bool isAttacking = false;
    private GameObject target;
    private LayerMask targetingUnitLayer;
    private LayerMask targetingTowerLayer;
    private FlowFieldNode currentNode;
    private Vector2 unitOffset;
    private float timeSinceLastAttack = 0f;
    [SerializeField] private Animator animator;
    private bool isMoving;
    protected float originalMoveSpeed;
    protected float rootDebuffDuration = 0f;
    private Vector2 lastDirection; // for die animation

    //----Health bar ---
    [SerializeField] private Image enemyHealthBar;

   void Start()
    {
        currentHealth = maxHealth;

        enemyHealthBar.fillAmount = currentHealth/maxHealth;

        unitType = UnitType.enemyunit;
        _unitBody = GetComponent<Rigidbody2D>();
        _unitCollider = GetComponent<Collider2D>();
        // Get singleton instance
        gm = GameMaster.Instance;
        if (gm == null)
        {
            Debug.LogError($"Enemy {gameObject.name} cannot find GameMaster instance!", this);
            enabled = false;
            return;
        }

        groundTilemap = gm.groundTilemap;
        towerTilemap = gm.destructibleObstacleTilemaps[0];
        if (groundTilemap == null)
        {
            Debug.LogError($"Enemy {gameObject.name} needs a Ground Tilemap reference, and couldn't get it from GameMaster!", this);
            enabled = false; // Disable script if tilemap is missing
            return;
        }
        currentWorldPos = _unitBody.position;
        targetPosition = _unitBody.position;
        unitOffset = (Vector2) groundTilemap.GetCellCenterWorld(groundTilemap.WorldToCell(currentWorldPos)) - currentWorldPos;
        targetingUnitLayer = LayerMask.GetMask("PlayerUnits");
        targetingTowerLayer = LayerMask.GetMask("Towers");
        originalMoveSpeed = moveSpeed;
    }

    protected virtual bool isTargetValid(GameObject target)
    {
        if (target == null)
            return false;
        if (target.GetComponent<TowerBase>() != null && target.GetComponent<TowerBase>().currentHealth > 0)
            return true;
        if (target.GetComponent<UnitBase>() == null)
            return false;
        if (target.GetComponent<UnitBase>().currentHealth <= 0)
            return false;
        if (target.GetComponent<Collider2D>() == null || target.GetComponent<Collider2D>().Distance(_unitCollider).distance > aggroRange)
            return false;
        FlowFieldNode targetNode = gm.pathfinderInstance.GetFlowFieldNode(groundTilemap.WorldToCell(target.transform.position));
        if (targetNode.Status == FlowFieldStatus.Blocked)
            return false;
        if (targetNode.Cost > currentNode.Cost)
            return false;
        return true;
    }

    protected virtual bool findTargetinCell(Vector3Int cellPos)
    {
        Collider2D[] collidersInRange;
        Vector2 cellWorldPos = groundTilemap.GetCellCenterWorld(cellPos);
        Vector2 cellSize = groundTilemap.cellSize;
        collidersInRange = Physics2D.OverlapBoxAll(cellWorldPos, cellSize, 0f, targetingUnitLayer);
        if (collidersInRange.Length == 0)
            return false;

        GameObject targetcandidate = null;
        bool foundTarget = false;
        float minCost = float.MaxValue;

        // --- Filter Results ---
        foreach (Collider2D col in collidersInRange)
        {
            if (col.gameObject == this.gameObject)
                continue;
            GameObject go = col.gameObject;
            if(!isTargetValid(go))
                continue;
            float distance = _unitCollider.Distance(col).distance;
            if(distance < minCost)
            {
                minCost = distance;
                targetcandidate = go;
                foundTarget = true;
            }
        }
        if (foundTarget)
        {
            target = targetcandidate;
        }
        return foundTarget;
    }

    Vector2 getApproachVector(GameObject target)
    {
        Collider2D targetCollider = target.GetComponent<Collider2D>();
        if (targetCollider == null)
            return Vector2.zero;
        Vector2 edgePoint = targetCollider.ClosestPoint(currentWorldPos);
        Vector2 directionOut = (edgePoint - (Vector2)target.transform.position).normalized;
        float unitRelevantExtent = Mathf.Max(_unitCollider.bounds.size.x, _unitCollider.bounds.size.y) / 2f;
        //Debug.Log("Unit scale: " + unitRelevantExtent*2f);
        return edgePoint + (unitRelevantExtent + attackRange) * directionOut;
    }

    void Update()
    {
        currentWorldPos = _unitBody.position;
       
        if(!isTargetValid(target))
        {
            target = null;
            isAttacking = false;
        }
        Vector3Int currentCell = groundTilemap.WorldToCell(currentWorldPos);
        if (target == null){
            if (findTargetinCell(currentCell)){
                isAttacking = true;
            }
        }
        currentNode = gm.pathfinderInstance.GetFlowFieldNode(currentCell);
        if (currentNode.Cost == 0)
        {
            HandleReachedTarget(currentNode.Status);
            return;
        }
        if (target == null && currentNode.DirectionToTarget != Vector3.zero)
        {
            Vector3Int nextCell = currentCell + Vector3Int.RoundToInt(currentNode.DirectionToTarget);
            Vector2 targetWorldPos = unitOffset + (Vector2) groundTilemap.GetCellCenterWorld(nextCell);
            targetPosition = targetWorldPos;
            if (towerTilemap != null && towerTilemap.HasTile(nextCell))
            {
                Collider2D[] collidersInRange = Physics2D.OverlapCircleAll(groundTilemap.GetCellCenterWorld(nextCell), 0.1f, targetingTowerLayer);
                foreach (Collider2D col in collidersInRange)
                {
                    if (col.gameObject.GetComponent<TowerBase>() != null){
                        target = col.gameObject;
                        isAttacking = true;
                        break;
                    }
                }
                if (target == null)
                {
                    Debug.LogWarning($"Enemy {gameObject.name} found a tower but no valid target!", this);
                }
            }
            else
            {
                if (findTargetinCell(nextCell))
                {
                    isAttacking = true;
                }
            }
        }
        if (isAttacking)
        {
            if (target != null)
            {
                targetPosition = getApproachVector(target);
            }
            else
            {
                isAttacking = false;
            }
        }
    }

   
    void FixedUpdate()
    {
        HandleMovement();
        
        rootDebuffDuration -= Time.fixedDeltaTime;
        if (rootDebuffDuration <= 0f)
        {
            rootDebuffDuration = 0f;
            moveSpeed = originalMoveSpeed;
        }
        if (rootDebuffDuration > 0f)
        {
            return;
        }
        timeSinceLastAttack += Time.fixedDeltaTime;
        if (isAttacking)
        {
            if (timeSinceLastAttack >= attactFrequency && (targetPosition - currentWorldPos).sqrMagnitude <= 0.01f)
            {
                timeSinceLastAttack = 0f;
                if (target.GetComponent<UnitBase>() != null)
                {
                    target.GetComponent<UnitBase>().TakeDamage(attackDamage, _unitBody.rotation, attackDamageType);
                }
                else if (target.GetComponent<TowerBase>() != null)
                {
                    target.GetComponent<TowerBase>().TakeDamage(attackDamage, _unitBody.rotation, attackDamageType);
                }
            }
        }
    }

    public void ApplyRootDebuff(float duration)
    {
        if (duration <= rootDebuffDuration) return;

        rootDebuffDuration = duration;
        moveSpeed = 0f;
    }
    /// <summary>
    /// Handles querying the flow field and moving the enemy accordingly.
    /// </summary>
    protected virtual void HandleMovement()
    {
        Vector2 direction = targetPosition - currentWorldPos;

        isMoving = direction.magnitude > 0.01f && moveSpeed > 0f;
        //Debug.Log(isMoving);
        animator.SetBool("isMoving", isMoving);

        if (isMoving)
        {

            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            float currentAngle = _unitBody.rotation;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, 180f * Time.fixedDeltaTime);
            //_unitBody.MoveRotation(newAngle);
            lastDirection = direction;
            if ((targetPosition - currentWorldPos).sqrMagnitude > 0.01f)
            {
                _unitBody.MovePosition(Vector2.MoveTowards(currentWorldPos, targetPosition, moveSpeed * Time.fixedDeltaTime));
            }
            animator.SetFloat("Direction", GetDirectionIndex(direction));
           
        }
        else
        {
            animator.SetFloat("Direction", GetDirectionIndex(lastDirection));
        }

        //Debug.Log(gameObject.name+" "+GetDirectionIndex(direction));

    }


    protected int GetDirectionIndex(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x > 0 ? 3 : 2; // Right=3, Left=2
        else
            return dir.y > 0 ? 0 : 1; // Up=0, Down=1
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

    override public void TakeDamage(float damage, float missileAngle, DamageType damageType)
    {
        if (damage <= 0) return; // No negative damage

        Debug.Log("currentHealth" + currentHealth);

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        enemyHealthBar.fillAmount = currentHealth / maxHealth;
    }

    /// <summary>
    /// Handles the enemy's death sequence.
    /// </summary>
    override protected void Die()
    {
        Debug.Log($"Enemy {gameObject.name} has died.");

        isMoving = false;
        animator.SetBool("isMoving", false);


        animator.SetFloat("Direction", GetDirectionIndex(lastDirection));


        // Optional: Play death animation, spawn particle effects, drop loot, notify game manager
        gm.AddGold(bounty);
        // Remove the enemy from the game
        Destroy(gameObject);

        //StartCoroutine(DestroyAfterAnimation());
    }

    /// <summary>
    /// Get the time to play death animation, then destroy enemies
    /// </summary>
    //private IEnumerator DestroyAfterAnimation()
    //{
       
    //    Debug.Log("Play die animation");

    //    // wait to play die animation     
    //    yield return new WaitForSeconds(1f);
    //    Destroy(gameObject);
    //}

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
