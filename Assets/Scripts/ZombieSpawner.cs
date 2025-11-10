using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefab del zombie a spawnear (debe tener ZombieController)")]
    public GameObject zombiePrefab;
    
    [Tooltip("Cantidad de zombies a generar al inicio")]
    public int initialZombieCount = 5;
    
    [Tooltip("Radio del área de spawn")]
    public float spawnRadius = 10f;
    
    [Tooltip("Radio del área de patrulla (puede ser diferente del spawn)")]
    public float patrolRadius = 15f;
    
    [Header("Spawn Timing")]
    [Tooltip("¿Spawnear zombies al inicio?")]
    public bool spawnOnStart = true;
    
    [Tooltip("¿Spawnear zombies continuamente?")]
    public bool continuousSpawn = false;
    
    [Tooltip("Tiempo de espera entre spawns continuos")]
    public float spawnInterval = 10f;
    
    [Tooltip("Máximo de zombies vivos simultáneamente")]
    public int maxActiveZombies = 10;
    
    [Header("Zombie Configuration")]
    [Tooltip("Forzar modo de comportamiento (Idle o Patrol)")]
    public bool overrideBehaviorMode = true;
    
    [Tooltip("Modo de comportamiento para zombies spawneados")]
    public ZombieBehaviorMode zombieBehaviorMode = ZombieBehaviorMode.Patrol;
    
    [Header("Advanced Settings")]
    [Tooltip("Verificar que el punto de spawn esté en NavMesh")]
    public bool validateNavMesh = true;
    
    [Tooltip("Altura máxima para buscar NavMesh")]
    public float navMeshCheckHeight = 5f;
    
    private List<GameObject> spawnedZombies = new List<GameObject>();
    private float nextSpawnTime = 0f;

    void Start()
    {
        // Validar que el spawner esté en un NavMesh
        if (validateNavMesh)
        {
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(transform.position, out hit, navMeshCheckHeight, NavMesh.AllAreas))
            {
                Debug.LogError($"ZombieSpawner '{gameObject.name}' no está cerca de un NavMesh válido! Muévelo o aumenta 'Nav Mesh Check Height'");
                enabled = false;
                return;
            }
        }
        
        // Spawn inicial
        if (spawnOnStart && zombiePrefab != null)
        {
            SpawnInitialZombies();
        }
        
        // Configurar spawn continuo
        if (continuousSpawn)
        {
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void Update()
    {
        // Limpiar referencias de zombies muertos
        spawnedZombies.RemoveAll(zombie => zombie == null);
        
        // Spawn continuo
        if (continuousSpawn && Time.time >= nextSpawnTime && spawnedZombies.Count < maxActiveZombies)
        {
            SpawnZombie();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void SpawnInitialZombies()
    {
        for (int i = 0; i < initialZombieCount; i++)
        {
            SpawnZombie();
        }
        
        Debug.Log($"ZombieSpawner '{gameObject.name}' generó {initialZombieCount} zombies iniciales");
    }

    public void SpawnZombie()
    {
        if (zombiePrefab == null)
        {
            Debug.LogError($"ZombieSpawner '{gameObject.name}' no tiene Zombie Prefab asignado!");
            return;
        }
        
        // Generar posición aleatoria dentro del radio de spawn
        Vector3 spawnPosition = GetRandomSpawnPosition();
        
        if (spawnPosition == Vector3.zero)
        {
            Debug.LogWarning($"ZombieSpawner '{gameObject.name}' no pudo encontrar posición válida en NavMesh");
            return;
        }
        
        // Instanciar zombie
        GameObject zombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
        zombie.name = $"{zombiePrefab.name}_{spawnedZombies.Count + 1}";
        
        // Configurar el zombie
        ZombieController controller = zombie.GetComponent<ZombieController>();
        if (controller != null)
        {
            // Sobrescribir el modo de comportamiento si está activado
            if (overrideBehaviorMode)
            {
                controller.behaviorMode = zombieBehaviorMode;
            }
            
            // Asignar el centro de patrulla (este spawner)
            controller.patrolCenter = transform;
            controller.patrolRadius = patrolRadius;
            
            // IMPORTANTE: Buscar al jugador manualmente si no está asignado
            if (controller.player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    controller.player = playerObj.transform;
                    Debug.Log($"✅ Zombie '{zombie.name}': Player asignado desde Spawner");
                }
                else
                {
                    Debug.LogError($"❌ Zombie '{zombie.name}': No se encontró Player con tag 'Player'!");
                }
            }
            
            Debug.Log($"🧟 Zombie '{zombie.name}' spawneado en modo {controller.behaviorMode} en {spawnPosition}");
            Debug.Log($"   - Patrol Center: {controller.patrolCenter?.name}");
            Debug.Log($"   - Patrol Radius: {controller.patrolRadius}");
            Debug.Log($"   - Player: {controller.player?.name ?? "NULL"}");
        }
        else
        {
            Debug.LogWarning($"Zombie spawneado '{zombie.name}' no tiene ZombieController!");
        }
        
        // Agregar a la lista de zombies activos
        spawnedZombies.Add(zombie);
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
        Debug.LogWarning($"ZombieSpawner '{gameObject.name}' no pudo encontrar punto válido en NavMesh después de {maxAttempts} intentos");
        return Vector3.zero;
    }

    // Método público para spawnear zombies manualmente (desde otros scripts o eventos)
    public void SpawnZombies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (spawnedZombies.Count >= maxActiveZombies)
            {
                Debug.Log($"ZombieSpawner '{gameObject.name}' alcanzó el límite de zombies activos ({maxActiveZombies})");
                break;
            }
            
            SpawnZombie();
        }
    }

    // Método para eliminar todos los zombies spawneados
    public void DestroyAllZombies()
    {
        foreach (GameObject zombie in spawnedZombies)
        {
            if (zombie != null)
                Destroy(zombie);
        }
        
        spawnedZombies.Clear();
        Debug.Log($"ZombieSpawner '{gameObject.name}' eliminó todos los zombies");
    }

    // Obtener cantidad de zombies vivos
    public int GetActiveZombieCount()
    {
        spawnedZombies.RemoveAll(zombie => zombie == null);
        return spawnedZombies.Count;
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
            $"Zombies: {(Application.isPlaying ? GetActiveZombieCount().ToString() : initialZombieCount.ToString())}");
        #endif
    }
}
