using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SurvivalController : MonoBehaviour
{
    [Serializable]
    public class WeaponOffer
    {
        public string weaponName;
        public int cost;
        public WeaponCategory category;
        public int magazineSize;
        public int reserveSize;
        public int ammoCostPerUnit;
    }

    public enum WeaponCategory
    {
        Pistol,
        Melee,
        Shotgun,
        MachinePistol,
        GrenadeLauncher,
        Revolver,
        SubmachineGun,
        SniperRifle,
        AssaultRifle,
        SupportMachineGun,
        RocketLauncher,
        FuelMelee
    }

    [Serializable]
    public class EnemySpawn
    {
        public string label;
        public GameObject prefab;
        public int count;
        public int killReward;
    }

    [Serializable]
    public class WaveDefinition
    {
        public string label;
        public List<EnemySpawn> spawns = new List<EnemySpawn>();
        public bool bossWave;
        public GameObject bossPrefab;
        public int bossKillReward;
    }

    // =========================
    // UI
    // =========================
    [Header("UI")]
    public GameObject survivalUIRoot;
    public TMP_Text objectiveText;
    public TMP_Text waveText;
    public TMP_Text coinsText;
    public TMP_Text intermissionText;
    public Image healthFillImage;
    public GameObject shopUIRoot;

    // =========================
    // SPAWNS
    // =========================
    [Header("Spawning")]
    public Transform enemySpawnsParent;
    public Transform crateSpawnsParent;
    public GameObject cratePrefab;

    // =========================
    // WAVES
    // =========================
    [Header("Waves")]
    public List<WaveDefinition> waves = new List<WaveDefinition>();
    public float intermissionDuration = 45f;

    // =========================
    // CURRENCY
    // =========================
    [Header("Currency")]
    public int hitCoinMin = 3;
    public int hitCoinMax = 5;

    // =========================
    // SHOP
    // =========================
    [Header("Shop")]
    public List<WeaponOffer> weaponOffers = new List<WeaponOffer>();

    // =========================
    // VICTORY
    // =========================
    [Header("Victory")]
    public GameObject confettiPrefab;

    private int currentWaveIndex = -1;
    private int aliveEnemies;
    private int coins;
    private float currentHealth = 100f;
    private float maxHealth = 100f;
    private bool waveRunning;
    private bool intermissionRunning;
    private float intermissionRemaining;
    private SurvivalSupplyCrate activeCrate;
    private Coroutine objectiveRoutine;

    // =========================
    // INIT
    // =========================
    void OnEnable()
    {
        if (GameSettings.gamemode != 1)
        {
            if (survivalUIRoot != null)
                survivalUIRoot.SetActive(false);

            enabled = false;
            return;
        }

        if (survivalUIRoot != null)
            survivalUIRoot.SetActive(true);

        if (shopUIRoot != null)
            shopUIRoot.SetActive(false);

        if (objectiveText != null)
            objectiveText.gameObject.SetActive(false);

        if (intermissionText != null)
            intermissionText.gameObject.SetActive(false);

        StartSurvival();
    }

    void StartSurvival()
    {
        coins = 0;
        UpdateCoinsUI();
        UpdateHealthUI();
        currentWaveIndex = -1;
        waveRunning = false;
        intermissionRunning = false;
        StartNextWave();
    }

    // =========================
    // UPDATE
    // =========================
    void Update()
    {
        if (intermissionRunning)
            UpdateIntermission();
    }

    void UpdateIntermission()
    {
        intermissionRemaining -= Time.deltaTime;
        if (intermissionRemaining <= 0f)
        {
            intermissionRemaining = 0f;
            intermissionRunning = false;
            HideIntermission();
            StartNextWave();
            return;
        }

        UpdateIntermissionUI();
    }

    // =========================
    // WAVES
    // =========================
    void StartNextWave()
    {
        if (currentWaveIndex + 1 >= waves.Count)
            return;

        currentWaveIndex++;
        WaveDefinition wave = waves[currentWaveIndex];

        waveRunning = true;
        aliveEnemies = 0;

        UpdateWaveUI();
        ShowObjective($"Wave {currentWaveIndex + 1} started.");

        SpawnWave(wave);
    }

    void SpawnWave(WaveDefinition wave)
    {
        if (wave == null)
            return;

        foreach (EnemySpawn spawn in wave.spawns)
            SpawnEnemies(spawn);

        if (wave.bossWave && wave.bossPrefab != null)
            SpawnBoss(wave.bossPrefab, wave.bossKillReward);

        if (aliveEnemies == 0)
            HandleWaveComplete();
    }

    void SpawnEnemies(EnemySpawn spawn)
    {
        if (spawn == null || spawn.prefab == null || enemySpawnsParent == null)
            return;

        if (enemySpawnsParent.childCount == 0)
            return;

        for (int i = 0; i < spawn.count; i++)
        {
            Transform spawnPoint = enemySpawnsParent.GetChild(
                UnityEngine.Random.Range(0, enemySpawnsParent.childCount));

            GameObject enemy = Instantiate(spawn.prefab, spawnPoint.position, spawnPoint.rotation);
            RegisterSpawnedEnemy(enemy, spawn.killReward);
        }
    }

    void SpawnBoss(GameObject bossPrefab, int killReward)
    {
        if (bossPrefab == null || enemySpawnsParent == null || enemySpawnsParent.childCount == 0)
            return;

        Transform spawnPoint = enemySpawnsParent.GetChild(
            UnityEngine.Random.Range(0, enemySpawnsParent.childCount));

        GameObject boss = Instantiate(bossPrefab, spawnPoint.position, spawnPoint.rotation);
        RegisterSpawnedEnemy(boss, killReward);
    }

    void RegisterSpawnedEnemy(GameObject enemy, int killReward)
    {
        if (enemy == null)
            return;

        aliveEnemies++;

        SurvivalEnemyTracker tracker = enemy.GetComponent<SurvivalEnemyTracker>();
        if (tracker == null)
            tracker = enemy.AddComponent<SurvivalEnemyTracker>();

        tracker.Initialize(this, killReward);
    }

    public void OnEnemyDestroyed(int killReward)
    {
        if (!waveRunning)
            return;

        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);

        if (killReward > 0)
            AddCoins(killReward);

        if (aliveEnemies == 0)
            HandleWaveComplete();
    }

    void HandleWaveComplete()
    {
        waveRunning = false;

        if (currentWaveIndex >= waves.Count - 1)
        {
            StartCoroutine(HandleVictory());
            return;
        }

        StartIntermission();
    }

    void StartIntermission()
    {
        intermissionRunning = true;
        intermissionRemaining = intermissionDuration;
        SpawnSupplyCrate();
        ShowObjective("Intermission started. Find the supply crate.");
        UpdateIntermissionUI();
        if (intermissionText != null)
            intermissionText.gameObject.SetActive(true);
    }

    void UpdateWaveUI()
    {
        if (waveText == null)
            return;

        string label = waves.Count > currentWaveIndex && currentWaveIndex >= 0
            ? waves[currentWaveIndex].label
            : "Wave";

        waveText.text = $"{label} (Wave {currentWaveIndex + 1}/{waves.Count})";
    }

    void UpdateIntermissionUI()
    {
        if (intermissionText == null)
            return;

        int seconds = Mathf.CeilToInt(intermissionRemaining);
        intermissionText.text = $"Next wave in {seconds}s";
    }

    void HideIntermission()
    {
        if (intermissionText != null)
            intermissionText.gameObject.SetActive(false);
    }

    // =========================
    // SUPPLY CRATE
    // =========================
    void SpawnSupplyCrate()
    {
        if (cratePrefab == null || crateSpawnsParent == null || crateSpawnsParent.childCount == 0)
        {
            Debug.LogWarning("SurvivalController: Supply crate prefab or spawn points missing.");
            return;
        }

        Transform spawn = crateSpawnsParent.GetChild(
            UnityEngine.Random.Range(0, crateSpawnsParent.childCount));

        GameObject crateObject = Instantiate(cratePrefab, spawn.position, spawn.rotation);
        activeCrate = crateObject.GetComponent<SurvivalSupplyCrate>();
        if (activeCrate == null)
            activeCrate = crateObject.AddComponent<SurvivalSupplyCrate>();

        activeCrate.Initialize(this);
    }

    public void OpenShop(SurvivalSupplyCrate crate)
    {
        if (shopUIRoot != null)
            shopUIRoot.SetActive(true);

        activeCrate = crate;
        ShowObjective("Supply crate opened.");
    }

    public void CloseShop()
    {
        if (shopUIRoot != null)
            shopUIRoot.SetActive(false);

        if (activeCrate != null)
            activeCrate.NotifyShopClosed();
    }

    public IReadOnlyList<WeaponOffer> GetWeaponOffers()
    {
        return weaponOffers;
    }

    // =========================
    // CURRENCY & HEALTH
    // =========================
    public void AddCoins(int amount)
    {
        coins = Mathf.Max(0, coins + amount);
        UpdateCoinsUI();
    }

    public void AwardHitCoins()
    {
        int amount = UnityEngine.Random.Range(hitCoinMin, hitCoinMax + 1);
        AddCoins(amount);
    }

    public bool TrySpendCoins(int amount)
    {
        if (coins < amount)
            return false;

        coins -= amount;
        UpdateCoinsUI();
        return true;
    }

    public void SetHealth(float current, float max)
    {
        maxHealth = Mathf.Max(1f, max);
        currentHealth = Mathf.Clamp(current, 0f, maxHealth);
        UpdateHealthUI();
    }

    public int GetHealCost(int desiredHeal, int costPerPoint)
    {
        int missing = Mathf.RoundToInt(maxHealth - currentHealth);
        int healAmount = Mathf.Clamp(desiredHeal, 0, missing);
        return healAmount * Mathf.Max(1, costPerPoint);
    }

    public int GetAmmoCost(WeaponOffer offer, int missingInMag, int missingInReserve)
    {
        if (offer == null)
            return 0;

        int missing = Mathf.Max(0, missingInMag + missingInReserve);
        int unitCost = Mathf.Max(1, offer.ammoCostPerUnit);
        return missing * unitCost;
    }

    void UpdateCoinsUI()
    {
        if (coinsText == null)
            return;

        coinsText.text = $"{coins}";
    }

    void UpdateHealthUI()
    {
        if (healthFillImage == null)
            return;

        healthFillImage.fillAmount = maxHealth <= 0f ? 0f : currentHealth / maxHealth;
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
    // VICTORY
    // =========================
    IEnumerator HandleVictory()
    {
        ShowObjective("Boss defeated!");

        if (confettiPrefab != null)
            Instantiate(confettiPrefab, Vector3.zero, Quaternion.identity);

        yield return new WaitForSeconds(10f);
        SceneManager.LoadScene("MainMenu");
    }

    // =========================
    // DEFAULT DATA
    // =========================
    void Reset()
    {
        waves = new List<WaveDefinition>
        {
            new WaveDefinition { label = "Newborns" },
            new WaveDefinition { label = "Newborns + Newborn V2" },
            new WaveDefinition { label = "Robot Spiders" },
            new WaveDefinition { label = "Rocketeer Newborns" },
            new WaveDefinition { label = "Shadow Newborns + Miniboss" },
            new WaveDefinition { label = "Blade Robots + Miniboss" },
            new WaveDefinition { label = "Berserker Newborns" },
            new WaveDefinition { label = "Miniboss Wave" },
            new WaveDefinition { label = "Endurance Wave" },
            new WaveDefinition { label = "Boss" , bossWave = true }
        };

        weaponOffers = new List<WeaponOffer>
        {
            new WeaponOffer { weaponName = "R-19", cost = 100, category = WeaponCategory.Pistol },
            new WeaponOffer { weaponName = "Knife", cost = 100, category = WeaponCategory.Melee },
            new WeaponOffer { weaponName = "R-19S", cost = 150, category = WeaponCategory.Pistol },
            new WeaponOffer { weaponName = "Machete", cost = 200, category = WeaponCategory.Melee },
            new WeaponOffer { weaponName = "Fireaxe", cost = 300, category = WeaponCategory.Melee },
            new WeaponOffer { weaponName = "M12C", cost = 500, category = WeaponCategory.Shotgun },
            new WeaponOffer { weaponName = "VZ-9", cost = 500, category = WeaponCategory.MachinePistol },
            new WeaponOffer { weaponName = "GL-6", cost = 625, category = WeaponCategory.GrenadeLauncher },
            new WeaponOffer { weaponName = "R44-C", cost = 700, category = WeaponCategory.Revolver },
            new WeaponOffer { weaponName = "M870A", cost = 750, category = WeaponCategory.Shotgun },
            new WeaponOffer { weaponName = "MP-5C", cost = 750, category = WeaponCategory.SubmachineGun },
            new WeaponOffer { weaponName = "Katana", cost = 850, category = WeaponCategory.Melee },
            new WeaponOffer { weaponName = "M40R", cost = 1000, category = WeaponCategory.SniperRifle },
            new WeaponOffer { weaponName = "AK-R", cost = 1250, category = WeaponCategory.AssaultRifle },
            new WeaponOffer { weaponName = "SPMG-249", cost = 1250, category = WeaponCategory.SupportMachineGun },
            new WeaponOffer { weaponName = "RPL-7", cost = 1250, category = WeaponCategory.RocketLauncher },
            new WeaponOffer { weaponName = "MK27R", cost = 1500, category = WeaponCategory.AssaultRifle },
            new WeaponOffer { weaponName = "SAW-12C", cost = 2000, category = WeaponCategory.FuelMelee }
        };
    }
}
