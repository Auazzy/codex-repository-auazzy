using UnityEngine;

public class SurvivalSupplyCrate : MonoBehaviour
{
    [Header("UI")]
    public GameObject indicator;

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
        if (indicator == null)
        {
            Transform indicatorChild = transform.Find("Indicator");
            if (indicatorChild != null)
                indicator = indicatorChild.gameObject;
        }

        if (indicator != null && indicator.GetComponent<BillboardUIAlwaysVisible>() == null)
            indicator.AddComponent<BillboardUIAlwaysVisible>();
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
        controller?.SetCratePrompt(false, promptMessage);
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
                holdTimer = 0f;

            controller?.SetCratePrompt(false, promptMessage);
            return;
        }

        if (Input.GetKey(KeyCode.E))
        {
            holdTimer += Time.deltaTime;
            controller?.SetCratePrompt(true, GetPromptMessage());

            if (holdTimer >= holdDuration)
            {
                opened = true;
                holdTimer = 0f;
                controller?.SetCratePrompt(false, promptMessage);
                controller?.OpenShop(this);
            }
        }
        else
        {
            holdTimer = 0f;
            controller?.SetCratePrompt(true, promptMessage);
        }
    }

    string GetPromptMessage()
    {
        float progress = Mathf.Clamp01(holdTimer / Mathf.Max(0.1f, holdDuration));
        int percent = Mathf.RoundToInt(progress * 100f);
        return $"{promptMessage} ({percent}%)";
    }

    public void NotifyShopClosed()
    {
        opened = false;
        holdTimer = 0f;
        controller?.SetCratePrompt(false, promptMessage);
    }
}
