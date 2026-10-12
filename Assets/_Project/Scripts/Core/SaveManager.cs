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
    [SerializeField] private CarryCapacityUpgradeZone carryUpgrade;
    [SerializeField] private SortingSpeedUpgradeZone sortingUpgrade;
    [SerializeField] private WorkerSpeedUpgradeZone workerSpeedUpgrade;
    [SerializeField] private float saveInterval = 0.5f;

    [NonSerialized] private SaveFileService files;
    [NonSerialized] private MoneyManager subscribedMoneyManager;
    [NonSerialized] private bool connectionsReady;
    // Hot reload preserves these fields, including the initial-load result and backup policy.
    private bool loadAttempted;
    private string pendingLegacySource;
    private bool preserveLoadedBackup;
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
        if (loadAttempted) return;
        loadAttempted = true;
        try
        {
            RequireReferences();
            ConnectRuntimeServices();
            moneyManager.Initialize();
            SaveData data = files.Load(Validate);
            RememberFileState();
            if (data != null)
            {
                // Restore abilities before activating workers or exposing purchase pads.
                carryUpgrade.RestoreState(data.carryUpgrade);
                sortingUpgrade.RestoreState(data.sortingUpgrade);
                workerSpeedUpgrade.RestoreState(data.workerSpeedUpgrade);
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
            canSave = false;
            Debug.LogError("Save initialization failed; existing files will not be overwritten.\n" + ex, this);
        }
        IsReady = true;
        if (progressionManager != null) progressionManager.RefreshProgression();
        if (goalUI != null) goalUI.RefreshGoal();
        if (canSave) RequestSave();
    }

    private void RequireReferences()
    {
        if (moneyManager == null || secondInbound == null || warehouseExpansion == null ||
            inputWorker == null || deliveryWorker == null || progressionManager == null || goalUI == null ||
            carryUpgrade == null || sortingUpgrade == null || workerSpeedUpgrade == null ||
            !secondInbound.HasSaveReferences || !warehouseExpansion.HasSaveReferences ||
            !inputWorker.HasSaveReferences || !deliveryWorker.HasSaveReferences ||
            !carryUpgrade.HasSaveReferences || !sortingUpgrade.HasSaveReferences || !workerSpeedUpgrade.HasSaveReferences)
            throw new InvalidOperationException("SaveManager required references are missing; refusing to save incomplete data.");
    }

    private void ConnectRuntimeServices()
    {
        connectionsReady = false;
        if (Instance != null && Instance != this)
            throw new InvalidOperationException("Another SaveManager owns the scene.");
        Instance = this;
        if (files == null)
            files = new SaveFileService(Application.persistentDataPath, pendingLegacySource, preserveLoadedBackup);
        if (subscribedMoneyManager != null) subscribedMoneyManager.MoneyChanged -= RequestSave;
        if (moneyManager != null)
        {
            moneyManager.MoneyChanged -= RequestSave;
            moneyManager.MoneyChanged += RequestSave;
        }
        subscribedMoneyManager = moneyManager;
        connectionsReady = true;
    }

    private void RememberFileState()
    {
        if (files == null) return;
        pendingLegacySource = files.LegacySource;
        preserveLoadedBackup = files.LoadedBackup;
    }

    private void OnEnable()
    {
        if (!loadAttempted) return;
        try
        {
            // Reconnect only: never load a file or reinitialize runtime progress here.
            RequireReferences();
            ConnectRuntimeServices();
            if (canSave && IsReady) RequestSave();
        }
        catch (Exception ex)
        {
            connectionsReady = false;
            Debug.LogError("Save service reconnection failed.\n" + ex, this);
        }
    }

    private void OnDisable()
    {
        RememberFileState();
        if (subscribedMoneyManager != null) subscribedMoneyManager.MoneyChanged -= RequestSave;
        subscribedMoneyManager = null;
        connectionsReady = false;
        if (Instance == this) Instance = null;
    }

    private bool Validate(SaveData data)
    {
        return data.currentMoney >= 0 && secondInbound.IsValidSave(data.secondInbound) &&
            warehouseExpansion.IsValidSave(data.warehouseExpansion) &&
            inputWorker.IsValidSave(data.inputWorker) && deliveryWorker.IsValidSave(data.deliveryWorker) &&
            carryUpgrade.IsValidSave(data.carryUpgrade) && sortingUpgrade.IsValidSave(data.sortingUpgrade) &&
            workerSpeedUpgrade.IsValidSave(data.workerSpeedUpgrade);
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
        try
        {
            RequireReferences();
            if (!connectionsReady || files == null || Instance != this || subscribedMoneyManager != moneyManager)
                ConnectRuntimeServices();
            SaveData data = new SaveData
            {
                currentMoney = moneyManager.CurrentMoney,
                secondInbound = secondInbound.CaptureState(),
                warehouseExpansion = warehouseExpansion.CaptureState(),
                inputWorker = inputWorker.CaptureState(),
                deliveryWorker = deliveryWorker.CaptureState(),
                carryUpgrade = carryUpgrade.CaptureState(),
                sortingUpgrade = sortingUpgrade.CaptureState(),
                workerSpeedUpgrade = workerSpeedUpgrade.CaptureState()
            };
            data.progressionStage = !data.secondInbound.purchased ? 1 :
                !data.deliveryWorker.purchased ? 2 : !data.inputWorker.purchased ? 3 : 4;
            if (!Validate(data))
                throw new InvalidOperationException("Captured save data is invalid; existing files were preserved.");
            files.Write(data);
            RememberFileState();
            dirty = false;
        }
        catch (Exception ex)
        {
            dirty = true;
            nextSaveTime = Time.unscaledTime + 2f;
            Debug.LogError("Save failed; retry scheduled.\n" + ex, this);
        }
    }

    private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
    private void OnApplicationQuit() { SaveNow(); }

    private void OnDestroy()
    {
        if (moneyManager != null) moneyManager.MoneyChanged -= RequestSave;
        if (Instance == this) Instance = null;
    }

    [ContextMenu("Reset Save (Play Mode Only)")]
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
