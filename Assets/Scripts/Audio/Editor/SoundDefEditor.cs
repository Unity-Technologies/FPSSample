using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SoundDef))]
public class SoundDefEditor : Editor
{
    static AudioSource testSource = null;

    public override void OnInspectorGUI()
    {
        if (testSource == null)
        {
            var go = new GameObject("testSource");
            go.hideFlags = HideFlags.HideAndDontSave;
            testSource = go.AddComponent<AudioSource>();
        }
        var sd = (SoundDef)target;

        // Allow playing audio even when sounddef is readonly
        var oldEnabled = GUI.enabled;
        GUI.enabled = true;
        if(testSource.isPlaying && GUILayout.Button("Stop []"))
        {
            testSource.Stop();
        }
        else if(!testSource.isPlaying && GUILayout.Button("Play >"))
        {
            SoundSystem.StartSource(testSource, sd);
        }
        GUI.enabled = oldEnabled;

        DrawPropertiesExcluding(serializedObject, new string[] { "m_Script" });

        serializedObject.ApplyModifiedProperties();
    }
}
