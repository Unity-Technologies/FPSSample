using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Rendering;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

using UnityObject = UnityEngine.Object;


public class VFXExternalShaderProcessor : AssetPostprocessor
{
    public const string k_ShaderDirectory = "Shaders";
    public const string k_ShaderExt = ".vfxshader";
    public static bool allowExternalization { get { return EditorPrefs.GetBool(VFXViewPreference.allowShaderExternalizationKey, false); } }

    void OnPreprocessAsset()
    {
        if (!allowExternalization)
            return;
        if (assetPath.EndsWith(".vfx"))
        {
            string vfxName = Path.GetFileNameWithoutExtension(assetPath);
            string vfxDirectory = Path.GetDirectoryName(assetPath);

            string shaderDirectory = vfxDirectory + "/" + k_ShaderDirectory + "/" + vfxName;

            if (!Directory.Exists(shaderDirectory))
            {
                return;
            }
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
            if (asset == null)
                return;

            bool oneFound = false;
            VisualEffectResource resource = asset.GetResource();
            if (resource == null)
                return;
            VFXShaderSourceDesc[] descs = resource.shaderSources;

            foreach (var shaderPath in Directory.GetFiles(shaderDirectory))
            {
                if (shaderPath.EndsWith(k_ShaderExt))
                {
                    System.IO.StreamReader file = new System.IO.StreamReader(shaderPath);

                    string shaderLine = file.ReadLine();
                    file.Close();
                    if (shaderLine == null || !shaderLine.StartsWith("//"))
                        continue;

                    string[] shaderParams = shaderLine.Split(',');

                    string shaderName = shaderParams[0].Substring(2);

                    int index;
                    if (!int.TryParse(shaderParams[1], out index))
                        continue;

                    if (index < 0 || index >= descs.Length)
                        continue;
                    if (descs[index].name != shaderName)
                        continue;

                    string shaderSource = File.ReadAllText(shaderPath);
                    //remove the first two lines that where added when externalized
                    shaderSource = shaderSource.Substring(shaderSource.IndexOf("\n", shaderSource.IndexOf("\n") + 1) + 1);

                    descs[index].source = shaderSource;
                    oneFound = true;
                }
            }
            if (oneFound)
            {
                resource.shaderSources = descs;
            }
        }
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (!allowExternalization)
            return;
        HashSet<string> vfxToRefresh = new HashSet<string>();
        HashSet<string> vfxToRecompile = new HashSet<string>(); // Recompile vfx if a shader is deleted to replace
        foreach (string assetPath in importedAssets.Concat(deletedAssets).Concat(movedAssets))
        {
            if (assetPath.EndsWith(k_ShaderExt))
            {
                string shaderDirectory = Path.GetDirectoryName(assetPath);
                string vfxName = Path.GetFileName(shaderDirectory);
                string vfxPath = Path.GetDirectoryName(shaderDirectory);

                if (Path.GetFileName(vfxPath) != k_ShaderDirectory)
                    continue;

                vfxPath = Path.GetDirectoryName(vfxPath) + "/" + vfxName + ".vfx";

                if (deletedAssets.Contains(assetPath))
                    vfxToRecompile.Add(vfxPath);
                else
                    vfxToRefresh.Add(vfxPath);
            }
        }

        foreach (var assetPath in vfxToRecompile)
        {
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
            if (asset == null)
                continue;

            // Force Recompilation to restore the previous shaders
            VisualEffectResource resource = asset.GetResource();
            if (resource == null)
                continue;
            resource.GetOrCreateGraph().SetExpressionGraphDirty();
            resource.GetOrCreateGraph().RecompileIfNeeded();
        }

        foreach (var assetPath in vfxToRefresh)
        {
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
            if (asset == null)
                return;
            AssetDatabase.ImportAsset(assetPath);
        }
    }
}

[CustomEditor(typeof(VisualEffectAsset))]
[CanEditMultipleObjects]
public class VisualEffectAssetEditor : Editor
{
    [OnOpenAsset(1)]
    public static bool OnOpenVFX(int instanceID, int line)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj is VFXGraph || obj is VFXModel || obj is VFXUI)
        {
            // for visual effect graph editor ScriptableObject select them when double clicking on them.
            //Since .vfx importer is a copyasset, the default is to open it with an external editor.
            Selection.activeInstanceID = instanceID;
            return true;
        }
        else if (obj is VisualEffectAsset)
        {
            VFXViewWindow.GetWindow<VFXViewWindow>().LoadAsset(obj as VisualEffectAsset, null);
            return true;
        }
        else if (obj is Shader || obj is ComputeShader)
        {
            string path = AssetDatabase.GetAssetPath(instanceID);

            if (path.EndsWith(".vfx"))
            {
                var resource = VisualEffectResource.GetResourceAtPath(path);
                if (resource != null)
                {
                    int index = resource.GetShaderIndex(obj);
                    resource.ShowGeneratedShaderFile(index, line);
                    return true;
                }
            }
        }
        return false;
    }

    ReorderableList m_ReorderableList;
    List<IVFXSubRenderer> m_OutputContexts = new List<IVFXSubRenderer>();

    void OnReorder(ReorderableList list)
    {
        for(int i = 0; i < m_OutputContexts.Count(); ++i)
        {
            m_OutputContexts[i].sortPriority =i;
        }
    }
    private void DrawOutputContextItem(Rect rect, int index, bool isActive, bool isFocused)
    {
        EditorGUI.LabelField(rect, EditorGUIUtility.TempContent((m_OutputContexts[index] as VFXContext).fileName));
    }

    private void DrawHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, EditorGUIUtility.TrTextContent("Output Render Order"));
    }

    static Mesh s_CubeWireFrame;
    void OnEnable()
    {
        VisualEffectAsset target = this.target as VisualEffectAsset;

        m_OutputContexts.Clear();
        m_OutputContexts.AddRange(target.GetResource().GetOrCreateGraph().children.OfType<IVFXSubRenderer>().OrderBy(t => t.sortPriority));

        m_ReorderableList = new ReorderableList(m_OutputContexts, typeof(IVFXSubRenderer));
        m_ReorderableList.displayRemove = false;
        m_ReorderableList.displayAdd = false;
        m_ReorderableList.onReorderCallback = OnReorder;
        m_ReorderableList.drawHeaderCallback = DrawHeader;

        m_ReorderableList.drawElementCallback = DrawOutputContextItem;

        if (m_VisualEffectGO == null)
        {
            m_PreviewUtility = new PreviewRenderUtility();
            m_PreviewUtility.camera.fieldOfView = 60.0f;
            m_PreviewUtility.camera.allowHDR = true;
            m_PreviewUtility.camera.allowMSAA = false;
            m_PreviewUtility.camera.farClipPlane = 10000.0f;
            m_PreviewUtility.ambientColor = new Color(.1f, .1f, .1f, 1.0f);
            m_PreviewUtility.lights[0].intensity = 1.4f;
            m_PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
            m_PreviewUtility.lights[1].intensity = 1.4f;

            m_VisualEffectGO = new GameObject("VisualEffect (Preview)");

            m_VisualEffectGO.hideFlags = HideFlags.DontSave;
            m_VisualEffect = m_VisualEffectGO.AddComponent<VisualEffect>();
            m_PreviewUtility.AddManagedGO(m_VisualEffectGO);

            m_VisualEffectGO.transform.localPosition = Vector3.zero;
            m_VisualEffectGO.transform.localRotation = Quaternion.identity;
            m_VisualEffectGO.transform.localScale = Vector3.one;

            m_VisualEffect.visualEffectAsset = target;

            m_CurrentBounds = new Bounds(Vector3.zero, Vector3.one);
            m_FrameCount = 0;
            m_Distance = 10;
            m_Angles = Vector3.forward;

            if (s_CubeWireFrame == null)
            {
                s_CubeWireFrame = new Mesh();

                var vertices = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, 0.5f),
                    new Vector3(-0.5f, 0.5f, 0.5f),
                    new Vector3(-0.5f, 0.5f, -0.5f),

                    new Vector3(0.5f, -0.5f, -0.5f),
                    new Vector3(0.5f, -0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, -0.5f)
                };


                var indices = new int[]
                {
                    0, 1,
                    0, 3,
                    0, 4,

                    6, 2,
                    6, 5,
                    6, 7,

                    1, 2,
                    1, 5,

                    3, 7,
                    3, 2,

                    4, 5,
                    4, 7
                };
                s_CubeWireFrame.vertices = vertices;
                s_CubeWireFrame.SetIndices(indices, MeshTopology.Lines, 0);
            }
        }


        resourceObject = new SerializedObject(targets.Cast<VisualEffectAsset>().Select(t => t.GetResource()).Where(t => t != null).ToArray());
        resourceUpdateModeProperty = resourceObject.FindProperty("m_Infos.m_UpdateMode");
        cullingFlagsProperty = resourceObject.FindProperty("m_Infos.m_CullingFlags");
        motionVectorRenderModeProperty = resourceObject.FindProperty("m_Infos.m_RendererSettings.motionVectorGenerationMode");
    }

    PreviewRenderUtility m_PreviewUtility;

    GameObject m_VisualEffectGO;
    VisualEffect m_VisualEffect;
    Vector3 m_Angles;
    float m_Distance;
    Bounds m_CurrentBounds;

    int m_FrameCount = 0;

    const int kSafeFrame = 2;

    public override bool HasPreviewGUI()
    {
        return !serializedObject.isEditingMultipleObjects;
    }

    void ComputeFarNear()
    {
        if (m_CurrentBounds.size != Vector3.zero)
        {
            float maxBounds = Mathf.Sqrt(m_CurrentBounds.size.x * m_CurrentBounds.size.x + m_CurrentBounds.size.y * m_CurrentBounds.size.y + m_CurrentBounds.size.z * m_CurrentBounds.size.z);
            m_PreviewUtility.camera.farClipPlane = m_Distance + maxBounds * 1.1f;
            m_PreviewUtility.camera.nearClipPlane = Mathf.Max(0.0001f, (m_Distance - maxBounds));
            m_PreviewUtility.camera.nearClipPlane = Mathf.Max(0.0001f, (m_Distance - maxBounds));
        }
    }

    public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
    {
        if (m_VisualEffectGO == null)
            OnEnable();

        bool isRepaint = (Event.current.type == EventType.Repaint);

        m_Angles = VFXPreviewGUI.Drag2D(m_Angles, r);
        Renderer renderer = m_VisualEffectGO.GetComponent<Renderer>();
        if (renderer == null)
            return;

        if (renderer.bounds.size != Vector3.zero)
        {
            m_CurrentBounds = renderer.bounds;

            //make sure that none of the bounds values are 0
            if (m_CurrentBounds.size.x == 0)
            {
                Vector3 size = m_CurrentBounds.size;
                size.x = (m_CurrentBounds.size.y + m_CurrentBounds.size.z) * 0.1f;
                m_CurrentBounds.size = size;
            }
            if (m_CurrentBounds.size.y == 0)
            {
                Vector3 size = m_CurrentBounds.size;
                size.y = (m_CurrentBounds.size.x + m_CurrentBounds.size.z) * 0.1f;
                m_CurrentBounds.size = size;
            }
            if (m_CurrentBounds.size.z == 0)
            {
                Vector3 size = m_CurrentBounds.size;
                size.z = (m_CurrentBounds.size.x + m_CurrentBounds.size.y) * 0.1f;
                m_CurrentBounds.size = size;
            }
        }

        if (m_FrameCount == kSafeFrame) // wait to frame before asking the renderer bounds as it is a computed value.
        {
            float maxBounds = Mathf.Sqrt(m_CurrentBounds.size.x * m_CurrentBounds.size.x + m_CurrentBounds.size.y * m_CurrentBounds.size.y + m_CurrentBounds.size.z * m_CurrentBounds.size.z);
            m_Distance = Mathf.Max(0.01f, maxBounds * 1.25f);
            ComputeFarNear();
        }
        else
        {
            ComputeFarNear();
        }
        m_FrameCount++;
        if (Event.current.isScrollWheel)
        {
            m_Distance *= 1 + (Event.current.delta.y * .015f);
        }

        if(m_Mat == null)
            m_Mat = (Material)EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat");

        if (isRepaint)
        {
            m_PreviewUtility.BeginPreview(r, background);

            Quaternion rot = Quaternion.Euler(0, m_Angles.x, 0) * Quaternion.Euler(m_Angles.y, 0, 0);
            m_PreviewUtility.camera.transform.position = m_CurrentBounds.center + rot * new Vector3(0, 0, -m_Distance);
            m_PreviewUtility.camera.transform.localRotation = rot;
            m_PreviewUtility.DrawMesh(s_CubeWireFrame, Matrix4x4.TRS(m_CurrentBounds.center, Quaternion.identity, m_CurrentBounds.size), m_Mat, 0);
            m_PreviewUtility.Render(true);
            m_PreviewUtility.EndAndDrawPreview(r);

            // Ask for repaint so the effect is animated.
            Repaint();
        }
    }

    Material m_Mat;

    void OnDisable()
    {
        if (!UnityObject.ReferenceEquals(m_VisualEffectGO, null))
        {
            UnityObject.DestroyImmediate(m_VisualEffectGO);
        }
        if (m_PreviewUtility != null)
        {
            m_PreviewUtility.Cleanup();
        }
    }

    private static readonly GUIContent[] k_CullingOptionsContents = new GUIContent[]
    {
        EditorGUIUtility.TrTextContent("Recompute bounds and simulate when visible"),
        EditorGUIUtility.TrTextContent("Always recompute bounds, simulate only when visible"),
        EditorGUIUtility.TrTextContent("Always recompute bounds and simulate")
    };
    static readonly VFXCullingFlags[] k_CullingOptionsValue = new VFXCullingFlags[]
    {
        VFXCullingFlags.CullSimulation | VFXCullingFlags.CullBoundsUpdate,
        VFXCullingFlags.CullSimulation,
        VFXCullingFlags.CullNone,
    };

    private string UpdateModeToString(VFXUpdateMode mode)
    {
        return ObjectNames.NicifyVariableName(mode.ToString());
    }

    SerializedObject resourceObject;
    SerializedProperty resourceUpdateModeProperty;
    SerializedProperty cullingFlagsProperty;
    SerializedProperty motionVectorRenderModeProperty;

    public override void OnInspectorGUI()
    {
        resourceObject.Update();


        bool enable = GUI.enabled; //Everything in external asset is disabled by default
        GUI.enabled = true;

        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = resourceUpdateModeProperty.hasMultipleDifferentValues;
        VFXUpdateMode newUpdateMode = (VFXUpdateMode)EditorGUILayout.EnumPopup(EditorGUIUtility.TrTextContent("Update Mode"), (VFXUpdateMode)resourceUpdateModeProperty.intValue);
        if (EditorGUI.EndChangeCheck())
        {
            resourceUpdateModeProperty.intValue = (int)newUpdateMode;
            resourceObject.ApplyModifiedProperties();
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUI.showMixedValue = cullingFlagsProperty.hasMultipleDifferentValues;
        EditorGUILayout.PrefixLabel(EditorGUIUtility.TrTextContent("Culling Flags"));
        EditorGUI.BeginChangeCheck();
        int newOption =  EditorGUILayout.Popup(Array.IndexOf(k_CullingOptionsValue, (VFXCullingFlags)cullingFlagsProperty.intValue), k_CullingOptionsContents);
        if (EditorGUI.EndChangeCheck())
        {
            cullingFlagsProperty.intValue = (int)k_CullingOptionsValue[newOption];
            resourceObject.ApplyModifiedProperties();
        }
        EditorGUILayout.EndHorizontal();

        bool needRecompile = false;
        EditorGUI.BeginChangeCheck();


        EditorGUI.showMixedValue = motionVectorRenderModeProperty.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        bool motionVector = EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Use Motion Vectors"), motionVectorRenderModeProperty.intValue == (int)MotionVectorGenerationMode.Object);
        if (EditorGUI.EndChangeCheck())
        {
            motionVectorRenderModeProperty.intValue = motionVector ? (int)MotionVectorGenerationMode.Object : (int)MotionVectorGenerationMode.Camera;
            resourceObject.ApplyModifiedProperties();
            needRecompile = true;
        }

        if (needRecompile)
        {
            foreach (VisualEffectResource resource in resourceObject.targetObjects)
            {
                VFXGraph graph = resource.GetOrCreateGraph() as VFXGraph;
                if (graph != null)
                {
                    graph.SetExpressionGraphDirty();
                    graph.RecompileIfNeeded();
                }
            }
        }

        if (!serializedObject.isEditingMultipleObjects)
        {
            VisualEffectAsset asset = (VisualEffectAsset)target;
            VisualEffectResource resource = asset.GetResource();

            m_OutputContexts.Clear();
            m_OutputContexts.AddRange(resource.GetOrCreateGraph().children.OfType<IVFXSubRenderer>().OrderBy(t => t.sortPriority));

            m_ReorderableList.DoLayoutList();

            VisualEffectEditor.ShowHeader(EditorGUIUtility.TrTextContent("Shaders"), true, true, false, false);

            var shaderSources = resource.shaderSources;

            string assetPath = AssetDatabase.GetAssetPath(asset);
            UnityObject[] objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            string directory = Path.GetDirectoryName(assetPath) + "/" + VFXExternalShaderProcessor.k_ShaderDirectory + "/" + asset.name + "/";

            foreach (var shader in objects)
            {
                if (shader is Shader || shader is ComputeShader)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(shader.name, GUILayout.ExpandWidth(true));
                    int index = resource.GetShaderIndex(shader);
                    if (index >= 0 && index < shaderSources.Length)
                    {
                        if (VFXExternalShaderProcessor.allowExternalization)
                        {
                            string externalPath = directory + shaderSources[index].name;
                            if (!shaderSources[index].compute)
                            {
                                externalPath = directory + shaderSources[index].name.Replace('/', '_') + VFXExternalShaderProcessor.k_ShaderExt;
                            }
                            else
                            {
                                externalPath = directory + shaderSources[index].name + VFXExternalShaderProcessor.k_ShaderExt;
                            }

                            if (System.IO.File.Exists(externalPath))
                            {
                                if (GUILayout.Button("Reveal External"))
                                {
                                    EditorUtility.RevealInFinder(externalPath);
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("Externalize", GUILayout.Width(80)))
                                {
                                    Directory.CreateDirectory(directory);

                                    File.WriteAllText(externalPath, "//" + shaderSources[index].name + "," + index.ToString() + "\n//Don't delete the previous line or this one\n" + shaderSources[index].source);
                                }
                            }
                        }

                        if (GUILayout.Button("Show Generated", GUILayout.Width(110)))
                        {
                            resource.ShowGeneratedShaderFile(index);
                        }
                    }
                    if (GUILayout.Button("Select", GUILayout.Width(50)))
                    {
                        Selection.activeObject = shader;
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }
        GUI.enabled = false;
    }
}


static class VFXPreviewGUI
{
    static int sliderHash = "Slider".GetHashCode();
    public static Vector2 Drag2D(Vector2 scrollPosition, Rect position)
    {
        int id = GUIUtility.GetControlID(sliderHash, FocusType.Passive);
        Event evt = Event.current;
        switch (evt.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (position.Contains(evt.mousePosition) && position.width > 50)
                {
                    GUIUtility.hotControl = id;
                    evt.Use();
                    EditorGUIUtility.SetWantsMouseJumping(1);
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id)
                {
                    scrollPosition -= -evt.delta * (evt.shift ? 3 : 1) / Mathf.Min(position.width, position.height) * 140.0f;
                    scrollPosition.y = Mathf.Clamp(scrollPosition.y, -90, 90);
                    evt.Use();
                    GUI.changed = true;
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                    GUIUtility.hotControl = 0;
                EditorGUIUtility.SetWantsMouseJumping(0);
                break;
        }
        return scrollPosition;
    }
}
