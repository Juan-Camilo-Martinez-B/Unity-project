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
        // NO procesar input si el juego está pausado (Time.timeScale == 0)
        if (Time.timeScale == 0f)
        {
            return;
        }
        
        if (player.weapons < 1)
            return;

        // Tecla Q: esconder todas las armas y volver a animación de caminar
        if (Input.GetKeyDown(KeyCode.Q))
        {
            HideAllWeapons();
            return;
        }

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

            UpdateWeaponIcons(1);
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

            UpdateWeaponIcons(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (player.throwableWeapon == null)
                return;
            ToggleSlot(throwableSlot);

            player.hasGrenade = true;
            player.hasPistol = false;
            player.hasRiffle = false;

            UpdateWeaponIcons(3);

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

    // Actualiza los colores de los iconos de armas según el slot activo
    private void UpdateWeaponIcons(int activeSlot)
    {
        player.primaryWeaponIcon.color = (activeSlot == 1) ? Color.green : Color.white;
        player.secondaryWeaponIcon.color = (activeSlot == 2) ? Color.green : Color.white;
        player.throwableWeaponIcon.color = (activeSlot == 3) ? Color.green : Color.white;
    }

    // Esconde todas las armas y vuelve a la animación de caminar
    public void HideAllWeapons()
    {
        // Desactivar todos los slots visualmente
        DeactivateAllSlots();
        
        // Limpiar el rastro del último slot activo
        lastActivatedSlot = null;
        
        // CRÍTICO: Resetear TODAS las flags de armas a false
        // Esto hace que el Animator detecte que NO hay ningún arma activa
        // y vuelva a la animación de caminar (idle)
        player.hasPistol = false;
        player.hasRiffle = false;
        player.hasGrenade = false;
        
        // Resetear todos los iconos de UI a blanco (ninguno seleccionado)
        if (player.primaryWeaponIcon != null)
            player.primaryWeaponIcon.color = Color.white;
        if (player.secondaryWeaponIcon != null)
            player.secondaryWeaponIcon.color = Color.white;
        if (player.throwableWeaponIcon != null)
            player.throwableWeaponIcon.color = Color.white;
    }
}
