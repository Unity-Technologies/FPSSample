using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditorForRenderPipeline(typeof(Cubemap), typeof(HDRenderPipelineAsset))]
    class HDCubemapInspector : Editor
    {
        private enum NavMode
        {
            None = 0,
            Zooming = 1,
            Rotating = 2
        }

        static GUIContent s_MipMapLow, s_MipMapHigh, s_ExposureLow;
        static GUIStyle s_PreLabel;
        static Mesh s_SphereMesh;

        static Mesh sphereMesh
        {
            get { return s_SphereMesh ?? (s_SphereMesh = Resources.GetBuiltinResource(typeof(Mesh), "New-Sphere.fbx") as Mesh); }
        }

        Material m_ReflectiveMaterial;
        PreviewRenderUtility m_PreviewUtility;
        float m_CameraPhi = 0.75f;
        float m_CameraTheta = 0.5f;
        float m_CameraDistance = 2.0f;
        NavMode m_NavMode = NavMode.None;
        Vector2 m_PreviousMousePosition = Vector2.zero;

        public float previewExposure = 0f;
        public float mipLevelPreview = 0f;

        void Awake()
        {
            m_ReflectiveMaterial = new Material(Shader.Find("Debug/ReflectionProbePreview"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        void OnEnable()
        {
            if (m_PreviewUtility == null)
                InitPreview();

            m_ReflectiveMaterial.SetTexture("_Cubemap", target as Texture);
        }

        void OnDisable()
        {
            if (m_PreviewUtility != null)
                m_PreviewUtility.Cleanup();
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (m_ReflectiveMaterial != null)
            {
                m_ReflectiveMaterial.SetFloat("_Exposure", previewExposure);
                m_ReflectiveMaterial.SetFloat("_MipLevel", mipLevelPreview);
            }

            if (m_PreviewUtility == null)
                InitPreview();

            UpdateCamera();

            m_PreviewUtility.BeginPreview(r, GUIStyle.none);
            m_PreviewUtility.DrawMesh(sphereMesh, Matrix4x4.identity, m_ReflectiveMaterial, 0);
            m_PreviewUtility.camera.Render();
            m_PreviewUtility.EndAndDrawPreview(r);

            if (Event.current.type != EventType.Repaint)
            {
                if (HandleMouse(r))
                    Repaint();
            }
        }

        public override void OnPreviewSettings()
        {
            if (s_MipMapLow == null)
                InitIcons();

            var mipmapCount = 0;
            var cubemap = target as Cubemap;
            var rt = target as RenderTexture;
            if (cubemap != null)
                mipmapCount = cubemap.mipmapCount;
            if (rt != null)
                mipmapCount = rt.useMipMap
                    ? (int)(Mathf.Log(Mathf.Max(rt.width, rt.height)) / Mathf.Log(2))
                    : 1;

            GUI.enabled = true;

            GUILayout.Box(s_ExposureLow, s_PreLabel, GUILayout.MaxWidth(20));
            GUI.changed = false;
            previewExposure = GUILayout.HorizontalSlider(previewExposure, -10f, 10f, GUILayout.MaxWidth(80));
            GUILayout.Space(5);
            GUILayout.Box(s_MipMapHigh, s_PreLabel, GUILayout.MaxWidth(20));
            GUI.changed = false;
            mipLevelPreview = GUILayout.HorizontalSlider(mipLevelPreview, 0, mipmapCount, GUILayout.MaxWidth(80));
            GUILayout.Box(s_MipMapLow, s_PreLabel, GUILayout.MaxWidth(20));
        }

        void InitPreview()
        {
            if (m_PreviewUtility != null)
                m_PreviewUtility.Cleanup();
            m_PreviewUtility = new PreviewRenderUtility(false, true);
            m_PreviewUtility.cameraFieldOfView = 50.0f;
            m_PreviewUtility.camera.nearClipPlane = 0.01f;
            m_PreviewUtility.camera.farClipPlane = 20.0f;
            m_PreviewUtility.camera.transform.position = new Vector3(0, 0, 2);
            m_PreviewUtility.camera.transform.LookAt(Vector3.zero);
        }

        bool HandleMouse(Rect Viewport)
        {
            var result = false;

            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 0)
                    m_NavMode = NavMode.Rotating;
                else if (Event.current.button == 1)
                    m_NavMode = NavMode.Zooming;

                m_PreviousMousePosition = Event.current.mousePosition;
                result = true;
            }

            if (Event.current.type == EventType.MouseUp || Event.current.rawType == EventType.MouseUp)
                m_NavMode = NavMode.None;

            if (m_NavMode != NavMode.None)
            {
                var mouseDelta = Event.current.mousePosition - m_PreviousMousePosition;
                switch (m_NavMode)
                {
                    case NavMode.Rotating:
                        m_CameraTheta = (m_CameraTheta - mouseDelta.x * 0.003f) % (Mathf.PI * 2);
                        m_CameraPhi = Mathf.Clamp(m_CameraPhi - mouseDelta.y * 0.003f, 0.2f, Mathf.PI - 0.2f);
                        break;
                    case NavMode.Zooming:
                        m_CameraDistance = Mathf.Clamp(mouseDelta.y * 0.01f + m_CameraDistance, 1, 10);
                        break;
                }
                result = true;
            }

            m_PreviousMousePosition = Event.current.mousePosition;
            return result;
        }

        void UpdateCamera()
        {
            var pos = new Vector3(Mathf.Sin(m_CameraPhi) * Mathf.Cos(m_CameraTheta), Mathf.Cos(m_CameraPhi), Mathf.Sin(m_CameraPhi) * Mathf.Sin(m_CameraTheta)) * m_CameraDistance;
            m_PreviewUtility.camera.transform.position = pos;
            m_PreviewUtility.camera.transform.LookAt(Vector3.zero);
        }

        static void InitIcons()
        {
            s_MipMapLow = EditorGUIUtility.IconContent("PreTextureMipMapLow");
            s_MipMapHigh = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
            s_ExposureLow = EditorGUIUtility.IconContent("SceneViewLighting");
            s_PreLabel = "preLabel";
        }
    }
}
