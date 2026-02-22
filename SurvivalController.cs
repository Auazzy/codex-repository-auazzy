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
        public int sellValue = 50;
    }

    [Serializable]
    public class WeaponVisualEntry
    {
        public string weaponName;
        public GameObject weaponPrefab;
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

    [Header("UI")]
    public GameObject survivalUIRoot;
    public TMP_Text objectiveText;
    public TMP_Text waveText;
    public TMP_Text enemiesLeftText;
    public TMP_Text coinsText;
    public TMP_Text intermissionText;
    public TMP_Text cratePromptText;
    public TMP_Text healthText;
    public Image healthFillImage;
    public Image damageFlashImage;
    public GameObject shopUIRoot;
    public Transform confettiUIParent;

    [Header("Spawning")]
    public Transform enemySpawnsParent;
    public Transform crateSpawnsParent;
    public GameObject cratePrefab;

    [Header("Waves")]
    public List<WaveDefinition> waves = new List<WaveDefinition>();
    public float intermissionDuration = 45f;

    [Header("Currency")]
    public int hitCoinMin = 3;
    public int hitCoinMax = 5;

    [Header("Shop")]
    public List<WeaponOffer> weaponOffers = new List<WeaponOffer>();

    [Header("Weapon Visuals")]
    public Transform weaponHoldPoint;
    public List<WeaponVisualEntry> weaponVisuals = new List<WeaponVisualEntry>();
    public SurvivalPlayerWeaponSystem playerWeaponSystem;

    [Header("Damage FX")]
    public Transform cameraShakeTarget;
    public float damageShakeDuration = 0.12f;
    public float damageShakeAmount = 0.08f;
    public float damageFlashFadeSpeed = 3.5f;
    public Color healthHighColor = new Color(0.2f, 1f, 0.2f);
    public Color healthLowColor = new Color(1f, 0.15f, 0.15f);

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip intermissionMusic;
    public List<AudioClip> waveMusic = new List<AudioClip>();

    [Header("Victory")]
    public GameObject confettiUIPrefab;

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
    private readonly List<string> ownedWeapons = new List<string>();
    private string equippedWeaponName;
    private GameObject equippedWeaponVisual;
    private float shakeTimer;
    private Vector3 cameraDefaultLocalPosition;
    private Coroutine damageFlashRoutine;
    private bool gameOverTriggered;

    public int Coins => coins;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsShopOpen => shopUIRoot != null && shopUIRoot.activeSelf;

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

        if (cratePromptText != null)
            cratePromptText.gameObject.SetActive(false);

        if (damageFlashImage != null)
        {
            Color flashColor = damageFlashImage.color;
            flashColor.a = 0f;
            damageFlashImage.color = flashColor;
        }

        if (cameraShakeTarget != null)
            cameraDefaultLocalPosition = cameraShakeTarget.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartSurvival();
    }

    void StartSurvival()
    {
        coins = 0;
        currentHealth = maxHealth;
        ownedWeapons.Clear();
        EquipStarterLoadout();

        UpdateCoinsUI();
        UpdateHealthUI();
        currentWaveIndex = -1;
        waveRunning = false;
        intermissionRunning = false;
        gameOverTriggered = false;
        UpdateEnemyCounterUI();
        StartNextWave();
    }

    void Update()
    {
        if (intermissionRunning)
            UpdateIntermission();

        UpdateDamageShake();
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

    void StartNextWave()
    {
        if (currentWaveIndex + 1 >= waves.Count)
            return;

        currentWaveIndex++;
        WaveDefinition wave = waves[currentWaveIndex];

        waveRunning = true;
        aliveEnemies = 0;

        if (waveText != null)
            waveText.gameObject.SetActive(true);

        if (enemiesLeftText != null)
            enemiesLeftText.gameObject.SetActive(true);

        UpdateWaveUI();
        ShowObjective($"Wave {currentWaveIndex + 1} started.");
        PlayWaveMusic(currentWaveIndex);
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
            Transform spawnPoint = enemySpawnsParent.GetChild(UnityEngine.Random.Range(0, enemySpawnsParent.childCount));
            GameObject enemy = Instantiate(spawn.prefab, spawnPoint.position, spawnPoint.rotation);
            RegisterSpawnedEnemy(enemy, spawn.killReward);
        }
    }

    void SpawnBoss(GameObject bossPrefab, int killReward)
    {
        if (bossPrefab == null || enemySpawnsParent == null || enemySpawnsParent.childCount == 0)
            return;

        Transform spawnPoint = enemySpawnsParent.GetChild(UnityEngine.Random.Range(0, enemySpawnsParent.childCount));
        GameObject boss = Instantiate(bossPrefab, spawnPoint.position, spawnPoint.rotation);
        RegisterSpawnedEnemy(boss, killReward);
    }

    void RegisterSpawnedEnemy(GameObject enemy, int killReward)
    {
        if (enemy == null)
            return;

        aliveEnemies++;
        UpdateEnemyCounterUI();

        SurvivalEnemyTracker tracker = enemy.GetComponent<SurvivalEnemyTracker>();
        if (tracker == null)
            tracker = enemy.AddComponent<SurvivalEnemyTracker>();

        tracker.Initialize(this, killReward);

        SurvivalEnemyAI ai = enemy.GetComponent<SurvivalEnemyAI>();
        if (ai != null)
            ai.SetSurvivalController(this);
    }

    public void OnEnemyDestroyed(int killReward)
    {
        if (!waveRunning)
            return;

        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        UpdateEnemyCounterUI();

        if (killReward > 0)
            AddCoins(killReward);

        if (aliveEnemies == 0)
            HandleWaveComplete();
    }

    void HandleWaveComplete()
    {
        waveRunning = false;
        aliveEnemies = 0;
        UpdateEnemyCounterUI();

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

        if (waveText != null)
            waveText.gameObject.SetActive(false);

        if (enemiesLeftText != null)
            enemiesLeftText.gameObject.SetActive(false);

        SpawnSupplyCrate();
        PlayIntermissionMusic();
        ShowObjective("Intermission started. Find the supply crate.");
        UpdateIntermissionUI();
        if (intermissionText != null)
            intermissionText.gameObject.SetActive(true);
    }

    void SpawnSupplyCrate()
    {
        if (cratePrefab == null || crateSpawnsParent == null || crateSpawnsParent.childCount == 0)
        {
            Debug.LogWarning("SurvivalController: Supply crate prefab or spawn points missing.");
            return;
        }

        Transform spawn = crateSpawnsParent.GetChild(UnityEngine.Random.Range(0, crateSpawnsParent.childCount));

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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetCratePrompt(false, string.Empty);
        activeCrate = crate;
        ShowObjective("Supply crate opened.");
    }

    public void CloseShop()
    {
        if (shopUIRoot != null)
            shopUIRoot.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (activeCrate != null)
            activeCrate.NotifyShopClosed();
    }

    public void SetCratePrompt(bool visible, string message)
    {
        if (cratePromptText == null)
            return;

        cratePromptText.gameObject.SetActive(visible);
        cratePromptText.text = message;
    }

    public IReadOnlyList<WeaponOffer> GetWeaponOffers()
    {
        return weaponOffers;
    }
    public IReadOnlyList<string> GetOwnedWeapons()
    {
        return ownedWeapons;
    }

    public bool IsWeaponOwned(string weaponName)
    {
        return ownedWeapons.Contains(weaponName);
    }

    public string GetEquippedWeaponName()
    {
        return equippedWeaponName;
    }

    public void EquipOwnedWeaponByIndex(int index)
    {
        if (ownedWeapons.Count == 0)
            return;

        int wrapped = ((index % ownedWeapons.Count) + ownedWeapons.Count) % ownedWeapons.Count;
        EquipWeapon(ownedWeapons[wrapped]);
    }

    public void EquipNextOwnedWeapon()
    {
        if (ownedWeapons.Count == 0)
            return;

        int current = ownedWeapons.IndexOf(equippedWeaponName);
        int next = current < 0 ? 0 : (current + 1) % ownedWeapons.Count;
        EquipWeapon(ownedWeapons[next]);
    }

    public void EquipPreviousOwnedWeapon()
    {
        if (ownedWeapons.Count == 0)
            return;

        int current = ownedWeapons.IndexOf(equippedWeaponName);
        int previous = current < 0 ? 0 : (current - 1 + ownedWeapons.Count) % ownedWeapons.Count;
        EquipWeapon(ownedWeapons[previous]);
    }


    public bool BuyWeapon(string weaponName)
    {
        WeaponOffer offer = GetOffer(weaponName);
        if (offer == null)
            return false;

        if (!ownedWeapons.Contains(weaponName))
        {
            if (!TrySpendCoins(offer.cost))
                return false;

            ownedWeapons.Add(weaponName);
        }

        EquipWeapon(weaponName);
        return true;
    }

    public bool SellWeapon(string weaponName)
    {
        if (weaponName == "R-19" || weaponName == "Knife")
            return false;

        WeaponOffer offer = GetOffer(weaponName);
        if (offer == null || !ownedWeapons.Contains(weaponName))
            return false;

        ownedWeapons.Remove(weaponName);
        AddCoins(Mathf.Max(0, offer.sellValue));

        if (equippedWeaponName == weaponName)
            EquipWeapon(ownedWeapons.Count > 0 ? ownedWeapons[0] : "R-19");

        return true;
    }

    public void EquipWeapon(string weaponName)
    {
        equippedWeaponName = weaponName;
        RefreshWeaponVisual();

        if (playerWeaponSystem != null)
            playerWeaponSystem.OnWeaponEquipped(weaponName, GetOffer(weaponName));
    }

    void EquipStarterLoadout()
    {
        if (!ownedWeapons.Contains("R-19"))
            ownedWeapons.Add("R-19");

        if (!ownedWeapons.Contains("Knife"))
            ownedWeapons.Add("Knife");

        EquipWeapon("R-19");
    }

    void RefreshWeaponVisual()
    {
        if (equippedWeaponVisual != null)
            Destroy(equippedWeaponVisual);

        if (weaponHoldPoint == null)
            return;

        WeaponVisualEntry entry = weaponVisuals.Find(v => v.weaponName == equippedWeaponName);
        if (entry == null || entry.weaponPrefab == null)
            return;

        equippedWeaponVisual = Instantiate(entry.weaponPrefab, weaponHoldPoint);
        equippedWeaponVisual.transform.localPosition = Vector3.zero;
        equippedWeaponVisual.transform.localRotation = Quaternion.identity;
    }

    WeaponOffer GetOffer(string weaponName)
    {
        return weaponOffers.Find(o => o.weaponName == weaponName);
    }

    void PlayWaveMusic(int waveIndex)
    {
        if (musicSource == null || waveMusic.Count == 0)
            return;

        AudioClip clip = waveMusic[Mathf.Clamp(waveIndex, 0, waveMusic.Count - 1)];
        if (clip == null)
            return;

        musicSource.loop = true;
        musicSource.clip = clip;
        musicSource.Play();
    }

    void PlayIntermissionMusic()
    {
        if (musicSource == null || intermissionMusic == null)
            return;

        musicSource.loop = true;
        musicSource.clip = intermissionMusic;
        musicSource.Play();
    }

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

    public void DamagePlayer(float amount)
    {
        if (amount <= 0f || gameOverTriggered)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, amount));
        UpdateHealthUI();
        PlayDamageFeedback();

        if (currentHealth <= 0f)
        {
            gameOverTriggered = true;
            StartCoroutine(GameOverRoutine());
        }
    }

    public int HealMissingHealth(int costPerHealthPoint)
    {
        int missing = Mathf.RoundToInt(maxHealth - currentHealth);
        if (missing <= 0)
            return 0;

        int totalCost = missing * Mathf.Max(1, costPerHealthPoint);
        if (!TrySpendCoins(totalCost))
            return 0;

        currentHealth = maxHealth;
        UpdateHealthUI();
        return totalCost;
    }

    public bool TryBuyAmmo(string weaponName, int missingInMag, int missingInReserve)
    {
        WeaponOffer offer = GetOffer(weaponName);
        if (offer == null)
            return false;

        int cost = GetAmmoCost(offer, missingInMag, missingInReserve);
        return TrySpendCoins(cost);
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
        float normalized = maxHealth <= 0f ? 0f : currentHealth / maxHealth;

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = normalized;
            healthFillImage.color = Color.Lerp(healthLowColor, healthHighColor, normalized);
        }

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
    }

    void UpdateWaveUI()
    {
        if (waveText == null)
            return;

        waveText.text = $"Wave: {currentWaveIndex + 1}";
    }

    void UpdateEnemyCounterUI()
    {
        if (enemiesLeftText == null)
            return;

        enemiesLeftText.text = $"Enemies left: {Mathf.Max(0, aliveEnemies)}";
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


    void UpdateDamageShake()
    {
        if (cameraShakeTarget == null)
            return;

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            Vector3 offset = UnityEngine.Random.insideUnitSphere * damageShakeAmount;
            cameraShakeTarget.localPosition = cameraDefaultLocalPosition + offset;
            return;
        }

        cameraShakeTarget.localPosition = Vector3.Lerp(
            cameraShakeTarget.localPosition,
            cameraDefaultLocalPosition,
            Time.deltaTime * 16f);
    }

    void PlayDamageFeedback()
    {
        shakeTimer = damageShakeDuration;

        if (damageFlashImage == null)
            return;

        Color flashColor = damageFlashImage.color;
        flashColor.a = 0.7f;
        damageFlashImage.color = flashColor;

        if (damageFlashRoutine != null)
            StopCoroutine(damageFlashRoutine);

        damageFlashRoutine = StartCoroutine(FadeDamageFlash());
    }

    IEnumerator FadeDamageFlash()
    {
        if (damageFlashImage == null)
            yield break;

        while (damageFlashImage.color.a > 0.01f)
        {
            Color flashColor = damageFlashImage.color;
            flashColor.a = Mathf.MoveTowards(flashColor.a, 0f, Time.deltaTime * damageFlashFadeSpeed);
            damageFlashImage.color = flashColor;
            yield return null;
        }

        Color clearColor = damageFlashImage.color;
        clearColor.a = 0f;
        damageFlashImage.color = clearColor;
    }

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

    IEnumerator HandleVictory()
    {
        ShowObjective("Boss defeated!");

        if (confettiUIPrefab != null)
        {
            if (confettiUIParent != null)
                Instantiate(confettiUIPrefab, confettiUIParent, false);
            else
                Instantiate(confettiUIPrefab);
        }

        if (musicSource != null)
            musicSource.Stop();

        yield return new WaitForSeconds(10f);
        SceneManager.LoadScene("MainMenu");
    }

    IEnumerator GameOverRoutine()
    {
        ShowObjective("You died!");
        if (musicSource != null)
            musicSource.Stop();

        yield return new WaitForSeconds(2.5f);
        SceneManager.LoadScene("MainMenu");
    }

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
            new WaveDefinition { label = "Boss", bossWave = true }
        };

        weaponOffers = new List<WeaponOffer>
        {
            new WeaponOffer { weaponName = "R-19", cost = 100, category = WeaponCategory.Pistol, ammoCostPerUnit = 1 },
            new WeaponOffer { weaponName = "Knife", cost = 100, category = WeaponCategory.Melee, ammoCostPerUnit = 0 },
            new WeaponOffer { weaponName = "R-19S", cost = 150, category = WeaponCategory.Pistol, ammoCostPerUnit = 1 },
            new WeaponOffer { weaponName = "Machete", cost = 200, category = WeaponCategory.Melee, ammoCostPerUnit = 0 },
            new WeaponOffer { weaponName = "Fireaxe", cost = 300, category = WeaponCategory.Melee, ammoCostPerUnit = 0 },
            new WeaponOffer { weaponName = "M12C", cost = 500, category = WeaponCategory.Shotgun, ammoCostPerUnit = 4 },
            new WeaponOffer { weaponName = "VZ-9", cost = 500, category = WeaponCategory.MachinePistol, ammoCostPerUnit = 2 },
            new WeaponOffer { weaponName = "GL-6", cost = 625, category = WeaponCategory.GrenadeLauncher, ammoCostPerUnit = 6 },
            new WeaponOffer { weaponName = "R44-C", cost = 700, category = WeaponCategory.Revolver, ammoCostPerUnit = 3 },
            new WeaponOffer { weaponName = "M870A", cost = 750, category = WeaponCategory.Shotgun, ammoCostPerUnit = 4 },
            new WeaponOffer { weaponName = "MP-5C", cost = 750, category = WeaponCategory.SubmachineGun, ammoCostPerUnit = 2 },
            new WeaponOffer { weaponName = "Katana", cost = 850, category = WeaponCategory.Melee, ammoCostPerUnit = 0 },
            new WeaponOffer { weaponName = "M40R", cost = 1000, category = WeaponCategory.SniperRifle, ammoCostPerUnit = 4 },
            new WeaponOffer { weaponName = "AK-R", cost = 1250, category = WeaponCategory.AssaultRifle, ammoCostPerUnit = 3 },
            new WeaponOffer { weaponName = "SPMG-249", cost = 1250, category = WeaponCategory.SupportMachineGun, ammoCostPerUnit = 3 },
            new WeaponOffer { weaponName = "RPL-7", cost = 1250, category = WeaponCategory.RocketLauncher, ammoCostPerUnit = 8 },
            new WeaponOffer { weaponName = "MK27R", cost = 1500, category = WeaponCategory.AssaultRifle, ammoCostPerUnit = 4 },
            new WeaponOffer { weaponName = "SAW-12C", cost = 2000, category = WeaponCategory.FuelMelee, ammoCostPerUnit = 5 }
        };
    }
}
