using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SpriteCompressor : EditorWindow
{
    private enum CompressionRatio
    {
        Half = 2,
        Third = 3,
        Quarter = 4
    }

    private CompressionRatio selectedRatio = CompressionRatio.Half;
    private bool compressOnlyUISprites = true;
    private Vector2 scrollPosition;
    private List<string> spritePaths = new List<string>();
    private bool showPreview = false;
    private string selectedFolderPath = "Assets";
    private bool useManualPath = false;

    [MenuItem("Tools/Sprite Compressor")]
    public static void ShowWindow()
    {
        GetWindow<SpriteCompressor>("Sprite Compressor");
    }

    private void OnEnable()
    {
        RefreshSpriteList();
    }

    private void OnGUI()
    {
        GUILayout.Label("Sprite Compression Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Folder selection mode
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Folder Selection", EditorStyles.boldLabel);
        
        useManualPath = EditorGUILayout.Toggle("Use Manual Path Selection", useManualPath);
        
        if (useManualPath)
        {
            EditorGUILayout.BeginHorizontal();
            selectedFolderPath = EditorGUILayout.TextField("Folder Path:", selectedFolderPath);
            
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Sprite Folder", selectedFolderPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert absolute path to relative Unity path
                    if (path.StartsWith(Application.dataPath))
                    {
                        selectedFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Path", 
                            "Please select a folder within your Unity project's Assets folder.", 
                            "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Validate path
            if (!string.IsNullOrEmpty(selectedFolderPath) && !AssetDatabase.IsValidFolder(selectedFolderPath))
            {
                EditorGUILayout.HelpBox("Invalid folder path! Please select a valid folder within Assets.", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Select folder(s) in the Project window, then click 'Refresh Sprite List'", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Compression ratio selection
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Compression Settings", EditorStyles.boldLabel);
        selectedRatio = (CompressionRatio)EditorGUILayout.EnumPopup("Compression Ratio:", selectedRatio);
        
        string ratioInfo = "";
        switch (selectedRatio)
        {
            case CompressionRatio.Half:
                ratioInfo = "Compress to 1/2 (50%) of original resolution";
                break;
            case CompressionRatio.Third:
                ratioInfo = "Compress to 1/3 (33.3%) of original resolution";
                break;
            case CompressionRatio.Quarter:
                ratioInfo = "Compress to 1/4 (25%) of original resolution";
                break;
        }
        EditorGUILayout.HelpBox(ratioInfo, MessageType.Info);
        
        compressOnlyUISprites = EditorGUILayout.Toggle("Only UI Sprites", compressOnlyUISprites);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Selected folders info
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Selected Folders", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Refresh Sprite List"))
        {
            RefreshSpriteList();
        }

        EditorGUILayout.LabelField("Sprites Found:", spritePaths.Count.ToString());
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Preview section
        showPreview = EditorGUILayout.Foldout(showPreview, "Preview Sprites to Compress");
        if (showPreview)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            foreach (string path in spritePaths)
            {
                EditorGUILayout.LabelField(path);
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space();

        // Compress button
        EditorGUI.BeginDisabledGroup(spritePaths.Count == 0);
        if (GUILayout.Button("Compress Selected Sprites", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Confirm Compression",
                $"This will compress {spritePaths.Count} sprite(s) to 1/{(int)selectedRatio} of their original resolution.\n\nThis action cannot be undone automatically. Continue?",
                "Compress", "Cancel"))
            {
                CompressSprites();
            }
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        // Instructions
        EditorGUILayout.HelpBox(
            "Instructions:\n" +
            "• Enable 'Use Manual Path Selection' to browse for a folder\n" +
            "• OR select folder(s) in the Project window\n" +
            "• Choose compression ratio\n" +
            "• Click 'Refresh Sprite List' to see found sprites\n" +
            "• Click 'Compress Selected Sprites' to apply",
            MessageType.Info);
    }

    private void RefreshSpriteList()
    {
        spritePaths.Clear();

        if (useManualPath)
        {
            // Use manually selected folder path
            if (!string.IsNullOrEmpty(selectedFolderPath) && AssetDatabase.IsValidFolder(selectedFolderPath))
            {
                FindSpritesInFolder(selectedFolderPath);
            }
            else
            {
                Debug.LogWarning("Invalid folder path: " + selectedFolderPath);
            }
        }
        else
        {
            // Get selected objects in the Project window
            Object[] selectedObjects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);

            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("No folders or assets selected in Project window. Please select folders or enable 'Use Manual Path Selection'.");
            }

            foreach (Object obj in selectedObjects)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);

                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    // It's a folder, find all sprites inside
                    FindSpritesInFolder(assetPath);
                }
                else
                {
                    // It's a file, check if it's a sprite
                    if (IsValidSprite(assetPath))
                    {
                        spritePaths.Add(assetPath);
                    }
                }
            }
        }

        spritePaths = spritePaths.Distinct().ToList();
        
        Debug.Log($"Found {spritePaths.Count} sprite(s) to compress.");
    }

    private void FindSpritesInFolder(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (IsValidSprite(assetPath))
            {
                spritePaths.Add(assetPath);
            }
        }
    }

    private bool IsValidSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        
        if (importer == null)
            return false;

        // Check if it's a sprite
        if (importer.textureType != TextureImporterType.Sprite)
            return false;

        // If filtering for UI sprites only
        if (compressOnlyUISprites)
        {
            // Check if the sprite is in a UI folder or has UI-related naming
            return assetPath.ToLower().Contains("ui") || 
                   assetPath.ToLower().Contains("sprite");
        }

        return true;
    }

    private void CompressSprites()
    {
        int successCount = 0;
        int failCount = 0;

        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (string assetPath in spritePaths)
            {
                try
                {
                    if (CompressSprite(assetPath))
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to compress {assetPath}: {e.Message}");
                    failCount++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Compression Complete",
            $"Successfully compressed: {successCount}\nFailed: {failCount}",
            "OK");

        Debug.Log($"Sprite compression complete. Success: {successCount}, Failed: {failCount}");
    }

    private bool CompressSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        
        if (importer == null)
            return false;

        // Get original texture to determine its size
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
            return false;

        int originalMaxSize = Mathf.Max(texture.width, texture.height);
        int newMaxSize = originalMaxSize / (int)selectedRatio;

        // Ensure minimum size
        newMaxSize = Mathf.Max(newMaxSize, 32);

        // Round to nearest power of 2 for better compression
        newMaxSize = Mathf.NextPowerOfTwo(newMaxSize);

        // Clamp to valid Unity texture sizes
        newMaxSize = Mathf.Clamp(newMaxSize, 32, 8192);

        // Apply new settings
        importer.maxTextureSize = newMaxSize;
        importer.isReadable = false; // Improve memory usage

        // Set compression settings for better file size
        TextureImporterPlatformSettings platformSettings = importer.GetDefaultPlatformTextureSettings();
        platformSettings.maxTextureSize = newMaxSize;
        platformSettings.format = TextureImporterFormat.Automatic;
        platformSettings.textureCompression = TextureImporterCompression.Compressed;
        importer.SetPlatformTextureSettings(platformSettings);

        // Save and reimport
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Debug.Log($"Compressed {assetPath}: {originalMaxSize}px -> {newMaxSize}px (1/{(int)selectedRatio})");

        return true;
    }

    private void OnSelectionChange()
    {
        RefreshSpriteList();
        Repaint();
    }
}
