using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class KoreanGoalFontSetup
{
    private const string SourcePath = "Assets/_Project/Fonts/NotoSansKR-VariableFont_wght.ttf";
    private const string AssetPath = "Assets/_Project/Fonts/NotoSansKR Goal SDF.asset";
    private const string MainScenePath = "Assets/_Project/Scenes/MainScene.unity";

    [MenuItem("Tools/ParcelTycoon/Create Korean Goal Font")]
    public static void CreateAndApply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play mode before creating the Korean goal font.");
            return;
        }
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != MainScenePath)
        {
            Debug.LogWarning("Open MainScene in Edit mode first. Other scenes are not modified.");
            return;
        }
        var scripts = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
        var progression = scripts.OfType<ProgressionManager>().SingleOrDefault();
        var goal = scripts.OfType<GoalUI>().SingleOrDefault();
        if (progression == null || goal == null) throw new InvalidOperationException("MainScene goal components are missing.");
        var goalSettings = new SerializedObject(goal);
        var text = goalSettings.FindProperty("goalText").objectReferenceValue as TMP_Text;
        if (text == null) throw new InvalidOperationException("GoalText reference is missing.");
        Font source = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
        var importer = AssetImporter.GetAtPath(SourcePath) as TrueTypeFontImporter;
        if (source == null || importer == null || !importer.includeFontData)
            throw new InvalidOperationException("Noto Sans KR must be imported with Include Font Data enabled.");

        // Collect actual goal strings, including terminal/error states. No purchase or save calls.
        string characters = string.Concat(Enum.GetValues(typeof(ProgressionManager.GoalId))
            .Cast<ProgressionManager.GoalId>().Select(id => progression.GetGoalText(id))) +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 /:.,!?";
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
        bool created = font == null;
        if (created)
        {
            font = TMP_FontAsset.CreateFontAsset(source, 64, 8, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            if (font == null)
                throw new InvalidOperationException("TMP could not load this variable font. Use a non-variable Noto Sans KR Regular TTF with Include Font Data enabled.");
            font.name = "NotoSansKR Goal SDF";
        }
        else if (font.sourceFontFile != source)
            throw new InvalidOperationException("Existing goal font has a different source; it was not overwritten.");

        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.isMultiAtlasTexturesEnabled = true;
        if (!font.TryAddCharacters(characters, out string missing))
        {
            if (created) UnityEngine.Object.DestroyImmediate(font);
            throw new InvalidOperationException("Noto Sans KR could not render goal characters: " + missing +
                ". Keep the current UI font and retry with a non-variable Regular TTF.");
        }
        // Preserve the prewarmed goals in builds as well as the source font for later glyphs.
        var fontSettings = new SerializedObject(font);
        var clearOnBuild = fontSettings.FindProperty("m_ClearDynamicDataOnBuild");
        if (clearOnBuild != null) clearOnBuild.boolValue = false;
        fontSettings.ApplyModifiedPropertiesWithoutUndo();
        if (created)
        {
            AssetDatabase.CreateAsset(font, AssetPath);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (Texture2D atlas in font.atlasTextures)
            {
                atlas.name = "NotoSansKR Goal Atlas";
                AssetDatabase.AddObjectToAsset(atlas, font);
            }
        }
        foreach (Texture2D atlas in font.atlasTextures)
        {
            if (atlas != null && !AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
            if (atlas != null) EditorUtility.SetDirty(atlas);
        }
        EditorUtility.SetDirty(font);
        EditorUtility.SetDirty(font.material);
        AssetDatabase.SaveAssets();

        bool alreadyDirty = scene.isDirty;
        Undo.RecordObjects(new UnityEngine.Object[] { text, text.rectTransform, text.rectTransform.parent }, "Apply Korean Goal Font");
        text.font = font;
        text.fontSharedMaterial = font.material;
        text.enableAutoSizing = true;
        text.fontSize = 30;
        text.fontSizeMin = 24;
        text.fontSizeMax = 30;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.anchoredPosition = Vector2.zero;
        text.rectTransform.sizeDelta = new Vector2(-24, -12);
        var panel = text.rectTransform.parent as RectTransform;
        if (panel != null) panel.sizeDelta = new Vector2(660, 96);
        text.text = progression.GetGoalText(ProgressionManager.GoalId.SecondInbound);
        EditorUtility.SetDirty(text);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!alreadyDirty) EditorSceneManager.SaveScene(scene);
        else Debug.Log("Korean font applied. MainScene already had unsaved changes; save it manually when ready.");
        Selection.activeObject = font;
        Debug.Log("Korean goal font created and verified: Dynamic, Multi Atlas, 1024 atlas, prewarmed goals. Only GoalText was changed. Test Korean goals in Game view and a mobile build.");
    }
}
