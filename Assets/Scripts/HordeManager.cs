using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class HordeManager : MonoBehaviour
{
    [Header("Horde Configuration")]
    [Tooltip("Prefabs de enemigos a spawnear (Zombie, Demon, etc.)")]
    public GameObject[] enemyPrefabs; // Array de prefabs
    
    [Tooltip("Radio del área de spawn alrededor de este objeto")]
    public float spawnRadius = 20f;
    
    [Tooltip("Altura para verificar NavMesh")]
    public float navMeshCheckHeight = 5f;
    
    [Tooltip("Forzar modo Horde en los zombies (recomendado: true)")]
    public bool overrideBehaviorMode = true;
    
    [Header("Horde Progression")]
    [Tooltip("Configuración de cada horda: cantidad, velocidad, daño")]
    public HordeWave[] hordeWaves = new HordeWave[]
    {
        new HordeWave { zombieCount = 5,  runSpeed = 3.5f, attackDamage = 20f }, // Horda 1
        new HordeWave { zombieCount = 15, runSpeed = 4.5f, attackDamage = 20f }, // Horda 2
        new HordeWave { zombieCount = 30, runSpeed = 5.5f, attackDamage = 30f }  // Horda 3
    };
    
    [Header("Current State")]
    public int currentHordeIndex = 0; // 0-based (0 = Horda 1)
    public int zombiesRemainingInHorde = 0;
    public bool hordeActive = false;
    
    [Header("Timing")]
    [Tooltip("Tiempo de espera antes de iniciar la primera horda")]
    public float initialDelay = 2f;
    
    [Tooltip("Tiempo de espera entre hordas")]
    public float timeBetweenHordes = 5f;
    
    private List<GameObject> activeZombies = new List<GameObject>();
    private Transform player;
    private bool allHordesCompleted = false;
    private int lastZombieCount = -1; // Para debug
    
    void Start()
    {
        
        // Buscar al jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
        }
        
        // Verificar que el LevelManager existe en la escena desde el inicio
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
        }
        else
        {
        }
        
        // Iniciar la primera horda con delay inicial
        StartCoroutine(StartFirstHorde());
    }
    
    IEnumerator StartFirstHorde()
    {
        yield return new WaitForSeconds(initialDelay);
        
        // Ahora sí iniciar la primera horda
        StartCoroutine(StartNextHorde());
    }
    
    void Update()
    {
        // Limpiar referencias de zombies muertos (null o inactivos)
        activeZombies.RemoveAll(zombie => zombie == null || !zombie.activeInHierarchy);
        
        // Log de debug solo cuando cambia el número de zombies
        if (activeZombies.Count != lastZombieCount)
        {
            lastZombieCount = activeZombies.Count;
        }
        
        // Verificar si la horda actual se completó
        if (hordeActive && activeZombies.Count == 0 && zombiesRemainingInHorde == 0)
        {
            HordeCompleted();
        }
    }
    
    IEnumerator StartNextHorde()
    {
        if (currentHordeIndex >= hordeWaves.Length)
        {
            AllHordesCompleted();
            yield break;
        }
        
        // Esperar antes de iniciar la horda
        if (currentHordeIndex > 0)
        {
            yield return new WaitForSeconds(timeBetweenHordes);
        }
        
        HordeWave currentWave = hordeWaves[currentHordeIndex];
        zombiesRemainingInHorde = currentWave.zombieCount;
        hordeActive = true;
        
        
        // Actualizar el contador de hordas en la UI
        UpdateHordeUI();
        
        // Spawnear todos los zombies de la horda
        for (int i = 0; i < currentWave.zombieCount; i++)
        {
            SpawnZombie(currentWave);
            yield return new WaitForSeconds(0.2f); // Pequeño delay entre spawns
        }
        
        zombiesRemainingInHorde = 0; // Ya spawneamos todos
    }
    
    void SpawnZombie(HordeWave wave)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            return;
        }
        
        // Seleccionar un prefab aleatorio del array
        GameObject selectedPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        
        if (selectedPrefab == null)
        {
            return;
        }
        
        // Generar posición de spawn aleatoria
        Vector3 spawnPosition = GetRandomNavMeshPosition();
        
        if (spawnPosition == Vector3.zero)
        {
            return;
        }
        
        // Instanciar enemigo
        GameObject enemy = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
        string enemyType = selectedPrefab.name.Contains("Demon") ? "Demon" : "Zombie";
        enemy.name = $"{enemyType}_Horde{currentHordeIndex + 1}_{activeZombies.Count + 1}";
        
        // Intentar configurar como Zombie
        ZombieController zombieController = enemy.GetComponent<ZombieController>();
        if (zombieController != null)
        {
            // Forzar modo Horde si está activado el override
            if (overrideBehaviorMode)
            {
                zombieController.behaviorMode = ZombieBehaviorMode.Horde;
            }
            
            // Asignar jugador
            zombieController.player = player;
            
            // Configurar velocidad y daño de la horda actual
            zombieController.runSpeed = wave.runSpeed;
            zombieController.attackDamage = wave.attackDamage;
            
        }
        
        // Intentar configurar como Demon
        DemonController demonController = enemy.GetComponent<DemonController>();
        if (demonController != null)
        {
            // Forzar modo Horde si está activado el override
            if (overrideBehaviorMode)
            {
                demonController.behaviorMode = DemonBehaviorMode.Horde;
            }
            
            // Asignar jugador
            demonController.player = player;
            
            // Configurar velocidad y daño de la horda actual
            demonController.runSpeed = wave.runSpeed;
            demonController.attackDamage = wave.attackDamage;
            
        }
        
        activeZombies.Add(enemy);
    }
    
    Vector3 GetRandomNavMeshPosition()
    {
        for (int i = 0; i < 30; i++) // 30 intentos
        {
            // Generar posición aleatoria en círculo
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Verificar si hay NavMesh en esa posición
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, navMeshCheckHeight, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        
        return Vector3.zero;
    }
    
    void HordeCompleted()
    {
        if (!hordeActive) return; // Evitar llamadas múltiples
        
        hordeActive = false; // Desactivar inmediatamente para evitar re-entrada
        
        
        // PRIMERO verificar si era la última horda ANTES de incrementar
        if (currentHordeIndex + 1 >= hordeWaves.Length)
        {
            // Era la última horda, victoria inmediata
            currentHordeIndex++;
            AllHordesCompleted();
        }
        else
        {
            // Hay más hordas, incrementar y continuar
            currentHordeIndex++;
            StartCoroutine(StartNextHorde());
        }
    }
    
    void AllHordesCompleted()
    {
        if (allHordesCompleted) return;
        allHordesCompleted = true;
        
        
        // Notificar al LevelManager usando la función específica para Industry
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        
        if (levelManager == null)
        {
            return;
        }
        
        levelManager.OnAllHordesCompleted(); // Función específica para Industry
    }
    
    // Función pública para obtener el progreso actual
    public string GetHordeProgress()
    {
        return $"{Mathf.Min(currentHordeIndex, hordeWaves.Length)}/{hordeWaves.Length}";
    }
    
    public int GetCurrentHordeNumber()
    {
        return Mathf.Min(currentHordeIndex, hordeWaves.Length);
    }
    
    public int GetTotalHordes()
    {
        return hordeWaves.Length;
    }
    
    // Actualizar el contador de hordas en la UI
    void UpdateHordeUI()
    {
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            // currentHordeIndex es 0-based, así que sumamos 1 para mostrar al jugador
            levelManager.UpdateHordeCounter(currentHordeIndex + 1, hordeWaves.Length);
        }
    }
}


[System.Serializable]
public class HordeWave
{
    public int zombieCount;
    public float runSpeed;
    public float attackDamage;
}
