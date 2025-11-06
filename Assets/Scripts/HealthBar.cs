using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{

    public PlayerController thePlayer;

    public Image lifebarFill;

    public RectTransform rectTransform;
    // Start is called before the first frame update
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        float healthPercentage = thePlayer.currentHealth / thePlayer.maxHealth;

        rectTransform.localScale = new Vector3(healthPercentage, 1, 1);

        lifebarFill.color = Color.Lerp(Color.red, Color.green, healthPercentage);
    }
}
