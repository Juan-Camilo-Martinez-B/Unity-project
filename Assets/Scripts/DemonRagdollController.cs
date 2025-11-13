using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemonRagdollController : MonoBehaviour
{
    Animator demonAnim;
    Rigidbody demonBody;
    Rigidbody[] demonBones;
    DemonController demon;

    public List<HitMultiplier> hitStats;

    void Awake()
    {
        demonAnim = GetComponent<Animator>();
        demonBody = GetComponentInParent<Rigidbody>();
        demonBones = GetComponentsInChildren<Rigidbody>();
        demon = GetComponentInParent<DemonController>();

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
        
        foreach (Rigidbody bone in demonBones)
        {
            // Configurar el rigidbody del hueso
            bone.collisionDetectionMode = CollisionDetectionMode.Continuous;
            bone.gameObject.layer = layerOfHits;

            // Asegurarse de que tiene un collider
            Collider col = bone.GetComponent<Collider>();
            if (col == null)
            {
                // Si no tiene collider, agregar uno tipo capsule
                CapsuleCollider capsule = bone.gameObject.AddComponent<CapsuleCollider>();
                capsule.radius = 0.05f;
                capsule.height = 0.2f;
                capsule.isTrigger = false; // CRÍTICO: NO debe ser trigger
                col = capsule;
            }
            else
            {
                // Asegurarse de que el collider NO es trigger
                col.isTrigger = false;
            }

            // Agregar DemonBodyPart si no existe
            DemonBodyPart partToCheck = bone.GetComponent<DemonBodyPart>();
            if (partToCheck == null)
            {
                partToCheck = bone.gameObject.AddComponent<DemonBodyPart>();
            }
            
            partToCheck.demon = demon;
            partToCheck.damageMultiplier = 1.0f; // Valor por defecto
            partToCheck.bodyPartName = "body"; // Valor por defecto

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
        foreach (Rigidbody bone in demonBones)
        {
            Collider c = bone.GetComponent<Collider>();

            if (c != null)
            {
                // IMPORTANTE: Cuando NO es ragdoll (state = false), el collider NO debe ser trigger
                // Esto permite que las balas colisionen con el demon
                c.isTrigger = false; // SIEMPRE falso para que las balas puedan colisionar
                bone.isKinematic = !state;
                bone.useGravity = state;
                
                if (state && demonBody != null)
                    bone.velocity = demonBody.velocity;
            }
        }

        demonAnim.enabled = !state;
        
        if (demonBody != null)
        {
            demonBody.useGravity = !state;
            demonBody.detectCollisions = !state;
            demonBody.isKinematic = state;
        }
    }
}
