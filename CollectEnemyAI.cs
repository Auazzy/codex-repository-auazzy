using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CollectEnemyAI : MonoBehaviour
{
    enum State
    {
        Idle,
        Camping,
        Chasing,
        Attacking
    }

    [Header("References")]
    public NavMeshAgent agent;
    public Collider attackHitbox; // child object

    Transform player;
    Transform currentCustard;
    State state;

    // ======================
    // MOVEMENT
    // ======================
    [Header("Movement Speeds")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6.5f;

    // ======================
    // RANGES
    // ======================
    [Header("Ranges")]
    public float chaseRange = 12f;
    public float loseRange = 16f;
    public float attackRange = 2.2f;

    // ======================
    // CAMPING
    // ======================
    [Header("Camping")]
    public float campRadius = 4f;
    public float campWanderDelay = 3f;
    float campTimer;

    // ======================
    // CHASE LOGIC
    // ======================
    [Header("Chase")]
    public float loseTime = 2.5f;
    float loseTimer;

    // ======================
    // ATTACK
    // ======================
    [Header("Attack")]
    public float attackWindup = 1.2f;
    public float attackActiveTime = 0.25f;
    public float attackCooldown = 1.5f;

    bool canAttack = true;

    // ======================
    // INIT
    // ======================
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
            Debug.LogError("CollectEnemyAI: Player not found!");

        if (!agent.isOnNavMesh)
            Debug.LogError("CollectEnemyAI: Enemy not on NavMesh!");

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        agent.isStopped = false;
        agent.speed = walkSpeed;
        state = State.Idle;
    }

    void Update()
    {
        if (player == null)
            return;

        // 🔒 SAFETY NET: never stay stopped unless attacking
        if (state != State.Attacking && agent.isStopped)
            agent.isStopped = false;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Idle:
                FindCustard();
                break;

            case State.Camping:
                CampUpdate(dist);
                break;

            case State.Chasing:
                ChaseUpdate(dist);
                break;

            case State.Attacking:
                // fully controlled by coroutine
                break;
        }
    }

    // ======================
    // CAMPING
    // ======================
    void CampUpdate(float dist)
    {
        ValidateCustard();

        if (dist <= chaseRange)
        {
            StartChase();
            return;
        }

        campTimer -= Time.deltaTime;
        if (campTimer <= 0f)
        {
            WanderAroundCustard();
            campTimer = campWanderDelay;
        }
    }

    void WanderAroundCustard()
    {
        if (currentCustard == null)
            return;

        Vector3 offset = Random.insideUnitSphere * campRadius;
        offset.y = 0f;

        agent.SetDestination(currentCustard.position + offset);
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
            return;
        }

        if (dist > loseRange)
        {
            loseTimer += Time.deltaTime;
            if (loseTimer >= loseTime)
                GoBackToCustard();
        }
        else
        {
            loseTimer = 0f;
        }
    }

    void StartChase()
    {
        agent.isStopped = false;
        agent.speed = runSpeed;
        loseTimer = 0f;
        state = State.Chasing;
    }

    void GoBackToCustard()
    {
        agent.isStopped = false;
        agent.speed = walkSpeed;
        state = State.Camping;
    }

    // ======================
    // CUSTARDS
    // ======================
    void FindCustard()
    {
        GameObject[] custards = GameObject.FindGameObjectsWithTag("Custard");
        if (custards.Length == 0)
            return;

        currentCustard = custards[Random.Range(0, custards.Length)].transform;
        campTimer = 0f;
        agent.isStopped = false;
        agent.speed = walkSpeed;
        state = State.Camping;
    }

    void ValidateCustard()
    {
        if (currentCustard == null || !currentCustard.gameObject.activeInHierarchy)
            state = State.Idle;
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

        yield return new WaitForSeconds(attackWindup);

        if (attackHitbox != null)
            attackHitbox.enabled = true;

        yield return new WaitForSeconds(attackActiveTime);

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        yield return new WaitForSeconds(attackCooldown);

        agent.isStopped = false;
        agent.speed = runSpeed;
        state = State.Chasing;
        canAttack = true;
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