using UnityEngine;

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Death Visuals")]
    public Camera playerCamera;
    public GameObject redOverlay; // UI Image / Canvas (disabled by default)

    [Header("Disable On Death")]
    public MonoBehaviour[] scriptsToDisable; // movement, input, etc.

    [Header("Camera Drift")]
    public float driftSpeed = 2f;
    public float tiltSpeed = 2f;
    public float targetTiltX = 75f;

    private bool dead = false;
    private Transform cameraOriginalParent;

    void Awake()
    {
        if (playerCamera != null)
            cameraOriginalParent = playerCamera.transform.parent;

        if (redOverlay != null)
            redOverlay.SetActive(false);
    }

    void Update()
    {
        if (!dead || playerCamera == null)
            return;

        // ST2-style downward drift
        playerCamera.transform.position += Vector3.down * driftSpeed * Time.deltaTime;

        // Optional slow tilt downward (very subtle)
        Quaternion targetRot = Quaternion.Euler(
            targetTiltX,
            playerCamera.transform.eulerAngles.y,
            0f
        );

        playerCamera.transform.rotation = Quaternion.Lerp(
            playerCamera.transform.rotation,
            targetRot,
            tiltSpeed * Time.deltaTime
        );
    }

    public void KillPlayer()
    {
        if (dead)
            return;

        dead = true;
        Debug.Log("Player died");

        // Disable gameplay scripts
        foreach (var script in scriptsToDisable)
        {
            if (script != null)
                script.enabled = false;
        }

        // Detach camera
        if (playerCamera != null)
        {
            playerCamera.transform.SetParent(null);
        }

        // Red screen overlay
        if (redOverlay != null)
            redOverlay.SetActive(true);

        // Hand off to DeathManager
        DeathManager.Instance.HandlePlayerDeath(this);
    }

    public void Respawn(Vector3 position)
    {
        dead = false;

        // Reset position
        transform.position = position;

        // Re-enable scripts
        foreach (var script in scriptsToDisable)
        {
            if (script != null)
                script.enabled = true;
        }

        // Reattach camera
        if (playerCamera != null)
        {
            playerCamera.transform.SetParent(cameraOriginalParent);
            playerCamera.transform.localPosition = Vector3.zero;
            playerCamera.transform.localRotation = Quaternion.identity;
        }

        // Disable overlay
        if (redOverlay != null)
            redOverlay.SetActive(false);

        gameObject.SetActive(true);
    }
}
