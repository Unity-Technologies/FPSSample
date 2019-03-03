using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(LinearFog))]
    public class LinearFogEditor : AtmosphericScatteringEditor
    {
        SerializedDataParameter m_Density;
        SerializedDataParameter m_FogStart;
        SerializedDataParameter m_FogEnd;
        SerializedDataParameter m_FogHeightStart;
        SerializedDataParameter m_FogHeightEnd;

        public override void OnEnable()
        {
            base.OnEnable();
            var o = new PropertyFetcher<LinearFog>(serializedObject);

            m_Density = Unpack(o.Find(x => x.density));
            m_FogStart = Unpack(o.Find(x => x.fogStart));
            m_FogEnd = Unpack(o.Find(x => x.fogEnd));
            m_FogHeightStart = Unpack(o.Find(x => x.fogHeightStart));
            m_FogHeightEnd = Unpack(o.Find(x => x.fogHeightEnd));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Density);
            PropertyField(m_FogStart);
            PropertyField(m_FogEnd);
            PropertyField(m_FogHeightStart);
            PropertyField(m_FogHeightEnd);
            PropertyField(m_MaxFogDistance);
            base.OnInspectorGUI(); // Color
        }
    }
}
