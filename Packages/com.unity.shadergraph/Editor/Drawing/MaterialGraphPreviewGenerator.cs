using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class MaterialGraphPreviewGenerator : IDisposable
    {
        private readonly Scene m_Scene;
        static Mesh s_Quad;
        private Camera m_Camera;
        private Light Light0 { get; set; }
        private Light Light1 { get; set; }

        private Material m_CheckerboardMaterial;

        private static readonly Mesh[] s_Meshes = { null, null, null, null, null };
        private static readonly GUIContent[] s_MeshIcons = { null, null, null, null, null };
        private static readonly GUIContent[] s_LightIcons = { null, null };
        private static readonly GUIContent[] s_TimeIcons = { null, null };

        protected static GameObject CreateLight()
        {
            GameObject lightGO = EditorUtility.CreateGameObjectWithHideFlags("PreRenderLight", HideFlags.HideAndDontSave, typeof(Light));
            var light = lightGO.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.enabled = false;
            return lightGO;
        }

        public MaterialGraphPreviewGenerator()
        {
            m_Scene = EditorSceneManager.NewPreviewScene();
            var camGO = EditorUtility.CreateGameObjectWithHideFlags("Preview Scene Camera", HideFlags.HideAndDontSave, typeof(Camera));
            SceneManager.MoveGameObjectToScene(camGO, m_Scene);

            m_Camera = camGO.GetComponent<Camera>();
            EditorUtility.SetCameraAnimateMaterials(m_Camera, true);

            m_Camera.cameraType = CameraType.Preview;
            m_Camera.enabled = false;
            m_Camera.clearFlags = CameraClearFlags.Depth;
            m_Camera.fieldOfView = 15;
            m_Camera.farClipPlane = 10.0f;
            m_Camera.nearClipPlane = 2.0f;
            m_Camera.backgroundColor = new Color(49.0f / 255.0f, 49.0f / 255.0f, 49.0f / 255.0f, 1.0f);

            // Explicitly use forward rendering for all previews
            // (deferred fails when generating some static previews at editor launch; and we never want
            // vertex lit previews if that is chosen in the player settings)
            m_Camera.renderingPath = RenderingPath.Forward;
            m_Camera.useOcclusionCulling = false;
            m_Camera.scene = m_Scene;

            var l0 = CreateLight();
            SceneManager.MoveGameObjectToScene(l0, m_Scene);

            //previewScene.AddGameObject(l0);
            Light0 = l0.GetComponent<Light>();

            var l1 = CreateLight();
            SceneManager.MoveGameObjectToScene(l1, m_Scene);

            //previewScene.AddGameObject(l1);
            Light1 = l1.GetComponent<Light>();

            Light0.color = new Color(0.769f, 0.769f, 0.769f, 1); // SceneView.kSceneViewFrontLight
            Light1.transform.rotation = Quaternion.Euler(340, 218, 177);
            Light1.color = new Color(.4f, .4f, .45f, 0f) * .7f;

            m_CheckerboardMaterial = new Material(Shader.Find("Hidden/Checkerboard"));
            m_CheckerboardMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            m_CheckerboardMaterial.hideFlags = HideFlags.HideAndDontSave;

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

        public static Mesh quad
        {
            get
            {
                if (s_Quad != null)
                    return s_Quad;

                var vertices = new[]
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(1f, 1f, 0f),
                    new Vector3(1f, -1f, 0f),
                    new Vector3(-1f, 1f, 0f)
                };

                var uvs = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f)
                };

                var indices = new[] { 0, 1, 2, 1, 0, 3 };

                s_Quad = new Mesh
                {
                    vertices = vertices,
                    uv = uvs,
                    triangles = indices
                };
                s_Quad.RecalculateNormals();
                s_Quad.RecalculateBounds();

                return s_Quad;
            }
        }

        public void DoRenderPreview(RenderTexture renderTexture, Material mat, Mesh mesh, PreviewMode mode, bool allowSRP, float time, MaterialPropertyBlock properties = null)
        {
            if (mat == null || mat.shader == null)
                return;

            m_Camera.targetTexture = renderTexture;
            if (mode == PreviewMode.Preview3D)
            {
                m_Camera.transform.position = -Vector3.forward * 5;
                m_Camera.transform.rotation = Quaternion.identity;
                m_Camera.orthographic = false;
            }
            else
            {
                m_Camera.transform.position = -Vector3.forward * 2;
                m_Camera.transform.rotation = Quaternion.identity;
                m_Camera.orthographicSize = 1;
                m_Camera.orthographic = true;
            }

            m_Camera.targetTexture = renderTexture;
            var previousRenderTexure = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.black);
            Graphics.Blit(Texture2D.whiteTexture, renderTexture, m_CheckerboardMaterial);

            EditorUtility.SetCameraAnimateMaterialsTime(m_Camera, time);
            Light0.enabled = true;
            Light0.intensity = 1.0f;
            Light0.transform.rotation = Quaternion.Euler(50f, 50f, 0);
            Light1.enabled = true;
            Light1.intensity = 1.0f;
            m_Camera.clearFlags = CameraClearFlags.Depth;

            Mesh previewMesh = mesh == null ? s_Meshes[0] : mesh;

            Graphics.DrawMesh(
                mode == PreviewMode.Preview3D ? previewMesh : quad,
                mode == PreviewMode.Preview3D ? Matrix4x4.TRS(-previewMesh.bounds.center, Quaternion.identity, Vector3.one) : Matrix4x4.identity,
                mat,
                1,
                m_Camera,
                0,
                properties,
                ShadowCastingMode.Off,
                false,
                null,
                false);

            var oldAllowPipes = Unsupported.useScriptableRenderPipeline;
            Unsupported.useScriptableRenderPipeline = allowSRP;
            m_Camera.Render();
            Unsupported.useScriptableRenderPipeline = oldAllowPipes;

            RenderTexture.active = previousRenderTexure;

            Light0.enabled = false;
            Light1.enabled = false;
        }

        public void Dispose()
        {
            if (Light0 != null)
            {
                UnityEngine.Object.DestroyImmediate(Light0.gameObject);
                Light0 = null;
            }

            if (Light1 != null)
            {
                UnityEngine.Object.DestroyImmediate(Light1.gameObject);
                Light1 = null;
            }

            if (m_Camera != null)
            {
                UnityEngine.Object.DestroyImmediate(m_Camera.gameObject);
                m_Camera = null;
            }

            if (m_CheckerboardMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(m_CheckerboardMaterial);
                m_CheckerboardMaterial = null;
            }

            EditorSceneManager.ClosePreviewScene(m_Scene);
        }
    }
}
