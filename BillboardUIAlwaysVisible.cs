using UnityEngine;
using UnityEngine.UI;

public class BillboardUIAlwaysVisible : MonoBehaviour
{
    public Camera targetCamera;
    public bool forceCanvasOnTop = true;
    public int sortingOrder = 500;

    private Canvas targetCanvas;

    void Awake()
    {
        targetCanvas = GetComponent<Canvas>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (forceCanvasOnTop && targetCanvas != null)
        {
            targetCanvas.overrideSorting = true;
            targetCanvas.sortingOrder = sortingOrder;
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
