using System;
using System.Linq;

namespace UnityEditor.Rendering.PostProcessing
{
    [InitializeOnLoad]
    sealed class DefineSetter
    {
        const string k_Define = "UNITY_POST_PROCESSING_STACK_V2";
        
        static DefineSetter()
        {
            var targets = Enum.GetValues(typeof(BuildTargetGroup))
                .Cast<BuildTargetGroup>()
                .Where(x => x != BuildTargetGroup.Unknown)
                .Where(x => !IsObsolete(x));

            foreach (var target in targets)
            {
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Trim();

                var list = defines.Split(';', ' ')
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

                if (list.Contains(k_Define))
                    continue;

                list.Add(k_Define);
                defines = list.Aggregate((a, b) => a + ";" + b);

                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
            }
        }

        static bool IsObsolete(BuildTargetGroup group)
        {
            var attrs = typeof(BuildTargetGroup)
                .GetField(group.ToString())
                .GetCustomAttributes(typeof(ObsoleteAttribute), false);

            return attrs != null && attrs.Length > 0;
        }
    }
}
