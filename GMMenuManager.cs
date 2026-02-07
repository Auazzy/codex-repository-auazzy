using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MenuController : MonoBehaviour
{
    // =========================
    // MAP CATEGORIES
    // =========================
    [Header("Map Lists")]
    public GameObject mapListST7;
    public GameObject mapListST3;
    public GameObject mapListST2;

    // =========================
    // GAMEMODE
    // =========================
    [Header("Gamemode")]
    public TMP_Dropdown gamemodeDropdown;

    // =========================
    // SETTINGS PANELS
    // =========================
    [Header("Settings Panels")]
    public GameObject collectSettings;
    public GameObject survivalSettings;
    public GameObject swarmSettings;
    public GameObject survivalAltSettings;
    public GameObject sandboxSettings;

    // =========================
    // COLLECT / SWARM
    // =========================
    [Header("Collect / Swarm Sliders")]
    public Slider collectCustardsSlider;
    public TMP_Text collectCustardsValue;
    public Slider collectTimeLimitSlider;
    public TMP_Text collectTimeLimitValue;

    public Slider swarmCustardsSlider;
    public TMP_Text swarmCustardsValue;
    public Slider swarmTimeLimitSlider;
    public TMP_Text swarmTimeLimitValue;

    // =========================
    // DIFFICULTY (ALL MODES)
    // =========================
    [Header("Difficulty Sliders")]
    public Slider survivalDifficultySlider;
    public TMP_Text survivalDifficultyValue;

    public Slider survivalAltDifficultySlider;
    public TMP_Text survivalAltDifficultyValue;

    public Slider sandboxDifficultySlider;
    public TMP_Text sandboxDifficultyValue;

    readonly string[] difficultyNames = { "Easy", "Normal", "Hard", "Insane" };

    // =========================
    // MAP INFO
    // =========================
    [Header("Map Info")]
    public TMP_Text mapNameValue;
    public TMP_Text mapSizeValue;

    // =========================
    // START / BACK
    // =========================
    [Header("Start")]
    public TMP_Text startButtonText;
    public string mainMenuScene = "MainMenu";

    // =========================
    // INIT
    // =========================
    void Start()
    {
        ShowCategory(0);

        // Load from GameSettings
        SelectGamemode(GameSettings.gamemode);

        gamemodeDropdown.SetValueWithoutNotify(GameSettings.gamemode);

        collectCustardsSlider.SetValueWithoutNotify(GameSettings.custards);
        collectTimeLimitSlider.SetValueWithoutNotify(GameSettings.timeLimit);
        swarmCustardsSlider.SetValueWithoutNotify(GameSettings.custards);
        swarmTimeLimitSlider.SetValueWithoutNotify(GameSettings.timeLimit);

        survivalDifficultySlider.SetValueWithoutNotify(GameSettings.difficulty);
        survivalAltDifficultySlider.SetValueWithoutNotify(GameSettings.difficulty);
        sandboxDifficultySlider.SetValueWithoutNotify(GameSettings.difficulty);

        UpdateAllValues();
        UpdateMapInfoUI();
    }

    // =========================
    // MAP CATEGORY SWITCHING
    // =========================
    public void ShowCategory(int index)
    {
        mapListST7.SetActive(index == 0);
        mapListST3.SetActive(index == 1);
        mapListST2.SetActive(index == 2);
    }

    // =========================
    // GAMEMODE SWITCHING
    // =========================
    public void OnGamemodeDropdownChanged(int index)
    {
        GameSettings.gamemode = index;
        SelectGamemode(index);
    }


    void SelectGamemode(int index)
    {
        collectSettings.SetActive(index == 0);
        survivalSettings.SetActive(index == 1);
        swarmSettings.SetActive(index == 2);
        survivalAltSettings.SetActive(index == 3);
        sandboxSettings.SetActive(index == 4);
    }

    // =========================
    // COLLECT
    // =========================
    public void OnCollectCustardsChanged(float v)
    {
        int value = Mathf.RoundToInt(v);
        GameSettings.custards = value;
        collectCustardsValue.text = value.ToString();
    }

    public void OnCollectTimeLimitChanged(float v)
    {
        int value = Mathf.RoundToInt(v);
        GameSettings.timeLimit = value;
        collectTimeLimitValue.text = value.ToString();
    }


    // =========================
    // SWARM
    // =========================
    public void OnSwarmCustardsChanged(float v)
    {
        swarmCustardsValue.text = Mathf.RoundToInt(v).ToString();
        int value = Mathf.RoundToInt(v);
        GameSettings.custards = value;
    }

    public void OnSwarmTimeLimitChanged(float v)
    {
        swarmTimeLimitValue.text = Mathf.RoundToInt(v).ToString();
        int value = Mathf.RoundToInt(v);
        GameSettings.timeLimit = value;
    }

    // =========================
    // DIFFICULTY
    // =========================
    public void OnSurvivalDifficultyChanged(float v)
    {
        int i = Mathf.RoundToInt(v);
        GameSettings.difficulty = i;
        survivalDifficultyValue.text = difficultyNames[Mathf.RoundToInt(v)];
    }

    public void OnSurvivalAltDifficultyChanged(float v)
    {
        int i = Mathf.RoundToInt(v);
        GameSettings.difficulty = i;
        survivalAltDifficultyValue.text = difficultyNames[Mathf.RoundToInt(v)];
    }

    public void OnSandboxDifficultyChanged(float v)
    {
        int i = Mathf.RoundToInt(v);
        GameSettings.difficulty = i;
        sandboxDifficultyValue.text = difficultyNames[Mathf.RoundToInt(v)];
    }

    // =========================
    // MAP SELECTION (CALLED BY MAP BUTTONS)
    // =========================
    public void OnMapSelected(string mapName, string mapSize, string sceneName)
    {  
        GameSettings.mapName = mapName;
        GameSettings.mapSize = mapSize;
        GameSettings.mapScene = sceneName;
        UpdateMapInfoUI();
    }

    public void UpdateMapInfoUI()
    {
    mapNameValue.text = string.IsNullOrEmpty(GameSettings.mapName)
        ? "No map selected"
        : GameSettings.mapName;

    mapSizeValue.text = string.IsNullOrEmpty(GameSettings.mapSize)
        ? "-"
        : GameSettings.mapSize;
    }

    // =========================
    // START / BACK
    // =========================
    public void OnStartPressed()
    {
        if (string.IsNullOrEmpty(GameSettings.mapScene))
        {
            StartCoroutine(FlashStartTextRed());
            return;
        }

        SceneManager.LoadScene(GameSettings.mapScene);
    }

    IEnumerator FlashStartTextRed()
    {
        Color original = startButtonText.color;

        for (int i = 0; i < 3; i++)
        {
            startButtonText.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            startButtonText.color = original;
            yield return new WaitForSeconds(0.1f);
        }
    }

    void UpdateAllValues()
    {
        OnCollectCustardsChanged(collectCustardsSlider.value);
        OnCollectTimeLimitChanged(collectTimeLimitSlider.value);
        OnSwarmCustardsChanged(swarmCustardsSlider.value);
        OnSwarmTimeLimitChanged(swarmTimeLimitSlider.value);

        OnSurvivalDifficultyChanged(survivalDifficultySlider.value);
        OnSurvivalAltDifficultyChanged(survivalAltDifficultySlider.value);
        OnSandboxDifficultyChanged(sandboxDifficultySlider.value);
    }
}
