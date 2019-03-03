using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    public class CameraSwitcher : MonoBehaviour
    {
        public Camera[] m_Cameras;

        private int m_CurrentCameraIndex = -1;
        private Camera m_OriginalCamera = null;
        private Vector3 m_OriginalCameraPosition;
        private Quaternion m_OriginalCameraRotation;
        private Camera m_CurrentCamera = null;

        GUIContent[]    m_CameraNames = null;
        int[]           m_CameraIndices = null;

        DebugUI.EnumField m_DebugEntry;

        //saved enum fields for when repainting
        int m_DebugEntryEnumIndex;

        void OnEnable()
        {
            m_OriginalCamera = GetComponent<Camera>();
            m_CurrentCamera = m_OriginalCamera;

            if (m_OriginalCamera == null)
            {
                Debug.LogError("Camera Switcher needs a Camera component attached");
                return;
            }

            m_CurrentCameraIndex = GetCameraCount() - 1;

            m_CameraNames = new GUIContent[GetCameraCount()];
            m_CameraIndices = new int[GetCameraCount()];

            for (int i = 0; i < m_Cameras.Length; ++i)
            {
                Camera cam = m_Cameras[i];
                if (cam != null)
                {
                    m_CameraNames[i] = new GUIContent(cam.name);
                }
                else
                {
                    m_CameraNames[i] = new GUIContent("null");
                }
                m_CameraIndices[i] = i;
            }

            m_CameraNames[GetCameraCount() - 1] = new GUIContent("Original Camera");
            m_CameraIndices[GetCameraCount() - 1] = GetCameraCount() - 1;

            m_DebugEntry = new DebugUI.EnumField { displayName = "Camera Switcher", getter = () => m_CurrentCameraIndex, setter = value => SetCameraIndex(value), enumNames = m_CameraNames, enumValues = m_CameraIndices, getIndex = () => m_DebugEntryEnumIndex, setIndex = value => m_DebugEntryEnumIndex = value };
            var panel = DebugManager.instance.GetPanel("Camera", true);
            panel.children.Add(m_DebugEntry);
        }

        void OnDisable()
        {
            if (m_DebugEntry != null && m_DebugEntry.panel != null)
            {
                var panel = m_DebugEntry.panel;
                panel.children.Remove(m_DebugEntry);
            }
        }

        int GetCameraCount()
        {
            return m_Cameras.Length + 1; // We need +1 for handling the original camera.
        }

        Camera GetNextCamera()
        {
            if (m_CurrentCameraIndex == m_Cameras.Length)
                return m_OriginalCamera;
            else
                return m_Cameras[m_CurrentCameraIndex];
        }

        void SetCameraIndex(int index)
        {
            if (index > 0 || index < GetCameraCount())
            {
                m_CurrentCameraIndex = index;

                if (m_CurrentCamera == m_OriginalCamera)
                {
                    m_OriginalCameraPosition = m_OriginalCamera.transform.position;
                    m_OriginalCameraRotation = m_OriginalCamera.transform.rotation;
                }

                m_CurrentCamera = GetNextCamera();

                if (m_CurrentCamera != null)
                {
                    // If we witch back to the original camera, put back the transform in it.
                    if (m_CurrentCamera == m_OriginalCamera)
                    {
                        m_OriginalCamera.transform.position = m_OriginalCameraPosition;
                        m_OriginalCamera.transform.rotation = m_OriginalCameraRotation;
                    }

                    transform.position = m_CurrentCamera.transform.position;
                    transform.rotation = m_CurrentCamera.transform.rotation;
                }
            }
        }
    }
}
