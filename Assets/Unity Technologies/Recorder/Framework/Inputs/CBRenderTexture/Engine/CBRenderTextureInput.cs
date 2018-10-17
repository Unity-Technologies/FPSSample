using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Rendering;

namespace UnityEngine.Recorder.Input
{
    public class CBRenderTextureInput : BaseRenderTextureInput
    {
        struct CanvasBackup
        {
            public Camera camera;
            public Canvas canvas;
        }

        bool m_ModifiedResolution;
        Shader m_shCopy;
        Material m_CopyMaterial;
        TextureFlipper m_VFlipper = new TextureFlipper();
        Mesh m_quad;
        CommandBuffer m_cbCopyFB;
        //CommandBuffer m_cbCopyGB;
        //CommandBuffer m_cbClearGB;
        //CommandBuffer m_cbCopyVelocity;
        Camera m_Camera;
        bool m_cameraChanged;
        Camera m_UICamera;

        CanvasBackup[] m_CanvasBackups;

        public CBRenderTextureInputSettings cbSettings
        {
            get { return (CBRenderTextureInputSettings)settings; }
        }

        public Camera targetCamera
        {
            get { return m_Camera; }

            set
            {
                if (m_Camera != value)
                {
                    ReleaseCamera();
                    m_Camera = value;
                    m_cameraChanged = true;
                }
            }
        }

        public Shader copyShader
        {
            get
            {
                if (m_shCopy == null)
                {
                    m_shCopy = Shader.Find("Hidden/Recorder/Inputs/CBRenderTexture/CopyFB");
                }
                return m_shCopy;
            }

            set { m_shCopy = value; }
        }

        public Material copyMaterial
        {
            get
            {
                if (m_CopyMaterial == null)
                {
                    m_CopyMaterial = new Material(copyShader);
                    copyMaterial.EnableKeyword("OFFSCREEN");
                    if (cbSettings.m_AllowTransparency)
                        m_CopyMaterial.EnableKeyword("TRANSPARENCY_ON");
                }
                return m_CopyMaterial;
            }
        }

        public override void BeginRecording(RecordingSession session)
        {
            if (cbSettings.m_FlipFinalOutput)
                m_VFlipper = new TextureFlipper();

            m_quad = CreateFullscreenQuad();
            switch (cbSettings.source)
            {
                case EImageSource.ActiveCameras:
                case EImageSource.MainCamera:
                case EImageSource.TaggedCamera:
                {
                    int screenWidth = Screen.width;
                    int screenHeight = Screen.height;
#if UNITY_EDITOR
                    switch (cbSettings.m_OutputSize)
                    {
                        case EImageDimension.Window:
                        {
                            GameViewSize.GetGameRenderSize(out screenWidth, out screenHeight);
                            outputWidth = screenWidth;
                            outputHeight = screenHeight;

                            if (cbSettings.m_ForceEvenSize)
                            {
                                outputWidth = (outputWidth + 1) & ~1;
                                outputHeight = (outputHeight + 1) & ~1;
                            }

                            break;
                        }

                        default:
                        {
                            outputHeight = (int)cbSettings.m_OutputSize;
                            outputWidth = (int)(outputHeight * AspectRatioHelper.GetRealAR(cbSettings.m_AspectRatio));

                            if (cbSettings.m_ForceEvenSize)
                            {
                                outputWidth = (outputWidth + 1) & ~1;
                                outputHeight = (outputHeight + 1) & ~1;
                            }

                            var size = GameViewSize.SetCustomSize(outputWidth, outputHeight);
                            if (size == null)
                                size = GameViewSize.AddSize(outputWidth, outputHeight);

                            if (GameViewSize.m_ModifiedResolutionCount == 0)
                                GameViewSize.BackupCurrentSize();
                            else
                            {
                                if (size != GameViewSize.currentSize)
                                {
                                    Debug.LogError("Requestion a resultion change while a recorder's input has already requested one! Undefined behaviour.");
                                }
                            }
                            GameViewSize.m_ModifiedResolutionCount++;
                            m_ModifiedResolution = true;
                            GameViewSize.SelectSize(size);
                            break;
                        }
                    }
#endif
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (cbSettings.m_CaptureUI)
            {
                var uiGO = new GameObject();
                uiGO.name = "UICamera";
                uiGO.transform.parent = session.m_RecorderGO.transform;

                m_UICamera = uiGO.AddComponent<Camera>();
                m_UICamera.cullingMask = 1 << 5;
                m_UICamera.clearFlags = CameraClearFlags.Depth;
                m_UICamera.renderingPath = RenderingPath.DeferredShading;
                m_UICamera.targetTexture = outputRT;
                m_UICamera.enabled = false;
            }
        }

        public override void NewFrameStarting(RecordingSession session)
        {
            switch (cbSettings.source)
            {
                case EImageSource.ActiveCameras:
                {
                    if (targetCamera == null)
                    {
                        var displayGO = new GameObject();
                        displayGO.name = "CameraHostGO-" + displayGO.GetInstanceID();
                        displayGO.transform.parent = session.m_RecorderGO.transform;
                        var camera = displayGO.AddComponent<Camera>();
                        camera.clearFlags = CameraClearFlags.Nothing;
                        camera.cullingMask = 0;
                        camera.renderingPath = RenderingPath.DeferredShading;
                        camera.targetDisplay = 0;
                        camera.rect = new Rect(0, 0, 1, 1);
                        camera.depth = float.MaxValue;

                        targetCamera = camera;
                    }
                    break;
                }

                case EImageSource.MainCamera:
                {
                    if (targetCamera != Camera.main )
                    {
                        targetCamera = Camera.main;
                        m_cameraChanged = true;
                    }
                    break;
                }
                case EImageSource.TaggedCamera:
                {
                    var tag = (settings as CBRenderTextureInputSettings).m_CameraTag;

                    if (targetCamera == null || targetCamera.gameObject.tag != tag )
                    {
                        try
                        {
                            var cams = GameObject.FindGameObjectsWithTag(tag);
                            if (cams.Length > 0)
                                Debug.LogWarning("More than one camera has the requested target tag:" + tag);
                            targetCamera = cams[0].transform.GetComponent<Camera>();
                            
                        }
                        catch (UnityException)
                        {
                            Debug.LogWarning("No camera has the requested target tag:" + tag);
                            targetCamera = null;
                        }
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var newTexture = PrepFrameRenderTexture();

            // initialize command buffer
            if (m_Camera != null && (m_cameraChanged || newTexture))
            {
                if (m_cbCopyFB != null)
                {
                    m_Camera.RemoveCommandBuffer(CameraEvent.AfterEverything, m_cbCopyFB);
                    m_cbCopyFB.Release();
                }

                var tid = Shader.PropertyToID("_TmpFrameBuffer");
                m_cbCopyFB = new CommandBuffer { name = "Recorder: copy frame buffer" };
                m_cbCopyFB.GetTemporaryRT(tid, -1, -1, 0, FilterMode.Bilinear);
                m_cbCopyFB.Blit(BuiltinRenderTextureType.CurrentActive, tid);
                m_cbCopyFB.SetRenderTarget(outputRT);
                m_cbCopyFB.DrawMesh(m_quad, Matrix4x4.identity, copyMaterial, 0, 0);
                m_cbCopyFB.ReleaseTemporaryRT(tid);
                m_Camera.AddCommandBuffer(CameraEvent.AfterEverything, m_cbCopyFB);

                m_cameraChanged = false;
            }

            if (Math.Abs(1-targetCamera.rect.width) > float.Epsilon || Math.Abs(1 - targetCamera.rect.height) > float.Epsilon)
            {
                Debug.LogWarning( string.Format("Recording output of camera '{0}' who's rectangle does not cover the viewport: resulting image will be up-sampled with associated quality degradation!", targetCamera.gameObject.name));
            }
        }

        public override void NewFrameReady(RecordingSession session)
        {
            if (cbSettings.m_CaptureUI)
            {
                // Find canvases
                var canvases = Object.FindObjectsOfType<Canvas>();
                if (m_CanvasBackups == null || m_CanvasBackups.Length != canvases.Length)
                    m_CanvasBackups = new CanvasBackup[canvases.Length];

                // Hookup canvase to UI camera
                for (var i = 0; i < canvases.Length; i++)
                {
                    var canvas = canvases[i];
                    if (canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        m_CanvasBackups[i].camera = canvas.worldCamera;
                        m_CanvasBackups[i].canvas = canvas;
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera = m_UICamera;
                    }
                    else
                    {
                        // Mark this canvas as null so we can skip it when restoring.
                        // The array might contain invalid data from a previous frame.
                        m_CanvasBackups[i].canvas = null;
                    }
                }

                m_UICamera.Render();

                // Restore canvas settings
                for (var i = 0; i < m_CanvasBackups.Length; i++)
                {
                    // Skip those canvases that are not roots canvases or are 
                    // not using ScreenSpaceOverlay as a render mode.
                    if (m_CanvasBackups[i].canvas == null)
                        continue;
                        
                    m_CanvasBackups[i].canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    m_CanvasBackups[i].canvas.worldCamera = m_CanvasBackups[i].camera;
                }
            }

            if( cbSettings.m_FlipFinalOutput )
                m_VFlipper.Flip(outputRT);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseCamera();
                UnityHelpers.Destroy(m_UICamera);
#if UNITY_EDITOR
                if (m_ModifiedResolution)
                {
                    GameViewSize.m_ModifiedResolutionCount --;
                    if(GameViewSize.m_ModifiedResolutionCount == 0 )
                        GameViewSize.RestoreSize();
                }
#endif
                if( m_VFlipper!=null )
                    m_VFlipper.Dispose();
            }

            base.Dispose(disposing);
        }

        protected virtual void ReleaseCamera()
        {
            if (m_cbCopyFB != null)
            {
                if (m_Camera != null)
                    m_Camera.RemoveCommandBuffer(CameraEvent.AfterEverything, m_cbCopyFB);

                m_cbCopyFB.Release();
                m_cbCopyFB = null;
            }

            if (m_CopyMaterial != null)
                UnityHelpers.Destroy(m_CopyMaterial);
        }

        bool PrepFrameRenderTexture()
        {
            if (outputRT != null)
            {
                if (outputRT.IsCreated() && outputRT.width == outputWidth && outputRT.height == outputHeight)
                {
                    return false;
                }

                ReleaseBuffer();
            }

            outputRT = new RenderTexture(outputWidth, outputHeight, 0, RenderTextureFormat.ARGB32)
            {
                wrapMode = TextureWrapMode.Repeat
            };
            outputRT.Create();
            if (m_UICamera != null)
            {
                m_UICamera.targetTexture = outputRT;
            }

            return true;
        }

        public static Mesh CreateFullscreenQuad()
        {
            var vertices = new Vector3[4]
            {
                new Vector3(1.0f, 1.0f, 0.0f),
                new Vector3(-1.0f, 1.0f, 0.0f),
                new Vector3(-1.0f, -1.0f, 0.0f),
                new Vector3(1.0f, -1.0f, 0.0f),
            };
            var indices = new[] { 0, 1, 2, 2, 3, 0 };

            var r = new Mesh
            {
                vertices = vertices,
                triangles = indices
            };
            return r;
        }
    }
}
