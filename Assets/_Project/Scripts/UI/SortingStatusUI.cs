using TMPro;
using UnityEngine;

// Reads waiting queues only. The box currently being sorted is not an input item.
[DefaultExecutionOrder(100)]
public sealed class SortingStatusUI : MonoBehaviour
{
    [SerializeField] private SortingStation sortingStation;
    [SerializeField] private TMP_Text inputText;
    [SerializeField] private TMP_Text outputText;

    private int lastInput = -1, lastOutput = -1, lastMaxInput = -1, lastMaxOutput = -1;

    private void OnEnable()
    {
        lastInput = lastOutput = lastMaxInput = lastMaxOutput = -1;
        Refresh();
    }

    private void LateUpdate() { Refresh(); }

    public void Refresh()
    {
        if (sortingStation == null) return;
        int input = sortingStation.InputCount, output = sortingStation.OutputCount;
        int maxInput = sortingStation.MaxInputCount, maxOutput = sortingStation.MaxOutputCount;
        if (inputText != null && (input != lastInput || maxInput != lastMaxInput))
            UpdateLabel(inputText, true, input, maxInput);
        if (outputText != null && (output != lastOutput || maxOutput != lastMaxOutput))
            UpdateLabel(outputText, false, output, maxOutput);
        lastInput = input;
        lastOutput = output;
        lastMaxInput = maxInput;
        lastMaxOutput = maxOutput;
    }

    private static void UpdateLabel(TMP_Text label, bool input, int count, int capacity)
    {
        label.color = count >= capacity ? new Color(1f, 0.47f, 0.43f, 1f) :
            input ? new Color(0.27f, 0.88f, 0.81f, 1f) : new Color(1f, 0.83f, 0.42f, 1f);
        label.outlineColor = new Color(0.025f, 0.04f, 0.06f, 1f);
        label.outlineWidth = 0.18f;
        label.SetText(input
            ? "<size=65%>INPUT</size>\n<b>{0}</b> <size=70%><color=#DDE7F1>/ {1}</color></size>"
            : "<size=65%>OUTPUT</size>\n<b>{0}</b> <size=70%><color=#DDE7F1>/ {1}</color></size>", count, capacity);
    }
}
