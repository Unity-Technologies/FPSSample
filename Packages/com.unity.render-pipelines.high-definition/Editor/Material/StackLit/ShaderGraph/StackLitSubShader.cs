using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class StackLitSubShader : IStackLitSubShader
    {
        Pass m_PassMETA = new Pass()
        {
            Name = "META",
            LightMode = "Meta",
            TemplateName = "StackLitPass.template",
            MaterialName = "StackLit",
            ShaderPassName = "SHADERPASS_LIGHT_TRANSPORT",
            CullOverride = "Cull Off",
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
            },
            PixelShaderSlots = new List<int>()
            {
                StackLitMasterNode.BaseColorSlotId,
                StackLitMasterNode.NormalSlotId,
                StackLitMasterNode.BentNormalSlotId,
                StackLitMasterNode.TangentSlotId,
                StackLitMasterNode.SubsurfaceMaskSlotId,
                StackLitMasterNode.ThicknessSlotId,
                StackLitMasterNode.DiffusionProfileSlotId,
                StackLitMasterNode.IridescenceMaskSlotId,
                StackLitMasterNode.IridescenceThicknessSlotId,
                StackLitMasterNode.SpecularColorSlotId,
                StackLitMasterNode.DielectricIorSlotId,
                StackLitMasterNode.MetallicSlotId,
                StackLitMasterNode.EmissionSlotId,
                StackLitMasterNode.SmoothnessASlotId,
                StackLitMasterNode.SmoothnessBSlotId,
                StackLitMasterNode.AmbientOcclusionSlotId,
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId,
                StackLitMasterNode.AnisotropyASlotId,
                StackLitMasterNode.AnisotropyBSlotId,
                StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                StackLitMasterNode.SpecularAAThresholdSlotId,
                StackLitMasterNode.CoatSmoothnessSlotId,
                StackLitMasterNode.CoatIorSlotId,
                StackLitMasterNode.CoatThicknessSlotId,
                StackLitMasterNode.CoatExtinctionSlotId,
                StackLitMasterNode.CoatNormalSlotId,
                StackLitMasterNode.LobeMixSlotId,
                StackLitMasterNode.HazinessSlotId,
                StackLitMasterNode.HazeExtentSlotId,
                StackLitMasterNode.HazyGlossMaxDielectricF0SlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                //StackLitMasterNode.PositionSlotId
            },
            UseInPreview = false
        };

        Pass m_PassShadowCaster = new Pass()
        {
            Name = "ShadowCaster",
            LightMode = "ShadowCaster",
            TemplateName = "StackLitPass.template",
            MaterialName = "StackLit",
            ShaderPassName = "SHADERPASS_SHADOWS",
            BlendOverride = "Blend One Zero",
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",
            ExtraDefines = new List<string>()
            {
                "#define USE_LEGACY_UNITY_MATRIX_VARIABLES",
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                StackLitMasterNode.PositionSlotId
            },
            UseInPreview = false
        };

        Pass m_SceneSelectionPass = new Pass()
        {
            Name = "SceneSelectionPass",
            LightMode = "SceneSelectionPass",
            TemplateName = "StackLitPass.template",
            MaterialName = "StackLit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ColorMaskOverride = "ColorMask 0",
            ExtraDefines = new List<string>()
            {
                "#define SCENESELECTIONPASS",
            },            
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                StackLitMasterNode.PositionSlotId
            },
            UseInPreview = true
        };

        Pass m_PassDepthForwardOnly = new Pass()
        {
            Name = "DepthOnly",
            LightMode = "DepthForwardOnly",
            TemplateName = "StackLitPass.template",
            MaterialName = "StackLit",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ZWriteOverride = "ZWrite On",

            ExtraDefines = new List<string>()
            {
                "#define WRITE_NORMAL_BUFFER",
                "#pragma multi_compile _ WRITE_MSAA_DEPTH"
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            // Code path for WRITE_NORMAL_BUFFER
            //
            // See StackLit.hlsl:ConvertSurfaceDataToNormalData()
            // which ShaderPassDepthOnly uses: we need to add proper interpolators dependencies depending on WRITE_NORMAL_BUFFER.
            // In our case WRITE_NORMAL_BUFFER is always enabled here.
            // Also, we need to add PixelShaderSlots dependencies for everything potentially used there.
            // See AddPixelShaderSlotsForWriteNormalBufferPasses()
            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                "AttributesMesh.uv3",           // DEBUG_DISPLAY

                "FragInputs.worldToTangent",
                "FragInputs.positionRWS",
                "FragInputs.texCoord0",
                "FragInputs.texCoord1",
                "FragInputs.texCoord2",
                "FragInputs.texCoord3",
                "FragInputs.color",
            },
            PixelShaderSlots = new List<int>()
            {
                // see AddPixelShaderSlotsForWriteNormalBufferPasses
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                StackLitMasterNode.PositionSlotId
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as StackLitMasterNode;

                int stencilDepthPrepassWriteMask = masterNode.receiveDecals.isOn ? (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer : 0;
                int stencilDepthPrepassRef = masterNode.receiveDecals.isOn ? (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer : 0;
                stencilDepthPrepassWriteMask |= !masterNode.receiveSSR.isOn ? (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR : 0;
                stencilDepthPrepassRef |= !masterNode.receiveSSR.isOn ? (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR : 0;

                if (stencilDepthPrepassWriteMask != 0)
                {
                    pass.StencilOverride = new List<string>()
                    {
                        "// Stencil setup",
                        "Stencil",
                        "{",
                        string.Format("   WriteMask {0}", stencilDepthPrepassWriteMask),
                        string.Format("   Ref  {0}", stencilDepthPrepassRef),
                        "   Comp Always",
                        "   Pass Replace",
                        "}"
                    };
                }
                // See comments in:
                AddPixelShaderSlotsForWriteNormalBufferPasses(masterNode, ref pass);
            }
        };

        Pass m_PassMotionVectors = new Pass()
        {
            Name = "Motion Vectors",
            LightMode = "MotionVectors",
            TemplateName = "StackLitPass.template",
            MaterialName = "StackLit",
            ShaderPassName = "SHADERPASS_VELOCITY",
            ExtraDefines = new List<string>()
            {
                "#define WRITE_NORMAL_BUFFER",
                "#pragma multi_compile _ WRITE_MSAA_DEPTH"
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassVelocity.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                "AttributesMesh.uv3",           // DEBUG_DISPLAY

                "FragInputs.worldToTangent",
                "FragInputs.positionRWS",
                "FragInputs.texCoord0",
                "FragInputs.texCoord1",
                "FragInputs.texCoord2",
                "FragInputs.texCoord3",
                "FragInputs.color",
            },

            PixelShaderSlots = new List<int>()
            {
                // see AddPixelShaderSlotsForWriteNormalBufferPasses
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                StackLitMasterNode.PositionSlotId
            },
            UseInPreview = false,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as StackLitMasterNode;

                int stencilWriteMaskMV = (int)HDRenderPipeline.StencilBitMask.ObjectVelocity;
                int stencilRefMV = (int)HDRenderPipeline.StencilBitMask.ObjectVelocity;

                pass.StencilOverride = new List<string>()
                {
                    "// If velocity pass (motion vectors) is enabled we tag the stencil so it don't perform CameraMotionVelocity",
                    "Stencil",
                    "{",
                    string.Format("   WriteMask {0}", stencilWriteMaskMV),
                    string.Format("   Ref  {0}", stencilRefMV),
                    "   Comp Always",
                    "   Pass Replace",
                    "}"
                };

                // For WRITE_NORMAL_BUFFER, see comments for pass m_PassDepthForwardOnly and in:
                AddPixelShaderSlotsForWriteNormalBufferPasses(masterNode, ref pass);
            }
        };

        Pass m_PassDistortion = new Pass()
        {
            Name = "Distortion",
            LightMode = "DistortionVectors",
            TemplateName = "StackLitPass.template",
            MaterialName = "StackLit",
            ShaderPassName = "SHADERPASS_DISTORTION",
            ZWriteOverride = "ZWrite Off",
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId,
                StackLitMasterNode.DistortionSlotId,
                StackLitMasterNode.DistortionBlurSlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                StackLitMasterNode.PositionSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as StackLitMasterNode;
                if (masterNode.distortionDepthTest.isOn)
                {
                    pass.ZTestOverride = "ZTest LEqual";
                }
                else
                {
                    pass.ZTestOverride = "ZTest Always";
                }
                if (masterNode.distortionMode == DistortionMode.Add)
                {
                    pass.BlendOverride = "Blend One One, One One";
                    pass.BlendOpOverride = "BlendOp Add, Add";
                }
                else // if (masterNode.distortionMode == DistortionMode.Multiply)
                {
                    pass.BlendOverride = "Blend DstColor Zero, DstAlpha Zero";
                    pass.BlendOpOverride = "BlendOp Add, Add";
                }
            }
        };

        Pass m_PassForwardOnly = new Pass()
        {
            Name = "Forward",
            LightMode = "ForwardOnly",
            TemplateName = "StackLitPass.template",
            MaterialName = "StackLit",
            ShaderPassName = "SHADERPASS_FORWARD",
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ DEBUG_DISPLAY",
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#pragma multi_compile _ SHADOWS_SHADOWMASK",
                "#pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT",
                "#define LIGHTLOOP_TILE_PASS",
                "#pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST",
                "#pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH"
            },
            Includes = new List<string>()
            {
                "#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of Variants lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
                "AttributesMesh.uv3",           // DEBUG_DISPLAY

                "FragInputs.worldToTangent",
                "FragInputs.positionRWS",
                "FragInputs.texCoord0",
                "FragInputs.texCoord1",
                "FragInputs.texCoord2",
                "FragInputs.texCoord3",
                "FragInputs.color",
            },
            PixelShaderSlots = new List<int>()
            {
                StackLitMasterNode.BaseColorSlotId,
                StackLitMasterNode.NormalSlotId,
                StackLitMasterNode.BentNormalSlotId,
                StackLitMasterNode.TangentSlotId,
                StackLitMasterNode.SubsurfaceMaskSlotId,
                StackLitMasterNode.ThicknessSlotId,
                StackLitMasterNode.DiffusionProfileSlotId,
                StackLitMasterNode.IridescenceMaskSlotId,
                StackLitMasterNode.IridescenceThicknessSlotId,
                StackLitMasterNode.SpecularColorSlotId,
                StackLitMasterNode.DielectricIorSlotId,
                StackLitMasterNode.MetallicSlotId,
                StackLitMasterNode.EmissionSlotId,
                StackLitMasterNode.SmoothnessASlotId,
                StackLitMasterNode.SmoothnessBSlotId,
                StackLitMasterNode.AmbientOcclusionSlotId,
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId,
                StackLitMasterNode.AnisotropyASlotId,
                StackLitMasterNode.AnisotropyBSlotId,
                StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                StackLitMasterNode.SpecularAAThresholdSlotId,
                StackLitMasterNode.CoatSmoothnessSlotId,
                StackLitMasterNode.CoatIorSlotId,
                StackLitMasterNode.CoatThicknessSlotId,
                StackLitMasterNode.CoatExtinctionSlotId,
                StackLitMasterNode.CoatNormalSlotId,
                StackLitMasterNode.LobeMixSlotId,
                StackLitMasterNode.HazinessSlotId,
                StackLitMasterNode.HazeExtentSlotId,
                StackLitMasterNode.HazyGlossMaxDielectricF0SlotId,
            },
            VertexShaderSlots = new List<int>()
            {
                StackLitMasterNode.PositionSlotId
            },
            UseInPreview = true,

            OnGeneratePassImpl = (IMasterNode node, ref Pass pass) =>
            {
                var masterNode = node as StackLitMasterNode;
                pass.StencilOverride = new List<string>()
                {
                    "// Stencil setup",
                    "Stencil",
                    "{",
                    string.Format("   WriteMask {0}", (int) HDRenderPipeline.StencilBitMask.LightingMask),
                    string.Format("   Ref  {0}", masterNode.RequiresSplitLighting() ? (int)StencilLightingUsage.SplitLighting : (int)StencilLightingUsage.RegularLighting),
                    "   Comp Always",
                    "   Pass Replace",
                    "}"
                };

                pass.ExtraDefines.Remove("#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST");
                if (masterNode.surfaceType == SurfaceType.Opaque && masterNode.alphaTest.isOn)
                {
                    pass.ExtraDefines.Add("#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST");
                    pass.ZTestOverride = "ZTest Equal";
                }
                else
                {
                    pass.ZTestOverride = null;
                }
            }
        };

        private static void AddPixelShaderSlotsForWriteNormalBufferPasses(StackLitMasterNode masterNode, ref Pass pass)
        {
            // See StackLit.hlsl:ConvertSurfaceDataToNormalData()
            // Note: We remove the slots we're adding as the editor will constantly regenerate the shader but
            // without recreating the nodes thus the passes in the subshader object.
            if (masterNode.coat.isOn)
            {
                // Check ConvertSurfaceDataToNormalData, in this case, we only need those:
                pass.PixelShaderSlots.Remove(StackLitMasterNode.CoatSmoothnessSlotId);
                pass.PixelShaderSlots.Add(StackLitMasterNode.CoatSmoothnessSlotId);
                pass.PixelShaderSlots.Remove(StackLitMasterNode.CoatNormalSlotId);
                pass.PixelShaderSlots.Add(StackLitMasterNode.CoatNormalSlotId);
            }
            else
            {
                pass.PixelShaderSlots.Remove(StackLitMasterNode.NormalSlotId);
                pass.PixelShaderSlots.Add(StackLitMasterNode.NormalSlotId);
                pass.PixelShaderSlots.Remove(StackLitMasterNode.LobeMixSlotId);
                pass.PixelShaderSlots.Add(StackLitMasterNode.LobeMixSlotId);
                pass.PixelShaderSlots.Remove(StackLitMasterNode.SmoothnessASlotId);
                pass.PixelShaderSlots.Add(StackLitMasterNode.SmoothnessASlotId);
                pass.PixelShaderSlots.Remove(StackLitMasterNode.SmoothnessBSlotId);
                pass.PixelShaderSlots.Add(StackLitMasterNode.SmoothnessBSlotId);
            }
            // Also, when geometricSpecularAA.isOn, might want to add SpecularAAScreenSpaceVarianceSlotId and SpecularAAThresholdSlotId,
            // since they affect smoothnesses in surfaceData directly. This is an implicit behavior for Lit, via the GBuffer pass.
            // Versus performance, might not be important for what it is used for, but SSR uses the roughness too so for now,
            // add them.
            if (masterNode.geometricSpecularAA.isOn) // TODOTODO || Normal Map Filtering is on
            {
                pass.PixelShaderSlots.Remove(StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId);
                pass.PixelShaderSlots.Add(StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId);
                pass.PixelShaderSlots.Remove(StackLitMasterNode.SpecularAAThresholdSlotId);
                pass.PixelShaderSlots.Add(StackLitMasterNode.SpecularAAThresholdSlotId);
            }
        }

        //
        // Reference for GetActiveFieldsFromMasterNode
        // -------------------------------------------
        //
        // Properties (enables etc):
        //
        //  ok+MFD -> material feature define: means we need a predicate, because we will transform it into a #define that match the material feature, shader_feature-defined, that the rest of the shader code uses.
        //
        //  ok+MFD masterNode.baseParametrization    --> even though we can just always transfer present fields (check with $SurfaceDescription.*) like specularcolor and metallic,
        //                                               we need to translate this into the _MATERIAL_FEATURE_SPECULAR_COLOR define.
        //
        //  ok masterNode.energyConservingSpecular
        //
        //  ~~~~ ok+MFD: these are almost all material features:
        //  masterNode.anisotropy
        //  masterNode.coat
        //  masterNode.coatNormal
        //  masterNode.dualSpecularLobe
        //  masterNode.dualSpecularLobeParametrization
        //  masterNode.capHazinessWrtMetallic           -> not a material feature define, as such, we will create a combined predicate for the HazyGlossMaxDielectricF0 slot dependency
        //                                                 instead of adding a #define in the template...
        //  masterNode.iridescence
        //  masterNode.subsurfaceScattering
        //  masterNode.transmission
        //
        //  ~~~~ ...ok+MFD: these are all material features
        //
        //  ok masterNode.receiveDecals
        //  ok masterNode.receiveSSR
        //  ok masterNode.geometricSpecularAA    --> check, a way to combine predicates and/or exclude passes: TODOTODO What about WRITE_NORMAL_BUFFER passes ? (ie smoothness)
        //  ok masterNode.specularOcclusion      --> no use for it though! see comments.
        //
        //  ~~~~ ok+D: these require translation to defines also...
        //
        //  masterNode.anisotropyForAreaLights
        //  masterNode.recomputeStackPerLight
        //  masterNode.shadeBaseUsingRefractedAngles
        //  masterNode.debug

        // Inputs: Most inputs don't need a specific predicate in addition to the "present field predicate", ie the $SurfaceDescription.*,
        //         but in some special cases we check connectivity to avoid processing the default value for nothing...
        //         (see specular occlusion with _MASKMAP and _BENTNORMALMAP in LitData, or _TANGENTMAP, _BENTNORMALMAP, etc. which act a bit like that
        //         although they also avoid sampling in that case, but default tiny texture map sampling isn't a big hit since they are all cached once
        //         a default "unityTexWhite" is sampled, it is cached for everyone defaulting to white...)
        //
        // ok+ means there's a specific additional predicate 
        //
        // ok masterNode.BaseColorSlotId
        // ok masterNode.NormalSlotId
        //
        // ok+ masterNode.BentNormalSlotId     --> Dependency of the predicate on IsSlotConnected avoids processing even if the slots
        // ok+ masterNode.TangentSlotId            are always there so any pass that declares its use in PixelShaderSlots will have the field in SurfaceDescription, 
        //                                         but it's not necessarily useful (if slot isnt connected, waste processing on potentially static expressions if
        //                                         shader compiler cant optimize...and even then, useless to have static override value for those.)
        //
        //                                         TODOTODO: Note you could have the same argument for NormalSlot (which we dont exclude with a predicate).
        //                                         Also and anyways, the compiler is smart enough not to do the TS to WS matrix multiply on a (0,0,1) vector.
        //
        // ok+ masterNode.CoatNormalSlotId       -> we already have a "material feature" coat normal map so can use that instead, although using that former, we assume the coat normal slot
        //                                         will be there, but it's ok, we can #ifdef the code on the material feature define, and use the $SurfaceDescription.CoatNormal predicate 
        //                                         for the actual assignment,
        //                                         although for that one we could again 
        //                                         use the "connected" condition like for tangent and bentnormal
        //
        // The following are all ok, no need beyond present field predicate, ie $SurfaceDescription.*,
        // except special cases where noted
        // 
        // ok masterNode.SubsurfaceMaskSlotId
        // ok masterNode.ThicknessSlotId
        // ok masterNode.DiffusionProfileSlotId
        // ok masterNode.IridescenceMaskSlotId
        // ok masterNode.IridescenceThicknessSlotId
        // ok masterNode.SpecularColorSlotId
        // ok masterNode.DielectricIorSlotId
        // ok masterNode.MetallicSlotId
        // ok masterNode.EmissionSlotId
        // ok masterNode.SmoothnessASlotId
        // ok masterNode.SmoothnessBSlotId
        // ok+ masterNode.AmbientOcclusionSlotId    -> defined a specific predicate, but not used, see StackLitData.
        // ok masterNode.AlphaSlotId
        // ok masterNode.AlphaClipThresholdSlotId
        // ok masterNode.AnisotropyASlotId
        // ok masterNode.AnisotropyBSlotId
        // ok masterNode.SpecularAAScreenSpaceVarianceSlotId
        // ok masterNode.SpecularAAThresholdSlotId
        // ok masterNode.CoatSmoothnessSlotId
        // ok masterNode.CoatIorSlotId
        // ok masterNode.CoatThicknessSlotId
        // ok masterNode.CoatExtinctionSlotId
        // ok masterNode.LobeMixSlotId
        // ok masterNode.HazinessSlotId
        // ok masterNode.HazeExtentSlotId
        // ok masterNode.HazyGlossMaxDielectricF0SlotId     -> No need for a predicate, the needed predicate is the combined (capHazinessWrtMetallic + HazyGlossMaxDielectricF0)
        //                                                     "leaking case": if the 2 are true, but we're not in metallic mode, the capHazinessWrtMetallic property is wrong,
        //                                                     that means the master node is really misconfigured, spew an error, should never happen...
        //                                                     If it happens, it's because we forgot UpdateNodeAfterDeserialization() call when modifying the capHazinessWrtMetallic or baseParametrization
        //                                                     properties, maybe through debug etc.
        //
        // ok masterNode.DistortionSlotId            -> Warning: peculiarly, instead of using $SurfaceDescription.Distortion and DistortionBlur,
        // ok masterNode.DistortionBlurSlotId           we do an #if (SHADERPASS == SHADERPASS_DISTORTION) in the template, instead of 
        //                                              relying on other passed NOT to include the DistortionSlotId in their PixelShaderSlots!!

        // Other to deal with, and 
        // Common between Lit and StackLit: 
        //
        // doubleSidedMode, alphaTest, receiveDecals,
        // surfaceType, alphaMode, blendPreserveSpecular, transparencyFog,
        // distortion, distortionMode, distortionDepthTest,
        // sortPriority (int)
        // geometricSpecularAA, energyConservingSpecular, specularOcclusion
        //

        private static HashSet<string> GetActiveFieldsFromMasterNode(INode iMasterNode, Pass pass)
        {
            HashSet<string> activeFields = new HashSet<string>();

            StackLitMasterNode masterNode = iMasterNode as StackLitMasterNode;
            if (masterNode == null)
            {
                return activeFields;
            }

            if (masterNode.doubleSidedMode != DoubleSidedMode.Disabled)
            {
                activeFields.Add("DoubleSided");
                if (pass.ShaderPassName != "SHADERPASS_VELOCITY")   // HACK to get around lack of a good interpolator dependency system
                {                                                   // we need to be able to build interpolators using multiple input structs
                                                                    // also: should only require isFrontFace if Normals are required...
                    if (masterNode.doubleSidedMode == DoubleSidedMode.FlippedNormals)
                    {
                        activeFields.Add("DoubleSided.Flip");
                    }
                    else if (masterNode.doubleSidedMode == DoubleSidedMode.MirroredNormals)
                    {
                        activeFields.Add("DoubleSided.Mirror");
                    }
                    // Important: the following is used in SharedCode.template.hlsl for determining the normal flip mode
                    activeFields.Add("FragInputs.isFrontFace");
                }
            }

            if (masterNode.alphaTest.isOn)
            {
                if (pass.PixelShaderUsesSlot(StackLitMasterNode.AlphaClipThresholdSlotId))
                { 
                    activeFields.Add("AlphaTest");
                }
            }

            if (masterNode.surfaceType != SurfaceType.Opaque)
            {
                activeFields.Add("SurfaceType.Transparent");

                if (masterNode.alphaMode == AlphaMode.Alpha)
                {
                    activeFields.Add("BlendMode.Alpha");
                }
                else if (masterNode.alphaMode == AlphaMode.Premultiply)
                {
                    activeFields.Add("BlendMode.Premultiply");
                }
                else if (masterNode.alphaMode == AlphaMode.Additive)
                {
                    activeFields.Add("BlendMode.Add");
                }

                if (masterNode.blendPreserveSpecular.isOn)
                {
                    activeFields.Add("BlendMode.PreserveSpecular");
                }

                if (masterNode.transparencyFog.isOn)
                {
                    activeFields.Add("AlphaFog");
                }
            }

            //
            // Predicates to change into defines:
            //

            // Even though we can just always transfer the present (check with $SurfaceDescription.*) fields like specularcolor
            // and metallic, we still need to know the baseParametrization in the template to translate into the
            // _MATERIAL_FEATURE_SPECULAR_COLOR define:
            if (masterNode.baseParametrization == StackLit.BaseParametrization.SpecularColor)
            {
                activeFields.Add("BaseParametrization.SpecularColor");
            }
            if (masterNode.energyConservingSpecular.isOn) // No defines, suboption of BaseParametrization.SpecularColor
            {
                activeFields.Add("EnergyConservingSpecular");
            }
            if (masterNode.anisotropy.isOn)
            {
                activeFields.Add("Material.Anisotropy");
            }
            if (masterNode.coat.isOn)
            {
                activeFields.Add("Material.Coat");
            }
            if (masterNode.coatNormal.isOn)
            {
                activeFields.Add("Material.CoatNormal");
            }
            if (masterNode.dualSpecularLobe.isOn)
            {
                activeFields.Add("Material.DualSpecularLobe");
                if (masterNode.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss)
                {
                    activeFields.Add("DualSpecularLobeParametrization.HazyGloss");
                    // Option for baseParametrization == Metallic && DualSpecularLobeParametrization == HazyGloss:
                    if (masterNode.capHazinessWrtMetallic.isOn)
                    {
                        // assert metallic, but masternode should deal with having a consistent
                        // property config with UpdateNodeAfterDeserialization() etc.
                        if (pass.PixelShaderUsesSlot(StackLitMasterNode.HazyGlossMaxDielectricF0SlotId))
                        {
                            // Again we assume masternode has HazyGlossMaxDielectricF0 which should always be the case
                            // if capHazinessWrtMetallic.isOn.
                            activeFields.Add("CapHazinessIfNotMetallic");
                        }
                    }
                }
            }
            if (masterNode.iridescence.isOn)
            {
                activeFields.Add("Material.Iridescence");
            }
            if (masterNode.subsurfaceScattering.isOn && masterNode.surfaceType != SurfaceType.Transparent)
            {
                activeFields.Add("Material.SubsurfaceScattering");
            }
            if (masterNode.transmission.isOn)
            {
                activeFields.Add("Material.Transmission");
            }

            // Advanced:
            if (masterNode.anisotropyForAreaLights.isOn)
            {
                activeFields.Add("AnisotropyForAreaLights");
            }
            if (masterNode.recomputeStackPerLight.isOn)
            {
                activeFields.Add("RecomputeStackPerLight");
            }
            if (masterNode.shadeBaseUsingRefractedAngles.isOn)
            {
                activeFields.Add("ShadeBaseUsingRefractedAngles");
            }
            if (masterNode.debug.isOn)
            {
                activeFields.Add("StackLitDebug");
            }

            //
            // Other property predicates:
            //

            if (!masterNode.receiveDecals.isOn)
            {
                activeFields.Add("DisableDecals");
            }

            if (!masterNode.receiveSSR.isOn)
            {
                activeFields.Add("DisableSSR");
            }

            // Note here we combine an "enable"-like predicate and the $SurfaceDescription.(slotname) predicate
            // into a single $GeometricSpecularAA pedicate.
            //
            // ($SurfaceDescription.* predicates are useful to make sure the field is present in the struct in the template.
            // The field will be present if both the master node and pass have the slotid, see this set intersection we make
            // in GenerateSurfaceDescriptionStruct(), with HDSubShaderUtilities.FindMaterialSlotsOnNode().)
            //
            // Normally, since the feature enable adds the required slots, only the $SurfaceDescription.* would be required,
            // but some passes might not need it and not declare the PixelShaderSlot, or, inversely, the pass might not
            // declare it as a way to avoid it.
            //
            // IE this has also the side effect to disable geometricSpecularAA - even if "on" - for passes that don't explicitly
            // advertise these slots(eg for a general feature, with separate "enable" and "field present" predicates, the
            // template could take a default value and process it anyway if a feature is "on").
            //
            // (Note we can achieve the same results in the template on just single predicates by making defines out of them,
            // and using #if defined() && etc)
            bool haveSomeSpecularAA = false; // TODOTODO in prevision of normal texture filtering
            if (masterNode.geometricSpecularAA.isOn
                && pass.PixelShaderUsesSlot(StackLitMasterNode.SpecularAAThresholdSlotId)
                && pass.PixelShaderUsesSlot(StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId))
            {
                haveSomeSpecularAA = true;
                activeFields.Add("GeometricSpecularAA");
            }
            if (haveSomeSpecularAA)
            {
                activeFields.Add("SpecularAA");
            }

            if (masterNode.specularOcclusion.isOn)
            {
                // We don't optimize based eg on baked ambient occlusion missing a texture, this makes this baked, data-based
                // SpecularOcclusion a waste (non baked, SSAO-based SpecularOcclusion is always applied with the TriACE trick),
                // but the user should know better.
                // TODOTODO: rename the UI to "baked specular occlusion" and "baked AO" ?
                activeFields.Add("SpecularOcclusion");
            }

            //
            // Input special-casing predicates:
            //

            if (masterNode.IsSlotConnected(StackLitMasterNode.BentNormalSlotId) && pass.PixelShaderUsesSlot(StackLitMasterNode.BentNormalSlotId))
            {
                activeFields.Add("BentNormal");
            }

            if (masterNode.IsSlotConnected(StackLitMasterNode.TangentSlotId) && pass.PixelShaderUsesSlot(StackLitMasterNode.TangentSlotId))
            {
                activeFields.Add("Tangent");
            }

            // The following idiom enables an optimization on feature ports that don't have an enable switch in the settings
            // view, where the default value might not produce a visual result and incur a processing cost we want to avoid.
            // For ambient occlusion, this is the case for the SpecularOcclusion calculations which also depend on it,
            // where a value of 1 will produce no results.
            // See SpecularOcclusion, we don't optimize out this case...
            if (pass.PixelShaderUsesSlot(StackLitMasterNode.AmbientOcclusionSlotId))
            {
                bool connected = masterNode.IsSlotConnected(StackLitMasterNode.AmbientOcclusionSlotId);
                var ambientOcclusionSlot = masterNode.FindSlot<Vector1MaterialSlot>(StackLitMasterNode.AmbientOcclusionSlotId);
                // master node always has it, assert ambientOcclusionSlot != null
                if (connected || ambientOcclusionSlot.value != ambientOcclusionSlot.defaultValue)
                {
                    activeFields.Add("AmbientOcclusion");
                }
            }

            if (masterNode.IsSlotConnected(StackLitMasterNode.CoatNormalSlotId) && pass.PixelShaderUsesSlot(StackLitMasterNode.CoatNormalSlotId))
            {
                activeFields.Add("CoatNormal");
            }

            return activeFields;
        }

        private static bool GenerateShaderPassLit(StackLitMasterNode masterNode, Pass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            if (mode == GenerationMode.ForReals || pass.UseInPreview)
            {
                SurfaceMaterialOptions materialOptions = HDSubShaderUtilities.BuildMaterialOptions(masterNode.surfaceType, masterNode.alphaMode, masterNode.doubleSidedMode != DoubleSidedMode.Disabled, refraction: false);

                pass.OnGeneratePass(masterNode);

                // apply master node options to active fields
                HashSet<string> activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

                // use standard shader pass generation
                bool vertexActive = masterNode.IsSlotConnected(StackLitMasterNode.PositionSlotId);
                return HDSubShaderUtilities.GenerateShaderPass(masterNode, pass, mode, materialOptions, activeFields, result, sourceAssetDependencyPaths, vertexActive);
            }
            else
            {
                return false;
            }
        }

        public string GetSubshader(IMasterNode iMasterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // StackLitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("9649efe3e0e8e2941a983bb0f3a034ad"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = iMasterNode as StackLitMasterNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                SurfaceMaterialTags materialTags = HDSubShaderUtilities.BuildMaterialTags(masterNode.surfaceType, masterNode.alphaTest.isOn, preRefraction: false, sortPriority: masterNode.sortPriority);

                // Add tags at the SubShader level
                {
                    var tagsVisitor = new ShaderStringBuilder();
                    materialTags.GetTags(tagsVisitor);
                    subShader.AddShaderChunk(tagsVisitor.ToString(), false);
                }

                // generate the necessary shader passes
                bool opaque = (masterNode.surfaceType == SurfaceType.Opaque);
                bool transparent = !opaque;

                bool distortionActive = transparent && masterNode.distortion.isOn;

                GenerateShaderPassLit(masterNode, m_PassMETA, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_PassShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPassLit(masterNode, m_SceneSelectionPass, mode, subShader, sourceAssetDependencyPaths);

                if (opaque)
                {
                    GenerateShaderPassLit(masterNode, m_PassDepthForwardOnly, mode, subShader, sourceAssetDependencyPaths);
                }

                GenerateShaderPassLit(masterNode, m_PassMotionVectors, mode, subShader, sourceAssetDependencyPaths);

                if (distortionActive)
                {
                    GenerateShaderPassLit(masterNode, m_PassDistortion, mode, subShader, sourceAssetDependencyPaths);
                }

                GenerateShaderPassLit(masterNode, m_PassForwardOnly, mode, subShader, sourceAssetDependencyPaths);
            }

            subShader.Deindent();
            subShader.AddShaderChunk("}", true);
            subShader.AddShaderChunk(@"CustomEditor ""UnityEditor.ShaderGraph.StackLitGUI""");

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is HDRenderPipelineAsset;
        }
    }
}
