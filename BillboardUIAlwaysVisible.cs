using UnityEngine;

public class BillboardUIAlwaysVisible : MonoBehaviour
{
    public Camera targetCamera;
    public bool forceCanvasOnTop = true;
    public int sortingOrder = 500;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (!forceCanvasOnTop)
            return;

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].overrideSorting = true;
            canvases[i].sortingOrder = sortingOrder;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = sortingOrder;
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        Vector3 direction = transform.position - targetCamera.transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }
}
