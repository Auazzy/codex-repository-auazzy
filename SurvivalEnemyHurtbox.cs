using UnityEngine;

public class SurvivalEnemyHurtbox : MonoBehaviour
{
    public enum HurtboxType
    {
        Body,
        Head
    }

    public SurvivalEnemyAI enemy;
    public HurtboxType hurtboxType = HurtboxType.Body;

    void Awake()
    {
        if (enemy == null)
            enemy = GetComponentInParent<SurvivalEnemyAI>();
    }

    public void ApplyHit(float damage)
    {
        if (enemy == null)
            return;

        enemy.TakeDamage(damage, hurtboxType == HurtboxType.Head);
    }
}
