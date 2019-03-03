using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DensityVolume))]
    class DensityVolumeEditor : Editor
    {
        [System.Flags]
        enum Expandable
        {
            Volume = 1 << 0,
            DensityMaskTexture = 1 << 1
        }

        const int k_MaxDisplayedBox = 10;
        const string k_StateKey = "HDRP:DensityVolume:UI_State";

        const EditMode.SceneViewEditMode k_EditShape = EditMode.SceneViewEditMode.ReflectionProbeBox;
        const EditMode.SceneViewEditMode k_EditBlend = EditMode.SceneViewEditMode.GridBox;

        static class Styles
        {
            public const string k_VolumeHeader = "Volume";
            public const string k_DensityMaskTextureHeader = "Density Mask Texture";

            public static readonly GUIContent[] s_Toolbar_Contents = new GUIContent[]
            {
                EditorGUIUtility.IconContent("EditCollider", "|Modify the base shape. (SHIFT+1)"),
                EditorGUIUtility.IconContent("PreMatCube", "|Modify the influence volume. (SHIFT+2)")
            };

            public static readonly GUIContent s_Size = new GUIContent("Size", "The size of this density volume which is transform's scale independent.");
            public static readonly GUIContent s_AlbedoLabel = new GUIContent("Single Scattering Albedo", "Hue and saturation control the color of the fog (the wavelength of in-scattered light). Value controls scattering (0 = max absorption & no scattering, 1 = no absorption & max scattering).");
            public static readonly GUIContent s_MeanFreePathLabel = new GUIContent("Fog Distance", "Controls the density, which determines how far you can see through the fog. A.k.a. \"mean free path length\". At this distance, 63% of background light is lost in the fog (due to absorption and out-scattering).");
            public static readonly GUIContent s_VolumeTextureLabel = new GUIContent("Texture");
            public static readonly GUIContent s_TextureScrollLabel = new GUIContent("Scroll Speed");
            public static readonly GUIContent s_TextureTileLabel = new GUIContent("Tiling");
            public static readonly GUIContent s_BlendLabel = new GUIContent("Blend Distance", "Distance from size where the linear fade is done.");
            public static readonly GUIContent s_InvertFadeLabel = new GUIContent("Invert Blend", "Inverts blend values in such a way that (0 -> Max), (half max -> half max) and (Max -> 0).");
            public static readonly GUIContent s_NormalModeContent = new GUIContent("Normal", "Normal parameters mode.");
            public static readonly GUIContent s_AdvancedModeContent = new GUIContent("Advanced", "Advanced parameters mode.");

            public static readonly Color k_GizmoColorBase = new Color(180 / 255f, 180 / 255f, 180 / 255f, 8 / 255f).gamma;

            public static readonly Color[] k_BaseHandlesColor = new Color[]
            {
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma
            }; 
        }

        SerializedProperty densityParams;
        SerializedProperty albedo;
        SerializedProperty meanFreePath;

        SerializedProperty volumeTexture;
        SerializedProperty textureScroll;
        SerializedProperty textureTile;

        SerializedProperty size;

        SerializedProperty positiveFade;
        SerializedProperty negativeFade;
        SerializedProperty uniformFade;
        SerializedProperty advancedFade;
        SerializedProperty invertFade;

        static Dictionary<DensityVolume, HierarchicalBox> shapeBoxes = new Dictionary<DensityVolume, HierarchicalBox>();
        static Dictionary<DensityVolume, HierarchicalBox> blendBoxes = new Dictionary<DensityVolume, HierarchicalBox>();

        uint expendedState { get { return (uint)EditorPrefs.GetInt(k_StateKey); } set { EditorPrefs.SetInt(k_StateKey, (int)value); } }

        bool GetExpendedAreas(uint mask) { return (expendedState & mask) > 0; }

        void SetExpendedAreas(uint mask, bool value)
        {
            uint state = expendedState;

            if (value)
            {
                state |= mask;
            }
            else
            {
                mask = ~mask;
                state &= mask;
            }

            expendedState = state;
        }

        void OnEnable()
        {
            densityParams = serializedObject.FindProperty("parameters");

            albedo = densityParams.FindPropertyRelative("albedo");
            meanFreePath = densityParams.FindPropertyRelative("meanFreePath");

            volumeTexture = densityParams.FindPropertyRelative("volumeMask");
            textureScroll = densityParams.FindPropertyRelative("textureScrollingSpeed");
            textureTile = densityParams.FindPropertyRelative("textureTiling");

            size = densityParams.FindPropertyRelative("size");

            positiveFade = densityParams.FindPropertyRelative("m_PositiveFade");
            negativeFade = densityParams.FindPropertyRelative("m_NegativeFade");
            uniformFade = densityParams.FindPropertyRelative("m_UniformFade");
            advancedFade = densityParams.FindPropertyRelative("advancedFade");
            invertFade = densityParams.FindPropertyRelative("invertFade");

            shapeBoxes.Clear();
            blendBoxes.Clear();
            int max = Mathf.Min(targets.Length, k_MaxDisplayedBox);
            for (int i = 0; i < max; ++i)
            {
                var shapeBox = shapeBoxes[targets[i] as DensityVolume] = new HierarchicalBox(Styles.k_GizmoColorBase, Styles.k_BaseHandlesColor);
                shapeBox.monoHandle = false;
                blendBoxes[targets[i] as DensityVolume] = new HierarchicalBox(Styles.k_GizmoColorBase, InfluenceVolumeUI.k_HandlesColor, container: shapeBox);
            }
            
            //init save of state if first time
            if (!EditorPrefs.HasKey(k_StateKey))
            {
                EditorPrefs.SetInt(k_StateKey, (int)(Expandable.Volume | Expandable.DensityMaskTexture));
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Drawer_ToolBar();

            EditorGUILayout.PropertyField(albedo, Styles.s_AlbedoLabel);
            EditorGUILayout.PropertyField(meanFreePath, Styles.s_MeanFreePathLabel);
            EditorGUILayout.Space();

            CoreEditorUtils.DrawSplitter();
            EditorGUI.BeginChangeCheck();
            bool expendedVolume = CoreEditorUtils.DrawHeaderFoldout(Styles.k_VolumeHeader, GetExpendedAreas((uint)Expandable.Volume));
            if (EditorGUI.EndChangeCheck())
            {
                SetExpendedAreas((uint)Expandable.Volume, expendedVolume);
            }
            if (expendedVolume)
            {
                Drawer_AdvancedSwitch();
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(size, Styles.s_Size);
                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 tmpClamp = size.vector3Value;
                    tmpClamp.x = Mathf.Max(0f, tmpClamp.x);
                    tmpClamp.y = Mathf.Max(0f, tmpClamp.y);
                    tmpClamp.z = Mathf.Max(0f, tmpClamp.z);
                    size.vector3Value = tmpClamp;
                }

                Vector3 s = size.vector3Value;
                EditorGUI.BeginChangeCheck();
                if (advancedFade.boolValue)
                {
                    Vector3 positive = positiveFade.vector3Value;
                    positive.x *= s.x;
                    positive.y *= s.y;
                    positive.z *= s.z;
                    Vector3 negative = negativeFade.vector3Value;
                    negative.x *= s.x;
                    negative.y *= s.y;
                    negative.z *= s.z;
                    EditorGUI.BeginChangeCheck();
                    CoreEditorUtils.DrawVector6(Styles.s_BlendLabel, ref positive, ref negative, Vector3.zero, s, InfluenceVolumeUI.k_HandlesColor);
                    if(EditorGUI.EndChangeCheck())
                    {
                        positive.x /= s.x;
                        positive.y /= s.y;
                        positive.z /= s.z;
                        negative.x /= s.x;
                        negative.y /= s.y;
                        negative.z /= s.z;

                        //forbid positive/negative box that doesn't intersect in inspector too
                        for(int axis = 0; axis < 3; ++axis)
                        {
                            if (positive[axis] > 1f - negative[axis])
                            {
                                if (positive == positiveFade.vector3Value)
                                {
                                    negative[axis] = 1f - positive[axis];
                                }
                                else
                                {
                                    positive[axis] = 1f - negative[axis];
                                }
                            }
                        }

                        positiveFade.vector3Value = positive;
                        negativeFade.vector3Value = negative;
                    }
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    float distanceMax = Mathf.Min(s.x, s.y, s.z);
                    float uniformFadeDistance = uniformFade.floatValue * distanceMax;
                    uniformFadeDistance = EditorGUILayout.FloatField(Styles.s_BlendLabel, uniformFadeDistance);
                    if (EditorGUI.EndChangeCheck())
                    {
                        uniformFade.floatValue = Mathf.Clamp(uniformFadeDistance / distanceMax, 0f, 0.5f);
                    }
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 posFade = new Vector3();
                    posFade.x = Mathf.Clamp01(positiveFade.vector3Value.x);
                    posFade.y = Mathf.Clamp01(positiveFade.vector3Value.y);
                    posFade.z = Mathf.Clamp01(positiveFade.vector3Value.z);

                    Vector3 negFade = new Vector3();
                    negFade.x = Mathf.Clamp01(negativeFade.vector3Value.x);
                    negFade.y = Mathf.Clamp01(negativeFade.vector3Value.y);
                    negFade.z = Mathf.Clamp01(negativeFade.vector3Value.z);

                    positiveFade.vector3Value = posFade;
                    negativeFade.vector3Value = negFade;
                }

                EditorGUILayout.PropertyField(invertFade, Styles.s_InvertFadeLabel);
                EditorGUILayout.Space();
            }

            CoreEditorUtils.DrawSplitter();
            EditorGUI.BeginChangeCheck();
            bool expendedDensityMaskTexture = CoreEditorUtils.DrawHeaderFoldout(Styles.k_DensityMaskTextureHeader, GetExpendedAreas((uint)Expandable.DensityMaskTexture));
            if (EditorGUI.EndChangeCheck())
            {
                SetExpendedAreas((uint)Expandable.DensityMaskTexture, expendedDensityMaskTexture);
            }
            if (expendedDensityMaskTexture)
            {
                EditorGUILayout.PropertyField(volumeTexture, Styles.s_VolumeTextureLabel);
                EditorGUILayout.PropertyField(textureScroll, Styles.s_TextureScrollLabel);
                EditorGUILayout.PropertyField(textureTile, Styles.s_TextureTileLabel);
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        void Drawer_ToolBar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditMode.DoInspectorToolbar(new[] { k_EditShape, k_EditBlend }, Styles.s_Toolbar_Contents, () =>
                {
                    var bounds = new Bounds();
                    foreach (Component targetObject in targets)
                    {
                        bounds.Encapsulate(targetObject.transform.position);
                    }
                    return bounds;
                },
                this);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        void Drawer_AdvancedSwitch()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                bool advanced = advancedFade.boolValue;
                advanced = !GUILayout.Toggle(!advanced, Styles.s_NormalModeContent, EditorStyles.miniButtonLeft, GUILayout.Width(60f), GUILayout.ExpandWidth(false));
                advanced = GUILayout.Toggle(advanced, Styles.s_AdvancedModeContent, EditorStyles.miniButtonRight, GUILayout.Width(60f), GUILayout.ExpandWidth(false));
                foreach (var containedBox in blendBoxes.Values)
                {
                    containedBox.monoHandle = !advanced;
                }
                if (advancedFade.boolValue ^ advanced)
                {
                    advancedFade.boolValue = advanced;
                }
            }
        }

        static Vector3 CenterBlendLocalPosition(DensityVolume densityVolume)
        {
            Vector3 size = densityVolume.parameters.size;
            Vector3 posBlend = densityVolume.parameters.positiveFade;
            posBlend.x *= size.x;
            posBlend.y *= size.y;
            posBlend.z *= size.z;
            Vector3 negBlend = densityVolume.parameters.negativeFade;
            negBlend.x *= size.x;
            negBlend.y *= size.y;
            negBlend.z *= size.z;
            Vector3 localPosition = (negBlend - posBlend) * 0.5f;
            return localPosition;
        }

        static Vector3 BlendSize(DensityVolume densityVolume)
        {
            Vector3 size = densityVolume.parameters.size;
            Vector3 blendSize = (Vector3.one - densityVolume.parameters.positiveFade - densityVolume.parameters.negativeFade);
            blendSize.x *= size.x;
            blendSize.y *= size.y;
            blendSize.z *= size.z;
            return blendSize;
        }
        
        [DrawGizmo(GizmoType.Selected|GizmoType.Active)]
        static void DrawGizmosSelected(DensityVolume densityVolume, GizmoType gizmoType)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(densityVolume.transform.position, densityVolume.transform.rotation, Vector3.one)))
            {
                // Blend box
                HierarchicalBox blendBox = blendBoxes[densityVolume];
                blendBox.center = CenterBlendLocalPosition(densityVolume);
                blendBox.size = BlendSize(densityVolume);
                Color baseColor = densityVolume.parameters.albedo;
                baseColor.a = 8/255f;
                blendBox.baseColor = baseColor;
                blendBox.DrawHull(EditMode.editMode == k_EditBlend);
                
                // Bounding box.
                HierarchicalBox shapeBox = shapeBoxes[densityVolume];
                shapeBox.center = Vector3.zero;
                shapeBox.size = densityVolume.parameters.size;
                shapeBox.DrawHull(EditMode.editMode == k_EditShape);
            }
        }

        void OnSceneGUI()
        {
            DensityVolume densityVolume = target as DensityVolume;
            HierarchicalBox shapeBox = shapeBoxes[densityVolume];
            HierarchicalBox blendBox = blendBoxes[densityVolume];

            switch (EditMode.editMode)
            {
                case k_EditBlend:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(densityVolume.transform.position, densityVolume.transform.rotation, Vector3.one)))
                    {
                        //contained must be initialized in all case
                        shapeBox.center = Vector3.zero;
                        shapeBox.size = densityVolume.parameters.size;

                        blendBox.monoHandle = !densityVolume.parameters.advancedFade;
                        blendBox.center = CenterBlendLocalPosition(densityVolume);
                        blendBox.size = BlendSize(densityVolume);
                        EditorGUI.BeginChangeCheck();
                        blendBox.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(densityVolume, "Change Density Volume Blend");

                            //work in local space to compute the change on positiveFade and negativeFade
                            Vector3 newCenterBlendLocalPosition = blendBox.center;
                            Vector3 halfSize = blendBox.size * 0.5f;
                            Vector3 size = densityVolume.parameters.size;
                            Vector3 posFade = newCenterBlendLocalPosition + halfSize;
                            posFade.x = 0.5f - posFade.x / size.x;
                            posFade.y = 0.5f - posFade.y / size.y;
                            posFade.z = 0.5f - posFade.z / size.z;
                            Vector3 negFade = newCenterBlendLocalPosition - halfSize;
                            negFade.x = 0.5f + negFade.x / size.x;
                            negFade.y = 0.5f + negFade.y / size.y;
                            negFade.z = 0.5f + negFade.z / size.z;
                            densityVolume.parameters.positiveFade = posFade;
                            densityVolume.parameters.negativeFade = negFade;
                        }
                    }
                    break;
                case k_EditShape:
                    //important: if the origin of the handle's space move along the handle,
                    //handles displacement will appears as moving two time faster.
                    using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, densityVolume.transform.rotation, Vector3.one)))
                    {
                        //contained must be initialized in all case
                        shapeBox.center = Quaternion.Inverse(densityVolume.transform.rotation) * densityVolume.transform.position;
                        shapeBox.size = densityVolume.parameters.size;

                        shapeBox.monoHandle = !densityVolume.parameters.advancedFade;
                        EditorGUI.BeginChangeCheck();
                        shapeBox.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(new Object[] { densityVolume, densityVolume.transform }, "ChangeDensity Volume Bounding Box");

                            densityVolume.parameters.size = shapeBox.size;
                            
                            Vector3 delta = densityVolume.transform.rotation * shapeBox.center - densityVolume.transform.position;
                            densityVolume.transform.position += delta; ;
                        }
                    }
                    break;
            }
        }
    }
}
