using UnityEngine;
using Unity.Collections;
public class PlayerHeroControllerBase : MonoBehaviour
{
    [Header("Stats")]
    [Tooltip("Hero movement speed in units per second.")]
    public float moveSpeed = 0.5f;
    [Tooltip("Hero maximum health points.")]
    public float maxHealth = 500;
    [Tooltip("Current health points.")]
    [ReadOnly] public float currentHealth;

    protected Rigidbody2D _playerBody;
    protected Vector2 _moveInput;
    protected Camera _mainCamera;

    void Start()
    {
        _playerBody = GetComponent<Rigidbody2D>();
        _mainCamera = GameMaster.Instance.gameCamera;
        currentHealth = maxHealth;

        if (_mainCamera == null)
        {
            Debug.LogError("PlayerHeroControllerBase requires a GameMaster with a valid gameCamera!", this);
        }
        if (_playerBody == null)
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
        Vector2 directionToMouse = (Vector2)mouseWorldPosition - _playerBody.position;
        float targetAngle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg - 90f;
        _playerBody.MoveRotation(targetAngle);
    }

    protected virtual void FixedUpdate()
    {
        // Apply movement velocity in FixedUpdate for smoother physics
        if (_playerBody != null)
        {
            // Use MovePosition for kinematic bodies if preferred, otherwise velocity works well for dynamic
             _playerBody.linearVelocity = _moveInput * moveSpeed;
            // Alternative for kinematic:
            // _playerBody.MovePosition(_playerBody.position + _moveInput * moveSpeed * Time.fixedDeltaTime);
        }
    }


    protected virtual void TakeDamage(float damage, float missileAngle)
    {
        float angleDifference = Mathf.DeltaAngle(_playerBody.rotation, missileAngle);

        if (Mathf.Abs(angleDifference) > 90f) // Hit from behind (more than 90 degrees off facing direction)
        {
            Debug.Log($"Hit from behind! Angle Diff: {angleDifference}");
            damage *= 1.5f;
        } else {
             Debug.Log($"Hit from front/side. Angle Diff: {angleDifference}");
        }
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
