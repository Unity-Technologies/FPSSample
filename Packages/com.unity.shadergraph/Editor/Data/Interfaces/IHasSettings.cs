using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Graphing
{
    public interface IHasSettings
    {
        VisualElement CreateSettingsElement();
    }
}
