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

    public Sprite weaponIcon;

    [Header("Dropped Item Reference")]
    [Tooltip("Prefab 'Dropped' de esta arma (con tag Item) que se instancia al soltar el arma.")]
    public GameObject droppedItemPrefab;

    [Header("Muzzle Flash & Shoot Sound")]
    public ParticleSystem muzzleFlashParticles; // Sistema de partículas para fogonazo
    public GameObject muzzleFlashPrefab; // Alternativa: prefab de fogonazo
    public float muzzleFlashLifetime = 0.15f;
    public AudioClip shootSound; // Sonido de disparo
    [Range(0f, 1f)] public float shootSoundVolume = 1f;
    public AudioSource weaponAudioSource; // AudioSource dedicado (opcional)

    [Header("Aiming")]
    public float maxShootDistance = 100f;
    [Tooltip("Capas consideradas para apuntar (lo que puede ser impactado por la cámara).")]
    public LayerMask aimMask = ~0; // todas por defecto
    [Tooltip("Capas que pueden obstruir la trayectoria desde el cañón al punto objetivo.")]
    public LayerMask obstructionMask = ~0; // todas por defecto
    [Tooltip("Mostrar líneas de depuración.")]
    public bool showDebugLines = true;

    [Header("Close-quarters safety")]
    [Tooltip("Si algo obstruye justo frente al cañón a esta distancia, se cancela el disparo para evitar autodaño.")]
    public float barrelBlockDistance = 0.3f;
    public bool cancelShotWhenBlocked = true;
    public GameObject blockedMuzzleVFXPrefab;
    public float blockedMuzzleVFXLifetime = 0.1f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // NO procesar input si el juego está pausado (Time.timeScale == 0)
        if (Time.timeScale == 0f)
        {
            shooting = false;
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            shooting = true;
            Shoot();
        }
        else if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            shooting = false;
        }

        // 1) Ray desde la cámara para obtener punto de mira (aim point)
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camOrigin = cam.transform.position;
        Vector3 camDir = cam.transform.forward;
        Ray cameraRay = new Ray(camOrigin, camDir);
        Vector3 aimPoint = camOrigin + camDir * maxShootDistance; // por defecto, punto lejano

        RaycastHit cameraHit;
        if (Physics.Raycast(cameraRay, out cameraHit, maxShootDistance, aimMask))
        {
            aimPoint = cameraHit.point; // cortamos al primer collider
        }

        // 2) Ray desde el cañón al aimPoint para detectar obstrucciones cercanas al arma
        Vector3 barrelOrigin = shootSpawn.position;
        Vector3 toAim = aimPoint - barrelOrigin;
        Vector3 barrelDir = toAim.normalized;
        float barrelDist = toAim.magnitude;
        Vector3 finalPoint = aimPoint;
        RaycastHit barrelHit;
        if (Physics.Raycast(barrelOrigin, barrelDir, out barrelHit, barrelDist, obstructionMask))
        {
            finalPoint = barrelHit.point; // recorta trayectoria si algo bloquea
        }

        // 3) Alinear el shootSpawn solamente hacia el punto final (sin desviar artificialmente)
        shootSpawn.rotation = Quaternion.LookRotation((finalPoint - barrelOrigin).normalized);

        // 4) Debug lines
        if (showDebugLines)
        {
            Debug.DrawLine(camOrigin, aimPoint, Color.blue);           // línea de la cámara
            Debug.DrawLine(barrelOrigin, finalPoint, Color.red);        // trayectoria real de la bala
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
        // Efectos de disparo: fogonazo y sonido
        PlayMuzzleFlash();
        PlayShootSound();

        // Seguridad: no dispares si hay algo pegado al cañón (pared/objeto), evita autodaño
        RaycastHit blockHit;
        if (Physics.Raycast(shootSpawn.position, shootSpawn.forward, out blockHit, barrelBlockDistance, obstructionMask))
        {
            // Si lo que golpea no es parte del propio jugador, cancelar el disparo
            if (blockHit.transform.root != transform.root)
            {
                if (blockedMuzzleVFXPrefab != null)
                {
                    var fx = Instantiate(blockedMuzzleVFXPrefab, blockHit.point, Quaternion.LookRotation(blockHit.normal));
                    if (blockedMuzzleVFXLifetime > 0f) Destroy(fx, blockedMuzzleVFXLifetime);
                }
                if (showDebugLines)
                    Debug.DrawLine(shootSpawn.position, blockHit.point, Color.yellow, 0.2f);
                if (cancelShotWhenBlocked)
                    return; // no instanciamos bala
            }
        }

        GameObject bullet = Instantiate(bulletPrefab, shootSpawn.position, shootSpawn.rotation);
        BulletController bulletCtrl = bullet.GetComponent<BulletController>();
        if (bulletCtrl != null)
        {
            bulletCtrl.SetShooter(transform.root.gameObject); // Configura lanzador y protección temporal
        }
    }

    private void PlayMuzzleFlash()
    {
        // Reproducir partículas de fogonazo si están configuradas
        if (muzzleFlashParticles != null)
        {
            muzzleFlashParticles.Play();
        }

        // O instanciar un prefab de fogonazo
        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, shootSpawn.position, shootSpawn.rotation, shootSpawn);
            
            // Reducir la intensidad de la luz del fogonazo para evitar que sature la cámara
            Light flashLight = flash.GetComponentInChildren<Light>();
            if (flashLight != null)
            {
                flashLight.intensity *= 0.3f; // Reduce la intensidad al 30%
                flashLight.range *= 0.5f;      // Reduce el rango al 50%
            }
            
            if (muzzleFlashLifetime > 0f)
                Destroy(flash, muzzleFlashLifetime);
        }
    }

    private void PlayShootSound()
    {
        if (shootSound == null) return;

        // Si hay AudioSource dedicado, usarlo
        if (weaponAudioSource != null)
        {
            weaponAudioSource.PlayOneShot(shootSound, shootSoundVolume);
        }
        else
        {
            // Sino, crear uno temporal en el punto de disparo
            AudioSource.PlayClipAtPoint(shootSound, shootSpawn.position, shootSoundVolume);
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