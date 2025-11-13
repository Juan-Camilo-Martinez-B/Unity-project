using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    Transform bulletTr;
    Rigidbody bulletRb;

    public float bulletPower = 0f;
    public float lifeTime = 4f;

    private float time = 0f;

    public float bulletDamage = 1f;

    Vector3 lastBulletPos;

    public LayerMask hitboxMask;
    
    [HideInInspector]
    public GameObject shooter; // Referencia al jugador que disparó la bala

    [Header("Protección contra autodaño")]
    public float shooterIgnoreDuration = 0.15f; // segundos a ignorar colisiones con el lanzador

    [Header("Impact Effects")]
    public GameObject hitEffect; // Prefab de efecto de impacto
    public AudioClip hitSound; // Sonido de impacto
    [Range(0f, 1f)] public float hitSoundVolume = 1f;

    Collider myCollider;
    List<Collider> shooterColliders;




    // Start is called before the first frame update
    void Start()
    {
        bulletTr = GetComponent<Transform>();
        bulletRb = GetComponent<Rigidbody>();
        myCollider = GetComponent<Collider>();

        bulletRb.velocity = this.transform.forward * bulletPower; 

        hitboxMask = 1 << LayerMask.NameToLayer("Hitbox");
        lastBulletPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;

        DetectCollision();

        if (time >= lifeTime)
        {
            Destroy(this.gameObject);
        }
        
    }

    public void DetectCollision()
    {
        Vector3 bulletNewPos = bulletTr.position;
        Vector3 bulletDirection = bulletNewPos - lastBulletPos;

        RaycastHit hit;

        // Raycast contra todos los colliders excepto triggers
        if (Physics.Raycast(lastBulletPos, bulletDirection.normalized, out hit, bulletDirection.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            // Ignorar impactos contra el propio lanzador por seguridad (raycast)
            if (shooter != null && hit.collider != null)
            {
                Transform root = hit.collider.transform.root;
                if (root != null && root.gameObject == shooter)
                {
                    lastBulletPos = bulletNewPos;
                    return;
                }
            }

            GameObject go = hit.collider.gameObject;
            
            // Detectar impacto en jugador
            BodyPartHitCheck playerBodyPart = go.GetComponent<BodyPartHitCheck>();
            if (playerBodyPart != null)
            {
                playerBodyPart.TakeHit(bulletDamage);
            }

            // Detectar impacto en zombie
            ZombieBodyPart zombieBodyPart = go.GetComponent<ZombieBodyPart>();
            if (zombieBodyPart != null)
            {
                zombieBodyPart.TakeHit(bulletDamage);
            }

            // Detectar impacto en demon
            DemonBodyPart demonBodyPart = go.GetComponent<DemonBodyPart>();
            if (demonBodyPart != null)
            {
                demonBodyPart.TakeHit(bulletDamage);
            }

            // Detectar impacto en boss
            BossBodyPart bossBodyPart = go.GetComponent<BossBodyPart>();
            if (bossBodyPart != null)
            {
                bossBodyPart.TakeHit(bulletDamage);
            }

            // Detectar impacto en barril y hacerlo explotar
            BarrelController barrel = go.GetComponent<BarrelController>();
            if (barrel != null)
            {
                barrel.TakeHit();
            }

            // Efectos de impacto
            SpawnImpactEffects(hit.point, hit.normal, hit.collider.transform);

            // Destruir la bala al impactar con cualquier cosa
            Destroy(gameObject);
        }

        lastBulletPos = bulletNewPos;
    }

    private void SpawnImpactEffects(Vector3 hitPoint, Vector3 hitNormal, Transform hitTransform)
    {
        // Efecto visual de impacto
        if (hitEffect != null)
        {
            GameObject fx = Instantiate(hitEffect, hitPoint, Quaternion.LookRotation(hitNormal));
            
            // Si golpeó un objeto con Rigidbody (enemigo), hacer el efecto hijo para que se mueva con él
            // Si golpeó superficie estática (pared), dejarlo libre y destruir rápido
            Rigidbody hitRb = hitTransform.GetComponent<Rigidbody>();
            if (hitRb != null)
            {
                fx.transform.SetParent(hitTransform); // Adherir al enemigo
            }
            
            Destroy(fx, 1f); // Destruir rápido (0.5s en lugar de 3s)
        }

        // Sonido de impacto
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, hitPoint, hitSoundVolume);
        }
    }

    // Llamar desde quien instancia la bala para configurar protección contra autodaño
    public void SetShooter(GameObject shooterObj)
    {
        shooter = shooterObj;
        if (shooter == null || myCollider == null) return;

        shooterColliders = new List<Collider>(shooter.GetComponentsInChildren<Collider>());
        foreach (var c in shooterColliders)
        {
            if (c != null)
                Physics.IgnoreCollision(myCollider, c, true);
        }

        // Restaurar colisiones después de un breve tiempo
        if (shooterIgnoreDuration > 0f)
            StartCoroutine(RestoreShooterCollisionsAfter(shooterIgnoreDuration));
    }

    System.Collections.IEnumerator RestoreShooterCollisionsAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (myCollider == null || shooterColliders == null) yield break;
        foreach (var c in shooterColliders)
        {
            if (c != null)
                Physics.IgnoreCollision(myCollider, c, false);
        }
        shooterColliders = null;
    }
}