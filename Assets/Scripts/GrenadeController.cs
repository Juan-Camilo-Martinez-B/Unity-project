using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeController : MonoBehaviour
{
    public bool throwing = false;

    public float throwDelayTime = 0f;

    public float time = 0f;

    public GameObject theGrenade;

    public Sprite weaponIcon;

    [Header("Dropped Item Reference")]
    [Tooltip("Prefab 'Dropped' de esta granada (con tag Item) que se instancia al soltar la granada.")]
    public GameObject droppedItemPrefab;

    //Externos
    PlayerController player;
    WeaponSlots weaponSlots;


    // Start is called before the first frame update
    void Start()
    {
        player = GetComponentInParent<PlayerController>();
        weaponSlots = player != null ? player.GetComponentInChildren<WeaponSlots>() : null;
        
    }

    // Update is called once per frame
    void Update()
    {
        // NO procesar input si el juego está pausado (Time.timeScale == 0)
        if (Time.timeScale == 0f)
        {
            return;
        }
        
        // CRÍTICO: Solo permitir lanzar granada si el throwableSlot está ACTIVO
        if (weaponSlots != null && weaponSlots.throwableSlot != null)
        {
            if (!weaponSlots.throwableSlot.gameObject.activeSelf)
            {
                return; // Si el slot de granada no está activo, no hacer nada
            }
        }
        
        if (Input.GetKey(KeyCode.Mouse0))
        {
        }
        else if(Input.GetKeyUp(KeyCode.Mouse0))
        {
            throwing = true;
        }

        if (throwing == true)
        {
            Throw();
        }

    }

    public void Throw()
    {
        time += Time.deltaTime;
        player.playerAnim.Play("Final Grenade");

        if (time >= throwDelayTime)
        {
            // Diagnostics: log prefab and spawn transform scales (local and lossy)
            if (player != null && player.spawnGrenade != null)
            {
            }

            // Instanciar la granada sin parent para evitar heredar escala de padres
            GameObject g = Instantiate(theGrenade, player.spawnGrenade.position, player.spawnGrenade.rotation);
            g.transform.SetParent(null);

            // Forzar la escala local para que coincida con la del prefab raíz
            g.transform.localScale = theGrenade.transform.localScale;


            // Actualizar estado antes de destruir el arma en mano
            throwing = false;

            // Vaciar el slot de granada y ocultar icono hasta que se recoja otra
            if (weaponSlots != null && weaponSlots.throwableSlot != null)
            {
                weaponSlots.throwableSlot.gameObject.SetActive(false);
            }
            if (player.throwableWeaponIcon != null)
            {
                player.throwableWeaponIcon.gameObject.SetActive(false);
                player.throwableWeaponIcon.sprite = null; // Limpiar sprite
            }
            player.throwableWeapon = null;

            // Actualizar contadores y animación
            player.weapons--;
            player.hasGrenade = false;
            player.playerAnim.SetLayerWeight(1, 1);
            player.playerAnim.SetLayerWeight(2, 0);

            // Restaurar arma previa o volver a idle
            if (weaponSlots != null)
            {
                weaponSlots.RestoreLastWeaponOrIdle();
            }

            // Destruir el objeto del arma de la mano
            Destroy(this.gameObject);
        }

    }
    
}