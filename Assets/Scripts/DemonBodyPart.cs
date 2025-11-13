using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemonBodyPart : MonoBehaviour
{
    [Header("Settings")]
    public DemonController demon;
    public string bodyPartName = "Body";
    public float damageMultiplier = 1f;

    [Header("Debug")]
    public float lastDamage = 0f;

    // El Start ya no es necesario porque el DemonRagdollController asigna todo

    public void TakeHit(float damage)
    {
        if (demon == null)
        {
            return;
        }

        lastDamage = damage * damageMultiplier;
        demon.TakeDamage(lastDamage);

    }
}
