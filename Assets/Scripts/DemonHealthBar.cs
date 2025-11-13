using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DemonHealthBar : MonoBehaviour
{
    [Header("References")]
    public DemonController demon;
    public Image healthBarFill;
    public RectTransform rectTransform;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2f, 0); // Offset sobre el demon
    public bool alwaysFaceCamera = true;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        // No mostrar error aquí porque el demon se asigna desde DemonController después del Start
    }

    void Update()
    {
        if (demon == null)
            return;

        // Actualizar la posición de la barra sobre el demon
        UpdatePosition();

        // Actualizar el fill de la barra
        UpdateHealthBar();

        // Hacer que la barra siempre mire a la cámara
        if (alwaysFaceCamera && mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                            mainCamera.transform.rotation * Vector3.up);
        }
    }

    void UpdatePosition()
    {
        if (demon == null || mainCamera == null)
            return;

        // Posición del demon + offset
        Vector3 worldPosition = demon.transform.position + offset;
        
        // Actualizar la posición directamente en el mundo
        transform.position = worldPosition;
    }

    void UpdateHealthBar()
    {
        if (demon == null || healthBarFill == null)
            return;

        // Calcular el porcentaje de vida
        float healthPercentage = demon.currentHealth / demon.maxHealth;

        // Si la vida llega a 0 o menos, ocultar la barra
        if (healthPercentage <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        // Cambiar el ancho de la barra mediante el RectTransform
        RectTransform fillRect = healthBarFill.GetComponent<RectTransform>();
        if (fillRect != null)
        {
            // Cambiar solo el ancho (X) manteniendo Y en 1
            fillRect.localScale = new Vector3(healthPercentage, 1f, 1f);
        }

        // Cambiar color según el porcentaje de vida
        healthBarFill.color = Color.Lerp(Color.red, Color.green, healthPercentage);
    }
}
