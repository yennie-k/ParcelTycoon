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
                if (zone == null) zone = pad.GetComponent<CarryCapacityUpgradeZone>();
                var data = new SerializedObject(zone);
                bool purchase = zone is PurchaseZone;
                int cost = data.FindProperty(purchase ? "purchaseCost" : "baseCost").intValue;
                string title = purchase ? data.FindProperty("displayName").stringValue :
                    zone is SortingSpeedUpgradeZone ? "분류 속도" :
                    zone is WorkerSpeedUpgradeZone ? "직원 이동 속도" : "플레이어 운반량";

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
                    string[] samples = BuildSamples(zone, title, cost);
                    foreach (string sample in samples)
                    {
                        text.text = sample;
                        text.ForceMeshUpdate();
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
            if (labels != 7) throw new Exception($"Expected 7 pads, found {labels}");
            Debug.Log($"PAD_LAYOUT_PASS: {labels} pads, {cases} generated TMP meshes; " +
                "all vertices inside local XZ +/-0.4, surface Y=0.55, no truncation. Scene not saved.");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
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
            string partial = PurchasePadText.Purchase(title, baseCost, baseCost / 2);
            if (!normal.Contains($"₩{baseCost:N0}") || !partial.Contains($"남은 금액 ₩{baseCost - baseCost / 2:N0}"))
                throw new Exception("Purchase price/remaining display mismatch");
            return new[] { normal, partial };
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
                expectedCosts = new[] { 750, 1500 };
                expectedEffects = new[] { "3개 → 4개", "4개 → 5개", "최대 5개" };
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
                int paid = cost / 2;
                restore(new UpgradeSaveData { level = level, paidAmount = paid });
                if (!text.text.Contains($"남은 금액 ₩{cost - paid:N0}") ||
                    !text.text.Contains(expectedEffects[level - 1]) || !text.text.Contains($"Lv.{level}"))
                    throw new Exception("Partial payment must preserve level/effect/remaining amount");
                if (capture().paidAmount != paid) throw new Exception("Display changed payment state");
                samples.Add(text.text);
            }
            return samples.ToArray();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

}
