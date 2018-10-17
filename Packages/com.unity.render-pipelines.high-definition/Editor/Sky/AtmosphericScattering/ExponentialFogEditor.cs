using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(ExponentialFog))]
    public class ExponentialFogEditor : AtmosphericScatteringEditor
    {
        private SerializedDataParameter m_FogDistance;
        private SerializedDataParameter m_FogBaseHeight;
        private SerializedDataParameter m_FogHeightAttenuation;

        public override void OnEnable()
        {
            base.OnEnable();
            var o = new PropertyFetcher<ExponentialFog>(serializedObject);

            m_FogDistance = Unpack(o.Find(x => x.fogDistance));
            m_FogBaseHeight = Unpack(o.Find(x => x.fogBaseHeight));
            m_FogHeightAttenuation = Unpack(o.Find(x => x.fogHeightAttenuation));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            PropertyField(m_FogDistance);
            PropertyField(m_FogBaseHeight);
            PropertyField(m_FogHeightAttenuation);
        }
    }
}
