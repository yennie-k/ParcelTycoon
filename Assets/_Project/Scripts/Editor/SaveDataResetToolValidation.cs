using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SaveDataResetToolValidation
{
    [MenuItem("Tools/ParcelTycoon/Validate Save Reset (Temporary Files Only)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run validation in Edit Mode.");
        string directory = Path.Combine(Path.GetTempPath(), "ParcelTycoon-ResetTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        int cases = 0;
        try
        {
            var service = new SaveFileService(directory);
            string[] active = { service.FilePath, service.FilePath + ".bak", service.FilePath + ".tmp" };
            string[] history = { service.FilePath + ".v1.bak", service.FilePath + ".v1.previous.bak" };
            foreach (string path in history) File.WriteAllText(path, "historical data");
            // All eight combinations cover none, each individual file, pairs and all three files.
            for (int mask = 0; mask < 8; mask++)
            {
                int expected = 0;
                for (int i = 0; i < active.Length; i++)
                    if ((mask & (1 << i)) != 0) { File.WriteAllText(active[i], "active save"); expected++; }
                var result = SaveDataResetTool.ResetActiveFiles(service);
                Require(!result.Blocked && result.Failures.Count == 0 && result.Deleted.Count == expected &&
                    result.Missing.Count == 3 - expected, "file combination " + mask);
                foreach (string path in active) Require(!File.Exists(path), "active file removed");
                foreach (string path in history) Require(File.ReadAllText(path) == "historical data", "history preserved");
                cases++;
            }
            foreach (string path in active) File.WriteAllText(path, "active save");
            var blocked = SaveDataResetTool.ResetActiveFiles(service, true);
            Require(blocked.Blocked && blocked.Deleted.Count == 0, "Play Mode guard");
            foreach (string path in active) Require(File.ReadAllText(path) == "active save", "guard preserves files");
            cases++;

            // A real Windows sharing violation must be reported without hiding other outcomes.
            using (var locked = new FileStream(active[1], FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var result = SaveDataResetTool.ResetActiveFiles(service);
                Require(result.Failures.Count == 1 && result.Failures[0].Contains(active[1]) &&
                    result.Deleted.Count == 2 && File.Exists(active[1]), "partial deletion/error report");
            }
            Require(SaveDataResetTool.ResetActiveFiles(service).Failures.Count == 0, "retry after unlock");
            cases++;
            var absent = new SaveFileService(Path.Combine(directory, "not-created"));
            Require(SaveDataResetTool.ResetActiveFiles(absent).Missing.Count == 3, "missing directory");
            cases++;
            foreach (string path in history) Require(File.ReadAllText(path) == "historical data", "final history preservation");
            Debug.Log($"SAVE_RESET_PASS: {cases} isolated cases; active files only, historical archives preserved, Play Mode blocked, real partial failure reported. User save directory never used.");
        }
        finally
        {
            // Remove only the known files and the uniquely-created empty test directory.
            foreach (string file in Directory.GetFiles(directory)) File.Delete(file);
            Directory.Delete(directory, false);
        }
    }

    public static void RunAll()
    {
        Run();
        ProgressionTestWindow.RunBatch();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("Save reset validation failed: " + message);
    }
}
