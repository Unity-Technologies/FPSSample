using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // A Material can be authored from the shader graph or by hand. When written by hand we need to provide an inspector.
    // Such a Material will share some properties between it various variant (shader graph variant or hand authored variant).
    // This is the purpose of BaseLitGUI. It contain all properties that are common to all Material based on Lit template.
    // For the default hand written Lit material see LitUI.cs that contain specific properties for our default implementation.
    abstract class BaseLitGUI : BaseUnlitGUI
    {
        protected static class StylesBaseLit
        {
            public static GUIContent doubleSidedNormalModeText = new GUIContent("Normal Mode", "This will modify the normal base on the selected mode. Mirror: Mirror the normal with vertex normal plane, Flip: Flip the normal");
            public static GUIContent depthOffsetEnableText = new GUIContent("Enable Depth Offset", "EnableDepthOffset on this shader (Use with heightmap)");

            // Displacement mapping (POM, tessellation, per vertex)
            //public static GUIContent enablePerPixelDisplacementText = new GUIContent("Enable Per Pixel Displacement", "");

            public static GUIContent displacementModeText = new GUIContent("Displacement Mode", "Apply heightmap displacement to the selected element: Vertex, pixel or tessellated vertex. Pixel displacement must be use with flat surfaces, it is an expensive features and typical usage is paved road.");
            public static GUIContent lockWithObjectScaleText = new GUIContent("Lock with object scale", "Displacement mapping will take the absolute value of the scale of the object into account.");
            public static GUIContent lockWithTilingRateText = new GUIContent("Lock with height map tiling rate", "Displacement mapping will take the absolute value of the tiling rate of the height map into account.");

            // Material ID
            public static GUIContent materialIDText = new GUIContent("Material Type", "Select a material feature to enable on top of regular material");
            public static GUIContent transmissionEnableText = new GUIContent("Enable Transmission", "Enable Transmission for getting  back lighting");

            // Per pixel displacement
            public static GUIContent ppdMinSamplesText = new GUIContent("Minimum steps", "Minimum steps (texture sample) to use with per pixel displacement mapping");
            public static GUIContent ppdMaxSamplesText = new GUIContent("Maximum steps", "Maximum steps (texture sample) to use with per pixel displacement mapping");
            public static GUIContent ppdLodThresholdText = new GUIContent("Fading mip level start", "Starting heightmap mipmap lod number where the parallax occlusion mapping effect start to disappear");
            public static GUIContent ppdPrimitiveLength = new GUIContent("Primitive length", "Dimensions of the primitive (with the scale of 1) to which the per-pixel displacement mapping is being applied. For example, the standard quad is 1 x 1 meter, while the standard plane is 10 x 10 meters.");
            public static GUIContent ppdPrimitiveWidth = new GUIContent("Primitive width", "Dimensions of the primitive (with the scale of 1) to which the per-pixel displacement mapping is being applied. For example, the standard quad is 1 x 1 meter, while the standard plane is 10 x 10 meters.");

            // Tessellation
            public static string tessellationModeStr = "Tessellation Mode";
            public static readonly string[] tessellationModeNames = Enum.GetNames(typeof(TessellationMode));

            public static GUIContent tessellationText = new GUIContent("Tessellation options", "Tessellation options");
            public static GUIContent tessellationFactorText = new GUIContent("Tessellation factor", "This value is the tessellation factor use for tessellation, higher mean more tessellated. Above 15 is costly. Maximum tessellation factor is 15 on XBone / PS4");
            public static GUIContent tessellationFactorMinDistanceText = new GUIContent("Start fade distance", "Distance (in unity unit) at which the tessellation start to fade out. Must be inferior at Max distance");
            public static GUIContent tessellationFactorMaxDistanceText = new GUIContent("End fade distance", "Maximum distance (in unity unit) to the camera where triangle are tessellated");
            public static GUIContent tessellationFactorTriangleSizeText = new GUIContent("Triangle size", "Desired screen space sized of triangle (in pixel). Smaller value mean smaller triangle.");
            public static GUIContent tessellationShapeFactorText = new GUIContent("Shape factor", "Strength of Phong tessellation shape (lerp factor)");
            public static GUIContent tessellationBackFaceCullEpsilonText = new GUIContent("Triangle culling Epsilon", "If -1.0 back face culling is enabled for tessellation, higher number mean more aggressive culling and better performance");

            // Vertex animation
            public static string vertexAnimation = "Vertex Animation";

            // Wind
            public static GUIContent windText = new GUIContent("Enable Wind");
            public static GUIContent windInitialBendText = new GUIContent("Initial Bend");
            public static GUIContent windStiffnessText = new GUIContent("Stiffness");
            public static GUIContent windDragText = new GUIContent("Drag");
            public static GUIContent windShiverDragText = new GUIContent("Shiver Drag");
            public static GUIContent windShiverDirectionalityText = new GUIContent("Shiver Directionality");

            public static GUIContent supportDecalsText = new GUIContent("Enable Decal", "Allow to specify if the material can receive decal or not");

            public static GUIContent enableGeometricSpecularAAText = new GUIContent("Enable geometric specular AA", "This reduce specular aliasing on highly dense mesh (Particularly useful when they don't use normal map)");
            public static GUIContent specularAAScreenSpaceVarianceText = new GUIContent("Screen space variance", "Allow to control the strength of the specular AA reduction. Higher mean more blurry result and less aliasing");
            public static GUIContent specularAAThresholdText = new GUIContent("Threshold", "Allow to limit the effect of specular AA reduction. 0 mean don't apply reduction, higher value mean allow higher reduction");

            // SSR
            public static GUIContent receivesSSRText = new GUIContent("Receives SSR", "Allow to specify if the material can receive SSR or not");

        }

        public enum DoubleSidedNormalMode
        {
            Flip,
            Mirror,
            None
        }

        public enum TessellationMode
        {
            None,
            Phong
        }

        public enum DisplacementMode
        {
            None,
            Vertex,
            Pixel,
            Tessellation
        }

        public enum MaterialId
        {
            LitSSS = 0,
            LitStandard = 1,
            LitAniso = 2,
            LitIridescence = 3,
            LitSpecular = 4,
            LitTranslucent = 5
        };

        public enum HeightmapParametrization
        {
            MinMax = 0,
            Amplitude = 1
        }

        protected MaterialProperty doubleSidedNormalMode = null;
        protected const string kDoubleSidedNormalMode = "_DoubleSidedNormalMode";
        protected MaterialProperty depthOffsetEnable = null;
        protected const string kDepthOffsetEnable = "_DepthOffsetEnable";

        // Properties
        // Material ID
        protected MaterialProperty materialID  = null;
        protected const string kMaterialID = "_MaterialID";
        protected MaterialProperty transmissionEnable = null;
        protected const string kTransmissionEnable = "_TransmissionEnable";

        protected const string kStencilRef = "_StencilRef";
        protected const string kStencilWriteMask = "_StencilWriteMask";
        protected const string kStencilRefMV = "_StencilRefMV";
        protected const string kStencilWriteMaskMV = "_StencilWriteMaskMV";

        protected MaterialProperty displacementMode = null;
        protected const string kDisplacementMode = "_DisplacementMode";
        protected MaterialProperty displacementLockObjectScale = null;
        protected const string kDisplacementLockObjectScale = "_DisplacementLockObjectScale";
        protected MaterialProperty displacementLockTilingScale = null;
        protected const string kDisplacementLockTilingScale = "_DisplacementLockTilingScale";

        // Per pixel displacement params
        protected MaterialProperty ppdMinSamples = null;
        protected const string kPpdMinSamples = "_PPDMinSamples";
        protected MaterialProperty ppdMaxSamples = null;
        protected const string kPpdMaxSamples = "_PPDMaxSamples";
        protected MaterialProperty ppdLodThreshold = null;
        protected const string kPpdLodThreshold = "_PPDLodThreshold";
        protected MaterialProperty ppdPrimitiveLength = null;
        protected const string kPpdPrimitiveLength = "_PPDPrimitiveLength";
        protected MaterialProperty ppdPrimitiveWidth = null;
        protected const string kPpdPrimitiveWidth = "_PPDPrimitiveWidth";
        protected MaterialProperty invPrimScale = null;
        protected const string kInvPrimScale = "_InvPrimScale";

        // Wind
        protected MaterialProperty windEnable = null;
        protected const string kWindEnabled = "_EnableWind";
        protected MaterialProperty windInitialBend = null;
        protected const string kWindInitialBend = "_InitialBend";
        protected MaterialProperty windStiffness = null;
        protected const string kWindStiffness = "_Stiffness";
        protected MaterialProperty windDrag = null;
        protected const string kWindDrag = "_Drag";
        protected MaterialProperty windShiverDrag = null;
        protected const string kWindShiverDrag = "_ShiverDrag";
        protected MaterialProperty windShiverDirectionality = null;
        protected const string kWindShiverDirectionality = "_ShiverDirectionality";

        // tessellation params
        protected MaterialProperty tessellationMode = null;
        protected const string kTessellationMode = "_TessellationMode";
        protected MaterialProperty tessellationFactor = null;
        protected const string kTessellationFactor = "_TessellationFactor";
        protected MaterialProperty tessellationFactorMinDistance = null;
        protected const string kTessellationFactorMinDistance = "_TessellationFactorMinDistance";
        protected MaterialProperty tessellationFactorMaxDistance = null;
        protected const string kTessellationFactorMaxDistance = "_TessellationFactorMaxDistance";
        protected MaterialProperty tessellationFactorTriangleSize = null;
        protected const string kTessellationFactorTriangleSize = "_TessellationFactorTriangleSize";
        protected MaterialProperty tessellationShapeFactor = null;
        protected const string kTessellationShapeFactor = "_TessellationShapeFactor";
        protected MaterialProperty tessellationBackFaceCullEpsilon = null;
        protected const string kTessellationBackFaceCullEpsilon = "_TessellationBackFaceCullEpsilon";

        // Decal
        protected MaterialProperty supportDecals = null;
        protected const string kSupportDecals = "_SupportDecals";
        protected MaterialProperty enableGeometricSpecularAA = null;
        protected const string kEnableGeometricSpecularAA = "_EnableGeometricSpecularAA";
        protected MaterialProperty specularAAScreenSpaceVariance = null;
        protected const string kSpecularAAScreenSpaceVariance = "_SpecularAAScreenSpaceVariance";
        protected MaterialProperty specularAAThreshold = null;
        protected const string kSpecularAAThreshold = "_SpecularAAThreshold";

        // SSR
        protected MaterialProperty receivesSSR = null;
        protected const string kReceivesSSR = "_ReceivesSSR";


        protected override void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            base.FindBaseMaterialProperties(props);

            doubleSidedNormalMode = FindProperty(kDoubleSidedNormalMode, props, false);
            depthOffsetEnable = FindProperty(kDepthOffsetEnable, props, false);

            // MaterialID
            materialID = FindProperty(kMaterialID, props, false);
            transmissionEnable = FindProperty(kTransmissionEnable, props, false);

            displacementMode = FindProperty(kDisplacementMode, props, false);
            displacementLockObjectScale = FindProperty(kDisplacementLockObjectScale, props, false);
            displacementLockTilingScale = FindProperty(kDisplacementLockTilingScale, props, false);

            // Per pixel displacement
            ppdMinSamples = FindProperty(kPpdMinSamples, props, false);
            ppdMaxSamples = FindProperty(kPpdMaxSamples, props, false);
            ppdLodThreshold = FindProperty(kPpdLodThreshold, props, false);
            ppdPrimitiveLength = FindProperty(kPpdPrimitiveLength, props, false);
            ppdPrimitiveWidth  = FindProperty(kPpdPrimitiveWidth, props, false);
            invPrimScale = FindProperty(kInvPrimScale, props, false);

            // tessellation specific, silent if not found
            tessellationMode = FindProperty(kTessellationMode, props, false);
            tessellationFactor = FindProperty(kTessellationFactor, props, false);
            tessellationFactorMinDistance = FindProperty(kTessellationFactorMinDistance, props, false);
            tessellationFactorMaxDistance = FindProperty(kTessellationFactorMaxDistance, props, false);
            tessellationFactorTriangleSize = FindProperty(kTessellationFactorTriangleSize, props, false);
            tessellationShapeFactor = FindProperty(kTessellationShapeFactor, props, false);
            tessellationBackFaceCullEpsilon = FindProperty(kTessellationBackFaceCullEpsilon, props, false);

            // Wind
            windEnable = FindProperty(kWindEnabled, props, false);
            windInitialBend = FindProperty(kWindInitialBend, props, false);
            windStiffness = FindProperty(kWindStiffness, props, false);
            windDrag = FindProperty(kWindDrag, props, false);
            windShiverDrag = FindProperty(kWindShiverDrag, props, false);
            windShiverDirectionality = FindProperty(kWindShiverDirectionality, props, false);

            // Decal
            supportDecals = FindProperty(kSupportDecals, props, false);

            // specular AA
            enableGeometricSpecularAA = FindProperty(kEnableGeometricSpecularAA, props, false);
            specularAAScreenSpaceVariance = FindProperty(kSpecularAAScreenSpaceVariance, props, false);
            specularAAThreshold = FindProperty(kSpecularAAThreshold, props, false);

            // SSR
            receivesSSR = FindProperty(kReceivesSSR, props, false);
        }

        void TessellationModePopup()
        {
            EditorGUI.showMixedValue = tessellationMode.hasMixedValue;
            var mode = (TessellationMode)tessellationMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (TessellationMode)EditorGUILayout.Popup(StylesBaseLit.tessellationModeStr, (int)mode, StylesBaseLit.tessellationModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Tessellation Mode");
                tessellationMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        protected virtual void UpdateDisplacement() {}

        protected override void BaseMaterialPropertiesGUI()
        {
            base.BaseMaterialPropertiesGUI();
            
            // This follow double sided option
            if (doubleSidedEnable != null && doubleSidedEnable.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(doubleSidedNormalMode, StylesBaseLit.doubleSidedNormalModeText);
                EditorGUI.indentLevel--;
            }

            if (materialID != null)
            {
                m_MaterialEditor.ShaderProperty(materialID, StylesBaseLit.materialIDText);

                if ((int)materialID.floatValue == (int)BaseLitGUI.MaterialId.LitSSS)
                {
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(transmissionEnable, StylesBaseLit.transmissionEnableText);
                    EditorGUI.indentLevel--;
                }
            }

            if (supportDecals != null)
            {
                m_MaterialEditor.ShaderProperty(supportDecals, StylesBaseLit.supportDecalsText);
            }            

            if (enableGeometricSpecularAA != null)
            {
                m_MaterialEditor.ShaderProperty(enableGeometricSpecularAA, StylesBaseLit.enableGeometricSpecularAAText);

                if (enableGeometricSpecularAA.floatValue > 0.0)
                {
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(specularAAScreenSpaceVariance, StylesBaseLit.specularAAScreenSpaceVarianceText);
                    m_MaterialEditor.ShaderProperty(specularAAThreshold, StylesBaseLit.specularAAThresholdText);
                    EditorGUI.indentLevel--;
                }
            }

            if(receivesSSR != null)
            {
                m_MaterialEditor.ShaderProperty(receivesSSR, StylesBaseLit.receivesSSRText);
            }

            if (displacementMode != null)
            {
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.ShaderProperty(displacementMode, StylesBaseLit.displacementModeText);
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateDisplacement();
                }

                if ((DisplacementMode)displacementMode.floatValue != DisplacementMode.None)
                {
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(displacementLockObjectScale, StylesBaseLit.lockWithObjectScaleText);
                    m_MaterialEditor.ShaderProperty(displacementLockTilingScale, StylesBaseLit.lockWithTilingRateText);
                    EditorGUI.indentLevel--;
                }

                if ((DisplacementMode)displacementMode.floatValue == DisplacementMode.Pixel)
                {
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(ppdMinSamples, StylesBaseLit.ppdMinSamplesText);
                    m_MaterialEditor.ShaderProperty(ppdMaxSamples, StylesBaseLit.ppdMaxSamplesText);
                    ppdMinSamples.floatValue = Mathf.Min(ppdMinSamples.floatValue, ppdMaxSamples.floatValue);
                    m_MaterialEditor.ShaderProperty(ppdLodThreshold, StylesBaseLit.ppdLodThresholdText);
                    m_MaterialEditor.ShaderProperty(ppdPrimitiveLength, StylesBaseLit.ppdPrimitiveLength);
                    ppdPrimitiveLength.floatValue = Mathf.Max(0.01f, ppdPrimitiveLength.floatValue);
                    m_MaterialEditor.ShaderProperty(ppdPrimitiveWidth, StylesBaseLit.ppdPrimitiveWidth);
                    ppdPrimitiveWidth.floatValue = Mathf.Max(0.01f, ppdPrimitiveWidth.floatValue);
                    invPrimScale.vectorValue = new Vector4(1.0f / ppdPrimitiveLength.floatValue, 1.0f / ppdPrimitiveWidth.floatValue); // Precompute
                    m_MaterialEditor.ShaderProperty(depthOffsetEnable, StylesBaseLit.depthOffsetEnableText);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawDelayedFloatProperty(MaterialProperty prop, GUIContent content)
        {
            Rect position = EditorGUILayout.GetControlRect();
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            float newValue = EditorGUI.DelayedFloatField(position, content, prop.floatValue);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.floatValue = newValue;
        }

        protected virtual void MaterialTesselationPropertiesGUI()
        {
            // Display tessellation option if it exist
            if (tessellationMode != null)
            {
                using (var header = new HeaderScope(StylesBaseLit.tessellationText.text, (uint)Expandable.Tesselation, this))
                {
                    if (header.expanded)
                    {
                        TessellationModePopup();
                        m_MaterialEditor.ShaderProperty(tessellationFactor, StylesBaseLit.tessellationFactorText);
                        DrawDelayedFloatProperty(tessellationFactorMinDistance, StylesBaseLit.tessellationFactorMinDistanceText);
                        DrawDelayedFloatProperty(tessellationFactorMaxDistance, StylesBaseLit.tessellationFactorMaxDistanceText);
                        // clamp min distance to be below max distance
                        tessellationFactorMinDistance.floatValue = Math.Min(tessellationFactorMaxDistance.floatValue, tessellationFactorMinDistance.floatValue);
                        m_MaterialEditor.ShaderProperty(tessellationFactorTriangleSize, StylesBaseLit.tessellationFactorTriangleSizeText);
                        if ((TessellationMode)tessellationMode.floatValue == TessellationMode.Phong)
                        {
                            m_MaterialEditor.ShaderProperty(tessellationShapeFactor, StylesBaseLit.tessellationShapeFactorText);
                        }
                        if (doubleSidedEnable.floatValue == 0.0)
                        {
                            m_MaterialEditor.ShaderProperty(tessellationBackFaceCullEpsilon, StylesBaseLit.tessellationBackFaceCullEpsilonText);
                        }
                    }
                }
            }
        }

        //override for adding Tesselation
        public override void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                using (var header = new HeaderScope(StylesBaseUnlit.optionText, (uint)Expandable.Base, this))
                {
                    if (header.expanded)
                        BaseMaterialPropertiesGUI();
                }
                MaterialTesselationPropertiesGUI();
                VertexAnimationPropertiesGUI();
                MaterialPropertiesGUI(material);
                DoEmissionArea(material);
                using (var header = new HeaderScope(StylesBaseUnlit.advancedText, (uint)Expandable.Advance, this))
                {
                    if (header.expanded)
                    {
                        m_MaterialEditor.EnableInstancingField();
                        MaterialPropertiesAdvanceGUI(material);
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in m_MaterialEditor.targets)
                    SetupMaterialKeywordsAndPassInternal((Material)obj);
            }
        }

        protected override void VertexAnimationPropertiesGUI()
        {
            using (var header = new HeaderScope(StylesBaseLit.vertexAnimation, (uint)Expandable.VertexAnimation, this))
            {
                if (header.expanded)
                {
                    if (windEnable != null)
                    {
                        // Hide wind option. Wind is deprecated and will be remove in the future. Use shader graph instead
                        /*
                        m_MaterialEditor.ShaderProperty(windEnable, StylesBaseLit.windText);
                        if (!windEnable.hasMixedValue && windEnable.floatValue > 0.0f)
                        {
                            EditorGUI.indentLevel++;
                            m_MaterialEditor.ShaderProperty(windInitialBend, StylesBaseLit.windInitialBendText);
                            m_MaterialEditor.ShaderProperty(windStiffness, StylesBaseLit.windStiffnessText);
                            m_MaterialEditor.ShaderProperty(windDrag, StylesBaseLit.windDragText);
                            m_MaterialEditor.ShaderProperty(windShiverDrag, StylesBaseLit.windShiverDragText);
                            m_MaterialEditor.ShaderProperty(windShiverDirectionality, StylesBaseLit.windShiverDirectionalityText);
                            EditorGUI.indentLevel--;
                        }
                        */
                    }

                    if (enableMotionVectorForVertexAnimation != null)
                        m_MaterialEditor.ShaderProperty(enableMotionVectorForVertexAnimation, StylesBaseUnlit.enableMotionVectorForVertexAnimationText);
                }
            }
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupBaseLitKeywords(Material material)
        {
            SetupBaseUnlitKeywords(material);

            bool doubleSidedEnable = material.HasProperty(kDoubleSidedEnable) ? material.GetFloat(kDoubleSidedEnable) > 0.0f : false;
            if (doubleSidedEnable)
            {
                DoubleSidedNormalMode doubleSidedNormalMode = (DoubleSidedNormalMode)material.GetFloat(kDoubleSidedNormalMode);
                switch (doubleSidedNormalMode)
                {
                    case DoubleSidedNormalMode.Mirror: // Mirror mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, -1.0f, 0.0f));
                        break;

                    case DoubleSidedNormalMode.Flip: // Flip mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(-1.0f, -1.0f, -1.0f, 0.0f));
                        break;

                    case DoubleSidedNormalMode.None: // None mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
                        break;
                }
            }

            // Set the reference value for the stencil test.
            int stencilRef = (int)StencilLightingUsage.RegularLighting;
            int stencilWriteMask = (int)HDRenderPipeline.StencilBitMask.LightingMask;
            if (material.HasProperty(kMaterialID) && (int)material.GetFloat(kMaterialID) == (int)BaseLitGUI.MaterialId.LitSSS)
            {
                stencilRef = (int)StencilLightingUsage.SplitLighting;
            }

            if(material.HasProperty(kReceivesSSR) && material.GetInt(kReceivesSSR) == 0)
            {
                stencilWriteMask |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR;
                stencilRef |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR;
            }

            // As we tag both during velocity pass and Gbuffer pass we need a separate state and we need to use the write mask
            material.SetInt(kStencilRef, stencilRef);
            material.SetInt(kStencilWriteMask, stencilWriteMask);
            material.SetInt(kStencilRefMV, (int)HDRenderPipeline.StencilBitMask.ObjectVelocity);
            material.SetInt(kStencilWriteMaskMV, (int)HDRenderPipeline.StencilBitMask.ObjectVelocity);

            if (material.HasProperty(kDisplacementMode))
            {
                bool enableDisplacement = (DisplacementMode)material.GetFloat(kDisplacementMode) != DisplacementMode.None;
                bool enableVertexDisplacement = (DisplacementMode)material.GetFloat(kDisplacementMode) == DisplacementMode.Vertex;
                bool enablePixelDisplacement = (DisplacementMode)material.GetFloat(kDisplacementMode) == DisplacementMode.Pixel;
                bool enableTessellationDisplacement = ((DisplacementMode)material.GetFloat(kDisplacementMode) == DisplacementMode.Tessellation) && material.HasProperty(kTessellationMode);

                CoreUtils.SetKeyword(material, "_VERTEX_DISPLACEMENT", enableVertexDisplacement);
                CoreUtils.SetKeyword(material, "_PIXEL_DISPLACEMENT", enablePixelDisplacement);
                // Only set if tessellation exist
                CoreUtils.SetKeyword(material, "_TESSELLATION_DISPLACEMENT", enableTessellationDisplacement);

                bool displacementLockObjectScale = material.GetFloat(kDisplacementLockObjectScale) > 0.0;
                bool displacementLockTilingScale = material.GetFloat(kDisplacementLockTilingScale) > 0.0;
                // Tessellation reuse vertex flag.
                CoreUtils.SetKeyword(material, "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE", displacementLockObjectScale && (enableVertexDisplacement || enableTessellationDisplacement));
                CoreUtils.SetKeyword(material, "_PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE", displacementLockObjectScale && enablePixelDisplacement);
                CoreUtils.SetKeyword(material, "_DISPLACEMENT_LOCK_TILING_SCALE", displacementLockTilingScale && enableDisplacement);

                // Depth offset is only enabled if per pixel displacement is
                bool depthOffsetEnable = (material.GetFloat(kDepthOffsetEnable) > 0.0f) && enablePixelDisplacement;
                CoreUtils.SetKeyword(material, "_DEPTHOFFSET_ON", depthOffsetEnable);
            }

            bool windEnabled = material.HasProperty(kWindEnabled) && material.GetFloat(kWindEnabled) > 0.0f;
            CoreUtils.SetKeyword(material, "_VERTEX_WIND", windEnabled);

            if (material.HasProperty(kTessellationMode))
            {
                TessellationMode tessMode = (TessellationMode)material.GetFloat(kTessellationMode);
                CoreUtils.SetKeyword(material, "_TESSELLATION_PHONG", tessMode == TessellationMode.Phong);
            }

            SetupMainTexForAlphaTestGI("_BaseColorMap", "_BaseColor", material);

            // Use negation so we don't create keyword by default
            CoreUtils.SetKeyword(material, "_DISABLE_DECALS", material.HasProperty(kSupportDecals) && material.GetFloat(kSupportDecals) == 0.0);
            CoreUtils.SetKeyword(material, "_DISABLE_SSR", material.HasProperty(kReceivesSSR) && material.GetFloat(kReceivesSSR) == 0.0);
            CoreUtils.SetKeyword(material, "_ENABLE_GEOMETRIC_SPECULAR_AA", material.HasProperty(kEnableGeometricSpecularAA) && material.GetFloat(kEnableGeometricSpecularAA) == 1.0);
        }

        static public void SetupBaseLitMaterialPass(Material material)
        {
            SetupBaseUnlitMaterialPass(material);
        }
    }
} // namespace UnityEditor
