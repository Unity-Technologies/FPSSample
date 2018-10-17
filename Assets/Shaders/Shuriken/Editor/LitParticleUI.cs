using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class LitParticleGUI : LitGUI
    {
        public static GUIContent perPixelLightingText = new GUIContent("Enable Full Lighting", "Allow direct lighting or just probe lighting");

        protected MaterialProperty perPixelLighting = null;
        protected const string kPerPixelLighting = "_FullPerPixelLighting";

        protected override void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            base.FindBaseMaterialProperties(props);

            perPixelLighting = FindProperty(kPerPixelLighting, props);
        }

        protected override void BaseMaterialPropertiesGUI()
        {
            base.BaseMaterialPropertiesGUI();

            m_MaterialEditor.ShaderProperty(perPixelLighting, perPixelLightingText);
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            base.SetupMaterialKeywordsAndPassInternal(material);

            CoreUtils.SetKeyword(material, "LIT_PARTICLE_FULLLIGHTING", material.GetFloat(kPerPixelLighting) > 0.0);
        }

        protected override void VertexAnimationPropertiesGUI()
        {
        }
    }
}
