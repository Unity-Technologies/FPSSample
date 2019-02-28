using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedHDCamera
    {
        public SerializedObject serializedObject;
        public SerializedObject serializedAdditionalDataObject;

        //public SerializedProperty backgroundColor;
        
        public SerializedProperty aperture;
        public SerializedProperty shutterSpeed;
        public SerializedProperty iso;

        public SerializedProperty clearColorMode;
        public SerializedProperty backgroundColorHDR;
        public SerializedProperty renderingPath;
        public SerializedProperty clearDepth;
        public SerializedProperty volumeLayerMask;
        public SerializedProperty volumeAnchorOverride;
        public SerializedFrameSettings frameSettings;
        public CameraEditor.Settings baseCameraSettings { get; private set; }


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

            baseCameraSettings = new CameraEditor.Settings(serializedObject);
            baseCameraSettings.OnEnable();
        }

        public void Update()
        {
            serializedObject.Update();
            serializedAdditionalDataObject.Update();

            // Be sure legacy HDR option is disable on camera as it cause banding in SceneView. Yes, it is a contradiction, but well, Unity...
            // When HDR option is enabled, Unity render in FP16 then convert to 8bit with a stretch copy (this cause banding as it should be convert to sRGB (or other color appropriate color space)), then do a final shader with sRGB conversion
            // When LDR, unity render in 8bitSRGB, then do a final shader with sRGB conversion
            // What should be done is just in our Post process we convert to sRGB and store in a linear 10bit, but require C++ change...
            baseCameraSettings.HDR.boolValue = false;
        }

        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
            serializedAdditionalDataObject.ApplyModifiedProperties();
        }
    }
}
