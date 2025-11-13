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
    public float destroyDelay = 0.2f; // Tiempo antes de destruir el barril después de explotar
    public bool showDebugGizmos = true;

    private bool hasExploded = false; // Evita explosiones múltiples

    void Start()
    {
        // Verificar que el objeto tenga el tag correcto
        if (!gameObject.CompareTag("Barrel"))
        {
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

        // Notificar al LevelManager que este barril fue destruido
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.OnBarrelDestroyed();
        }

        // PRIMERO: Reproducir efectos de explosión ANTES de cualquier otra cosa
        SpawnExplosionEffects(explosionPosition);

        // SEGUNDO: Ocultar el barril inmediatamente
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        // TERCERO: Desactivar el collider
        Collider barrelCollider = GetComponent<Collider>();
        if (barrelCollider != null)
        {
            barrelCollider.enabled = false;
        }

        // CUARTO: Aplicar daño a todos los colliders en el radio de explosión
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
            }

            // Aplicar daño a zombies
            ZombieBodyPart zombiePart = hit.GetComponent<ZombieBodyPart>();
            if (zombiePart != null)
            {
                zombiePart.TakeHit(finalDamage);
            }

            // Aplicar daño a demons
            DemonBodyPart demonPart = hit.GetComponent<DemonBodyPart>();
            if (demonPart != null)
            {
                demonPart.TakeHit(finalDamage);
            }

            // Aplicar daño al boss
            BossBodyPart bossPart = hit.GetComponent<BossBodyPart>();
            if (bossPart != null)
            {
                bossPart.TakeHit(finalDamage);
            }

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

        // FINALMENTE: Destruir el GameObject del barril
        Destroy(gameObject, destroyDelay);
    }

    private void SpawnExplosionEffects(Vector3 explosionPosition)
    {
        
        // Efecto visual de explosión
        if (explosionEffect != null)
        {
            GameObject fx = Instantiate(explosionEffect, explosionPosition, Quaternion.identity);
            Destroy(fx, 5f);
        }
        else
        {
        }

        // Sonido de explosión - crear GameObject temporal INDEPENDIENTE
        if (explosionSound != null)
        {
            
            // Crear GameObject completamente independiente
            GameObject audioObject = new GameObject("BarrelExplosionAudio");
            audioObject.transform.position = explosionPosition;
            
            // Configurar AudioSource
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = explosionSound;
            audioSource.volume = explosionSoundVolume;
            audioSource.spatialBlend = 1f; // 100% 3D
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 100f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.playOnAwake = false;
            
            // Reproducir inmediatamente
            audioSource.Play();
            
            
            // Destruir después de que termine con más margen de seguridad
            float destroyTime = explosionSound.length + 2f; // +2 segundos de margen
            Destroy(audioObject, destroyTime);
            
        }
        else
        {
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
