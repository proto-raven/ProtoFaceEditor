using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FrameThumbnailStrip : MonoBehaviour
{
    public ToastPixelEditor editor;
    public RectTransform contentRoot;   // This will be your "ThumbnailBar" RectTransform
    public Button thumbnailPrefab;      // This will be "ThumbnailTemplate"

    private readonly List<Button> thumbnailButtons = new List<Button>();

    // Rebuild all thumbnails whenever frames change or current index changes
    public void Rebuild(List<Texture2D> frames, int currentIndex)
    {
        if (contentRoot == null || thumbnailPrefab == null || frames == null)
            return;

        // Clear old thumbnails
        foreach (var btn in thumbnailButtons)
        {
            if (btn != null)
            {
                Destroy(btn.gameObject);
            }
        }
        thumbnailButtons.Clear();

        // Recreate
        for (int i = 0; i < frames.Count; i++)
        {
            Button btn = Instantiate(thumbnailPrefab, contentRoot);
            btn.gameObject.SetActive(true); // in case prefab is disabled in the scene
            thumbnailButtons.Add(btn);

            // Set thumbnail texture on the ThumbImage child
            RawImage thumbImage = btn.GetComponentInChildren<RawImage>();
            if (thumbImage != null && frames[i] != null)
            {
                thumbImage.texture = frames[i];
                thumbImage.color = Color.white;
            }

            // Capture index for button click
            int index = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (editor != null)
                {
                    editor.JumpToFrame(index);
                }
            });

            // Ensure a FrameThumbnailItem exists and is initialized
            FrameThumbnailItem item = btn.GetComponent<FrameThumbnailItem>();
            if (item == null)
            {
                item = btn.gameObject.AddComponent<FrameThumbnailItem>();
            }
            item.Init(this, i);
        }

        Highlight(currentIndex);
    }

    public void Highlight(int currentIndex)
    {
        for (int i = 0; i < thumbnailButtons.Count; i++)
        {
            var bg = thumbnailButtons[i].GetComponent<Image>();
            if (bg != null)
            {
                // Yellow for current frame, grey otherwise
                bg.color = (i == currentIndex) ? new Color(1f, 1f, 0.3f, 1f)
                                               : new Color(0.2f, 0.2f, 0.2f, 1f);
            }
        }
    }

    // Called by FrameThumbnailItem when a drag is dropped onto another thumbnail
    public void OnThumbnailDropped(int fromIndex, int toIndex)
    {
        if (editor != null)
        {
            editor.ReorderFrame(fromIndex, toIndex);
        }
    }
}
