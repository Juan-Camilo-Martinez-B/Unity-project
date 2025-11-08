using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSlots : MonoBehaviour
{
    public Transform primarySlot;
    public Transform secondarySlot;
    public Transform throwableSlot;

    private Transform lastActivatedSlot;
    // Último slot de arma (no throwable) seleccionado por el jugador
    public Transform lastNonThrowableSlot;

    // Animación de sacar granada
    [Header("Grenade Draw Animation")]
    public string grenadeDrawStateName = "Start Grenade"; // Configurable en el inspector
    public int grenadeLayerIndex = 2; // Capa donde se reproducen animaciones de granada

    //Externos
    PlayerController player;
    // Start is called before the first frame update
    void Start()
    {
        player = GetComponentInParent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (player.weapons < 1)
            return;
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (player.primaryWeapon == null)
                return;
            // Registrar último slot no arrojadizo
            lastNonThrowableSlot = primarySlot;
            ToggleSlot(primarySlot);

            player.hasPistol = true;
            player.hasRiffle = false;
            player.hasGrenade = false;

            player.primaryWeaponIcon.color = Color.green;
            player.secondaryWeaponIcon.color = Color.white;
            player.throwableWeaponIcon.color = Color.white;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (player.secondaryWeapon == null)
                return;
            // Registrar último slot no arrojadizo
            lastNonThrowableSlot = secondarySlot;
            ToggleSlot(secondarySlot);

            player.hasRiffle = true;
            player.hasPistol = false;
            player.hasGrenade = false;

            player.secondaryWeaponIcon.color = Color.green;
            player.throwableWeaponIcon.color = Color.white;
            player.primaryWeaponIcon.color = Color.white;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (player.throwableWeapon == null)
                return;
            ToggleSlot(throwableSlot);

            player.hasGrenade = true;
            player.hasPistol = false;
            player.hasRiffle = false;

            player.throwableWeaponIcon.color = Color.green;
            player.primaryWeaponIcon.color = Color.white;
            player.secondaryWeaponIcon.color = Color.white;

            // Forzar que la animación de "sacar granada" se reproduzca cada vez que cambiamos a la granada
            if (player != null && player.playerAnim != null && !string.IsNullOrEmpty(grenadeDrawStateName))
            {
                // Reinicia la animación desde el comienzo en la capa indicada
                player.playerAnim.Play(grenadeDrawStateName, grenadeLayerIndex, 0f);
            }
        }

    }

    private void ToggleSlot(Transform slot)
    {
        if (slot  == lastActivatedSlot)
        {    
            return;
        }

        DeactivateAllSlots();

        bool isActive = slot.gameObject.activeSelf;
        slot.gameObject.SetActive(!isActive);
        lastActivatedSlot = isActive ? null : slot;
        
    }

    public void DeactivateAllSlots()
    {
        primarySlot.gameObject.SetActive(false);
        secondarySlot.gameObject.SetActive(false);
        throwableSlot.gameObject.SetActive(false);
    }

    // Restaura el último arma seleccionada o vuelve a estado base si no hay
    public void RestoreLastWeaponOrIdle()
    {
        // Si hay un último slot de arma válido, activarlo
        if (lastNonThrowableSlot == primarySlot && player.primaryWeapon != null)
        {
            ToggleSlot(primarySlot);
            player.hasPistol = true;
            player.hasRiffle = false;
            player.hasGrenade = false;
        }
        else if (lastNonThrowableSlot == secondarySlot && player.secondaryWeapon != null)
        {
            ToggleSlot(secondarySlot);
            player.hasRiffle = true;
            player.hasPistol = false;
            player.hasGrenade = false;
        }
        else
        {
            // No hay arma previa: desactivar slots y limpiar flags para animación de caminar
            DeactivateAllSlots();
            player.hasPistol = false;
            player.hasRiffle = false;
            player.hasGrenade = false;
        }
    }
}
