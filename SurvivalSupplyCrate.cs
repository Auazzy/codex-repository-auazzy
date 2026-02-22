using UnityEngine;
using TMPro;

public class SurvivalSupplyCrate : MonoBehaviour
{
    [Header("UI")]
    public GameObject indicator;
    public TMP_Text promptText;

    [Header("Interaction")]
    public float interactionRange = 3f;
    public float holdDuration = 1.5f;
    public string promptMessage = "Hold E to buy supplies";

    private SurvivalController controller;
    private Transform player;
    private float holdTimer;
    private bool opened;

    void Awake()
    {
        if (promptText == null)
            promptText = GetComponentInChildren<TMP_Text>(true);

        if (indicator == null)
        {
            Transform indicatorChild = transform.Find("Indicator");
            if (indicatorChild != null)
                indicator = indicatorChild.gameObject;
        }

        if (indicator != null && indicator.GetComponent<BillboardUIAlwaysVisible>() == null)
            indicator.AddComponent<BillboardUIAlwaysVisible>();

        if (promptText != null && promptText.gameObject.GetComponent<BillboardUIAlwaysVisible>() == null)
            promptText.gameObject.AddComponent<BillboardUIAlwaysVisible>();

        SetPrompt(false);
    }

    public void Initialize(SurvivalController survivalController)
    {
        controller = survivalController;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (indicator != null)
            indicator.SetActive(true);

        holdTimer = 0f;
        opened = false;
        SetPrompt(false);
        UpdatePrompt();
    }

    void Update()
    {
        if (opened)
            return;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
            return;

        bool inRange = Vector3.Distance(player.position, transform.position) <= interactionRange;

        if (!inRange)
        {
            if (holdTimer > 0f)
            {
                holdTimer = 0f;
                UpdatePrompt();
            }

            SetPrompt(false);
            return;
        }

        SetPrompt(true);

        if (Input.GetKey(KeyCode.E))
        {
            holdTimer += Time.deltaTime;
            UpdatePrompt();

            if (holdTimer >= holdDuration)
            {
                opened = true;
                holdTimer = 0f;
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
        opened = false;
        holdTimer = 0f;
        UpdatePrompt();
    }
}
