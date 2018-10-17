using System.Reflection;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    public interface IControlAttribute
    {
        VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo);
    }
}
