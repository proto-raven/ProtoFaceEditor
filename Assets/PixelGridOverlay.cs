using UnityEngine;
using UnityEngine.UI;

public class PixelGridOverlay : MonoBehaviour
{
    [Header("References")]
    public ToastPixelEditor editor;   // ToastPixelEditor object
    public RawImage gridImage;        // RawImage on GridOverlay
    public Toggle gridToggle;         // Optional UI toggle

    [Header("Grid Settings")]
    public int gridWidth = ToastPixelEditor.WIDTH;
    public int gridHeight = ToastPixelEditor.HEIGHT;
    [Range(0.001f, 0.25f)]
    public float lineThickness = 0.06f;

    private Material materialInstance;

    private void Awake()
    {
        if (gridImage == null)
            gridImage = GetComponent<RawImage>();

        if (gridImage == null)
        {
            Debug.LogError("PixelGridOverlay: No RawImage assigned.");
            enabled = false;
            return;
        }

        // Make our own material instance so we don't modify a shared one
        materialInstance = new Material(gridImage.material);
        gridImage.material = materialInstance;

        gridImage.raycastTarget = false; // clicks pass through

        UpdateMaterialSettings();

        if (gridToggle != null)
        {
            gridToggle.onValueChanged.AddListener(SetGridEnabled);
            SetGridEnabled(gridToggle.isOn);
        }
        else
        {
            SetGridEnabled(true);
        }
    }

    private void OnDestroy()
    {
        if (gridToggle != null)
        {
            gridToggle.onValueChanged.RemoveListener(SetGridEnabled);
        }
    }

    private void UpdateMaterialSettings()
    {
        if (materialInstance == null)
            return;

        materialInstance.SetVector("_GridResolution",
            new Vector4(gridWidth, gridHeight, 0f, 0f));
        materialInstance.SetFloat("_Thickness", lineThickness);
    }

    public void SyncToCurrentFrame()
    {
        if (editor == null || materialInstance == null || gridImage == null)
            return;

        Texture2D tex = editor.GetCurrentFrameTexture();
        if (tex == null)
            return;

        gridImage.texture = tex;
        gridWidth = tex.width;
        gridHeight = tex.height;
        UpdateMaterialSettings();
    }

    public void SetGridEnabled(bool enabled)
    {
        if (materialInstance != null)
        {
            materialInstance.SetFloat("_GridEnabled", enabled ? 1f : 0f);
        }
        gridImage.enabled = enabled;
    }
}
