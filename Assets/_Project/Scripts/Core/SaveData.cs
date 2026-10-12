using System;

// Version 2 adds upgrade levels and payment progress without changing version-1 purchases.
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 2;
    public int schemaVersion = CurrentVersion;
    public int currentMoney;
    public int progressionStage;
    public PurchaseSaveData secondInbound;
    public PurchaseSaveData warehouseExpansion;
    public PurchaseSaveData inputWorker;
    public PurchaseSaveData deliveryWorker;
    public UpgradeSaveData carryUpgrade;
    public UpgradeSaveData sortingUpgrade;
    public UpgradeSaveData workerSpeedUpgrade;

    public void Migrate()
    {
        if (schemaVersion == 1 || schemaVersion == CurrentVersion)
        {
            // Missing upgrade records mean Lv.1, not zero. Preserve every existing field.
            if (carryUpgrade == null) carryUpgrade = new UpgradeSaveData();
            if (sortingUpgrade == null) sortingUpgrade = new UpgradeSaveData();
            if (workerSpeedUpgrade == null) workerSpeedUpgrade = new UpgradeSaveData();
            schemaVersion = CurrentVersion;
        }
    }
}

[Serializable]
public class PurchaseSaveData
{
    public bool purchased;
    public int paidAmount;
}

[Serializable]
public class UpgradeSaveData
{
    // The level also identifies the next purchase tier; price remains scene-configured.
    public int level = 1;
    public int paidAmount;
}
