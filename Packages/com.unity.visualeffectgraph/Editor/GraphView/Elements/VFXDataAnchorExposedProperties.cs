using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    partial class VFXEditableDataAnchor : VFXDataAnchor
    {
        const string SelectedFieldBackgroundProperty = "selected-field-background";
        const string IMBorderProperty = "im-border";
        const string IMPaddingProperty = "im-padding";


        StyleValue<Texture2D> m_SelectedFieldBackground;
        StyleValue<int> m_IMBorder;
        StyleValue<int> m_IMPadding;
        public Texture2D selectedFieldBackground
        {
            get
            {
                return m_SelectedFieldBackground.GetSpecifiedValueOrDefault(null);
            }
        }

        public int IMBorder
        {
            get
            {
                return m_IMBorder.GetSpecifiedValueOrDefault(0);
            }
        }

        public int IMPadding
        {
            get
            {
                return m_IMPadding.GetSpecifiedValueOrDefault(0);
            }
        }

        protected override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);

            styles.ApplyCustomProperty(SelectedFieldBackgroundProperty, ref m_SelectedFieldBackground);
            styles.ApplyCustomProperty(IMBorderProperty, ref m_IMBorder);
            styles.ApplyCustomProperty(IMPaddingProperty, ref m_IMPadding);
            /*
            if (m_GUIStyles != null)
            {
                m_GUIStyles.baseStyle.active.background = selectedFieldBackground;
                m_GUIStyles.baseStyle.focused.background = m_GUIStyles.baseStyle.active.background;

                m_GUIStyles.baseStyle.border.top = m_GUIStyles.baseStyle.border.left = m_GUIStyles.baseStyle.border.right = m_GUIStyles.baseStyle.border.bottom = IMBorder;
                m_GUIStyles.baseStyle.padding = new RectOffset(IMPadding, IMPadding, IMPadding, IMPadding);
            }
            */
        }
    }
}
