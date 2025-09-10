using UnityEngine;
using UnityEngine.UI;

public class FollowWorldTargetUI : MonoBehaviour
{
    [Header("Target")]
    public Transform target;               // Player transform
    public bool autoFromSprite = true;     // auto from sprite height
    public float extraY = 0.2f;            // small offset above the head
    public Vector3 worldOffset = new Vector3(0f, 1.2f, 0f); // used if auto is off

    Camera cam;
    RectTransform rt;
    Canvas canvas;

    void Awake()
    {
        cam = Camera.main;
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void LateUpdate()
    {
        if (!target || !cam || !canvas || !rt) return;

        Vector3 offset = worldOffset;

        if (autoFromSprite)
        {
            var sr = target.GetComponentInChildren<SpriteRenderer>();
            if (sr)
                offset = new Vector3(0f, sr.bounds.extents.y + extraY, 0f);
        }

        Vector3 worldPos = target.position + offset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        // For Screen Space Overlay: position directly in pixel coordinates
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rt.position = screenPos;
        }
        else
        {
            // Safe conversion for Screen Space - Camera / World Space
            RectTransform canvasRect = canvas.transform as RectTransform;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
                out localPoint
            );
            rt.localPosition = localPoint;
        }
    }
}
