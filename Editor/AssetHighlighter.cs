using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AssetHighlighter : AssetPostprocessor
{
    private static readonly HashSet<string> s_highlightedAssetGuids = new HashSet<string>();

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

        ProcessAssets(importedAssets);
        ProcessAssets(movedAssets);

        EditorApplication.RepaintProjectWindow();
    }

    private static void ProcessAssets(string[] assetPaths)
    {
        foreach (string path in assetPaths)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid))
            {
                s_highlightedAssetGuids.Add(guid);
            }

            string parentPath = Path.GetDirectoryName(path);
            while (!string.IsNullOrEmpty(parentPath) && parentPath != "Assets")
            {
                string parentGuid = AssetDatabase.AssetPathToGUID(parentPath);
                if (!string.IsNullOrEmpty(parentGuid))
                {
                    s_highlightedAssetGuids.Add(parentGuid);
                }
                parentPath = Path.GetDirectoryName(parentPath);
            }
        }
    }

    private static void OnProjectWindowGUI(string guid, Rect selectionRect)
    {
        if (!AssetHighlighterSettings.IsEnabled || !s_highlightedAssetGuids.Contains(guid))
        {
            return;
        }

        Rect highlightRect = selectionRect;
        if (highlightRect.x > 14) 
        {
            highlightRect.x = 14;
            highlightRect.width += selectionRect.x - 14;
        }
        
        Color highlightColor = AssetHighlighterSettings.HighlightColor;
        EditorGUI.DrawRect(highlightRect, highlightColor);
    }
    
    public static void ClearAllHighlights()
    {
        s_highlightedAssetGuids.Clear();
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
                EditorPrefs.GetFloat(ColorPreferenceKeyA, 0.1176f)
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
