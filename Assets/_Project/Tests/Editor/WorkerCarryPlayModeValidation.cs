using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Batch entry point using the save-free progression fixture. Never opens MainScene in Play Mode.
public static class WorkerCarryPlayModeValidation
{
    private const string ScenePath = "Assets/_Project/Tests/Editor/ProgressionTest.unity";
    private const string Pending = "ParcelTycoon.WorkerCarry.PlayValidation";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run in an isolated batch Editor without -quit.");
        EditorSceneManager.OpenScene(ScenePath);
        if (UnityEngine.Object.FindObjectsByType<SaveManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0)
            throw new InvalidOperationException("A save manager is present in the test scene.");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void Register() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.EraseBool(Pending);
        Application.targetFrameRate = 60;
        var exercise = ExerciseWorkers();
        int previousFrame = -1;
        double timeout = EditorApplication.timeSinceStartup + 30;
        EditorApplication.update += Tick;

        void Tick()
        {
            try
            {
                Check(EditorApplication.timeSinceStartup < timeout, "timed Play validation did not finish");
                Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path == ScenePath &&
                    SaveManager.Instance == null, "isolated Play scene");
                if (Time.frameCount == previousFrame) return;
                previousFrame = Time.frameCount;
                if (exercise.MoveNext()) return;
                EditorApplication.update -= Tick;
                Debug.Log("WORKER_CARRY_PLAY_PASS: real-time pickup intervals, both workers collect 1/2/3, input waiting/transfer, deadline, partial payment and ₩100 per delivery.");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                EditorApplication.update -= Tick;
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }
    }

    private static IEnumerator ExerciseWorkers()
    {
        var root = new GameObject("Worker Carry isolated Play fixture");
        Transform Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform);
            return child.transform;
        }
        var input = Child("Input").gameObject.AddComponent<InputWorkerAI>();
        var delivery = Child("Delivery").gameObject.AddComponent<WorkerAI>();
        var station = Child("Station").gameObject.AddComponent<SortingStation>();
        station.enabled = false;
        // Step the real AI methods once per Play frame, without autonomous target selection.
        input.enabled = false;
        delivery.enabled = false;
        var inputPoint = Child("Input Point");
        var outputPoint = Child("Output Point");
        var waiting = Child("Waiting Point");
        var sortingTarget = Child("Sorting Target");
        var wallet = Child("Purchase Wallet").gameObject.AddComponent<MoneyManager>();
        var revenue = Child("Delivery Revenue").gameObject.AddComponent<MoneyManager>();
        var zone = Child("Staff Carry").gameObject.AddComponent<WorkerCarryCapacityUpgradeZone>();
        Set(input, "carryPoint", Child("Input Carry")); Set(input, "sortingStation", station);
        Set(input, "waitingPoint", waiting); Set(input, "sortingZoneTarget", sortingTarget);
        Set(delivery, "carryPoint", Child("Delivery Carry")); Set(delivery, "moneyManager", revenue);
        Set(station, "inputStackAnchorA", Child("Stack A")); Set(station, "inputStackAnchorB", Child("Stack B"));
        Set(station, "inputPoint", inputPoint); Set(station, "outputPoint", outputPoint);
        Set(zone, "inputWorker", input); Set(zone, "deliveryWorker", delivery); Set(zone, "moneyManager", wallet);

        wallet.RestoreMoney(6000);
        zone.RestoreState(new UpgradeSaveData());
        Check(zone.CurrentLevel == 1 && zone.CurrentCost == 2000, "initial tier");
        var notSorted = Child("Rejected unsorted output").gameObject.AddComponent<ParcelBox>();
        Set(delivery, "targetBox", notSorted); Call(delivery, "PickUpBox");
        Check(delivery.CarryCount == 0 && notSorted.IsAvailable, "delivery rejects unsorted box");
        foreach (var frame in CheckCollection(1, 5)) yield return frame;
        var partial = (IEnumerator)Call(zone, "PayForUpgrade");
        for (int i = 0; i < 7; i++) Check(partial.MoveNext(), "partial payment tick");
        Call(zone, "StopPayment");
        (partial as IDisposable)?.Dispose();
        Check(zone.CurrentLevel == 1 && zone.CaptureState().paidAmount == 700 && wallet.CurrentMoney == 5300,
            "exit keeps 700 paid without applying the upgrade");
        FinishPayment(zone);
        Check(zone.CurrentLevel == 2 && zone.CurrentCost == 4000 && wallet.CurrentMoney == 4000, "first purchase");
        foreach (var frame in CheckCollection(2, 5)) yield return frame;
        FinishPayment(zone);
        Check(zone.CurrentLevel == 3 && !zone.CanUpgrade && wallet.CurrentMoney == 0, "second purchase/MAX");
        foreach (var frame in CheckCollection(3, 5)) yield return frame;
        foreach (var frame in CheckCollection(3, 2)) yield return frame;
        foreach (var frame in CheckDeadline()) yield return frame;

        ParcelBox Candidate(MonoBehaviour worker, bool sorted)
        {
            var box = Child(sorted ? "Sorted" : "Unsorted").gameObject.AddComponent<ParcelBox>();
            Vector3 position = worker.transform.position + Vector3.right * 0.7f;
            if (sorted) box.CompleteSorting(outputPoint, position);
            else box.transform.position = position;
            Set(worker, "targetBox", box);
            return box;
        }

        IEnumerable CheckCollection(int capacity, int inputSlots)
        {
            Check(input.MaxCarryCount == capacity && delivery.MaxCarryCount == capacity, "both capacities");
            station.SetStorageCapacity(inputSlots, 5);
            int accepted = Mathf.Min(capacity, inputSlots);
            float lastInput = 0f, lastDelivery = 0f;
            for (int i = 0; i < capacity; i++)
            {
                int inputBefore = input.CarryCount;
                int deliveryBefore = delivery.CarryCount;
                var inputBox = Candidate(input, false);
                var outputBox = Candidate(delivery, true);
                if (i == 1 && capacity == 3 && inputSlots == 5)
                {
                    var otherOwner = Child("Other owner");
                    Check(inputBox.TryPickUp(otherOwner, Vector3.zero) &&
                        outputBox.TryPickUp(otherOwner, Vector3.zero), "competing pickup setup");
                    Call(input, "MoveToBox"); Call(delivery, "MoveToBox");
                    Check(input.CarryCount == inputBefore && delivery.CarryCount == deliveryBefore &&
                        inputBox.IsCarriedBy(otherOwner) && outputBox.IsCarriedBy(otherOwner),
                        "lost targets rejected during cooldown");
                    Candidate(input, false); Candidate(delivery, true);
                }
                Vector3 inputStart = input.transform.position;
                Vector3 deliveryStart = delivery.transform.position;
                Call(input, "MoveToBox"); Call(delivery, "MoveToBox");
                if (i == 0)
                {
                    Check(input.CarryCount == 1 && delivery.CarryCount == 1, "empty hands pick up immediately");
                    lastInput = lastDelivery = Time.time;
                    continue;
                }
                Check(input.CarryCount == inputBefore && delivery.CarryCount == deliveryBefore,
                    "additional boxes cannot be picked up in the same frame");
                bool inputPending = i < accepted;
                bool deliveryPending = true;
                bool inputMoved = false, deliveryMoved = false;
                while (inputPending || deliveryPending)
                {
                    yield return null;
                    if (inputPending)
                    {
                        Call(input, "MoveToBox");
                        if (input.CarryCount > inputBefore)
                        {
                            Check(Time.time - lastInput >= 0.299f, "input pickup interval");
                            Debug.Log($"Input pickup gap: {Time.time - lastInput:F3}s");
                            lastInput = Time.time;
                            inputPending = false;
                        }
                        else inputMoved |= input.transform.position != inputStart;
                    }
                    if (deliveryPending)
                    {
                        Call(delivery, "MoveToBox");
                        if (delivery.CarryCount > deliveryBefore)
                        {
                            Check(Time.time - lastDelivery >= 0.299f, "delivery pickup interval");
                            Debug.Log($"Delivery pickup gap: {Time.time - lastDelivery:F3}s");
                            lastDelivery = Time.time;
                            deliveryPending = false;
                        }
                        else deliveryMoved |= delivery.transform.position != deliveryStart;
                    }
                }
                Check((i >= accepted || inputMoved) && deliveryMoved, "workers can approach targets during cooldown");
            }
            Candidate(input, false); Call(input, "PickUpBox");
            Candidate(delivery, true); Call(delivery, "PickUpBox");
            Check(input.CarryCount == accepted && delivery.CarryCount == capacity, "actual pickup limits");
            int transferred = 0;
            if (capacity == 3 && inputSlots == 5)
            {
                var blockers = new List<ParcelBox>();
                for (int i = 0; i < inputSlots; i++)
                {
                    var box = Child("Input blocker").gameObject.AddComponent<ParcelBox>();
                    Check(station.TryAddInputBox(box), "fill input while worker carries cargo");
                    blockers.Add(box);
                }
                input.enabled = true;
                Check(!input.TryTransferCarriedBox(station) && input.CarryCount == accepted, "full input preserves cargo");
                input.enabled = false;
                Vector3 start = input.transform.position;
                waiting.position = start + Vector3.forward * 3;
                sortingTarget.position = start - Vector3.forward * 3;
                Call(input, "MoveToSortingZone");
                Check(input.transform.position.z > start.z, "full input sends worker to waiting point");
                blockers[0].transform.SetParent(root.transform);
                station.SetStorageCapacity(inputSlots, 5);
                start = input.transform.position;
                Call(input, "MoveToSortingZone");
                Check(input.transform.position.z < start.z, "opened input sends worker back to sorting");
                input.enabled = true;
                Check(input.TryTransferCarriedBox(station) && input.CarryCount == accepted - 1, "transfer resumes once space opens");
                transferred++;
                foreach (var box in blockers) box.transform.SetParent(root.transform);
                station.SetStorageCapacity(inputSlots, 5);
            }
            input.enabled = true;
            for (int i = transferred; i < accepted; i++)
                Check(input.TryTransferCarriedBox(station), "accepted input transfer");
            input.enabled = false;
            Check(input.CarryCount == 0 && station.InputCount == accepted, "all collected inputs transferred");
            int before = revenue.CurrentMoney;
            Call(delivery, "DeliverBox"); Call(delivery, "DeliverBox");
            Check(delivery.CarryCount == 0 && revenue.CurrentMoney - before == 100 * capacity, "delivery rewards paid once");
            while (inputPoint.childCount > 0) inputPoint.GetChild(0).SetParent(root.transform);
        }

        IEnumerable CheckDeadline()
        {
            station.SetStorageCapacity(5, 5);
            // A longer Inspector interval makes it possible to exercise a deadline during cooldown.
            Set(input, "pickupInterval", 2f); Set(delivery, "pickupInterval", 2f);
            Candidate(input, false); Call(input, "PickUpBox");
            Candidate(delivery, true); Call(delivery, "PickUpBox");
            float started = Time.time;
            Candidate(input, false); Candidate(delivery, true);
            while (Time.time < started + 1.5f) yield return null;
            Call(input, "MoveToBox"); Call(delivery, "MoveToBox");
            Check(input.CarryCount == 1 && delivery.CarryCount == 1 &&
                Read(input, "currentState").ToString() == "MovingToSorting" &&
                Read(delivery, "currentState").ToString() == "MovingToDelivery",
                "collection deadline exits even during cooldown");
        }
    }

    private static void FinishPayment(WorkerCarryCapacityUpgradeZone zone)
    {
        var routine = (IEnumerator)Call(zone, "PayForUpgrade");
        int ticks = 0;
        while (routine.MoveNext()) Check(++ticks <= 80, "payment completed within configured ticks");
    }

    private static object Read(object obj, string field) => obj.GetType().GetField(field, Private).GetValue(obj);
    private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Private).SetValue(obj, value);
    private static object Call(object obj, string method) => obj.GetType().GetMethod(method, Private).Invoke(obj, null);
    private static void Check(bool condition, string test)
    {
        if (!condition) throw new Exception("Worker Carry Play validation failed: " + test);
    }
}
