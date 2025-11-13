using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeExplosion : MonoBehaviour
{
    Transform grenadeTr;
    Rigidbody grenadeRb;
    MeshRenderer grenadeRenderer;

    public bool explode = false;

    public float damageArea = 0f;

    public float throwForce = 0f;

    public float explodePower = 0f;
    public float lifeTime = 0f;

    public float explodeDamage = 0f;

    private float time = 0f;

    public LayerMask hitboxMask;
    Vector3 lastGrenadePos;

    public bool ShowDebugGizmos = true;

    [Header("Explosion Effects")]
    public GameObject explosionEffect; // Prefab de efecto de explosión
    public AudioClip explosionSound; // Sonido de explosión
    [Range(0f, 1f)] public float explosionSoundVolume = 1f;

    void Start()
    {
        grenadeTr = GetComponent<Transform>();
        grenadeRb = GetComponent<Rigidbody>();
        grenadeRenderer = GetComponent<MeshRenderer>();

        // Asegurarse de que la granada sea visible
        if (grenadeRenderer != null)
        {
            grenadeRenderer.enabled = true;
        }

        // Configurar el Rigidbody para física continua
        grenadeRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        grenadeRb.interpolation = RigidbodyInterpolation.Interpolate;
        
        hitboxMask = LayerMask.NameToLayer("Hitbox");
        lastGrenadePos = transform.position;

        grenadeRb.velocity = grenadeTr.forward * throwForce;
    }
    void Update()
    {
        time += Time.deltaTime;
        if (explode)
        {
            if (time >= lifeTime)
            {
                ExplodeNow();
                // Ocultar la granada antes de destruirla
                if (grenadeRenderer != null)
                {
                    grenadeRenderer.enabled = false;
                }
                // Esperar un frame para que se vea el efecto de explosión antes de destruir
                Destroy(this.gameObject, 0.1f);
            }
            
        }
        else
        {
            DetectCollision();

        }
    }

    public void ExplodeNow()
    {
        Vector3 explodePos = grenadeTr.position;

        // Efectos de explosión: visual y sonido
        SpawnExplosionEffects(explodePos);

        Collider[] checking = Physics.OverlapSphere(explodePos, this.damageArea);

        if (checking.Length > 0)
        {
            foreach (Collider c in checking)
            {
                GameObject go = c.gameObject;

                // Detectar barriles y hacerlos explotar
                BarrelController barrel = go.GetComponent<BarrelController>();
                if (barrel != null)
                {
                    barrel.TakeHit();
                    continue; // Saltar al siguiente objeto
                }

                if (go.layer == hitboxMask)
                {
                    // Detectar partes del cuerpo del jugador
                    BodyPartHitCheck playerBodyPart = go.GetComponent<BodyPartHitCheck>();

                    if (playerBodyPart != null)
                    {
                        Vector3 collisionPos = c.ClosestPoint(explodePos);

                        float distance = Vector3.Distance(explodePos, collisionPos);

                        float damageDisminution = distance / damageArea;

                        float finalDamage = explodeDamage - explodeDamage * damageDisminution;
                        playerBodyPart.TakeHit(finalDamage);
                    }

                    // Detectar partes del cuerpo del zombie
                    ZombieBodyPart zombieBodyPart = go.GetComponent<ZombieBodyPart>();

                    if (zombieBodyPart != null)
                    {
                        Vector3 collisionPos = c.ClosestPoint(explodePos);

                        float distance = Vector3.Distance(explodePos, collisionPos);

                        float damageDisminution = distance / damageArea;

                        float finalDamage = explodeDamage - explodeDamage * damageDisminution;
                        zombieBodyPart.TakeHit(finalDamage);
                    }

                    // Detectar partes del cuerpo del demon
                    DemonBodyPart demonBodyPart = go.GetComponent<DemonBodyPart>();

                    if (demonBodyPart != null)
                    {
                        Vector3 collisionPos = c.ClosestPoint(explodePos);

                        float distance = Vector3.Distance(explodePos, collisionPos);

                        float damageDisminution = distance / damageArea;

                        float finalDamage = explodeDamage - explodeDamage * damageDisminution;
                        demonBodyPart.TakeHit(finalDamage);
                    }

                    // Detectar partes del cuerpo del boss
                    BossBodyPart bossBodyPart = go.GetComponent<BossBodyPart>();

                    if (bossBodyPart != null)
                    {
                        Vector3 collisionPos = c.ClosestPoint(explodePos);

                        float distance = Vector3.Distance(explodePos, collisionPos);

                        float damageDisminution = distance / damageArea;

                        float finalDamage = explodeDamage - explodeDamage * damageDisminution;
                        bossBodyPart.TakeHit(finalDamage);
                    }
                }
            }
        }

    }

    private void SpawnExplosionEffects(Vector3 explosionPosition)
    {
        // Efecto visual de explosión
        if (explosionEffect != null)
        {
            GameObject fx = Instantiate(explosionEffect, explosionPosition, Quaternion.identity);
            Destroy(fx, 5f); // Auto-destruir después de 5 segundos
        }

        // Sonido de explosión
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, explosionPosition, explosionSoundVolume);
        }
    }

    public void DetectCollision()
    {
        Vector3 grenadeNewPos = grenadeTr.position;
        Vector3 grenadeDirection = lastGrenadePos - grenadeNewPos;
        float distance = grenadeDirection.magnitude;

        // Usar SphereCast en lugar de Raycast para mejor detección
        RaycastHit hit;
        if (Physics.SphereCast(grenadeNewPos, 0.1f, grenadeDirection.normalized, out hit, distance))
        {
            // Si golpea cualquier objeto, explotar
            explode = true;
            time = lifeTime; // Forzar explosión inmediata
        }

        lastGrenadePos = grenadeNewPos;
    }

    private void OnDrawGizmos()
    {
        if (ShowDebugGizmos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, damageArea);
        }
    }
 
}