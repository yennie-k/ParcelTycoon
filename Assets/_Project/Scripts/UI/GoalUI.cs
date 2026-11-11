using GoalId = ProgressionManager.GoalId;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoalUI : MonoBehaviour
{
    [SerializeField] private TMP_Text goalText;
    [SerializeField] private ProgressionManager progressionManager;

    private ProgressionManager.GoalId? checkedGoal;
    private TMP_FontAsset checkedFont;
    private (int A, int B, int C, int D, int E, int F, int G, int H) checkedProgress;

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
        GoalId goal = progressionManager.CurrentGoal;
        var progress = ReadProgress(goal);
        var font = goalText.font;
        if (checkedGoal == goal && checkedFont == font && checkedProgress.Equals(progress)) return;
        checkedGoal = goal;
        checkedFont = font;
        checkedProgress = progress;

        string title = progressionManager.GetGoalText(goal);
        if (!Supports(title)) title = progressionManager.GetGoalText(goal, false);
        if (title.StartsWith("목표: ")) title = title.Substring(4);
        else if (title.StartsWith("Goal: ")) title = title.Substring(6);

        bool complete = goal == GoalId.Complete;
        string header = complete ? "GROWTH COMPLETE" : "NEXT GOAL";
        string color = complete ? "#F5C86C" : "#45E0CE";
        string text = $"<size=55%><color={color}>{header}</color></size>\n<color=#FFFFFF>{title}</color>";
        string detail = FormatProgress(goal, progress, true);
        if (!Supports(detail)) detail = FormatProgress(goal, progress, false);
        if (detail.Length > 0) text += $"\n<size=65%><color=#DDE7F1>{detail}</color></size>";
        if (goalText.text != text) goalText.text = text;
    }

    // Probe existing glyphs only. Do not populate or save the shared font atlas here.
    private bool Supports(string text) => text.Length == 0 ||
        (checkedFont != null && checkedFont.HasCharacters(text, out _, true, false));

    private (int, int, int, int, int, int, int, int) ReadProgress(GoalId goal)
    {
        if (goal == GoalId.BuyTwoUpgrades) return (progressionManager.PurchasedUpgradeCount, 0, 0, 0, 0, 0, 0, 0);
        if (goal == GoalId.AllUpgradesLevel2 || goal == GoalId.AnyUpgradeMax || goal == GoalId.AllUpgradesMax)
            return progressionManager.UpgradeLevels;
        var purchase = progressionManager.GetGoalPurchase(goal);
        return purchase == null ? default : (purchase.PurchaseCost, purchase.PaidAmount, 1, 0, 0, 0, 0, 0);
    }

    private string FormatProgress(GoalId goal, (int A, int B, int C, int D, int E, int F, int G, int H) p, bool korean)
    {
        if (goal == GoalId.BuyTwoUpgrades) return $"{p.A} / {ProgressionManager.RequiredUpgradePurchases}";
        if (goal == GoalId.AllUpgradesLevel2 || goal == GoalId.AnyUpgradeMax || goal == GoalId.AllUpgradesMax)
        {
            bool showMax = goal != GoalId.AllUpgradesLevel2;
            string sorting = Level(p.A, p.D, showMax);
            string carry = Level(p.B, p.E, showMax);
            string worker = Level(p.C, p.F, showMax);
            return korean ? $"분류 속도 {sorting} · 플레이어 운반량 {carry}\n직원 이동 속도 {worker} · 직원 운반량 {Level(p.G, p.H, showMax)}" :
                $"Sorting speed {sorting} / Player carry {carry}\nStaff speed {worker} / Staff carry {Level(p.G, p.H, showMax)}";
        }
        if (p.C == 0) return "";
        string symbol = korean || Supports("₩") ? "₩" : "KRW ";
        string price = $"{symbol}{Mathf.Max(0, p.A - p.B):N0}";
        return p.B == 0 ? price : (korean ? "남은 금액 " : "Remaining ") + price;
    }

    private static string Level(int current, int max, bool showMax) =>
        !showMax ? $"Lv.{current}" : current >= max ? "MAX" : $"Lv.{current}/{max}";
}
