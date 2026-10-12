using System;
using System.IO;
using UnityEngine;

public sealed class SaveFileService
{
    public string FilePath { get; }
    private string legacySource;
    private bool loadedBackup;
    public SaveFileService(string directory)
    {
        FilePath = Path.Combine(directory, "parceltycoon-save.json");
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
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                // Never replace a future-version save with an older backup.
                if (data != null && data.schemaVersion > SaveData.CurrentVersion)
                    throw new NotSupportedException("Save version is newer than this game.");
                bool legacy = data != null && data.schemaVersion == 1;
                data?.Migrate();
                if (data == null || data.schemaVersion != SaveData.CurrentVersion || !validate(data))
                    throw new InvalidDataException("Invalid save data.");
                legacySource = legacy ? path : null;
                loadedBackup = path == FilePath + ".bak";
                return data;
            }
            catch (NotSupportedException) { throw; }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException)
            {
                failure = ex;
            }
        }
        throw new InvalidDataException("Could not load save or backup. Existing files were preserved.", failure);
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
