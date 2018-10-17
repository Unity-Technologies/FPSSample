using System.Linq;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class PropertyRow : VisualElement
    {
        VisualElement m_ContentContainer;
        VisualElement m_LabelContainer;

        public override VisualElement contentContainer
        {
            get { return m_ContentContainer; }
        }

        public VisualElement label
        {
            get { return m_LabelContainer.FirstOrDefault(); }
            set
            {
                var first = m_LabelContainer.FirstOrDefault();
                if (first != null)
                    first.RemoveFromHierarchy();
                m_LabelContainer.Add(value);
            }
        }

        public PropertyRow(VisualElement label = null)
        {
            AddStyleSheetPath("Styles/PropertyRow");
            VisualElement container = new VisualElement {name = "container"};
            m_ContentContainer = new VisualElement { name = "content"  };
            m_LabelContainer = new VisualElement {name = "label" };
            m_LabelContainer.Add(label);

            container.Add(m_LabelContainer);
            container.Add(m_ContentContainer);

            shadow.Add(container);
        }
    }
}
