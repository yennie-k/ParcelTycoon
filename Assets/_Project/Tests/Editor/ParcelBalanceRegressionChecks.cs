using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// No SaveManager is instantiated. Every save/backup used here belongs to a fresh temp directory.
public static class ParcelBalanceRegressionChecks
{
    private static int passed;

    [MenuItem("Tools/ParcelTycoon/Progression Balance Checks")]
    public static void RunFromMenu()
    {
        try { Run(); }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
        passed++;
        Debug.Log("PASS: " + name);
    }

    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var settings = new SerializedObject(target);
        settings.FindProperty(field).objectReferenceValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(UnityEngine.Object target, string field, int value)
    {
        var settings = new SerializedObject(target);
        settings.FindProperty(field).intValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T Component<T>(GameObject root, string name) where T : MonoBehaviour
    {
        var child = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
        child.transform.SetParent(root.transform);
        child.SetActive(false);
        T component = child.AddComponent<T>();
        component.enabled = false;
        return component;
    }

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SaveManager.Instance != null)
            throw new InvalidOperationException("Play 모드를 종료하세요. 활성 저장 시스템이 있는 상태에서는 검사하지 않습니다.");
        passed = 0;
        Scene scene = EditorSceneManager.NewPreviewScene();
        string directory = null;
        try
        {
            var root = EditorUtility.CreateGameObjectWithHideFlags("Isolated Balance Checks", HideFlags.HideAndDontSave);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.SetActive(false);
            var manager = Component<ProgressionManager>(root, "Progression");
            var money = Component<MoneyManager>(root, "Money");
            var sorting = Component<SortingStation>(root, "Sorting");
            var carry = Component<PlayerCarry>(root, "Carry");
            var worker = Component<WorkerSpeedUpgradeZone>(root, "Worker Upgrade");
            var staffCarry = Component<WorkerCarryCapacityUpgradeZone>(root, "Staff Carry Upgrade");
            var inputAI = Component<InputWorkerAI>(root, "Input AI");
            var deliveryAI = Component<WorkerAI>(root, "Delivery AI");
            Set(worker, "inputWorker", inputAI); Set(worker, "deliveryWorker", deliveryAI);
            Set(staffCarry, "inputWorker", inputAI); Set(staffCarry, "deliveryWorker", deliveryAI);
            var carryUpgrade = Component<CarryCapacityUpgradeZone>(root, "Carry Upgrade");
            var sortingUpgrade = Component<SortingSpeedUpgradeZone>(root, "Sorting Upgrade");
            Set(carryUpgrade, "playerCarry", carry); Set(sortingUpgrade, "sortingStation", sorting);
            var inbound = EditorUtility.CreateGameObjectWithHideFlags("Inbound", HideFlags.HideAndDontSave); inbound.transform.SetParent(root.transform); inbound.SetActive(false);
            var input = inputAI.gameObject; var delivery = deliveryAI.gameObject;
            var expansion = EditorUtility.CreateGameObjectWithHideFlags("Expansion", HideFlags.HideAndDontSave); expansion.transform.SetParent(root.transform); expansion.SetActive(false);
            Set(manager, "secondInbound", inbound); Set(manager, "inputWorker", input); Set(manager, "deliveryWorker", delivery);
            Set(manager, "warehouseExpansion", expansion); Set(manager, "sortingStation", sorting);
            Set(manager, "playerCarry", carry); Set(manager, "workerSpeedUpgrade", worker);
            Set(manager, "workerCarryUpgrade", staffCarry);
            string[] fields = { "secondInboundPurchaseZone", "deliveryWorkerHireZone", "inputWorkerHireZone",
                "sortingSpeedUpgradeZone", "carryCapacityUpgradeZone", "workerSpeedUpgradeZone", "warehouseExpansionPurchaseZone" };
            var pads = new GameObject[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                pads[i] = EditorUtility.CreateGameObjectWithHideFlags(fields[i], HideFlags.HideAndDontSave); pads[i].transform.SetParent(root.transform); Set(manager, fields[i], pads[i]);
            }
            Action<int, bool> checkPads = (stage, warehouse) =>
            {
                manager.RefreshProgression();
                Check(pads[0].activeSelf == (stage == 0) && pads[1].activeSelf == (stage == 1) &&
                    pads[2].activeSelf == (stage == 2) && pads[3].activeSelf == (stage >= 2) &&
                    pads[4].activeSelf == (stage >= 1) && pads[5].activeSelf == (stage >= 3) && pads[6].activeSelf == warehouse,
                    "패드 단계 " + stage + ", 창고=" + warehouse);
            };
            Action<ProgressionManager.GoalId> goal = expected => Check(manager.CurrentGoal == expected,
                "Goal " + expected + " / 실제 " + manager.CurrentGoal);
            money.Initialize(); Check(money.CurrentMoney == 0, "MoneyManager 신규 기본 재화 0");
            money.RestoreMoney(4321); Check(money.CurrentMoney == 4321, "기존 재화 복원 유지");
            goal(ProgressionManager.GoalId.SecondInbound); checkPads(0, false);
            Check(!staffCarry.gameObject.activeSelf, "직원 운반량은 창고 확장 전 잠김");
            staffCarry.RestoreState(new UpgradeSaveData { level = 3 });
            Check(manager.PurchasedUpgradeCount == 0, "직원 운반량은 창고 해금 구매 횟수에 미포함");
            staffCarry.RestoreState(new UpgradeSaveData());
            inbound.SetActive(true); goal(ProgressionManager.GoalId.DeliveryWorker); checkPads(1, false);
            carry.RestoreCarryLevel(3); checkPads(1, false); // Two early upgrades cannot bypass automation.
            carry.RestoreCarryLevel(1);
            delivery.SetActive(true); goal(ProgressionManager.GoalId.InputWorker); checkPads(2, false);
            input.SetActive(true); goal(ProgressionManager.GoalId.BuyTwoUpgrades); checkPads(3, false);
            sorting.RestoreSortingLevel(2); Check(manager.PurchasedUpgradeCount == 1, "1회 구매 계산"); checkPads(3, false);
            sorting.RestoreSortingLevel(3); Check(manager.PurchasedUpgradeCount == 2, "같은 종류 2회 구매 계산"); checkPads(3, true);
            sorting.RestoreSortingLevel(2); carry.RestoreCarryLevel(2);
            Check(manager.PurchasedUpgradeCount == 2, "다른 종류 2회 구매 계산");
            goal(ProgressionManager.GoalId.WarehouseExpansion); checkPads(3, true);
            expansion.SetActive(true); goal(ProgressionManager.GoalId.AllUpgradesLevel2); checkPads(3, false);
            Check(staffCarry.gameObject.activeSelf, "창고 확장 후 직원 운반량 해금");
            worker.RestoreWorkerLevel(2); goal(ProgressionManager.GoalId.AllUpgradesLevel2);
            staffCarry.RestoreState(new UpgradeSaveData { level = 2 }); goal(ProgressionManager.GoalId.AnyUpgradeMax);
            staffCarry.RestoreState(new UpgradeSaveData { level = 3 }); goal(ProgressionManager.GoalId.AllUpgradesMax);
            staffCarry.RestoreState(new UpgradeSaveData { level = 2 });
            carry.RestoreCarryLevel(3); sorting.RestoreSortingLevel(4); worker.RestoreWorkerLevel(4);
            goal(ProgressionManager.GoalId.AllUpgradesMax);
            staffCarry.RestoreState(new UpgradeSaveData { level = 3 }); goal(ProgressionManager.GoalId.Complete);
            expansion.SetActive(false); goal(ProgressionManager.GoalId.WarehouseExpansion);
            expansion.SetActive(true);
            sorting.RestoreSortingLevel(1); carry.RestoreCarryLevel(1); worker.RestoreWorkerLevel(1);
            goal(ProgressionManager.GoalId.BuyTwoUpgrades); checkPads(3, false); // Old purchased expansion remains purchased.

            for (int level = 1; level <= 4; level++)
            {
                sorting.RestoreSortingLevel(level); worker.RestoreWorkerLevel(level);
                Check(Mathf.Abs(sorting.SortingTime - (2f - (level - 1) * .4f)) < .0001f, "분류 능력치 Lv." + level);
                Check(Mathf.Abs(inputAI.MoveSpeed - (3f + (level - 1) * .75f)) < .0001f &&
                    Mathf.Abs(deliveryAI.MoveSpeed - inputAI.MoveSpeed) < .0001f, "양 직원 속도 Lv." + level);
            }
            for (int level = 1; level <= 3; level++)
            {
                carry.RestoreCarryLevel(level); Check(carry.MaxCarryCount == level + 2, "운반 능력치 Lv." + level);
            }
            var effect = Component<WarehouseExpansionEffect>(root, "Expansion Effect");
            Set(effect, "warehouseExpansion", expansion); Set(effect, "sortingStation", sorting);
            var apply = typeof(WarehouseExpansionEffect).GetMethod("ApplyCapacity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            expansion.SetActive(false); apply.Invoke(effect, null);
            Check(sorting.MaxInputCount == 5 && sorting.MaxOutputCount == 5, "미확장 5/5");
            expansion.SetActive(true); apply.Invoke(effect, null); apply.Invoke(effect, null);
            Check(sorting.MaxInputCount == 10 && sorting.MaxOutputCount == 10, "확장 반복 적용 10/10");

            var purchase = Component<PurchaseZone>(root, "Expansion Purchase");
            Set(purchase, "targetObject", expansion); Set(purchase, "moneyManager", money); SetInt(purchase, "purchaseCost", 2500);
            foreach (int paid in new[] { 0, 50, 250, 300 })
            {
                var state = new PurchaseSaveData { purchased = false, paidAmount = paid };
                Check(purchase.IsValidSave(state), "기존 확장 부분 결제 유효 " + paid);
                purchase.RestoreState(state);
                Check(!expansion.activeSelf && purchase.CaptureState().paidAmount == paid && money.CurrentMoney == 4321,
                    "기존 결제액/재화 유지 " + paid);
            }
            purchase.RestoreState(new PurchaseSaveData { purchased = true });
            Check(expansion.activeSelf && !purchase.gameObject.activeSelf && purchase.CaptureState().purchased &&
                purchase.CaptureState().paidAmount == 0 && money.CurrentMoney == 4321, "300원에 구매한 기존 확장 재결제 없음");

            Func<SaveData, bool> valid = data => data.currentMoney >= 0 && purchase.IsValidSave(data.warehouseExpansion) &&
                purchase.IsValidSave(data.secondInbound) && purchase.IsValidSave(data.inputWorker) && purchase.IsValidSave(data.deliveryWorker) &&
                carryUpgrade.IsValidSave(data.carryUpgrade) && sortingUpgrade.IsValidSave(data.sortingUpgrade) && worker.IsValidSave(data.workerSpeedUpgrade) &&
                staffCarry.IsValidSave(data.workerCarryUpgrade);
            directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ParcelBalance-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var service = new SaveFileService(directory);
            var dataV2 = new SaveData { schemaVersion = 2, currentMoney = 4321, secondInbound = new PurchaseSaveData { purchased = true },
                inputWorker = new PurchaseSaveData { purchased = true }, deliveryWorker = new PurchaseSaveData { purchased = true },
                warehouseExpansion = new PurchaseSaveData { paidAmount = 250 },
                carryUpgrade = new UpgradeSaveData { level = 2, paidAmount = 100 },
                sortingUpgrade = new UpgradeSaveData { level = 3, paidAmount = 50 },
                workerSpeedUpgrade = new UpgradeSaveData { level = 2, paidAmount = 150 } };
            service.Write(dataV2); SaveData loaded = service.Load(valid);
            Check(loaded.schemaVersion == 3 && loaded.workerCarryUpgrade.level == 1 && loaded.workerCarryUpgrade.paidAmount == 0 &&
                loaded.currentMoney == 4321 && loaded.warehouseExpansion.paidAmount == 250 && loaded.carryUpgrade.paidAmount == 100 &&
                loaded.sortingUpgrade.level == 3 && loaded.workerSpeedUpgrade.level == 2, "v2 재화/구매/레벨/부분 결제 왕복");
            purchase.RestoreState(loaded.warehouseExpansion); carryUpgrade.RestoreState(loaded.carryUpgrade);
            sortingUpgrade.RestoreState(loaded.sortingUpgrade); worker.RestoreState(loaded.workerSpeedUpgrade);
            Check(purchase.CaptureState().paidAmount == 250 && carry.MaxCarryCount == 4 &&
                Mathf.Abs(sorting.SortingTime - 1.2f) < .0001f && Mathf.Abs(inputAI.MoveSpeed - 3.75f) < .0001f,
                "v2 실제 능력치/부분 결제 복원");
            dataV2.warehouseExpansion = new PurchaseSaveData { purchased = true }; service.Write(dataV2);
            purchase.RestoreState(service.Load(valid).warehouseExpansion);
            Check(expansion.activeSelf && !purchase.gameObject.activeSelf, "기존 구매 v2 왕복 후 확장 완료 유지");
            string validJson = JsonUtility.ToJson(dataV2);
            string incompleteV2 = validJson.Replace("\"carryUpgrade\":{\"level\":2,\"paidAmount\":100}", "\"carryUpgrade\":null");
            Check(incompleteV2 != validJson, "손상 v2 테스트 구성");
            File.WriteAllText(service.FilePath, incompleteV2);
            Check(new SaveFileService(directory).Load(valid).warehouseExpansion.paidAmount == 250, "손상 v2 본 파일 → 정상 백업 복구");
            string v1 = "{\"schemaVersion\":1,\"currentMoney\":7654,\"progressionStage\":4,\"secondInbound\":{\"purchased\":true,\"paidAmount\":0}," +
                "\"warehouseExpansion\":{\"purchased\":true,\"paidAmount\":0},\"inputWorker\":{\"purchased\":true,\"paidAmount\":0},\"deliveryWorker\":{\"purchased\":true,\"paidAmount\":0}}";
            File.WriteAllText(service.FilePath, v1);
            var legacy = new SaveFileService(directory); SaveData migrated = legacy.Load(valid);
            Check(migrated.schemaVersion == 3 && migrated.workerCarryUpgrade.level == 1 && migrated.workerCarryUpgrade.paidAmount == 0 && migrated.currentMoney == 7654 && migrated.warehouseExpansion.purchased &&
                migrated.carryUpgrade.level == 1 && migrated.sortingUpgrade.level == 1 && migrated.workerSpeedUpgrade.level == 1, "v1 → v3 신규 업그레이드 기본값");
            legacy.Write(migrated);
            Check(File.ReadAllText(service.FilePath + ".v1.bak") == v1 && new SaveFileService(directory).Load(valid).currentMoney == 7654,
                "v1 원본 백업 및 변환 후 재로드");
            File.WriteAllText(service.FilePath, incompleteV2); File.WriteAllText(service.FilePath + ".bak", incompleteV2);
            bool refused = false;
            try { new SaveFileService(directory).Load(valid); } catch (InvalidDataException) { refused = true; }
            Check(refused && File.ReadAllText(service.FilePath) == incompleteV2, "본 파일/백업 손상 시 원본 보존");
            loaded.workerCarryUpgrade = new UpgradeSaveData { level = 2, paidAmount = 650 };
            service.Write(loaded);
            var v3 = service.Load(valid);
            staffCarry.RestoreState(v3.workerCarryUpgrade);
            Check(staffCarry.CurrentLevel == 2 && staffCarry.CurrentCost == 4000 &&
                staffCarry.CaptureState().paidAmount == 650 && inputAI.MaxCarryCount == 2 && deliveryAI.MaxCarryCount == 2,
                "v3 양 직원 능력치와 부분 결제 복원");
            v3.workerCarryUpgrade = new UpgradeSaveData { level = 3 };
            service.Write(v3); staffCarry.RestoreState(service.Load(valid).workerCarryUpgrade);
            Check(!staffCarry.CanUpgrade && inputAI.MaxCarryCount == 3 && deliveryAI.MaxCarryCount == 3, "v3 MAX 복원");
            string incompleteV3 = JsonUtility.ToJson(v3).Replace("\"workerCarryUpgrade\":{\"level\":3,\"paidAmount\":0}", "\"workerCarryUpgrade\":{\"level\":3}");
            File.WriteAllText(service.FilePath, incompleteV3);
            Check(service.Load(valid).workerCarryUpgrade.level == 2, "불완전한 v3 직원 운반량 → 정상 백업 복구");
            string future = JsonUtility.ToJson(v3).Replace("\"schemaVersion\":3", "\"schemaVersion\":4");
            File.WriteAllText(service.FilePath, future);
            bool futureRefused = false;
            try { service.Load(valid); } catch (NotSupportedException) { futureRefused = true; }
            Check(futureRefused && File.ReadAllText(service.FilePath) == future, "미래 버전과 정상 백업 보호");
            // Fresh-game balances use only the two real ₩100 reward sources.
            var directDelivery = Component<DeliveryZone>(root, "Player delivery");
            int playerReward = new SerializedObject(directDelivery).FindProperty("rewardPerBox").intValue;
            int workerReward = new SerializedObject(deliveryAI).FindProperty("rewardPerBox").intValue;
            Check(playerReward == 100 && workerReward == 100, "신규 게임 기본 출고 보상 ₩100");
            foreach (int price in new[] { 300, 500, 1000, 2500 })
            {
                SetInt(purchase, "purchaseCost", price);
                purchase.RestoreState(new PurchaseSaveData());
                CheckPayments(money, purchase, "PayForPurchase", price, () => purchase.CaptureState().purchased);
            }
            carryUpgrade.RestoreState(new UpgradeSaveData());
            CheckPayments(money, carryUpgrade, "PayForUpgrade", 800, () => carry.CarryLevel == 2);
            CheckPayments(money, carryUpgrade, "PayForUpgrade", 1600, () => carry.CarryLevel == 3);
            sortingUpgrade.RestoreState(new UpgradeSaveData());
            CheckPayments(money, sortingUpgrade, "PayForUpgrade", 500, () => sorting.SortingLevel == 2);
            CheckPayments(money, sortingUpgrade, "PayForUpgrade", 1000, () => sorting.SortingLevel == 3);
            CheckPayments(money, sortingUpgrade, "PayForUpgrade", 2000, () => sorting.SortingLevel == 4);
            worker.RestoreState(new UpgradeSaveData());
            CheckPayments(money, worker, "PayForUpgrade", 1000, () => worker.WorkerSpeedLevel == 2);
            CheckPayments(money, worker, "PayForUpgrade", 2000, () => worker.WorkerSpeedLevel == 3);
            CheckPayments(money, worker, "PayForUpgrade", 4000, () => worker.WorkerSpeedLevel == 4);
            staffCarry.RestoreState(new UpgradeSaveData());
            CheckPayments(money, staffCarry, "PayForUpgrade", 2000, () => staffCarry.CurrentLevel == 2);
            CheckPayments(money, staffCarry, "PayForUpgrade", 4000, () => staffCarry.CurrentLevel == 3);
            Debug.Log("ECONOMY_100_PASS: all 8 purchase pads and 14 price tiers retain whole ₩100 balances.");
            CheckMainScene();
            Debug.Log("ParcelTycoon 레벨링 검사 완료: PASS " + passed + " / FAIL 0. 실제 저장 경로 접근 없음.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static void CheckPayments(MoneyManager money, MonoBehaviour zone, string method, int price, Func<bool> complete)
    {
        Set(zone, "moneyManager", money);
        var data = new SerializedObject(zone);
        Check(data.FindProperty("paymentPerTick").intValue == 100 &&
            Mathf.Approximately(data.FindProperty("paymentInterval").floatValue, 0.2f), $"{zone.name} 기본 결제 단위");
        money.RestoreMoney(0);
        var routine = (IEnumerator)zone.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(zone, null);
        bool valid = routine.MoveNext() && money.CurrentMoney == 0 && !complete();
        for (int paid = 0; valid && paid < price; paid += 100)
        {
            bool last = paid + 100 == price;
            money.AddMoney(100);
            if (last) { money.AddMoney(100); money.AddMoney(100); }
            int before = money.CurrentMoney;
            bool continuing = routine.MoveNext();
            valid = before - money.CurrentMoney == 100 && money.CurrentMoney % 100 == 0 &&
                continuing == !last && complete() == last;
        }
        (routine as IDisposable)?.Dispose();
        Check(valid && money.CurrentMoney == 200, $"{zone.name} ₩{price:N0} 실제 100원씩 결제 후 ₩200 잔액");
    }

    private static void CheckMainScene()
    {
        string main = File.ReadAllText("Assets/_Project/Scenes/MainScene.unity");
        Check(Regex.IsMatch(main, @"^  startingMoney: 0\r?$", RegexOptions.Multiline), "MainScene 신규 재화 0");
        Match purchase = Regex.Match(main, @"--- !u!114 &473666158\r?\n.*?(?=--- !u!|\z)", RegexOptions.Singleline);
        Check(purchase.Success && purchase.Value.Contains("  purchaseCost: 2500"), "MainScene 창고 가격 2500");
        Check(main.Contains("  warehouseExpansionPurchaseZone: {fileID: 473666156}"), "MainScene 창고 패드 연결");
        Check(Regex.Matches(main, @"^  paymentPerTick: 100\r?$", RegexOptions.Multiline).Count == 8 &&
            Regex.Matches(main, @"^  paymentInterval: 0\.2\r?$", RegexOptions.Multiline).Count == 8,
            "MainScene 8개 패드의 결제 단위");
        Check(Regex.Matches(main, @"^  rewardPerBox: 100\r?$", RegexOptions.Multiline).Count == 2,
            "MainScene 직접/자동 출고 보상 ₩100");
    }
}
