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
    public AudioSource audioSource;
    public AudioClip abilityUnlockCue;

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
    bool isJumping;
    bool isClimbing;
    bool isTeleporting;

    [Header("Ability Settings")]
    public float jumpHeight = 1.25f;
    public float jumpDuration = 0.35f;
    public float jumpForwardMaxDistance = 15f;
    public float jumpCooldown = 4f;
    public float climbSpeed = 4f;
    public float climbHeightThreshold = 1.2f;
    public float climbCooldown = 5f;
    public float teleportMinDistance = 10f;
    public float teleportMaxDistance = 20f;
    public float teleportCooldown = 8f;

    float nextJumpTime;
    float nextClimbTime;
    float nextTeleportTime;

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

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

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
        TryUseAbilities(dist);

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

    void TryUseAbilities(float dist)
    {
        float heightDelta = player.position.y - transform.position.y;
        bool hasPath = TryGetPathToPlayer(out float pathLength, out bool pathComplete);
        bool shorterPathAvailable = hasPath && pathComplete && pathLength > dist * 1.25f;

        if (canTeleport && !isTeleporting && Time.time >= nextTeleportTime && ShouldTeleport(dist, pathComplete))
            StartCoroutine(Teleport());

        bool shouldClimb = heightDelta >= climbHeightThreshold && (shorterPathAvailable || (hasPath && !pathComplete));
        if (canClimb && !isClimbing && Time.time >= nextClimbTime && shouldClimb)
        {
            StartCoroutine(Climb(heightDelta));
            return;
        }

        bool shouldJump = shorterPathAvailable && heightDelta < climbHeightThreshold;
        if (canJump && !isJumping && Time.time >= nextJumpTime && shouldJump)
            StartCoroutine(Jump());
    }

    bool TryGetPathToPlayer(out float pathLength, out bool pathComplete)
    {
        pathLength = 0f;
        pathComplete = false;

        if (player == null || !agent.isOnNavMesh)
            return false;

        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(player.position, path))
            return false;

        pathComplete = path.status == NavMeshPathStatus.PathComplete;
        if (path.corners.Length < 2)
            return true;

        for (int i = 1; i < path.corners.Length; i++)
            pathLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);

        return true;
    }

    bool ShouldTeleport(float dist, bool pathComplete)
    {
        bool farFromPlayer = dist >= teleportMinDistance;
        bool closeForConfuse = dist <= attackRange * 1.25f;
        bool blockedPath = !pathComplete;
        return farFromPlayer || closeForConfuse || blockedPath;
    }

    IEnumerator Jump()
    {
        isJumping = true;
        nextJumpTime = Time.time + jumpCooldown;
        float startOffset = agent.baseOffset;
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            float t = elapsed / jumpDuration;
            float arc = Mathf.Sin(t * Mathf.PI);
            agent.baseOffset = startOffset + arc * jumpHeight;
            elapsed += Time.deltaTime;
            yield return null;
        }

        agent.baseOffset = startOffset;
        isJumping = false;
    }

    IEnumerator Climb(float heightDelta)
    {
        isClimbing = true;
        nextClimbTime = Time.time + climbCooldown;
        Vector3 climbTarget = new Vector3(transform.position.x, player.position.y, transform.position.z);
        if (NavMesh.SamplePosition(climbTarget, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            float startOffset = agent.baseOffset;
            float targetOffset = startOffset + Mathf.Max(heightDelta, climbHeightThreshold);
            float elapsed = 0f;

            while (elapsed < climbDuration)
            {
                float t = elapsed / climbDuration;
                agent.baseOffset = Mathf.Lerp(startOffset, targetOffset, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            agent.baseOffset = startOffset;
            agent.Warp(hit.position);
        }
        else
        {
            yield return new WaitForSeconds(climbDuration);
        }

        isClimbing = false;
    }

    IEnumerator Teleport()
    {
        isTeleporting = true;
        nextTeleportTime = Time.time + teleportCooldown;

        float currentDist = Vector3.Distance(transform.position, player.position);
        bool getCloser = currentDist > attackRange * 1.5f;
        Vector3 target;

        if (getCloser)
        {
            Vector3 toEnemy = (transform.position - player.position).normalized;
            if (toEnemy == Vector3.zero)
                toEnemy = -player.forward;

            float desiredDistance = Mathf.Clamp(currentDist * 0.5f, attackRange * 0.75f, teleportMaxDistance);
            target = player.position + toEnemy * desiredDistance;
        }
        else
        {
            float randomDistance = Mathf.Clamp(Random.Range(teleportMinDistance * 0.5f, teleportMaxDistance), attackRange, teleportMaxDistance);
            Vector2 offset2D = Random.insideUnitCircle.normalized * randomDistance;
            target = player.position + new Vector3(offset2D.x, 0f, offset2D.y);
        }

        if (TryFindTeleportTarget(target, out Vector3 finalTarget))
            agent.Warp(finalTarget);

        yield return null;
        isTeleporting = false;
    }

    bool TryFindTeleportTarget(Vector3 desiredTarget, out Vector3 finalTarget)
    {
        const int maxAttempts = 6;
        const float sampleRadius = 6f;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 candidate = desiredTarget;
            if (i > 0)
            {
                Vector2 jitter = Random.insideUnitCircle * sampleRadius;
                candidate += new Vector3(jitter.x, 0f, jitter.y);
            }

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            {
                finalTarget = hit.position;
                return true;
            }
        }

        finalTarget = transform.position;
        return false;
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
                PlayUnlockCue();
                break;

            case EnemyAbilityType.Climb:
                canClimb = true;
                Debug.Log("Enemy unlocked CLIMB");
                PlayUnlockCue();
                break;

            case EnemyAbilityType.Teleport:
                canTeleport = true;
                Debug.Log("Enemy unlocked TELEPORT");
                PlayUnlockCue();
                break;
        }
    }

    void PlayUnlockCue()
    {
        if (audioSource != null && abilityUnlockCue != null)
            audioSource.PlayOneShot(abilityUnlockCue);
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
