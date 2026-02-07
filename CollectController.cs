using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CollectController : MonoBehaviour
{
    // =========================
    // UI
    // =========================
    [Header("UI")]
    public GameObject collectUIRoot;
    public TMP_Text objectiveText;
    public TMP_Text timerText;

    // =========================
    // CUSTARDS
    // =========================
    [Header("Custards")]
    public Transform custardsParent;
    private int remainingCustards;

    // =========================
    // ENEMY
    // =========================
    [Header("Enemy")]
    public GameObject enemyPrefab;
    public Transform enemySpawnsParent; // EnemySpawns

    // =========================
    // TIMER
    // =========================
    private float timeRemaining;
    private bool timerRunning;

    private Coroutine objectiveRoutine;

    // =========================
    // SPAWN GUARD
    // =========================
    private static bool enemySpawned = false;

    // =========================
    // INIT
    // =========================
    void OnEnable()
    {
        // Collect ONLY
        if (GameSettings.gamemode != 0)
        {
            if (collectUIRoot != null)
                collectUIRoot.SetActive(false);

            enabled = false;
            return;
        }

        collectUIRoot.SetActive(true);
        objectiveText.gameObject.SetActive(false);

        StartCollect();
    }

    // =========================
    // START COLLECT
    // =========================
    void StartCollect()
    {
        EnableCustards();
        SpawnEnemy();
        StartTimer();

        ShowObjective($"Collect {remainingCustards} custards.");
    }

    void EnableCustards()
    {
        remainingCustards = 0;

        // Enable all custards first
        foreach (Transform c in custardsParent)
        {
            c.gameObject.SetActive(true);
            remainingCustards++;
        }

        // Clamp to menu-selected amount
        if (remainingCustards > GameSettings.custards)
        {
            for (int i = GameSettings.custards; i < custardsParent.childCount; i++)
                custardsParent.GetChild(i).gameObject.SetActive(false);

            remainingCustards = GameSettings.custards;
        }
    }

    void SpawnEnemy()
    {
        if (enemySpawned)
            return;

        if (enemyPrefab == null || enemySpawnsParent == null)
        {
            Debug.LogWarning("CollectController: Enemy prefab or EnemySpawns not assigned.");
            return;
        }

        Transform spawn = enemySpawnsParent.Find("EnemySpawn1");

        if (spawn == null)
        {
            Debug.LogWarning("CollectController: EnemySpawn1 not found.");
            return;
        }

        Instantiate(enemyPrefab, spawn.position, spawn.rotation);
        enemySpawned = true;
    }

    void StartTimer()
    {
        timeRemaining = GameSettings.timeLimit * 60f;
        timerRunning = true;
        UpdateTimerUI();
    }

    // =========================
    // UPDATE
    // =========================
    void Update()
    {
        if (!timerRunning)
            return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerRunning = false;
            StartCoroutine(GameOver());
        }

        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // =========================
    // CUSTARD CALLBACK
    // =========================
    public void OnCustardCollected()
    {
        remainingCustards--;

        if (remainingCustards <= 0)
        {
            remainingCustards = 0;
            timerRunning = false;
            ShowObjective("All custards collected!");
            StartCoroutine(ReturnToMenu());
        }
        else
        {
            ShowObjective($"{remainingCustards} custards remain.");
        }
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
        objectiveText.text = message;
        objectiveText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3f);

        objectiveText.gameObject.SetActive(false);
    }

    // =========================
    // END STATES
    // =========================
    IEnumerator GameOver()
    {
        ShowObjective("Game Over!");
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("MainMenu");
    }

    IEnumerator ReturnToMenu()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("MainMenu");
    }

    // =========================
    // CLEANUP
    // =========================
    void OnDestroy()
    {
        enemySpawned = false;
    }
}