using UnityEngine;

public class PlayerHeroControllerArcher : PlayerHeroControllerBase
{   
    [Header("Primary attack stats")]
    [Tooltip("Hit damage")]
    public float hitDamage = 40f;
    [Tooltip("Hit cooldown in seconds")]
    public float hitCooldown = 0.5f;
    [Tooltip("Missile prefab")]
    public GameObject missilePrefab;
    
    private float timeSinceLastHit = 0f;
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

        //Primary attack
        timeSinceLastHit += Time.deltaTime;
        if (Input.GetMouseButtonDown(0) && timeSinceLastHit >= hitCooldown)
        {
            FireMissile();
            timeSinceLastHit = 0f; // Reset cooldown timer
        }
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
            missileScript.Setup(null, hitDamage, DamageType.Physical, false);
        }
        else
        {
            Debug.LogError("Missile prefab does not contain MissileBase script!", missilePrefab);
            Destroy(missileInstance);
        }
    }


}
