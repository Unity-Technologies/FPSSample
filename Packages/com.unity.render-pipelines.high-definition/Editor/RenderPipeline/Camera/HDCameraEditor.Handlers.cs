using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;

    partial class HDCameraEditor
    {
        void OnSceneGUI()
        {
            var c = (Camera)target;

            if (!CameraEditorUtils.IsViewPortRectValidToRender(c.rect))
                return;

            SceneViewOverlay_Window(_.GetContent("Camera Preview"), OnOverlayGUI, -100, target);

            UnityEditor.CameraEditorUtils.HandleFrustum(c, c.GetInstanceID());
        }

        void OnOverlayGUI(Object target, SceneView sceneView)
        {
            CameraEditorUtils.DrawCameraSceneViewOverlay(target, sceneView, InitializePreviewCamera);
        }

        Camera InitializePreviewCamera(Camera c, Vector2 previewSize)
        {
            m_PreviewCamera.CopyFrom(c);
            EditorUtility.CopySerialized(c, m_PreviewCamera);
            var cameraData = c.GetComponent<HDAdditionalCameraData>();
            EditorUtility.CopySerialized(cameraData, m_PreviewAdditionalCameraData);
            var layer = c.GetComponent<PostProcessLayer>() ?? ComponentSingleton<PostProcessLayer>.instance;
            EditorUtility.CopySerialized(layer, m_PreviewPostProcessLayer);

            var previewTexture = GetPreviewTextureWithSize((int)previewSize.x, (int)previewSize.y);
            m_PreviewCamera.targetTexture = previewTexture;
            m_PreviewCamera.pixelRect = new Rect(0, 0, previewSize.x, previewSize.y);

            return m_PreviewCamera;
        }

        static Type k_SceneViewOverlay_WindowFunction = Type.GetType("UnityEditor.SceneViewOverlay+WindowFunction,UnityEditor");
        static Type k_SceneViewOverlay_WindowDisplayOption = Type.GetType("UnityEditor.SceneViewOverlay+WindowDisplayOption,UnityEditor");
        static MethodInfo k_SceneViewOverlay_Window = Type.GetType("UnityEditor.SceneViewOverlay,UnityEditor")
            .GetMethod(
                "Window",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                CallingConventions.Any,
                new[] { typeof(GUIContent), k_SceneViewOverlay_WindowFunction, typeof(int), typeof(Object), k_SceneViewOverlay_WindowDisplayOption },
                null);
        static void SceneViewOverlay_Window(GUIContent title, Action<Object, SceneView> sceneViewFunc, int order, Object target)
        {
            k_SceneViewOverlay_Window.Invoke(null, new[]
            {
                title, DelegateUtility.Cast(sceneViewFunc, k_SceneViewOverlay_WindowFunction),
                order,
                target,
                Enum.ToObject(k_SceneViewOverlay_WindowDisplayOption, 1)
            });
        }
    }
}
