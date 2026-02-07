using UnityEngine;

public class CustardPickup : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Collect
        if (GameSettings.gamemode == 0)
        {
            CollectController collect =
                FindObjectOfType<CollectController>();

            if (collect != null)
                collect.OnCustardCollected();
        }
        // Swarm
        else if (GameSettings.gamemode == 2)
        {
            SwarmController swarm =
                FindObjectOfType<SwarmController>();

            if (swarm != null)
                swarm.OnCustardCollected();
        }

        Destroy(gameObject);
    }
}
