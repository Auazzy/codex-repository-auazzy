using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SurvivalAltEnemyAI : MonoBehaviour
{
    enum State
    {
        Chasing,
        Attacking
    }

    public enum EnemyAbilityType
    {
        Jump,
        Climb,
        Teleport
    }



    [Header("References")]
    public NavMeshAgent agent;
    public Collider attackHitbox; // child object

    private Transform player;
    private State state;

    // ======================
    // MOVEMENT
    // ======================
    [Header("Movement")]
    public float baseRunSpeed = 6.5f;

    // ======================
    // ATTACK
    // ======================
    [Header("Attack")]
    public float baseWindup = 1.2f;
    public float attackActiveTime = 0.25f;
    public float attackCooldown = 1.5f;

    // Same feel as Collect
    public float attackRange = 2.2f;

    bool canAttack = true;

    // ======================
    // ABILITIES (LOCKED BY DEFAULT)
    // ======================
    bool canJump;
    bool canClimb;
    bool canTeleport;

    // ======================
    // DIFFICULTY SCALING
    // ======================
    int lastScaleStep = 0;

    // ======================
    // INIT
    // ======================
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
            Debug.LogError("SurvivalAltEnemyAI: Player not found!");

        if (!agent.isOnNavMesh)
            Debug.LogError("SurvivalAltEnemyAI: Enemy not on NavMesh!");

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        agent.isStopped = false;
        agent.speed = baseRunSpeed;

        state = State.Chasing;
    }

    void Update()
    {
        if (player == null)
            return;

        // 🔒 same safety net as Collect
        if (state != State.Attacking && agent.isStopped)
            agent.isStopped = false;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Chasing:
                ChaseUpdate(dist);
                break;

            case State.Attacking:
                // handled by coroutine
                break;
        }
    }

    // ======================
    // CHASING
    // ======================
    void ChaseUpdate(float dist)
    {
        agent.SetDestination(player.position);

        if (dist <= attackRange && canAttack)
        {
            StartCoroutine(Attack());
        }
    }

    // ======================
    // ATTACK
    // ======================
    IEnumerator Attack()
    {
        canAttack = false;
        state = State.Attacking;

        agent.isStopped = true;
        agent.ResetPath();

        yield return new WaitForSeconds(baseWindup);

        if (attackHitbox != null)
            attackHitbox.enabled = true;

        yield return new WaitForSeconds(attackActiveTime);

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        yield return new WaitForSeconds(attackCooldown);

        agent.isStopped = false;
        agent.speed = baseRunSpeed;
        state = State.Chasing;
        canAttack = true;
    }

    // ======================
    // ABILITY UNLOCK API
    // ======================
    public void UnlockAbility(EnemyAbilityType type)
    {
        switch (type)
        {
            case EnemyAbilityType.Jump:
                canJump = true;
                Debug.Log("Enemy unlocked JUMP");
                break;

            case EnemyAbilityType.Climb:
                canClimb = true;
                Debug.Log("Enemy unlocked CLIMB");
                break;

            case EnemyAbilityType.Teleport:
                canTeleport = true;
                Debug.Log("Enemy unlocked TELEPORT");
                break;
        }
    }

    // ======================
    // DIFFICULTY SCALING
    // ======================
    public void ApplyDifficultyScaling(float timeSurvived)
    {
        int difficulty = GameSettings.difficulty;

        float interval =
            difficulty == 0 ? 900f :   // Easy: 15 min
            difficulty == 1 ? 450f :   // Normal: 7.5 min
            difficulty == 2 ? 120f :   // Hard: 2 min
                              30f;    // Insane: 30 sec

        float windupDecrease =
            difficulty == 0 ? 0.01f :
            difficulty == 1 ? 0.025f :
            difficulty == 2 ? 0.035f :
                              0.05f;

        float speedIncrease =
            difficulty == 0 ? 1f :
            difficulty == 1 ? 1.5f :
            difficulty == 2 ? 2.25f :
                              2.75f;

        int step = Mathf.FloorToInt(timeSurvived / interval);

        if (step <= lastScaleStep)
            return;

        int stepsToApply = step - lastScaleStep;
        for (int i = 0; i < stepsToApply; i++)
        {
            baseRunSpeed += speedIncrease;
            baseWindup = Mathf.Max(0.3f, baseWindup - windupDecrease);
        }

        lastScaleStep = step;
        agent.speed = baseRunSpeed;
    }

    // ======================
    // HITBOX
    // ======================
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerDeathHandler death = other.GetComponent<PlayerDeathHandler>();
        if (death != null)
            death.KillPlayer();
    }
}
