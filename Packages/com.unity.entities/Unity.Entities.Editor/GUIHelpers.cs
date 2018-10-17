using UnityEngine;

namespace Unity.Entities.Editor
{
    public static class GUIHelpers {

        public static void ShowCenteredNotification(Rect area, string message)
        {
            GUILayout.BeginArea(area);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(message);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        public static Rect GetExpandingRect()
        {
            return GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        }
    }
}