using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;


[RequireComponent(typeof(Collider2D))]
public class TowerArtillery : TowerBase
{
    [Header("Combat")]
    public float damageRadius = 0.8f;
    override protected void openFire()
    {
        Vector2 targetPos = _target.transform.position;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(targetPos, damageRadius, targetingLayer);
        foreach (Collider2D col in hitColliders)
        {
            GameObject hitObject = col.gameObject;
            UnitBase unit = hitObject.GetComponent<UnitBase>();
            if (unit == null)
            {
                continue;
            }
            unit.TakeDamage(hitDamage, -1f, damageType);
        }
    }
}
