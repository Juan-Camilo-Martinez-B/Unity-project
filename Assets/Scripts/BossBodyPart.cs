using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossBodyPart : MonoBehaviour
{
    [Header("Settings")]
    public BossController boss;
    public string bodyPartName = "Body";
    public float damageMultiplier = 1f;

    [Header("Debug")]
    public float lastDamage = 0f;

    public void TakeHit(float damage)
    {
        if (boss == null)
        {
            Debug.LogWarning($"No hay boss asignado en {gameObject.name}");
            return;
        }

        lastDamage = damage * damageMultiplier;
        boss.TakeDamage(lastDamage);

        Debug.Log($"Boss golpeado en {bodyPartName}: {damage} * {damageMultiplier} = {lastDamage}");
    }
}
