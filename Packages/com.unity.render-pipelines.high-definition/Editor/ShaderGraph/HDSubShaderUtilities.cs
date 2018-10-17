using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;              // Vector3,4
using UnityEditor.ShaderGraph;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal static class HDRPShaderStructs
    {
        internal struct AttributesMesh
        {
            [Semantic("POSITION")]                  Vector3 positionOS;
            [Semantic("NORMAL")][Optional]          Vector3 normalOS;
            [Semantic("TANGENT")][Optional]         Vector4 tangentOS;       // Stores bi-tangent sign in w
            [Semantic("TEXCOORD0")][Optional]       Vector4 uv0;
            [Semantic("TEXCOORD1")][Optional]       Vector4 uv1;
            [Semantic("TEXCOORD2")][Optional]       Vector4 uv2;
            [Semantic("TEXCOORD3")][Optional]       Vector4 uv3;
            [Semantic("COLOR")][Optional]           Vector4 color;
            [Semantic("INSTANCEID_SEMANTIC")] [PreprocessorIf("INSTANCING_ON")] uint instanceID;
        };

        [InterpolatorPack]
        internal struct VaryingsMeshToPS
        {
            [Semantic("SV_Position")]                                               Vector4 positionCS;
            [Optional]                                                              Vector3 positionRWS;
            [Optional]                                                              Vector3 normalWS;
            [Optional]                                                              Vector4 tangentWS;      // w contain mirror sign
            [Optional]                                                              Vector4 texCoord0;
            [Optional]                                                              Vector4 texCoord1;
            [Optional]                                                              Vector4 texCoord2;
            [Optional]                                                              Vector4 texCoord3;
            [Optional]                                                              Vector4 color;
            [Semantic("INSTANCEID_SEMANTIC")] [PreprocessorIf("INSTANCING_ON")]     uint instanceID;
            [Optional][Semantic("FRONT_FACE_SEMANTIC")][OverrideType("FRONT_FACE_TYPE")][PreprocessorIf("SHADER_STAGE_FRAGMENT")] bool cullFace;

            public static Dependency[] tessellationDependencies = new Dependency[]
            {
                new Dependency("VaryingsMeshToPS.positionRWS",       "VaryingsMeshToDS.positionRWS"),
                new Dependency("VaryingsMeshToPS.normalWS",         "VaryingsMeshToDS.normalWS"),
                new Dependency("VaryingsMeshToPS.tangentWS",        "VaryingsMeshToDS.tangentWS"),
                new Dependency("VaryingsMeshToPS.texCoord0",        "VaryingsMeshToDS.texCoord0"),
                new Dependency("VaryingsMeshToPS.texCoord1",        "VaryingsMeshToDS.texCoord1"),
                new Dependency("VaryingsMeshToPS.texCoord2",        "VaryingsMeshToDS.texCoord2"),
                new Dependency("VaryingsMeshToPS.texCoord3",        "VaryingsMeshToDS.texCoord3"),
                new Dependency("VaryingsMeshToPS.color",            "VaryingsMeshToDS.color"),
                new Dependency("VaryingsMeshToPS.instanceID",       "VaryingsMeshToDS.instanceID"),
            };

            public static Dependency[] standardDependencies = new Dependency[]
            {
                new Dependency("VaryingsMeshToPS.positionRWS",       "AttributesMesh.positionOS"),
                new Dependency("VaryingsMeshToPS.normalWS",         "AttributesMesh.normalOS"),
                new Dependency("VaryingsMeshToPS.tangentWS",        "AttributesMesh.tangentOS"),
                new Dependency("VaryingsMeshToPS.texCoord0",        "AttributesMesh.uv0"),
                new Dependency("VaryingsMeshToPS.texCoord1",        "AttributesMesh.uv1"),
                new Dependency("VaryingsMeshToPS.texCoord2",        "AttributesMesh.uv2"),
                new Dependency("VaryingsMeshToPS.texCoord3",        "AttributesMesh.uv3"),
                new Dependency("VaryingsMeshToPS.color",            "AttributesMesh.color"),
                new Dependency("VaryingsMeshToPS.instanceID",       "AttributesMesh.instanceID"),
            };
        };

        [InterpolatorPack]
        internal struct VaryingsMeshToDS
        {
            Vector3 positionRWS;
            Vector3 normalWS;
            [Optional]      Vector4 tangentWS;
            [Optional]      Vector4 texCoord0;
            [Optional]      Vector4 texCoord1;
            [Optional]      Vector4 texCoord2;
            [Optional]      Vector4 texCoord3;
            [Optional]      Vector4 color;
            [Semantic("INSTANCEID_SEMANTIC")] [PreprocessorIf("INSTANCING_ON")] uint instanceID;

            public static Dependency[] tessellationDependencies = new Dependency[]
            {
                new Dependency("VaryingsMeshToDS.tangentWS",     "VaryingsMeshToPS.tangentWS"),
                new Dependency("VaryingsMeshToDS.texCoord0",     "VaryingsMeshToPS.texCoord0"),
                new Dependency("VaryingsMeshToDS.texCoord1",     "VaryingsMeshToPS.texCoord1"),
                new Dependency("VaryingsMeshToDS.texCoord2",     "VaryingsMeshToPS.texCoord2"),
                new Dependency("VaryingsMeshToDS.texCoord3",     "VaryingsMeshToPS.texCoord3"),
                new Dependency("VaryingsMeshToDS.color",         "VaryingsMeshToPS.color"),
                new Dependency("VaryingsMeshToDS.instanceID",    "VaryingsMeshToPS.instanceID"),
            };
        };

        internal struct FragInputs
        {
            public static Dependency[] dependencies = new Dependency[]
            {
                new Dependency("FragInputs.positionRWS",        "VaryingsMeshToPS.positionRWS"),
                new Dependency("FragInputs.worldToTangent",     "VaryingsMeshToPS.tangentWS"),
                new Dependency("FragInputs.worldToTangent",     "VaryingsMeshToPS.normalWS"),
                new Dependency("FragInputs.texCoord0",          "VaryingsMeshToPS.texCoord0"),
                new Dependency("FragInputs.texCoord1",          "VaryingsMeshToPS.texCoord1"),
                new Dependency("FragInputs.texCoord2",          "VaryingsMeshToPS.texCoord2"),
                new Dependency("FragInputs.texCoord3",          "VaryingsMeshToPS.texCoord3"),
                new Dependency("FragInputs.color",              "VaryingsMeshToPS.color"),
                new Dependency("FragInputs.isFrontFace",        "VaryingsMeshToPS.cullFace"),
            };
        };

        // this describes the input to the pixel shader graph eval
        internal struct SurfaceDescriptionInputs
        {
            [Optional] Vector3 ObjectSpaceNormal;
            [Optional] Vector3 ViewSpaceNormal;
            [Optional] Vector3 WorldSpaceNormal;
            [Optional] Vector3 TangentSpaceNormal;

            [Optional] Vector3 ObjectSpaceTangent;
            [Optional] Vector3 ViewSpaceTangent;
            [Optional] Vector3 WorldSpaceTangent;
            [Optional] Vector3 TangentSpaceTangent;

            [Optional] Vector3 ObjectSpaceBiTangent;
            [Optional] Vector3 ViewSpaceBiTangent;
            [Optional] Vector3 WorldSpaceBiTangent;
            [Optional] Vector3 TangentSpaceBiTangent;

            [Optional] Vector3 ObjectSpaceViewDirection;
            [Optional] Vector3 ViewSpaceViewDirection;
            [Optional] Vector3 WorldSpaceViewDirection;
            [Optional] Vector3 TangentSpaceViewDirection;

            [Optional] Vector3 ObjectSpacePosition;
            [Optional] Vector3 ViewSpacePosition;
            [Optional] Vector3 WorldSpacePosition;
            [Optional] Vector3 TangentSpacePosition;

            [Optional] Vector4 ScreenPosition;
            [Optional] Vector4 uv0;
            [Optional] Vector4 uv1;
            [Optional] Vector4 uv2;
            [Optional] Vector4 uv3;
            [Optional] Vector4 VertexColor;
            [Optional] float FaceSign;

            public static Dependency[] dependencies = new Dependency[]
            {
                new Dependency("SurfaceDescriptionInputs.WorldSpaceNormal",          "FragInputs.worldToTangent"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceNormal",         "SurfaceDescriptionInputs.WorldSpaceNormal"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceNormal",           "SurfaceDescriptionInputs.WorldSpaceNormal"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceTangent",         "FragInputs.worldToTangent"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceTangent",        "SurfaceDescriptionInputs.WorldSpaceTangent"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceTangent",          "SurfaceDescriptionInputs.WorldSpaceTangent"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceBiTangent",       "FragInputs.worldToTangent"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceBiTangent",      "SurfaceDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceBiTangent",        "SurfaceDescriptionInputs.WorldSpaceBiTangent"),

                new Dependency("SurfaceDescriptionInputs.WorldSpacePosition",        "FragInputs.positionRWS"),
                new Dependency("SurfaceDescriptionInputs.ObjectSpacePosition",       "FragInputs.positionRWS"),
                new Dependency("SurfaceDescriptionInputs.ViewSpacePosition",         "FragInputs.positionRWS"),

                new Dependency("SurfaceDescriptionInputs.WorldSpaceViewDirection",   "FragInputs.positionRWS"),                   // we build WorldSpaceViewDirection using FragInputs.positionRWS in GetWorldSpaceNormalizeViewDir()
                new Dependency("SurfaceDescriptionInputs.ObjectSpaceViewDirection",  "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.ViewSpaceViewDirection",    "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceTangent"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("SurfaceDescriptionInputs.TangentSpaceViewDirection", "SurfaceDescriptionInputs.WorldSpaceNormal"),

                new Dependency("SurfaceDescriptionInputs.ScreenPosition",            "SurfaceDescriptionInputs.WorldSpacePosition"),
                new Dependency("SurfaceDescriptionInputs.uv0",                       "FragInputs.texCoord0"),
                new Dependency("SurfaceDescriptionInputs.uv1",                       "FragInputs.texCoord1"),
                new Dependency("SurfaceDescriptionInputs.uv2",                       "FragInputs.texCoord2"),
                new Dependency("SurfaceDescriptionInputs.uv3",                       "FragInputs.texCoord3"),
                new Dependency("SurfaceDescriptionInputs.VertexColor",               "FragInputs.color"),
                new Dependency("SurfaceDescriptionInputs.FaceSign",                  "FragInputs.isFrontFace"),
            };
        };

        // this describes the input to the pixel shader graph eval
        internal struct VertexDescriptionInputs
        {
            [Optional] Vector3 ObjectSpaceNormal;
            [Optional] Vector3 ViewSpaceNormal;
            [Optional] Vector3 WorldSpaceNormal;
            [Optional] Vector3 TangentSpaceNormal;

            [Optional] Vector3 ObjectSpaceTangent;
            [Optional] Vector3 ViewSpaceTangent;
            [Optional] Vector3 WorldSpaceTangent;
            [Optional] Vector3 TangentSpaceTangent;

            [Optional] Vector3 ObjectSpaceBiTangent;
            [Optional] Vector3 ViewSpaceBiTangent;
            [Optional] Vector3 WorldSpaceBiTangent;
            [Optional] Vector3 TangentSpaceBiTangent;

            [Optional] Vector3 ObjectSpaceViewDirection;
            [Optional] Vector3 ViewSpaceViewDirection;
            [Optional] Vector3 WorldSpaceViewDirection;
            [Optional] Vector3 TangentSpaceViewDirection;

            [Optional] Vector3 ObjectSpacePosition;
            [Optional] Vector3 ViewSpacePosition;
            [Optional] Vector3 WorldSpacePosition;
            [Optional] Vector3 TangentSpacePosition;

            [Optional] Vector4 ScreenPosition;
            [Optional] Vector4 uv0;
            [Optional] Vector4 uv1;
            [Optional] Vector4 uv2;
            [Optional] Vector4 uv3;
            [Optional] Vector4 VertexColor;

            public static Dependency[] dependencies = new Dependency[]
            {                                                                       // TODO: NOCHECKIN: these dependencies are not correct for vertex pass
                new Dependency("VertexDescriptionInputs.ObjectSpaceNormal",         "AttributesMesh.normalOS"),
                new Dependency("VertexDescriptionInputs.WorldSpaceNormal",          "AttributesMesh.normalOS"),
                new Dependency("VertexDescriptionInputs.ViewSpaceNormal",           "VertexDescriptionInputs.WorldSpaceNormal"),

                new Dependency("VertexDescriptionInputs.ObjectSpaceTangent",        "AttributesMesh.tangentOS"),
                new Dependency("VertexDescriptionInputs.WorldSpaceTangent",         "AttributesMesh.tangentOS"),
                new Dependency("VertexDescriptionInputs.ViewSpaceTangent",          "VertexDescriptionInputs.WorldSpaceTangent"),

                new Dependency("VertexDescriptionInputs.ObjectSpaceBiTangent",      "AttributesMesh.normalOS"),
                new Dependency("VertexDescriptionInputs.ObjectSpaceBiTangent",      "AttributesMesh.tangentOS"),
                new Dependency("VertexDescriptionInputs.WorldSpaceBiTangent",       "VertexDescriptionInputs.ObjectSpaceBiTangent"),
                new Dependency("VertexDescriptionInputs.ViewSpaceBiTangent",        "VertexDescriptionInputs.WorldSpaceBiTangent"),

                new Dependency("VertexDescriptionInputs.ObjectSpacePosition",       "AttributesMesh.positionOS"),
                new Dependency("VertexDescriptionInputs.WorldSpacePosition",        "AttributesMesh.positionOS"),
                new Dependency("VertexDescriptionInputs.ViewSpacePosition",         "VertexDescriptionInputs.WorldSpacePosition"),

                new Dependency("VertexDescriptionInputs.WorldSpaceViewDirection",   "VertexDescriptionInputs.WorldSpacePosition"),
                new Dependency("VertexDescriptionInputs.ObjectSpaceViewDirection",  "VertexDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("VertexDescriptionInputs.ViewSpaceViewDirection",    "VertexDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceViewDirection"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceTangent"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceBiTangent"),
                new Dependency("VertexDescriptionInputs.TangentSpaceViewDirection", "VertexDescriptionInputs.WorldSpaceNormal"),

                new Dependency("VertexDescriptionInputs.ScreenPosition",            "VertexDescriptionInputs.WorldSpacePosition"),
                new Dependency("VertexDescriptionInputs.uv0",                       "AttributesMesh.uv0"),
                new Dependency("VertexDescriptionInputs.uv1",                       "AttributesMesh.uv1"),
                new Dependency("VertexDescriptionInputs.uv2",                       "AttributesMesh.uv2"),
                new Dependency("VertexDescriptionInputs.uv3",                       "AttributesMesh.uv3"),
                new Dependency("VertexDescriptionInputs.VertexColor",               "AttributesMesh.color"),
            };
        };

        // TODO: move this out of HDRPShaderStructs
        static public void AddActiveFieldsFromVertexGraphRequirements(HashSet<string> activeFields, ShaderGraphRequirements requirements)
        {
            if (requirements.requiresScreenPosition)
            {
                activeFields.Add("VertexDescriptionInputs.ScreenPosition");
            }

            if (requirements.requiresVertexColor)
            {
                activeFields.Add("VertexDescriptionInputs.VertexColor");
            }

            if (requirements.requiresNormal != 0)
            {
                if ((requirements.requiresNormal & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("VertexDescriptionInputs.ObjectSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("VertexDescriptionInputs.ViewSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("VertexDescriptionInputs.WorldSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("VertexDescriptionInputs.TangentSpaceNormal");
            }

            if (requirements.requiresTangent != 0)
            {
                if ((requirements.requiresTangent & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("VertexDescriptionInputs.ObjectSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("VertexDescriptionInputs.ViewSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("VertexDescriptionInputs.WorldSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("VertexDescriptionInputs.TangentSpaceTangent");
            }

            if (requirements.requiresBitangent != 0)
            {
                if ((requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("VertexDescriptionInputs.ObjectSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("VertexDescriptionInputs.ViewSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("VertexDescriptionInputs.WorldSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("VertexDescriptionInputs.TangentSpaceBiTangent");
            }

            if (requirements.requiresViewDir != 0)
            {
                if ((requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("VertexDescriptionInputs.ObjectSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("VertexDescriptionInputs.ViewSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("VertexDescriptionInputs.WorldSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("VertexDescriptionInputs.TangentSpaceViewDirection");
            }

            if (requirements.requiresPosition != 0)
            {
                if ((requirements.requiresPosition & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("VertexDescriptionInputs.ObjectSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("VertexDescriptionInputs.ViewSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("VertexDescriptionInputs.WorldSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("VertexDescriptionInputs.TangentSpacePosition");
            }

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
            {
                activeFields.Add("VertexDescriptionInputs." + channel.GetUVName());
            }
        }

        // TODO: move this out of HDRPShaderStructs
        static public void AddActiveFieldsFromPixelGraphRequirements(HashSet<string> activeFields, ShaderGraphRequirements requirements)
        {
            if (requirements.requiresScreenPosition)
            {
                activeFields.Add("SurfaceDescriptionInputs.ScreenPosition");
            }

            if (requirements.requiresVertexColor)
            {
                activeFields.Add("SurfaceDescriptionInputs.VertexColor");
            }

            if (requirements.requiresFaceSign)
            {
                activeFields.Add("SurfaceDescriptionInputs.FaceSign");
            }

            if (requirements.requiresNormal != 0)
            {
                if ((requirements.requiresNormal & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ObjectSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ViewSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.WorldSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.TangentSpaceNormal");
            }

            if (requirements.requiresTangent != 0)
            {
                if ((requirements.requiresTangent & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ObjectSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ViewSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.WorldSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.TangentSpaceTangent");
            }

            if (requirements.requiresBitangent != 0)
            {
                if ((requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ObjectSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ViewSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.WorldSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.TangentSpaceBiTangent");
            }

            if (requirements.requiresViewDir != 0)
            {
                if ((requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ObjectSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ViewSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.WorldSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.TangentSpaceViewDirection");
            }

            if (requirements.requiresPosition != 0)
            {
                if ((requirements.requiresPosition & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ObjectSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.ViewSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.WorldSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("SurfaceDescriptionInputs.TangentSpacePosition");
            }

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
            {
                activeFields.Add("SurfaceDescriptionInputs." + channel.GetUVName());
            }
        }

        public static void AddRequiredFields(
            List<string> passRequiredFields,            // fields the pass requires
            HashSet<string> activeFields)
        {
            if (passRequiredFields != null)
            {
                foreach (var requiredField in passRequiredFields)
                {
                    activeFields.Add(requiredField);
                }
            }
        }
    };

    public delegate void OnGeneratePassDelegate(IMasterNode masterNode, ref Pass pass);
    public struct Pass
    {
        public string Name;
        public string LightMode;
        public string ShaderPassName;
        public List<string> Includes;
        public string TemplateName;
        public string MaterialName;
        public List<string> ExtraDefines;
        public List<int> VertexShaderSlots;         // These control what slots are used by the pass vertex shader
        public List<int> PixelShaderSlots;          // These control what slots are used by the pass pixel shader
        public string CullOverride;
        public string BlendOverride;
        public string BlendOpOverride;
        public string ZTestOverride;
        public string ZWriteOverride;
        public string ColorMaskOverride;
        public List<string> StencilOverride;
        public List<string> RequiredFields;         // feeds into the dependency analysis
        public ShaderGraphRequirements requirements;
        public bool UseInPreview;

        // All these lists could probably be hashed to aid lookups.
        public bool VertexShaderUsesSlot(int slotId)
        {
            return VertexShaderSlots.Contains(slotId);
        }
        public bool PixelShaderUsesSlot(int slotId)
        {
            return PixelShaderSlots.Contains(slotId);
        }
        public void OnGeneratePass(IMasterNode masterNode)
        {
            if (OnGeneratePassImpl != null)
            {
                OnGeneratePassImpl(masterNode, ref this);
            }
        }
        public OnGeneratePassDelegate OnGeneratePassImpl;
    }

    public static class HDSubShaderUtilities
    {
        public static bool GenerateShaderPass(AbstractMaterialNode masterNode, Pass pass, GenerationMode mode, SurfaceMaterialOptions materialOptions, HashSet<string> activeFields, ShaderGenerator result, List<string> sourceAssetDependencyPaths, bool vertexActive)
        {
            string templatePath = Path.Combine(HDUtils.GetHDRenderPipelinePath(), "Editor/Material");
            string templateLocation = Path.Combine(Path.Combine(Path.Combine(templatePath, pass.MaterialName), "ShaderGraph"), pass.TemplateName);
            if (!File.Exists(templateLocation))
            {
                // TODO: produce error here
                Debug.LogError("Template not found: " + templateLocation);
                return false;
            }

            bool debugOutput = false;

            // grab all of the active nodes (for pixel and vertex graphs)
            var vertexNodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(vertexNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.VertexShaderSlots);

            var pixelNodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(pixelNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots);

            // graph requirements describe what the graph itself requires
            var pixelRequirements = ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment, false);   // TODO: is ShaderStageCapability.Fragment correct?
            var vertexRequirements = ShaderGraphRequirements.FromNodes(vertexNodes, ShaderStageCapability.Vertex, false);
            var graphRequirements = pixelRequirements.Union(vertexRequirements);

            // Function Registry tracks functions to remove duplicates, it wraps a string builder that stores the combined function string
            ShaderStringBuilder graphNodeFunctions = new ShaderStringBuilder();
            graphNodeFunctions.IncreaseIndent();
            var functionRegistry = new FunctionRegistry(graphNodeFunctions);

            // TODO: this can be a shared function for all HDRP master nodes -- From here through GraphUtil.GenerateSurfaceDescription(..)

            // Build the list of active slots based on what the pass requires
            var pixelSlots = HDSubShaderUtilities.FindMaterialSlotsOnNode(pass.PixelShaderSlots, masterNode);
            var vertexSlots = HDSubShaderUtilities.FindMaterialSlotsOnNode(pass.VertexShaderSlots, masterNode);

            // properties used by either pixel and vertex shader
            PropertyCollector sharedProperties = new PropertyCollector();

            // build the graph outputs structure to hold the results of each active slots (and fill out activeFields to indicate they are active)
            string pixelGraphInputStructName = "SurfaceDescriptionInputs";
            string pixelGraphOutputStructName = "SurfaceDescription";
            string pixelGraphEvalFunctionName = "SurfaceDescriptionFunction";
            ShaderStringBuilder pixelGraphEvalFunction = new ShaderStringBuilder();
            ShaderStringBuilder pixelGraphOutputs = new ShaderStringBuilder();

            // build initial requirements
            HDRPShaderStructs.AddActiveFieldsFromPixelGraphRequirements(activeFields, pixelRequirements);

            // build the graph outputs structure, and populate activeFields with the fields of that structure
            GraphUtil.GenerateSurfaceDescriptionStruct(pixelGraphOutputs, pixelSlots, true, pixelGraphOutputStructName, activeFields);

            // Build the graph evaluation code, to evaluate the specified slots
            GraphUtil.GenerateSurfaceDescriptionFunction(
                pixelNodes,
                masterNode,
                masterNode.owner as AbstractMaterialGraph,
                pixelGraphEvalFunction,
                functionRegistry,
                sharedProperties,
                pixelRequirements,  // TODO : REMOVE UNUSED
                mode,
                pixelGraphEvalFunctionName,
                pixelGraphOutputStructName,
                null,
                pixelSlots,
                pixelGraphInputStructName);

            string vertexGraphInputStructName = "VertexDescriptionInputs";
            string vertexGraphOutputStructName = "VertexDescription";
            string vertexGraphEvalFunctionName = "VertexDescriptionFunction";
            ShaderStringBuilder vertexGraphEvalFunction = new ShaderStringBuilder();
            ShaderStringBuilder vertexGraphOutputs = new ShaderStringBuilder();

            // check for vertex animation -- enables HAVE_VERTEX_MODIFICATION
            if (vertexActive)
            {
                vertexActive = true;
                activeFields.Add("features.modifyMesh");
                HDRPShaderStructs.AddActiveFieldsFromVertexGraphRequirements(activeFields, vertexRequirements);

                // -------------------------------------
                // Generate Output structure for Vertex Description function
                GraphUtil.GenerateVertexDescriptionStruct(vertexGraphOutputs, vertexSlots, vertexGraphOutputStructName, activeFields);

                // -------------------------------------
                // Generate Vertex Description function
                GraphUtil.GenerateVertexDescriptionFunction(
                    masterNode.owner as AbstractMaterialGraph,
                    vertexGraphEvalFunction,
                    functionRegistry,
                    sharedProperties,
                    mode,
                    vertexNodes,
                    vertexSlots,
                    vertexGraphInputStructName,
                    vertexGraphEvalFunctionName,
                    vertexGraphOutputStructName);
            }

            var blendCode = new ShaderStringBuilder();
            var cullCode = new ShaderStringBuilder();
            var zTestCode = new ShaderStringBuilder();
            var zWriteCode = new ShaderStringBuilder();
            var zClipCode = new ShaderStringBuilder();
            var stencilCode = new ShaderStringBuilder();
            var colorMaskCode = new ShaderStringBuilder();
            HDSubShaderUtilities.BuildRenderStatesFromPassAndMaterialOptions(pass, materialOptions, blendCode, cullCode, zTestCode, zWriteCode, zClipCode, stencilCode, colorMaskCode);

            HDRPShaderStructs.AddRequiredFields(pass.RequiredFields, activeFields);

            // propagate active field requirements using dependencies
            ShaderSpliceUtil.ApplyDependencies(
                activeFields,
                new List<Dependency[]>()
                {
                    HDRPShaderStructs.FragInputs.dependencies,
                    HDRPShaderStructs.VaryingsMeshToPS.standardDependencies,
                    HDRPShaderStructs.SurfaceDescriptionInputs.dependencies,
                    HDRPShaderStructs.VertexDescriptionInputs.dependencies
                });

            // debug output all active fields
            var interpolatorDefines = new ShaderGenerator();
            if (debugOutput)
            {
                interpolatorDefines.AddShaderChunk("// ACTIVE FIELDS:");
                foreach (string f in activeFields)
                {
                    interpolatorDefines.AddShaderChunk("//   " + f);
                }
            }

            // build graph inputs structures
            ShaderGenerator pixelGraphInputs = new ShaderGenerator();
            ShaderSpliceUtil.BuildType(typeof(HDRPShaderStructs.SurfaceDescriptionInputs), activeFields, pixelGraphInputs);
            ShaderGenerator vertexGraphInputs = new ShaderGenerator();
            ShaderSpliceUtil.BuildType(typeof(HDRPShaderStructs.VertexDescriptionInputs), activeFields, vertexGraphInputs);

            ShaderGenerator defines = new ShaderGenerator();
            {
                defines.AddShaderChunk(string.Format("#define SHADERPASS {0}", pass.ShaderPassName), true);
                if (pass.ExtraDefines != null)
                {
                    foreach (var define in pass.ExtraDefines)
                        defines.AddShaderChunk(define);
                }
                if (graphRequirements.requiresDepthTexture)
                    defines.AddShaderChunk("#define REQUIRE_DEPTH_TEXTURE");
                defines.AddGenerator(interpolatorDefines);
            }

            var shaderPassIncludes = new ShaderGenerator();
            if (pass.Includes != null)
            {
                foreach (var include in pass.Includes)
                    shaderPassIncludes.AddShaderChunk(include);
            }


            // build graph code
            var graph = new ShaderGenerator();
            {
                graph.AddShaderChunk("// Shared Graph Properties (uniform inputs)");
                graph.AddShaderChunk(sharedProperties.GetPropertiesDeclaration(1));

                if (vertexActive)
                {
                    graph.AddShaderChunk("// Vertex Graph Inputs");
                    graph.Indent();
                    graph.AddGenerator(vertexGraphInputs);
                    graph.Deindent();
                    graph.AddShaderChunk("// Vertex Graph Outputs");
                    graph.Indent();
                    graph.AddShaderChunk(vertexGraphOutputs.ToString());
                    graph.Deindent();
                }

                graph.AddShaderChunk("// Pixel Graph Inputs");
                graph.Indent();
                graph.AddGenerator(pixelGraphInputs);
                graph.Deindent();
                graph.AddShaderChunk("// Pixel Graph Outputs");
                graph.Indent();
                graph.AddShaderChunk(pixelGraphOutputs.ToString());
                graph.Deindent();

                graph.AddShaderChunk("// Shared Graph Node Functions");
                graph.AddShaderChunk(graphNodeFunctions.ToString());

                if (vertexActive)
                {
                    graph.AddShaderChunk("// Vertex Graph Evaluation");
                    graph.Indent();
                    graph.AddShaderChunk(vertexGraphEvalFunction.ToString());
                    graph.Deindent();
                }

                graph.AddShaderChunk("// Pixel Graph Evaluation");
                graph.Indent();
                graph.AddShaderChunk(pixelGraphEvalFunction.ToString());
                graph.Deindent();
            }

            // build the hash table of all named fragments      TODO: could make this Dictionary<string, ShaderGenerator / string>  ?
            Dictionary<string, string> namedFragments = new Dictionary<string, string>();
            namedFragments.Add("Defines", defines.GetShaderString(2, false));
            namedFragments.Add("Graph", graph.GetShaderString(2, false));
            namedFragments.Add("LightMode", pass.LightMode);
            namedFragments.Add("PassName", pass.Name);
            namedFragments.Add("Includes", shaderPassIncludes.GetShaderString(2, false));
            namedFragments.Add("Blending", blendCode.ToString());
            namedFragments.Add("Culling", cullCode.ToString());
            namedFragments.Add("ZTest", zTestCode.ToString());
            namedFragments.Add("ZWrite", zWriteCode.ToString());
            namedFragments.Add("ZClip", zClipCode.ToString());
            namedFragments.Add("Stencil", stencilCode.ToString());
            namedFragments.Add("ColorMask", colorMaskCode.ToString());
            namedFragments.Add("LOD", materialOptions.lod.ToString());

            // this is the format string for building the 'C# qualified assembly type names' for $buildType() commands
            string buildTypeAssemblyNameFormat = "UnityEditor.Experimental.Rendering.HDPipeline.HDRPShaderStructs+{0}, " + typeof(HDSubShaderUtilities).Assembly.FullName.ToString();

            string sharedTemplatePath = Path.Combine(Path.Combine(HDUtils.GetHDRenderPipelinePath(), "Editor"), "ShaderGraph");
            // process the template to generate the shader code for this pass
            ShaderSpliceUtil.TemplatePreprocessor templatePreprocessor =
                new ShaderSpliceUtil.TemplatePreprocessor(activeFields, namedFragments, debugOutput, sharedTemplatePath, sourceAssetDependencyPaths, buildTypeAssemblyNameFormat);

            templatePreprocessor.ProcessTemplateFile(templateLocation);

            result.AddShaderChunk(templatePreprocessor.GetShaderCode().ToString(), false);

            return true;
        }

        public static List<MaterialSlot> FindMaterialSlotsOnNode(IEnumerable<int> slots, AbstractMaterialNode node)
        {
            var activeSlots = new List<MaterialSlot>();
            if (slots != null)
            {
                foreach (var id in slots)
                {
                    MaterialSlot slot = node.FindSlot<MaterialSlot>(id);
                    if (slot != null)
                    {
                        activeSlots.Add(slot);
                    }
                }
            }
            return activeSlots;
        }

        public static void BuildRenderStatesFromPassAndMaterialOptions(
            Pass pass,
            SurfaceMaterialOptions materialOptions,
            ShaderStringBuilder blendCode,
            ShaderStringBuilder cullCode,
            ShaderStringBuilder zTestCode,
            ShaderStringBuilder zWriteCode,
            ShaderStringBuilder zClipCode,
            ShaderStringBuilder stencilCode,
            ShaderStringBuilder colorMaskCode)
        {
            if (pass.BlendOverride != null)
            {
                blendCode.AppendLine(pass.BlendOverride);
            }
            else
            {
                materialOptions.GetBlend(blendCode);
            }

            if (pass.BlendOpOverride != null)
            {
                blendCode.AppendLine(pass.BlendOpOverride);
            }

            if (pass.CullOverride != null)
            {
                cullCode.AppendLine(pass.CullOverride);
            }
            else
            {
                materialOptions.GetCull(cullCode);
            }

            if (pass.ZTestOverride != null)
            {
                zTestCode.AppendLine(pass.ZTestOverride);
            }
            else
            {
                materialOptions.GetDepthTest(zTestCode);
            }

            if (pass.ZWriteOverride != null)
            {
                zWriteCode.AppendLine(pass.ZWriteOverride);
            }
            else
            {
                materialOptions.GetDepthWrite(zWriteCode);
            }

            // No point in an override for this.
            materialOptions.GetDepthClip(zClipCode);

            if (pass.ColorMaskOverride != null)
            {
                colorMaskCode.AppendLine(pass.ColorMaskOverride);
            }
            else
            {
                // material option default is to not declare anything for color mask
            }

            if (pass.StencilOverride != null)
            {
                foreach (var str in pass.StencilOverride)
                {
                    stencilCode.AppendLine(str);
                }
            }
            else
            {
                stencilCode.AppendLine("// Default Stencil");
            }
        }

        public static SurfaceMaterialTags BuildMaterialTags(SurfaceType surfaceType, bool alphaTest, bool preRefraction, int sortPriority)
        {
            SurfaceMaterialTags materialTags = new SurfaceMaterialTags();

            if (surfaceType == SurfaceType.Opaque)
            {
                if (alphaTest)
                {
                    materialTags.renderQueue = SurfaceMaterialTags.RenderQueue.AlphaTest;
                    materialTags.renderType = SurfaceMaterialTags.RenderType.TransparentCutout;
                }
                else
                {
                    materialTags.renderQueue = SurfaceMaterialTags.RenderQueue.Geometry;
                    materialTags.renderType = SurfaceMaterialTags.RenderType.Opaque;
                }
            }
            else
            {
                materialTags.renderQueue = SurfaceMaterialTags.RenderQueue.Transparent;
                materialTags.renderQueueOffset = sortPriority;
                if (preRefraction)
                {
                    materialTags.renderQueueOffset -= HDRenderQueue.Priority.Transparent - HDRenderQueue.Priority.PreRefraction;
                }
                materialTags.renderType = SurfaceMaterialTags.RenderType.Transparent;
            }

            return materialTags;
        }

        public static SurfaceMaterialOptions BuildMaterialOptions(SurfaceType surfaceType,
                                                                  AlphaMode alphaMode,
                                                                  bool twoSided,
                                                                  bool refraction)
        {
            SurfaceMaterialOptions materialOptions = new SurfaceMaterialOptions();
            if (surfaceType == SurfaceType.Opaque)
            {
                materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.Zero;
                materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.On;
            }
            else
            {
                if (refraction)
                {
                    materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                    materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                }
                else
                {
                    switch (alphaMode)
                    {
                        case AlphaMode.Alpha:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                            break;
                        case AlphaMode.Additive:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.One;
                            break;
                        case AlphaMode.Premultiply:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                            break;
                        // This isn't supported in HDRP.
                        case AlphaMode.Multiply:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                            break;
                    }
                }
                materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
            }

            materialOptions.cullMode = twoSided ? SurfaceMaterialOptions.CullMode.Off : SurfaceMaterialOptions.CullMode.Back;

            return materialOptions;
        }
    }
}
