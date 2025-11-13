using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefabs de enemigos a spawnear (Zombie, Demon, etc.)")]
    public GameObject[] monsterPrefabs; // Array de prefabs
    
    [Tooltip("Cantidad de monstruos a generar al inicio")]
    public int initialMonsterCount = 5;
    
    [Tooltip("Radio del área de spawn")]
    public float spawnRadius = 10f;
    
    [Tooltip("Radio del área de patrulla (puede ser diferente del spawn)")]
    public float patrolRadius = 15f;
    
    [Header("Spawn Timing")]
    [Tooltip("¿Spawnear monstruos al inicio?")]
    public bool spawnOnStart = true;
    
    [Tooltip("¿Spawnear monstruos continuamente?")]
    public bool continuousSpawn = false;
    
    [Tooltip("Tiempo de espera entre spawns continuos")]
    public float spawnInterval = 10f;
    
    [Tooltip("Máximo de monstruos vivos simultáneamente")]
    public int maxActiveMonsters = 10;
    
    [Header("Monster Configuration")]
    [Tooltip("Forzar modo de comportamiento (Idle o Patrol)")]
    public bool overrideBehaviorMode = true;
    
    [Tooltip("Modo de comportamiento para zombies spawneados")]
    public ZombieBehaviorMode zombieBehaviorMode = ZombieBehaviorMode.Patrol;
    
    [Tooltip("Modo de comportamiento para demons spawneados")]
    public DemonBehaviorMode demonBehaviorMode = DemonBehaviorMode.Patrol;
    
    [Header("Advanced Settings")]
    [Tooltip("Verificar que el punto de spawn esté en NavMesh")]
    public bool validateNavMesh = true;
    
    [Tooltip("Altura máxima para buscar NavMesh")]
    public float navMeshCheckHeight = 5f;
    
    private List<GameObject> spawnedMonsters = new List<GameObject>();
    private float nextSpawnTime = 0f;

    void Start()
    {
        // Validar que el spawner esté en un NavMesh
        if (validateNavMesh)
        {
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(transform.position, out hit, navMeshCheckHeight, NavMesh.AllAreas))
            {
                enabled = false;
                return;
            }
        }
        
        // Spawn inicial
        if (spawnOnStart && monsterPrefabs != null && monsterPrefabs.Length > 0)
        {
            SpawnInitialMonsters();
        }
        
        // Configurar spawn continuo
        if (continuousSpawn)
        {
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void Update()
    {
        // Limpiar referencias de monstruos muertos
        spawnedMonsters.RemoveAll(monster => monster == null);
        
        // Spawn continuo
        if (continuousSpawn && Time.time >= nextSpawnTime && spawnedMonsters.Count < maxActiveMonsters)
        {
            SpawnMonster();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void SpawnInitialMonsters()
    {
        for (int i = 0; i < initialMonsterCount; i++)
        {
            SpawnMonster();
        }
        
    }

    public void SpawnMonster()
    {
        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
        {
            return;
        }
        
        // Seleccionar un prefab aleatorio del array
        GameObject selectedPrefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
        
        if (selectedPrefab == null)
        {
            return;
        }
        
        // Generar posición aleatoria dentro del radio de spawn
        Vector3 spawnPosition = GetRandomSpawnPosition();
        
        if (spawnPosition == Vector3.zero)
        {
            return;
        }
        
        // Instanciar monstruo
        GameObject monster = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
        string monsterType = selectedPrefab.name.Contains("Demon") ? "Demon" : "Zombie";
        monster.name = $"{monsterType}_{spawnedMonsters.Count + 1}";
        
        // Intentar configurar como Zombie
        ZombieController zombieController = monster.GetComponent<ZombieController>();
        if (zombieController != null)
        {
            // Sobrescribir el modo de comportamiento si está activado
            if (overrideBehaviorMode)
            {
                zombieController.behaviorMode = zombieBehaviorMode;
            }
            
            // Asignar el centro de patrulla (este spawner)
            zombieController.patrolCenter = transform;
            zombieController.patrolRadius = patrolRadius;
            
            // IMPORTANTE: Buscar al jugador manualmente si no está asignado
            if (zombieController.player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    zombieController.player = playerObj.transform;
                }
                else
                {
                }
            }
            
        }
        
        // Intentar configurar como Demon
        DemonController demonController = monster.GetComponent<DemonController>();
        if (demonController != null)
        {
            // Sobrescribir el modo de comportamiento si está activado
            if (overrideBehaviorMode)
            {
                demonController.behaviorMode = demonBehaviorMode;
            }
            
            // Asignar el centro de patrulla (este spawner)
            demonController.patrolCenter = transform;
            demonController.patrolRadius = patrolRadius;
            
            // IMPORTANTE: Buscar al jugador manualmente si no está asignado
            if (demonController.player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    demonController.player = playerObj.transform;
                }
                else
                {
                }
            }
            
        }
        
        // Advertir si no tiene ningún controlador
        if (zombieController == null && demonController == null)
        {
        }
        
        // Agregar a la lista de monstruos activos
        spawnedMonsters.Add(monster);
    }

    Vector3 GetRandomSpawnPosition()
    {
        // Intentar varias veces encontrar un punto válido
        int maxAttempts = 30;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            // Generar punto aleatorio en círculo
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Buscar el punto más cercano en el NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, spawnRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        
        // Si no encuentra ningún punto válido después de varios intentos
        return Vector3.zero;
    }

    // Método público para spawnear monstruos manualmente (desde otros scripts o eventos)
    public void SpawnMonsters(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (spawnedMonsters.Count >= maxActiveMonsters)
            {
                break;
            }
            
            SpawnMonster();
        }
    }

    // Método para eliminar todos los monstruos spawneados
    public void DestroyAllMonsters()
    {
        foreach (GameObject monster in spawnedMonsters)
        {
            if (monster != null)
                Destroy(monster);
        }
        
        spawnedMonsters.Clear();
    }

    // Obtener cantidad de monstruos vivos
    public int GetActiveMonsterCount()
    {
        spawnedMonsters.RemoveAll(monster => monster == null);
        return spawnedMonsters.Count;
    }

    // Visualizar áreas en el editor
    void OnDrawGizmosSelected()
    {
        // Área de spawn (amarillo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        
        // Área de patrulla (cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
        
        // Centro del spawner (rojo)
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.5f);
        
        // Texto de información
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
            $"Spawner: {gameObject.name}\n" +
            $"Spawn Radius: {spawnRadius}m\n" +
            $"Patrol Radius: {patrolRadius}m\n" +
            $"Monsters: {(Application.isPlaying ? GetActiveMonsterCount().ToString() : initialMonsterCount.ToString())}");
        #endif
    }
}
