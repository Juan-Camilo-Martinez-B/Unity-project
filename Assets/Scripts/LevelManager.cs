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

    [Header("Victory Panels - Assign per level")]
    public GameObject victoryPanelLaberinto;
    public GameObject victoryPanelNivel2; // Para el nivel del medio cuando lo crees
    public GameObject victoryPanelBoss;

    [Header("Victory Panel Stats - Laberinto")]
    public TextMeshProUGUI laberintoLevelTimeText;
    public TextMeshProUGUI laberintoBarrelsText;

    [Header("Victory Panel Stats - Nivel 2")]
    public TextMeshProUGUI nivel2LevelTimeText;
    // Agregar más stats según necesites

    [Header("Victory Panel Stats - Boss (Final)")]
    public TextMeshProUGUI bossLaberintoTimeText;
    public TextMeshProUGUI bossNivel2TimeText;
    public TextMeshProUGUI bossBossTimeText;
    public TextMeshProUGUI bossTotalTimeText;

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
    private static float nivel2Time = 0f; // Tiempo del nivel 2
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
        
        Debug.Log($"Escena actual: {currentSceneName}");
        Debug.Log($"Sistema de barriles: {(useBarrelSystem ? "ACTIVADO" : "DESACTIVADO")}");
        Debug.Log($"Tiempo global acumulado: {GetFormattedTime(globalTime)}");

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
            case "Nivel2":
                // Si iniciamos en Nivel 2, simular que completamos el Laberinto
                if (laberintoTime == 0f)
                {
                    laberintoTime = 120f; // Tiempo simulado: 2 minutos
                    globalTime = laberintoTime;
                    Debug.Log($"🔧 [TESTING] Simulando progresión previa para Nivel 2");
                    Debug.Log($"   - Tiempo Laberinto simulado: {GetFormattedTime(laberintoTime)}");
                }
                break;
                
            case "Boss":
                // Si iniciamos en Boss, simular que completamos Laberinto y Nivel 2
                if (laberintoTime == 0f && nivel2Time == 0f)
                {
                    laberintoTime = 120f; // 2 minutos
                    nivel2Time = 180f;    // 3 minutos
                    globalTime = laberintoTime + nivel2Time;
                    Debug.Log($"🔧 [TESTING] Simulando progresión previa para Boss");
                    Debug.Log($"   - Tiempo Laberinto simulado: {GetFormattedTime(laberintoTime)}");
                    Debug.Log($"   - Tiempo Nivel 2 simulado: {GetFormattedTime(nivel2Time)}");
                    Debug.Log($"   - Tiempo global simulado: {GetFormattedTime(globalTime)}");
                }
                break;
                
            case "Laberinto":
                // Si iniciamos en Laberinto, resetear todo (es el nivel inicial)
                Debug.Log($"🎮 Iniciando desde el primer nivel (Laberinto)");
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
        if (victoryPanelNivel2 != null)
            victoryPanelNivel2.SetActive(false);
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
        
        Debug.Log($"Barril destruido! {destroyedBarrels}/{totalBarrels}");

        // Verificar si se destruyeron todos los barriles
        if (destroyedBarrels >= totalBarrels)
        {
            // DETENER TIMER INMEDIATAMENTE al completar el objetivo
            isGameOver = true;
            Debug.Log($"⏱️ TIMER DETENIDO - Tiempo final: {GetFormattedTime(currentLevelTime)}");
            
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
        Debug.Log($"🏆 BOSS DERROTADO - Timer detenido en: {GetFormattedTime(currentLevelTime)}");
        Debug.Log("⏳ Esperando animación de muerte...");
        
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
        // NOTA: isGameOver ya se estableció en true cuando se completó el objetivo
        // Esto evita que el timer siga corriendo durante la espera del panel
        
        // Guardar el tiempo del nivel actual en la variable estática correspondiente
        switch (currentSceneName)
        {
            case "Laberinto":
                laberintoTime = currentLevelTime;
                Debug.Log($"✅ ¡VICTORIA LABERINTO! Tiempo final: {GetFormattedTime(currentLevelTime)}");
                break;
            case "Nivel2":
                nivel2Time = currentLevelTime;
                Debug.Log($"✅ ¡VICTORIA NIVEL 2! Tiempo final: {GetFormattedTime(currentLevelTime)}");
                break;
            case "Boss":
                bossTime = currentLevelTime;
                Debug.Log($"✅ ¡VICTORIA FINAL! Tiempo Boss: {GetFormattedTime(currentLevelTime)}");
                Debug.Log($"📊 Tiempo Total del juego: {GetFormattedTime(globalTime)}");
                break;
        }

        Debug.Log($"⏱️ Tiempo del nivel guardado: {GetFormattedTime(currentLevelTime)}");
        Debug.Log($"🌍 Tiempo global acumulado: {GetFormattedTime(globalTime)}");
        
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

        // Mostrar panel de victoria correspondiente
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
        // Determinar qué panel mostrar según el nivel actual
        GameObject panelToShow = null;
        
        switch (currentSceneName)
        {
            case "Laberinto":
                panelToShow = victoryPanelLaberinto;
                
                // Actualizar estadísticas del Laberinto
                if (laberintoLevelTimeText != null)
                    laberintoLevelTimeText.text = GetFormattedTime(laberintoTime);
                
                if (laberintoBarrelsText != null && useBarrelSystem)
                    laberintoBarrelsText.text = $"{destroyedBarrels}/{totalBarrels}";
                
                Debug.Log($"Mostrando panel de victoria del Laberinto - Tiempo: {GetFormattedTime(laberintoTime)}");
                break;
                
            case "Nivel2":
                panelToShow = victoryPanelNivel2;
                
                // Actualizar estadísticas del Nivel 2
                if (nivel2LevelTimeText != null)
                    nivel2LevelTimeText.text = GetFormattedTime(nivel2Time);
                
                Debug.Log($"Mostrando panel de victoria del Nivel 2 - Tiempo: {GetFormattedTime(nivel2Time)}");
                break;
                
            case "Boss":
                panelToShow = victoryPanelBoss;
                
                // Actualizar estadísticas finales del Boss (mostrar todos los tiempos)
                if (bossLaberintoTimeText != null)
                    bossLaberintoTimeText.text = GetFormattedTime(laberintoTime);
                
                if (bossNivel2TimeText != null)
                    bossNivel2TimeText.text = GetFormattedTime(nivel2Time);
                
                if (bossBossTimeText != null)
                    bossBossTimeText.text = GetFormattedTime(bossTime);
                
                if (bossTotalTimeText != null)
                    bossTotalTimeText.text = GetFormattedTime(globalTime);
                
                Debug.Log($"Mostrando panel de victoria FINAL:");
                Debug.Log($"  - Tiempo Laberinto: {GetFormattedTime(laberintoTime)}");
                Debug.Log($"  - Tiempo Nivel 2: {GetFormattedTime(nivel2Time)}");
                Debug.Log($"  - Tiempo Boss: {GetFormattedTime(bossTime)}");
                Debug.Log($"  - Tiempo TOTAL: {GetFormattedTime(globalTime)}");
                break;
                
            default:
                Debug.LogWarning($"Escena '{currentSceneName}' no tiene panel de victoria asignado");
                return;
        }
        
        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
        }
        else
        {
            Debug.LogError($"Panel de victoria para '{currentSceneName}' no está asignado en el Inspector!");
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
    }

    // Función para el botón Restart (llamar desde el Inspector)
    public void RestartLevel()
    {
        // Restablecer el timeScale antes de recargar
        Time.timeScale = 1f;
        
        // Resetear variables
        hasGameBegun = false;
        
        // Si es el panel de victoria, solo recarga el nivel actual
        // Si es el panel de derrota, reinicia TODA la progresión del juego
        if (isGameOver && defeatPanel != null && defeatPanel.activeSelf)
        {
            // DERROTA: Reiniciar TODO el juego desde el primer nivel
            Debug.Log("DERROTA - Reiniciando progresión completa del juego");
            
            // Resetear todos los tiempos estáticos
            globalTime = 0f;
            laberintoTime = 0f;
            nivel2Time = 0f;
            bossTime = 0f;
            
            // Cargar siempre el primer nivel (Laberinto)
            UnityEngine.SceneManagement.SceneManager.LoadScene("Laberinto");
        }
        else
        {
            // VICTORIA: Solo reiniciar el nivel actual
            Debug.Log($"VICTORIA - Reiniciando nivel actual: {currentSceneName}");
            
            // Recargar la escena actual sin resetear tiempos globales
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneName);
        }
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
                nextScene = "Nivel2";
                Debug.Log("Avanzando del Laberinto al Nivel 2");
                break;
                
            case "Nivel2":
                nextScene = "Boss";
                Debug.Log("Avanzando del Nivel 2 al Boss");
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
}
