using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PurchasePadLayoutValidation
{
    [MenuItem("Tools/ParcelTycoon/Validate Purchase Pad Layout")]
    public static void Run()
    {
        const string path = "Assets/_Project/Scenes/MainScene.unity";
        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        int labels = 0, cases = 0;
        try
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (PurchasePadSurfaceText surface in root.GetComponentsInChildren<PurchasePadSurfaceText>(true))
            {
                Transform pad = surface.transform.parent;
                var zone = pad.GetComponent<PurchaseZone>() as MonoBehaviour;
                if (zone == null) zone = pad.GetComponent<SortingSpeedUpgradeZone>();
                if (zone == null) zone = pad.GetComponent<WorkerSpeedUpgradeZone>();
                if (zone == null) zone = pad.GetComponent<WorkerCarryCapacityUpgradeZone>();
                if (zone is WorkerCarryCapacityUpgradeZone) ValidateUpgradePlacement(pad);
                if (zone == null) zone = pad.GetComponent<CarryCapacityUpgradeZone>();
                var data = new SerializedObject(zone);
                bool purchase = zone is PurchaseZone;
                if (data.FindProperty("paymentPerTick").intValue != 100 ||
                    !Mathf.Approximately(data.FindProperty("paymentInterval").floatValue, 0.2f))
                    throw new Exception($"{pad.name}: expected scene payment of ₩100 every 0.2s");
                if (purchase && pad.name != "WarehouseExpansionZone")
                {
                    var target = (GameObject)data.FindProperty("targetObject").objectReferenceValue;
                    if (target == null || Vector2.Distance(XZ(pad.position), XZ(target.transform.position)) > 2.1f)
                        throw new Exception($"{pad.name}: purchase target must appear beside its pad");
                }
                int cost = data.FindProperty(purchase ? "purchaseCost" : "baseCost").intValue;
                string title = purchase ? data.FindProperty("displayName").stringValue :
                    zone is SortingSpeedUpgradeZone ? "분류 속도" :
                    zone is WorkerSpeedUpgradeZone ? "직원 이동 속도" :
                    zone is WorkerCarryCapacityUpgradeZone ? "직원 운반량" : "플레이어 운반량";

                // Clone only presentation objects; never run gameplay or touch save data.
                var holder = new GameObject("Pad layout validation");
                holder.hideFlags = HideFlags.HideAndDontSave;
                holder.transform.SetPositionAndRotation(pad.position, pad.rotation);
                holder.transform.localScale = pad.lossyScale;
                GameObject clone = UnityEngine.Object.Instantiate(surface.gameObject, holder.transform, false);
                clone.SetActive(true);
                try
                {
                    var text = clone.GetComponent<TextMeshPro>();
                    if (text.enableAutoSizing) throw new Exception($"{pad.name}: Auto Size is enabled");
                    float baseSize = text.fontSize;
                    var glyphHeights = new Dictionary<(char, float), float>();
                    string[] samples = BuildSamples(zone, title, cost);
                    foreach (string sample in samples)
                    {
                        text.text = sample;
                        text.ForceMeshUpdate();
                        if (!Mathf.Approximately(baseSize, text.fontSize))
                            throw new Exception($"{pad.name}: base font size changed");
                        for (int i = 0; i < text.textInfo.characterCount; i++)
                        {
                            var c = text.textInfo.characterInfo[i];
                            if (!c.isVisible) continue;
                            var mesh = text.textInfo.meshInfo[c.materialReferenceIndex];
                            float height = mesh.vertices[c.vertexIndex + 1].y - mesh.vertices[c.vertexIndex].y;
                            var key = (c.character, c.pointSize);
                            if (glyphHeights.TryGetValue(key, out float previous) && Mathf.Abs(previous - height) > 0.0001f)
                                throw new Exception($"{pad.name}: rendered glyph size changed for {c.character}");
                            glyphHeights[key] = height;
                        }
                        int vertices = 0;
                        for (int m = 0; m < text.textInfo.materialCount; m++)
                        {
                            TMP_MeshInfo mesh = text.textInfo.meshInfo[m];
                            for (int v = 0; v < mesh.vertexCount; v++)
                            {
                                Vector3 p = holder.transform.InverseTransformPoint(
                                    text.transform.TransformPoint(mesh.vertices[v]));
                                if (Mathf.Abs(p.x) > 0.4001f || Mathf.Abs(p.z) > 0.4001f ||
                                    Mathf.Abs(p.y - 0.55f) > 0.0001f)
                                    throw new Exception($"{pad.name}: mesh outside inset surface: {p}");
                                vertices++;
                            }
                        }
                        if (vertices == 0 || text.isTextTruncated)
                            throw new Exception($"{pad.name}: empty or truncated text");
                        cases++;
                    }
                    labels++;
                }
                finally { UnityEngine.Object.DestroyImmediate(holder); }
            }
            if (labels != 8) throw new Exception($"Expected 8 pads, found {labels}");
            Debug.Log($"PAD_LAYOUT_PASS: {labels} pads, {cases} generated TMP meshes; " +
                "all vertices inside local XZ +/-0.4, surface Y=0.55, no truncation. Scene not saved.");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    private static void ValidateUpgradePlacement(Transform pad)
    {
        Transform group = pad.parent;
        var speed = group.GetComponentInChildren<WorkerSpeedUpgradeZone>(true);
        var carry = group.GetComponentInChildren<CarryCapacityUpgradeZone>(true);
        var sorting = group.GetComponentInChildren<SortingSpeedUpgradeZone>(true);
        if (group.name != "PurchaseZones" || speed == null || carry == null || sorting == null ||
            speed.transform.parent != group || carry.transform.parent != group || sorting.transform.parent != group)
            throw new Exception("Staff carry must share the existing upgrade pad parent");
        float staffSpacing = Vector2.Distance(XZ(pad.position), XZ(speed.transform.position));
        float playerSpacing = Vector2.Distance(XZ(pad.position), XZ(carry.transform.position));
        if (!Mathf.Approximately(pad.position.z, speed.transform.position.z) ||
            staffSpacing > 3.5f || playerSpacing < staffSpacing + 2f)
            throw new Exception("Staff pads must form a separate pair from the player pad");

        Transform machine = null;
        foreach (var root in group.gameObject.scene.GetRootGameObjects())
        {
            var station = root.GetComponentInChildren<SortingStation>(true);
            if (station != null) { machine = station.transform.Find("Visual"); break; }
        }
        if (machine == null) throw new Exception("Missing sorting machine");
        Camera sourceCamera = null;
        foreach (var root in group.gameObject.scene.GetRootGameObjects())
        {
            sourceCamera = root.GetComponentInChildren<Camera>(true);
            if (sourceCamera != null) break;
        }
        var view = new GameObject("Isolated 16:9 projection");
        try
        {
            var camera = view.AddComponent<Camera>();
            EditorUtility.CopySerialized(sourceCamera, camera);
            camera.enabled = false;
            view.transform.SetPositionAndRotation(sourceCamera.transform.position, sourceCamera.transform.rotation);
            camera.aspect = 16f / 9f;
            Vector3 before = camera.WorldToViewportPoint(new Vector3(1.1f, 0.1f, 1.05f));
            Vector3 after = camera.WorldToViewportPoint(sorting.transform.position);
            Vector3 machineView = camera.WorldToViewportPoint(machine.position);
            if (Vector2.Distance(before, after) < 0.02f || Vector2.Distance(after, machineView) < 0.08f ||
                after.x < 0.49f || after.x > 0.60f || after.y < 0.40f || after.y > 0.64f)
                throw new Exception("Sorting pad must visibly separate from the machine in 16:9");
            Debug.Log($"SORTING_PAD_VIEW_PASS: before={before:F5}; after={after:F5}; delta={after - before:F5}");
        }
        finally { UnityEngine.Object.DestroyImmediate(view); }

        var trigger = pad.GetComponent<BoxCollider>();
        var reference = speed.GetComponent<BoxCollider>();
        if (trigger == null || reference == null || !trigger.enabled || !trigger.isTrigger ||
            trigger.size != reference.size || trigger.center != reference.center)
            throw new Exception("Staff carry trigger must match the existing upgrade pad trigger");
        var pads = group.GetComponentsInChildren<BoxCollider>(true);
        for (int i = 0; i < pads.Length; i++)
        {
            Rect footprint = Footprint(pads[i]);
            for (int j = i + 1; j < pads.Length; j++)
                if (footprint.Overlaps(Footprint(pads[j])))
                    throw new Exception($"{pads[i].name} overlaps {pads[j].name}");
        }
        Debug.Log("PAD_GROUP_LAYOUT_PASS: staff pair, separate player pad, sorting pad separated in 16:9, matching trigger, no pad overlap.");
    }

    private static Vector2 XZ(Vector3 point) => new Vector2(point.x, point.z);

    private static Rect Footprint(BoxCollider collider)
    {
        Vector3 half = collider.size * 0.5f;
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int corner = 0; corner < 8; corner++)
        {
            var offset = new Vector3((corner & 1) == 0 ? -half.x : half.x,
                (corner & 2) == 0 ? -half.y : half.y, (corner & 4) == 0 ? -half.z : half.z);
            Vector3 point = collider.transform.TransformPoint(collider.center + offset);
            var xz = new Vector2(point.x, point.z);
            min = Vector2.Min(min, xz);
            max = Vector2.Max(max, xz);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    private static T Copy<T>(T source, GameObject root) where T : MonoBehaviour
    {
        var copy = root.AddComponent<T>();
        EditorUtility.CopySerialized(source, copy);
        return copy;
    }

    private static void Reference(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string[] BuildSamples(MonoBehaviour source, string title, int baseCost)
    {
        if (source is PurchaseZone)
        {
            // Verify scene prices without defining runtime prices in UI.
            int expected = source.name == "WarehouseExpansionZone" ? 2500 :
                source.name == "InputWorkerHireZone_1000" ? 1000 :
                source.name == "DeliveryWorkerHireZone_500" ? 500 : 300;
            if (baseCost != expected) throw new Exception($"Unexpected purchase cost {source.name}: {baseCost}");
            string normal = PurchasePadText.Purchase(title, baseCost, 0);
            int paid = baseCost / 200 * 100;
            string partial = PurchasePadText.Purchase(title, baseCost, paid);
            if (!normal.Contains($"₩{baseCost:N0}") || !partial.Contains($"남은 ₩{baseCost - paid:N0}"))
                throw new Exception("Purchase price/remaining display mismatch");
            return new[] { normal, PurchasePadText.Purchase(title, baseCost, 100), partial, partial };
        }
        var root = new GameObject("Isolated upgrade display validation");
        root.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var original = new SerializedObject(source);
            var text = root.AddComponent<TextMeshPro>();
            var samples = new List<string>();
            Action<UpgradeSaveData> restore;
            Func<UpgradeSaveData> capture;
            int max;
            int[] expectedCosts;
            string[] expectedEffects;
            if (source is SortingSpeedUpgradeZone sorting)
            {
                var zone = Copy(sorting, root);
                var station = Copy((SortingStation)original.FindProperty("sortingStation").objectReferenceValue, root);
                Reference(zone, "sortingStation", station); Reference(zone, "priceText", text);
                restore = zone.RestoreState; capture = zone.CaptureState; max = station.MaxSortingLevel;
                expectedCosts = new[] { 500, 1000, 2000 };
                expectedEffects = new[] { "2.0초 → 1.6초", "1.6초 → 1.2초", "1.2초 → 0.8초", "0.8초" };
            }
            else if (source is CarryCapacityUpgradeZone carry)
            {
                var zone = Copy(carry, root);
                var player = Copy((PlayerCarry)original.FindProperty("playerCarry").objectReferenceValue, root);
                Reference(zone, "playerCarry", player); Reference(zone, "priceText", text);
                restore = zone.RestoreState; capture = zone.CaptureState; max = player.MaxCarryLevel;
                expectedCosts = new[] { 800, 1600 };
                expectedEffects = new[] { "3개 → 4개", "4개 → 5개", "최대 5개" };
            }
            else if (source is WorkerCarryCapacityUpgradeZone staffCarry)
            {
                var zone = Copy(staffCarry, root);
                var input = Copy((InputWorkerAI)original.FindProperty("inputWorker").objectReferenceValue, root);
                var output = Copy((WorkerAI)original.FindProperty("deliveryWorker").objectReferenceValue, root);
                Reference(zone, "inputWorker", input); Reference(zone, "deliveryWorker", output); Reference(zone, "priceText", text);
                restore = data =>
                {
                    zone.RestoreState(data);
                    if (input.MaxCarryCount != data.level || output.MaxCarryCount != data.level)
                        throw new Exception("Worker carry capacity was not applied to both workers");
                };
                capture = zone.CaptureState; max = zone.MaxLevel;
                expectedCosts = new[] { 2000, 4000 };
                expectedEffects = new[] { "1개 → 2개", "2개 → 3개", "최대 3개" };
            }
            else
            {
                var zone = Copy((WorkerSpeedUpgradeZone)source, root);
                var input = Copy((InputWorkerAI)original.FindProperty("inputWorker").objectReferenceValue, root);
                var output = Copy((WorkerAI)original.FindProperty("deliveryWorker").objectReferenceValue, root);
                Reference(zone, "inputWorker", input); Reference(zone, "deliveryWorker", output); Reference(zone, "priceText", text);
                restore = zone.RestoreState; capture = zone.CaptureState;
                max = original.FindProperty("maxWorkerSpeedLevel").intValue;
                expectedCosts = new[] { 1000, 2000, 4000 };
                expectedEffects = new[] { "3.00 → 3.75", "3.75 → 4.50", "4.50 → 5.25", "5.25" };
            }
            if (max != expectedEffects.Length) throw new Exception("Unexpected max level");
            int multiplier = original.FindProperty("costMultiplier").intValue;
            for (int level = 1; level <= max; level++)
            {
                int cost = baseCost * (int)Mathf.Pow(multiplier, level - 1);
                if (level < max && cost != expectedCosts[level - 1]) throw new Exception("Unexpected tier price");
                restore(new UpgradeSaveData { level = level, paidAmount = 0 });
                if (!text.text.Contains(expectedEffects[level - 1])) throw new Exception($"Effect mismatch: {text.text}");
                if (level == max)
                {
                    if (!text.text.Contains("MAX") || text.text.Contains("₩")) throw new Exception("MAX price must be absent");
                }
                else if (!text.text.Contains($"₩{cost:N0}")) throw new Exception("Tier price mismatch");
                samples.Add(text.text);
                if (level == max) continue;
                int paid = cost / 200 * 100;
                restore(new UpgradeSaveData { level = level, paidAmount = paid });
                if (!text.text.Contains($"남은 ₩{cost - paid:N0}") ||
                    !text.text.Contains(expectedEffects[level - 1]) || !text.text.Contains($"Lv.{level}"))
                    throw new Exception("Partial payment must preserve level/effect/remaining amount");
                if (capture().paidAmount != paid) throw new Exception("Display changed payment state");
                samples.Add(text.text);
                restore(new UpgradeSaveData { level = level, paidAmount = 100 });
                samples.Add(text.text);
                samples.Add(text.text); // Same last balance remains visible after exiting the trigger.
            }
            return samples.ToArray();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

}
