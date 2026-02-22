using UnityEngine;
using UnityEngine.UI;

public class BillboardUIAlwaysVisible : MonoBehaviour
{
    public Camera targetCamera;
    public bool forceCanvasOnTop = true;
    public int sortingOrder = 500;
    public bool keepConstantScreenSize = true;
    public float baseDistance = 12f;

    private Vector3 initialScale;

    void Awake()
    {
        initialScale = transform.localScale;

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
            Material material = renderers[i].material;
            if (material != null && material.HasProperty("_ZTest"))
                material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Material material = graphics[i].material;
            if (material != null && material.HasProperty("_ZTest"))
                material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
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

        if (!keepConstantScreenSize)
            return;

        float distance = direction.magnitude;
        float scaleFactor = distance / Mathf.Max(0.1f, baseDistance);
        transform.localScale = initialScale * scaleFactor;
    }
}
