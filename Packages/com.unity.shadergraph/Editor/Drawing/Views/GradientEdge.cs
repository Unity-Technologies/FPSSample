using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class GradientEdge : Edge
    {
        const string k_InputColorProperty = "edge-input-color";
        const string k_OutputColorProperty = "edge-output-color";

        StyleValue<Color> m_InputColor;
        StyleValue<Color> m_OutputColor;

        public Color inputColor
        {
            get { return m_InputColor.GetSpecifiedValueOrDefault(defaultColor); }
        }

        public Color outputColor
        {
            get { return m_OutputColor.GetSpecifiedValueOrDefault(defaultColor); }
        }

        public void UpdateClasses(ConcreteSlotValueType outputType, ConcreteSlotValueType inputType)
        {
            ClearClassList();
            AddToClassList("edge");
            AddToClassList("from" + outputType);
            AddToClassList("to" + inputType);
        }

        protected override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);
            styles.ApplyCustomProperty(k_InputColorProperty, ref m_InputColor);
            styles.ApplyCustomProperty(k_OutputColorProperty, ref m_OutputColor);
        }

        protected override void DrawEdge()
        {
            if (!UpdateEdgeControl())
                return;

            edgeControl.edgeWidth = edgeWidth;
            edgeControl.inputColor = isGhostEdge ? ghostColor : (selected ? selectedColor : inputColor);
            edgeControl.outputColor = isGhostEdge ? ghostColor : (selected ? selectedColor : outputColor);
//            edgeControl.startCapColor = edgeControl.outputColor;
//            edgeControl.endCapColor = edgeControl.inputColor;
        }
    }
}
