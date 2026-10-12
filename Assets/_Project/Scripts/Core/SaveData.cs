using System;

// Version 1 persists money and one-time purchases only. Upgrades belong to version 2.
[Serializable]
public class SaveData
{
    public int schemaVersion = 1;
    public int currentMoney;
    public int progressionStage;
    public PurchaseSaveData secondInbound;
    public PurchaseSaveData warehouseExpansion;
    public PurchaseSaveData inputWorker;
    public PurchaseSaveData deliveryWorker;
}

[Serializable]
public class PurchaseSaveData
{
    public bool purchased;
    public int paidAmount;
}
