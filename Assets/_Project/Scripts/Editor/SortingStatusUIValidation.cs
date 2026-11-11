using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SortingStatusUIValidation
{
    private static void Assign(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(name).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void RunAll()
    {
        ValidateScene();
        Run();
        PurchasePadLayoutValidation.Run();
    }

    private static void ValidateScene()
    {
        const string path = "Assets/_Project/Scenes/MainScene.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            SortingStatusUI found = null;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                Check(t.name != "StatusCanvas", "old panel absent");
                var ui = t.GetComponent<SortingStatusUI>();
                if (ui == null) continue;
                Check(found == null, "single status controller"); found = ui;
            }
            Check(found != null, "scene UI exists");
            var data = new SerializedObject(found);
            var station = (SortingStation)data.FindProperty("sortingStation").objectReferenceValue;
            var input = (TMP_Text)data.FindProperty("inputText").objectReferenceValue;
            var output = (TMP_Text)data.FindProperty("outputText").objectReferenceValue;
            Check(station != null && found.transform == station.transform, "station reference/attachment");
            Check(input != null && input.name == "InputCapacityText" &&
                input.transform.parent == station.transform, "existing input label reference");
            Check(output != null && output.name == "OutputCapacityText" &&
                output.transform.parent == station.transform, "existing output label reference");
            foreach (var root in scene.GetRootGameObjects())
            foreach (var effect in root.GetComponentsInChildren<WarehouseExpansionEffect>(true))
            {
                var effectData = new SerializedObject(effect);
                if (effectData.FindProperty("sortingStation").objectReferenceValue != station) continue;
                Check(effectData.FindProperty("inputCapacityText").objectReferenceValue == null &&
                    effectData.FindProperty("outputCapacityText").objectReferenceValue == null,
                    "capacity effect cannot overwrite status labels");
            }
            SortingZone plate = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                plate = root.GetComponentInChildren<SortingZone>(true);
                if (plate != null) break;
            }
            Check(plate != null, "MainScene input plate");
            ValidateStacks(station, plate.transform);
            Debug.Log("SORTING_SCENE_PASS: existing large labels connected, one controller, no StatusCanvas, no competing label writer.");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    private static void ValidateStacks(SortingStation source, Transform plate)
    {
        var root = new GameObject("Isolated stack layout test");
        try
        {
            var station = root.AddComponent<SortingStation>();
            EditorUtility.CopySerialized(source, station);
            station.enabled = false;
            var settings = new SerializedObject(source);
            Transform Point(string field)
            {
                var original = (Transform)settings.FindProperty(field).objectReferenceValue;
                var point = new GameObject(field).transform;
                point.SetParent(root.transform);
                point.SetPositionAndRotation(original.position, original.rotation);
                Assign(station, field, point);
                return point;
            }
            var input = Point("inputPoint");
            var anchorA = Point("inputStackAnchorA");
            var anchorB = Point("inputStackAnchorB");
            var output = Point("outputPoint");
            var sorting = Point("sortingPoint");
            var boxes = new List<ParcelBox>();
            ParcelBox NewBox()
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/ParcelBox.prefab");
                var go = UnityEngine.Object.Instantiate(prefab, root.transform);
                return go.GetComponent<ParcelBox>();
            }
            void CheckInputs()
            {
                Check(station.InputCount == boxes.Count, "input queue count");
                for (int i = 0; i < boxes.Count; i++)
                {
                    Vector3 expected = (i < 5 ? anchorA : anchorB).position + Vector3.up * ((i % 5) * 1.05f);
                    Check(Vector3.Distance(expected, boxes[i].transform.position) < 0.001f, "input reindex " + i);
                }
            }
            station.SetStorageCapacity(10, 10);
            for (int i = 0; i < 10; i++)
            {
                var box = NewBox();
                Check(station.TryAddInputBox(box), "input acceptance " + i);
                boxes.Add(box);
                CheckInputs();
            }
            Bounds surface = plate.GetComponent<Renderer>().bounds;
            Debug.Log($"INPUT_ANCHOR_BOUNDS: plate renderer={surface.ToString("F5")}; trigger={plate.GetComponent<Collider>().bounds.ToString("F5")}; A={anchorA.position:F5}; B={anchorB.position:F5}");
            foreach (var box in boxes)
            {
                Bounds bounds = box.GetComponent<Renderer>().bounds;
                Check(bounds.min.x >= surface.min.x - 0.001f && bounds.max.x <= surface.max.x + 0.001f &&
                    bounds.min.z >= surface.min.z - 0.001f && bounds.max.z <= surface.max.z + 0.001f, "whole box horizontal footprint on plate");
            }
            foreach (int index in new[] { 0, 5 })
                Check(Mathf.Abs(boxes[index].GetComponent<Renderer>().bounds.min.y - surface.max.y) < 0.001f,
                    "first box bottom touches rendered plate surface");
            boxes[0].PlaceForSorting(sorting);
            boxes.RemoveAt(0);
            station.SetStorageCapacity(10, 10);
            CheckInputs();
            boxes[2].transform.SetParent(root.transform);
            boxes.RemoveAt(2);
            station.SetStorageCapacity(10, 10);
            CheckInputs();
            input.rotation *= Quaternion.Euler(0, 90, 0);
            station.SetStorageCapacity(10, 10);
            CheckInputs();

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var current = typeof(SortingStation).GetField("currentBox", flags);
            var finish = typeof(SortingStation).GetMethod("SortCurrentBox", flags);
            for (int i = 0; i < 10; i++)
            {
                var box = NewBox();
                current.SetValue(station, box);
                var routine = (IEnumerator)finish.Invoke(station, null);
                routine.MoveNext(); routine.MoveNext();
                Vector3 expected = output.position + Vector3.up * ((i % 5) * 1.05f) + Vector3.forward * (i / 5 * 1.5f);
                Check(Vector3.Distance(expected, box.transform.position) < 0.001f, "unchanged output layout " + i);
            }
            Debug.Log("INPUT_STACK_PASS: 1–10, both anchor stacks on actual renderer bounds, sorting/removal reindex, local rotation, unchanged output.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    [MenuItem("Tools/ParcelTycoon/Validate Sorting Status UI")]
    public static void Run()
    {
        ValidateScene();
        var root = new GameObject("Isolated status test");
        try
        {
            var station = root.AddComponent<SortingStation>(); station.enabled = false;
            foreach (string field in new[] { "inputPoint", "inputStackAnchorA", "inputStackAnchorB", "sortingPoint", "outputPoint" })
            {
                var point = new GameObject(field); point.transform.SetParent(root.transform);
                Assign(station, field, point.transform);
            }
            var ui = root.AddComponent<SortingStatusUI>();
            var input = MakeText(root.transform); var output = MakeText(root.transform);
            Assign(ui, "sortingStation", station); Assign(ui, "inputText", input); Assign(ui, "outputText", output);
            ui.Refresh();
            Check(station.MaxInputCount == 5 && station.MaxOutputCount == 5 && LabelEquals(input, "INPUT\n0 / 5"), "base capacity/UI");
            var boxObject = new GameObject("Test box"); boxObject.transform.SetParent(root.transform);
            var box = boxObject.AddComponent<ParcelBox>();
            Check(station.TryAddInputBox(box), "input acceptance"); ui.Refresh();
            Check(station.InputCount == 1 && LabelEquals(input, "INPUT\n1 / 5"), "input UI");
            // Deterministically finish the existing coroutine in Edit Mode, without time or saves.
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var queue = (List<ParcelBox>)typeof(SortingStation).GetField("inputBoxes", flags).GetValue(station);
            queue.RemoveAt(0);
            typeof(SortingStation).GetField("currentBox", flags).SetValue(station, box);
            var routine = (IEnumerator)typeof(SortingStation).GetMethod("SortCurrentBox", flags).Invoke(station, null);
            routine.MoveNext(); routine.MoveNext(); ui.Refresh();
            Check(station.OutputCount == 1 && station.InputCount == 0 && LabelEquals(output, "OUTPUT\n1 / 5"), "output UI/processing exclusion");
            var expansion = new GameObject("Expansion"); expansion.transform.SetParent(root.transform); expansion.SetActive(false);
            var effectObject = new GameObject("Effect"); effectObject.transform.SetParent(root.transform); effectObject.SetActive(false);
            var effect = effectObject.AddComponent<WarehouseExpansionEffect>();
            Assign(effect, "sortingStation", station); Assign(effect, "warehouseExpansion", expansion);
            expansion.SetActive(true); effectObject.SetActive(true);
            // MonoBehaviour callbacks do not run in Edit Mode for this gameplay component.
            typeof(WarehouseExpansionEffect).GetMethod("OnEnable", flags).Invoke(effect, null);
            ui.Refresh();
            Check(station.MaxInputCount == 10 && station.MaxOutputCount == 10 &&
                LabelEquals(input, "INPUT\n0 / 10") && LabelEquals(output, "OUTPUT\n1 / 10"), "expansion/UI");
            string unchanged = input.text; ui.Refresh(); Check(input.text == unchanged, "unchanged refresh");
            Debug.Log("SORTING_STATUS_PASS: base, input, output, processing exclusion, expansion 5->10, UI refresh. No saves used.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    private static bool LabelEquals(TMP_Text label, string expected)
    {
        label.ForceMeshUpdate(true);
        return label.GetParsedText() == expected;
    }
    private static TMP_Text MakeText(Transform parent)
    {
        var go = new GameObject("Test text", typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(parent, false); return go.GetComponent<TMP_Text>();
    }
    private static void Check(bool valid, string step)
    {
        if (!valid) throw new Exception("Sorting status validation failed: " + step);
    }
}
