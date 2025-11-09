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

        // Raycast contra cualquier collider para que la bala se detenga en paredes/props/etc.
        if (Physics.Raycast(lastBulletPos, bulletDirection.normalized, out hit, bulletDirection.magnitude))
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
            BodyPartHitCheck playerBodyPart = go.GetComponent<BodyPartHitCheck>();

            if (playerBodyPart != null)
            {
                playerBodyPart.TakeHit(bulletDamage);
                Debug.Log("Disparo en " + playerBodyPart.BodyName);
            }

            // Detectar impacto en barril y hacerlo explotar
            BarrelController barrel = go.GetComponent<BarrelController>();
            if (barrel != null)
            {
                barrel.TakeHit();
            }

            // Efectos de impacto
            SpawnImpactEffects(hit.point, hit.normal);

            // Destruir la bala al impactar con cualquier cosa
            Destroy(gameObject);
        }

        lastBulletPos = bulletNewPos;
    }

    private void SpawnImpactEffects(Vector3 hitPoint, Vector3 hitNormal)
    {
        // Efecto visual de impacto
        if (hitEffect != null)
        {
            GameObject fx = Instantiate(hitEffect, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(fx, 3f); // Auto-destruir después de 3 segundos
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