using System.Collections;
using UnityEngine;
using TMPro;

public class SurvivalAltController : MonoBehaviour
{
    // =========================
    // UI
    // =========================
    [Header("UI")]
    public GameObject uiRoot;
    public TMP_Text objectiveText;
    public TMP_Text timerText;

    // =========================
    // ENEMY
    // =========================
    [Header("Enemy")]
    public GameObject enemyPrefab;
    public Transform enemySpawnsParent;

    private GameObject spawnedEnemy;

    // =========================
    // TIMER
    // =========================
    private float timeSurvived;
    private bool running;

    private Coroutine objectiveRoutine;

    // =========================
    // SPAWN GUARD
    // =========================
    private static bool enemySpawned;

    // =========================
    // ABILITY FLAGS (prevents spam)
    // =========================
    private bool jumpUnlocked;
    private bool climbUnlocked;
    private bool teleportUnlocked;

    // =========================
    // INIT
    // =========================
    void OnEnable()
    {
        if (GameSettings.gamemode != 3)
        {
            if (uiRoot != null)
                uiRoot.SetActive(false);

            enabled = false;
            return;
        }

        if (uiRoot != null)
            uiRoot.SetActive(true);

        if (objectiveText != null)
            objectiveText.gameObject.SetActive(false);

        StartSurvival();
    }

    void StartSurvival()
    {
        SpawnEnemy();

        timeSurvived = 0f;
        running = true;

        jumpUnlocked = false;
        climbUnlocked = false;
        teleportUnlocked = false;

        StartCoroutine(DelayedObjective());
        UpdateTimerUI();
    }

    IEnumerator DelayedObjective()
    {
        yield return null;
        ShowObjective("Survive for as long as you can!");
    }

    // =========================
    // ENEMY SPAWN
    // =========================
    void SpawnEnemy()
    {
        if (enemySpawned)
            return;

        if (enemyPrefab == null || enemySpawnsParent == null)
        {
            Debug.LogWarning("SurvivalAltController: Enemy prefab or spawn parent not assigned.");
            return;
        }

        if (enemySpawnsParent.childCount == 0)
        {
            Debug.LogWarning("SurvivalAltController: No enemy spawn points found.");
            return;
        }

        Transform spawn =
            enemySpawnsParent.GetChild(Random.Range(0, enemySpawnsParent.childCount));

        spawnedEnemy = Instantiate(enemyPrefab, spawn.position, spawn.rotation);
        enemySpawned = true;
    }

    // =========================
    // UPDATE
    // =========================
    void Update()
    {
        if (!running)
            return;

        timeSurvived += Time.deltaTime;
        UpdateTimerUI();
        HandleAbilityUnlocks();
    }

    void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        int minutes = Mathf.FloorToInt(timeSurvived / 60f);
        int seconds = Mathf.FloorToInt(timeSurvived % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // =========================
    // ABILITY UNLOCK LOGIC
    // =========================
    void HandleAbilityUnlocks()
    {
        if (spawnedEnemy == null)
            return;

        SurvivalAltEnemyAI enemyAI =
            spawnedEnemy.GetComponent<SurvivalAltEnemyAI>();

        if (enemyAI == null)
            return;

        float minutes = timeSurvived / 60f;
        int difficulty = GameSettings.difficulty;
    }

    // =========================
    // OBJECTIVE UI
    // =========================
    void ShowObjective(string message)
    {
        if (objectiveRoutine != null)
            StopCoroutine(objectiveRoutine);

        objectiveRoutine = StartCoroutine(ObjectiveRoutine(message));
    }

    IEnumerator ObjectiveRoutine(string message)
    {
        if (objectiveText == null)
            yield break;

        objectiveText.text = message;
        objectiveText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3f);

        objectiveText.gameObject.SetActive(false);
    }

    // =========================
    // CLEANUP
    // =========================
    void OnDestroy()
    {
        enemySpawned = false;
    }
}