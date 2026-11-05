using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor-only: never calls SaveFileService.Delete(), which also removes historical v1 archives.
public static class SaveDataResetTool
{
    private const string MenuPath = "Tools/ParcelTycoon/Reset Save Data";

    [MenuItem(MenuPath)]
    private static void ShowConfirmation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("ParcelTycoon 저장 데이터 초기화",
                "Play Mode를 종료한 뒤 저장 데이터를 초기화해 주세요.", "확인");
            return;
        }
        var service = new SaveFileService(Application.persistentDataPath);
        if (!EditorUtility.DisplayDialog("ParcelTycoon 저장 데이터 초기화",
            "현재 저장된 게임 진행 기록을 삭제합니다.\n다음 플레이는 신규 게임 상태로 시작합니다.\n\n" +
            "이 작업은 되돌릴 수 없습니다.\n본 저장 파일과 .bak, .tmp만 삭제하며 v1 역사 백업은 보존합니다.\n\n" +
            service.FilePath, "초기화", "취소")) return;

        // Recheck after the modal dialog, before any filesystem operation.
        ResetResult result = ResetActiveFiles(service);
        string details = result.Describe();
        if (result.Blocked)
        {
            EditorUtility.DisplayDialog("초기화 중단", details, "확인");
            return;
        }
        if (result.Failures.Count > 0)
        {
            Debug.LogError("ParcelTycoon 저장 초기화에 실패한 파일이 있습니다.\n" + details);
            EditorUtility.DisplayDialog("저장 데이터 초기화 일부 또는 전체 실패",
                "아래 결과를 확인해 주세요. 남아 있는 .bak는 다음 실행 시 복원될 수 있습니다.\n\n" + details, "확인");
            return;
        }
        string message = result.Deleted.Count == 0 ? "초기화할 저장 데이터가 없습니다." :
            "ParcelTycoon 저장 데이터가 초기화되었습니다.\n다음 MainScene 실행은 신규 게임으로 시작합니다.";
        Debug.Log(message + "\n" + details);
        EditorUtility.DisplayDialog("저장 데이터 초기화", message, "확인");
    }

    internal sealed class ResetResult
    {
        public bool Blocked;
        public readonly List<string> Deleted = new List<string>();
        public readonly List<string> Missing = new List<string>();
        public readonly List<string> Failures = new List<string>();

        public string Describe()
        {
            if (Blocked) return "Play Mode를 종료한 뒤 저장 데이터를 초기화해 주세요.";
            return "삭제됨:\n" + string.Join("\n", Deleted) + "\n\n원래 없음:\n" +
                string.Join("\n", Missing) + "\n\n실패:\n" + string.Join("\n", Failures);
        }
    }

    // Explicit service allows isolated temporary-directory tests; production uses persistentDataPath.
    internal static ResetResult ResetActiveFiles(SaveFileService service, bool simulatePlayMode = false)
    {
        var result = new ResetResult();
        if (EditorApplication.isPlayingOrWillChangePlaymode || simulatePlayMode)
        {
            result.Blocked = true;
            return result;
        }
        if (service == null) throw new ArgumentNullException(nameof(service));
        foreach (string path in new[] { service.FilePath, service.FilePath + ".bak", service.FilePath + ".tmp" })
        {
            try
            {
                // Unlike File.Exists, GetAttributes reports access failures rather than hiding them.
                FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.Directory) != 0)
                    throw new IOException("저장 파일 경로에 디렉터리가 있습니다. 디렉터리는 삭제하지 않습니다.");
                File.Delete(path);
                result.Deleted.Add(path);
            }
            catch (FileNotFoundException) { result.Missing.Add(path); }
            catch (DirectoryNotFoundException) { result.Missing.Add(path); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
            {
                result.Failures.Add(path + "\n" + ex.GetType().Name + ": " + ex.Message);
            }
        }
        return result;
    }
}
