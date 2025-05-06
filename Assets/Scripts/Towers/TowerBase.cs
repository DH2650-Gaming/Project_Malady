using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;


[RequireComponent(typeof(Collider2D))]
public class TowerBase : MonoBehaviour
{

    [Header("Info")]
    public string unitName = "Undefined";
    public string additionalTooltip = "";
    public Sprite unitIcon;
    public GameObject projectilePrefab; 
    public int cost = 5;
    [Header("Combat")]
    public float range = 3f;
    public float minimumRange = 0f;
    public bool squareRange = false;
    public float hitDamage = 10f;
    public DamageType damageType = DamageType.None;
    public float hitCooldown = 0.1f;
    public float maxHealth = 500;
    [ReadOnly] public float currentHealth = 0f;
    [Header("Magazine")]
    public bool useMagazine = false;
    public bool roundReload = false;
    public int magazineSize = 5;
    [Tooltip("When Round Reload is false, this delay will be used if magazine is not empty to instantly reload the turret. When true this is the delay before reloading starts")]
    public float reloadTimer1 = 2f;
    [Tooltip("When Round Reload is false, this timer will be used if magazine is empty, when true this is the time delay between each round")]
    public float reloadTimer2 = 1f;

    private GameObject turret;
    private GameObject _target;
    private GameMaster gm;
    private float timeSinceLastFire = 0f;
    private float timeSinceLastReload = 0f;
    private LayerMask targetingLayer;
    private LayerMask obstacleLayer;
    private Collider2D thisCollider;
    private int curmagazineSize = 0;
    private bool isReloading = false;
    private float gridscale = 0f;
    protected virtual void Start()
    {
        gm = GameMaster.Instance;
        currentHealth = maxHealth;
        curmagazineSize = magazineSize;
        turret = transform.Find("turret").gameObject;
        targetingLayer = LayerMask.GetMask("EnemyUnits");
        obstacleLayer = LayerMask.GetMask("Towers", "Obstacles");
        thisCollider = GetComponent<Collider2D>();
        gridscale = gm.groundTilemap.transform.localScale.x;
        range *= gridscale;
        minimumRange *= gridscale;
        if (gm == null)
        {
            Debug.LogError("TowerBase requires a GameMaster instance.", this);
            Destroy(gameObject);
        }
        if (turret == null)
        {
            Debug.LogError("TowerBase requires a Turret child object.", this);
            Destroy(gameObject);
        }
        if (targetingLayer == 0)
        {
            Debug.LogError("TowerBase requires a valid targeting layer mask.", this);
            Destroy(gameObject);
        }
        
    }

    protected virtual bool isTargetValid(GameObject target)
    {
        if (target == null)
            return false;
        if (target.GetComponent<UnitBase>() == null)
            return false;
        // if (target.GetComponent<UnitBase>().unitType != UnitType.enemyunit && target.GetComponent<UnitBase>().unitType != UnitType.enemyunit2 && target.GetComponent<UnitBase>().unitType != UnitType.enemyhero)
        //     return false;
        if (target.GetComponent<UnitBase>().currentHealth <= 0)
            return false;
        Collider2D targetCollider = target.GetComponent<Collider2D>();
        if (targetCollider == null)
            return false;
        if (targetCollider.Distance(thisCollider).distance > range)
            return false;
        if (targetCollider.Distance(thisCollider).distance < minimumRange)
            return false;
        if (projectilePrefab != null)
        {
            RaycastHit2D hit = Physics2D.Raycast(turret.transform.position + (target.transform.position - turret.transform.position).normalized, target.transform.position - turret.transform.position, range, obstacleLayer);
            if (hit.collider != null && hit.collider.gameObject != target)
            {
                return false;
            }
        }
        return true;
    }
    protected virtual bool findTarget()
    {
        Collider2D[] collidersInRange;
        if (squareRange)
        {
            collidersInRange = Physics2D.OverlapBoxAll(transform.position, new Vector2(range * 2, range * 2), 0f, targetingLayer);
        }
        else
        {
            collidersInRange = Physics2D.OverlapCircleAll(transform.position, range, targetingLayer);
        }

        GameObject targetcandidate = null;
        bool foundTarget = false;
        float maxdis = 0f;
        float minCost = float.MaxValue;

        // --- Filter Results ---
        foreach (Collider2D col in collidersInRange)
        {
            if (col.gameObject == this.gameObject)
                continue;
            targetcandidate = col.gameObject;
            if(!isTargetValid(targetcandidate))
                continue;
            float distance = thisCollider.Distance(col).distance;
            float flowfieldcost = gm.pathfinderInstance.GetFlowFieldNode(gm.groundTilemap.WorldToCell(targetcandidate.transform.position)).Cost;
            if(flowfieldcost < minCost)
            {
                minCost = flowfieldcost;
                maxdis = distance;
                _target = targetcandidate;
                foundTarget = true;
            }else if (flowfieldcost == minCost && distance > maxdis)
            {
                maxdis = distance;
                _target = targetcandidate;
                foundTarget = true;
            }

        }
        return foundTarget;
    }

    protected virtual void Update()
    {
        timeSinceLastFire += Time.deltaTime;
        if(useMagazine)
        {
            timeSinceLastReload += Time.deltaTime;
            if(!roundReload)
            {
                if (curmagazineSize <= 0 && timeSinceLastReload >= reloadTimer2)
                {
                    curmagazineSize = magazineSize;
                    timeSinceLastReload = 0f;
                }else if (curmagazineSize < magazineSize && timeSinceLastReload >= reloadTimer1)
                {
                    curmagazineSize = magazineSize;
                    timeSinceLastReload = 0f;
                }
            }else
            {
                if (timeSinceLastReload >= reloadTimer1 && curmagazineSize < magazineSize)
                {
                    isReloading = true;
                    curmagazineSize++;
                    timeSinceLastReload = 0f;
                }
                if (isReloading && curmagazineSize < magazineSize && timeSinceLastReload >= reloadTimer2)
                {
                    curmagazineSize++;
                    timeSinceLastReload = 0f;
                }
                if (isReloading && curmagazineSize >= magazineSize)
                {
                    isReloading = false;
                    timeSinceLastReload = 0f;
                }
                
            }
        }
        if (!isTargetValid(_target))
        {
            _target = null;
        }
        if (_target == null)
        {
            if(!findTarget())
            {
                return;
            }
            timeSinceLastFire = hitCooldown;
        }
        // --- Rotate Turret ---
        Vector2 directionToTarget = _target.transform.position - turret.transform.position;
        float targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
        turret.transform.rotation = Quaternion.RotateTowards(
            turret.transform.rotation,
            targetRotation,
            360f * Time.deltaTime
        );
        float angleDifference = Quaternion.Angle(turret.transform.rotation, targetRotation);
        // --- Fire ---
        if(angleDifference < 5f)
        {
            if (!useMagazine){
                if (timeSinceLastFire >= hitCooldown)
                {
                    openFire();
                    timeSinceLastFire = 0f;
                }
            }else{
                if (curmagazineSize > 0)
                {
                    if (timeSinceLastFire >= hitCooldown)
                    {
                        openFire();
                        isReloading = false;
                        timeSinceLastFire = 0f;
                        timeSinceLastReload = 0f;
                        curmagazineSize--;
                    }
                }
            }
            
        }

    }

    protected virtual Vector2 GetEdgePoint(Transform t)
    {
        Vector2 point = t.position;
        float degree = t.rotation.eulerAngles.z + 90f;
        degree %= 360f;
        float radians = degree * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        direction.Normalize();
        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);
        if (absX > absY)
        {
            direction = new Vector2(Mathf.Sign(direction.x), direction.y / absX);
        }
        else
        {
            direction = new Vector2(direction.x / absY, Mathf.Sign(direction.y));
        }
        point += direction * gridscale * 0.6f;
        return point;
    }

    protected virtual void openFire()
    {
        if (projectilePrefab == null)
        {
            _target.GetComponent<UnitBase>().TakeDamage(hitDamage, 0f, damageType);
            return;
        }

        GameObject missileInstance = Instantiate(projectilePrefab, GetEdgePoint(turret.transform), turret.transform.rotation);
        MissileBase missileScript = missileInstance.GetComponent<MissileBase>();

        if (missileScript != null)
        {
            missileScript.Setup(null, hitDamage, damageType, false);
        }
        else
        {
            Destroy(missileInstance);
            _target.GetComponent<UnitBase>().TakeDamage(hitDamage, 0f, damageType);
        }
    }
    public virtual void TakeDamage(float damage, float missileAngle, DamageType damageType)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    protected virtual void Die()
    {
        gm.destructibleObstacleTilemaps[0].SetTile(gm.groundTilemap.WorldToCell(transform.position), null);
        Destroy(gameObject);
        gm.pathfinderInstance.CalculateFlowFields();
    }
}
