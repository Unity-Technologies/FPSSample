using System.Collections.Generic;
using System;
using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    namespace Attributes
    {
        // 0 is reserved!
        [GenerateHLSL]
        public enum DebugViewVarying
        {
            None = 0,
            Texcoord0 = 1,
            Texcoord1,
            Texcoord2,
            Texcoord3,
            VertexTangentWS,
            VertexBitangentWS,
            VertexNormalWS,
            VertexColor,
            VertexColorAlpha,
            // if you add more values here, fix the first entry of next enum
        };

        // Number must be contiguous
        [GenerateHLSL]
        public enum DebugViewGbuffer
        {
            None = 0,
            Depth = DebugViewVarying.VertexColorAlpha + 1,
            BakeDiffuseLightingWithAlbedoPlusEmissive,
            BakeShadowMask0,
            BakeShadowMask1,
            BakeShadowMask2,
            BakeShadowMask3,
            // if you add more values here, fix the first entry of next enum
        }

        // Number must be contiguous
        [GenerateHLSL]
        public enum DebugViewProperties
        {
            None = 0,
            Tessellation = DebugViewGbuffer.BakeShadowMask3 + 1,
            PixelDisplacement,
            VertexDisplacement,
            TessellationDisplacement,
            DepthOffset,
            Lightmap,
            Instancing,
        }
    }

    [Serializable]
    public class MaterialDebugSettings
    {
        static bool isDebugViewMaterialInit = false;

        public static GUIContent[] debugViewMaterialStrings = null;
        public static int[] debugViewMaterialValues = null;
        public static GUIContent[] debugViewEngineStrings = null;
        public static int[] debugViewEngineValues = null;
        public static GUIContent[] debugViewMaterialVaryingStrings = null;
        public static int[] debugViewMaterialVaryingValues = null;
        public static GUIContent[] debugViewMaterialPropertiesStrings = null;
        public static int[] debugViewMaterialPropertiesValues = null;
        public static GUIContent[] debugViewMaterialTextureStrings = null;
        public static int[] debugViewMaterialTextureValues = null;
        public static GUIContent[] debugViewMaterialGBufferStrings = null;
        public static int[] debugViewMaterialGBufferValues = null;

        public MaterialDebugSettings()
        {
            BuildDebugRepresentation();
        }

        // className include the additional "/"
        void FillWithProperties(Type type, ref List<GUIContent> debugViewMaterialStringsList, ref List<int> debugViewMaterialValuesList, string className)
        {
            var attributes = type.GetCustomAttributes(true);
            // Get attribute to get the start number of the value for the enum
            var attr = attributes[0] as GenerateHLSL;

            if (!attr.needParamDebug)
            {
                return;
            }

            var fields = type.GetFields();

            var localIndex = 0;
            foreach (var field in fields)
            {
                // Note: One field can have multiple name. This is to allow to have different debug view mode for the same field
                // like for example display normal in world space or in view space. Same field but two different modes.
                List<String> displayNames = new List<string>();
                displayNames.Add(field.Name);

                // Check if the display name have been override by the users
                if (Attribute.IsDefined(field, typeof(SurfaceDataAttributes)))
                {
                    var propertyAttr = (SurfaceDataAttributes[])field.GetCustomAttributes(typeof(SurfaceDataAttributes), false);
                    if (propertyAttr[0].displayNames.Length > 0 && propertyAttr[0].displayNames[0] != "")
                    {
                        displayNames.Clear();

                        displayNames.AddRange(propertyAttr[0].displayNames);
                    }
                }

                foreach (string fieldName in displayNames)
                {
                    debugViewMaterialStringsList.Add(new GUIContent(className + fieldName));
                    debugViewMaterialValuesList.Add(attr.paramDefinesStart + (int)localIndex);
                    localIndex++;
                }
            }
        }

        void FillWithPropertiesEnum(Type type, ref List<GUIContent> debugViewMaterialStringsList, ref List<int> debugViewMaterialValuesList, string prefix)
        {
            var names = Enum.GetNames(type);

            var localIndex = 0;
            foreach (var value in Enum.GetValues(type))
            {
                var valueName = prefix + names[localIndex];

                debugViewMaterialStringsList.Add(new GUIContent(valueName));
                debugViewMaterialValuesList.Add((int)value);
                localIndex++;
            }
        }

        public class MaterialItem
        {
            public String className;
            public Type surfaceDataType;
            public Type bsdfDataType;
        };

        void BuildDebugRepresentation()
        {
            if (!isDebugViewMaterialInit)
            {
                List<RenderPipelineMaterial> materialList = HDUtils.GetRenderPipelineMaterialList();

                // TODO: Share this code to retrieve deferred material with HDRenderPipeline
                // Find first material that is a deferredMaterial
                Type bsdfDataDeferredType = null;
                foreach (RenderPipelineMaterial material in materialList)
                {
                    if (material.IsDefferedMaterial())
                    {
                        bsdfDataDeferredType = material.GetType().GetNestedType("BSDFData");
                    }
                }

                // TODO: Handle the case of no Gbuffer material
                Debug.Assert(bsdfDataDeferredType != null);

                List<MaterialItem> materialItems = new List<MaterialItem>();

                int numSurfaceDataFields = 0;
                int numBSDFDataFields = 0;
                foreach (RenderPipelineMaterial material in materialList)
                {
                    MaterialItem item = new MaterialItem();

                    item.className = material.GetType().Name + "/";

                    item.surfaceDataType = material.GetType().GetNestedType("SurfaceData");
                    numSurfaceDataFields += item.surfaceDataType.GetFields().Length;

                    item.bsdfDataType = material.GetType().GetNestedType("BSDFData");
                    numBSDFDataFields += item.bsdfDataType.GetFields().Length;

                    materialItems.Add(item);
                }

                // Init list
                List<GUIContent> debugViewMaterialStringsList = new List<GUIContent>();
                List<int> debugViewMaterialValuesList = new List<int>();
                List<GUIContent> debugViewEngineStringsList = new List<GUIContent>();
                List<int> debugViewEngineValuesList = new List<int>();
                List<GUIContent> debugViewMaterialVaryingStringsList = new List<GUIContent>();
                List<int> debugViewMaterialVaryingValuesList = new List<int>();
                List<GUIContent> debugViewMaterialPropertiesStringsList = new List<GUIContent>();
                List<int> debugViewMaterialPropertiesValuesList = new List<int>();
                List<GUIContent> debugViewMaterialTextureStringsList = new List<GUIContent>();
                List<int> debugViewMaterialTextureValuesList = new List<int>();
                List<GUIContent> debugViewMaterialGBufferStringsList = new List<GUIContent>();
                List<int> debugViewMaterialGBufferValuesList = new List<int>();

                // First element is a reserved location and should not be used (allow to track error)
                // Special case for None since it cannot be inferred from SurfaceData/BuiltinData
                debugViewMaterialStringsList.Add(new GUIContent("None"));
                debugViewMaterialValuesList.Add(0);

                foreach (MaterialItem item in materialItems)
                {
                    // BuiltinData are duplicated for each material
                    // Giving the material specific types allow to move iterator at a separate range for each material
                    // Otherwise, all BuiltinData will be at same offset and will broke the enum
                    FillWithProperties(typeof(Builtin.BuiltinData), ref debugViewMaterialStringsList, ref debugViewMaterialValuesList, item.className);
                    FillWithProperties(item.surfaceDataType, ref debugViewMaterialStringsList, ref debugViewMaterialValuesList, item.className);
                }

                // Engine properties debug
                // First element is a reserved location and should not be used (allow to track error)
                // Special case for None since it cannot be inferred from SurfaceData/BuiltinData
                debugViewEngineStringsList.Add(new GUIContent("None"));
                debugViewEngineValuesList.Add(0);

                foreach (MaterialItem item in materialItems)
                {
                    FillWithProperties(item.bsdfDataType, ref debugViewEngineStringsList, ref debugViewEngineValuesList, item.className);
                }

                // For the following, no need to reserve the 0 case as it is handled in the Enum

                // Attributes debug
                FillWithPropertiesEnum(typeof(DebugViewVarying), ref debugViewMaterialVaryingStringsList, ref debugViewMaterialVaryingValuesList, "");

                // Properties debug
                FillWithPropertiesEnum(typeof(DebugViewProperties), ref debugViewMaterialPropertiesStringsList, ref debugViewMaterialPropertiesValuesList, "");

                // Gbuffer debug
                FillWithPropertiesEnum(typeof(DebugViewGbuffer), ref debugViewMaterialGBufferStringsList, ref debugViewMaterialGBufferValuesList, "");
                FillWithProperties(typeof(Lit.BSDFData), ref debugViewMaterialGBufferStringsList, ref debugViewMaterialGBufferValuesList, "");

                // Convert to array for UI
                debugViewMaterialStrings = debugViewMaterialStringsList.ToArray();
                debugViewMaterialValues = debugViewMaterialValuesList.ToArray();

                debugViewEngineStrings = debugViewEngineStringsList.ToArray();
                debugViewEngineValues = debugViewEngineValuesList.ToArray();

                debugViewMaterialVaryingStrings = debugViewMaterialVaryingStringsList.ToArray();
                debugViewMaterialVaryingValues = debugViewMaterialVaryingValuesList.ToArray();

                debugViewMaterialPropertiesStrings = debugViewMaterialPropertiesStringsList.ToArray();
                debugViewMaterialPropertiesValues = debugViewMaterialPropertiesValuesList.ToArray();

                debugViewMaterialTextureStrings = debugViewMaterialTextureStringsList.ToArray();
                debugViewMaterialTextureValues = debugViewMaterialTextureValuesList.ToArray();

                debugViewMaterialGBufferStrings = debugViewMaterialGBufferStringsList.ToArray();
                debugViewMaterialGBufferValues = debugViewMaterialGBufferValuesList.ToArray();

                isDebugViewMaterialInit = true;
            }
        }

        public int debugViewMaterial { get { return m_DebugViewMaterial; } }
        public int debugViewEngine { get { return m_DebugViewEngine; } }
        public DebugViewVarying debugViewVarying { get { return m_DebugViewVarying; } }
        public DebugViewProperties debugViewProperties { get { return m_DebugViewProperties; } }
        public int debugViewGBuffer { get { return m_DebugViewGBuffer; } }

        int                             m_DebugViewMaterial = 0; // No enum there because everything is generated from materials.
        int                             m_DebugViewEngine = 0;  // No enum there because everything is generated from BSDFData
        DebugViewVarying     m_DebugViewVarying = DebugViewVarying.None;
        DebugViewProperties  m_DebugViewProperties = DebugViewProperties.None;
        int                             m_DebugViewGBuffer = 0; // Can't use GBuffer enum here because the values are actually split between this enum and values from Lit.BSDFData

        public int GetDebugMaterialIndex()
        {
            // This value is used in the shader for the actual debug display.
            // There is only one uniform parameter for that so we just add all of them
            // They are all mutually exclusive so return the sum will return the right index.
            return m_DebugViewGBuffer + m_DebugViewMaterial + m_DebugViewEngine + (int)m_DebugViewVarying + (int)m_DebugViewProperties;
        }

        public void DisableMaterialDebug()
        {
            m_DebugViewMaterial = 0;
            m_DebugViewEngine = 0;
            m_DebugViewVarying = DebugViewVarying.None;
            m_DebugViewProperties = DebugViewProperties.None;
            m_DebugViewGBuffer = 0;
        }

        public void SetDebugViewMaterial(int value)
        {
            if (value != 0)
                DisableMaterialDebug();
            m_DebugViewMaterial = value;
        }

        public void SetDebugViewEngine(int value)
        {
            if (value != 0)
                DisableMaterialDebug();
            m_DebugViewEngine = value;
        }

        public void SetDebugViewVarying(DebugViewVarying value)
        {
            if (value != 0)
                DisableMaterialDebug();
            m_DebugViewVarying = value;
        }

        public void SetDebugViewProperties(DebugViewProperties value)
        {
            if (value != 0)
                DisableMaterialDebug();
            m_DebugViewProperties = value;
        }

        public void SetDebugViewGBuffer(int value)
        {
            if (value != 0)
                DisableMaterialDebug();
            m_DebugViewGBuffer = value;
        }

        public bool IsDebugGBufferEnabled()
        {
            return m_DebugViewGBuffer != 0;
        }

        public bool IsDebugDisplayEnabled()
        {
            return (m_DebugViewEngine != 0 || m_DebugViewMaterial != 0 || m_DebugViewVarying != DebugViewVarying.None || m_DebugViewProperties != DebugViewProperties.None || m_DebugViewGBuffer != 0);
        }
    }
}
