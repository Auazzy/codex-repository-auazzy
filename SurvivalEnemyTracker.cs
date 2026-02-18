using UnityEngine;

public class SurvivalEnemyTracker : MonoBehaviour
{
    private SurvivalController controller;
    private int killReward;
    private bool initialized;

    public void Initialize(SurvivalController survivalController, int reward)
    {
        controller = survivalController;
        killReward = reward;
        initialized = true;
    }

    void OnDestroy()
    {
        if (!initialized || controller == null)
            return;

        controller.OnEnemyDestroyed(killReward);
    }
}
