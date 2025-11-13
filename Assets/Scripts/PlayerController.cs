using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
public class PlayerController : MonoBehaviour
{


    public bool Player = true;
    public bool Active = true;
    //Personaje
    Transform playerTr;
    Rigidbody playerRb;
    internal Animator playerAnim;
    RagdollController playerRagdoll;

    public float maxHealth = 100f;
    public float currentHealth;
    public float playerSpeed = 0f;
    
    [Header("Sprint")]
    public float sprintMultiplier = 2f; // Multiplicador cuando tiene armas equipadas
    public float unarmedSprintMultiplier = 3f; // Multiplicador cuando NO tiene armas equipadas
    private bool isSprinting = false;

    private Vector2 newDirection;

    public bool hasPistol = false;
    public bool hasRiffle = false;
    public bool hasGrenade = false;

    //Cámara
    public Transform cameraAxis;
    public Transform cameraTrack;
    private Transform theCamera;
    public Transform cameraWeaponTrack;

    private float rotY = 0f;
    private float rotX = 0f;
    public float camRotSpeed = 200f;
    public float minAngle = -45f;
    public float maxAngle = 45f;
    public float cameraSpeed = 200f;


    //Items
    public GameObject nearItem;

    public GameObject[] itemPrefabs;

    public Transform itemSlot;

    public GameObject crosshair;

    //Armas
    public int weapons;

    public GameObject primaryWeapon;
    public GameObject secondaryWeapon;
    public GameObject throwableWeapon;

    public Transform primarySlot;
    public Transform secondarySlot;
    public Transform throwableSlot;

    public Transform spawnGrenade;

    // Referencias a los prefabs "Dropped" para cada slot (para poder soltarlos correctamente)
    private GameObject primaryDroppedPrefab;
    private GameObject secondaryDroppedPrefab;
    private GameObject throwableDroppedPrefab;

    //UI

    public Canvas playerUI;
    public Image primaryWeaponIcon;
    public Image secondaryWeaponIcon;
    public Image throwableWeaponIcon;
    
    // Start is called before the first frame update
    void Start()
    {
        playerTr = this.transform;
        playerRb = GetComponent<Rigidbody>();
        playerAnim = GetComponentInChildren<Animator>();
        playerRagdoll = GetComponentInChildren<RagdollController>();

        

        theCamera = Camera.main.transform;


        
        Cursor.lockState = CursorLockMode.Locked;
        
        currentHealth = maxHealth;
        Active = true;
        
    }

    // Update is called once per frame
    void Update()
    {
        // NO procesar input si el juego está pausado (Time.timeScale == 0)
        if (Time.timeScale == 0f)
        {
            return;
        }
        
        // Si el jugador está muerto, detener todo
        if (!Active)
        {
            return;
        }
        
        if (Player)
        {
            MoveLogic();
            CameraLogic();
        }
        
        ItemLogic();
        AnimLogic();

      


        if (Input.GetKeyDown(KeyCode.Y))
        {
            TakeDamage(10f);
        }
    }

    public void MoveLogic()
    {
        Vector3 direction = playerRb.velocity;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        float theTime = Time.deltaTime;

        // Detectar si está corriendo (Shift presionado)
        isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Calcular velocidad actual
        float currentSpeed = playerSpeed;
        
        // Si está corriendo, aplicar multiplicador correspondiente
        if (isSprinting)
        {
            // Verificar si todos los slots están desactivados O vacíos
            bool allSlotsInactive = !primarySlot.gameObject.activeSelf && 
                                    !secondarySlot.gameObject.activeSelf && 
                                    !throwableSlot.gameObject.activeSelf;
            
            bool allSlotsEmpty = primarySlot.childCount == 0 && 
                                 secondarySlot.childCount == 0 && 
                                 throwableSlot.childCount == 0;
            
            // Si todos los slots están desactivados O vacíos (sin armas), velocidad x3
            // Si hay armas y algún slot está activo, velocidad x2
            currentSpeed *= (allSlotsInactive || allSlotsEmpty) ? unarmedSprintMultiplier : sprintMultiplier;
        }

        newDirection = new Vector2(moveX, moveZ);

        Vector3 side = currentSpeed * moveX * theTime * playerTr.right;
        Vector3 forward = currentSpeed * moveZ * theTime * playerTr.forward;

        Vector3 endDirection = side + forward;

        playerRb.velocity = endDirection;
    }

    public void CameraLogic()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        float theTime = Time.deltaTime;

        rotY += mouseY * camRotSpeed * theTime;
        rotX = mouseX * camRotSpeed * theTime;

        playerTr.Rotate(0, rotX, 0);

        rotY = Mathf.Clamp(rotY, minAngle, maxAngle);

        Quaternion localRotation = Quaternion.Euler(-rotY, 0, 0);

        cameraAxis.localRotation = localRotation;

        if (hasPistol || hasRiffle || hasGrenade)
        {
            cameraTrack.gameObject.SetActive(false);
            cameraWeaponTrack.gameObject.SetActive(true);

            crosshair.gameObject.SetActive(true);

            theCamera.position = Vector3.Lerp(theCamera.position, cameraWeaponTrack.position, cameraSpeed * theTime);
            theCamera.rotation = Quaternion.Lerp(theCamera.rotation, cameraWeaponTrack.rotation, cameraSpeed * theTime);
        }
        else
        {
            cameraTrack.gameObject.SetActive(true);
            cameraWeaponTrack.gameObject.SetActive(false);
            
            theCamera.position = Vector3.Lerp(theCamera.position, cameraTrack.position, cameraSpeed * theTime);
            theCamera.rotation = Quaternion.Lerp(theCamera.rotation, cameraTrack.rotation, cameraSpeed * theTime);
        }
        
        
    


    }

    public void ItemLogic()
    {
        if (nearItem == null)
            return;

        // Tecla E: Recoger item solo si el slot está vacío
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickupItem(false); // false = no intercambiar
        }
        // Tecla F: Intercambiar item (soltar el actual y recoger el nuevo)
        else if (Input.GetKeyDown(KeyCode.F))
        {
            TryPickupItem(true); // true = intercambiar si slot está lleno
        }
    }

    private void TryPickupItem(bool allowSwap)
    {
        if (nearItem == null)
            return;

        GameObject instantiatedItem = null;

        foreach (GameObject itemPrefab in itemPrefabs)
        {
            if (itemPrefab.CompareTag("PW") && nearItem.CompareTag("PW"))
            {
                // Verificar si el slot ya tiene un arma
                if (primarySlot.childCount > 0)
                {
                    if (!allowSwap)
                    {
                        return; // No permitir recoger si el slot está lleno y no es intercambio
                    }
                    else
                    {
                        // Intercambiar: soltar arma actual al piso
                        DropWeaponFromSlot(primarySlot, nearItem.transform.position, primaryDroppedPrefab);
                    }
                }

                // Guardar referencia al prefab "Dropped" original (nearItem es el objeto Dropped en el mundo)
                primaryDroppedPrefab = nearItem;

                // Instanciar como hijo del itemSlot
                instantiatedItem = Instantiate(itemPrefab, itemSlot);
                
                // Resetear posición y rotación local
                instantiatedItem.transform.localPosition = Vector3.zero;
                instantiatedItem.transform.localRotation = Quaternion.identity;
                
                primaryWeapon = this.gameObject;

                if (primarySlot.childCount == 0) // Solo incrementar si el slot estaba vacío
                    weapons++;

                Destroy(nearItem.gameObject);
                instantiatedItem.transform.parent = primarySlot;

                nearItem = null;

                WeaponController pwIcon = instantiatedItem.GetComponentInChildren<WeaponController>();
                primaryWeaponIcon.sprite = pwIcon.weaponIcon;
                primaryWeaponIcon.gameObject.SetActive(true);

                break;
            }
                else if (itemPrefab.CompareTag("SW") && nearItem.CompareTag("SW"))
            {
                // Verificar si el slot ya tiene un arma
                if (secondarySlot.childCount > 0)
                {
                    if (!allowSwap)
                    {
                        return; // No permitir recoger si el slot está lleno y no es intercambio
                    }
                    else
                    {
                        // Intercambiar: soltar arma actual al piso
                        DropWeaponFromSlot(secondarySlot, nearItem.transform.position, secondaryDroppedPrefab);
                    }
                }

                // Guardar referencia al prefab "Dropped" original
                secondaryDroppedPrefab = nearItem;

                // Instanciar como hijo del itemSlot
                instantiatedItem = Instantiate(itemPrefab, itemSlot);
                
                // Resetear posición y rotación local
                instantiatedItem.transform.localPosition = Vector3.zero;
                instantiatedItem.transform.localRotation = Quaternion.identity;
                
                secondaryWeapon = this.gameObject;

                if (secondarySlot.childCount == 0) // Solo incrementar si el slot estaba vacío
                    weapons++;

                Destroy(nearItem.gameObject);
                instantiatedItem.transform.parent = secondarySlot;

                nearItem = null;
                WeaponController swIcon = instantiatedItem.GetComponentInChildren<WeaponController>();
                secondaryWeaponIcon.sprite = swIcon.weaponIcon;
                secondaryWeaponIcon.gameObject.SetActive(true);

                break;
            }
                else if (itemPrefab.CompareTag("TW") && nearItem.CompareTag("TW"))
            {
                // Verificar si el slot ya tiene un arma
                if (throwableSlot.childCount > 0)
                {
                    if (!allowSwap)
                    {
                        return; // No permitir recoger si el slot está lleno y no es intercambio
                    }
                    else
                    {
                        // Intercambiar: soltar arma actual al piso
                        DropWeaponFromSlot(throwableSlot, nearItem.transform.position, throwableDroppedPrefab);
                    }
                }

                // Reactivar el throwableSlot por si había sido desactivado al lanzar una granada previa
                if (throwableSlot != null && !throwableSlot.gameObject.activeSelf)
                {
                    throwableSlot.gameObject.SetActive(true);
                }

                // Guardar referencia al prefab "Dropped" original
                throwableDroppedPrefab = nearItem;

                // Instanciar como hijo del itemSlot
                instantiatedItem = Instantiate(itemPrefab, itemSlot);
                
                // Resetear posición y rotación local
                instantiatedItem.transform.localPosition = Vector3.zero;
                instantiatedItem.transform.localRotation = Quaternion.identity;
                
                throwableWeapon = this.gameObject;

                if (throwableSlot.childCount == 0) // Solo incrementar si el slot estaba vacío
                    weapons++;

                Destroy(nearItem.gameObject);
                instantiatedItem.transform.parent = throwableSlot;

                nearItem = null;

                GrenadeController twIcon = instantiatedItem.GetComponentInChildren<GrenadeController>();
                throwableWeaponIcon.sprite = twIcon.weaponIcon;
                throwableWeaponIcon.gameObject.SetActive(true);

                break;
            }
        }
    }

    // Método para soltar el arma del slot al piso
    private void DropWeaponFromSlot(Transform slot, Vector3 dropPosition, GameObject droppedPrefabReference)
    {
        if (slot.childCount == 0)
            return;

        // Obtener el arma actual del slot (Hand Grenade, Hand Pistol, etc.)
        Transform weaponTransform = slot.GetChild(0);
        GameObject weaponObject = weaponTransform.gameObject;
        
        // Intentar obtener el prefab "Dropped" desde el componente del arma
        GameObject droppedPrefab = null;
        
        // Primero, intentar obtener desde WeaponController o GrenadeController
        WeaponController weaponCtrl = weaponObject.GetComponentInChildren<WeaponController>();
        if (weaponCtrl != null && weaponCtrl.droppedItemPrefab != null)
        {
            droppedPrefab = weaponCtrl.droppedItemPrefab;
        }
        else
        {
            GrenadeController grenadeCtrl = weaponObject.GetComponentInChildren<GrenadeController>();
            if (grenadeCtrl != null && grenadeCtrl.droppedItemPrefab != null)
            {
                droppedPrefab = grenadeCtrl.droppedItemPrefab;
            }
        }
        
        // Si no se encontró en el componente, usar la referencia guardada (fallback)
        if (droppedPrefab == null)
        {
            droppedPrefab = droppedPrefabReference;
        }
        
        // Si aún no hay referencia, buscar en itemPrefabs[] por nombre/tipo (último fallback)
        if (droppedPrefab == null)
        {
            string droppedNameToFind = "";
            
            if (slot == primarySlot)
            {
                droppedNameToFind = "M1911";
            }
            else if (slot == secondarySlot)
            {
                droppedNameToFind = "Ak-7";
            }
            else if (slot == throwableSlot)
            {
                droppedNameToFind = "Grenade";
            }
            
            // Buscar el prefab "Dropped" en itemPrefabs[]
            foreach (GameObject itemPrefab in itemPrefabs)
            {
                if (itemPrefab != null && 
                    itemPrefab.CompareTag("Item") && 
                    itemPrefab.name.Contains(droppedNameToFind))
                {
                    droppedPrefab = itemPrefab;
                    break;
                }
            }
        }
        
        // Verificar que tengamos un prefab "Dropped" válido
        if (droppedPrefab == null)
        {
            return;
        }
        
        // Guardar la posición y rotación del nearItem original antes de destruirlo
        Vector3 originalPosition = nearItem.transform.position;
        Quaternion originalRotation = nearItem.transform.rotation;
        
        // Destruir el objeto "Hand" del slot
        Destroy(weaponObject);
        
        // Instanciar el prefab "Dropped" en la misma posición y rotación que el nearItem original
        GameObject droppedWeapon = Instantiate(droppedPrefab, originalPosition, originalRotation);
        
        // Asegurar que esté en la capa "Item"
        droppedWeapon.layer = LayerMask.NameToLayer("Item");
        
        // Decrementar el contador de armas
        weapons--;
        
    }

    

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        // Asegurar que la vida esté entre 0 y maxHealth
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (currentHealth <= 0f)
        {

            playerRagdoll.Active(true);
            Active = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Item"))
        {
            nearItem = other.gameObject;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Item"))
        {
            nearItem = null;
        }
        
    }


    public void AnimLogic()
    {
        playerAnim.SetFloat("X", newDirection.x);
        playerAnim.SetFloat("Y", newDirection.y);

        playerAnim.SetBool("holdPistol", hasPistol);
        playerAnim.SetBool("holdRiffle", hasRiffle);
        playerAnim.SetBool("holdGrenade", hasGrenade);

        if (hasPistol || hasRiffle)
        {
            playerAnim.SetLayerWeight(2, 0);
            playerAnim.SetLayerWeight(1, 1);
        }
        else if (hasGrenade)
        {
            playerAnim.SetLayerWeight(1, 0);
            playerAnim.SetLayerWeight(2, 1);
        }
        else
        {
            // Ningún arma activa: resetear capas a 0 para usar animación base (caminar)
            playerAnim.SetLayerWeight(1, 0);
            playerAnim.SetLayerWeight(2, 0);
        }
        
    }
}
