using UnityEngine;

public class PlayerHeroControllerArcher : PlayerHeroControllerBase
{   

    [Header("Archer specific settings")]
    [Tooltip("Missile prefab")]
    public GameObject missilePrefab;
    [Tooltip("Ability 1 teleport distance")]
    public float ability1TeleportDistance = 2f;
    [Tooltip("Ability 2 root cast distance")]
    public float ability2RootDistance = 3f;
    [Tooltip("Ability 2 root radius")]
    public float ability2RootRadius = 1f;
    [Tooltip("Ability 2 root duration in seconds")]
    public float ability2RootDuration = 3f;


    protected LayerMask ability1CheckLayerMask;
    protected LayerMask ability2CheckLayerMask;

    override protected void Start()
    {
        unitType = UnitType.playerhero;
        _unitBody = GetComponent<Rigidbody2D>();
        _unitCollider = GetComponent<Collider2D>();
        _mainCamera = GameMaster.Instance.gameCamera;
        currentHealth = maxHealth;

        if (_mainCamera == null)
        {
            Debug.LogError("PlayerHeroControllerBase requires a GameMaster with a valid gameCamera!", this);
        }
        if (_unitBody == null)
        {
            Debug.LogError("PlayerHeroControllerBase requires a Rigidbody2D component!", this);
        }
        ability1CheckLayerMask = LayerMask.GetMask("Towers", "Obstacles", "EnemyUnits", "EnemyHeroes");
        ability1TeleportDistance *= GameMaster.Instance.groundTilemap.cellSize.x;
        ability2CheckLayerMask = LayerMask.GetMask("EnemyUnits");
        ability2RootDistance *= GameMaster.Instance.groundTilemap.cellSize.x;
        ability2RootRadius *= GameMaster.Instance.groundTilemap.cellSize.x;
    }

    override protected void Update()
    {
        // Movement
        _moveInput.x = Input.GetAxisRaw("Horizontal");
        _moveInput.y = Input.GetAxisRaw("Vertical");
        _moveInput.Normalize(); 

        Vector3 mouseScreenPosition = Input.mousePosition;
        Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPosition.x,
            mouseScreenPosition.y,
            _mainCamera.transform.position.z - transform.position.z
        ));
        Vector2 directionToMouse = (Vector2)mouseWorldPosition - _unitBody.position;
        float targetAngle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg - 90f;
        _unitBody.MoveRotation(targetAngle);

        timeSinceLastHit += Time.deltaTime;
        timeSinceLastRegen += Time.deltaTime;
        timeSinceLastAbility1 += Time.deltaTime;
        timeSinceLastAbility2 += Time.deltaTime;
        timeSinceLastAutoAttack += Time.deltaTime;

        if (timeSinceLastAutoAttack >= autoAttackCooldown)
        {
            FireMissile();
            timeSinceLastAutoAttack = 0f;
        }
        if (timeSinceLastHit >= healthRegenDelay)
        {
            if (timeSinceLastRegen >= 0.1f)
            {
                currentHealth = Mathf.Min(currentHealth + healthRegenRate / 10f, maxHealth);
                timeSinceLastRegen = 0f;
            }
        }
    }

    override public bool Attack(){
        if (timeSinceLastAutoAttack >= autoAttackCooldown)
        {
            FireMissile();
            timeSinceLastAutoAttack = 0f;
            return true;
        }
        return false;
    }

    void FireMissile()
    {
        if (missilePrefab == null)
        {
            Debug.LogWarning("Missing missile prefab or target!");
            return;
        }

        GameObject missileInstance = Instantiate(missilePrefab, transform.position, transform.rotation);
        MissileBase missileScript = missileInstance.GetComponent<MissileBase>();

        if (missileScript != null)
        {
            // *** MODIFIED: Pass the updated HandleMissileHit method ***
            missileScript.Setup(null, autoAttackDamage, DamageType.Physical, false);
        }
        else
        {
            Debug.LogError("Missile prefab does not contain MissileBase script!", missilePrefab);
            Destroy(missileInstance);
        }
    }

    override public bool Ability1()
    {
        if (timeSinceLastAbility1 >= ability1Cooldown)
        {
            Vector2 direction = _unitBody.transform.up;
            float actualDistance = ability1TeleportDistance;
            float unitRadius = Mathf.Sqrt(_unitCollider.bounds.size.x * _unitCollider.bounds.size.y) / 2f;
            Collider2D hit = Physics2D.OverlapCircle(_unitBody.position + direction * actualDistance, unitRadius, ability1CheckLayerMask);
            while (hit != null)
            {
                actualDistance -= 0.1f;
                if (actualDistance < 0.5f) return false;
                hit = Physics2D.OverlapCircle(_unitBody.position + direction * actualDistance, unitRadius, ability1CheckLayerMask);
            }
            Vector2 teleportPosition = _unitBody.position + direction * actualDistance;
            transform.position = teleportPosition;
            timeSinceLastAbility1 = 0f;
            return true;
        }
        return false;
    }

    override public bool Ability2()
    {
        if (timeSinceLastAbility2 >= ability2Cooldown)
        {
            Vector3 mouseScreenPosition = Input.mousePosition;
            Vector2 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(new Vector3(
                mouseScreenPosition.x,
                mouseScreenPosition.y,
                _mainCamera.transform.position.z - transform.position.z
            ));
            float distanceToMouse = Vector2.Distance(_unitBody.position, mouseWorldPosition);
            if (distanceToMouse > ability2RootDistance)
            {
                return false;
            }
            Collider2D[] hits = Physics2D.OverlapCircleAll(mouseWorldPosition, ability2RootRadius, ability2CheckLayerMask);
            if (hits.Length == 0)
            {
                return false;
            }
            foreach (Collider2D hit in hits)
            {
                EnemyController unitScript = hit.gameObject.GetComponent<EnemyController>();
                if (unitScript != null)
                {
                    unitScript.ApplyRootDebuff(ability2RootDuration);
                }
            }
            timeSinceLastAbility2 = 0f;
            return true;
        }
        return false;
    }

}
