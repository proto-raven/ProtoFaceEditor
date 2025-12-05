using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecentColorSwatches : MonoBehaviour
{
    public ToastPixelEditor editor;
    public RawImage currentColorDisplay;
    public Button[] swatchButtons;

    private List<Color> recentColors = new List<Color>();

    // Called by ColorWheelPicker when a color is picked (but not yet used)
    public void OnColorPicked(Color c)
    {
        // Just update the "current color" display UI
        if (currentColorDisplay != null)
        {
            currentColorDisplay.color = c;
        }

        // Editor's currentColor is usually already set by ColorWheelPicker,
        // but if you want to be extra safe:
        if (editor != null)
        {
            editor.SetCurrentColor(c);
        }
    }

    // Called by ToastPixelEditor when a pixel is actually painted
    public void OnColorUsed(Color c)
    {
        // Remove any previous occurrence of this color
        for (int i = recentColors.Count - 1; i >= 0; i--)
        {
            if (recentColors[i] == c)
            {
                recentColors.RemoveAt(i);
            }
        }

        // Add to the front
        recentColors.Insert(0, c);

        // Trim list to available swatch slots
        int max = (swatchButtons != null) ? swatchButtons.Length : 0;
        if (max > 0 && recentColors.Count > max)
        {
            recentColors.RemoveRange(max, recentColors.Count - max);
        }

        UpdateSwatchButtons();
    }

    private void UpdateSwatchButtons()
    {
        if (swatchButtons == null)
            return;

        for (int i = 0; i < swatchButtons.Length; i++)
        {
            Image img = swatchButtons[i].GetComponent<Image>();
            if (img == null) continue;

            if (i < recentColors.Count)
            {
                img.color = recentColors[i];
                swatchButtons[i].interactable = true;
            }
            else
            {
                img.color = new Color(0f, 0f, 0f, 0f); // clear / empty
                swatchButtons[i].interactable = false;
            }
        }
    }

    // Called by each swatch button's OnClick, with its index
    public void UseSwatch(int index)
    {
        if (index < 0 || index >= recentColors.Count)
            return;

        Color c = recentColors[index];

        if (editor != null)
        {
            editor.SetCurrentColor(c);
        }

        if (currentColorDisplay != null)
        {
            currentColorDisplay.color = c;
        }
    }
}
