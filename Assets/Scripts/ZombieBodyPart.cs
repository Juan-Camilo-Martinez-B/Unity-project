using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieBodyPart : MonoBehaviour
{
    [Header("Settings")]
    public ZombieController zombie;
    public string bodyPartName = "Body";
    public float damageMultiplier = 1f;

    [Header("Debug")]
    public float lastDamage = 0f;

    // El Start ya no es necesario porque el ZombieRagdollController asigna todo

    public void TakeHit(float damage)
    {
        if (zombie == null)
        {
            return;
        }

        lastDamage = damage * damageMultiplier;
        zombie.TakeDamage(lastDamage);

    }
}
