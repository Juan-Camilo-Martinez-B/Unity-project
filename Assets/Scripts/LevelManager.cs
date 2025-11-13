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

    [Header("Horde Counter UI - Solo para Industry")]
    public TextMeshProUGUI hordeCounterText;

    [Header("Victory Panels - Assign per level")]
    public GameObject victoryPanelLaberinto;
    public GameObject victoryPanelIndustry;
    public GameObject victoryPanelBoss;

    [Header("Victory Panel Stats - Laberinto")]
    public TextMeshProUGUI laberintoLevelTimeText;
    public TextMeshProUGUI laberintoBarrelsText;

    [Header("Victory Panel Stats - Industry")]
    public TextMeshProUGUI industryPlayedTimeText;      // Tiempo jugado original
    public TextMeshProUGUI industryAdjustedTimeText;    // Tiempo con bonificadores aplicados
    public TextMeshProUGUI industryTimeReducedText;     // Tiempo reducido por bonificadores
    public TextMeshProUGUI industryHordeProgressText;   // Hordas completadas (X/3)

    [Header("Victory Panel Stats - Boss (Final)")]
    public TextMeshProUGUI bossLaberintoTimeText;
    public TextMeshProUGUI bossIndustryTimeText;
    public TextMeshProUGUI bossBossTimeText;
    public TextMeshProUGUI bossTotalTimeText;
    public TextMeshProUGUI bossRankText; // S+, S, A, B, C
    
    [Header("Defeat Panel - Industry")]
    public TextMeshProUGUI defeatIndustryTimeText;
    public TextMeshProUGUI defeatIndustryHordeText;

    [Header("Defeat Panel")]
    public GameObject defeatPanel;
    public TextMeshProUGUI defeatPlayedTimeText;
    public TextMeshProUGUI defeatDestroyedBarrelsText;
    public GameObject defeatRestartButton;
    public GameObject defeatExitButton;

    [Header("Level Settings")]
    public float victoryDelayTime = 3.0f; // Tiempo de espera antes de mostrar victoria
    [Tooltip("Activar para testing: simula tiempos previos si inicias desde niveles intermedios")]
    public bool allowTestingFromAnyLevel = true;

    private bool isGameStarted = false;
    private bool isGamePaused = false;
    private bool isGameOver = false;
    private bool hasGameBegun = false;
    private PlayerController playerController;
    private WeaponSlots weaponSlots;
    
    // Timers
    private float currentLevelTime = 0f; // Tiempo del nivel actual
    private static float globalTime = 0f; // Tiempo total del juego
    private static float laberintoTime = 0f; // Tiempo del nivel Laberinto
    private static float industryRawTime = 0f; // Tiempo jugado original de Industry
    private static float industryTime = 0f; // Tiempo del nivel Industry (ajustado con bonus)
    private static float bossTime = 0f; // Tiempo del nivel Boss
    private static bool isFirstLevelLoad = true; // Para detectar si es la primera carga del juego
    
    // Sistema de barriles
    private int totalBarrels = 0;
    private int destroyedBarrels = 0;
    private bool useBarrelSystem = false;
    private string currentSceneName;

    void Start()
    {
        
        // Obtener el nombre de la escena actual
        currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        
        // Si es la primera carga del juego y estamos en testing, simular progresión previa
        if (isFirstLevelLoad && allowTestingFromAnyLevel)
        {
            SimulateProgressionForTesting();
            isFirstLevelLoad = false;
        }
        
        // Determinar si usar el sistema de barriles (solo en Laberinto)
        useBarrelSystem = (currentSceneName == "Laberinto");
        

        // Obtener referencia al PlayerController
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            weaponSlots = player.GetComponentInChildren<WeaponSlots>();
        }

        // Esperar un frame para que el EventSystem se inicialice
        StartCoroutine(InitializeMenuAfterFrame());
    }

    // Simular progresión previa si iniciamos desde un nivel intermedio (para testing en Unity)
    void SimulateProgressionForTesting()
    {
        switch (currentSceneName)
        {
            case "Industry":
                // Si iniciamos en Industry, simular que completamos el Laberinto
                if (laberintoTime == 0f)
                {
                    laberintoTime = 120f; // Tiempo simulado: 2 minutos
                    globalTime = laberintoTime;
                }
                break;
                
            case "Boss":
                // Si iniciamos en Boss, simular que completamos Laberinto e Industry
                if (laberintoTime == 0f && industryTime == 0f)
                {
                    laberintoTime = 120f; // 2 minutos
                    industryTime = 150f;    // 2.5 minutos (ya con bonus aplicado simulado)
                    globalTime = laberintoTime + industryTime;
                }
                break;
                
            case "Laberinto":
                // Si iniciamos en Laberinto, resetear todo (es el nivel inicial)
                break;
        }
    }

    IEnumerator InitializeMenuAfterFrame()
    {
        // Esperar al final del frame para asegurar que EventSystem esté listo
        yield return new WaitForEndOfFrame();
        
        // Contar los barriles en la escena
        CountBarrels();
        
        // Ocultar TODOS los paneles de victoria y derrota al inicio
        if (victoryPanelLaberinto != null)
            victoryPanelLaberinto.SetActive(false);
        if (victoryPanelIndustry != null)
            victoryPanelIndustry.SetActive(false);
        if (victoryPanelBoss != null)
            victoryPanelBoss.SetActive(false);
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
            
            return;
        }

        GameObject[] barrels = GameObject.FindGameObjectsWithTag("Barrel");
        totalBarrels = barrels.Length;
        destroyedBarrels = 0;
        
        UpdateBarrelUI();
        
    }

    void Update()
    {
        // Actualizar los cronómetros solo si el juego ha comenzado
        if (hasGameBegun && isGameStarted && !isGamePaused && !isGameOver)
        {
            float deltaTime = Time.deltaTime;
            currentLevelTime += deltaTime; // Timer del nivel actual
            globalTime += deltaTime; // Timer global
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
                return;
            }

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);


            foreach (RaycastResult result in results)
            {
                
                if (result.gameObject == startButton)
                {
                    StartGame();
                    return;
                }
                else if (result.gameObject == resumeButton)
                {
                    ResumeGame();
                    return;
                }
                else if (result.gameObject == exitButton)
                {
                    ExitGame();
                    return;
                }
                else if (result.gameObject == playButton)
                {
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

        // Reiniciar el cronómetro del nivel solo si es la primera vez
        if (currentLevelTime == 0f && timerText != null)
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

        // Reiniciar el cronómetro del nivel actual
        currentLevelTime = 0f;
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
        int minutes = Mathf.FloorToInt(currentLevelTime / 60f);
        int seconds = Mathf.FloorToInt(currentLevelTime % 60f);
        int milliseconds = Mathf.FloorToInt((currentLevelTime * 100f) % 100f);

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
        

        // Verificar si se destruyeron todos los barriles
        if (destroyedBarrels >= totalBarrels)
        {
            // DETENER TIMER INMEDIATAMENTE al completar el objetivo
            isGameOver = true;
            
            WinGame();
        }
    }

    // Función pública para llamar cuando el boss es derrotado
    public void OnBossDefeated()
    {
        if (!isGameStarted || isGameOver)
            return;

        // DETENER TIMER INMEDIATAMENTE al derrotar al boss
        isGameOver = true;
        
        // Iniciar corrutina para esperar antes de mostrar victoria
        StartCoroutine(BossDefeatedSequence());
    }
    
    // Función específica para cuando se completan todas las hordas en Industry
    public void OnAllHordesCompleted()
    {
        
        if (!isGameStarted || isGameOver)
        {
            return;
        }

        // DETENER TIMER INMEDIATAMENTE al eliminar todos los zombies
        isGameOver = true;
        
        // Para Industry no hay animación de muerte del boss, mostrar victoria inmediatamente
        WinGame();
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
        
        WinGame();
    }

    // Función que se ejecuta cuando el jugador gana
    void WinGame()
    {
        // NOTA: isGameOver ya se estableció en true cuando se completó el objetivo
        // Esto evita que el timer siga corriendo durante la espera del panel
        
        // Guardar el tiempo del nivel actual en la variable estática correspondiente
        switch (currentSceneName)
        {
            case "Laberinto":
                laberintoTime = currentLevelTime;
                break;
            case "Industry":
                // Guardar el tiempo jugado original y el ajustado con bonificadores
                industryRawTime = currentLevelTime;
                KillStreakBonus killStreak = FindObjectOfType<KillStreakBonus>();
                if (killStreak != null)
                {
                    industryTime = killStreak.GetAdjustedTime(industryRawTime);
                }
                else
                {
                    industryTime = industryRawTime;
                }
                break;
            case "Boss":
                bossTime = currentLevelTime;
                break;
        }

        
        // Pausar el juego y desactivar controles
        PauseGameAndDisableControls();

        // Agregar delay antes de mostrar panel (SOLO para nivel Boss)
        if (currentSceneName == "Boss")
        {
            StartCoroutine(ShowVictoryPanelWithDelay());
        }
        else
        {
            // Para Laberinto e Industry, mostrar panel inmediatamente
            ShowVictoryPanel();
        }
    }
    
    // Helper: Pausar juego y desactivar todos los controles
    void PauseGameAndDisableControls()
    {
        Time.timeScale = 0f;
        isGamePaused = true;

        // Desactivar controles del jugador
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Desactivar sistema de armas completamente
        if (weaponSlots != null)
        {
            weaponSlots.enabled = false;
            if (weaponSlots.primarySlot != null)
                weaponSlots.primarySlot.gameObject.SetActive(false);
            if (weaponSlots.secondarySlot != null)
                weaponSlots.secondarySlot.gameObject.SetActive(false);
            if (weaponSlots.throwableSlot != null)
                weaponSlots.throwableSlot.gameObject.SetActive(false);
        }

        // Mostrar cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    // Mostrar panel de victoria con delay
    IEnumerator ShowVictoryPanelWithDelay()
    {
        yield return new WaitForSecondsRealtime(victoryDelayTime); // Usa realtime porque Time.timeScale = 0
        ShowVictoryPanel();
    }

    // Función que se ejecuta cuando el jugador pierde
    void DefeatGame()
    {
        if (isGameOver) return; // Evitar ejecutar múltiples veces
        isGameOver = true;

        
        // Pausar el juego y desactivar controles
        PauseGameAndDisableControls();

        // Mostrar panel de derrota
        ShowDefeatPanel();
    }

    // Mostrar el panel de victoria
    void ShowVictoryPanel()
    {
        
        // Determinar qué panel mostrar según el nivel actual
        GameObject panelToShow = null;
        
        switch (currentSceneName)
        {
            case "Laberinto":
            {
                panelToShow = victoryPanelLaberinto;
                
                
                // PRIMERO activar el panel para que los componentes estén disponibles
                if (panelToShow != null)
                {
                    panelToShow.SetActive(true);
                }
                else
                {
                    return;
                }
                
                // LUEGO actualizar los textos
                if (laberintoLevelTimeText != null)
                {
                    string formattedTime = GetFormattedTime(laberintoTime);
                    laberintoLevelTimeText.text = formattedTime;
                }
                else
                {
                }
                
                if (laberintoBarrelsText != null)
                {
                    string barrelsText = $"{destroyedBarrels}/{totalBarrels}";
                    laberintoBarrelsText.text = barrelsText;
                }
                else
                {
                }
                
                break;
            }
                
            case "Industry":
            {
                panelToShow = victoryPanelIndustry;
                // PRIMERO verificar que el panel esté asignado
                if (panelToShow == null)
                {
                    return;
                }
                // Activar el panel
                panelToShow.SetActive(true);
                // Actualizar estadísticas del Industry
                HordeManager hordeManager = FindObjectOfType<HordeManager>();
                if (industryPlayedTimeText != null)
                {
                    industryPlayedTimeText.text = GetFormattedTime(industryRawTime);
                }
                else
                {
                }
                if (industryAdjustedTimeText != null)
                {
                    industryAdjustedTimeText.text = GetFormattedTime(industryTime);
                }
                else
                {
                }
                if (industryTimeReducedText != null)
                {
                    float timeReduced = industryRawTime - industryTime;
                    industryTimeReducedText.text = GetFormattedTime(timeReduced);
                }
                else
                {
                }
                if (hordeManager != null && industryHordeProgressText != null)
                {
                    string progress = hordeManager.GetHordeProgress();
                    industryHordeProgressText.text = progress;
                }
                else if (industryHordeProgressText == null)
                {
                }
                break;
            }
                
            case "Boss":
            {
                panelToShow = victoryPanelBoss;
                
                // Activar el panel primero
                if (panelToShow != null)
                {
                    panelToShow.SetActive(true);
                }
                
                // El tiempo de Industry YA está ajustado con bonificadores (guardado en WinGame)
                // No necesitamos recalcularlo aquí
                float totalAdjustedTime = laberintoTime + industryTime + bossTime;
                
                // Calcular el rango basado en el tiempo total
                string rank = CalculateRank(totalAdjustedTime);
                
                // Actualizar estadísticas finales del Boss (mostrar todos los tiempos)
                if (bossLaberintoTimeText != null)
                    bossLaberintoTimeText.text = GetFormattedTime(laberintoTime);
                
                if (bossIndustryTimeText != null)
                    bossIndustryTimeText.text = GetFormattedTime(industryTime);
                
                if (bossBossTimeText != null)
                    bossBossTimeText.text = GetFormattedTime(bossTime);
                
                if (bossTotalTimeText != null)
                    bossTotalTimeText.text = GetFormattedTime(totalAdjustedTime);
                
                if (bossRankText != null)
                    bossRankText.text = rank;
                
                break;
            }
                
            default:
                return;
        }
        
        // Ya no es necesario activar el panel aquí porque se activa en cada case
    }

    // Mostrar el panel de derrota
    void ShowDefeatPanel()
    {
        if (defeatPanel == null)
        {
            return;
        }

        defeatPanel.SetActive(true);
        

        // Actualizar tiempo jugado del nivel actual
        if (defeatPlayedTimeText != null)
            defeatPlayedTimeText.text = GetFormattedTime(currentLevelTime);

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
        
        // Actualizar estadísticas específicas para Industry
        if (currentSceneName == "Industry")
        {
            HordeManager hordeManager = FindObjectOfType<HordeManager>();
            
            if (defeatIndustryTimeText != null)
            {
                defeatIndustryTimeText.text = GetFormattedTime(currentLevelTime);
            }
            
            if (hordeManager != null && defeatIndustryHordeText != null)
            {
                string progress = hordeManager.GetHordeProgress();
                defeatIndustryHordeText.text = progress;
                Debug.Log($"✓ Progreso hordas en derrota: {progress}");
            }
        }
        
        Debug.Log("=== FIN PANEL DE DERROTA ===");
    }

    // Función para REINICIAR EL JUEGO COMPLETO (llamar desde el Inspector)
    public void RestartGame()
    {
        // Restablecer el timeScale antes de recargar
        Time.timeScale = 1f;
        
        Debug.Log("🔄 REINICIANDO JUEGO COMPLETO - Volviendo al inicio");
        
        // Resetear todos los tiempos estáticos (toda la progresión)
        globalTime = 0f;
        laberintoTime = 0f;
        industryTime = 0f;
        bossTime = 0f;
        
        // Resetear variables
        hasGameBegun = false;
        isGameOver = false;
        
        // Cargar siempre el primer nivel (Laberinto)
        UnityEngine.SceneManagement.SceneManager.LoadScene("Laberinto");
    }

    // Función para el botón Next Level (llamar desde el Inspector)
    public void NextLevel()
    {
        // Restablecer el timeScale antes de cambiar de escena
        Time.timeScale = 1f;
        
        // Determinar el siguiente nivel según el actual
        string nextScene = "";
        
        switch (currentSceneName)
        {
            case "Laberinto":
                // Verificar si existe la escena Industry en el Build Settings
                if (SceneExists("Industry"))
                {
                    nextScene = "Industry";
                    Debug.Log("Avanzando del Laberinto a Industry");
                }
                else
                {
                    // Si no existe Industry, saltar directamente al Boss
                    nextScene = "Boss";
                    Debug.Log("⚠️ Industry no encontrado en Build Settings - Saltando directamente al Boss");
                }
                break;
                
            case "Industry":
                nextScene = "Boss";
                Debug.Log("Avanzando de Industry al Boss");
                break;
                
            case "Boss":
                // Ya es el último nivel, podrías cargar un menú principal o créditos
                Debug.Log("¡Juego completado! Volviendo al menú principal");
                // Por ahora, vuelve a cargar el Boss (puedes cambiarlo a "MainMenu" si tienes uno)
                nextScene = "Boss";
                break;
                
            default:
                Debug.LogWarning($"Nivel '{currentSceneName}' no tiene siguiente nivel definido");
                nextScene = currentSceneName; // Recargar el mismo nivel por defecto
                break;
        }
        
        // Cargar la escena siguiente
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextScene);
    }

    // Verificar si una escena existe en el Build Settings
    bool SceneExists(string sceneName)
    {
        int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneNameFromPath == sceneName)
            {
                Debug.Log($"✅ Escena '{sceneName}' encontrada en Build Settings (índice {i})");
                return true;
            }
        }
        
        Debug.Log($"❌ Escena '{sceneName}' NO encontrada en Build Settings");
        return false;
    }

    // Función pública para obtener el tiempo actual del nivel (reemplaza GetElapsedTime)
    public float GetCurrentLevelTime()
    {
        return currentLevelTime;
    }

    // Función pública para obtener el tiempo global acumulado
    public float GetGlobalTime()
    {
        return globalTime;
    }

    // Función pública para obtener el tiempo en formato texto (sobrecargada)
    public string GetFormattedTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }
    
    // Versión sin parámetros usa el tiempo del nivel actual
    public string GetFormattedTime()
    {
        return GetFormattedTime(currentLevelTime);
    }
    
    // Función para reducir el tiempo del nivel (bonificadores)
    public void ReduceTime(float seconds)
    {
        currentLevelTime -= seconds;
        globalTime -= seconds;
        
        // Asegurar que no sea negativo
        currentLevelTime = Mathf.Max(0, currentLevelTime);
        globalTime = Mathf.Max(0, globalTime);
        
        Debug.Log($"⚡ Tiempo reducido en {seconds}s - Nuevo tiempo: {GetFormattedTime(currentLevelTime)}");
    }
    
    // Función para actualizar el contador de hordas (llamada por HordeManager)
    public void UpdateHordeCounter(int currentHorde, int totalHordes)
    {
        if (hordeCounterText != null)
        {
            hordeCounterText.text = $"Horda: {currentHorde}/{totalHordes}";
        }
    }
    
    // Calcular el rango basado en el tiempo total (como Resident Evil)
    string CalculateRank(float totalTimeInSeconds)
    {
        float totalMinutes = totalTimeInSeconds / 60f;
        
        if (totalMinutes < 5f)
        {
            return "S+";
        }
        else if (totalMinutes < 5.5f)
        {
            return "S";
        }
        else if (totalMinutes < 6f)
        {
            return "A";
        }
        else if (totalMinutes < 6.5f)
        {
            return "B";
        }
        else
        {
            return "C";
        }
    }
}
