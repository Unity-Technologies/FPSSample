using System.Linq;
using UnityEditorInternal;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class PropertySheet : VisualElement
    {
        VisualElement m_ContentContainer;
        VisualElement m_HeaderContainer;
        Label m_Header;
        public override VisualElement contentContainer
        {
            get { return m_ContentContainer; }
        }

        public VisualElement headerContainer
        {
            get { return m_HeaderContainer.FirstOrDefault(); }
            set
            {
                var first = m_HeaderContainer.FirstOrDefault();
                if (first != null)
                    first.RemoveFromHierarchy();

                m_HeaderContainer.Add(value);
            }
        }

        public PropertySheet(Label header = null)
        {
            AddStyleSheetPath("Styles/PropertySheet");
            m_ContentContainer = new VisualElement { name = "content" };
            m_HeaderContainer = new VisualElement { name = "header" };
            if (header != null)
                m_HeaderContainer.Add(header);

            m_ContentContainer.Add(m_HeaderContainer);
            shadow.Add(m_ContentContainer);
        }
    }
}
