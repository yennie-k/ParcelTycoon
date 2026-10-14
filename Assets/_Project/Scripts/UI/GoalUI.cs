using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoalUI : MonoBehaviour
{
    [SerializeField] private TMP_Text goalText;
    [SerializeField] private ProgressionManager progressionManager;

    private ProgressionManager.GoalId? checkedGoal;
    private TMP_FontAsset checkedFont;
    private bool supportsKorean;

    private Texture2D roundedTexture;
    private Sprite roundedSprite;
    private Image panelImage;

    private void OnEnable()
    {
        checkedGoal = null;
        SetupRoundedPanel();
    }

    private void SetupRoundedPanel()
    {
        if (goalText == null || goalText.transform.parent == null) return;
        panelImage = goalText.transform.parent.GetComponent<Image>();
        if (panelImage == null || panelImage.sprite != null) return;
        // Small local UI texture; no downloaded image or shared asset modification.
        const int size = 64;
        const float radius = 12f;
        roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        roundedTexture.name = "GoalPanel Rounded Background";
        roundedTexture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - (x + 0.5f), (x + 0.5f) - (size - radius));
                float dy = Mathf.Max(radius - (y + 0.5f), (y + 0.5f) - (size - radius));
                float distance = Mathf.Sqrt(Mathf.Max(0f, dx) * Mathf.Max(0f, dx) + Mathf.Max(0f, dy) * Mathf.Max(0f, dy));
                roundedTexture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance + 0.5f)));
            }
        roundedTexture.Apply(false, true);
        roundedSprite = Sprite.Create(roundedTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        panelImage.sprite = roundedSprite;
        panelImage.type = Image.Type.Sliced;
    }

    private void OnDestroy()
    {
        if (panelImage != null && panelImage.sprite == roundedSprite) panelImage.sprite = null;
        if (roundedSprite != null) Destroy(roundedSprite);
        if (roundedTexture != null) Destroy(roundedTexture);
    }

    private void Update() { RefreshGoal(); }

    public void RefreshGoal()
    {
        if (!SaveManager.GameplayReady || goalText == null || progressionManager == null) return;
        ProgressionManager.GoalId goal = progressionManager.CurrentGoal;
        string message = progressionManager.GetGoalText(goal);
        // Preserve the existing font. Avoid missing-glyph warnings until a Korean font
        // (or fallback) is supplied; dynamic fonts may add supported glyphs here.
        if (checkedGoal != goal || checkedFont != goalText.font)
        {
            checkedGoal = goal;
            checkedFont = goalText.font;
            supportsKorean = checkedFont != null && checkedFont.HasCharacters(message, out _, true, true);
        }
        if (!supportsKorean)
            message = progressionManager.GetGoalText(goal, false);
        if (goal == ProgressionManager.GoalId.Complete)
            message = $"<color=#F5C86C><b>{message}</b></color>";
        else
        {
            if (message.StartsWith("목표: ")) message = message.Substring(4);
            else if (message.StartsWith("Goal: ")) message = message.Substring(6);
            message = $"<size=55%><color=#45E0CE>NEXT GOAL</color></size>\n<color=#FFFFFF>{message}</color>";
        }
        if (goalText.text != message) goalText.text = message;
    }
}
