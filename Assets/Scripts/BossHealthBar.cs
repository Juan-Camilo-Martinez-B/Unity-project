using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    [Header("References")]
    public BossController boss;
    public Image healthBarFill;
    public RectTransform rectTransform;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 4f, 0);
    public bool alwaysFaceCamera = true;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (boss == null)
            return;

        UpdatePosition();
        UpdateHealthBar();

        if (alwaysFaceCamera && mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                            mainCamera.transform.rotation * Vector3.up);
        }
    }

    void UpdatePosition()
    {
        if (boss == null || mainCamera == null)
            return;

        Vector3 worldPosition = boss.transform.position + offset;
        transform.position = worldPosition;
    }

    void UpdateHealthBar()
    {
        if (boss == null || healthBarFill == null)
            return;

        float healthPercentage = boss.currentHealth / boss.maxHealth;

        if (healthPercentage <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        RectTransform fillRect = healthBarFill.GetComponent<RectTransform>();
        if (fillRect != null)
        {
            fillRect.localScale = new Vector3(healthPercentage, 1f, 1f);
        }

        healthBarFill.color = Color.Lerp(Color.red, Color.green, healthPercentage);
    }
}
