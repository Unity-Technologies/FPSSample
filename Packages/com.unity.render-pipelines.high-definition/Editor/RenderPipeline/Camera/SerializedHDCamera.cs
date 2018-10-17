using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedHDCamera
    {
        public SerializedObject serializedObject;
        public SerializedObject serializedAdditionalDataObject;

        //public SerializedProperty backgroundColor;
        public SerializedProperty normalizedViewPortRect;
        public SerializedProperty fieldOfView;
        public SerializedProperty orthographic;
        public SerializedProperty orthographicSize;
        public SerializedProperty depth;
        public SerializedProperty cullingMask;
        public SerializedProperty occlusionCulling;
        public SerializedProperty targetTexture;
        public SerializedProperty HDR;
        public SerializedProperty stereoConvergence;
        public SerializedProperty stereoSeparation;
        public SerializedProperty nearClippingPlane;
        public SerializedProperty farClippingPlane;
        public SerializedProperty targetEye;

        public SerializedProperty aperture;
        public SerializedProperty shutterSpeed;
        public SerializedProperty iso;

#if ENABLE_MULTIPLE_DISPLAYS
        public SerializedProperty targetDisplay;
#endif

        public SerializedProperty clearColorMode;
        public SerializedProperty backgroundColorHDR;
        public SerializedProperty renderingPath;
        public SerializedProperty clearDepth;
        public SerializedProperty volumeLayerMask;
        public SerializedProperty volumeAnchorOverride;
        public SerializedFrameSettings frameSettings;


        public SerializedHDCamera(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            var additionals = CoreEditorUtils.GetAdditionalData<HDAdditionalCameraData>(serializedObject.targetObjects, HDAdditionalCameraData.InitDefaultHDAdditionalCameraData);
            serializedAdditionalDataObject = new SerializedObject(additionals);

            var hideFlags = serializedAdditionalDataObject.FindProperty("m_ObjectHideFlags");
            // We don't hide additional camera data anymore on UX team request. To be compatible with already author scene we force to be visible
            //hideFlags.intValue = (int)HideFlags.HideInInspector;
            hideFlags.intValue = (int)HideFlags.None;
            serializedAdditionalDataObject.ApplyModifiedProperties();

            //backgroundColor = serializedObject.FindProperty("m_BackGroundColor");
            normalizedViewPortRect = serializedObject.FindProperty("m_NormalizedViewPortRect");
            nearClippingPlane = serializedObject.FindProperty("near clip plane");
            farClippingPlane = serializedObject.FindProperty("far clip plane");
            fieldOfView = serializedObject.FindProperty("field of view");
            orthographic = serializedObject.FindProperty("orthographic");
            orthographicSize = serializedObject.FindProperty("orthographic size");
            depth = serializedObject.FindProperty("m_Depth");
            cullingMask = serializedObject.FindProperty("m_CullingMask");
            occlusionCulling = serializedObject.FindProperty("m_OcclusionCulling");
            targetTexture = serializedObject.FindProperty("m_TargetTexture");
            HDR = serializedObject.FindProperty("m_HDR");

            stereoConvergence = serializedObject.FindProperty("m_StereoConvergence");
            stereoSeparation = serializedObject.FindProperty("m_StereoSeparation");

#if ENABLE_MULTIPLE_DISPLAYS
            targetDisplay = serializedObject.FindProperty("m_TargetDisplay");
#endif

            targetEye = serializedObject.FindProperty("m_TargetEye");

            aperture = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.aperture);
            shutterSpeed = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.shutterSpeed);
            iso = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.iso);

            clearColorMode = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.clearColorMode);
            backgroundColorHDR = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.backgroundColorHDR);
            renderingPath = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.renderingPath);
            clearDepth = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.clearDepth);
            volumeLayerMask = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.volumeLayerMask);
            volumeAnchorOverride = serializedAdditionalDataObject.Find((HDAdditionalCameraData d) => d.volumeAnchorOverride);
            frameSettings = new SerializedFrameSettings(serializedAdditionalDataObject.FindProperty("m_FrameSettings"));
        }

        public void Update()
        {
            serializedObject.Update();
            serializedAdditionalDataObject.Update();

            // Be sure legacy HDR option is disable on camera as it cause banding in SceneView. Yes, it is a contradiction, but well, Unity...
            // When HDR option is enabled, Unity render in FP16 then convert to 8bit with a stretch copy (this cause banding as it should be convert to sRGB (or other color appropriate color space)), then do a final shader with sRGB conversion
            // When LDR, unity render in 8bitSRGB, then do a final shader with sRGB conversion
            // What should be done is just in our Post process we convert to sRGB and store in a linear 10bit, but require C++ change...
            HDR.boolValue = false;
        }

        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
            serializedAdditionalDataObject.ApplyModifiedProperties();
        }
    }
}
