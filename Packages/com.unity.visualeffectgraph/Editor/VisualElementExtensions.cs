using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.UIElements;
using System.Reflection;


static class VisualElementExtensions
{
    static MethodInfo m_ValidateLayoutMethod;
    public static void InternalValidateLayout(this IPanel panel)
    {
        if (m_ValidateLayoutMethod == null)
            m_ValidateLayoutMethod = panel.GetType().GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public);

        m_ValidateLayoutMethod.Invoke(panel, new object[] {});
    }

    static PropertyInfo m_OwnerPropertyInfo;

    public static GUIView  InternalGetGUIView(this IPanel panel)
    {
        if (m_OwnerPropertyInfo == null)
            m_OwnerPropertyInfo = panel.GetType().GetProperty("ownerObject", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public);


        return (GUIView)m_OwnerPropertyInfo.GetValue(panel, new object[] {});
    }

    public static bool HasFocus(this VisualElement visualElement)
    {
        if (visualElement.panel == null) return false;
        return visualElement.panel.focusController.focusedElement == visualElement;
    }

    public static void AddStyleSheetPathWithSkinVariant(this VisualElement visualElement, string path)
    {
        visualElement.AddStyleSheetPath(path);
        //if (true)
        {
            visualElement.AddStyleSheetPath(path + "Dark");
        }
        /*else
        {
            visualElement.AddStyleSheetPath(path + "Light");
        }*/
    }

    public static Vector2 GlobalToBound(this VisualElement visualElement, Vector2 position)
    {
        return visualElement.worldTransform.inverse.MultiplyPoint3x4(position);
    }

    public static Vector2 BoundToGlobal(this VisualElement visualElement, Vector2 position)
    {
        /*do
        {*/
        position = visualElement.worldTransform.MultiplyPoint3x4(position);
        /*
        visualElement = visualElement.parent;
    }
    while (visualElement != null;)*/

        return position;
    }
}
