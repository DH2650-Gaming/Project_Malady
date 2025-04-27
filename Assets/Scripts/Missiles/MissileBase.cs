using UnityEngine;
using UnityEngine.Tilemaps;
using System;

/// <summary>
/// Base class for missiles in a 2D top-down game. Handles movement,
/// tracking, lifetime, and collision callbacks.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MissileBase : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed of the missile in units per second.")]
    public float missileSpeed = 10f;

    [Tooltip("How quickly the missile can turn towards its target (degrees per second). Only used in tracking mode.")]
    public float turnRate = 180f;

    [Tooltip("Maximum time the missile can exist in seconds before self-destructing.")]
    public float lifetime = 5.0f;

    [Header("Tracking")]
    [Tooltip("The target the missile should track.")]
    public Transform target;

    [Tooltip("Is the missile currently in tracking mode?")]
    public bool isTracking = true;

    // --- protected Variables ---
    protected Rigidbody2D rb;
    protected float timeAlive = 0f;
    protected float damage = 0f;
    protected DamageType damageType = DamageType.None;
    protected bool hasPassedTarget = false;     // Flag to prevent re-engaging after passing
    protected Rigidbody2D targetRigidbody;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0; // Common for top-down 2D

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogError("MissileBase requires a Collider2D component.", this);
        }
    }

    public void Setup(Transform initialTarget, float damage, DamageType damageType, bool startInTrackingMode = true)
    {
        this.target = initialTarget;
        this.damage = damage;
        this.damageType = damageType;
        this.isTracking = startInTrackingMode;
        this.timeAlive = 0f;
        this.hasPassedTarget = false;

        if (target != null)
        {
            targetRigidbody = target.GetComponent<Rigidbody2D>();
        }
        else
        {
            Debug.LogWarning("MissileBase Setup called with null target. Tracking will be disabled.", this);
            isTracking = false;
        }

        //rotate the missile to face the target
        if (target != null)
        {
            Vector2 directionToTarget = ((Vector2)target.position - rb.position).normalized;
            float targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f; // -90 because sprite 'up' is usually Y+
            rb.MoveRotation(targetAngle);
        }
        // Set initial velocity if not tracking from the start
        if (!isTracking)
        {
            //launch the missile in the direction it is facing
            rb.linearVelocity = transform.up * missileSpeed;

            
        }
    }


    protected virtual void FixedUpdate()
    {
        // --- Lifetime Check ---
        timeAlive += Time.fixedDeltaTime;
        if (timeAlive >= lifetime)
        {
            DestroyMissile();
            return; // Stop further processing
        }

        // --- Movement Logic ---
        if (isTracking && target != null && !hasPassedTarget)
        {
            TrackTarget();
        }
        else
        {
            // Non-tracking mode or target lost/passed
            // Continue with existing velocity (set during tracking or initial setup)
            // Ensure speed is maintained if needed (velocity can degrade due to drag etc.)
             rb.linearVelocity = rb.linearVelocity.normalized * missileSpeed;
        }
    }

    protected virtual void TrackTarget()
    {
        // --- Calculate Direction to Target ---
        Vector2 directionToTarget = ((Vector2)target.position - rb.position).normalized;

        // --- Check if Target is Behind ---
        float dotProduct = Vector2.Dot(transform.up, directionToTarget);
        if (dotProduct < 0) // Adjust threshold slightly if needed (e.g., < -0.1f)
        {
            isTracking = false;
            hasPassedTarget = true;
            rb.linearVelocity = transform.up * missileSpeed; // Ensure it keeps moving forward
            return; // Skip rotation logic for this frame
        }


        // --- Rotate Towards Target ---
        float targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f; // -90 because sprite 'up' is usually Y+
        float currentAngle = rb.rotation;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnRate * Time.fixedDeltaTime);
        rb.MoveRotation(newAngle);

        // --- Apply Forward Velocity ---
        rb.linearVelocity = transform.up * missileSpeed;
    }


    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        //Get the object that was hit
        GameObject hitObject = other.gameObject;
        GameMaster gm= GameMaster.Instance;
        bool isEnemy = gm.spawnedEnemies.Contains(hitObject);
        bool isHero = gm.playerhero == hitObject;
        Tilemap towers = gm.destructibleObstacleTilemaps[0];
        bool isTower = towers != null && towers.HasTile(towers.WorldToCell(hitObject.transform.position));
        bool isObsticle = false;
        foreach (Tilemap tilemap in gm.indestructibleObstacleTilemaps)
        {
            if (tilemap.HasTile(tilemap.WorldToCell(hitObject.transform.position)))
            {
                isObsticle = true;
                break;
            }
        }
        // Implement corresponding logic for the hit object
    }

    protected virtual void DestroyMissile()
    {
        // Add particle effects, sound effects here if needed before destroying
        Destroy(gameObject);
    }

}


// --- Example Usage (Tower Script) ---
/*
using UnityEngine;
using System; // For Action

public class Tower : MonoBehaviour
{
    public GameObject missilePrefab; // Assign your Missile Prefab in the Inspector
    public Transform targetEnemy;    // Logic to find/assign the target

    void FireMissile()
    {
        if (missilePrefab == null || targetEnemy == null)
        {
            Debug.LogWarning("Missing missile prefab or target!");
            return;
        }

        GameObject missileInstance = Instantiate(missilePrefab, transform.position, transform.rotation);
        MissileBase missileScript = missileInstance.GetComponent<MissileBase>();

        if (missileScript != null)
        {
            // *** MODIFIED: Pass the updated HandleMissileHit method ***
            missileScript.Setup(targetEnemy, HandleMissileHit, true);
        }
        else
        {
            Debug.LogError("Missile prefab does not contain MissileBase script!", missilePrefab);
            Destroy(missileInstance);
        }
    }

    // *** MODIFIED: Function now accepts the impact angle ***
    void HandleMissileHit(GameObject hitObject, float impactAngle)
    {
        Debug.Log($"Missile hit: {hitObject.name} at angle: {impactAngle} degrees");

        // --- Add your damage logic here ---
        // You can now use the impactAngle if needed for effects or physics
        // E.g., Instantiate particle effect rotated by impactAngle
        // E.g., Apply force based on the angle

        EnemyHealth enemyHealth = hitObject.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(10); // Example damage amount
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && targetEnemy != null)
        {
            FireMissile();
        }
    }
}
*/
