using UnityEngine;

public class EnemyAttackHitbox : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        var death = other.GetComponent<PlayerDeathHandler>();
        if (death != null)
        {
            death.KillPlayer();
        }
    }
}
