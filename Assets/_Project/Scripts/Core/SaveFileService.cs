using System;
using System.IO;
using UnityEngine;

public sealed class SaveFileService
{
    public string FilePath { get; }
    private string legacySource;
    private bool loadedBackup;
    public string LegacySource => legacySource;
    public bool LoadedBackup => loadedBackup;

    public SaveFileService(string directory, string pendingLegacySource = null, bool preserveLoadedBackup = false)
    {
        FilePath = Path.Combine(directory, "parceltycoon-save.json");
        legacySource = pendingLegacySource;
        loadedBackup = preserveLoadedBackup;
    }

    public SaveData Load(Func<SaveData, bool> validate)
    {
        if (!File.Exists(FilePath) && !File.Exists(FilePath + ".bak")) return null;
        Exception failure = null;
        foreach (string path in new[] { FilePath, FilePath + ".bak" })
        {
            if (!File.Exists(path)) continue;
            try
            {
                string json = File.ReadAllText(path);
                // Probe uses sentinel field values instead of gameplay defaults. JsonUtility's
                // missing-field behavior must not turn an incomplete v2 record into Lv.1.
                SaveShape shape = new SaveShape();
                JsonUtility.FromJsonOverwrite(json, shape);
                if (shape.schemaVersion < 1)
                    throw new InvalidDataException("Missing save version.");
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                // Never replace a future-version save with an older backup.
                if (data != null && data.schemaVersion > SaveData.CurrentVersion)
                    throw new NotSupportedException("Save version is newer than this game.");
                if (!shape.HasPurchases || (shape.schemaVersion == 2 && !shape.HasUpgrades))
                    throw new InvalidDataException("Required save fields are missing.");
                bool legacy = data != null && data.schemaVersion == 1;
                data?.Migrate();
                if (data == null || data.schemaVersion != SaveData.CurrentVersion || !validate(data))
                    throw new InvalidDataException("Invalid save data.");
                legacySource = legacy ? path : null;
                loadedBackup = path == FilePath + ".bak";
                return data;
            }
            catch (NotSupportedException) { throw; }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException)
            {
                failure = ex;
            }
        }
        throw new InvalidDataException("Could not load save or backup. Existing files were preserved.", failure);
    }

    [Serializable]
    private sealed class SaveShape
    {
        public int schemaVersion = -1;
        public int currentMoney = -1;
        public PurchaseSaveData secondInbound;
        public PurchaseSaveData warehouseExpansion;
        public PurchaseSaveData inputWorker;
        public PurchaseSaveData deliveryWorker;
        public UpgradeShape carryUpgrade = new UpgradeShape();
        public UpgradeShape sortingUpgrade = new UpgradeShape();
        public UpgradeShape workerSpeedUpgrade = new UpgradeShape();
        public bool HasPurchases => currentMoney >= 0 && secondInbound != null &&
            warehouseExpansion != null && inputWorker != null && deliveryWorker != null;
        public bool HasUpgrades => Complete(carryUpgrade) && Complete(sortingUpgrade) && Complete(workerSpeedUpgrade);
        private static bool Complete(UpgradeShape value) => value != null && value.level >= 1 && value.paidAmount >= 0;
    }

    [Serializable]
    private sealed class UpgradeShape
    {
        public int level = -1;
        public int paidAmount = -1;
    }

    public void Write(SaveData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        // Keep an untouched version-1 snapshot even after rotating the normal backup.
        if (legacySource != null)
        {
            if (!File.Exists(FilePath + ".v1.bak"))
                File.Copy(legacySource, FilePath + ".v1.bak");
            if (File.Exists(FilePath + ".bak") && !File.Exists(FilePath + ".v1.previous.bak"))
                File.Copy(FilePath + ".bak", FilePath + ".v1.previous.bak");
        }
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
        if (File.Exists(FilePath))
            File.Replace(temporary, FilePath, loadedBackup ? null : FilePath + ".bak");
        else
            File.Move(temporary, FilePath);
        legacySource = null;
        loadedBackup = false;
    }

    public void Delete()
    {
        foreach (string path in new[] { FilePath, FilePath + ".bak", FilePath + ".tmp", FilePath + ".v1.bak", FilePath + ".v1.previous.bak" })
            if (File.Exists(path)) File.Delete(path);
    }
}
