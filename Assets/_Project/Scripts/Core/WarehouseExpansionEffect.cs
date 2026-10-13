using TMPro;
using UnityEngine;

// Run after SaveManager (-1000), before the station and workers (default order).
[DefaultExecutionOrder(-900)]
public class WarehouseExpansionEffect : MonoBehaviour
{
    [SerializeField] private GameObject warehouseExpansion;
    [SerializeField] private SortingStation sortingStation;
    [SerializeField] private TMP_Text inputCapacityText;
    [SerializeField] private TMP_Text outputCapacityText;
    [SerializeField] private int baseCapacity = 5;
    [SerializeField] private int expandedCapacity = 10;

    [Header("Capacity Label Colors")]
    [SerializeField] private Color inputColor = new Color(0.27f, 0.88f, 0.81f, 1f);
    [SerializeField] private Color outputColor = new Color(1f, 0.83f, 0.42f, 1f);
    [SerializeField] private Color fullColor = new Color(1f, 0.47f, 0.43f, 1f);

    private int displayedInputCount = -1;
    private int displayedOutputCount = -1;
    private int displayedInputCapacity = -1;
    private int displayedOutputCapacity = -1;

    private void OnEnable()
    {
        displayedInputCount = displayedOutputCount = -1;
        displayedInputCapacity = displayedOutputCapacity = -1;
        ApplyCapacity();
    }

    private void Update() { ApplyCapacity(); }

    private void LateUpdate()
    {
        // Catch purchases completing in a coroutine later in the same frame.
        if (!ApplyCapacity()) return;
        if (inputCapacityText != null && (displayedInputCount != sortingStation.InputCount ||
            displayedInputCapacity != sortingStation.MaxInputCount))
        {
            displayedInputCount = sortingStation.InputCount;
            displayedInputCapacity = sortingStation.MaxInputCount;
            UpdateCapacityLabel(inputCapacityText, "INPUT", displayedInputCount, displayedInputCapacity, inputColor);
        }
        if (outputCapacityText != null && (displayedOutputCount != sortingStation.OutputCount ||
            displayedOutputCapacity != sortingStation.MaxOutputCount))
        {
            displayedOutputCount = sortingStation.OutputCount;
            displayedOutputCapacity = sortingStation.MaxOutputCount;
            UpdateCapacityLabel(outputCapacityText, "OUTPUT", displayedOutputCount, displayedOutputCapacity, outputColor);
        }
    }

    private void UpdateCapacityLabel(TMP_Text label, string title, int count, int capacity, Color normalColor)
    {
        label.color = count >= capacity ? fullColor : normalColor;
        // TMP creates a local material instance; the shared font asset is not modified.
        label.outlineColor = new Color(0.025f, 0.04f, 0.06f, 1f);
        label.outlineWidth = 0.18f;
        label.text = $"<size=65%>{title}</size>\n<b>{count}</b> <size=70%><color=#DDE7F1>/ {capacity}</color></size>";
    }

    private bool ApplyCapacity()
    {
        if (!SaveManager.GameplayReady || warehouseExpansion == null || sortingStation == null) return false;
        int capacity = warehouseExpansion.activeSelf ? expandedCapacity : baseCapacity;
        if (sortingStation.MaxInputCount != capacity || sortingStation.MaxOutputCount != capacity)
            sortingStation.SetStorageCapacity(capacity, capacity);
        return true;
    }
}
