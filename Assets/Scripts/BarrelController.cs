using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrelController : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionRadius = 5f; // Radio de daño de la explosión
    public float explosionDamage = 50f; // Daño máximo en el centro
    public float explosionForce = 500f; // Fuerza de empuje (opcional para ragdolls)

    [Header("Explosion Effects")]
    public GameObject explosionEffect; // Prefab de efecto de explosión
    public AudioClip explosionSound; // Sonido de explosión
    [Range(0f, 5f)] public float explosionSoundVolume = 2.5f;
    public AudioSource barrelAudioSource; // AudioSource dedicado (opcional)

    [Header("Visual Settings")]
    public float destroyDelay = 2f; // Tiempo antes de destruir el barril después de explotar (debe ser suficiente para que termine el audio)
    public bool showDebugGizmos = true;

    private bool hasExploded = false; // Evita explosiones múltiples

    void Start()
    {
        // Verificar que el objeto tenga el tag correcto
        if (!gameObject.CompareTag("Barrel"))
        {
            Debug.LogWarning($"BarrelController en {gameObject.name} requiere el tag 'Barrel'!");
        }
    }

    // Método público para ser llamado cuando una bala impacta
    public void TakeHit()
    {
        if (!hasExploded)
        {
            Explode();
        }
    }

    public void Explode()
    {
        if (hasExploded) return; // Prevenir explosiones múltiples
        hasExploded = true;

        Vector3 explosionPosition = transform.position;

        // Efectos de explosión: visual y sonido
        SpawnExplosionEffects(explosionPosition);

        // Aplicar daño a todos los colliders en el radio de explosión
        Collider[] hitColliders = Physics.OverlapSphere(explosionPosition, explosionRadius);

        foreach (Collider hit in hitColliders)
        {
            // Ignorar el propio barril
            if (hit.gameObject == gameObject) continue;

            // Calcular distancia y daño
            float distance = Vector3.Distance(explosionPosition, hit.transform.position);
            float damageMultiplier = 1f - (distance / explosionRadius); // Daño disminuye con la distancia
            float finalDamage = explosionDamage * damageMultiplier;

            // Aplicar daño a BodyParts (jugador)
            BodyPartHitCheck bodyPart = hit.GetComponent<BodyPartHitCheck>();
            if (bodyPart != null)
            {
                bodyPart.TakeHit(finalDamage);
                Debug.Log($"Barril dañó a {bodyPart.BodyName} con {finalDamage} de daño");
            }

            // Aplicar daño a enemigos (cuando se implementen)
            // Descomenta esto cuando tengas un script de enemigo:
            // EnemyController enemy = hit.GetComponentInParent<EnemyController>();
            // if (enemy != null)
            // {
            //     enemy.TakeDamage(finalDamage);
            //     Debug.Log($"Barril dañó a enemigo con {finalDamage} de daño");
            // }

            // Opcional: aplicar fuerza a rigidbodies cercanos
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
            }

            // Hacer explotar otros barriles en cadena
            BarrelController otherBarrel = hit.GetComponent<BarrelController>();
            if (otherBarrel != null && !otherBarrel.hasExploded)
            {
                otherBarrel.Explode();
            }
        }

        // Ocultar el barril y destruirlo después de un breve delay
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        // Desactivar el collider para que no interfiera
        Collider barrelCollider = GetComponent<Collider>();
        if (barrelCollider != null)
        {
            barrelCollider.enabled = false;
        }

        Destroy(gameObject, destroyDelay);
    }

    private void SpawnExplosionEffects(Vector3 explosionPosition)
    {
        // Efecto visual de explosión
        if (explosionEffect != null)
        {
            GameObject fx = Instantiate(explosionEffect, explosionPosition, Quaternion.identity);
            Destroy(fx, 5f); // Auto-destruir después de 5 segundos
        }

        // Sonido de explosión - usar PlayClipAtPoint para que no se destruya con el barril
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, explosionPosition, explosionSoundVolume);
            Debug.Log($"Reproduciendo sonido de explosión del barril con volumen: {explosionSoundVolume}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (showDebugGizmos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
