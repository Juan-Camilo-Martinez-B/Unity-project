using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillStreakBonus : MonoBehaviour
{
    public static KillStreakBonus Instance { get; private set; }
    
    [Header("Kill Streak Configuration")]
    [Tooltip("Cantidad de kills necesarios para bonificador")]
    public int killsRequired = 5;
    
    [Tooltip("Tiempo máximo para lograr los kills (en segundos)")]
    public float timeWindow = 8f;
    
    [Tooltip("Tiempo que se reduce del timer por cada bonificador")]
    public float timeReduction = 3f;
    
    [Header("Healing Configuration")]
    [Tooltip("Cantidad de vida que se otorga cada X kills")]
    public int healAmount = 10;
    
    [Tooltip("Cada cuántos kills se otorga vida")]
    public int killsPerHeal = 5;
    
    [Header("Current Stats")]
    public int totalKills = 0;
    public float totalTimeReduced = 0f;
    public int totalBonuses = 0;
    
    private List<float> recentKillTimes = new List<float>();
    private LevelManager levelManager;
    private PlayerController playerController;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerController = playerObj.GetComponent<PlayerController>();
    }
    
    // Llamar esta función cuando un zombie muera
    public void OnZombieKilled()
    {
        totalKills++;
        float currentTime = Time.time;
        
        // Agregar el tiempo del kill actual
        recentKillTimes.Add(currentTime);
        
        // Limpiar kills antiguos (fuera de la ventana de tiempo)
        recentKillTimes.RemoveAll(killTime => currentTime - killTime > timeWindow);
        
        Debug.Log($"💀 Kill #{totalKills} - Kills recientes en {timeWindow}s: {recentKillTimes.Count}");
        
        // Verificar si se logró un kill streak bonificador
        if (recentKillTimes.Count >= killsRequired)
        {
            ApplyTimeBonus();
            recentKillTimes.Clear(); // Resetear para el siguiente bonificador
        }
        
        // Verificar si se debe curar al jugador
        if (totalKills % killsPerHeal == 0)
        {
            HealPlayer();
        }
    }
    
    void ApplyTimeBonus()
    {
        totalBonuses++;
        totalTimeReduced += timeReduction;
        
        Debug.Log($"⚡ ¡KILL STREAK BONIFICADOR! ({killsRequired} kills en {timeWindow}s)");
        Debug.Log($"   - Tiempo reducido: {timeReduction} segundos");
        Debug.Log($"   - Tiempo total reducido: {totalTimeReduced} segundos");
        
        // Reducir el tiempo del nivel actual en el LevelManager
        if (levelManager != null)
        {
            levelManager.ReduceTime(timeReduction);
        }
        
        // Aquí podrías agregar efectos visuales/sonoros
        // Por ejemplo, mostrar un mensaje en pantalla
    }
    
    void HealPlayer()
    {
        if (playerController == null)
            return;
        
        float healedAmount = Mathf.Min(healAmount, playerController.maxHealth - playerController.currentHealth);
        
        if (healedAmount > 0)
        {
            playerController.currentHealth += healedAmount;
            Debug.Log($"💚 ¡CURACIÓN! +{healedAmount} HP ({totalKills} kills) - Vida actual: {playerController.currentHealth}/{playerController.maxHealth}");
        }
        else
        {
            Debug.Log($"❤️ Vida completa - No se aplicó curación");
        }
    }
    
    // Función para obtener el tiempo ajustado (con bonificadores)
    public float GetAdjustedTime(float originalTime)
    {
        return Mathf.Max(0, originalTime - totalTimeReduced);
    }
    
    public void ResetStats()
    {
        totalKills = 0;
        totalTimeReduced = 0f;
        totalBonuses = 0;
        recentKillTimes.Clear();
    }
}
