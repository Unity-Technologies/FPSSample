using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Gradient", "Gradient")]
    public class GradientNode : AbstractMaterialNode, IGeneratesFunction
    {
        [SerializeField]
        private float m_Value;

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public GradientNode()
        {
            name = "Gradient";
            UpdateNodeAfterDeserialization();
        }

        string GetFunctionName()
        {
            return string.Format("Unity_{0}", GetVariableNameForNode());
        }

        Gradient m_Gradient = new Gradient();

        [SerializeField]
        Vector4[] m_SerializableColorKeys = { new Vector4(1f, 1f, 1f, 0f), new Vector4(0f, 0f, 0f, 1f), };

        [SerializeField]
        Vector2[] m_SerializableAlphaKeys = { new Vector2(1f, 0f), new Vector2(1f, 1f) };

        [SerializeField]
        int m_SerializableMode = 0;

        [GradientControl("")]
        public Gradient gradient
        {
            get
            {
                return m_Gradient;
            }
            set
            {
                var scope = ModificationScope.Nothing;

                if (!GradientUtils.CheckEquivalency(m_Gradient, value))
                    scope = scope < ModificationScope.Graph ? ModificationScope.Graph : scope;

                if (scope > ModificationScope.Nothing)
                {
                    var newColorKeys = value.colorKeys;
                    var newAlphaKeys = value.alphaKeys;

                    m_Gradient.SetKeys(newColorKeys, newAlphaKeys);
                    m_Gradient.mode = value.mode;
                    Dirty(ModificationScope.Node);
                }
            }
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_Gradient = new Gradient();
            var colorKeys = m_SerializableColorKeys.Select(k => new GradientColorKey(new Color(k.x, k.y, k.z, 1f), k.w)).ToArray();
            var alphaKeys = m_SerializableAlphaKeys.Select(k => new GradientAlphaKey(k.x, k.y)).ToArray();
            m_SerializableAlphaKeys = null;
            m_SerializableColorKeys = null;
            m_Gradient.SetKeys(colorKeys, alphaKeys);
            m_Gradient.mode = (GradientMode)m_SerializableMode;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializableColorKeys = gradient.colorKeys.Select(k => new Vector4(k.color.r, k.color.g, k.color.b, k.time)).ToArray();
            m_SerializableAlphaKeys = gradient.alphaKeys.Select(k => new Vector2(k.alpha, k.time)).ToArray();
            m_SerializableMode = (int)gradient.mode;
        }

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new GradientMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.Preview)
            {
                registry.ProvideFunction(GetFunctionName(), s =>
                    {
                        s.AppendLine("Gradient {0} ()",
                            GetFunctionName());
                        using (s.BlockScope())
                        {
                            s.AppendLine("Gradient g;");
                            s.AppendLine("g.type = _{0}_Type;", GetVariableNameForNode());
                            s.AppendLine("g.colorsLength = _{0}_ColorsLength;", GetVariableNameForNode());
                            s.AppendLine("g.alphasLength = _{0}_AlphasLength;", GetVariableNameForNode());
                            for (int i = 0; i < 8; i++)
                            {
                                s.AppendLine("g.colors[{0}] = _{1}_ColorKey{0};", i, GetVariableNameForNode());
                            }
                            for (int i = 0; i < 8; i++)
                            {
                                s.AppendLine("g.alphas[{0}] = _{1}_AlphaKey{0};", i, GetVariableNameForNode());
                            }
                            s.AppendLine("return g;", true);
                        }
                    });
            }
            else
            {
                registry.ProvideFunction(GetFunctionName(), s =>
                    {
                        s.AppendLine("Gradient {0} ()",
                            GetFunctionName());
                        using (s.BlockScope())
                        {
                            GradientUtils.GetGradientDeclaration(m_Gradient, ref s);
                            s.AppendLine("return g;", true);
                        }
                    });
            }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return string.Format("{0}()", GetFunctionName());
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            properties.Add(new PreviewProperty(PropertyType.Vector1)
            {
                name = string.Format("_{0}_Type", GetVariableNameForNode()),
                floatValue = (int)m_Gradient.mode
            });

            properties.Add(new PreviewProperty(PropertyType.Vector1)
            {
                name = string.Format("_{0}_ColorsLength", GetVariableNameForNode()),
                floatValue = m_Gradient.colorKeys.Length
            });

            properties.Add(new PreviewProperty(PropertyType.Vector1)
            {
                name = string.Format("_{0}_AlphasLength", GetVariableNameForNode()),
                floatValue = m_Gradient.alphaKeys.Length
            });

            for (int i = 0; i < 8; i++)
            {
                properties.Add(new PreviewProperty(PropertyType.Vector4)
                {
                    name = string.Format("_{0}_ColorKey{1}", GetVariableNameForNode(), i),
                    vector4Value = i < m_Gradient.colorKeys.Length ? GradientUtils.ColorKeyToVector(m_Gradient.colorKeys[i]) : Vector4.zero
                });
            }

            for (int i = 0; i < 8; i++)
            {
                properties.Add(new PreviewProperty(PropertyType.Vector2)
                {
                    name = string.Format("_{0}_AlphaKey{1}", GetVariableNameForNode(), i),
                    vector4Value = i < m_Gradient.alphaKeys.Length ? GradientUtils.AlphaKeyToVector(m_Gradient.alphaKeys[i]) : Vector2.zero
                });
            }
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            base.CollectShaderProperties(properties, generationMode);

            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_Type", GetVariableNameForNode()),
                generatePropertyBlock = false
            });

            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_ColorsLength", GetVariableNameForNode()),
                generatePropertyBlock = false
            });

            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_AlphasLength", GetVariableNameForNode()),
                generatePropertyBlock = false
            });

            for (int i = 0; i < 8; i++)
            {
                properties.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = string.Format("_{0}_ColorKey{1}", GetVariableNameForNode(), i),
                    generatePropertyBlock = false
                });
            }

            for (int i = 0; i < 8; i++)
            {
                properties.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = string.Format("_{0}_AlphaKey{1}", GetVariableNameForNode(), i),
                    generatePropertyBlock = false
                });
            }
        }
    }
}
