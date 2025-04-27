using UnityEngine;
using Unity.Collections;
public class PlayerHeroControllerBase : UnitBase
{

    protected Vector2 _moveInput;
    protected Camera _mainCamera;

    void Start()
    {
        unitType = UnitType.playerhero;
        _unitBody = GetComponent<Rigidbody2D>();
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
        // Apply movement velocity in FixedUpdate for smoother physics
        if (_unitBody != null)
        {
            // Use MovePosition for kinematic bodies if preferred, otherwise velocity works well for dynamic
             _unitBody.linearVelocity = _moveInput * moveSpeed;
            // Alternative for kinematic:
            // _unitBody.MovePosition(_unitBody.position + _moveInput * moveSpeed * Time.fixedDeltaTime);
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
        Debug.Log("Hero has died!");
        // Add death effects, game over logic, etc. here
        Destroy(gameObject);
    }
}
