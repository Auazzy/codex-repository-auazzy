using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class SurvivalEnemyAI : MonoBehaviour
{
    public enum EnemyArchetype
    {
        Newborn,
        NewbornV2,
        SpiderRobot,
        RocketeerNewborn,
        ShadowNewborn,
        Miniboss1,
        BladeRobot,
        Miniboss2,
        BerserkerNewborn,
        Miniboss3
    }

    [Header("Core")]
    public EnemyArchetype archetype = EnemyArchetype.Newborn;
    public NavMeshAgent agent;

    [Header("Melee Attack")]
    public Collider attackHitbox;
    public float attackWindup = 1f;
    public float attackActiveTime = 0.2f;
    public float attackCooldown = 1.2f;
    public float attackRange = 2f;

    [Header("Health & Damage")]
    public float maxHealth = 50f;
    public float currentHealth = 50f;
    public float basicDamage = 10f;

    [Header("Health Bar")]
    public GameObject healthBarRoot;
    public Image healthBarFill;
    public float healthBarVisibleDuration = 5f;

    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 4.5f;

    [Header("Specials")]
    public GameObject projectilePrefab;
    public Transform projectileSpawn;
    public float specialInterval = 10f;
    public float specialDuration = 3f;
    public float specialRange = 12f;

    private SurvivalController survivalController;
    private Transform player;
    private Renderer[] renderers;
    private float nextSpecialTime;
    private bool inSpecial;
    private bool canAttack = true;
    private bool isAttacking;
    private bool specialRoutineActive;
    private float healthBarHideTimer;

    static readonly float[] DamageMultipliers = { 0.4f, 1f, 2f, 4f };
    static readonly float[] SpeedMultipliers = { 0.8f, 1f, 1.2f, 1.5f };

    void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        renderers = GetComponentsInChildren<Renderer>(true);

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        ApplyArchetypeDefaults();
        ApplyDifficultyModifiers();
        currentHealth = maxHealth;
        UpdateHealthBarUI();

        if (healthBarRoot != null)
            healthBarRoot.SetActive(false);
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (survivalController == null)
            survivalController = FindObjectOfType<SurvivalController>();

        nextSpecialTime = Time.time + specialInterval;
    }

    void Update()
    {
        if (player == null || agent == null)
            return;

        float dist = Vector3.Distance(transform.position, player.position);

        UpdateMovement(dist);
        UpdateSpecial(dist);
        UpdateAttack(dist);
        UpdateHealthBarVisibility();
    }

    public void SetSurvivalController(SurvivalController controller)
    {
        survivalController = controller;
    }

    void UpdateMovement(float dist)
    {
        if (isAttacking)
        {
            agent.isStopped = true;
            return;
        }

        if (inSpecial && (archetype == EnemyArchetype.RocketeerNewborn || archetype == EnemyArchetype.BladeRobot))
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;

        if (archetype == EnemyArchetype.Miniboss1 || archetype == EnemyArchetype.Miniboss2 || archetype == EnemyArchetype.Miniboss3)
            agent.speed = dist <= attackRange * 2f ? walkSpeed : runSpeed;
        else
            agent.speed = runSpeed;

        agent.SetDestination(player.position);
    }

    void UpdateAttack(float dist)
    {
        if (!canAttack || isAttacking || dist > attackRange)
            return;

        if (inSpecial && archetype == EnemyArchetype.RocketeerNewborn)
            return;

        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        canAttack = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        yield return new WaitForSeconds(attackWindup);

        if (attackHitbox != null)
            attackHitbox.enabled = true;

        yield return new WaitForSeconds(attackActiveTime);

        if (attackHitbox != null)
            attackHitbox.enabled = false;

        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
        canAttack = true;
    }

    void UpdateSpecial(float dist)
    {
        if (specialRoutineActive || Time.time < nextSpecialTime)
            return;

        switch (archetype)
        {
            case EnemyArchetype.RocketeerNewborn:
                if (dist <= specialRange)
                {
                    specialRoutineActive = true;
                    StartCoroutine(RocketVolleyRoutine());
                }
                break;

            case EnemyArchetype.ShadowNewborn:
                specialRoutineActive = true;
                StartCoroutine(ShadowCloakRoutine());
                break;

            case EnemyArchetype.Miniboss1:
                specialRoutineActive = true;
                StartCoroutine(MinibossRoutine());
                break;

            case EnemyArchetype.BladeRobot:
                specialRoutineActive = true;
                StartCoroutine(BladeSpinRoutine());
                break;

            case EnemyArchetype.Miniboss2:
            case EnemyArchetype.Miniboss3:
                specialRoutineActive = true;
                StartCoroutine(LineStrikeRoutine());
                break;
        }

        nextSpecialTime = Time.time + specialInterval;
    }

    IEnumerator RocketVolleyRoutine()
    {
        inSpecial = true;
        canAttack = false;

        float end = Time.time + specialDuration;
        while (Time.time < end)
        {
            SpawnProjectile();
            yield return new WaitForSeconds(1f);
        }

        canAttack = true;
        inSpecial = false;
        specialRoutineActive = false;
    }

    IEnumerator ShadowCloakRoutine()
    {
        SetAlpha(0.2f);
        yield return new WaitForSeconds(10f);
        SetAlpha(0.02f);
        yield return new WaitForSeconds(25f);
        SetAlpha(1f);
        specialRoutineActive = false;
    }

    IEnumerator MinibossRoutine()
    {
        inSpecial = true;
        canAttack = false;

        if (agent != null)
            agent.speed = walkSpeed;

        float castEnd = Time.time + 4f;
        while (Time.time < castEnd)
        {
            SpawnProjectile();
            yield return new WaitForSeconds(0.75f);
        }

        WarpNearPlayerOnNavMesh(8f, 12f);

        float runEnd = Time.time + 20f;
        if (agent != null)
            agent.speed = runSpeed;

        while (Time.time < runEnd)
            yield return null;

        canAttack = true;
        inSpecial = false;
        specialRoutineActive = false;
    }

    IEnumerator BladeSpinRoutine()
    {
        inSpecial = true;
        canAttack = true;
        yield return new WaitForSeconds(10f);
        inSpecial = false;
        specialRoutineActive = false;
    }

    IEnumerator LineStrikeRoutine()
    {
        inSpecial = true;

        Vector3 direction = player != null ? (player.position - transform.position).normalized : transform.forward;
        Vector3 origin = transform.position + Vector3.up;

        for (int i = 0; i < 6; i++)
        {
            if (projectilePrefab != null)
            {
                Vector3 pos = origin + direction * (i * 2f);
                Instantiate(projectilePrefab, pos, Quaternion.LookRotation(direction));
            }

            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(archetype == EnemyArchetype.Miniboss3 ? 2f : 1f);
        inSpecial = false;
        specialRoutineActive = false;
    }

    void WarpNearPlayerOnNavMesh(float radius, float sampleDistance)
    {
        if (player == null || agent == null)
            return;

        Vector3 candidate = player.position + Random.insideUnitSphere * radius;
        candidate.y = player.position.y;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(candidate, out hit, sampleDistance, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return;
        }

        if (NavMesh.SamplePosition(transform.position, out hit, sampleDistance, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }

    void SpawnProjectile()
    {
        if (projectilePrefab == null)
            return;

        Transform spawnPoint = projectileSpawn != null ? projectileSpawn : transform;
        GameObject projectile = Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb != null && player != null)
        {
            Vector3 direction = (player.position - spawnPoint.position).normalized;
            rb.velocity = direction * 14f;
        }
    }


    void UpdateHealthBarVisibility()
    {
        if (healthBarRoot == null || !healthBarRoot.activeSelf)
            return;

        if (healthBarHideTimer > 0f)
        {
            healthBarHideTimer -= Time.deltaTime;
            return;
        }

        healthBarRoot.SetActive(false);
    }

    void ShowHealthBar()
    {
        if (healthBarRoot == null)
            return;

        healthBarRoot.SetActive(true);
        healthBarHideTimer = healthBarVisibleDuration;
    }

    void UpdateHealthBarUI()
    {
        if (healthBarFill == null)
            return;

        float normalized = maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
        healthBarFill.fillAmount = normalized;
    }

    public float GetCurrentAttackDamage()
    {
        float damage = basicDamage * GetDifficultyDamageMultiplier();

        if (archetype == EnemyArchetype.BladeRobot && inSpecial)
            damage *= 0.5f;

        return damage;
    }

    public void TakeDamage(float damage, bool headshot)
    {
        float finalDamage = Mathf.Max(0f, damage) * (headshot ? 1.8f : 1f);
        currentHealth -= finalDamage;
        survivalController?.AwardHitCoins();

        ShowHealthBar();
        UpdateHealthBarUI();

        if (currentHealth <= 0f)
            Destroy(gameObject);
    }

    void ApplyArchetypeDefaults()
    {
        switch (archetype)
        {
            case EnemyArchetype.Newborn:
                maxHealth = 45f; basicDamage = 10f; walkSpeed = 2.6f; runSpeed = 3.2f; break;
            case EnemyArchetype.NewbornV2:
                maxHealth = 80f; basicDamage = 10f; walkSpeed = 3.3f; runSpeed = 4f; break;
            case EnemyArchetype.SpiderRobot:
                maxHealth = 65f; basicDamage = 8f; walkSpeed = 3.6f; runSpeed = 4.2f; break;
            case EnemyArchetype.RocketeerNewborn:
                maxHealth = 80f; basicDamage = 20f; walkSpeed = 2.4f; runSpeed = 3f; specialInterval = 8f; break;
            case EnemyArchetype.ShadowNewborn:
                maxHealth = 100f; basicDamage = 10f; walkSpeed = 3.8f; runSpeed = 4.4f; specialInterval = 10f; break;
            case EnemyArchetype.Miniboss1:
                maxHealth = 400f; basicDamage = 20f; walkSpeed = 3.6f; runSpeed = 6.2f; specialInterval = 20f; break;
            case EnemyArchetype.BladeRobot:
                maxHealth = 180f; basicDamage = 10f; walkSpeed = 4.2f; runSpeed = 4.8f; specialInterval = 30f; break;
            case EnemyArchetype.Miniboss2:
                maxHealth = 500f; basicDamage = 20f; walkSpeed = 3.8f; runSpeed = 6.4f; specialInterval = 18f; break;
            case EnemyArchetype.BerserkerNewborn:
                maxHealth = 220f; basicDamage = 12f; walkSpeed = 4.4f; runSpeed = 5f; break;
            case EnemyArchetype.Miniboss3:
                maxHealth = 575f; basicDamage = 22f; walkSpeed = 4.1f; runSpeed = 6.8f; specialInterval = 16f; break;
        }
    }

    void ApplyDifficultyModifiers()
    {
        int difficulty = Mathf.Clamp(GameSettings.difficulty, 0, 3);
        float speedMultiplier = SpeedMultipliers[difficulty];

        walkSpeed *= speedMultiplier;
        runSpeed *= speedMultiplier;

        if (agent != null)
        {
            agent.speed = runSpeed;
            agent.stoppingDistance = attackRange;
        }
    }

    float GetDifficultyDamageMultiplier()
    {
        int difficulty = Mathf.Clamp(GameSettings.difficulty, 0, 3);
        return DamageMultipliers[difficulty];
    }

    void SetAlpha(float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            for (int m = 0; m < mats.Length; m++)
            {
                Color color = mats[m].color;
                color.a = alpha;
                mats[m].color = color;
            }
        }
    }
}
