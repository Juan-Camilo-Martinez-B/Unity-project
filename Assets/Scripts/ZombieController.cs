using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum ZombieBehaviorMode
{
    Idle,    // Quieto hasta detectar jugador
    Patrol,  // Patrullando por área hasta detectar jugador
    Horde    // Siempre persigue al jugador (para modo hordas)
}

public class ZombieController : MonoBehaviour
{
    [Header("Behavior Mode")]
    [Tooltip("Idle: Quieto hasta detectar jugador | Patrol: Vagando por área | Horde: Siempre persigue al jugador")]
    public ZombieBehaviorMode behaviorMode = ZombieBehaviorMode.Idle;
    
    [Header("Patrol Settings - Solo para Patrol Mode")]
    [Tooltip("Centro del área de patrulla (usualmente el ZombieSpawner)")]
    public Transform patrolCenter;
    [Tooltip("Radio del área donde patrullará el zombie")]
    public float patrolRadius = 10f;
    [Tooltip("Velocidad al patrullar (más lenta que al perseguir)")]
    public float patrolSpeed = 1.5f;
    [Tooltip("Tiempo de espera al llegar a un punto de patrulla")]
    public float patrolWaitTime = 2f;
    
    private Vector3 currentPatrolTarget;
    private bool isWaitingAtPatrolPoint = false;
    private float patrolWaitTimer = 0f;
    
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Detection Settings")]
    public float detectionRadius = 5f; // Reducido para detección más cercana
    public float fieldOfView = 110f; // Campo de visión en grados
    public float attackRange = 1.5f; // Rango más cercano para tocar al jugador
    public LayerMask playerLayer;
    public LayerMask obstacleLayer; // Para detectar obstáculos
    public float detectionAnimationDuration = 1.5f;
    public float detectionCheckInterval = 0.2f; // Optimización: no chequear cada frame

    [Header("Attack Settings")]
    public float attackDamage = 10f; // Daño que hace al jugador
    public AudioClip attackSound1; // Sonido de ataque 1
    public AudioClip attackSound2; // Sonido de ataque 2
    [Range(0f, 1f)] public float attackSoundVolume = 0.8f;

    [Header("Movement Settings")]
    public float runSpeed = 3.5f;
    public float walkSpeed = 1.5f;
    public float rotationSpeed = 5f; // Velocidad de rotación suave
    public float stoppingDistance = 1.2f; // Distancia para detenerse cerca del jugador

    [Header("Audio Settings")]
    public AudioClip zombieIdleSound; // Sonido que se repite constantemente
    public float idleSoundMinInterval = 3f; // Intervalo mínimo entre sonidos
    public float idleSoundMaxInterval = 7f; // Intervalo máximo entre sonidos
    [Range(0f, 1f)] public float idleSoundVolume = 0.7f;
    public AudioClip zombieDeathSound; // Sonido de muerte
    [Range(0f, 1f)] public float deathSoundVolume = 1f;

    [Header("Health Bar Settings")]
    public GameObject healthBarPrefab; // Prefab de la barra de vida
    public Vector3 healthBarOffset = new Vector3(0, 2.5f, 0); // Altura sobre el zombie

    [Header("References")]
    public Transform player;
    public Transform eyePosition; // Posición desde donde "ve" el zombie (cabeza)
    public Animator zombieAnimator;
    public NavMeshAgent agent;
    public ZombieRagdollController zombieRagdoll;
    private AudioSource audioSource;

    private bool playerDetected = false;
    private bool isLowHealth = false;
    private bool isDead = false;
    private bool isPlayingDetectionAnimation = false;
    private bool hasPlayedDetectionAnimation = false; // Nueva bandera para saber si ya se reprodujo
    private float detectionAnimationTimer = 0f;
    private float detectionCheckTimer = 0f;
    private Vector3 lastKnownPlayerPosition;
    private float nextIdleSoundTime = 0f;
    private int attackCount = 0; // Contador para el patrón de ataques (0, 1, 2)
    private bool isAttacking = false; // Si está ejecutando una animación de ataque
    private bool lastAttackWasType1 = true; // Para saber qué sonido reproducir en OnAttackHit
    private bool deathAnimationPlayed = false; // Para asegurar que solo se reproduce una vez
    private GameObject healthBarInstance; // Instancia de la barra de vida

    void Start()
    {
        // Inicializar salud
        currentHealth = maxHealth;

        // Obtener componentes
        if (zombieAnimator == null)
            zombieAnimator = GetComponent<Animator>();

        
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (zombieRagdoll == null)
            zombieRagdoll = GetComponentInChildren<ZombieRagdollController>();

        // Configurar AudioSource para los sonidos del zombie
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // Audio 3D
        audioSource.minDistance = 5f;
        audioSource.maxDistance = 20f;
        audioSource.volume = idleSoundVolume;
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // Programar el primer sonido
        nextIdleSoundTime = Time.time + Random.Range(idleSoundMinInterval, idleSoundMaxInterval);

        // Configurar el Rigidbody principal (si existe) para que no interfiera
        Rigidbody mainRb = GetComponent<Rigidbody>();
        if (mainRb != null)
        {
            mainRb.isKinematic = true;
            mainRb.useGravity = false;
            mainRb.detectCollisions = false;
        }

        // Verificar que el NavMeshAgent esté en un NavMesh válido
        if (agent != null && !agent.isOnNavMesh)
        {
            agent.enabled = false;
        }
        else if (agent != null)
        {
            // Configurar NavMeshAgent para mejor navegación Y evitar traspasar paredes
            agent.speed = (behaviorMode == ZombieBehaviorMode.Patrol) ? patrolSpeed : runSpeed;
            agent.angularSpeed = 120f; // Velocidad de giro
            agent.acceleration = 8f; // Aceleración
            // En modo patrol, usar stopping distance más pequeña para que llegue a los puntos
            agent.stoppingDistance = (behaviorMode == ZombieBehaviorMode.Patrol) ? 0.5f : stoppingDistance;
            agent.autoBraking = true;
            
            // IMPORTANTE: Configuración para NO traspasar paredes
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = 50; // 0-99, menor = mayor prioridad para evitar
            agent.radius = 0.5f; // Aumentado de 0.3 a 0.5 para mejor detección de colisiones
            agent.height = 1.8f;
            
            // Asegurar que el agente use el NavMesh correctamente
            agent.updatePosition = true; // El NavMesh controla la posición
            agent.updateRotation = true; // El NavMesh controla la rotación
            
        }

        // Buscar al jugador si no está asignado
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
            }
        }

        // Si no hay eyePosition asignado, usar la posición del zombie + offset
        if (eyePosition == null)
        {
            GameObject eyeObj = new GameObject("EyePosition");
            eyeObj.transform.SetParent(transform);
            eyeObj.transform.localPosition = new Vector3(0, 1.6f, 0); // Altura de los ojos
            eyePosition = eyeObj.transform;
        }

        // Instanciar la barra de vida
        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab);
            ZombieHealthBar healthBarScript = healthBarInstance.GetComponent<ZombieHealthBar>();
            
            if (healthBarScript != null)
            {
                healthBarScript.zombie = this;
                healthBarScript.offset = healthBarOffset;
                
                // Buscar el componente Image del Fill
                Image fillImage = healthBarInstance.GetComponentInChildren<Image>();
                if (fillImage != null && fillImage.name.Contains("Fill"))
                {
                    healthBarScript.healthBarFill = fillImage;
                }
                
                // Obtener el RectTransform
                healthBarScript.rectTransform = healthBarInstance.GetComponent<RectTransform>();
            }
        }
        
        // Inicializar patrulla si está en modo Patrol
        if (behaviorMode == ZombieBehaviorMode.Patrol)
        {
            // Si no hay patrol center asignado, usar la posición inicial del zombie
            if (patrolCenter == null)
            {
                GameObject patrolCenterObj = new GameObject($"{gameObject.name}_PatrolCenter");
                patrolCenterObj.transform.position = transform.position;
                patrolCenter = patrolCenterObj.transform;
            }
            
            // Generar primer punto de patrulla INMEDIATAMENTE
            GenerateNewPatrolPoint();
            
            // Asegurarse de que NO esté esperando al inicio
            isWaitingAtPatrolPoint = false;
            patrolWaitTimer = 0f;
        }
    }

    void Update()
    {
        if (isDead || player == null)
            return;

        // Si el juego está pausado, detener todos los sonidos y no procesar nada
        if (Time.timeScale == 0f)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Pause();
            }
            return;
        }
        
        // Si el juego se reanuda, reanudar el audio si estaba pausado
        if (audioSource != null && Time.timeScale > 0f)
        {
            audioSource.UnPause();
        }

        // Reproducir sonidos idle del zombie
        PlayIdleSound();

        // Optimización: Detectar al jugador cada cierto intervalo, no cada frame
        detectionCheckTimer += Time.deltaTime;
        if (detectionCheckTimer >= detectionCheckInterval)
        {
            DetectPlayer();
            detectionCheckTimer = 0f;
        }

        // Si está reproduciendo la animación de detección, esperar
        if (isPlayingDetectionAnimation)
        {
            detectionAnimationTimer += Time.deltaTime;
            
            // Mantener al zombie quieto durante la animación
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.SetDestination(transform.position);
            
            // Rotar suavemente hacia el jugador durante la detección
            if (player != null)
            {
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                directionToPlayer.y = 0; // Solo rotar en Y
                if (directionToPlayer != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
            
            // Cuando termine la animación de detección, empezar a perseguir
            if (detectionAnimationTimer >= detectionAnimationDuration)
            {
                isPlayingDetectionAnimation = false;
                hasPlayedDetectionAnimation = true; // Marcar que ya se reprodujo
                
                // IMPORTANTE: Cambiar velocidad a runSpeed INMEDIATAMENTE
                if (agent != null && agent.enabled)
                {
                    agent.speed = runSpeed;
                    agent.stoppingDistance = stoppingDistance;
                }
                
                // FORZAR cambio de animación INMEDIATAMENTE
                if (zombieAnimator != null)
                {
                    zombieAnimator.SetBool("isWalking", false); // FORZAR a false para Run
                    zombieAnimator.SetLayerWeight(1, 1f); // Asegurar que Walk layer esté activo
                    zombieAnimator.SetLayerWeight(2, 0f); // Desactivar detectPlayer layer
                }
                
                UpdateAnimator(true, false); // Activar animación de Run
                
            }
            
            return;
        }

        // Si el jugador está detectado, perseguirlo
        if (playerDetected)
        {
            ChasePlayer();
        }
        else
        {
            // Comportamiento según el modo cuando NO hay jugador detectado
            switch (behaviorMode)
            {
                case ZombieBehaviorMode.Idle:
                    // Modo Idle: Quedarse quieto
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                        agent.SetDestination(transform.position);
                    
                    UpdateAnimator(false, false);
                    break;
                    
                case ZombieBehaviorMode.Patrol:
                    // Modo Patrol: Caminar por el área
                    PatrolArea();
                    break;
                    
                case ZombieBehaviorMode.Horde:
                    // Modo Horde: SIEMPRE perseguir al jugador (forzar detección)
                    if (player != null)
                    {
                        playerDetected = true;
                        ChasePlayer();
                    }
                    break;
            }
        }
    }

    void PlayIdleSound()
    {
        // Solo reproducir si no está muerto, hay un sonido asignado Y el juego no está pausado
        if (isDead || zombieIdleSound == null || audioSource == null || Time.timeScale == 0f)
            return;

        // Verificar si es tiempo de reproducir el siguiente sonido
        if (Time.time >= nextIdleSoundTime)
        {
            audioSource.PlayOneShot(zombieIdleSound, idleSoundVolume);
            
            // Programar el siguiente sonido en un intervalo aleatorio
            nextIdleSoundTime = Time.time + Random.Range(idleSoundMinInterval, idleSoundMaxInterval);
        }
    }

    void DetectPlayer()
    {
        if (player == null || eyePosition == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Si ya detectó al jugador antes (persecución activa)
        if (hasPlayedDetectionAnimation)
        {
            // Una vez detectado, perseguir sin importar distancia o ángulo
            // Solo verificar límite de área de patrulla en modo Patrol
            if (behaviorMode == ZombieBehaviorMode.Patrol && patrolCenter != null)
            {
                float distanceFromPatrolCenter = Vector3.Distance(transform.position, patrolCenter.position);
                
                // Si está muy lejos del centro de patrulla, volver
                if (distanceFromPatrolCenter > patrolRadius * 2f)
                {
                    playerDetected = false;
                    isPlayingDetectionAnimation = false;
                    detectionAnimationTimer = 0f;
                    return;
                }
            }
            
            // Mantener persecución constante
            playerDetected = true;
            lastKnownPlayerPosition = player.position;
            return; // Continuar persecución
        }

        // Detección INICIAL (primera vez) - requiere estar dentro del FOV
        if (distanceToPlayer <= detectionRadius)
        {
            // Verificar campo de visión (FOV)
            Vector3 directionToPlayer = (player.position - eyePosition.position).normalized;
            float angleToPlayer = Vector3.Angle(eyePosition.forward, directionToPlayer);

            if (angleToPlayer <= fieldOfView / 2f)
            {
                // Verificar que no haya obstáculos entre el zombie y el jugador (Raycast)
                RaycastHit hit;
                if (Physics.Raycast(eyePosition.position, directionToPlayer, out hit, detectionRadius, ~(1 << LayerMask.NameToLayer("Hitbox"))))
                {
                    // Si el raycast golpea al jugador (no hay obstáculos)
                    if (hit.transform == player || hit.transform.root == player.root)
                    {
                        // Si acaba de detectar al jugador (primera vez) Y no ha reproducido la animación antes
                        if (!playerDetected && !hasPlayedDetectionAnimation)
                        {
                            playerDetected = true;
                            lastKnownPlayerPosition = player.position;
                            
                            // Iniciar la animación de detección
                            isPlayingDetectionAnimation = true;
                            detectionAnimationTimer = 0f;
                            
                            // Activar el layer detectPlayer
                            if (zombieAnimator != null)
                            {
                                zombieAnimator.SetLayerWeight(2, 1f); // detectPlayer layer
                                zombieAnimator.SetLayerWeight(1, 0f); // Walk layer apagado
                            }
                            
                        }
                        else if (!playerDetected && hasPlayedDetectionAnimation)
                        {
                            // Si ya reprodujo la animación antes, detectar directamente sin animación
                            playerDetected = true;
                            lastKnownPlayerPosition = player.position;
                            isPlayingDetectionAnimation = false;
                            
                            // IMPORTANTE: Cambiar velocidad a runSpeed inmediatamente
                            if (agent != null && agent.enabled)
                            {
                                agent.speed = runSpeed;
                                agent.stoppingDistance = stoppingDistance;
                            }
                            
                            // FORZAR cambio de animación INMEDIATAMENTE
                            if (zombieAnimator != null)
                            {
                                zombieAnimator.SetBool("isWalking", false); // FORZAR a false para Run
                                zombieAnimator.SetLayerWeight(1, 1f);
                                zombieAnimator.SetLayerWeight(2, 0f);
                            }
                            
                        }
                        else
                        {
                            // Actualizar última posición conocida
                            lastKnownPlayerPosition = player.position;
                        }
                    }
                    else
                    {
                        // Hay un obstáculo entre el zombie y el jugador (solo perder detección si no había detectado antes)
                        if (!hasPlayedDetectionAnimation)
                            playerDetected = false;
                    }
                }
            }
            else
            {
                // Fuera del campo de visión (solo perder detección si no había detectado antes)
                if (!hasPlayedDetectionAnimation)
                    playerDetected = false;
            }
        }
        // Ya no se pierde detección por distancia, solo por límite de patrulla (verificado arriba)
    }

    void ChasePlayer()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || player == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Si está muy cerca del jugador (en rango de ataque)
        if (distanceToPlayer <= attackRange)
        {
            // Detener el movimiento
            agent.isStopped = true;
            
            // Rotar suavemente hacia el jugador
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            // Intentar atacar SOLO UNA VEZ (cuando no está atacando)
            AttemptAttack();
            
            // Mantener idle visual mientras ataca (la animación de ataque se reproduce automáticamente)
            // No llamar UpdateAnimator aquí para no interferir con las animaciones de ataque
        }
        else
        {
            // Perseguir al jugador
            agent.isStopped = false;
            
            // IMPORTANTE: Cambiar stopping distance a la de ataque cuando persigue
            if (agent.stoppingDistance != stoppingDistance)
            {
                agent.stoppingDistance = stoppingDistance;
            }
            
            agent.SetDestination(player.position);

            // Cambiar velocidad y animación según la salud
            if (isLowHealth)
            {
                agent.speed = walkSpeed;
                UpdateAnimator(false, true); // Walk lento
            }
            else
            {
                // Al perseguir, siempre usar velocidad de run (independiente del modo de patrulla)
                agent.speed = runSpeed;
                UpdateAnimator(true, false); // Run
                
                // Debug para verificar TODO
                if (Time.frameCount % 60 == 0) // Cada segundo aprox
                {
                }
            }
        }
    }

    void AttemptAttack()
    {
        // Solo iniciar ataque si NO está atacando actualmente
        if (!isAttacking)
        {
            isAttacking = true;
            
            // Determinar qué ataque usar según el patrón: Attack1, Attack1, Attack2
            bool useAttack1 = (attackCount % 3) != 2;
            attackCount = (attackCount + 1) % 3;
            
            // Guardar qué tipo de ataque es para reproducir el sonido correcto en OnAttackHit
            lastAttackWasType1 = useAttack1;

            // Activar el trigger de animación
            if (zombieAnimator != null)
            {
                if (useAttack1)
                {
                    zombieAnimator.SetTrigger("Attack1");
                }
                else
                {
                    zombieAnimator.SetTrigger("Attack2");
                }
            }
        }
    }

    // Este método se llama desde un Animation Event en el frame del golpe
    public void OnAttackHit()
    {
        
        // No reproducir sonido ni hacer daño si el juego está pausado
        if (Time.timeScale == 0f)
        {
            return;
        }
        
        // Reproducir sonido de golpe sincronizado con la animación
        if (audioSource != null)
        {
            AudioClip soundToPlay = lastAttackWasType1 ? attackSound1 : attackSound2;
            if (soundToPlay != null)
            {
                audioSource.PlayOneShot(soundToPlay, attackSoundVolume);
            }
        }
        
        if (player == null || isDead)
        {
            return;
        }

        // Verificar que el jugador sigue en rango
        float currentDistance = Vector3.Distance(transform.position, player.position);
        
        if (currentDistance <= attackRange * 1.5f)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = player.GetComponentInParent<PlayerController>();
            }

            if (playerController != null)
            {
                playerController.TakeDamage(attackDamage);
            }
            else
            {
            }
        }
        else
        {
        }
    }

    // Este método se llama desde un Animation Event al final de la animación
    public void OnAttackEnd()
    {
        isAttacking = false;
    }

    void UpdateAnimator(bool running, bool walking)
    {
        if (zombieAnimator == null)
        {
            return;
        }

        // Debug para patrulla
        if (behaviorMode == ZombieBehaviorMode.Patrol && Time.frameCount % 120 == 0)
        {
        }

        // Usar layers según el estado
        if (running)
        {
            // Activar layer Walk (Running) y desactivar detectPlayer
            zombieAnimator.SetLayerWeight(1, 1f); // Walk layer - animación de correr
            zombieAnimator.SetLayerWeight(2, 0f); // detectPlayer layer - apagar detección
            
            // FORZAR animación de correr (desactivar walking)
            // Importante: Debe ser false para que el Blend Tree use Run en lugar de Walk
            zombieAnimator.SetBool("isWalking", false);
            
            // Debug cada vez que se establece Run
            if (behaviorMode == ZombieBehaviorMode.Patrol && Time.frameCount % 60 == 0)
            {
            }
        }
        else if (walking)
        {
            // Activar layer Walk (Walking lento por baja vida o PATRULLA)
            zombieAnimator.SetLayerWeight(1, 1f); // Walk layer - animación de caminar
            zombieAnimator.SetLayerWeight(2, 0f); // detectPlayer layer - apagar detección
            
            // Activar animación de caminar
            zombieAnimator.SetBool("isWalking", true);
        }
        else
        {
            // Idle - desactivar todos los layers
            zombieAnimator.SetLayerWeight(1, 0f); // Walk layer
            zombieAnimator.SetLayerWeight(2, 0f); // detectPlayer layer
            zombieAnimator.SetBool("isWalking", false);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        
        // Si recibe daño y aún no ha detectado al jugador, detectarlo automáticamente
        if (!playerDetected && player != null)
        {
            lastKnownPlayerPosition = player.position;
            playerDetected = true;
            if (!hasPlayedDetectionAnimation)
            {
                // Iniciar la animación de detección igual que en DetectPlayer()
                isPlayingDetectionAnimation = true;
                detectionAnimationTimer = 0f;
                hasPlayedDetectionAnimation = true;
                if (zombieAnimator != null)
                {
                    zombieAnimator.SetLayerWeight(2, 1f); // detectPlayer layer
                    zombieAnimator.SetLayerWeight(1, 0f); // Walk layer apagado
                }
            }
            else
            {
                // Si ya hizo la animación, saltar directo a correr
                if (agent != null && agent.enabled)
                {
                    agent.speed = runSpeed;
                    agent.stoppingDistance = stoppingDistance;
                }
                if (zombieAnimator != null)
                {
                    zombieAnimator.SetBool("isWalking", false);
                    zombieAnimator.SetLayerWeight(1, 1f);
                    zombieAnimator.SetLayerWeight(2, 0f);
                }
            }
        }

        // Si la vida cae por debajo de 30, cambiar a walk
        if (currentHealth < 30f && !isLowHealth)
        {
            isLowHealth = true;
        }

        // Si la vida llega a 0, morir
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        
        // Notificar al ZombieKillTracker (si existe - para nivel Boss)
        if (ZombieKillTracker.Instance != null)
        {
            ZombieKillTracker.Instance.OnZombieKilled();
        }
        
        // Notificar al KillStreakBonus (si existe - para nivel Industry)
        if (KillStreakBonus.Instance != null)
        {
            KillStreakBonus.Instance.OnZombieKilled();
        }

        // Reproducir animación de muerte
        if (zombieAnimator != null && !deathAnimationPlayed)
        {
            deathAnimationPlayed = true;
            zombieAnimator.SetBool("isDead", true);
        }

        // Reproducir sonido de muerte
        if (audioSource != null && zombieDeathSound != null)
        {
            audioSource.Stop(); // Detener sonidos actuales
            audioSource.PlayOneShot(zombieDeathSound, deathSoundVolume);
        }
        else if (audioSource != null)
        {
            audioSource.Stop();
        }

        // Detener el NavMeshAgent
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Esperar un tiempo antes de activar el ragdoll (dar tiempo a la animación)
        StartCoroutine(ActivateRagdollAfterDeathAnimation());
    }

    System.Collections.IEnumerator ActivateRagdollAfterDeathAnimation()
    {
        // Esperar a que termine la animación de muerte (ajusta este tiempo según tu animación)
        yield return new WaitForSeconds(2.0f);

        // Activar ragdoll
        if (zombieRagdoll != null)
        {
            zombieRagdoll.Active(true);
        }

        // Desactivar completamente el AudioSource
        if (audioSource != null)
        {
            audioSource.enabled = false;
        }

        // DESACTIVAR inmediatamente para que HordeManager detecte que murió
        // Esto permite que el timer se detenga correctamente en Industry
        gameObject.SetActive(false);
        
        // Destruir después de que termine la animación (el objeto ya está inactivo)
        Destroy(gameObject, 10f);
    }

    void OnDestroy()
    {
        // Destruir la barra de vida cuando el zombie se destruya
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
        }
    }

    // Visualizar el radio de detección y FOV en el editor
    void OnDrawGizmosSelected()
    {
        // Radio de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Rango de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Campo de visión (FOV)
        if (eyePosition != null)
        {
            Gizmos.color = Color.blue;
            Vector3 forward = eyePosition.forward * detectionRadius;
            Vector3 rightBoundary = Quaternion.Euler(0, fieldOfView / 2f, 0) * forward;
            Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfView / 2f, 0) * forward;
            
            Gizmos.DrawLine(eyePosition.position, eyePosition.position + rightBoundary);
            Gizmos.DrawLine(eyePosition.position, eyePosition.position + leftBoundary);
        }

        // Raycast hacia el jugador (si está asignado)
        if (player != null && eyePosition != null)
        {
            Gizmos.color = playerDetected ? Color.green : Color.gray;
            Gizmos.DrawLine(eyePosition.position, player.position);
        }
        
        // Área de patrulla (solo en modo Patrol)
        if (behaviorMode == ZombieBehaviorMode.Patrol && patrolCenter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(patrolCenter.position, patrolRadius);
            
            // Línea hacia el punto de patrulla actual
            if (currentPatrolTarget != Vector3.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, currentPatrolTarget);
                Gizmos.DrawWireSphere(currentPatrolTarget, 0.5f);
            }
        }
    }

    // ============================================
    // MÉTODOS DE PATRULLA
    // ============================================
    
    void PatrolArea()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || patrolCenter == null)
        {
            return;
        }
        
        // IMPORTANTE: Solo patrullar si NO está persiguiendo al jugador
        if (playerDetected)
        {
            // Si detectó al jugador, no hacer nada aquí (ChasePlayer() se encarga)
            return;
        }
        
        // Si está esperando en un punto de patrulla
        if (isWaitingAtPatrolPoint)
        {
            patrolWaitTimer += Time.deltaTime;
            
            // Quedarse quieto mientras espera
            agent.SetDestination(transform.position);
            UpdateAnimator(false, false); // Idle
            
            // Cuando termine la espera, generar nuevo punto
            if (patrolWaitTimer >= patrolWaitTime)
            {
                isWaitingAtPatrolPoint = false;
                patrolWaitTimer = 0f;
                GenerateNewPatrolPoint();
            }
            
            return;
        }
        
        // Verificar si el target es válido
        if (currentPatrolTarget == Vector3.zero)
        {
            GenerateNewPatrolPoint();
            return;
        }
        
        // Verificar si llegó al punto de patrulla
        if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
        {
            // Llegó al punto, empezar a esperar
            isWaitingAtPatrolPoint = true;
            patrolWaitTimer = 0f;
            return;
        }
        
        // Está caminando hacia el punto de patrulla
        // IMPORTANTE: Solo cambiar velocidad si NO está persiguiendo
        agent.speed = patrolSpeed;
        agent.isStopped = false;
        
        // Asegurarse de establecer el destino
        if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            agent.SetDestination(currentPatrolTarget);
        }
        
        // Actualizar animación de caminar
        UpdateAnimator(false, true); // Walking (isRunning = false, isWalking = true)
        
        // Debug visual cada 2 segundos
        if (Time.frameCount % 120 == 0)
        {
        }
    }
    
    void GenerateNewPatrolPoint()
    {
        if (patrolCenter == null)
        {
            return;
        }
        
        // Generar punto aleatorio dentro del radio de patrulla
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        Vector3 randomPoint = patrolCenter.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // Encontrar el punto más cercano en el NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
        {
            currentPatrolTarget = hit.position;
        }
        else
        {
            // Si no encuentra un punto válido, intentar desde la posición actual del zombie
            if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
            {
                currentPatrolTarget = hit.position;
            }
            else
            {
                currentPatrolTarget = transform.position;
            }
        }
    }
}
