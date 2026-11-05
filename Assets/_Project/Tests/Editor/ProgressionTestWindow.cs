using System;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProgressionTestWindow : EditorWindow
{
    private const string Path = "Assets/_Project/Tests/Editor/ProgressionTest.unity";
    private const string Key = "ParcelTycoon.ProgressionTest.";
    private string report = "격리 테스트를 시작하세요. 실제 저장 데이터는 사용하지 않습니다.";
    private int step, passed, failed;
    private bool sequentialState = true;
    private ProgressionManager.GoalId lastExpectedGoal = ProgressionManager.GoalId.SecondInbound;
    private Vector2 scroll;
    private static readonly ProgressionManager.GoalId[] Expected = {
        ProgressionManager.GoalId.SecondInbound, ProgressionManager.GoalId.DeliveryWorker,
        ProgressionManager.GoalId.InputWorker, ProgressionManager.GoalId.BuyTwoUpgrades,
        ProgressionManager.GoalId.WarehouseExpansion, ProgressionManager.GoalId.AllUpgradesLevel2,
        ProgressionManager.GoalId.AnyUpgradeMax, ProgressionManager.GoalId.AllUpgradesMax,
        ProgressionManager.GoalId.Complete };

    [MenuItem("Tools/ParcelTycoon/Progression Test")]
    private static void Open() { GetWindow<ProgressionTestWindow>("성장 목표 테스트"); }

    [InitializeOnLoadMethod]
    private static void Register()
    {
        EditorApplication.playModeStateChanged -= Changed;
        EditorApplication.playModeStateChanged += Changed;
    }

    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key + "active", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            try { GetFixture(); }
            catch (Exception ex)
            {
                SessionState.SetString(Key + "error", ex.ToString());
                Debug.LogError("Progression test isolation failed: " + ex);
                EditorApplication.isPlaying = false;
            }
        }
        if (state == PlayModeStateChange.EnteredEditMode) RestoreEditor();
    }

    private static void RestoreEditor()
    {
        if (!SessionState.GetBool(Key + "active", false)) return;
        string previous = SessionState.GetString(Key + "start", "");
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        SessionState.SetBool(Key + "active", false);
    }

    private static void EndTest()
    {
        if (!SessionState.GetBool(Key + "active", false)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
        else RestoreEditor();
    }

    private void OnDestroy()
    {
        if (!EditorApplication.isCompiling && !EditorApplication.isUpdating) EndTest();
    }

    private sealed class Fixture
    {
        public ProgressionManager manager;
        public GoalUI ui;
        public TMP_Text text;
        public SortingStation sorting;
        public PlayerCarry carry;
        public WorkerSpeedUpgradeZone worker;
        public GameObject inbound, delivery, input, expansion;
        public GameObject inboundPad, deliveryPad, inputPad, sortingPad, carryPad, workerPad, expansionPad;
    }

    private static T Reference<T>(UnityEngine.Object target, string name) where T : UnityEngine.Object
    {
        var property = new SerializedObject(target).FindProperty(name);
        T reference = property == null ? null : property.objectReferenceValue as T;
        if (reference == null) throw new InvalidOperationException("Missing test reference: " + name);
        return reference;
    }

    private static Fixture GetFixture(bool batch = false)
    {
        if (!batch && (!SessionState.GetBool(Key + "active", false) || !EditorApplication.isPlaying || SceneManager.sceneCount != 1 || SceneManager.GetSceneAt(0).path != Path))
            throw new InvalidOperationException("Only the isolated ProgressionTest scene may be tested.");
        if (SaveManager.Instance != null) throw new InvalidOperationException("SaveManager.Instance remains active; no state was forced or cleared.");
        var scripts = SceneManager.GetSceneByPath(Path).GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
        if (scripts.Any(s => s is SaveManager || s is MoneyManager || s is WorkerAI || s is InputWorkerAI || s is PurchaseZone ||
            s is CarryCapacityUpgradeZone || s is SortingSpeedUpgradeZone || s is BoxSpawner || s is WarehouseExpansionEffect))
            throw new InvalidOperationException("Forbidden game/save component exists in test scene.");
        var f = new Fixture { manager = scripts.OfType<ProgressionManager>().Single(), ui = scripts.OfType<GoalUI>().Single() };
        f.text = Reference<TMP_Text>(f.ui, "goalText");
        f.sorting = Reference<SortingStation>(f.manager, "sortingStation");
        f.carry = Reference<PlayerCarry>(f.manager, "playerCarry");
        f.worker = Reference<WorkerSpeedUpgradeZone>(f.manager, "workerSpeedUpgrade");
        if (f.sorting.enabled || f.carry.enabled || f.worker.enabled)
            throw new InvalidOperationException("State-provider components must remain disabled in this test.");
        f.inbound = Reference<GameObject>(f.manager, "secondInbound");
        f.delivery = Reference<GameObject>(f.manager, "deliveryWorker");
        f.input = Reference<GameObject>(f.manager, "inputWorker");
        f.expansion = Reference<GameObject>(f.manager, "warehouseExpansion");
        f.inboundPad = Reference<GameObject>(f.manager, "secondInboundPurchaseZone");
        f.deliveryPad = Reference<GameObject>(f.manager, "deliveryWorkerHireZone");
        f.inputPad = Reference<GameObject>(f.manager, "inputWorkerHireZone");
        f.sortingPad = Reference<GameObject>(f.manager, "sortingSpeedUpgradeZone");
        f.carryPad = Reference<GameObject>(f.manager, "carryCapacityUpgradeZone");
        f.workerPad = Reference<GameObject>(f.manager, "workerSpeedUpgradeZone");
        f.expansionPad = Reference<GameObject>(f.manager, "warehouseExpansionPurchaseZone");
        // Do not allow an Inspector assignment to target a component in another scene.
        foreach (var obj in new Component[] { f.manager, f.ui, f.text, f.sorting, f.carry, f.worker })
            if (obj.gameObject.scene.path != Path) throw new InvalidOperationException("Cross-scene test reference.");
        foreach (var obj in new[] { f.inbound, f.delivery, f.input, f.expansion, f.inboundPad, f.deliveryPad, f.inputPad, f.sortingPad, f.carryPad, f.workerPad, f.expansionPad })
            if (obj.scene.path != Path) throw new InvalidOperationException("Cross-scene pad reference.");
        return f;
    }

    private static void Reset(Fixture f)
    {
        f.inbound.SetActive(false); f.delivery.SetActive(false); f.input.SetActive(false); f.expansion.SetActive(false);
        f.sorting.RestoreSortingLevel(1); f.carry.RestoreCarryLevel(1); f.worker.RestoreWorkerLevel(1);
    }

    private static void Advance(Fixture f, int index)
    {
        switch (index) {
            case 1: f.inbound.SetActive(true); break;
            case 2: f.delivery.SetActive(true); break;
            case 3: f.input.SetActive(true); break;
            case 4: f.sorting.RestoreSortingLevel(2); f.carry.RestoreCarryLevel(2); break;
            case 5: f.expansion.SetActive(true); break;
            case 6: f.worker.RestoreWorkerLevel(2); break;
            case 7: f.carry.RestoreCarryLevel(f.carry.MaxCarryLevel); break;
            case 8: f.sorting.RestoreSortingLevel(f.sorting.MaxSortingLevel); f.carry.RestoreCarryLevel(f.carry.MaxCarryLevel); f.worker.RestoreWorkerLevel(4); break;
        }
    }

    private static string Pads(Fixture f) => $"입고={f.inboundPad.activeSelf}, 출고직원={f.deliveryPad.activeSelf}, 입고직원={f.inputPad.activeSelf}, 분류={f.sortingPad.activeSelf}, 운반={f.carryPad.activeSelf}, 직원속도={f.workerPad.activeSelf}, 독립창고={f.expansionPad.activeSelf}";

    private void Check(Fixture f, string name, ProgressionManager.GoalId expected)
    {
        lastExpectedGoal = expected;
        f.manager.RefreshProgression(); f.ui.RefreshGoal();
        var actual = f.manager.CurrentGoal;
        string raw = f.manager.GetGoalText(expected);
        string core = raw.StartsWith("목표: ") ? raw.Substring(4) : raw;
        string expectedText = expected == ProgressionManager.GoalId.Complete ? core : "NEXT GOAL\n" + core;
        string actualText = Regex.Replace(f.text.text, "<[^>]*>", "").Replace("\r", "");
        string english = f.manager.GetGoalText(expected, false);
        if (english.StartsWith("Goal: ")) english = english.Substring(6);
        string fallback = expected == ProgressionManager.GoalId.Complete ? english : "NEXT GOAL\n" + english;
        bool ok = actual == expected && (actualText == expectedText || actualText == fallback);
        if (ok) passed++; else failed++;
        report += $"\n{(ok ? "PASS" : "FAIL")} [{name}] 예상 ID={expected}, 실제 ID={actual}\n예상 문구: {expectedText}\n실제 문구: {actualText}\n패드: {Pads(f)}\n";
        if (!ok) report += "원인: 목표 ID 또는 실제 GoalUI 문구 불일치 (폰트 지원 포함).\n";
    }

    private void CheckPads(Fixture f, string name, int stage)
    {
        f.manager.RefreshProgression();
        bool ok = f.inboundPad.activeSelf == (stage == 0) && f.deliveryPad.activeSelf == (stage == 1) &&
            f.inputPad.activeSelf == (stage == 2) && f.sortingPad.activeSelf == (stage >= 2) &&
            f.carryPad.activeSelf == (stage >= 1) && f.workerPad.activeSelf == (stage >= 3) &&
            f.expansionPad.activeSelf == (stage >= 3 && f.manager.PurchasedUpgradeCount >= 2 && !f.expansion.activeSelf);
        if (ok) passed++; else failed++;
        report += $"\n{(ok ? "PASS" : "FAIL")} [{name}] 패드 활성화 계약, 목표 ID={f.manager.CurrentGoal}\n{Pads(f)}\n";
        if (!ok)
        {
            report += $"예상 GoalId={lastExpectedGoal}, 실제 GoalId={f.manager.CurrentGoal}\n예상 목표 문구={f.manager.GetGoalText(lastExpectedGoal)}\n실제 UI 문구={Regex.Replace(f.text.text, "<[^>]*>", "")}\n";
            report += "원인: 기존 입고/직원 구매 순서 또는 독립 창고/업그레이드 패드 유지 조건 위반.\n";
        }
    }

    private void Run(Action<Fixture> action)
    {
        Fixture fixture = null;
        try { fixture = GetFixture(); action(fixture); }
        catch (Exception ex)
        {
            failed++;
            report += $"\nFAIL [검사 중단, 단계 {step + 1}] 예상 GoalId={lastExpectedGoal}\n";
            try
            {
                if (fixture == null) report += "실제 ID/문구/패드: 격리 검증 실패로 조회하지 않음.\n";
                else report += $"실제 GoalId={fixture.manager.CurrentGoal}\n예상 문구={fixture.manager.GetGoalText(lastExpectedGoal)}\n실제 문구={fixture.text.text}\n패드={Pads(fixture)}\n";
            }
            catch (Exception diagnostic) { report += "진단 조회 실패: " + diagnostic.Message + "\n"; }
            report += ex + "\n";
            EndTest();
        }
    }

    private void Sequential(Fixture f)
    {
        Reset(f);
        for (int i = 0; i < Expected.Length; i++)
        {
            Advance(f, i); Check(f, "순차 " + (i + 1), Expected[i]); CheckPads(f, "순차 " + (i + 1), Math.Min(i, 3));
        }
        step = 8; sequentialState = true;
    }

    private void Skips(Fixture f)
    {
        Reset(f); f.expansion.SetActive(true);
        Check(f, "창고 선구매, 초기 목표 유지", Expected[0]);
        for (int i = 1; i <= 3; i++) Advance(f, i);
        Check(f, "창고 목표 건너뛰기", ProgressionManager.GoalId.BuyTwoUpgrades);
        f.sorting.RestoreSortingLevel(3); f.carry.RestoreCarryLevel(2);
        Check(f, "분류/운반 선행 달성 건너뛰기", ProgressionManager.GoalId.AllUpgradesLevel2);
        CheckPads(f, "선행 달성에도 세 업그레이드 유지", 3);
        f.worker.RestoreWorkerLevel(4); f.sorting.RestoreSortingLevel(4); f.carry.RestoreCarryLevel(3);
        Check(f, "완료 상태 복원 시 즉시 완료", ProgressionManager.GoalId.Complete);
        f.expansion.SetActive(false);
        Check(f, "MAX지만 창고 미구매", ProgressionManager.GoalId.WarehouseExpansion);
        CheckPads(f, "창고 목표 중 세 업그레이드 유지", 3);
        step = 3; sequentialState = false;
    }

    public static void RunBatch()
    {
        if (EditorApplication.isPlaying || SaveManager.Instance != null)
            throw new InvalidOperationException("Run only in an isolated Edit Mode editor.");
        var scene = EditorSceneManager.OpenScene(Path, OpenSceneMode.Additive);
        var window = CreateInstance<ProgressionTestWindow>();
        try
        {
            var f = GetFixture(true);
            window.Sequential(f);
            Reset(f); f.inbound.SetActive(true); f.delivery.SetActive(true); f.input.SetActive(true);
            f.sorting.RestoreSortingLevel(2); window.CheckPads(f, "one upgrade stays locked", 3);
            f.sorting.RestoreSortingLevel(3); window.CheckPads(f, "same upgrade twice unlocks warehouse", 3);
            if (f.manager.PurchasedUpgradeCount != 2) throw new Exception("Derived upgrade count mismatch");
            f.input.SetActive(false); window.CheckPads(f, "missing input worker keeps warehouse locked", 2);
            window.Skips(f);
            if (window.failed != 0) throw new Exception(window.report);
            Debug.Log($"LEVELING_PASS: {window.passed} progression/Goal/pad assertions, all nine goals, same-tier repeated purchase, restored completion skips.\n" + window.report);
        }
        finally { DestroyImmediate(window); EditorSceneManager.CloseScene(scene, true); }
        ValidateMainAndLegacy();
        SortingStatusUIValidation.RunAll();
    }

    private static void ValidateMainAndLegacy()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainScene.unity", OpenSceneMode.Additive);
        try
        {
            var scripts = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            var manager = scripts.OfType<ProgressionManager>().Single();
            var warehouse = Reference<GameObject>(manager, "warehouseExpansionPurchaseZone").GetComponent<PurchaseZone>();
            if (new SerializedObject(warehouse).FindProperty("purchaseCost").intValue != 2500 ||
                new SerializedObject(scripts.OfType<MoneyManager>().Single()).FindProperty("startingMoney").intValue != 0)
                throw new Exception("MainScene money/warehouse price mismatch");
            if (scripts.OfType<WorkerAI>().Any(w => new SerializedObject(w).FindProperty("rewardPerBox").intValue != 100))
                throw new Exception("Delivery reward changed");
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
        var root = new GameObject("Legacy warehouse restore test");
        var target = new GameObject("Expansion target");
        try
        {
            var zone = root.AddComponent<PurchaseZone>();
            var data = new SerializedObject(zone);
            data.FindProperty("purchaseCost").intValue = 2500;
            data.FindProperty("targetObject").objectReferenceValue = target;
            data.ApplyModifiedPropertiesWithoutUndo();
            var completed = new PurchaseSaveData { purchased = true, paidAmount = 0 };
            if (!zone.IsValidSave(completed)) throw new Exception("Old complete save rejected");
            zone.RestoreState(completed);
            if (!target.activeSelf || root.activeSelf || !zone.CaptureState().purchased)
                throw new Exception("Old purchase completion lost");
            var partial = new PurchaseSaveData { purchased = false, paidAmount = 150 };
            zone.RestoreState(partial);
            if (target.activeSelf || zone.CaptureState().paidAmount != 150) throw new Exception("Old partial amount lost");
            var saved = new SaveData { currentMoney = 1234, warehouseExpansion = partial };
            var restored = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(saved));
            if (restored.schemaVersion != 2 || restored.currentMoney != 1234 || restored.warehouseExpansion.paidAmount != 150)
                throw new Exception("Save v2 JSON roundtrip failed");
            Debug.Log("LEVELING_SAVE_PASS: MainScene money 0, warehouse 2500, delivery reward 100; old completed and partial purchases preserved; Save v2 JSON roundtrip. No user save accessed.");
        }
        finally { DestroyImmediate(root); DestroyImmediate(target); }
    }

    private void StartTest()
    {
        if (SaveManager.Instance != null) { report = "FAIL: SaveManager.Instance가 남아 있어 시작하지 않습니다."; return; }
        if (SessionState.GetBool("ParcelTycoon.CapacityTest.active", false)) { report = "다른 테스트를 먼저 종료하세요."; return; }
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                EditorUtility.DisplayDialog("미저장 씬 변경", "변경 사항을 먼저 직접 저장하거나 정리하세요. 도구가 MainScene을 저장하거나 버리지 않습니다.", "확인");
                return;
            }
        var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Path);
        if (asset == null) { report = "FAIL: 테스트 씬을 찾을 수 없습니다."; return; }
        SessionState.SetString(Key + "start", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetString(Key + "error", "");
        SessionState.SetBool(Key + "active", true);
        EditorSceneManager.playModeStartScene = asset;
        step = passed = failed = 0; sequentialState = true; report = "신규 상태로 시작합니다. 다음 단계 또는 전체 검사를 실행하세요.";
        EditorApplication.isPlaying = true;
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("실제 목표 판정·GoalUI를 재사용합니다. 결제/저장 복원 테스트는 아닙니다.\nMainScene과 JSON은 읽거나 수정하지 않습니다.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            if (GUILayout.Button("테스트 시작")) StartTest();
        bool running = SessionState.GetBool(Key + "active", false) && EditorApplication.isPlaying;
        using (new EditorGUI.DisabledScope(!running))
        {
            if (GUILayout.Button("현재 단계 확인")) Run(f => Check(f, "현재 단계", Expected[step]));
            if (GUILayout.Button("다음 단계 진행")) Run(f => { if (!sequentialState) { Reset(f); step = 0; sequentialState = true; } else if (step < 8) Advance(f, ++step); Check(f, "단계 " + (step + 1), Expected[step]); });
            if (GUILayout.Button("전체 순차 검사")) { passed = failed = 0; report = "전체 순차 검사"; Run(Sequential); }
            if (GUILayout.Button("선행 달성 / 자동 건너뛰기 검사")) Run(Skips);
            if (GUILayout.Button("구매·업그레이드 패드 활성화 검사")) Run(f => CheckPads(f, "현재 패드", Math.Min(step, 3)));
            if (GUILayout.Button("테스트 상태 초기화")) Run(f => { Reset(f); step = 0; sequentialState = true; Check(f, "신규 상태 초기화", Expected[0]); });
        }
        if (SessionState.GetBool(Key + "active", false) && GUILayout.Button("테스트 종료 / 기존 씬 복귀")) EndTest();
        EditorGUILayout.LabelField($"PASS {passed} / FAIL {failed}");
        string isolationError = SessionState.GetString(Key + "error", "");
        if (!string.IsNullOrEmpty(isolationError)) EditorGUILayout.HelpBox(isolationError, MessageType.Error);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void Update() { if (SessionState.GetBool(Key + "active", false)) Repaint(); }
}
