using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ChannelMixerControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        float m_Minimum;
        float m_Maximum;

        public ChannelMixerControlAttribute(string label = null, float minimum = -2f, float maximum = 2f)
        {
            m_Label = label;
            m_Minimum = minimum;
            m_Maximum = maximum;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ChannelMixerControlView(m_Label, m_Minimum, m_Maximum, node, propertyInfo);
        }
    }

    public class ChannelMixerControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        ChannelMixerNode.ChannelMixer m_ChannelMixer;
        int m_OutChannel;

        Slider m_RedSlider;
        Slider m_GreenSlider;
        Slider m_BlueSlider;

        FloatField m_RedInputField;
        FloatField m_GreenInputField;
        FloatField m_BlueInputField;

        float m_Minimum;
        float m_Maximum;
        bool m_Initialized;

        public ChannelMixerControlView(string label, float minimum, float maximum, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            AddStyleSheetPath("Styles/Controls/ChannelMixerControlView");
            m_ChannelMixer = (ChannelMixerNode.ChannelMixer)m_PropertyInfo.GetValue(m_Node, null);
            m_OutChannel = 0;

            m_Minimum = minimum;
            m_Maximum = maximum;

            if (propertyInfo.PropertyType != typeof(ChannelMixerNode.ChannelMixer))
                throw new ArgumentException("Property must be of type ChannelMixer.", "propertyInfo");
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);

            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            var buttonPanel = new VisualElement { name = "buttonPanel" };

            Action changedOutputRed = () => OnClickButton(0);
            var outputButtonRed = new Button(changedOutputRed);
            outputButtonRed.Add(new Label("R"));
            buttonPanel.Add(outputButtonRed);

            Action changedOutputGreen = () => OnClickButton(1);
            var outputButtonGreen = new Button(changedOutputGreen);
            outputButtonGreen.Add(new Label("G"));
            buttonPanel.Add(outputButtonGreen);

            Action changedOutputBlue = () => OnClickButton(2);
            var outputButtonBlue = new Button(changedOutputBlue);
            outputButtonBlue.Add(new Label("B"));
            buttonPanel.Add(outputButtonBlue);

            Add(buttonPanel);

            var redSliderPanel = new VisualElement { name = "sliderPanel" };
            redSliderPanel.Add(new Label("R"));
            Action<float> changedRedIn = (s) => { OnChangeSlider(s, 0); };
            m_RedSlider = new Slider(m_Minimum, m_Maximum, changedRedIn);
            redSliderPanel.Add(m_RedSlider);
            m_RedInputField = new FloatField { value = m_ChannelMixer.outRed.x };
            m_RedInputField.RegisterCallback<ChangeEvent<double>, int>(OnChangeInputField, 0);
            redSliderPanel.Add(m_RedInputField);
            Add(redSliderPanel);

            var greenSliderPanel = new VisualElement { name = "sliderPanel" };
            greenSliderPanel.Add(new Label("G"));
            Action<float> changedGreenIn = (s) => { OnChangeSlider(s, 1); };
            m_GreenSlider = new Slider(m_Minimum, m_Maximum, changedGreenIn);
            greenSliderPanel.Add(m_GreenSlider);
            m_GreenInputField = new FloatField { value = m_ChannelMixer.outRed.y };
            m_GreenInputField.RegisterCallback<ChangeEvent<double>, int>(OnChangeInputField, 1);
            greenSliderPanel.Add(m_GreenInputField);
            Add(greenSliderPanel);

            var blueSliderPanel = new VisualElement { name = "sliderPanel" };
            blueSliderPanel.Add(new Label("B"));
            Action<float> changedBlueIn = (s) => { OnChangeSlider(s, 2); };
            m_BlueSlider = new Slider(m_Minimum, m_Maximum, changedBlueIn);
            blueSliderPanel.Add(m_BlueSlider);
            m_BlueInputField = new FloatField { value = m_ChannelMixer.outRed.z };
            m_BlueInputField.RegisterCallback<ChangeEvent<double>, int>(OnChangeInputField, 2);
            blueSliderPanel.Add(m_BlueInputField);
            Add(blueSliderPanel);

            m_Initialized = true;
            ResetSliders();
        }

        void ResetSliders()
        {
            Vector3 outputChannel = GetOutputChannel();
            m_RedSlider.value = outputChannel[0];
            m_GreenSlider.value = outputChannel[1];
            m_BlueSlider.value = outputChannel[2];
        }

        void OnChangeSlider(float value, int inChannel)
        {
            if (!m_Initialized)
                return;
            m_Node.owner.owner.RegisterCompleteObjectUndo("Slider Change");
            switch (m_OutChannel)
            {
                case 1:
                    m_ChannelMixer.outGreen[inChannel] = value;
                    break;
                case 2:
                    m_ChannelMixer.outBlue[inChannel] = value;
                    break;
                default:
                    m_ChannelMixer.outRed[inChannel] = value;
                    break;
            }
            switch (inChannel)
            {
                case 1:
                    m_GreenInputField.value = value;
                    break;
                case 2:
                    m_BlueInputField.value = value;
                    break;
                default:
                    m_RedInputField.value = value;
                    break;
            }
            m_PropertyInfo.SetValue(m_Node, m_ChannelMixer, null);
        }

        void OnChangeInputField(ChangeEvent<double> evt, int inChannel)
        {
            if (!m_Initialized)
                return;
            var value = Mathf.Max(Mathf.Min((float)evt.newValue, m_Maximum), m_Minimum);
            m_Node.owner.owner.RegisterCompleteObjectUndo("Input Field Change");
            switch (m_OutChannel)
            {
                case 1:
                    m_ChannelMixer.outGreen[inChannel] = value;
                    break;
                case 2:
                    m_ChannelMixer.outBlue[inChannel] = value;
                    break;
                default:
                    m_ChannelMixer.outRed[inChannel] = value;
                    break;
            }
            switch (inChannel)
            {
                case 1:
                    m_GreenSlider.value = value;
                    break;
                case 2:
                    m_BlueSlider.value = value;
                    break;
                default:
                    m_RedSlider.value = value;
                    break;
            }
            m_PropertyInfo.SetValue(m_Node, m_ChannelMixer, null);
            this.MarkDirtyRepaint();
        }

        void OnClickButton(int outChannel)
        {
            m_OutChannel = outChannel;
            ResetSliders();
        }

        Vector3 GetOutputChannel()
        {
            switch (m_OutChannel)
            {
                case 1:
                    return m_ChannelMixer.outGreen;
                case 2:
                    return m_ChannelMixer.outBlue;
                default:
                    return m_ChannelMixer.outRed;
            }
        }
    }
}
