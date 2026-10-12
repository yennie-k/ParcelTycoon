using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public static bool GameplayReady => Instance == null || Instance.IsReady;
    public bool IsReady { get; private set; }

    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private PurchaseZone secondInbound;
    [SerializeField] private PurchaseZone warehouseExpansion;
    [SerializeField] private PurchaseZone inputWorker;
    [SerializeField] private PurchaseZone deliveryWorker;
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private GoalUI goalUI;
    [SerializeField] private float saveInterval = 0.5f;

    private SaveFileService files;
    private bool dirty;
    private bool canSave;
    private float nextSaveTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one SaveManager is allowed per scene.", this);
            enabled = false;
            return;
        }
        Instance = this;
        files = new SaveFileService(Application.persistentDataPath);
        if (moneyManager == null || secondInbound == null || warehouseExpansion == null ||
            inputWorker == null || deliveryWorker == null || progressionManager == null || goalUI == null ||
            !secondInbound.HasSaveReferences || !warehouseExpansion.HasSaveReferences ||
            !inputWorker.HasSaveReferences || !deliveryWorker.HasSaveReferences)
        {
            Debug.LogError("SaveManager references are incomplete; saving is disabled.", this);
            IsReady = true;
            return;
        }

        moneyManager.Initialize();
        moneyManager.MoneyChanged += RequestSave;
        try
        {
            SaveData data = files.Load(Validate);
            if (data != null)
            {
                moneyManager.RestoreMoney(data.currentMoney);
                secondInbound.RestoreState(data.secondInbound);
                warehouseExpansion.RestoreState(data.warehouseExpansion);
                inputWorker.RestoreState(data.inputWorker);
                deliveryWorker.RestoreState(data.deliveryWorker);
            }
            canSave = true;
        }
        catch (Exception ex)
        {
            Debug.LogError("Save load failed; starting with scene defaults without overwriting the save. " + ex.Message, this);
        }
        IsReady = true;
        progressionManager.RefreshProgression();
        goalUI.RefreshGoal();
        if (canSave) RequestSave();
    }

    private bool Validate(SaveData data)
    {
        return data.currentMoney >= 0 && secondInbound.IsValidSave(data.secondInbound) &&
            warehouseExpansion.IsValidSave(data.warehouseExpansion) &&
            inputWorker.IsValidSave(data.inputWorker) && deliveryWorker.IsValidSave(data.deliveryWorker);
    }

    public void RequestSave()
    {
        if (!canSave || !IsReady) return;
        if (!dirty) nextSaveTime = Time.unscaledTime + Mathf.Max(0f, saveInterval);
        dirty = true;
    }

    private void LateUpdate()
    {
        if (dirty && Time.unscaledTime >= nextSaveTime) SaveNow();
    }

    public void SaveNow()
    {
        if (!canSave || !IsReady) return;
        SaveData data = new SaveData
        {
            currentMoney = moneyManager.CurrentMoney,
            secondInbound = secondInbound.CaptureState(),
            warehouseExpansion = warehouseExpansion.CaptureState(),
            inputWorker = inputWorker.CaptureState(),
            deliveryWorker = deliveryWorker.CaptureState()
        };
        data.progressionStage = !data.secondInbound.purchased ? 1 :
            !data.deliveryWorker.purchased ? 2 : !data.inputWorker.purchased ? 3 : 4;
        try
        {
            files.Write(data);
            dirty = false;
        }
        catch (Exception ex)
        {
            dirty = true;
            nextSaveTime = Time.unscaledTime + 2f;
            Debug.LogError("Save failed; retry scheduled. " + ex.Message, this);
        }
    }

    private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
    private void OnApplicationQuit() { SaveNow(); }

    private void OnDestroy()
    {
        if (moneyManager != null) moneyManager.MoneyChanged -= RequestSave;
        if (Instance == this) Instance = null;
    }

    [ContextMenu("Reset Stage 1 Save (Play Mode Only)")]
    public void ResetSave()
    {
        if (!Application.isPlaying || Instance != this) return;
        canSave = false;
        dirty = false;
        try
        {
            files.Delete();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        catch (Exception ex)
        {
            Debug.LogError("Reset failed; saving remains disabled. " + ex.Message, this);
        }
    }
}
