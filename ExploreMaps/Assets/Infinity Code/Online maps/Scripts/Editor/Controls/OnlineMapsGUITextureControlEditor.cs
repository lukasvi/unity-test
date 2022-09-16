/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

#if !UNITY_4_6 && !UNITY_4_7 && !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2
#define UNITY_5_3P
#endif

using UnityEditor;
using UnityEngine;

#if UNITY_5_3P
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

[CustomEditor(typeof (OnlineMapsGUITextureControl), true)]
public class OnlineMapsGUITextureControlEditor : Editor
{
    public override void OnInspectorGUI()
    {
#if !UNITY_2017_2_OR_NEWER || ONLINEMAPS_GUITEXTURE
        bool dirty = false;

        OnlineMapsControlBase control = target as OnlineMapsControlBase;
        OnlineMapsControlBaseEditor.CheckMultipleInstances(control, ref dirty);

        OnlineMaps map = OnlineMapsControlBaseEditor.GetOnlineMaps(control);
        OnlineMapsControlBaseEditor.CheckTarget(map, OnlineMapsTarget.texture, ref dirty);

        base.OnInspectorGUI();

        if (dirty)
        {
            EditorUtility.SetDirty(map);
            EditorUtility.SetDirty(control);
            if (!Application.isPlaying)
            {
#if UNITY_5_3P
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
#endif
            }
            else map.Redraw();
        }
#else
        EditorGUILayout.HelpBox("GUITexture is part of the legacy GUI, and will be removed in future versions of Unity Editor.", MessageType.Warning);
        if (GUILayout.Button("Enable GUITexture"))
        {
            OnlineMapsEditor.AddCompilerDirective("ONLINEMAPS_GUITEXTURE");
        }
#endif
    } 
}