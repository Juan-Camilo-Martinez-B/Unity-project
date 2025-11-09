using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieRagdollController : MonoBehaviour
{
    Animator zombieAnim;
    Rigidbody zombieBody;
    Rigidbody[] zombieBones;
    ZombieController zombie;

    public List<HitMultiplier> hitStats;

    void Awake()
    {
        zombieAnim = GetComponent<Animator>();
        zombieBody = GetComponentInParent<Rigidbody>();
        zombieBones = GetComponentsInChildren<Rigidbody>();
        zombie = GetComponentInParent<ZombieController>();

        SetUp();
    }

    public void SetUp()
    {
        int layerOfHits = LayerMask.NameToLayer("Hitbox");
        
        if (layerOfHits == -1)
        {
            Debug.LogError("La capa 'Hitbox' no existe! Créala en Edit → Project Settings → Tags and Layers");
            return;
        }

        int configuredBones = 0;
        
        foreach (Rigidbody bone in zombieBones)
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
                Debug.Log($"Collider agregado a {bone.gameObject.name}");
            }
            else
            {
                // Asegurarse de que el collider NO es trigger
                col.isTrigger = false;
            }

            // Agregar ZombieBodyPart si no existe
            ZombieBodyPart partToCheck = bone.GetComponent<ZombieBodyPart>();
            if (partToCheck == null)
            {
                partToCheck = bone.gameObject.AddComponent<ZombieBodyPart>();
            }
            
            partToCheck.zombie = zombie;
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
        
        Debug.Log($"<color=green>Zombie Ragdoll configurado: {configuredBones} huesos en capa Hitbox (isTrigger=false)</color>");
    }

    public void Active(bool state)
    {
        foreach (Rigidbody bone in zombieBones)
        {
            Collider c = bone.GetComponent<Collider>();

            if (c != null)
            {
                // IMPORTANTE: Cuando NO es ragdoll (state = false), el collider NO debe ser trigger
                // Esto permite que las balas colisionen con el zombie
                c.isTrigger = false; // SIEMPRE falso para que las balas puedan colisionar
                bone.isKinematic = !state;
                bone.useGravity = state;
                
                if (state && zombieBody != null)
                    bone.velocity = zombieBody.velocity;
            }
        }

        zombieAnim.enabled = !state;
        
        if (zombieBody != null)
        {
            zombieBody.useGravity = !state;
            zombieBody.detectCollisions = !state;
            zombieBody.isKinematic = state;
        }
    }
}
