using UnityEngine;
using UnityEngine.Tilemaps;
using System;


public class MissilePlayerArrow : MissileBase
{
    override protected void OnTriggerEnter2D(Collider2D other)
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
        // For the player side arrow, we only trigger the missile if it hits an enemy/tower/obstacle.
        if (isEnemy || isTower || isObsticle)
        {
            if(isEnemy){//Only damage enemies
                float impactAngle = Vector2.Angle(transform.up, other.transform.position - transform.position);
                UnitBase unitscript= hitObject.GetComponent<UnitBase>();
                if (unitscript != null)
                {
                    unitscript.TakeDamage(damage, impactAngle, damageType);
                }
                else
                {
                    Debug.LogError("Missile hit an object without UnitBase script!", hitObject);
                }
            }
            
            DestroyMissile();
        }
    }
}