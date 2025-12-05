# Toast Face Editor – Public API Reference

This file defines the stable, external-facing API for the editor’s core scripts.
Method names here should be treated as a contract – do not rename them casually.

---

## ToastPixelEditor.cs

```csharp
public class ToastPixelEditor : MonoBehaviour,
    IPointerDownHandler, IDragHandler
{
    // Constants
    public const int WIDTH;
    public const int HEIGHT;

    // Frame & drawing API
    public void OnPointerDown(PointerEventData eventData);
    public void OnDrag(PointerEventData eventData);
    public void NextFrame();
    public void PrevFrame();
    public void NewFrame();
    public void DuplicateCurrentFrame();
    public void DeleteCurrentFrame();
    public void JumpToFrame(int index);
    public void TogglePlay();
    public void ReorderFrame(int fromIndex, int toIndex);
    public void SetCurrentColor(Color col);

    // Data access
    public Texture2D GetCurrentFrameTexture();
}
Notes:

ReorderFrame is called by the thumbnail strip when frames are drag-reordered.

GetCurrentFrameTexture is used by systems like the grid overlay.

RecentColorSwatches.cs
public class RecentColorSwatches : MonoBehaviour
{
    public void OnColorPicked(Color c);
    public void OnColorUsed(Color c);
    public void UseSwatch(int index);
}


OnColorPicked – called when the color wheel changes the current brush color.

OnColorUsed – called by ToastPixelEditor when a pixel is actually painted.

UseSwatch – called by swatch buttons (OnClick) with their swatch index.

ColorWheelPicker.cs
public class ColorWheelPicker : MonoBehaviour,
    IPointerDownHandler, IDragHandler
{
    public void OnPointerDown(PointerEventData eventData);
    public void OnDrag(PointerEventData eventData);
}


Internally sets the editor’s current color and notifies RecentColorSwatches.

FrameThumbnailStrip.cs
public class FrameThumbnailStrip : MonoBehaviour
{
    public void Rebuild(List<Texture2D> frames, int currentIndex);
    public void Highlight(int currentIndex);
    public void OnThumbnailDropped(int fromIndex, int toIndex);
}


Rebuild – populate all thumbnails from the current frame list.

Highlight – update which frame is visually selected.

OnThumbnailDropped – called when a thumbnail is dropped on another, triggers ReorderFrame on the editor.

FrameThumbnailItem.cs
public class FrameThumbnailItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public FrameThumbnailStrip strip;
    public int index;

    public void Init(FrameThumbnailStrip strip, int index);
    public void OnBeginDrag(PointerEventData eventData);
    public void OnDrag(PointerEventData eventData);
    public void OnEndDrag(PointerEventData eventData);
    public void OnDrop(PointerEventData eventData);
}


Init links the thumbnail to its strip and logical frame index.

PixelZoomPan.cs
public class PixelZoomPan : MonoBehaviour
{
    // Behavior is configured via serialized fields:
    // - RectTransform target
    // - RectTransform[] extraTargets
    // - int minZoomStep, maxZoomStep, startZoomStep
    // - float scrollNotchesPerStep
    // - int panMouseButton
    // - float panSpeed
    // - KeyCode resetKey
}


This script currently exposes its behavior purely through public fields.

If we add explicit public methods later (e.g. ResetView()), they should be listed here.

PixelGridOverlay.cs
public class PixelGridOverlay : MonoBehaviour
{
    public void SyncToCurrentFrame();
    public void SetGridEnabled(bool enabled);
}


SyncToCurrentFrame – keeps the overlay grid aligned with the editor’s current frame texture and resolution.

SetGridEnabled – used by the Grid toggle and at startup to enable/disable rendering.

Shader: Toast/PixelGridOverlay
Shader "Toast/PixelGridOverlay"


Important properties:

_MainTex – bound to the current frame texture.

_GridResolution – (width, height, 0, 0).

_Thickness – normalized line thickness inside each cell.

_GridEnabled – 0 or 1, masks out the grid when disabled.