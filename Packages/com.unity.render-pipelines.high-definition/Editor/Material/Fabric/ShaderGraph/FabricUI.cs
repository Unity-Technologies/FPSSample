using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.ShaderGraph
{
    public class FabricGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            materialEditor.PropertiesDefaultGUI(props);
            if (materialEditor.EmissionEnabledProperty())
            {
                // Use the overload version of this function once the following PR is merged: Pull request #74105
                materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
                //materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true, true);
            }

            // Make sure all selected materials are initialized.
            string materialTag = "MotionVector";
            foreach (var obj in materialEditor.targets)
            {
                var material = (Material)obj;
                string tag = material.GetTag(materialTag, false, "Nothing");
                if (tag == "Nothing")
                {
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, false);
                    material.SetOverrideTag(materialTag, "User");
                }
            }

            {
                // If using multi-select, apply toggled material to all materials.
                bool enabled = ((Material)materialEditor.target).GetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr);
                EditorGUI.BeginChangeCheck();
                enabled = EditorGUILayout.Toggle("Enable Motion Vector For Vertex Animation", enabled);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var obj in materialEditor.targets)
                    {
                        var material = (Material)obj;
                        material.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, enabled);
                    }
                }
            }
        }
    }
}
