using System;

namespace UnityEditor.Experimental.Rendering
{
    [Obsolete("Use EditorGUI.DisabledScope instead", true)]
    public struct SettingsDisableScope : IDisposable
    {
        EditorGUI.DisabledScope scope;

        public SettingsDisableScope(bool enable)
        {
            scope = new EditorGUI.DisabledScope(!enable);
        }

        public void Dispose()
        {
            scope.Dispose();
        }
    }
}
