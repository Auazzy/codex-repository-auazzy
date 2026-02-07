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
    public float speedIncrease = 0.3f;

    // ======================
    // ATTACK
    // ======================
    [Header("Attack")]
    public float baseWindup = 1.2f;
    public float windupDecrease = 0.05f;
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
    int lastScaleStep = -1;

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

        HandleDifficultyScaling();

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
    void HandleDifficultyScaling()
    {
        float elapsed = Time.timeSinceLevelLoad;
        int difficulty = GameSettings.difficulty;

        float interval =
            difficulty == 0 ? 300f :   // Easy: 5 min
            difficulty == 1 ? 180f :   // Normal: 3 min
            difficulty == 2 ? 60f  :   // Hard: 1 min
                              30f;    // Insane: 30 sec

        int step = Mathf.FloorToInt(elapsed / interval);

        if (step == lastScaleStep)
            return;

        lastScaleStep = step;

        // Speed scales cleanly (no Mach 5 spawn)
        baseRunSpeed += speedIncrease;
        agent.speed = baseRunSpeed;

        // Windup gets faster but never broken
        baseWindup = Mathf.Max(0.3f, baseWindup - windupDecrease);
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
