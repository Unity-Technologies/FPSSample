using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditorForRenderPipeline(typeof(ReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    partial class HDReflectionProbeEditor : HDProbeEditor
    {
        [MenuItem("CONTEXT/ReflectionProbe/Remove Component", false, 0)]
        static void RemoveReflectionProbe(MenuCommand menuCommand)
        {
            GameObject go = ((ReflectionProbe)menuCommand.context).gameObject;

            Assert.IsNotNull(go);

            Undo.SetCurrentGroupName("Remove HD Reflection Probe");
            Undo.DestroyObjectImmediate(go.GetComponent<ReflectionProbe>());
            Undo.DestroyObjectImmediate(go.GetComponent<HDAdditionalReflectionData>());
        }

        [MenuItem("CONTEXT/ReflectionProbe/Reset", false, 0)]
        static void ResetReflectionProbe(MenuCommand menuCommand)
        {
            GameObject go = ((ReflectionProbe)menuCommand.context).gameObject;

            Assert.IsNotNull(go);

            ReflectionProbe reflectionProbe = go.GetComponent<ReflectionProbe>();
            HDAdditionalReflectionData reflectionProbeAdditionalData = go.GetComponent<HDAdditionalReflectionData>();

            Assert.IsNotNull(reflectionProbe);
            Assert.IsNotNull(reflectionProbeAdditionalData);

            Undo.SetCurrentGroupName("Reset HD Reflection Probe");
            Undo.RecordObjects(new UnityEngine.Object[] { reflectionProbe, reflectionProbeAdditionalData }, "Reset HD Reflection Probe");
            reflectionProbe.Reset();
            // To avoid duplicating init code we copy default settings to Reset additional data
            // Note: we can't call this code inside the HDAdditionalReflectionData, thus why we don't wrap it in Reset() function
            if(HDUtils.s_DefaultHDAdditionalReflectionData.influenceVolume == null)
            {
                HDUtils.s_DefaultHDAdditionalReflectionData.Awake();
            }
            HDUtils.s_DefaultHDAdditionalReflectionData.CopyTo(reflectionProbeAdditionalData);
        }

        static Dictionary<ReflectionProbe, HDReflectionProbeEditor> s_ReflectionProbeEditors = new Dictionary<ReflectionProbe, HDReflectionProbeEditor>();

        internal override HDProbe GetTarget(Object editorTarget)
        {
            HDReflectionProbeEditor e = s_ReflectionProbeEditors[(ReflectionProbe)editorTarget];
            return (HDProbe)e.m_AdditionalDataSerializedObject.targetObjects.First(a => ((HDAdditionalReflectionData)a).reflectionProbe == editorTarget);
        }

        protected override void Draw(HDProbeUI s, SerializedHDProbe serialized, Editor owner)
        {
#pragma warning disable 612 //Draw
            HDReflectionProbeUI.Inspector.Draw(s, serialized, owner);
#pragma warning restore 612
        }

        static HDReflectionProbeEditor GetEditorFor(ReflectionProbe p)
        {
            HDReflectionProbeEditor e;
            if (s_ReflectionProbeEditors.TryGetValue(p, out e)
                && e != null
                && !e.Equals(null)
                && ArrayUtility.IndexOf(e.targets, p) != -1)
                return e;

            return null;
        }
        
        SerializedObject m_AdditionalDataSerializedObject;

        public bool sceneViewEditing
        {
            get { return HDProbeUI.IsProbeEditMode(EditMode.editMode) && EditMode.IsOwner(this); }
        }
        
        protected override void OnEnable()
        {
            var additionalData = CoreEditorUtils.GetAdditionalData<HDAdditionalReflectionData>(targets);
            m_AdditionalDataSerializedObject = new SerializedObject(additionalData);
            m_SerializedHDProbe = new SerializedHDReflectionProbe(serializedObject, m_AdditionalDataSerializedObject);

            foreach (var t in targets)
            {
                var p = (ReflectionProbe)t;
                s_ReflectionProbeEditors[p] = this;
            }

            base.OnEnable();
            
            InitializeTargetProbe();

            HDAdditionalReflectionData probe = (HDAdditionalReflectionData)m_AdditionalDataSerializedObject.targetObject;
            probe.influenceVolume.Init(probe);

            //unhide previously hidden components if any
            probe.hideFlags = HideFlags.None;
        }
    }
}
