using System;
using System.IO;
using UnityEngine;

public sealed class SaveFileService
{
    public string FilePath { get; }
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
                if (data != null && data.schemaVersion > 1)
                    throw new NotSupportedException("Save version is newer than this game.");
                if (data == null || data.schemaVersion != 1 || !validate(data))
                    throw new InvalidDataException("Invalid stage-1 save data.");
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
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
        if (File.Exists(FilePath))
            File.Replace(temporary, FilePath, FilePath + ".bak");
        else
            File.Move(temporary, FilePath);
    }

    public void Delete()
    {
        foreach (string path in new[] { FilePath, FilePath + ".bak", FilePath + ".tmp" })
            if (File.Exists(path)) File.Delete(path);
    }
}
