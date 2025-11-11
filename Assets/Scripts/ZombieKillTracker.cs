using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieKillTracker : MonoBehaviour
{
    [Header("Kill to Heal System")]
    [Tooltip("Cantidad de zombies que debe matar para recibir curación")]
    public int killsRequiredForHeal = 5;
    
    [Tooltip("Cantidad de vida que recupera cada vez")]
    public float healAmount = 20f;
    
    [Tooltip("¿Activar sistema de curación por kills?")]
    public bool healOnKillEnabled = true;
    
    [Header("References")]
    public PlayerController playerController;
    
    [Header("Statistics")]
    public int totalZombiesKilled = 0;
    private int killsSinceLastHeal = 0;
    
    // Singleton para acceso global
    public static ZombieKillTracker Instance { get; private set; }

    void Awake()
    {
        // Configurar singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Ya existe un ZombieKillTracker en la escena. Destruyendo duplicado.");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Buscar PlayerController si no está asignado
        if (playerController == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerController = playerObj.GetComponent<PlayerController>();
            }
            
            if (playerController == null)
            {
                Debug.LogError("ZombieKillTracker: No se encontró PlayerController!");
            }
        }
    }

    // Método público para llamar cuando un zombie muere
    public void OnZombieKilled()
    {
        totalZombiesKilled++;
        killsSinceLastHeal++;
        
        Debug.Log($"🎯 Zombie eliminado! Total: {totalZombiesKilled}, Desde última curación: {killsSinceLastHeal}/{killsRequiredForHeal}");
        
        // Verificar si debe curar al jugador
        if (healOnKillEnabled && killsSinceLastHeal >= killsRequiredForHeal)
        {
            HealPlayer();
        }
    }

    void HealPlayer()
    {
        if (playerController == null)
        {
            Debug.LogWarning("No se puede curar: PlayerController no asignado");
            return;
        }
        
        // Guardar vida antes de curar para el log
        float healthBefore = playerController.currentHealth;
        
        // Curar al jugador (asegurarse de no exceder maxHealth)
        playerController.currentHealth = Mathf.Min(
            playerController.currentHealth + healAmount,
            playerController.maxHealth
        );
        
        float healthAfter = playerController.currentHealth;
        float actualHeal = healthAfter - healthBefore;
        
        // Resetear contador
        killsSinceLastHeal = 0;
        
        Debug.Log($"💚 ¡CURACIÓN! {killsRequiredForHeal} zombies eliminados → +{actualHeal} HP (Vida: {healthBefore:F0} → {healthAfter:F0})");
        
        // Opcional: Agregar efecto visual/sonido de curación aquí
        // PlayHealEffect();
    }

    // Método para obtener cuántos zombies faltan para la siguiente curación
    public int GetKillsUntilNextHeal()
    {
        return killsRequiredForHeal - killsSinceLastHeal;
    }
    
    // Método para obtener el progreso en porcentaje
    public float GetHealProgress()
    {
        return (float)killsSinceLastHeal / killsRequiredForHeal;
    }
    
    // Resetear estadísticas (útil para reiniciar nivel)
    public void ResetStats()
    {
        totalZombiesKilled = 0;
        killsSinceLastHeal = 0;
        Debug.Log("🔄 Estadísticas de kills reseteadas");
    }
}
