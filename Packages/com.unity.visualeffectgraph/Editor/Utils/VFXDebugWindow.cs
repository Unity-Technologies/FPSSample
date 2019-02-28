using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    public class VFXDebugWindow : EditorWindow
    {
        [MenuItem("Window/Visual Effects/VFXEditor Debug Window", false, 3011, true)]
        public static void OpenWindow()
        {
            GetWindow<VFXDebugWindow>();
        }

        private void OnGUI()
        {
            titleContent = Contents.title;

            EditorGUILayout.LabelField("VFX Assets", EditorStyles.boldLabel);
            using (new GUILayout.HorizontalScope())
            {
                /*if (GUILayout.Button("Clear"))
                    VFXCacheManager.Clear();*/

                if (GUILayout.Button("Recompile All"))
                    VFXCacheManager.Build();
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Run VFX Tests", EditorStyles.boldLabel);
            using (new GUILayout.HorizontalScope())
            {
                /*if (GUILayout.Button("GUI Tests"))
                    Test.VFXGUITests.RunGUITests();*/
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Migration", EditorStyles.boldLabel);
            using (new GUILayout.HorizontalScope())
            {
                /*if (GUILayout.Button("Migrate to .vfx"))
                    VFXMigration.Migrate();*/
                if (GUILayout.Button("Resave all .vfx"))
                    VFXMigration.Resave();
            }
            EditorGUILayout.Space();
        }

        static class Contents
        {
            public static GUIContent title = new GUIContent("VFX Debug");
        }
    }
}
