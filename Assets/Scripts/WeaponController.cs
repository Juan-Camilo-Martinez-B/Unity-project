using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public Transform shootSpawn;

    public bool shooting = false;

    public float shootDelay = 0f;
    public float lastShootTime = 0f;

    public enum ShootMode 
    {
        Single,
        Auto
    }

    public ShootMode currentShootMode = ShootMode.Single;

    public GameObject bulletPrefab;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            shooting = true;
            Shoot();
        }
        else if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            shooting = false;
        }

        Debug.DrawLine(Camera.main.transform.position, Camera.main.transform.position + Camera.main.transform.forward * 10f, Color.blue);
        
        RaycastHit cameraHit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out cameraHit))
        {
            // Calcular la dirección desde el arma hasta el punto donde mira la cámara
            Vector3 targetPoint = cameraHit.point;
            Vector3 directionToTarget = (targetPoint - shootSpawn.position).normalized;
            shootSpawn.rotation = Quaternion.LookRotation(directionToTarget);
            
            // Dibujar la línea roja solo hasta el punto de impacto
            RaycastHit weaponHit;
            if (Physics.Raycast(shootSpawn.position, directionToTarget, out weaponHit))
            {
                Debug.DrawLine(shootSpawn.position, weaponHit.point, Color.red);
            }
            else
            {
                Debug.DrawLine(shootSpawn.position, shootSpawn.position + directionToTarget * 10f, Color.red);
            }
        }


    }

    public void Shoot()
    {
       if (Time.time - lastShootTime > shootDelay)
       {
        if (shooting)
        {
            switch (currentShootMode)
            {
                case ShootMode.Single:
                    InstantiateBullet();
                    break;
                case ShootMode.Auto:
                    StartCoroutine(AutomaticShoot());
                    break;
            }
        }
        
       }
    }

    public void InstantiateBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, shootSpawn.position, shootSpawn.rotation);
        BulletController bulletCtrl = bullet.GetComponent<BulletController>();
        if (bulletCtrl != null)
        {
            bulletCtrl.SetShooter(transform.root.gameObject); // Configura lanzador y protección temporal
        }
    }

    IEnumerator AutomaticShoot()
    {
        while (shooting)
        {
            InstantiateBullet();
            yield return new WaitForSeconds(shootDelay);
        }
    }
}
