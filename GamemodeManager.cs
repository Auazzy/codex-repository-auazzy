using UnityEngine;

public class GamemodeManager : MonoBehaviour
{
    public static GamemodeManager Instance;

    [Header("Gamemode Controllers")]
    public CollectController collectController;
    public SurvivalController survivalController;
    public SwarmController swarmController;
    public SurvivalAltController survivalAltController;
    public SandboxController sandboxController;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DisableAllGamemodes();
    }

    public void Start()
    {
        StartGamemode();
    }

    void DisableAllGamemodes()
    {
        collectController.enabled = false;
        survivalController.enabled = false;
        swarmController.enabled = false;
        survivalAltController.enabled = false;
        sandboxController.enabled = false;
    }

    public void StartGamemode()
    {
        switch (GameSettings.gamemode)
        {
            case 0:
                collectController.enabled = true;
                break;

            case 1:
                survivalController.enabled = true;
                break;

            case 2:
                swarmController.enabled = true;
                break;

            case 3:
                survivalAltController.enabled = true;
                break;

            case 4:
                sandboxController.enabled = true;
                break;
        }
    }
}
