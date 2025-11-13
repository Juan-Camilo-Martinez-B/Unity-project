using UnityEngine;

/// <summary>
/// Coloca este script en el GameObject que tiene el Animator
/// Redirige los Animation Events al DemonController que esté en los padres
/// </summary>
public class DemonAnimationReceiver : MonoBehaviour
{
    // Este método se llama desde un Animation Event en el frame del golpe
    public void OnAttackHit()
    {
        DemonController demon = GetComponentInParent<DemonController>();
        if (demon != null)
        {
            demon.OnAttackHit();
        }
        else
        {
        }
    }

    // Este método se llama desde un Animation Event al final de la animación
    public void OnAttackEnd()
    {
        DemonController demon = GetComponentInParent<DemonController>();
        if (demon != null)
        {
            demon.OnAttackEnd();
        }
        else
        {
        }
    }
}
