using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossRagdollController : MonoBehaviour
{
    Animator bossAnim;
    Rigidbody bossBody;
    Rigidbody[] bossBones;
    BossController boss;

    public List<HitMultiplier> hitStats;

    void Awake()
    {
        bossAnim = GetComponent<Animator>();
        bossBody = GetComponentInParent<Rigidbody>();
        bossBones = GetComponentsInChildren<Rigidbody>();
        boss = GetComponentInParent<BossController>();

        SetUp();
    }

    public void SetUp()
    {
        int layerOfHits = LayerMask.NameToLayer("Hitbox");
        
        if (layerOfHits == -1)
        {
            return;
        }

        int configuredBones = 0;
        
        foreach (Rigidbody bone in bossBones)
        {
            bone.collisionDetectionMode = CollisionDetectionMode.Continuous;
            bone.gameObject.layer = layerOfHits;

            Collider col = bone.GetComponent<Collider>();
            if (col == null)
            {
                CapsuleCollider capsule = bone.gameObject.AddComponent<CapsuleCollider>();
                capsule.radius = 0.08f;
                capsule.height = 0.3f;
                capsule.isTrigger = false;
                col = capsule;
            }
            else
            {
                col.isTrigger = false;
            }

            BossBodyPart partToCheck = bone.GetComponent<BossBodyPart>();
            if (partToCheck == null)
            {
                partToCheck = bone.gameObject.AddComponent<BossBodyPart>();
            }
            
            partToCheck.boss = boss;
            partToCheck.damageMultiplier = 1.0f;
            partToCheck.bodyPartName = "body";

            string bName = bone.gameObject.name.ToLower();
            
            foreach (HitMultiplier hit in hitStats)
            {
                if (bName.Contains(hit.boneName.ToLower()))
                {
                    partToCheck.damageMultiplier = hit.multiplyBy;
                    partToCheck.bodyPartName = hit.boneName;
                    break;
                }
            }
            
            configuredBones++;
        }

        Active(false);
        
    }

    public void Active(bool state)
    {
        foreach (Rigidbody bone in bossBones)
        {
            Collider c = bone.GetComponent<Collider>();

            if (c != null)
            {
                // IMPORTANTE: Cuando NO es ragdoll (state = false), el collider NO debe ser trigger
                // Esto permite que las balas colisionen con el boss
                c.isTrigger = false; // SIEMPRE falso para que las balas puedan colisionar
                bone.isKinematic = !state;
                bone.useGravity = state;
                
                if (state && bossBody != null)
                    bone.velocity = bossBody.velocity;
            }
        }

        bossAnim.enabled = !state;
        
        if (bossBody != null)
        {
            bossBody.useGravity = !state;
            bossBody.detectCollisions = !state;
            bossBody.isKinematic = state;
        }
    }
}
