using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class FrameThumbnailItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public FrameThumbnailStrip strip;
    public int index;

    private static FrameThumbnailItem currentlyDragging;
    private CanvasGroup canvasGroup;

    private Vector3 startPosition;
    private Transform startParent;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Init(FrameThumbnailStrip s, int idx)
    {
        strip = s;
        index = idx;
    }

    // ---------------------------------------------------------
    // DRAG START
    // ---------------------------------------------------------
    public void OnBeginDrag(PointerEventData eventData)
    {
        currentlyDragging = this;

        startPosition = transform.position;
        startParent = transform.parent;

        // Let raycasts pass through this item while dragging
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f; // slight feedback; optional
    }

    // ---------------------------------------------------------
    // DRAGGING
    // ---------------------------------------------------------
    public void OnDrag(PointerEventData eventData)
    {
        // Move the thumbnail with the mouse
        // (If you don't want it to move visually, you can comment this out)
        transform.position = eventData.position;
    }

    // ---------------------------------------------------------
    // DRAG END
    // ---------------------------------------------------------
    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore raycasts so this thumbnail can be clicked again
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Snap back to original position; the strip will rebuild if order changed
        transform.position = startPosition;

        currentlyDragging = null;
    }

    // ---------------------------------------------------------
    // DROP ON THIS THUMBNAIL
    // ---------------------------------------------------------
    public void OnDrop(PointerEventData eventData)
    {
        if (currentlyDragging != null &&
            currentlyDragging != this &&
            strip != null)
        {
            strip.OnThumbnailDropped(currentlyDragging.index, this.index);
        }
    }
}
