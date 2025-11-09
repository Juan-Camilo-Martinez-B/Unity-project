using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Menu Panel")]
    public GameObject menuPanel;

    [Header("Menu Buttons")]
    public GameObject startButton;
    public GameObject resumeButton;
    public GameObject exitButton;

    [Header("Player")]
    public GameObject player;

    private bool isGameStarted = false;
    private bool isGamePaused = false;
    private PlayerController playerController;
    private WeaponSlots weaponSlots;

    void Start()
    {
        // Obtener referencia al PlayerController
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            weaponSlots = player.GetComponentInChildren<WeaponSlots>();
        }

        // Esperar un frame para que el EventSystem se inicialice
        StartCoroutine(InitializeMenuAfterFrame());
    }

    IEnumerator InitializeMenuAfterFrame()
    {
        // Esperar al final del frame para asegurar que EventSystem esté listo
        yield return new WaitForEndOfFrame();
        
        // Ahora mostrar el menú
        ShowMainMenu();
    }

    void Update()
    {
        // Detectar clicks en botones cuando el juego está pausado (con raycast manual)
        if (Input.GetMouseButtonDown(0) && isGamePaused)
        {
            // Verificar que EventSystem esté disponible
            if (EventSystem.current == null)
            {
                Debug.LogWarning("EventSystem no disponible");
                return;
            }

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            Debug.Log($"Raycast detectó {results.Count} objetos");

            foreach (RaycastResult result in results)
            {
                Debug.Log($"Objeto detectado: {result.gameObject.name}");
                
                if (result.gameObject == startButton)
                {
                    Debug.Log("Click en Start!");
                    StartGame();
                    return;
                }
                else if (result.gameObject == resumeButton)
                {
                    Debug.Log("Click en Resume!");
                    ResumeGame();
                    return;
                }
                else if (result.gameObject == exitButton)
                {
                    Debug.Log("Click en Exit!");
                    ExitGame();
                    return;
                }
            }
        }

        // Detectar tecla Enter para iniciar desde el menú
        if (!isGameStarted && Input.GetKeyDown(KeyCode.Return))
        {
            StartGame();
        }
        
        // Solo permitir pausar si el juego ya comenzó
        if (isGameStarted && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // Mostrar menú principal (al inicio del juego)
    void ShowMainMenu()
    {
        menuPanel.SetActive(true);
        
        // Mostrar botones de menú principal
        startButton.SetActive(true);
        resumeButton.SetActive(false);
        exitButton.SetActive(true);

        // Pausar el juego y desactivar controles del jugador
        Time.timeScale = 0f;
        isGamePaused = true;
        isGameStarted = false;

        // Desactivar controles del jugador
        if (playerController != null)
            playerController.enabled = false;
        if (weaponSlots != null)
            weaponSlots.enabled = false;

        // Desbloquear y mostrar el cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Mostrar menú de pausa (durante el juego)
    void ShowPauseMenu()
    {
        menuPanel.SetActive(true);
        
        // Mostrar botones de pausa
        startButton.SetActive(false);
        resumeButton.SetActive(true);
        exitButton.SetActive(true);

        // Pausar el juego y desactivar controles
        Time.timeScale = 0f;
        isGamePaused = true;

        // Desactivar controles del jugador
        if (playerController != null)
            playerController.enabled = false;
        if (weaponSlots != null)
            weaponSlots.enabled = false;

        // Desbloquear y mostrar el cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Función para el botón Start (llamar desde el Inspector)
    public void StartGame()
    {
        menuPanel.SetActive(false);
        
        // Reanudar el tiempo del juego
        Time.timeScale = 1f;
        isGameStarted = true;
        isGamePaused = false;

        // Reactivar controles del jugador
        if (playerController != null)
            playerController.enabled = true;
        if (weaponSlots != null)
            weaponSlots.enabled = true;

        // Bloquear y ocultar el cursor para jugar
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Función para el botón Resume (llamar desde el Inspector)
    public void ResumeGame()
    {
        menuPanel.SetActive(false);
        
        // Reanudar el tiempo del juego
        Time.timeScale = 1f;
        isGamePaused = false;

        // Reactivar controles del jugador
        if (playerController != null)
            playerController.enabled = true;
        if (weaponSlots != null)
            weaponSlots.enabled = true;

        // Bloquear y ocultar el cursor para jugar
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Pausar el juego con Escape
    void PauseGame()
    {
        ShowPauseMenu();
    }

    // Función para el botón Exit (llamar desde el Inspector)
    public void ExitGame()
    {
        #if UNITY_EDITOR
            // Si estamos en el editor de Unity, detener el juego
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // Si es un ejecutable, cerrar la aplicación
            Application.Quit();
        #endif
    }
}
