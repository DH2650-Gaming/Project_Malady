using UnityEngine;
using UnityEngine.Tilemaps;
using System;


public class MissilePlayerArrow : MissileBase
{
    bool colliderFired = false;
    override protected void OnTriggerEnter2D(Collider2D other)
    {
        //Get the object that was hit
        if (colliderFired) return;
        GameObject hitObject = other.gameObject;
        GameMaster gm= GameMaster.Instance;
        bool isEnemy = gm.spawnedEnemies.Contains(hitObject);
        bool isHero = gm.playerhero == hitObject;
        bool isObsticle = false;
        bool isTower = hitObject.GetComponent<Tilemap>() == gm.destructibleObstacleTilemaps[0];
        foreach (Tilemap tilemap in gm.indestructibleObstacleTilemaps)
        {
            if(tilemap == hitObject.GetComponent<Tilemap>()){
                isObsticle = true;
                break;
            }
        }
        
        // For the player side arrow, we only trigger the missile if it hits an enemy/tower/obstacle.
        if (isEnemy || isTower || isObsticle)
        {
            colliderFired = true;
            
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