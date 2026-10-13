using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Entire tool lives in the Editor assembly. No test component is attached to the scene.
public class SortingCapacityTestWindow : EditorWindow
{
    private const string ScenePath = "Assets/_Project/Tests/Editor/SortingCapacityTest.unity";
    private const string PrefabPath = "Assets/_Project/Prefabs/ParcelBox.prefab";
    private const string SessionKey = "ParcelTycoon.CapacityTest.";
    private string result = "테스트 시작을 누르세요. 실제 저장 파일은 사용하지 않습니다.";
    private bool observingResume;

    [MenuItem("Tools/ParcelTycoon/Sorting Capacity Test")]
    private static void Open() { GetWindow<SortingCapacityTestWindow>("적재 용량 테스트"); }

    [InitializeOnLoadMethod]
    private static void Register()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey + "active", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SortingStation station = SandboxStation();
            if (station == null)
            {
                Debug.LogError("Capacity test isolation check failed. Stopping Play mode.");
                EditorApplication.isPlaying = false;
                return;
            }
            station.enabled = false; // No sorting coroutine has been started yet.
            station.SetStorageCapacity(10, 10);
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            string previous = SessionState.GetString(SessionKey + "startScene", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
            SessionState.SetBool(SessionKey + "active", false);
            // Play mode returns to the original edit-time scene setup automatically.
        }
    }

    private static SortingStation SandboxStation()
    {
        if (!EditorApplication.isPlaying || SceneManager.sceneCount != 1) return null;
        Scene scene = SceneManager.GetSceneAt(0);
        if (scene.path != ScenePath) return null;
        var scripts = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
        if (scripts.Any(s => s is SaveManager || s is MoneyManager || s is WorkerAI || s is InputWorkerAI ||
            s is PurchaseZone || s is CarryCapacityUpgradeZone || s is SortingSpeedUpgradeZone || s is WorkerSpeedUpgradeZone)) return null;
        return scripts.OfType<SortingStation>().SingleOrDefault();
    }

    private static Transform Point(SortingStation station, string name)
    {
        return station.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("MainScene을 열거나 저장하지 않고 별도 씬에서 Play합니다.\n상자는 기존 TryAddInputBox / 분류 / PickUp API로 처리합니다.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            if (GUILayout.Button("테스트 시작 (격리 씬 Play)")) StartTest();
        SortingStation station = SandboxStation();
        if (station != null)
        {
            EditorGUILayout.LabelField($"INPUT {station.InputCount}/{station.MaxInputCount}   OUTPUT {station.OutputCount}/{station.MaxOutputCount}");
            EditorGUILayout.LabelField(station.IsBusy ? "분류 중" : station.enabled ? "분류 가능 / 출력 포화 시 대기" : "분류 정지");
            if (GUILayout.Button("1. 입력을 10개까지 적재")) FillInput(station);
            if (GUILayout.Button("2. 11번째 입력 시도")) TryExtra(station);
            if (GUILayout.Button("3. 정상 분류 시작")) { station.enabled = true; result = "기본 2초 × 10개 분류를 기다리세요."; }
            if (GUILayout.Button("4. 출력 10개 / 포화 대기 확인"))
                result = station.OutputCount == 10 && !station.IsBusy ? "출력 10개, 분류 정지 확인. 입력을 다시 10개 채워 포화를 관찰하세요." : "아직 출력 10개 대기 상태가 아닙니다.";
            if (GUILayout.Button("5. 첫 열 출력 1개 회수 + 재개 관찰")) TakeOutput(station);
            if (GUILayout.Button("6. 두 열 재정렬 검사")) CheckLayout(station);
            if (GUILayout.Button("7. 테스트 상태 초기화 (테스트 씬만 재로드)"))
            {
                observingResume = false;
                EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                result = "새 테스트 씬을 불러왔습니다. 다음 프레임에 용량 10, 분류 정지를 적용합니다.";
                EditorApplication.delayCall += PrepareReset;
            }
        }
        if (SessionState.GetBool(SessionKey + "active", false) && GUILayout.Button("테스트 종료 / 기존 씬으로 복귀")) EditorApplication.isPlaying = false;
        EditorGUILayout.HelpBox(result, MessageType.None);
    }

    private void StartTest()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                EditorUtility.DisplayDialog("저장되지 않은 씬 변경", "씬 변경 사항을 먼저 직접 저장하거나 정리한 후 다시 시작하세요. 도구가 대신 저장하거나 버리지 않습니다.", "확인");
                return;
            }
        SceneAsset test = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (test == null) { result = "테스트 씬을 찾을 수 없습니다."; return; }
        SessionState.SetString(SessionKey + "startScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool(SessionKey + "active", true);
        EditorSceneManager.playModeStartScene = test;
        observingResume = false;
        EditorApplication.isPlaying = true;
    }

    private static void PrepareReset()
    {
        var station = SandboxStation();
        if (station == null) return;
        station.enabled = false;
        station.SetStorageCapacity(10, 10);
    }

    private ParcelBox CreateBox()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new InvalidOperationException("ParcelBox prefab is missing.");
        return Instantiate(prefab).GetComponent<ParcelBox>();
    }

    private void FillInput(SortingStation station)
    {
        int added = 0;
        while (station.InputCount < 10)
        {
            ParcelBox box = CreateBox();
            if (!station.TryAddInputBox(box)) { Destroy(box.gameObject); break; }
            added++;
        }
        result = $"{added}개 추가, 입력 {station.InputCount}/10. 첫 관찰 후 분류 시작을 누르세요.";
    }

    private void TryExtra(SortingStation station)
    {
        if (station.InputCount != 10) { result = "먼저 입력을 10개까지 채우세요."; return; }
        ParcelBox box = CreateBox();
        bool accepted = station.TryAddInputBox(box);
        if (!accepted) Destroy(box.gameObject);
        result = !accepted && station.InputCount == 10 ? "PASS: 11번째 거부, 입력 10개 유지." : "FAIL: 11번째 상자를 받았습니다.";
    }

    private void TakeOutput(SortingStation station)
    {
        Transform output = Point(station, "OutputPoint");
        ParcelBox box = output.GetComponentsInChildren<ParcelBox>().Where(b => b.IsAvailable).OrderBy(b => b.transform.position.y).FirstOrDefault();
        if (box == null) { result = "회수할 출력 상자가 없습니다."; return; }
        var holder = new GameObject("TestCarryPoint");
        holder.transform.position = new Vector3(10, 1, -3);
        box.PickUp(holder.transform, Vector3.zero);
        observingResume = station.InputCount > 0 && station.enabled;
        result = "PickUp으로 1개 회수했습니다. 재정렬과 분류 재개를 관찰하세요.";
    }

    private void CheckLayout(SortingStation station)
    {
        bool valid = CheckPoint(Point(station, "InputPoint")) && CheckPoint(Point(station, "OutputPoint"));
        result = valid ? "PASS: 두 열(열당 5개, 높이 간격 1.05, Z 간격 1.5) 배치 확인." : "FAIL: 배치가 예상과 다릅니다. 다음 프레임 이후 다시 검사하세요.";
    }

    private static bool CheckPoint(Transform point)
    {
        var boxes = point.GetComponentsInChildren<ParcelBox>().Where(b => b.transform.parent == point).OrderBy(b => b.transform.position.z).ThenBy(b => b.transform.position.y).ToArray();
        for (int i = 0; i < boxes.Length; i++)
        {
            Vector3 expected = point.position + Vector3.up * ((i % 5) * 1.05f) + new Vector3(0, 0, (i / 5) * 1.5f);
            if (Vector3.Distance(expected, boxes[i].transform.position) > 0.01f) return false;
        }
        return boxes.Length <= 10;
    }

    private void Update()
    {
        SortingStation station = SandboxStation();
        if (station == null) return;
        foreach (var text in station.GetComponentsInChildren<TMP_Text>())
        {
            bool input = text.name == "InputCapacityText";
            if (!input && text.name != "OutputCapacityText") continue;
            int count = input ? station.InputCount : station.OutputCount;
            int max = input ? station.MaxInputCount : station.MaxOutputCount;
            text.text = $"<size=65%>{(input ? "INPUT" : "OUTPUT")}</size>\n<b>{count}</b> / {max}";
            text.outlineWidth = 0.18f;
            text.outlineColor = Color.black;
        }
        if (observingResume && station.IsBusy) { result = "PASS: 출력 공간이 생긴 뒤 정상 분류가 재개되었습니다."; observingResume = false; }
        Repaint();
    }
}
