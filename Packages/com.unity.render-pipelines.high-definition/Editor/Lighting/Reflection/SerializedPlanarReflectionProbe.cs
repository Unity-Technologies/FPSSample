using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal class SerializedPlanarReflectionProbe : SerializedHDProbe
    {
        internal SerializedProperty captureLocalPosition;
        internal SerializedProperty capturePositionMode;
        internal SerializedProperty captureMirrorPlaneLocalPosition;
        internal SerializedProperty captureMirrorPlaneLocalNormal;

        internal new PlanarReflectionProbe target { get { return serializedObject.targetObject as PlanarReflectionProbe; } }

        internal bool isMirrored
        {
            get
            {
                return refreshMode.intValue == (int)ReflectionProbeRefreshMode.EveryFrame
                    && mode.intValue == (int)ReflectionProbeMode.Realtime
                    && capturePositionMode.intValue == (int)PlanarReflectionProbe.CapturePositionMode.MirrorCamera;
            }
        }

        internal SerializedPlanarReflectionProbe(SerializedObject serializedObject) : base(serializedObject)
        {
            captureLocalPosition = serializedObject.Find((PlanarReflectionProbe p) => p.captureLocalPosition);
            nearClip = serializedObject.Find((PlanarReflectionProbe p) => p.captureSettings.nearClipPlane);
            farClip = serializedObject.Find((PlanarReflectionProbe p) => p.captureSettings.farClipPlane);
            capturePositionMode = serializedObject.Find((PlanarReflectionProbe p) => p.capturePositionMode);
            captureMirrorPlaneLocalPosition = serializedObject.Find((PlanarReflectionProbe p) => p.captureMirrorPlaneLocalPosition);
            captureMirrorPlaneLocalNormal = serializedObject.Find((PlanarReflectionProbe p) => p.captureMirrorPlaneLocalNormal);

            influenceVolume.editorSimplifiedModeBlendNormalDistance.floatValue = 0;
        }
        
        internal override void Update()
        {
            base.Update();

            //temporarily force value until other mode are supported
            mode.enumValueIndex = (int)ReflectionProbeMode.Realtime;
            refreshMode.enumValueIndex = (int)ReflectionProbeRefreshMode.EveryFrame;
            capturePositionMode.enumValueIndex = (int)PlanarReflectionProbe.CapturePositionMode.MirrorCamera;
        }
    }
}
