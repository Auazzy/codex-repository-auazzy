using UnityEngine;

public class EnemyAttackHitbox : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (GameSettings.gamemode == 1)
        {
            SurvivalController controller = FindObjectOfType<SurvivalController>();
            SurvivalEnemyAI enemyAI = GetComponentInParent<SurvivalEnemyAI>();

            if (controller != null && enemyAI != null)
                controller.DamagePlayer(enemyAI.GetCurrentAttackDamage());

            return;
        }

        var death = other.GetComponent<PlayerDeathHandler>();
        if (death != null)
            death.KillPlayer();
    }
}
