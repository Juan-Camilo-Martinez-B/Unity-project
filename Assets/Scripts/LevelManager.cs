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

    [Header("Initial Message")]
    public GameObject initialMessagePanel;
    public GameObject playButton;

    [Header("Player")]
    public GameObject player;

    [Header("Timer UI")]
    public TextMeshProUGUI timerText;

    [Header("Barrel Counter UI")]
    public TextMeshProUGUI barrelTargetText;
    public TextMeshProUGUI barrelCountText;

    [Header("Victory Panel")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryRecordTimeText;
    public TextMeshProUGUI victoryDestroyedBarrelsText;
    public GameObject restartButton;
    public GameObject nextLevelButton;

    [Header("Defeat Panel")]
    public GameObject defeatPanel;
    public TextMeshProUGUI defeatPlayedTimeText;
    public TextMeshProUGUI defeatDestroyedBarrelsText;

    [Header("Level Settings")]
    public string nextLevelName = "Boss"; // Nombre de la siguiente escena

    private bool isGameStarted = false;
    private bool isGamePaused = false;
    private bool isGameOver = false;
    private bool hasGameBegun = false; // Nueva variable para controlar si ya se presionó Play
    private PlayerController playerController;
    private WeaponSlots weaponSlots;
    private float elapsedTime = 0f;
    
    // Sistema de barriles
    private int totalBarrels = 0;
    private int destroyedBarrels = 0;
    private bool useBarrelSystem = false; // Determina si usar el sistema de barriles
    private string currentSceneName;

    void Start()
    {
        // Obtener el nombre de la escena actual
        currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        // Determinar si usar el sistema de barriles (solo en Laberinto)
        useBarrelSystem = (currentSceneName == "Laberinto");
        
        Debug.Log($"Escena actual: {currentSceneName}");
        Debug.Log($"Sistema de barriles: {(useBarrelSystem ? "ACTIVADO" : "DESACTIVADO")}");

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
        
        // Contar los barriles en la escena
        CountBarrels();
        
        // Ocultar paneles de victoria y derrota al inicio
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
        if (defeatPanel != null)
            defeatPanel.SetActive(false);
        
        // Ahora mostrar el menú
        ShowMainMenu();
    }

    // Contar todos los barriles en la escena
    void CountBarrels()
    {
        // Solo contar barriles si el sistema está activo (Laberinto)
        if (!useBarrelSystem)
        {
            totalBarrels = 0;
            destroyedBarrels = 0;
            
            // Ocultar la UI de barriles en niveles que no la usan
            if (barrelTargetText != null)
                barrelTargetText.transform.parent.gameObject.SetActive(false);
            if (barrelCountText != null)
                barrelCountText.transform.parent.gameObject.SetActive(false);
            
            Debug.Log("Sistema de barriles desactivado para esta escena");
            return;
        }

        GameObject[] barrels = GameObject.FindGameObjectsWithTag("Barrel");
        totalBarrels = barrels.Length;
        destroyedBarrels = 0;
        
        UpdateBarrelUI();
        
        Debug.Log($"Total de barriles en la escena: {totalBarrels}");
    }

    void Update()
    {
        // Actualizar el cronómetro solo si el juego ha comenzado (después de presionar Play)
        if (hasGameBegun && isGameStarted && !isGamePaused && !isGameOver)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
            
            // Verificar si el jugador murió
            CheckPlayerHealth();
        }

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
                else if (result.gameObject == playButton)
                {
                    Debug.Log("Click en Play!");
                    PlayGame();
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
        
        // Si no se ha presionado Play aún, mostrar el initial message
        if (!hasGameBegun)
        {
            ShowInitialMessage();
            return;
        }
        
        // Reanudar el tiempo del juego
        Time.timeScale = 1f;
        isGameStarted = true;
        isGamePaused = false;

        // Reiniciar el cronómetro solo si es la primera vez
        if (elapsedTime == 0f && timerText != null)
            timerText.gameObject.SetActive(true);

        // Reactivar controles del jugador
        if (playerController != null)
            playerController.enabled = true;
        if (weaponSlots != null)
            weaponSlots.enabled = true;

        // Bloquear y ocultar el cursor para jugar
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Mostrar el mensaje inicial con instrucciones
    void ShowInitialMessage()
    {
        if (initialMessagePanel == null)
        {
            Debug.LogError("Initial Message Panel no está asignado!");
            return;
        }

        initialMessagePanel.SetActive(true);
        
        // Mantener el juego pausado
        Time.timeScale = 0f;
        isGamePaused = true;

        // Mostrar cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Función para el botón Play (llamar desde el Inspector o desde el raycast)
    public void PlayGame()
    {
        if (initialMessagePanel != null)
            initialMessagePanel.SetActive(false);

        hasGameBegun = true;
        
        // Iniciar el juego
        Time.timeScale = 1f;
        isGameStarted = true;
        isGamePaused = false;

        // Reiniciar el cronómetro
        elapsedTime = 0f;
        if (timerText != null)
            timerText.gameObject.SetActive(true);

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

    // Actualizar el texto del cronómetro
    void UpdateTimerDisplay()
    {
        if (timerText == null)
            return;

        // Convertir el tiempo a formato mm:ss
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = Mathf.FloorToInt((elapsedTime * 100f) % 100f);

        // Mostrar en formato: 00:00:00 (minutos:segundos:centésimas)
        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }

    // Actualizar los textos de los barriles
    void UpdateBarrelUI()
    {
        if (barrelTargetText != null)
            barrelTargetText.text = totalBarrels.ToString();
        
        if (barrelCountText != null)
            barrelCountText.text = destroyedBarrels.ToString();
    }

    // Verificar la salud del jugador
    void CheckPlayerHealth()
    {
        if (playerController != null && playerController.currentHealth <= 0 && !isGameOver)
        {
            DefeatGame();
        }
    }

    // Función pública para llamar cuando se destruye un barril
    public void OnBarrelDestroyed()
    {
        // Solo procesar si el sistema de barriles está activo
        if (!useBarrelSystem || !isGameStarted)
            return;

        destroyedBarrels++;
        UpdateBarrelUI();
        
        Debug.Log($"Barril destruido! {destroyedBarrels}/{totalBarrels}");

        // Verificar si se destruyeron todos los barriles
        if (destroyedBarrels >= totalBarrels)
        {
            WinGame();
        }
    }

    // Función pública para llamar cuando el boss es derrotado
    public void OnBossDefeated()
    {
        if (!isGameStarted || isGameOver)
            return;

        Debug.Log("🏆 BOSS DERROTADO - Esperando animación de muerte...");
        
        // Iniciar corrutina para esperar antes de mostrar victoria
        StartCoroutine(BossDefeatedSequence());
    }

    // Corrutina que espera para mostrar el panel de victoria después de la muerte del boss
    IEnumerator BossDefeatedSequence()
    {
        // Esperar 5 segundos en tiempo REAL (no afectado por timeScale)
        // 2 segundos para la animación de muerte + 3 segundos para ver el ragdoll caer
        float waitTime = 5.0f;
        float elapsedWaitTime = 0f;
        
        while (elapsedWaitTime < waitTime)
        {
            elapsedWaitTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        Debug.Log("🏆 VICTORIA! Mostrando panel...");
        WinGame();
    }

    // Función que se ejecuta cuando el jugador gana
    void WinGame()
    {
        if (isGameOver) return; // Evitar ejecutar múltiples veces
        isGameOver = true;

        Debug.Log("¡VICTORIA! Todos los barriles destruidos");
        Debug.Log($"Tiempo final: {GetFormattedTime()}");
        
        // Pausar el juego
        Time.timeScale = 0f;
        isGamePaused = true;

        // Desactivar controles del jugador
        if (playerController != null)
            playerController.enabled = false;
        if (weaponSlots != null)
            weaponSlots.enabled = false;

        // Mostrar cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Mostrar panel de victoria
        ShowVictoryPanel();
    }

    // Función que se ejecuta cuando el jugador pierde
    void DefeatGame()
    {
        if (isGameOver) return; // Evitar ejecutar múltiples veces
        isGameOver = true;

        Debug.Log("DERROTA - El jugador murió");
        Debug.Log($"Tiempo jugado: {GetFormattedTime()}");
        Debug.Log($"Barriles destruidos: {destroyedBarrels}/{totalBarrels}");
        
        // Pausar el juego
        Time.timeScale = 0f;
        isGamePaused = true;

        // Desactivar controles del jugador
        if (playerController != null)
            playerController.enabled = false;
        if (weaponSlots != null)
            weaponSlots.enabled = false;

        // Mostrar cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Mostrar panel de derrota
        ShowDefeatPanel();
    }

    // Mostrar el panel de victoria
    void ShowVictoryPanel()
    {
        if (victoryPanel == null)
        {
            Debug.LogError("Victory Panel no está asignado!");
            return;
        }

        victoryPanel.SetActive(true);

        // Actualizar tiempo récord
        if (victoryRecordTimeText != null)
            victoryRecordTimeText.text = GetFormattedTime();

        // Actualizar barriles destruidos (solo si el sistema de barriles está activo)
        if (victoryDestroyedBarrelsText != null)
        {
            if (useBarrelSystem)
            {
                victoryDestroyedBarrelsText.text = $"{destroyedBarrels}/{totalBarrels}";
            }
            else
            {
                // Ocultar el texto de barriles si no se usa el sistema
                victoryDestroyedBarrelsText.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    // Mostrar el panel de derrota
    void ShowDefeatPanel()
    {
        if (defeatPanel == null)
        {
            Debug.LogError("Defeat Panel no está asignado!");
            return;
        }

        defeatPanel.SetActive(true);

        // Actualizar tiempo jugado
        if (defeatPlayedTimeText != null)
            defeatPlayedTimeText.text = GetFormattedTime();

        // Actualizar barriles destruidos (solo si el sistema de barriles está activo)
        if (defeatDestroyedBarrelsText != null)
        {
            if (useBarrelSystem)
            {
                defeatDestroyedBarrelsText.text = $"{destroyedBarrels}/{totalBarrels}";
            }
            else
            {
                // Ocultar el texto de barriles si no se usa el sistema
                defeatDestroyedBarrelsText.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    // Función para el botón Restart (llamar desde el Inspector)
    public void RestartLevel()
    {
        // Restablecer el timeScale antes de recargar
        Time.timeScale = 1f;
        
        // Resetear variables
        hasGameBegun = false;
        
        // Recargar la escena actual
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    // Función para el botón Next Level (llamar desde el Inspector)
    public void NextLevel()
    {
        // Restablecer el timeScale antes de cambiar de escena
        Time.timeScale = 1f;
        
        Debug.Log($"Cargando siguiente nivel: {nextLevelName}");
        
        // Cargar la escena especificada
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextLevelName);
    }

    // Función pública para obtener el tiempo actual (por si necesitas usarlo en otros scripts)
    public float GetElapsedTime()
    {
        return elapsedTime;
    }

    // Función pública para obtener el tiempo en formato texto
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = Mathf.FloorToInt((elapsedTime * 100f) % 100f);
        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }
}
