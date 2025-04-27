using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;


public enum UnitType
{
    none,
    playerhero,
    playerunit,
    enemyhero,
    enemyunit,
    enemyunit2 // Hostile to enemy1 and player
}
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class UnitBase : MonoBehaviour
{

    [Header("Stats")]
    public string unitName = "Undefined";
    public string additionalTooltip = "";
    [ReadOnly] public UnitType unitType = UnitType.none;
    public Sprite unitIcon;
    public float moveSpeed = 0.5f;
    public float maxHealth = 500;
    [ReadOnly] public float currentHealth = 0f;
    protected Rigidbody2D _unitBody;
    

    public virtual void TakeDamage(float damage, float missileAngle, DamageType damageType)
    {
        float angleDifference = Mathf.DeltaAngle(_unitBody.rotation, missileAngle);

        if (Mathf.Abs(angleDifference) > 90f) // Hit from behind (more than 90 degrees off facing direction)
        {
            Debug.Log($"Hit from behind! Angle Diff: {angleDifference}");
            damage *= 1.5f;
        } else {
            Debug.Log($"Hit from front/side. Angle Diff: {angleDifference}");
        }
        if (damageType == DamageType.Poison){
            damage *= -1f;
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
        Destroy(gameObject);
    }
}
