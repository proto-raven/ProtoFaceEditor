using UnityEngine;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(RectTransform))]
public class PixelZoomPan : MonoBehaviour
{
    [Header("Target")]
    public RectTransform target;          // The RectTransform of PixelDisplay

    [Tooltip("Any extra RectTransforms that should move/zoom with the main target (e.g. onion layers).")]
    public RectTransform[] extraTargets;  // e.g. OnionPrev, OnionNext

    [Header("Zoom Settings")]
    [Tooltip("Minimum integer zoom level (1 = 100%)")]
    public int minZoomStep = 1;

    [Tooltip("Maximum integer zoom level (16 = 1600%)")]
    public int maxZoomStep = 16;

    [Tooltip("Starting zoom level (integer scale)")]
    public int startZoomStep = 4;         // 400% by default

    [Tooltip("How many scroll notches per zoom step (1 = every notch changes zoom)")]
    public float scrollNotchesPerStep = 1f;

    [Header("Pan Settings")]
    [Tooltip("Hold this mouse button to pan (0 = left, 1 = right, 2 = middle)")]
    public int panMouseButton = 1;        // 1 = right mouse button

    [Tooltip("Pan speed multiplier")]
    public float panSpeed = 1f;

    [Header("Reset")]
    [Tooltip("Key to reset zoom & pan")]
    public KeyCode resetKey = KeyCode.R;

    private Canvas parentCanvas;
    private RectTransform parentRect;     // Workspace panel

    private int zoomStep;
    private float scrollAccumulator = 0f;

    private Vector2 defaultAnchoredPosition;
    private Vector3 defaultScale;

    private Vector2[] extraDefaultAnchored;
    private Vector3[] extraDefaultScale;

    private Vector2 lastMousePosition;
    private bool isPanning = false;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        parentCanvas = GetComponentInParent<Canvas>();
        parentRect = target.parent as RectTransform;

        zoomStep = Mathf.Clamp(startZoomStep, minZoomStep, maxZoomStep);

        defaultAnchoredPosition = target.anchoredPosition;
        defaultScale = target.localScale;

        if (extraTargets != null && extraTargets.Length > 0)
        {
            extraDefaultAnchored = new Vector2[extraTargets.Length];
            extraDefaultScale = new Vector3[extraTargets.Length];

            for (int i = 0; i < extraTargets.Length; i++)
            {
                if (extraTargets[i] == null) continue;
                extraDefaultAnchored[i] = extraTargets[i].anchoredPosition;
                extraDefaultScale[i] = extraTargets[i].localScale;
            }
        }

        ApplyZoom();
    }

    private void Update()
    {
        HandleZoom();
        HandlePan();
        HandleReset();
    }

    // ------------------------------
    // ZOOM
    // ------------------------------
    private void HandleZoom()
    {
        float scroll = 0f;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        scroll = mouse.scroll.ReadValue().y;
#else
        scroll = Input.mouseScrollDelta.y;
#endif

        if (Mathf.Approximately(scroll, 0f))
            return;

        scrollAccumulator += scroll;

        if (Mathf.Abs(scrollAccumulator) >= scrollNotchesPerStep)
        {
            int steps = (int)(scrollAccumulator / scrollNotchesPerStep);
            scrollAccumulator -= steps * scrollNotchesPerStep;

            zoomStep += steps;
            zoomStep = Mathf.Clamp(zoomStep, minZoomStep, maxZoomStep);

            ApplyZoom();
            ClampToWorkspace(true); // after zoom, ensure we’re still inside
        }
    }

    private void ApplyZoom()
    {
        float scale = Mathf.Max(1, zoomStep);
        Vector3 scaleVec = new Vector3(scale, scale, 1f);

        target.localScale = scaleVec;

        if (extraTargets != null)
        {
            for (int i = 0; i < extraTargets.Length; i++)
            {
                if (extraTargets[i] == null) continue;
                extraTargets[i].localScale = scaleVec;
            }
        }
    }

    // ------------------------------
    // PAN
    // ------------------------------
    private void HandlePan()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        bool buttonDown = false;
        bool buttonUp = false;
        bool buttonHeld = false;

        switch (panMouseButton)
        {
            case 0:
                buttonDown = mouse.leftButton.wasPressedThisFrame;
                buttonUp = mouse.leftButton.wasReleasedThisFrame;
                buttonHeld = mouse.leftButton.isPressed;
                break;
            case 1:
                buttonDown = mouse.rightButton.wasPressedThisFrame;
                buttonUp = mouse.rightButton.wasReleasedThisFrame;
                buttonHeld = mouse.rightButton.isPressed;
                break;
            case 2:
                buttonDown = mouse.middleButton.wasPressedThisFrame;
                buttonUp = mouse.middleButton.wasReleasedThisFrame;
                buttonHeld = mouse.middleButton.isPressed;
                break;
        }

        if (buttonDown)
        {
            isPanning = true;
            lastMousePosition = mouse.position.ReadValue();
        }

        if (buttonUp)
        {
            isPanning = false;
        }

        if (!isPanning || !buttonHeld)
            return;

        Vector2 currentMousePos = mouse.position.ReadValue();
#else
        if (Input.GetMouseButtonDown(panMouseButton))
        {
            isPanning = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(panMouseButton))
        {
            isPanning = false;
        }

        if (!isPanning)
            return;

        Vector2 currentMousePos = Input.mousePosition;
#endif

        Vector2 mouseDelta = currentMousePos - lastMousePosition;
        lastMousePosition = currentMousePos;

        float scaleFactor = (parentCanvas != null && parentCanvas.scaleFactor != 0f)
            ? parentCanvas.scaleFactor
            : 1f;

        Vector2 uiDelta = (mouseDelta / scaleFactor) * panSpeed;

        // Propose new position
        Vector2 proposed = target.anchoredPosition + uiDelta;

        // Clamp within workspace and get the actual delta we ended up applying
        Vector2 oldPos = target.anchoredPosition;
        Vector2 clamped = ClampPositionToWorkspace(proposed);
        Vector2 appliedDelta = clamped - oldPos;

        target.anchoredPosition = clamped;

        // Apply same delta to onion layers / extras
        if (extraTargets != null)
        {
            for (int i = 0; i < extraTargets.Length; i++)
            {
                if (extraTargets[i] == null) continue;
                extraTargets[i].anchoredPosition += appliedDelta;
            }
        }
    }

    // ------------------------------
    // RESET
    // ------------------------------
    private void HandleReset()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (resetKey == KeyCode.R && keyboard.rKey.wasPressedThisFrame)
            {
                ResetTransform();
            }
        }
#else
        if (Input.GetKeyDown(resetKey))
        {
            ResetTransform();
        }
#endif
    }

    private void ResetTransform()
    {
        zoomStep = Mathf.Clamp(startZoomStep, minZoomStep, maxZoomStep);

        target.localScale = defaultScale;
        target.anchoredPosition = defaultAnchoredPosition;

        if (extraTargets != null && extraDefaultAnchored != null && extraDefaultScale != null)
        {
            for (int i = 0; i < extraTargets.Length; i++)
            {
                if (extraTargets[i] == null) continue;
                extraTargets[i].localScale = extraDefaultScale[i];
                extraTargets[i].anchoredPosition = extraDefaultAnchored[i];
            }
        }

        ApplyZoom();
        ClampToWorkspace(true);
    }

    // ------------------------------
    // CLAMPING HELPERS
    // ------------------------------
    private void ClampToWorkspace(bool moveExtras)
    {
        Vector2 oldPos = target.anchoredPosition;
        Vector2 clamped = ClampPositionToWorkspace(oldPos);
        Vector2 delta = clamped - oldPos;

        target.anchoredPosition = clamped;

        if (moveExtras && extraTargets != null)
        {
            for (int i = 0; i < extraTargets.Length; i++)
            {
                if (extraTargets[i] == null) continue;
                extraTargets[i].anchoredPosition += delta;
            }
        }
    }

    private Vector2 ClampPositionToWorkspace(Vector2 proposed)
    {
        if (parentRect == null)
            return proposed;

        // We assume the defaultAnchoredPosition is the "centered" position.
        Vector2 offsetFromDefault = proposed - defaultAnchoredPosition;

        // Parent size (workspace panel)
        Vector2 parentSize = parentRect.rect.size;

        // Child size (pixel display), scaled
        Vector2 childSize = target.rect.size;
        Vector2 scaledChildSize = new Vector2(
            childSize.x * target.localScale.x,
            childSize.y * target.localScale.y
        );

        Vector2 clampedOffset = offsetFromDefault;

        // Horizontal clamp
        if (scaledChildSize.x <= parentSize.x)
        {
            // If the canvas is smaller than the workspace, keep it centered
            clampedOffset.x = 0f;
        }
        else
        {
            float maxOffsetX = (scaledChildSize.x - parentSize.x) * 0.5f;
            clampedOffset.x = Mathf.Clamp(offsetFromDefault.x, -maxOffsetX, maxOffsetX);
        }

        // Vertical clamp
        if (scaledChildSize.y <= parentSize.y)
        {
            clampedOffset.y = 0f;
        }
        else
        {
            float maxOffsetY = (scaledChildSize.y - parentSize.y) * 0.5f;
            clampedOffset.y = Mathf.Clamp(offsetFromDefault.y, -maxOffsetY, maxOffsetY);
        }

        return defaultAnchoredPosition + clampedOffset;
    }
}
