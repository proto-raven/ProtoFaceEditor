using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToastPixelEditor : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    // Resolution of the visor
    public const int WIDTH = 128;
    public const int HEIGHT = 32;

    [Header("UI")]
    public RawImage pixelDisplay;   // Main drawing canvas
    public Slider fpsSlider;        // Playback speed slider
    public Toggle mirrorXToggle;    // Horizontal mirroring
    public Toggle eraserToggle;     // Eraser mode toggle
    public Text frameLabel;         // Displays frame count

    [Header("Playback Controls")]
    public Button playPauseButton;                // The button in the top bar
    public TextMeshProUGUI playPauseLabel;     // Text (TMP) child of BtnPlayPause

    [Header("Onion Skin")]
    public RawImage onionPrev;      // Semi-transparent overlay of the previous frame
    private bool onionEnabled = true; // Always on for now

    [Header("Drawing Settings")]
    public Color currentColor = new Color(1f, 0.5f, 0f, 1f); // Toast-orange
    public Color backgroundColor = Color.black;

    [Header("Color System")]
    public RecentColorSwatches swatchManager;  // Notified when a color is actually used

    [Header("Thumbnails")]
    public FrameThumbnailStrip thumbnailStrip;

    [Header("Grid Overlay")]
    public PixelGridOverlay gridOverlay;

    private List<Texture2D> frames = new List<Texture2D>();
    private int currentFrameIndex = 0;

    private bool isPlaying = false;
    private float frameTimer = 0f;

    // ---------------------------------------------------------
    // INITIALIZATION
    // ---------------------------------------------------------
    private void Start()
    {
        if (pixelDisplay == null)
            pixelDisplay = GetComponent<RawImage>();

        if (frames.Count == 0)
            frames.Add(CreateBlankFrame());

        pixelDisplay.texture = frames[currentFrameIndex];
        pixelDisplay.texture.filterMode = FilterMode.Point;
        pixelDisplay.color = Color.white;

        UpdateFrameLabel();
        UpdateOnionLayers();
        UpdateThumbnails();
        if (gridOverlay != null) gridOverlay.SyncToCurrentFrame();
    }

    // ---------------------------------------------------------
    // UPDATE LOOP (Playback)
    // ---------------------------------------------------------
    private void Update()
    {
        if (isPlaying && frames.Count > 0)
        {
            float fps = (fpsSlider != null) ? Mathf.Max(1f, fpsSlider.value) : 10f;
            float frameDuration = 1f / fps;

            frameTimer += Time.deltaTime;

            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % frames.Count;

                pixelDisplay.texture = frames[currentFrameIndex];
                UpdateFrameLabel();
                UpdateOnionLayers();
                UpdateThumbnailsHighlight();
            }
        }
    }

    // ---------------------------------------------------------
    // FRAME CREATION
    // ---------------------------------------------------------
    private Texture2D CreateBlankFrame()
    {
        Texture2D tex = new Texture2D(WIDTH, HEIGHT, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[WIDTH * HEIGHT];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = backgroundColor;

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private Texture2D DuplicateFrame(Texture2D src)
    {
        Texture2D tex = new Texture2D(WIDTH, HEIGHT, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(src.GetPixels());
        tex.Apply();
        return tex;
    }

    // ---------------------------------------------------------
    // ONION SKIN OVERLAY
    // ---------------------------------------------------------
    private void UpdateOnionLayers()
    {
        if (onionPrev == null)
            return;

        if (!onionEnabled || isPlaying || frames.Count <= 1)
        {
            onionPrev.enabled = false;
            onionPrev.texture = null;
            return;
        }

        int prevIndex = currentFrameIndex - 1;
        if (prevIndex < 0)
            prevIndex = frames.Count - 1;

        Texture2D prevTex = frames[prevIndex];

        onionPrev.texture = prevTex;
        onionPrev.enabled = true;

        Color c = onionPrev.color;
        c.a = 0.25f;
        onionPrev.color = c;
    }

    // ---------------------------------------------------------
    // DRAWING LOGIC
    // ---------------------------------------------------------
    public void OnPointerDown(PointerEventData eventData)
    {
        // Only draw on left click
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!isPlaying)
            DrawAtPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Only draw on left click
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!isPlaying)
            DrawAtPointer(eventData);
    }

    private void DrawAtPointer(PointerEventData eventData)
    {
        if (pixelDisplay == null || frames.Count == 0)
            return;

        RectTransform rt = pixelDisplay.rectTransform;

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

        int x = Mathf.FloorToInt(u * WIDTH);
        int y = Mathf.FloorToInt(v * HEIGHT);

        x = Mathf.Clamp(x, 0, WIDTH - 1);
        y = Mathf.Clamp(y, 0, HEIGHT - 1);

        // Determine if we're erasing or drawing
        bool erasing = (eraserToggle != null && eraserToggle.isOn);
        Color drawColor = erasing ? backgroundColor : currentColor;

        // Draw main pixel
        SetPixel(x, y, drawColor);

        // Mirroring
        if (mirrorXToggle != null && mirrorXToggle.isOn)
        {
            int mx = WIDTH - 1 - x;
            if (mx != x)
                SetPixel(mx, y, drawColor);
        }

        // Only track colors as "used" when actually drawing, not erasing
        if (!erasing && swatchManager != null)
        {
            swatchManager.OnColorUsed(currentColor);
        }

        frames[currentFrameIndex].Apply();
        UpdateOnionLayers();
        if (gridOverlay != null) gridOverlay.SyncToCurrentFrame();
        // Thumbnails share the same textures; they update automatically visually.
    }

    private void SetPixel(int x, int y, Color col)
    {
        Texture2D tex = frames[currentFrameIndex];
        tex.SetPixel(x, y, col);
    }

    // ---------------------------------------------------------
    // FRAME CONTROLS
    // ---------------------------------------------------------
    public void NextFrame()
    {
        if (frames.Count == 0) return;
        currentFrameIndex = (currentFrameIndex + 1) % frames.Count;
        pixelDisplay.texture = frames[currentFrameIndex];
        UpdateFrameLabel();
        UpdateOnionLayers();
        UpdateThumbnailsHighlight();
        if (gridOverlay != null) gridOverlay.SyncToCurrentFrame();

    }

    public void PrevFrame()
    {
        if (frames.Count == 0) return;
        currentFrameIndex--;
        if (currentFrameIndex < 0)
            currentFrameIndex = frames.Count - 1;

        pixelDisplay.texture = frames[currentFrameIndex];
        UpdateFrameLabel();
        UpdateOnionLayers();
        UpdateThumbnailsHighlight();
        if (gridOverlay != null) gridOverlay.SyncToCurrentFrame();

    }

    public void NewFrame()
    {
        Texture2D tex = CreateBlankFrame();
        frames.Add(tex);
        currentFrameIndex = frames.Count - 1;

        pixelDisplay.texture = tex;
        UpdateFrameLabel();
        UpdateOnionLayers();
        UpdateThumbnails();
        if (gridOverlay != null) gridOverlay.SyncToCurrentFrame();

    }

    public void DuplicateCurrentFrame()
    {
        Texture2D copy = DuplicateFrame(frames[currentFrameIndex]);
        frames.Add(copy);
        currentFrameIndex = frames.Count - 1;

        pixelDisplay.texture = copy;
        UpdateFrameLabel();
        UpdateOnionLayers();
        UpdateThumbnails();
        if (gridOverlay != null) gridOverlay.SyncToCurrentFrame();

    }

    public void DeleteCurrentFrame()
    {
        if (frames.Count <= 1) return;

        frames.RemoveAt(currentFrameIndex);
        if (currentFrameIndex >= frames.Count)
            currentFrameIndex = frames.Count - 1;

        pixelDisplay.texture = frames[currentFrameIndex];
        UpdateFrameLabel();
        UpdateOnionLayers();
        UpdateThumbnails();
        if (gridOverlay != null) gridOverlay.SyncToCurrentFrame();

    }

    public void TogglePlay()
    {
        isPlaying = !isPlaying;
        frameTimer = 0f;
        UpdateOnionLayers();
        UpdateThumbnailsHighlight();
        if (gridOverlay != null) gridOverlay.SyncToCurrentFrame();


        // Update button label
        if (playPauseLabel != null)
        {
            playPauseLabel.text = isPlaying ? "Pause" : "Play";
        }

        // Optional: tweak button color slightly when playing
        if (playPauseButton != null)
        {
            var colors = playPauseButton.colors;
            if (isPlaying)
            {
                // Slightly greener tint when playing
                colors.normalColor = new Color(0.35f, 0.55f, 0.35f, 1f);
            }
            else
            {
                // Neutral tint when stopped
                colors.normalColor = new Color(0.35f, 0.35f, 0.45f, 1f);
            }
            playPauseButton.colors = colors;
        }
    }



    public void JumpToFrame(int index)
    {
        if (index < 0 || index >= frames.Count)
            return;

        currentFrameIndex = index;
        pixelDisplay.texture = frames[currentFrameIndex];
        UpdateFrameLabel();
        UpdateOnionLayers();
        UpdateThumbnailsHighlight();
        if (gridOverlay != null) gridOverlay.SyncToCurrentFrame();
    }

    private void UpdateFrameLabel()
    {
        if (frameLabel != null)
            frameLabel.text = "Frame " + (currentFrameIndex + 1) + " / " + frames.Count;
    }

    // ---------------------------------------------------------
    // NEW: REORDER FRAMES (used by thumbnail drag/drop)
    // ---------------------------------------------------------
    public void ReorderFrame(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= frames.Count)
            return;
        if (toIndex < 0 || toIndex >= frames.Count)
            return;
        if (fromIndex == toIndex)
            return;

        int oldCurrent = currentFrameIndex;

        // Take the frame out
        Texture2D moving = frames[fromIndex];
        frames.RemoveAt(fromIndex);

        // Adjust target index after removal if needed
        if (toIndex > fromIndex)
            toIndex--;

        // Insert at new position
        frames.Insert(toIndex, moving);

        // Keep the same logical frame selected
        if (oldCurrent == fromIndex)
        {
            currentFrameIndex = toIndex;
        }
        else if (oldCurrent > fromIndex && oldCurrent <= toIndex)
        {
            currentFrameIndex = oldCurrent - 1;
        }
        else if (oldCurrent < fromIndex && oldCurrent >= toIndex)
        {
            currentFrameIndex = oldCurrent + 1;
        }
        else
        {
            currentFrameIndex = oldCurrent;
        }

        currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, frames.Count - 1);

        // Refresh UI
        pixelDisplay.texture = frames[currentFrameIndex];
        UpdateFrameLabel();
        UpdateOnionLayers();
        UpdateThumbnails();   // order changed -> rebuild thumbnails
    }

    // ---------------------------------------------------------
    // THUMBNAILS HELPERS
    // ---------------------------------------------------------
    private void UpdateThumbnails()
    {
        if (thumbnailStrip != null)
        {
            thumbnailStrip.Rebuild(frames, currentFrameIndex);
        }
    }

    private void UpdateThumbnailsHighlight()
    {
        if (thumbnailStrip != null)
        {
            thumbnailStrip.Highlight(currentFrameIndex);
        }
    }

    // ---------------------------------------------------------
    // Grid helper
    // ---------------------------------------------------------

    public Texture2D GetCurrentFrameTexture()
    {
        if (frames == null || frames.Count == 0)
            return null;
        return frames[currentFrameIndex];
    }


    // ---------------------------------------------------------
    // COLOR (used by picker & swatches)
    // ---------------------------------------------------------
    public void SetCurrentColor(Color col)
    {
        currentColor = col;
    }
}
