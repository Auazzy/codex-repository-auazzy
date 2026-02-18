using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class SurvivalSupplyCrate : MonoBehaviour
{
    [Header("UI")]
    public GameObject indicator;
    public TMP_Text promptText;

    [Header("Interaction")]
    public float holdDuration = 1.5f;
    public string promptMessage = "Hold E to buy supplies";

    private SurvivalController controller;
    private bool playerInRange;
    private float holdTimer;
    private bool opened;

    public void Initialize(SurvivalController survivalController)
    {
        controller = survivalController;
        if (indicator != null)
            indicator.SetActive(true);

        SetPrompt(false);
    }

    void Update()
    {
        if (!playerInRange || opened)
            return;

        if (Input.GetKey(KeyCode.E))
        {
            holdTimer += Time.deltaTime;
            UpdatePrompt();

            if (holdTimer >= holdDuration)
            {
                opened = true;
                SetPrompt(false);
                controller?.OpenShop(this);
            }
        }
        else if (holdTimer > 0f)
        {
            holdTimer = 0f;
            UpdatePrompt();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
        UpdatePrompt();
        SetPrompt(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;
        holdTimer = 0f;
        SetPrompt(false);
    }

    void UpdatePrompt()
    {
        if (promptText == null)
            return;

        if (holdTimer <= 0f)
        {
            promptText.text = promptMessage;
            return;
        }

        float progress = Mathf.Clamp01(holdTimer / Mathf.Max(0.1f, holdDuration));
        int percent = Mathf.RoundToInt(progress * 100f);
        promptText.text = $"{promptMessage} ({percent}%)";
    }

    void SetPrompt(bool visible)
    {
        if (promptText != null)
            promptText.gameObject.SetActive(visible);
    }

    public void NotifyShopClosed()
    {
        if (!playerInRange)
            return;

        holdTimer = 0f;
        opened = false;
        UpdatePrompt();
        SetPrompt(true);
    }
}
