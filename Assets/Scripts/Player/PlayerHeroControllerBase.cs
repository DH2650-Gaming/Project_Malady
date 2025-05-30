using UnityEngine;
using Unity.Collections;
public class PlayerHeroControllerBase : UnitBase
{
    [Header("Unit stats")]
    [Tooltip("Auto attack damage")]
    public float autoAttackDamage = 10f;
    [Tooltip("Hit cooldown in seconds")]
    public float autoAttackCooldown = 0.2f;
    [Tooltip("Health regen rate")]
    public float healthRegenRate = 1f;
    [Tooltip("Health regen delay in seconds")]
    public float healthRegenDelay = 5f;
    [Tooltip("Ability 1 cooldown in seconds")]
    public float ability1Cooldown = 5f;
    [Tooltip("Ability 2 cooldown in seconds")]
    public float ability2Cooldown = 10f;

    protected Vector2 _moveInput;
    protected Camera _mainCamera;
    protected float timeSinceLastHit = 0f;
    protected float timeSinceLastAutoAttack = 0f;
    protected float timeSinceLastRegen = 0f;
    protected float timeSinceLastAbility1 = 0f;
    protected float timeSinceLastAbility2 = 0f;


    protected virtual void Start()
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
    }


    protected virtual void Update()
    {

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


    }

    protected virtual void FixedUpdate()
    {
        timeSinceLastHit += Time.fixedDeltaTime;
        timeSinceLastRegen += Time.fixedDeltaTime;
        timeSinceLastAbility1 += Time.fixedDeltaTime;
        timeSinceLastAbility2 += Time.fixedDeltaTime;
        timeSinceLastAutoAttack += Time.fixedDeltaTime;

        if (timeSinceLastAutoAttack >= autoAttackCooldown)
        {
            Attack();
        }
        if (timeSinceLastHit >= healthRegenDelay)
        {
            if (timeSinceLastRegen >= 0.1f)
            {
                currentHealth = Mathf.Min(currentHealth + healthRegenRate / 10f, maxHealth);
                timeSinceLastRegen = 0f;
            }
        }
        // Apply movement velocity in FixedUpdate for smoother physics
        if (_unitBody != null)
        {
            // Use MovePosition for kinematic bodies if preferred, otherwise velocity works well for dynamic
             _unitBody.linearVelocity = _moveInput * moveSpeed;
            // Alternative for kinematic:
            // _unitBody.MovePosition(_unitBody.position + _moveInput * moveSpeed * Time.fixedDeltaTime);
        }
    }
    public virtual bool Attack()
    {
        return false;
    }

    public virtual bool Ability1()
    {
        return false;
    }
    public virtual bool Ability2()
    {
        return false;
    }

    override public void TakeDamage(float damage, float missileAngle, DamageType damageType)
    {
        currentHealth -= damage;
        timeSinceLastHit = 0f;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    override protected void Die()
    {
        Debug.Log("Hero has died!");
        // Add death effects, game over logic, etc. here
        Destroy(gameObject);
    }
}
