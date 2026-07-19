using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MobileGameplayFrameRenderer : MonoBehaviour
{
    private const float BorderWidth = 0.045f;
    private const int BorderSortingOrder = 1000;

    private Camera targetCamera;
    private LineRenderer lineRenderer;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        lineRenderer = gameObject.GetOrAddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.positionCount = 4;
        lineRenderer.widthMultiplier = BorderWidth;
        lineRenderer.numCapVertices = 0;
        lineRenderer.numCornerVertices = 0;
        lineRenderer.sortingOrder = BorderSortingOrder;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            return;

        Rect frame = Utils.GetMobileGameplayFrame(targetCamera);
        float z = 0f;

        lineRenderer.SetPosition(0, new Vector3(frame.xMin, frame.yMin, z));
        lineRenderer.SetPosition(1, new Vector3(frame.xMin, frame.yMax, z));
        lineRenderer.SetPosition(2, new Vector3(frame.xMax, frame.yMax, z));
        lineRenderer.SetPosition(3, new Vector3(frame.xMax, frame.yMin, z));
    }
}
