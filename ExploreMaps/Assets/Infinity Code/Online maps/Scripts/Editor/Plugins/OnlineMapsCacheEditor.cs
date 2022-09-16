/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

using System.Diagnostics;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnlineMapsCache), true)]
public class OnlineMapsCacheEditor:Editor
{
    private bool showPathTokens;

    private SerializedProperty pUseMemoryCache;
    private SerializedProperty pUseFileCache;
    private SerializedProperty pFileCacheLocation;
    private SerializedProperty pFileCacheCustomPath;
    private SerializedProperty pFileCacheTilePath;
    private SerializedProperty pMaxFileCacheSize;
    private SerializedProperty pMaxMemoryCacheSize;
    private SerializedProperty pMemoryCacheUnloadRate;
    private SerializedProperty pFileCacheUnloadRate;
    private OnlineMapsCache cache;
    private int? fileCacheSize;
    private OnlineMaps map;

    private void CacheSerializedFields()
    {
        pUseMemoryCache = serializedObject.FindProperty("useMemoryCache");
        pUseFileCache = serializedObject.FindProperty("useFileCache");
        pFileCacheLocation = serializedObject.FindProperty("fileCacheLocation");
        pFileCacheCustomPath = serializedObject.FindProperty("fileCacheCustomPath");
        pFileCacheTilePath = serializedObject.FindProperty("fileCacheTilePath");
        pMaxFileCacheSize = serializedObject.FindProperty("maxFileCacheSize");
        pMaxMemoryCacheSize = serializedObject.FindProperty("maxMemoryCacheSize");
        pMemoryCacheUnloadRate = serializedObject.FindProperty("memoryCacheUnloadRate");
        pFileCacheUnloadRate = serializedObject.FindProperty("fileCacheUnloadRate");
    }

    private void CheckFileCacheSize()
    {
        if (pMaxFileCacheSize.intValue >= pMaxMemoryCacheSize.intValue * 2) return;

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.HelpBox("The size of the file cache should be a minimum of twice the size of the memory cache.", MessageType.Error);
        if (GUILayout.Button("Increase size of the file cache"))
        {
            pMaxFileCacheSize.intValue = pMaxMemoryCacheSize.intValue * 2;
        }

        EditorGUILayout.EndVertical();
    }

    private void CheckMemoryCacheSize()
    {
        if (map == null) return;
        int w = map.target == OnlineMapsTarget.tileset ? map.tilesetWidth : map.width;
        int h = map.target == OnlineMapsTarget.tileset ? map.tilesetHeight : map.height;
        w /= OnlineMapsUtils.tileSize;
        h /= OnlineMapsUtils.tileSize;
        w += 2;
        h += 2;
        int c = w * h;

        for (int i = 0; i < 5; i++)
        {
            c += (w * h) >> (i + 2);
        }

        int s = (int)(c * 0.2);

        if (pMaxMemoryCacheSize.intValue >= s) return;

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.HelpBox("The minimum recommended memory cache is " + s + " MB.", MessageType.Error);
        if (GUILayout.Button("Increase size of the memory cache"))
        {
            pMaxMemoryCacheSize.intValue = s;
        }

        EditorGUILayout.EndVertical();
    }

    private void OnEnable()
    {
        cache = target as OnlineMapsCache;
        map = cache.GetComponent<OnlineMaps>();
        CacheSerializedFields();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        OnMemoryCacheGUI();
        OnFileCacheGUI();

        serializedObject.ApplyModifiedProperties();
    }

    private void OnFileCacheGUI()
    {
        bool fileCache = pUseFileCache.boolValue;
        if (fileCache) EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.PropertyField(pUseFileCache, new GUIContent("File Cache"));

        if (pUseFileCache.boolValue)
        {
#if UNITY_WEBPLAYER || UNITY_WEBGL
            EditorGUILayout.HelpBox("File Cache is not supported for Webplayer and WebGL.", MessageType.Warning);
#endif
            CheckFileCacheSize();


            EditorGUILayout.PropertyField(pMaxFileCacheSize, new GUIContent("Size (mb)"));
            pFileCacheUnloadRate.floatValue = EditorGUILayout.Slider("Unload (%)", Mathf.RoundToInt(pFileCacheUnloadRate.floatValue * 100), 1, 50) / 100;
            EditorGUILayout.PropertyField(pFileCacheLocation, new GUIContent("Cache Location"));
            if (pFileCacheLocation.enumValueIndex == (int) OnlineMapsCache.CacheLocation.custom)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(pFileCacheCustomPath, new GUIContent("Cache Folder"));
                if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
                {
                    string folder = EditorUtility.OpenFolderPanel("Cache folder", pFileCacheCustomPath.stringValue, "");
                    if (!string.IsNullOrEmpty(folder)) pFileCacheCustomPath.stringValue = folder;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(pFileCacheTilePath, new GUIContent("Tile Path"));
            if (GUILayout.Button("Open", GUILayout.ExpandWidth(false))) Process.Start(cache.GetFileCacheFolder().ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            showPathTokens = OnlineMapsEditor.Foldout(showPathTokens, "Available Tokens");
            if (showPathTokens)
            {
                GUILayout.Label("{pid} - Provider ID");
                GUILayout.Label("{mid} - MapType ID");
                GUILayout.Label("{zoom}, {z} - Tile Zoom");
                GUILayout.Label("{x} - Tile X");
                GUILayout.Label("{y} - Tile Y");
                GUILayout.Label("{quad} - Tile Quad Key");
                GUILayout.Label("{lng} - Language code");
                GUILayout.Label("{lbs} - Labels");
                GUILayout.Space(10);
            }
            EditorGUILayout.EndVertical();

            if (Application.isPlaying || !fileCacheSize.HasValue) fileCacheSize = cache.GetFileCacheSizeFast();

            float fileCacheSizeMb = fileCacheSize.Value / 1000000f;
            string fileCacheSizeStr = fileCacheSizeMb.ToString("F2");
            EditorGUILayout.LabelField("Current Size (mb)", fileCacheSizeStr);
            if (GUILayout.Button("Clear"))
            {
                cache.ClearFileCache();
                fileCacheSize = null;
            }
        }

        if (fileCache) EditorGUILayout.EndVertical();
    }

    private void OnMemoryCacheGUI()
    {
        bool memoryCache = pUseMemoryCache.boolValue;
        if (memoryCache) EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.PropertyField(pUseMemoryCache, new GUIContent("Memory Cache"));

        if (pUseMemoryCache.boolValue)
        {
            CheckMemoryCacheSize();

            EditorGUILayout.PropertyField(pMaxMemoryCacheSize, new GUIContent("Size (mb)"));
            pMemoryCacheUnloadRate.floatValue = EditorGUILayout.Slider("Unload (%)", Mathf.RoundToInt(pMemoryCacheUnloadRate.floatValue * 100), 1, 50) / 100;
            if (Application.isPlaying)
            {
                int memoryCacheSize = (target as OnlineMapsCache).memoryCacheSize;
                float memoryCacheSizeMb = memoryCacheSize / 1000000f;
                string memoryCacheSizeStr = memoryCacheSizeMb < 10? memoryCacheSizeMb.ToString("F2"): memoryCacheSizeMb.ToString("F0");
                EditorGUILayout.LabelField("Current Size (mb)", memoryCacheSizeStr);
                if (GUILayout.Button("Clear")) (target as OnlineMapsCache).ClearMemoryCache();
            }
        }

        if (memoryCache) EditorGUILayout.EndVertical();
    }
}