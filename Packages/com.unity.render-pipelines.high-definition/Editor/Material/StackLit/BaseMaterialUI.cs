using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    abstract class BaseMaterialGUI : BaseUnlitGUI
    {
        #region GUI Property Classes
        public abstract class BaseProperty
        {
            public BaseMaterialGUI Parent = null;

            protected Func<object, bool> IsVisible;

            protected BaseProperty(BaseMaterialGUI parent, Func<object, bool> isVisible = null)
            {
                Parent = parent;
                IsVisible = isVisible;
            }

            public abstract void OnFindProperty(MaterialProperty[] props);

            public abstract void OnGUI();

            internal abstract string ToShaderPropertiesStringInternal();
        }

        public class GroupProperty : BaseProperty
        {
            public string m_Title = string.Empty;

            private readonly BaseProperty[] m_ChildProperties;
            private readonly Property m_Show;

            public GroupProperty(BaseMaterialGUI parent, string groupName, BaseProperty[] childProperties, Func<object, bool> isVisible = null)
                : this(parent, groupName, string.Empty, childProperties, isVisible)
            {
            }

            public GroupProperty(BaseMaterialGUI parent, string groupName, string groupTitle, BaseProperty[] childProperties, Func<object, bool> isVisible = null)
                : base(parent, isVisible)
            {
                m_Show = new Property(parent, groupName + "Show", "", false);

                m_Title = groupTitle;
                m_ChildProperties = childProperties;
            }

            public override void OnFindProperty(MaterialProperty[] props)
            {
                m_Show.OnFindProperty(props);

                foreach (var c in m_ChildProperties)
                {
                    c.OnFindProperty(props);
                }
            }

            public override void OnGUI()
            {
                if (IsVisible == null || IsVisible(this))
                {
                    if (!string.IsNullOrEmpty(m_Title))
                    {
                        m_Show.BoolValue = EditorGUILayout.Foldout(m_Show.BoolValue, m_Title);
                    }
                    else if (m_Show.IsValid)
                    {
                        m_Show.BoolValue = true;
                    }

                    if (!m_Show.IsValid || m_Show.BoolValue)
                    {
                        EditorGUI.indentLevel++;

                        foreach (var c in m_ChildProperties)
                        {
                            c.OnGUI();
                        }

                        EditorGUI.indentLevel--;
                    }
                }
            }

            internal override string ToShaderPropertiesStringInternal()
            {
                string outputString = string.Empty;

                foreach (var c in m_ChildProperties)
                {
                    outputString += c.ToShaderPropertiesStringInternal() + "\n";
                }

                return outputString;
            }
        }

        public class Property : BaseProperty
        {
            public string PropertyName;
            public string PropertyText;

            public MaterialProperty m_MaterialProperty = null;

            protected readonly GUIContent m_GuiContent = null;

            public bool IsMandatory = false;

            public bool IsValid
            {
                get { return m_MaterialProperty != null; }
            }

            public float FloatValue
            {
                get { return m_MaterialProperty.floatValue; }
                set { m_MaterialProperty.floatValue = value; }
            }

            public bool BoolValue
            {
                get { return m_MaterialProperty == null || Math.Abs(m_MaterialProperty.floatValue) > 0.0f; }
                set { m_MaterialProperty.floatValue = value ? 1.0f : 0.0f; }
            }

            public Texture TextureValue
            {
                get { return m_MaterialProperty != null ? m_MaterialProperty.textureValue : null; }
                set { if (m_MaterialProperty != null) { m_MaterialProperty.textureValue = value; } }
            }

            public Vector4 VectorValue
            {
                get { return m_MaterialProperty.vectorValue; }
                set { m_MaterialProperty.vectorValue = value; }
            }

            public Property(BaseMaterialGUI parent, string propertyName, string guiText, bool isMandatory = true, Func<object, bool> isVisible = null)
                : this(parent, propertyName, guiText, string.Empty, isMandatory, isVisible)
            {
            }

            public Property(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, isVisible)
            {
                m_GuiContent = new GUIContent(guiText, toolTip);
                PropertyName = propertyName;
                PropertyText = guiText;
                IsMandatory = isMandatory;
            }

            public override void OnFindProperty(MaterialProperty[] props)
            {
                m_MaterialProperty = ShaderGUI.FindProperty(PropertyName, props, IsMandatory);
            }

            public override void OnGUI()
            {
                if (IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    Parent.m_MaterialEditor.ShaderProperty(m_MaterialProperty, m_GuiContent);
                }
            }

            internal override string ToShaderPropertiesStringInternal()
            {
                if (IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    switch (m_MaterialProperty.type)
                    {
                        case MaterialProperty.PropType.Color:
                            return string.Format("{0}(\"{1}\", Color) = (1, 1, 1, 1)", PropertyName, PropertyText);

                        case MaterialProperty.PropType.Vector:
                            return string.Format("{0}(\"{1}\", Vector) = (0, 0, 0, 0)", PropertyName, PropertyText);

                        case MaterialProperty.PropType.Float:
                            return string.Format("{0}(\"{1}\", Float) = 0.0", PropertyName, PropertyText);

                        case MaterialProperty.PropType.Range:
                            return string.Format("{0}(\"{1}\", Range({2:0.0###}, {3:0.0###})) = 0", PropertyName, PropertyText, m_MaterialProperty.rangeLimits.x, m_MaterialProperty.rangeLimits.y);

                        case MaterialProperty.PropType.Texture:
                            return string.Format("{0}(\"{1}\", 2D) = \"white\" {{}}", PropertyName, PropertyText);

                        default:
                            // Unknown type... default to outputting a float.
                            return string.Format("{0}(\"{1}\", Float) = 0.0", PropertyName, PropertyText);
                    }
                }
                else
                {
                    // Material property is not loaded, default to outputting a float.
                    return string.Format("{0}(\"{1}\", Float) = 0.0", PropertyName, PropertyText);
                }
            }
        }

        public class ComboProperty : Property
        {
            private readonly string[] m_Options;
            private readonly int[] m_Values = null;

            public ComboProperty(BaseMaterialGUI parent, string propertyName, string guiText, string[] options, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, isMandatory, isVisible)
            {
                m_Options = options;
            }

            public ComboProperty(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, string[] options, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, toolTip, isMandatory, isVisible)
            {
                m_Options = options;
            }

            public ComboProperty(BaseMaterialGUI parent, string propertyName, string guiText, string[] options, int[] values, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, isMandatory, isVisible)
            {
                m_Options = options;
                m_Values = values;
            }

            public ComboProperty(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, string[] options, int[] values, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, toolTip, isMandatory, isVisible)
            {
                m_Options = options;
                m_Values = values;
            }

            public override void OnGUI()
            {
                if (IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    EditorGUI.showMixedValue = m_MaterialProperty.hasMixedValue;
                    float floatValue = m_MaterialProperty.floatValue;

                    EditorGUI.BeginChangeCheck();

                    if (m_Values == null)
                    {
                        floatValue = EditorGUILayout.Popup(m_GuiContent, (int)floatValue, m_Options);
                    }
                    else
                    {
                        floatValue = EditorGUILayout.IntPopup(m_GuiContent.text, (int)floatValue, m_Options, m_Values);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Parent.m_MaterialEditor.RegisterPropertyChangeUndo(PropertyName);
                        m_MaterialProperty.floatValue = (float)floatValue;
                    }

                    EditorGUI.showMixedValue = false;
                }
            }
        }

        public class DiffusionProfileProperty : Property
        {
            public DiffusionProfileProperty(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, toolTip, isMandatory, isVisible)
            {
            }

            public override void OnGUI()
            {
                if (IsValid
                    && (IsVisible == null || IsVisible(this)))
                {
                    var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                    if (hdPipeline == null)
                    {
                        return;
                    }

                    var diffusionProfileSettings = hdPipeline.diffusionProfileSettings;
                    if (hdPipeline.IsInternalDiffusionProfile(diffusionProfileSettings))
                    {
                        EditorGUILayout.HelpBox(
                            "No diffusion profile Settings have been assigned to the render pipeline asset.",
                            MessageType.Warning);
                        return;
                    }

                    // TODO: Optimize me
                    var profiles = diffusionProfileSettings.profiles;
                    var names = new GUIContent[profiles.Length + 1];
                    names[0] = new GUIContent("None");

                    var values = new int[names.Length];
                    values[0] = DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID;

                    for (int i = 0; i < profiles.Length; i++)
                    {
                        names[i + 1] = new GUIContent(profiles[i].name);
                        values[i + 1] = i + 1;
                    }

                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        int profileID = (int)FloatValue;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PrefixLabel(m_GuiContent.text);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                profileID = EditorGUILayout.IntPopup(profileID, names, values);

                                if (GUILayout.Button("Goto", EditorStyles.miniButton, GUILayout.Width(50f)))
                                {
                                    Selection.activeObject = diffusionProfileSettings;
                                }
                            }
                        }

                        if (scope.changed)
                        {
                            FloatValue = profileID;
                        }
                    }
                }
            }
        }

        public class TextureOneLineProperty : Property
        {
            public string ExtraPropertyName;

            private MaterialProperty m_ExtraProperty;

            public TextureOneLineProperty(BaseMaterialGUI parent, string propertyName, string guiText, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, isMandatory, isVisible)
            {
            }

            public TextureOneLineProperty(BaseMaterialGUI parent, string propertyName, string guiText, string toolTip, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, toolTip, isMandatory, isVisible)
            {
            }

            public TextureOneLineProperty(BaseMaterialGUI parent, string propertyName, string extraPropertyName, string guiText, string toolTip, bool isMandatory = true, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, toolTip, isMandatory, isVisible)
            {
                ExtraPropertyName = extraPropertyName;
            }

            public override void OnFindProperty(MaterialProperty[] props)
            {
                base.OnFindProperty(props);

                if (!string.IsNullOrEmpty(ExtraPropertyName))
                {
                    m_ExtraProperty = ShaderGUI.FindProperty(ExtraPropertyName, props, IsMandatory);
                }
            }

            public override void OnGUI()
            {
                if (IsValid && (IsVisible == null || IsVisible(this)))
                {
                    Parent.m_MaterialEditor.TexturePropertySingleLine(m_GuiContent, m_MaterialProperty, m_ExtraProperty);
                }
            }

            internal override string ToShaderPropertiesStringInternal()
            {
                return string.Format("{0}(\"{1}\", 2D) = \"white\" {{}}\n" +
                    "{2}(\"{3}\", Float) = 0.0",
                    PropertyName, PropertyText,
                    ExtraPropertyName, ExtraPropertyName);
            }
        }

        public class TextureProperty : Property
        {
            public enum Tiling
            {
                Wrap,
                Clamp,
            }
            public enum Channel
            {
                R,
                G,
                B,
                A,
            }

            public enum PlanarSpace
            {
                World,
                Local
            }

            public enum NormalSpace
            {
                Tangent,
                Object
            }

            public enum UVMapping
            {
                UV0,
                UV1,
                UV2,
                UV3,
                PlanarXY,
                PlanarYZ,
                PlanarZX,
                Triplanar,
            }

            public Property m_Show;

            public Property m_ConstantProperty;

            public TextureOneLineProperty m_TextureProperty;

            public ComboProperty m_UvSetProperty;

            public ComboProperty m_LocalOrWorldProperty;

            public ComboProperty m_NormalSpaceProperty;

            public Property m_ChannelProperty;

            public Property m_RemapProperty;

            public Property m_InvertRemapProperty;

            public string m_ConstantPropertyName;

            public bool m_IsNormalMap;

            public TextureProperty(BaseMaterialGUI parent, string propertyName, string constantPropertyName, string guiText, bool useConstantAsTint, bool isMandatory = true, bool isNormalMap = false, Func<object, bool> isVisible = null)
                : this(parent, propertyName, constantPropertyName, guiText, string.Empty, useConstantAsTint, isMandatory, isNormalMap, isVisible)
            {
            }

            public TextureProperty(BaseMaterialGUI parent, string propertyName, string constantPropertyName, string guiText, string toolTip, bool useConstantAsTint, bool isMandatory = true, bool isNormalMap = false, Func<object, bool> isVisible = null)
                : base(parent, propertyName, guiText, toolTip, isMandatory, isVisible)
            {
                m_IsNormalMap = isNormalMap;

                m_ConstantPropertyName = constantPropertyName;

                m_Show = new Property(parent, propertyName + "Show", "", isMandatory);

                if (useConstantAsTint == false)
                {
                    m_ConstantProperty = new Property(parent, constantPropertyName, guiText, toolTip, isMandatory);
                }

                m_TextureProperty = new TextureOneLineProperty(parent, propertyName, useConstantAsTint ? constantPropertyName : string.Empty, guiText, toolTip, isMandatory);

                m_UvSetProperty = new ComboProperty(parent, propertyName + "UV", "UV Mapping", Enum.GetNames(typeof(UVMapping)), false);
                m_LocalOrWorldProperty = new ComboProperty(parent, propertyName + "UVLocal", "Local or world", Enum.GetNames(typeof(PlanarSpace)), false);

                m_NormalSpaceProperty = new ComboProperty(parent, propertyName + "ObjSpace", "Normal space", Enum.GetNames(typeof(NormalSpace)), false);


                m_ChannelProperty = new ComboProperty(parent, propertyName + "Channel", "Channel", Enum.GetNames(typeof(Channel)), false);

                m_RemapProperty = new Property(parent, propertyName + "Remap", "Remapping", "Defines the range to remap/scale the values in texture", false);
                m_InvertRemapProperty = new Property(parent, propertyName + "RemapInverted", "Invert Remapping", "Whether the mapping values are inverted.", false);
            }

            public override void OnFindProperty(MaterialProperty[] props)
            {
                base.OnFindProperty(props);

                m_Show.OnFindProperty(props);
                if (m_ConstantProperty != null)
                {
                    m_ConstantProperty.OnFindProperty(props);
                }
                m_TextureProperty.OnFindProperty(props);
                m_UvSetProperty.OnFindProperty(props);
                m_LocalOrWorldProperty.OnFindProperty(props);
                m_NormalSpaceProperty.OnFindProperty(props);
                m_ChannelProperty.OnFindProperty(props);
                m_RemapProperty.OnFindProperty(props);
                m_InvertRemapProperty.OnFindProperty(props);
            }

            public override void OnGUI()
            {
                if ((IsVisible == null || IsVisible(this))
                    && m_Show.IsValid
                    && m_TextureProperty.IsValid)
                {
                    m_Show.BoolValue = EditorGUILayout.Foldout(m_Show.BoolValue, PropertyText);

                    if (m_Show.BoolValue)
                    {
                        EditorGUI.indentLevel++;
                        if (m_ConstantProperty != null && m_ConstantProperty.IsValid
                            && m_TextureProperty.TextureValue == null)
                        {
                            m_ConstantProperty.OnGUI();
                        }

                        m_TextureProperty.OnGUI();

                        if (m_TextureProperty.TextureValue != null)
                        {
                            m_UvSetProperty.OnGUI();
                            m_ChannelProperty.OnGUI();

                            Parent.m_MaterialEditor.TextureScaleOffsetProperty(m_TextureProperty.m_MaterialProperty);

                            if (m_UvSetProperty.FloatValue >= (float)UVMapping.PlanarXY)
                            {
                                m_LocalOrWorldProperty.OnGUI();
                            }

                            if (m_IsNormalMap)
                            {
                                m_NormalSpaceProperty.OnGUI();
                            }

                            if (m_RemapProperty.IsValid)
                            {
                                // Display the remap of texture values.
                                Vector2 remap = m_RemapProperty.VectorValue;
                                EditorGUI.BeginChangeCheck();
                                EditorGUILayout.MinMaxSlider(m_RemapProperty.PropertyText, ref remap.x, ref remap.y, 0.0f, 1.0f);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    m_RemapProperty.VectorValue = remap;
                                }

                                if (m_InvertRemapProperty.IsValid)
                                {
                                    m_InvertRemapProperty.OnGUI();
                                }
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                }
            }

            internal override string ToShaderPropertiesStringInternal()
            {
                string constantName = m_ConstantPropertyName.StartsWith("_")
                    ? m_ConstantPropertyName.Substring(1)
                    : m_ConstantPropertyName;

                return string.Format(
                    "[HideInInspector] {0}MapShow(\"{1} Show\", Float) = 0\n" +
                    "{0}(\"{1}\", Range(0.0, 1.0)) = 0\n" +
                    "{2}(\"{1} Map\", 2D) = " + (m_IsNormalMap ? "\"bump\"" : "\"white\"") + " {{ }}\n" +
                    "{0}UseMap(\"{1} Use Map\", Float) = 0\n" +
                    (m_IsNormalMap ? "{0}ObjSpace(\"{1} Object Space\", Float) = 0\n" : "") +
                    "{2}UV(\"{1} Map UV\", Float) = 0.0\n" +
                    "{2}UVLocal(\"{1} Map UV Local\", Float) = 0.0\n" +
                    "{2}Channel(\"{1} Map Channel\", Float) = 0.0\n" +
                    "{2}ChannelMask(\"{1} Map Channel Mask\", Vector) = (1, 0, 0, 0)\n" +
                    "{0}Remap(\"{1} Remap\", Vector) = (0, 1, 0, 0)\n" +
                    "[ToggleUI] {0}RemapInverted(\"Invert {1} Remap\", Float) = 0.0\n" +
                    "[HideInInspector] {0}Range(\"{1} Range\", Vector) = (0, 1, 0, 0)\n",
                    m_ConstantPropertyName, constantName, PropertyName);
            }
        }
        #endregion
    }
}
