using UnityEngine;
using UnityEditorInternal;
using System.IO;
using UnityEngine.Rendering;
using UnityEditor;

//As ScriptableSingleton is not usable due to internal FilePathAttribute,
//copying mechanism here

public class VFXVolumeMixerSettings : ScriptableObject
{
    [SettingsProvider]
    static SettingsProvider PreferenceGUI()
    {
        ReorderableList floatNameList = new ReorderableList(floatPropertyNames, typeof(string), false, true, false, false);
        ReorderableList vectorNameList = new ReorderableList(vectorPropertyNames, typeof(string), false, true, false, false);
        ReorderableList colorNameList = new ReorderableList(colorPropertyNames, typeof(string), false, true, false, false);

        floatNameList.drawElementCallback = EditFloatString;
        floatNameList.drawHeaderCallback = FloatHeaderGUI;

        vectorNameList.drawElementCallback = EditVectorString;
        vectorNameList.drawHeaderCallback = VectorHeaderGUI;

        colorNameList.drawElementCallback = EditColorString;
        colorNameList.drawHeaderCallback = ColorHeaderGUI;

        void EditFloatString(Rect rect, int index, bool isActive, bool isFocused)
        {
            
            EditorGUI.BeginDisabledGroup(index >= floatPropertyCount);
            floatPropertyNames[index] = EditorGUI.DelayedTextField(rect, floatPropertyNames[index]);
            EditorGUI.EndDisabledGroup();
        }

        void EditVectorString(Rect rect, int index, bool isActive, bool isFocused)
        {
            EditorGUI.BeginDisabledGroup(index >= vectorPropertyCount);
            vectorPropertyNames[index] = EditorGUI.DelayedTextField(rect, vectorPropertyNames[index]);
            EditorGUI.EndDisabledGroup();
        }

        void EditColorString(Rect rect, int index, bool isActive, bool isFocused)
        {
            EditorGUI.BeginDisabledGroup(index >= colorPropertyCount);
            colorPropertyNames[index] = EditorGUI.DelayedTextField(rect, colorPropertyNames[index]);
            EditorGUI.EndDisabledGroup();
        }

        return new SettingsProvider("Project/VFX Volume Mixer", SettingsScope.Project)
        {
            guiHandler = searchContext => OpenGUI()
        };

        void FloatHeaderGUI(Rect r)
        {
            floatPropertyCount = EditorGUI.IntSlider(r,  floatPropertyCount, 0, 8);
        }
        void VectorHeaderGUI(Rect r)
        {
            vectorPropertyCount = EditorGUI.IntSlider(r, vectorPropertyCount, 0, 8);
        }
        void ColorHeaderGUI(Rect r)
        {
            colorPropertyCount = EditorGUI.IntSlider(r, colorPropertyCount, 0, 8);
        }

        void OpenGUI()
        {
            DrawList("Float Properties", floatNameList);
            DrawList("Vector3 Properties", vectorNameList);
            DrawList("Color Properties", colorNameList);
        }

        
    }

    static void DrawList(string name, ReorderableList list)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
            using (new EditorGUILayout.VerticalScope())
            {
                list.DoLayoutList();
            }

        }
        EditorGUILayout.Space();
    }

    const string filePath = "ProjectSettings/VFXVolumeMixerSettings.asset";

    [SerializeField, Range(0,8)]
    int m_FloatPropertyCount = 1;
    [SerializeField, Range(0, 8)]
    int m_Vector3PropertyCount = 1;
    [SerializeField, Range(0, 8)]
    int m_ColorPropertyCount = 1;

    [SerializeField]
    string[] m_FloatPropertyNames = new string[] { "Float1", "Float2", "Float3", "Float4", "Float5", "Float6", "Float7", "Float8" };
    [SerializeField]
    string[] m_Vector3PropertyNames = new string[] { "Vector1", "Vector2", "Vector3", "Vector4", "Vector5", "Vector6", "Vector7", "Vector8" };
    [SerializeField]
    string[] m_ColorPropertyNames = new string[] { "Color1", "Color2", "Color3", "Color4", "Color5", "Color6", "Color7", "Color8" };


    public static int floatPropertyCount
    {
        get => instance.m_FloatPropertyCount;
        set
        {
            instance.m_FloatPropertyCount = value;
            Save();
        }
    }

    public static int vectorPropertyCount
    {
        get => instance.m_Vector3PropertyCount;
        set
        {
            instance.m_Vector3PropertyCount = value;
            Save();
        }
    }

    public static int colorPropertyCount
    {
        get => instance.m_ColorPropertyCount;
        set
        {
            instance.m_ColorPropertyCount = value;
            Save();
        }
    }

    public static string[] floatPropertyNames
    {
        get => instance.m_FloatPropertyNames;
        set
        {
            instance.m_FloatPropertyNames = value;
            Save();
        }
    }

    public static string[] vectorPropertyNames
    {
        get => instance.m_Vector3PropertyNames;
        set
        {
            instance.m_Vector3PropertyNames = value;
            Save();
        }
    }

    public static string[] colorPropertyNames
    {
        get => instance.m_ColorPropertyNames;
        set
        {
            instance.m_ColorPropertyNames = value;
            Save();
        }
    }

    //singleton pattern
    static VFXVolumeMixerSettings s_Instance;
    static VFXVolumeMixerSettings instance => s_Instance ?? CreateOrLoad();
    VFXVolumeMixerSettings()
    {
        s_Instance = this;
    }

    static VFXVolumeMixerSettings CreateOrLoad()
    {
        //try load
        InternalEditorUtility.LoadSerializedFileAndForget(filePath);

        //else create
        if (s_Instance == null)
        {
            VFXVolumeMixerSettings created = CreateInstance<VFXVolumeMixerSettings>();
            created.hideFlags = HideFlags.HideAndDontSave;
        }

        System.Diagnostics.Debug.Assert(s_Instance != null);
        return s_Instance;
    }

    static void Save()
    {
        if (s_Instance == null)
        {
            Debug.Log("Cannot save ScriptableSingleton: no instance!");
            return;
        }

        string folderPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        InternalEditorUtility.SaveToSerializedFileAndForget(new[] { s_Instance }, filePath, allowTextSerialization: true);
    }
}
