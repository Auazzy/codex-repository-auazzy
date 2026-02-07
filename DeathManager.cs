using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathManager : MonoBehaviour
{
    public static DeathManager Instance;

    [Header("Sandbox Respawn")]
    public Transform sandboxRespawnPoint;
    public float sandboxRespawnDelay = 3f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void HandlePlayerDeath(PlayerDeathHandler player)
    {
        switch (GameSettings.gamemode)
        {
            // Collect, Survival, Swarm, SurvivalAlt
            case 0:
            case 1:
            case 2:
            case 3:
                StartCoroutine(ReturnToMenu());
                break;

            // Sandbox
            case 4:
                StartCoroutine(RespawnSandbox(player));
                break;
        }
    }

    IEnumerator ReturnToMenu()
    {
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene("MainMenu");
    }

    IEnumerator RespawnSandbox(PlayerDeathHandler player)
    {
        player.gameObject.SetActive(false);
        yield return new WaitForSeconds(sandboxRespawnDelay);

        if (sandboxRespawnPoint != null)
            player.Respawn(sandboxRespawnPoint.position);
        else
            Debug.LogWarning("Sandbox respawn point not assigned!");
    }
}
