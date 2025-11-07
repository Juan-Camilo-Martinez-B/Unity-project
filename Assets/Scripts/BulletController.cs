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




    // Start is called before the first frame update
    void Start()
    {
        bulletTr = GetComponent<Transform>();
        bulletRb = GetComponent<Rigidbody>();

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

        if (Physics.Raycast(lastBulletPos, bulletDirection.normalized, out hit, bulletDirection.magnitude, hitboxMask))
        {
            GameObject go = hit.collider.gameObject;

            // Verificar si el objeto golpeado pertenece al jugador que disparó
            if (shooter != null && go.transform.IsChildOf(shooter.transform))
            {
                lastBulletPos = bulletNewPos;
                return; // Ignorar colisión con el jugador que disparó
            }

            BodyPartHitCheck playerBodyPart = go.GetComponent<BodyPartHitCheck>();

            if (playerBodyPart != null)
            {
                playerBodyPart.TakeHit(bulletDamage);
                Debug.Log("Disparo en " + playerBodyPart.BodyName);
            }

            // Destruir la bala al impactar
            Destroy(gameObject);
        }

        lastBulletPos = bulletNewPos;
    }
}
