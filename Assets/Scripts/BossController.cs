using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class BossController : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 10000f;
    public float currentHealth;

    [Header("Detection Settings")]
    public float detectionRadius = 15f;
    public float fieldOfView = 120f;
    public float attackRange = 3f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;
    public float detectionAnimationDuration = 2.0f; // Duración del scream
    public float detectionCheckInterval = 0.3f;

    [Header("Attack Settings")]
    public float attackDamage = 30f;
    public float attackAreaRadius = 4f; // Radio del área de daño
    public AudioClip swipeAttackSound;
    public AudioClip jumpAttackSound;
    [Range(0f, 1f)] public float attackSoundVolume = 0.8f;

    [Header("Movement Settings")]
    public float runSpeed = 5f;
    public float walkSpeed = 2f;
    public float rotationSpeed = 3f;
    public float stoppingDistance = 2.5f;

    [Header("Audio Settings")]
    public AudioClip bossIdleSound;
    public float idleSoundMinInterval = 5f;
    public float idleSoundMaxInterval = 10f;
    [Range(0f, 1f)] public float idleSoundVolume = 0.7f;
    public AudioClip screamSound;
    [Range(0f, 1f)] public float screamSoundVolume = 1f;
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathSoundVolume = 1f;

    [Header("Health Bar Settings")]
    public GameObject healthBarPrefab;
    public Vector3 healthBarOffset = new Vector3(0, 4f, 0);

    [Header("References")]
    public Transform player;
    public Transform eyePosition;
    public Animator bossAnimator;
    public NavMeshAgent agent;
    public BossRagdollController bossRagdoll;
    private AudioSource audioSource;

    private bool playerDetected = false;
    private bool isLowHealth = false;
    private bool isDead = false;
    private bool isPlayingDetectionAnimation = false;
    private bool hasPlayedDetectionAnimation = false;
    private float detectionAnimationTimer = 0f;
    private float detectionCheckTimer = 0f;
    private Vector3 lastKnownPlayerPosition;
    private float nextIdleSoundTime = 0f;
    private int attackCount = 0; // Contador para el patrón de ataques
    private bool isAttacking = false;
    private bool lastAttackWasSwipe = true;
    private bool deathAnimationPlayed = false;
    private GameObject healthBarInstance;

    void Start()
    {
        currentHealth = maxHealth;

        if (bossAnimator == null)
            bossAnimator = GetComponentInChildren<Animator>();
        
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (bossRagdoll == null)
            bossRagdoll = GetComponentInChildren<BossRagdollController>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 10f;
        audioSource.maxDistance = 50f;
        audioSource.volume = idleSoundVolume;
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        nextIdleSoundTime = Time.time + Random.Range(idleSoundMinInterval, idleSoundMaxInterval);

        Rigidbody mainRb = GetComponent<Rigidbody>();
        if (mainRb != null)
        {
            mainRb.isKinematic = true;
            mainRb.useGravity = false;
            mainRb.detectCollisions = false;
        }

        if (agent != null && !agent.isOnNavMesh)
        {
            agent.enabled = false;
        }
        else if (agent != null)
        {
            // Configuración mejorada para evitar traspasar paredes
            agent.speed = runSpeed;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = stoppingDistance;
            agent.autoBraking = true;
            
            // IMPORTANTE: Configuración para NO traspasar paredes
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = 40; // Mayor prioridad que zombies (menor número)
            agent.radius = 1.0f; // Aumentado de 0.8 a 1.0 para mejor detección (el boss es grande)
            agent.height = 2.5f;
            
            // Asegurar que el agente use el NavMesh correctamente
            agent.updatePosition = true; // El NavMesh controla la posición
            agent.updateRotation = true; // El NavMesh controla la rotación
            
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (eyePosition == null)
        {
            GameObject eyeObj = new GameObject("EyePosition");
            eyeObj.transform.SetParent(transform);
            eyeObj.transform.localPosition = new Vector3(0, 2.2f, 0);
            eyePosition = eyeObj.transform;
        }

        // Instanciar barra de vida
        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab);
            BossHealthBar healthBarScript = healthBarInstance.GetComponent<BossHealthBar>();
            
            if (healthBarScript != null)
            {
                healthBarScript.boss = this;
                healthBarScript.offset = healthBarOffset;
                
                Image fillImage = healthBarInstance.GetComponentInChildren<Image>();
                if (fillImage != null && fillImage.name.Contains("Fill"))
                {
                    healthBarScript.healthBarFill = fillImage;
                }
                
                healthBarScript.rectTransform = healthBarInstance.GetComponent<RectTransform>();
            }
        }
    }

    void Update()
    {
        // Si está muerto, no ejecutar lógica de juego pero permitir que el Animator siga funcionando
        if (isDead)
            return;
            
        if (player == null)
            return;

        if (Time.timeScale == 0f)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Pause();
            }
            return;
        }
        
        if (audioSource != null && Time.timeScale > 0f)
        {
            audioSource.UnPause();
        }

        PlayIdleSound();

        detectionCheckTimer += Time.deltaTime;
        if (detectionCheckTimer >= detectionCheckInterval)
        {
            DetectPlayer();
            detectionCheckTimer = 0f;
        }

        // Si está reproduciendo la animación de scream, esperar
        if (isPlayingDetectionAnimation)
        {
            detectionAnimationTimer += Time.deltaTime;
            
            // Mantener al boss quieto durante la animación
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.SetDestination(transform.position);
            
            // Rotar suavemente hacia el jugador durante el scream
            if (player != null)
            {
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                directionToPlayer.y = 0;
                if (directionToPlayer != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
            
            // Cuando termine la animación de scream, empezar a perseguir
            if (detectionAnimationTimer >= detectionAnimationDuration)
            {
                isPlayingDetectionAnimation = false;
                hasPlayedDetectionAnimation = true;
                UpdateAnimator(true, false); // Running
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
            // Quedarse quieto (Idle)
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.SetDestination(transform.position);
            
            UpdateAnimator(false, false);
            
            // Debug cada 2 segundos para no saturar la consola
            if (Time.frameCount % 120 == 0)
            {
            }
        }
    }

    void PlayIdleSound()
    {
        if (isDead || bossIdleSound == null || audioSource == null || Time.timeScale == 0f)
            return;

        if (Time.time >= nextIdleSoundTime)
        {
            audioSource.PlayOneShot(bossIdleSound, idleSoundVolume);
            nextIdleSoundTime = Time.time + Random.Range(idleSoundMinInterval, idleSoundMaxInterval);
        }
    }

    void DetectPlayer()
    {
        if (player == null || eyePosition == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Si ya detectó al jugador antes (persecución activa)
        if (hasPlayedDetectionAnimation && distanceToPlayer <= detectionRadius * 2f)
        {
            Vector3 directionToPlayer = (player.position - eyePosition.position).normalized;
            RaycastHit hit;
            
            if (Physics.Raycast(eyePosition.position, directionToPlayer, out hit, distanceToPlayer, ~(1 << LayerMask.NameToLayer("Hitbox"))))
            {
                if (hit.transform == player || hit.transform.root == player.root)
                {
                    playerDetected = true;
                    lastKnownPlayerPosition = player.position;
                    return;
                }
            }
            
            if (playerDetected)
            {
                return;
            }
        }

        // Detección INICIAL (primera vez) - requiere estar dentro del FOV
        if (distanceToPlayer <= detectionRadius)
        {
            Vector3 directionToPlayer = (player.position - eyePosition.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            if (angleToPlayer <= fieldOfView / 2f)
            {
                RaycastHit hit;
                if (Physics.Raycast(eyePosition.position, directionToPlayer, out hit, detectionRadius, ~(1 << LayerMask.NameToLayer("Hitbox"))))
                {
                    if (hit.transform == player || hit.transform.root == player.root)
                    {
                        if (!playerDetected && !hasPlayedDetectionAnimation)
                        {
                            playerDetected = true;
                            lastKnownPlayerPosition = player.position;
                            
                            // Iniciar la animación de scream
                            isPlayingDetectionAnimation = true;
                            detectionAnimationTimer = 0f;
                            
                            // Activar el layer Detect Player
                            if (bossAnimator != null)
                            {
                                bossAnimator.SetLayerWeight(2, 1f); // Detect Player layer
                                bossAnimator.SetLayerWeight(1, 0f); // Movement layer apagado
                            }
                            
                            // Reproducir sonido de scream
                            if (screamSound != null && audioSource != null)
                            {
                                audioSource.PlayOneShot(screamSound, screamSoundVolume);
                            }
                            
                        }
                        else if (!playerDetected && hasPlayedDetectionAnimation)
                        {
                            playerDetected = true;
                            lastKnownPlayerPosition = player.position;
                            isPlayingDetectionAnimation = false;
                            
                        }
                        else
                        {
                            lastKnownPlayerPosition = player.position;
                        }
                    }
                    else
                    {
                        if (!hasPlayedDetectionAnimation)
                            playerDetected = false;
                    }
                }
            }
            else
            {
                if (!hasPlayedDetectionAnimation)
                    playerDetected = false;
            }
        }
        else
        {
            if (distanceToPlayer > detectionRadius * 2f)
            {
                playerDetected = false;
                isPlayingDetectionAnimation = false;
                detectionAnimationTimer = 0f;
            }
        }
    }

    void ChasePlayer()
    {
        if (agent == null)
        {
            return;
        }
        
        if (!agent.enabled)
        {
            return;
        }
        
        if (!agent.isOnNavMesh)
        {
            return;
        }
        
        if (player == null)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            agent.isStopped = true;
            
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            AttemptAttack();
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            // Cambiar velocidad y animación según la salud
            if (isLowHealth)
            {
                agent.speed = walkSpeed;
                UpdateAnimator(false, true); // Walk lento
            }
            else
            {
                agent.speed = runSpeed;
                UpdateAnimator(true, false); // Running
            }
        }
    }

    void AttemptAttack()
    {
        if (!isAttacking)
        {
            isAttacking = true;
            
            // Patrón: swipe, swipe, jump (como zombie: attack1, attack1, attack2)
            bool useSwipe = (attackCount % 3) != 2;
            attackCount = (attackCount + 1) % 3;
            
            lastAttackWasSwipe = useSwipe;

            if (bossAnimator != null)
            {
                if (useSwipe)
                {
                    bossAnimator.SetTrigger("swipeAttack");
                }
                else
                {
                    bossAnimator.SetTrigger("jumpAttack");
                }
            }
        }
    }

    // Llamado por Animation Event en el frame del golpe
    public void OnAttackHit()
    {
        
        if (Time.timeScale == 0f)
        {
            return;
        }
        
        // Reproducir sonido
        if (audioSource != null)
        {
            AudioClip soundToPlay = lastAttackWasSwipe ? swipeAttackSound : jumpAttackSound;
            if (soundToPlay != null)
            {
                audioSource.PlayOneShot(soundToPlay, attackSoundVolume);
            }
        }
        
        if (player == null || isDead)
        {
            return;
        }

        // Área de daño (OverlapSphere en lugar de daño directo)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackAreaRadius);

        bool playerHit = false; // Para asegurar que solo golpeamos al jugador UNA VEZ

        foreach (Collider hit in hitColliders)
        {
            // Daño al jugador - SOLO UNA VEZ aunque detecte múltiples partes
            if (!playerHit)
            {
                BodyPartHitCheck playerPart = hit.GetComponent<BodyPartHitCheck>();
                if (playerPart != null)
                {
                    playerPart.TakeHit(attackDamage);
                    playerHit = true; // Marcar que ya golpeamos al jugador
                    continue; // Saltar al siguiente collider
                }
            }

            // Daño a barriles
            BarrelController barrel = hit.GetComponent<BarrelController>();
            if (barrel != null)
            {
                barrel.TakeHit();
            }
        }
    }

    // Llamado por Animation Event al final de la animación
    public void OnAttackEnd()
    {
        isAttacking = false;
    }

    void UpdateAnimator(bool running, bool walking)
    {
        if (bossAnimator == null)
            return;

        // Usar layers según el estado
        if (running)
        {
            // Activar layer Movement (Running) y desactivar Detect Player
            bossAnimator.SetLayerWeight(1, 1f); // Movement layer - corriendo
            bossAnimator.SetLayerWeight(2, 0f); // Detect Player layer - apagar
            
            // Desactivar walking (para que use Run en el Blend Tree)
            bossAnimator.SetBool("isWalking", false);
        }
        else if (walking)
        {
            // Activar layer Movement (Walking lento por baja vida)
            bossAnimator.SetLayerWeight(1, 1f); // Movement layer - caminando
            bossAnimator.SetLayerWeight(2, 0f); // Detect Player layer - apagar
            
            // Activar walking
            bossAnimator.SetBool("isWalking", true);
        }
        else
        {
            // Idle - desactivar todos los layers excepto Base
            bossAnimator.SetLayerWeight(1, 0f); // Movement layer
            bossAnimator.SetLayerWeight(2, 0f); // Detect Player layer
            bossAnimator.SetBool("isWalking", false);
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
                if (bossAnimator != null)
                {
                    bossAnimator.SetLayerWeight(2, 1f); // detectPlayer layer
                    bossAnimator.SetLayerWeight(1, 0f); // Walk layer apagado
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
                if (bossAnimator != null)
                {
                    bossAnimator.SetBool("isWalking", false);
                    bossAnimator.SetLayerWeight(1, 1f);
                    bossAnimator.SetLayerWeight(2, 0f);
                }
            }
        }

        // Si la vida cae por debajo del 50%, cambiar a walk
        if (currentHealth < maxHealth * 0.5f && !isLowHealth)
        {
            isLowHealth = true;
        }

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

        if (bossAnimator != null && !deathAnimationPlayed)
        {
            deathAnimationPlayed = true;
            
            // Desactivar TODOS los layers excepto el Base Layer para que la transición a Dead funcione
            for (int i = 1; i < bossAnimator.layerCount; i++)
            {
                bossAnimator.SetLayerWeight(i, 0f);
            }
            
            // Activar animación de muerte
            bossAnimator.SetBool("isDead", true);
            
            // Forzar reproducción del estado Dead directamente (fix para cuando está en Idle)
            bossAnimator.Play("Dead", 0, 0f);
            
        }

        if (audioSource != null && deathSound != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(deathSound, deathSoundVolume);
        }
        else if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        StartCoroutine(ActivateRagdollAfterDeathAnimation());
        
        // Notificar victoria al LevelManager
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.OnBossDefeated();
        }
    }

    System.Collections.IEnumerator ActivateRagdollAfterDeathAnimation()
    {
        // Esperar 2 segundos para que la animación de muerte se reproduzca una vez
        yield return new WaitForSeconds(2.0f);

        if (bossRagdoll != null)
        {
            bossRagdoll.Active(true);
        }

        if (audioSource != null)
        {
            audioSource.enabled = false;
        }

        Destroy(gameObject, 15f);
    }

    void OnDestroy()
    {
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Radio de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Rango de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Área de daño del ataque
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackAreaRadius);

        // Campo de visión (FOV)
        if (eyePosition != null)
        {
            Gizmos.color = Color.blue;
            Vector3 forward = transform.forward * detectionRadius;
            Vector3 rightBoundary = Quaternion.Euler(0, fieldOfView / 2f, 0) * forward;
            Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfView / 2f, 0) * forward;
            
            Gizmos.DrawLine(eyePosition.position, eyePosition.position + rightBoundary);
            Gizmos.DrawLine(eyePosition.position, eyePosition.position + leftBoundary);
        }

        // Raycast hacia el jugador
        if (player != null && eyePosition != null)
        {
            Gizmos.color = playerDetected ? Color.green : Color.gray;
            Gizmos.DrawLine(eyePosition.position, player.position);
        }
    }
}
