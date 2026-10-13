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
            inputCapacityText.text = $"INPUT {displayedInputCount}/{displayedInputCapacity}";
        }
        if (outputCapacityText != null && (displayedOutputCount != sortingStation.OutputCount ||
            displayedOutputCapacity != sortingStation.MaxOutputCount))
        {
            displayedOutputCount = sortingStation.OutputCount;
            displayedOutputCapacity = sortingStation.MaxOutputCount;
            outputCapacityText.text = $"OUTPUT {displayedOutputCount}/{displayedOutputCapacity}";
        }
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
