using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SwarmController : MonoBehaviour
{
    // =========================
    // UI
    // =========================
    [Header("UI")]
    public GameObject swarmUIRoot;
    public TMP_Text objectiveText;
    public TMP_Text timerText;

    // =========================
    // CUSTARDS
    // =========================
    [Header("Custards")]
    public Transform custardsParent;
    private int remainingCustards;

    // =========================
    // ENEMIES
    // =========================
    [Header("Enemies")]
    public GameObject[] enemyPrefabs; // from Assets/Prefabs/Enemies
    public Transform enemySpawnsParent;

    private float enemySpawnTimer = 60f;

    // =========================
    // TIMER
    // =========================
    private float timeRemaining;
    private bool timerRunning;

    Coroutine objectiveRoutine;

    // =========================
    // INIT
    // =========================
    void OnEnable()
    {
        if (GameSettings.gamemode != 2)
        {
            swarmUIRoot.SetActive(false);
            enabled = false;
            return;
        }

        swarmUIRoot.SetActive(true);
        objectiveText.gameObject.SetActive(false);

        StartSwarm();
    }

    // =========================
    // START SWARM
    // =========================
    void StartSwarm()
    {
        EnableCustards();
        SpawnEnemy(); // initial enemy
        StartTimer();

        ShowObjective($"Collect {remainingCustards} custards before it's too late.");
    }

    void EnableCustards()
    {
        remainingCustards = 0;

        foreach (Transform c in custardsParent)
        {
            c.gameObject.SetActive(true);
            remainingCustards++;
        }

        if (remainingCustards > GameSettings.custards)
        {
            for (int i = GameSettings.custards; i < custardsParent.childCount; i++)
                custardsParent.GetChild(i).gameObject.SetActive(false);

            remainingCustards = GameSettings.custards;
        }
    }

    // =========================
    // UPDATE
    // =========================
    void Update()
    {
        if (!timerRunning)
            return;

        timeRemaining -= Time.deltaTime;
        enemySpawnTimer -= Time.deltaTime;

        if (enemySpawnTimer <= 0f)
        {
            SpawnEnemy();
            enemySpawnTimer = 60f;
        }

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerRunning = false;
            StartCoroutine(GameOver());
        }

        UpdateTimerUI();
    }

    // =========================
    // ENEMY SPAWNING
    // =========================
    void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0 || enemySpawnsParent == null)
            return;

        Transform spawn =
            enemySpawnsParent.GetChild(
                Random.Range(0, enemySpawnsParent.childCount));

        GameObject enemy =
            enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        Instantiate(enemy, spawn.position, spawn.rotation);
    }

    // =========================
    // TIMER
    // =========================
    void StartTimer()
    {
        timeRemaining = GameSettings.timeLimit * 60f;
        timerRunning = true;
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
}