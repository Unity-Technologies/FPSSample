using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ScreenSpaceRefraction))]
    public class ScreenSpaceRefractionEditor : VolumeComponentEditor
    {
        protected SerializedDataParameter m_ScreenFadeDistance;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceRefraction>(serializedObject);

            m_ScreenFadeDistance = Unpack(o.Find(x => x.screenFadeDistance));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_ScreenFadeDistance, CoreEditorUtils.GetContent("Screen Weight Distance"));
        }
    }
}
