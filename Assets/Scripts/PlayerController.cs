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

        newDirection = new Vector2(moveX, moveZ);

        Vector3 side = playerSpeed * moveX * theTime * playerTr.right;
        Vector3 forward = playerSpeed * moveZ * theTime * playerTr.forward;

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
        if (nearItem != null && Input.GetKeyDown(KeyCode.E))
        {
            // Instanciar el item directamente como hijo del itemSlot
            GameObject instantiatedItem = null;



            int countWeapons = 0;

            foreach (GameObject itemPrefab in itemPrefabs)
            {
                if (itemPrefab.CompareTag("PW") && nearItem.CompareTag("PW"))
                {
                    // Instanciar como hijo del itemSlot
                    instantiatedItem = Instantiate(itemPrefab, itemSlot);
                    
                    // Resetear posición y rotación local
                    instantiatedItem.transform.localPosition = Vector3.zero;
                    instantiatedItem.transform.localRotation = Quaternion.identity;
                    
                    primaryWeapon = this.gameObject;

                    countWeapons++;
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
                    // Instanciar como hijo del itemSlot
                    instantiatedItem = Instantiate(itemPrefab, itemSlot);
                    
                    // Resetear posición y rotación local
                    instantiatedItem.transform.localPosition = Vector3.zero;
                    instantiatedItem.transform.localRotation = Quaternion.identity;
                    
                    secondaryWeapon = this.gameObject;

                    countWeapons++;
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
                    // Instanciar como hijo del itemSlot
                    instantiatedItem = Instantiate(itemPrefab, itemSlot);
                    
                    // Resetear posición y rotación local
                    instantiatedItem.transform.localPosition = Vector3.zero;
                    instantiatedItem.transform.localRotation = Quaternion.identity;
                    
                    throwableWeapon = this.gameObject;

                    countWeapons++;
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

    }

    

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            Debug.Log("A casa malo!");

            playerRagdoll.Active(true);
            Active = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Item"))
        {
            Debug.Log("Hay un Item cerca!");
            nearItem = other.gameObject;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Item"))
        {
            Debug.Log("Ya no hay un Item cerca!");
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
        
    }
}
