using UnityEngine;

/// <summary>
/// Coloca este script en el GameObject que tiene el Animator del Boss
/// Redirige los Animation Events al BossController que esté en los padres
/// </summary>
public class BossAnimationReceiver : MonoBehaviour
{
    // Este método se llama desde un Animation Event en el frame del golpe
    public void OnAttackHit()
    {
        BossController boss = GetComponentInParent<BossController>();
        if (boss != null)
        {
            boss.OnAttackHit();
        }
        else
        {
        }
    }

    // Este método se llama desde un Animation Event al final de la animación
    public void OnAttackEnd()
    {
        BossController boss = GetComponentInParent<BossController>();
        if (boss != null)
        {
            boss.OnAttackEnd();
        }
        else
        {
        }
    }
}
