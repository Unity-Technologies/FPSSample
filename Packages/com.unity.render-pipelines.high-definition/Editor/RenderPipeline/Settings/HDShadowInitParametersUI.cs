using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<HDShadowInitParametersUI, SerializedHDShadowInitParameters>;

    class HDShadowInitParametersUI : BaseUI<SerializedHDShadowInitParameters>
    {
        enum ShadowResolution
        {
            ShadowResolution128 = 128,
            ShadowResolution256 = 256,
            ShadowResolution512 = 512,
            ShadowResolution1024 = 1024,
            ShadowResolution2048 = 2048,
            ShadowResolution4096 = 4096,
            ShadowResolution8192 = 8192,
            ShadowResolution16384 = 16384
        }

#pragma warning disable 618 //CED
        public static readonly CED.IDrawer Inspector = CED.FoldoutGroup(
#pragma warning restore 618
            "Shadows",
            (s, d, o) => s.isSectionExpendedShadowSettings,
            FoldoutOption.None,
            CED.Action(Drawer_FieldHDShadows)
            );

        AnimatedValues.AnimBool isSectionExpendedShadowSettings { get { return m_AnimBools[0]; } }

        public HDShadowInitParametersUI()
            : base(1)
        {
            isSectionExpendedShadowSettings.value = true;
        }

        static void Drawer_FieldHDShadows(HDShadowInitParametersUI s, SerializedHDShadowInitParameters d, Editor o)
        {
            EditorGUILayout.LabelField(_.GetContent("Atlas"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            d.shadowAtlasResolution.intValue = (int)(ShadowResolution)EditorGUILayout.EnumPopup(_.GetContent("Resolution"), (ShadowResolution)d.shadowAtlasResolution.intValue);
            
            bool shadowMap16Bits = (DepthBits)d.shadowMapDepthBits.intValue	== DepthBits.Depth16;
            shadowMap16Bits = EditorGUILayout.Toggle(_.GetContent("16-bit"), shadowMap16Bits);
            d.shadowMapDepthBits.intValue = (shadowMap16Bits) ? (int)DepthBits.Depth16 : (int)DepthBits.Depth32;
            EditorGUILayout.PropertyField(d.useDynamicViewportRescale, _.GetContent("Dynamic Rescale|Scale the shadow map size using the screen size of the light to leave more space for other shadows in the atlas"));
            --EditorGUI.indentLevel;

            EditorGUILayout.Space();

            // EditorGUILayout.LabelField(_.GetContent("Budget"), EditorStyles.boldLabel);
            // ++EditorGUI.indentLevel;
            EditorGUILayout.DelayedIntField(d.maxShadowRequests, _.GetContent("Max Shadow on Screen|Max shadow on screen (S) per frame, 1 point light = 6 S, 1 spot light = 1 S and the directional is 4 S"));
            // --EditorGUI.indentLevel;
            
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(_.GetContent("Filtering Qualities"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.shadowQuality, _.GetContent("ShadowQuality"));
            --EditorGUI.indentLevel;

            EditorGUILayout.Space();
        }
    }
}
