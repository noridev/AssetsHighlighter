using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AssetHighlighter : AssetPostprocessor
{
    private static HashSet<string> s_importedAssets = new HashSet<string>();
    private static float s_lastImportTime = 0f;

    static AssetHighlighter()
    {
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowGUI;
    }

    public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (!AssetHighlighterSettings.IsEnabled)
        {
            return;
        }

        if (s_lastImportTime == Time.time && s_importedAssets != null)
        {
            ProcessAssets(importedAssets);
        }
        else
        {
            s_importedAssets = new HashSet<string>();
            ProcessAssets(importedAssets);
        }
        s_lastImportTime = Time.time;

        EditorApplication.RepaintProjectWindow();
    }

    private static void ProcessAssets(string[] assetPaths)
    {
        foreach (string path in assetPaths)
        {
            if (!string.IsNullOrEmpty(path))
            {
                s_importedAssets.Add(path);
            }

            string parentPath = Path.GetDirectoryName(path);
            while (!string.IsNullOrEmpty(parentPath) && parentPath != "Assets")
            {
                s_importedAssets.Add(parentPath);
                parentPath = Path.GetDirectoryName(parentPath);
            }
        }
    }

    private static void OnProjectWindowGUI(string guid, Rect selectionRect)
    {
        if (!AssetHighlighterSettings.IsEnabled || s_importedAssets == null)
        {
            return;
        }

        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(assetPath)) return;

        if (s_importedAssets.Contains(assetPath))
        {
            var originColor = GUI.color;
            GUI.color = AssetHighlighterSettings.HighlightColor;
            GUI.Box(selectionRect, string.Empty);
            GUI.color = originColor;
        }
    }
    
    public static void ClearAllHighlights()
    {
        s_importedAssets?.Clear();
        EditorApplication.RepaintProjectWindow();
    }
}

public class AssetSaveProcessor : UnityEditor.AssetModificationProcessor
{
    public static string[] OnWillSaveAssets(string[] paths)
    {
        AssetHighlighter.ClearAllHighlights();
        return paths;
    }
}

public class AssetHighlighterSettings : EditorWindow
{
    private const string Title = "NoriDev - Multi Object Adder";
    private string version = "";

    private const string EnabledPreferenceKey = "AssetHighlighter.Enabled";
    private const string ColorPreferenceKeyR = "AssetHighlighter.Color.R";
    private const string ColorPreferenceKeyG = "AssetHighlighter.Color.G";
    private const string ColorPreferenceKeyB = "AssetHighlighter.Color.B";
    private const string ColorPreferenceKeyA = "AssetHighlighter.Color.A";

    public static bool IsEnabled
    {
        get => EditorPrefs.GetBool(EnabledPreferenceKey, true);
        private set => EditorPrefs.SetBool(EnabledPreferenceKey, value);
    }

    public static Color HighlightColor
    {
        get
        {
            return new Color(
                EditorPrefs.GetFloat(ColorPreferenceKeyR, 1.0f),
                EditorPrefs.GetFloat(ColorPreferenceKeyG, 0.0f),
                EditorPrefs.GetFloat(ColorPreferenceKeyB, 0.0f),
                EditorPrefs.GetFloat(ColorPreferenceKeyA, 1.0f)
            );
        }
        private set
        {
            EditorPrefs.SetFloat(ColorPreferenceKeyR, value.r);
            EditorPrefs.SetFloat(ColorPreferenceKeyG, value.g);
            EditorPrefs.SetFloat(ColorPreferenceKeyB, value.b);
            EditorPrefs.SetFloat(ColorPreferenceKeyA, value.a);
        }
    }

    [MenuItem("Tools/_NORIDEV_/Asset Highlighter Settings")]
    public static void ShowWindow()
    {
        AssetHighlighterSettings window = GetWindow<AssetHighlighterSettings>(Title);
        window.LoadVersion();
    }

    private void LoadVersion()
    {
        string packageJsonPath = Path.Combine(Application.dataPath, "../Packages/moe.noridev.assets-highlighter/package.json");

        if (File.Exists(packageJsonPath))
        {
            string json = File.ReadAllText(packageJsonPath);
            var packageInfo = JsonUtility.FromJson<PackageInfo>(json);
            version = packageInfo.version;
        }
        else
        {
            Debug.LogWarning($"package.json not found at {packageJsonPath}");
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(Title, new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 });
        GUILayout.FlexibleSpace();
        GUILayout.Label($"v{version}", new GUIStyle(EditorStyles.label) { fontSize = 13 });
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();

        GUILayout.Label("Asset Highlighter Settings", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();

        bool currentEnabled = IsEnabled;
        Color currentColor = HighlightColor;

        currentEnabled = EditorGUILayout.Toggle("Enable Highlighter", currentEnabled);
        currentColor = EditorGUILayout.ColorField("Highlight Color", currentColor);

        if (EditorGUI.EndChangeCheck())
        {
            IsEnabled = currentEnabled;
            HighlightColor = currentColor;
            EditorApplication.RepaintProjectWindow();
        }

        if (GUILayout.Button("Remove all highlight"))
        {
            AssetHighlighter.ClearAllHighlights();
        }
    }

    [System.Serializable]
    private class PackageInfo
    {
        public string version;
    }
}
