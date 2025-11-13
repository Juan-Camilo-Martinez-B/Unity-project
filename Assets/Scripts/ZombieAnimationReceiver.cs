using UnityEngine;

/// <summary>
/// Coloca este script en el GameObject que tiene el Animator
/// Redirige los Animation Events al ZombieController que esté en los padres
/// </summary>
public class ZombieAnimationReceiver : MonoBehaviour
{
    // Este método se llama desde un Animation Event en el frame del golpe
    public void OnAttackHit()
    {
        ZombieController zombie = GetComponentInParent<ZombieController>();
        if (zombie != null)
        {
            zombie.OnAttackHit();
        }
        else
        {
        }
    }

    // Este método se llama desde un Animation Event al final de la animación
    public void OnAttackEnd()
    {
        ZombieController zombie = GetComponentInParent<ZombieController>();
        if (zombie != null)
        {
            zombie.OnAttackEnd();
        }
        else
        {
        }
    }
}
