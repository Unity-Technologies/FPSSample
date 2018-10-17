using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public class PBRMasterGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
			materialEditor.PropertiesDefaultGUI(props);

            foreach (MaterialProperty prop in props)
            {
                if (prop.name == "_EmissionColor")
                {
                    if (materialEditor.EmissionEnabledProperty())
                    {
                        materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
                    }
                    return;
                }
            }
        }
    }
}
