using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class PreviewSceneResources : IDisposable
    {
        readonly Scene m_Scene;
        Camera m_Camera;
        public Light light0 { get; private set; }
        public Light light1 { get; private set; }

        Material m_CheckerboardMaterial;
        Material m_BlitNoAlphaMaterial;

        static readonly Mesh[] s_Meshes = { null, null, null, null, null };
        static readonly GUIContent[] s_MeshIcons = { null, null, null, null, null };
        static readonly GUIContent[] s_LightIcons = { null, null };
        static readonly GUIContent[] s_TimeIcons = { null, null };

        static GameObject CreateLight()
        {
            GameObject lightGO = EditorUtility.CreateGameObjectWithHideFlags("PreRenderLight", HideFlags.HideAndDontSave, typeof(Light));
            var light = lightGO.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.enabled = false;
            return lightGO;
        }

        public PreviewSceneResources()
        {
            m_Scene = EditorSceneManager.NewPreviewScene();
            var camGO = EditorUtility.CreateGameObjectWithHideFlags("Preview Scene Camera", HideFlags.HideAndDontSave, typeof(Camera));
            SceneManager.MoveGameObjectToScene(camGO, m_Scene);

            m_Camera = camGO.GetComponent<Camera>();
            EditorUtility.SetCameraAnimateMaterials(camera, true);

            camera.cameraType = CameraType.Preview;
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.fieldOfView = 15;
            camera.farClipPlane = 10.0f;
            camera.nearClipPlane = 2.0f;
            camera.backgroundColor = new Color(49.0f / 255.0f, 49.0f / 255.0f, 49.0f / 255.0f, 1.0f);

            // Explicitly use forward rendering for all previews
            // (deferred fails when generating some static previews at editor launch; and we never want
            // vertex lit previews if that is chosen in the player settings)
            camera.renderingPath = RenderingPath.Forward;
            camera.useOcclusionCulling = false;
            camera.scene = m_Scene;

            var l0 = CreateLight();
            SceneManager.MoveGameObjectToScene(l0, m_Scene);

            //previewScene.AddGameObject(l0);
            light0 = l0.GetComponent<Light>();

            var l1 = CreateLight();
            SceneManager.MoveGameObjectToScene(l1, m_Scene);

            //previewScene.AddGameObject(l1);
            light1 = l1.GetComponent<Light>();

            light0.color = new Color(0.769f, 0.769f, 0.769f, 1); // SceneView.kSceneViewFrontLight
            light1.transform.rotation = Quaternion.Euler(340, 218, 177);
            light1.color = new Color(.4f, .4f, .45f, 0f) * .7f;

            m_CheckerboardMaterial = new Material(Shader.Find("Hidden/Checkerboard"));
            m_BlitNoAlphaMaterial = new Material(Shader.Find("Hidden/BlitNoAlpha"));
            checkerboardMaterial.hideFlags = HideFlags.HideAndDontSave;
            blitNoAlphaMaterial.hideFlags = HideFlags.HideAndDontSave;

            if (s_Meshes[0] == null)
            {
                var handleGo = (GameObject)EditorGUIUtility.LoadRequired("Previews/PreviewMaterials.fbx");

                // @TODO: temp workaround to make it not render in the scene
                handleGo.SetActive(false);
                foreach (Transform t in handleGo.transform)
                {
                    var meshFilter = t.GetComponent<MeshFilter>();
                    switch (t.name)
                    {
                        case "sphere":
                            s_Meshes[0] = meshFilter.sharedMesh;
                            break;
                        case "cube":
                            s_Meshes[1] = meshFilter.sharedMesh;
                            break;
                        case "cylinder":
                            s_Meshes[2] = meshFilter.sharedMesh;
                            break;
                        case "torus":
                            s_Meshes[3] = meshFilter.sharedMesh;
                            break;
                        default:
                            Debug.LogWarning("Something is wrong, weird object found: " + t.name);
                            break;
                    }
                }

                s_MeshIcons[0] = EditorGUIUtility.IconContent("PreMatSphere");
                s_MeshIcons[1] = EditorGUIUtility.IconContent("PreMatCube");
                s_MeshIcons[2] = EditorGUIUtility.IconContent("PreMatCylinder");
                s_MeshIcons[3] = EditorGUIUtility.IconContent("PreMatTorus");
                s_MeshIcons[4] = EditorGUIUtility.IconContent("PreMatQuad");

                s_LightIcons[0] = EditorGUIUtility.IconContent("PreMatLight0");
                s_LightIcons[1] = EditorGUIUtility.IconContent("PreMatLight1");

                s_TimeIcons[0] = EditorGUIUtility.IconContent("PlayButton");
                s_TimeIcons[1] = EditorGUIUtility.IconContent("PauseButton");

                Mesh quadMesh = Resources.GetBuiltinResource(typeof(Mesh), "Quad.fbx") as Mesh;
                s_Meshes[4] = quadMesh;
            }
        }

        public Mesh sphere
        {
            get { return s_Meshes[0]; }
        }

        public Mesh quad
        {
            get { return s_Meshes[4]; }
        }

        public Material checkerboardMaterial
        {
            get { return m_CheckerboardMaterial; }
        }

        public Material blitNoAlphaMaterial
        {
            get { return m_BlitNoAlphaMaterial; }
        }

        public Camera camera
        {
            get { return m_Camera; }
        }

        public void Dispose()
        {
            if (light0 != null)
            {
                UnityEngine.Object.DestroyImmediate(light0.gameObject);
                light0 = null;
            }

            if (light1 != null)
            {
                UnityEngine.Object.DestroyImmediate(light1.gameObject);
                light1 = null;
            }

            if (camera != null)
            {
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
                m_Camera = null;
            }

            if (checkerboardMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(checkerboardMaterial, true);
                m_CheckerboardMaterial = null;
            }
            if (blitNoAlphaMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(blitNoAlphaMaterial, true);
                m_BlitNoAlphaMaterial = null;
            }

            EditorSceneManager.ClosePreviewScene(m_Scene);
        }
    }
}
