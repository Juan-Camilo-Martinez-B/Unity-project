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
        Debug.Log(time);
        player.playerAnim.Play("Final Grenade");

        if (time >= throwDelayTime)
        {
            // Diagnostics: log prefab and spawn transform scales (local and lossy)
            Debug.Log("[Grenade] Prefab localScale: " + theGrenade.transform.localScale + " | lossyScale: " + theGrenade.transform.lossyScale);
            if (player != null && player.spawnGrenade != null)
            {
                Debug.Log("[Grenade] Spawn transform localScale: " + player.spawnGrenade.localScale + " | lossyScale: " + player.spawnGrenade.lossyScale);
            }

            // Instanciar la granada sin parent para evitar heredar escala de padres
            GameObject g = Instantiate(theGrenade, player.spawnGrenade.position, player.spawnGrenade.rotation);
            g.transform.SetParent(null);

            // Forzar la escala local para que coincida con la del prefab raíz
            g.transform.localScale = theGrenade.transform.localScale;

            Debug.Log("[Grenade] Instanciada localScale: " + g.transform.localScale + " | lossyScale: " + g.transform.lossyScale);

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