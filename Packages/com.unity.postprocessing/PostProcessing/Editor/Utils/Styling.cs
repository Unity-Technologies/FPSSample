using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    public static class Styling
    {
        public static readonly GUIStyle smallTickbox;
        public static readonly GUIStyle miniLabelButton;
        
        static readonly Color splitterDark;
        static readonly Color splitterLight;
        public static Color splitter { get { return EditorGUIUtility.isProSkin ? splitterDark : splitterLight; } }

        static readonly Texture2D paneOptionsIconDark;
        static readonly Texture2D paneOptionsIconLight;
        public static Texture2D paneOptionsIcon { get { return EditorGUIUtility.isProSkin ? paneOptionsIconDark : paneOptionsIconLight; } }

        public static readonly GUIStyle headerLabel;

        static readonly Color headerBackgroundDark;
        static readonly Color headerBackgroundLight;
        public static Color headerBackground { get { return EditorGUIUtility.isProSkin ? headerBackgroundDark : headerBackgroundLight; } }

        public static readonly GUIStyle wheelLabel;
        public static readonly GUIStyle wheelThumb;
        public static readonly Vector2 wheelThumbSize;

        public static readonly GUIStyle preLabel;

        static Styling()
        {
            smallTickbox = new GUIStyle("ShurikenToggle");

            miniLabelButton = new GUIStyle(EditorStyles.miniLabel);
            miniLabelButton.normal = new GUIStyleState
            {
                background = RuntimeUtilities.transparentTexture,
                scaledBackgrounds = null,
                textColor = Color.grey
            };
            var activeState = new GUIStyleState
            {
                background = RuntimeUtilities.transparentTexture,
                scaledBackgrounds = null,
                textColor = Color.white
            };
            miniLabelButton.active = activeState;
            miniLabelButton.onNormal = activeState;
            miniLabelButton.onActive = activeState;

            splitterDark = new Color(0.12f, 0.12f, 0.12f, 1.333f);
            splitterLight = new Color(0.6f, 0.6f, 0.6f, 1.333f);
            
            headerBackgroundDark = new Color(0.1f, 0.1f, 0.1f, 0.2f);
            headerBackgroundLight = new Color(1f, 1f, 1f, 0.2f);

            paneOptionsIconDark = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/pane options.png");
            paneOptionsIconLight = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/pane options.png");

            headerLabel = new GUIStyle(EditorStyles.miniLabel);

            wheelThumb = new GUIStyle("ColorPicker2DThumb");

            wheelThumbSize = new Vector2(
                !Mathf.Approximately(wheelThumb.fixedWidth, 0f) ? wheelThumb.fixedWidth : wheelThumb.padding.horizontal,
                !Mathf.Approximately(wheelThumb.fixedHeight, 0f) ? wheelThumb.fixedHeight : wheelThumb.padding.vertical
            );

            wheelLabel = new GUIStyle(EditorStyles.miniLabel);

            preLabel = new GUIStyle("ShurikenLabel");
        }
    }
} 
