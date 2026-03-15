using UnityEngine;

public class PlayerCrosshair : MonoBehaviour
{
    [Header("Crosshair Ayarları")]
    [SerializeField] private float size = 2f;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] private float outlineThickness = 0.5f;

    private Texture2D crosshairTexture;
    private float lastSize;
    private float lastOutlineThickness;
    private Color lastColor;
    private Color lastOutlineColor;

    private void Awake()
    {
        CreateCircleTexture();
        UpdateLastValues();
    }

    private void Update()
    {
        // Eğer inspector'dan değerler değiştirilirse dokuyu yenile
        if (size != lastSize || outlineThickness != lastOutlineThickness || 
            defaultColor != lastColor || outlineColor != lastOutlineColor)
        {
            CreateCircleTexture();
            UpdateLastValues();
        }
    }

    private void UpdateLastValues()
    {
        lastSize = size;
        lastOutlineThickness = outlineThickness;
        lastColor = defaultColor;
        lastOutlineColor = outlineColor;
    }

    private void CreateCircleTexture()
    {
        int texSize = 32;
        crosshairTexture = new Texture2D(texSize, texSize, TextureFormat.ARGB32, false);
        crosshairTexture.filterMode = FilterMode.Point;
        
        float center = texSize / 2f;
        float radius = texSize / 2f - 1f;

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    crosshairTexture.SetPixel(x, y, Color.white);
                }
                else
                {
                    crosshairTexture.SetPixel(x, y, Color.clear);
                }
            }
        }
        crosshairTexture.Apply();
    }

    private void OnDestroy()
    {
        if (crosshairTexture != null) Destroy(crosshairTexture);
    }

    private void OnGUI()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        if (outlineThickness > 0f) DrawCrosshair(cx, cy, outlineColor, outlineThickness);
        DrawCrosshair(cx, cy, defaultColor, 0f);
    }

    private void DrawCrosshair(float cx, float cy, Color color, float expand)
    {
        GUI.color = color;
        float totalSize = size + (expand * 2f);
        GUI.DrawTexture(new Rect(cx - totalSize / 2f, cy - totalSize / 2f, totalSize, totalSize), crosshairTexture);
        GUI.color = Color.white;
    }
}
