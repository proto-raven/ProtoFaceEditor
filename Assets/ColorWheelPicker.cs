using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorWheelPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public RawImage wheelImage;
    public ToastPixelEditor editor;
    public RecentColorSwatches swatchManager;
    public Slider brightnessSlider;

    private Texture2D wheelTexture;

    // Track last picked hue/saturation so brightness can adjust it
    private float lastHue = 0f;
    private float lastSat = 0f;
    private bool hasLastColor = false;

    private void Start()
    {
        if (wheelImage == null)
        {
            wheelImage = GetComponent<RawImage>();
        }

        GenerateWheelTexture(256); // 256x256 color wheel

        if (brightnessSlider != null)
        {
            if (brightnessSlider.value <= 0f)
                brightnessSlider.value = 1f; // default to full bright

            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }

        // Start with full brightness visual
        if (wheelImage != null)
        {
            wheelImage.color = Color.white;
        }
    }

    private void GenerateWheelTexture(int size)
    {
        wheelTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        wheelTexture.filterMode = FilterMode.Bilinear;
        wheelTexture.wrapMode = TextureWrapMode.Clamp;

        int half = size / 2;
        float radius = half;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - half;
                float dy = y - half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > radius)
                {
                    wheelTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
                else
                {
                    float angle = Mathf.Atan2(dy, dx); // -PI..PI
                    float hue = (angle / (2f * Mathf.PI)) + 0.5f; // 0..1
                    float sat = dist / radius;                  // 0..1
                    float val = 1f;                             // full brightness for display

                    Color rgb = Color.HSVToRGB(hue, sat, val);
                    wheelTexture.SetPixel(x, y, rgb);
                }
            }
        }

        wheelTexture.Apply();

        if (wheelImage != null)
        {
            wheelImage.texture = wheelTexture;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PickColor(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        PickColor(eventData);
    }

    private void PickColor(PointerEventData eventData)
    {
        if (wheelImage == null || wheelTexture == null)
            return;

        RectTransform rt = wheelImage.rectTransform;
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            return;
        }

        Vector2 size = rt.rect.size;
        Vector2 pivot = rt.pivot;

        float px = localPoint.x + size.x * pivot.x;
        float py = localPoint.y + size.y * pivot.y;

        float u = px / size.x;
        float v = py / size.y;

        if (u < 0f || u > 1f || v < 0f || v > 1f)
            return;

        // Compute position relative to center to derive hue/sat
        float cx = size.x * 0.5f;
        float cy = size.y * 0.5f;
        float dx = px - cx;
        float dy = py - cy;
        float radius = Mathf.Min(cx, cy);

        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        if (dist > radius)
        {
            // outside the wheel
            return;
        }

        // Hue & saturation from geometry (independent of brightness)
        float angle = Mathf.Atan2(dy, dx); // -PI..PI
        float hue = (angle / (2f * Mathf.PI)) + 0.5f; // 0..1
        float sat = dist / radius;                    // 0..1

        lastHue = hue;
        lastSat = sat;
        hasLastColor = true;

        float brightness = (brightnessSlider != null) ? brightnessSlider.value : 1f;
        Color final = Color.HSVToRGB(lastHue, lastSat, brightness);

        if (editor != null)
            editor.SetCurrentColor(final);

        if (swatchManager != null)
            swatchManager.OnColorPicked(final);
    }

    private void OnBrightnessChanged(float value)
    {
        // Visually dim/brighten the wheel itself
        if (wheelImage != null)
        {
            wheelImage.color = new Color(value, value, value, 1f);
        }

        if (!hasLastColor)
            return;

        // Recalculate brush color with same hue/sat but new brightness
        Color final = Color.HSVToRGB(lastHue, lastSat, value);

        if (editor != null)
            editor.SetCurrentColor(final);

        if (swatchManager != null)
            swatchManager.OnColorPicked(final);
    }
}
