using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.PostProcessing
{
    using SceneManagement;
    using UnityObject = UnityEngine.Object;

    /// <summary>
    /// A set of runtime utilities used by the post-processing stack.
    /// </summary>
    public static class RuntimeUtilities
    {
        #region Textures

        static Texture2D m_WhiteTexture;

        /// <summary>
        /// A 1x1 white texture.
        /// </summary>
        /// <remarks>
        /// This texture is only created once and recycled afterward. You shouldn't modify it.
        /// </remarks>
        public static Texture2D whiteTexture
        {
            get
            {
                if (m_WhiteTexture == null)
                {
                    m_WhiteTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false) { name = "White Texture" };
                    m_WhiteTexture.SetPixel(0, 0, Color.white);
                    m_WhiteTexture.Apply();
                }

                return m_WhiteTexture;
            }
        }

        static Texture3D m_WhiteTexture3D;

        /// <summary>
        /// A 1x1x1 white texture.
        /// </summary>
        /// <remarks>
        /// This texture is only created once and recycled afterward. You shouldn't modify it.
        /// </remarks>
        public static Texture3D whiteTexture3D
        {
            get
            {
                if (m_WhiteTexture3D == null)
                {
                    m_WhiteTexture3D = new Texture3D(1, 1, 1, TextureFormat.ARGB32, false) { name = "White Texture 3D" };
                    m_WhiteTexture3D.SetPixels(new Color[] { Color.white });
                    m_WhiteTexture3D.Apply();
                }

                return m_WhiteTexture3D;
            }
        }

        static Texture2D m_BlackTexture;

        /// <summary>
        /// A 1x1 black texture.
        /// </summary>
        /// <remarks>
        /// This texture is only created once and recycled afterward. You shouldn't modify it.
        /// </remarks>
        public static Texture2D blackTexture
        {
            get
            {
                if (m_BlackTexture == null)
                {
                    m_BlackTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false) { name = "Black Texture" };
                    m_BlackTexture.SetPixel(0, 0, Color.black);
                    m_BlackTexture.Apply();
                }

                return m_BlackTexture;
            }
        }

        static Texture3D m_BlackTexture3D;

        /// <summary>
        /// A 1x1x1 black texture.
        /// </summary>
        /// <remarks>
        /// This texture is only created once and recycled afterward. You shouldn't modify it.
        /// </remarks>
        public static Texture3D blackTexture3D
        {
            get
            {
                if (m_BlackTexture3D == null)
                {
                    m_BlackTexture3D = new Texture3D(1, 1, 1, TextureFormat.ARGB32, false) { name = "Black Texture 3D" };
                    m_BlackTexture3D.SetPixels(new Color[] { Color.black });
                    m_BlackTexture3D.Apply();
                }

                return m_BlackTexture3D;
            }
        }

        static Texture2D m_TransparentTexture;

        /// <summary>
        /// A 1x1 transparent texture.
        /// </summary>
        /// <remarks>
        /// This texture is only created once and recycled afterward. You shouldn't modify it.
        /// </remarks>
        public static Texture2D transparentTexture
        {
            get
            {
                if (m_TransparentTexture == null)
                {
                    m_TransparentTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false) { name = "Transparent Texture" };
                    m_TransparentTexture.SetPixel(0, 0, Color.clear);
                    m_TransparentTexture.Apply();
                }

                return m_TransparentTexture;
            }
        }

        static Texture3D m_TransparentTexture3D;

        /// <summary>
        /// A 1x1x1 transparent texture.
        /// </summary>
        /// <remarks>
        /// This texture is only created once and recycled afterward. You shouldn't modify it.
        /// </remarks>
        public static Texture3D transparentTexture3D
        {
            get
            {
                if (m_TransparentTexture3D == null)
                {
                    m_TransparentTexture3D = new Texture3D(1, 1, 1, TextureFormat.ARGB32, false) { name = "Transparent Texture 3D" };
                    m_TransparentTexture3D.SetPixels(new Color[] { Color.clear });
                    m_TransparentTexture3D.Apply();
                }

                return m_TransparentTexture3D;
            }
        }

        static Dictionary<int, Texture2D> m_LutStrips = new Dictionary<int, Texture2D>();

        /// <summary>
        /// Gets a 2D lookup table for color grading use. Its size will be <c>width = height * height</c>.
        /// </summary>
        /// <param name="size">The height of the lookup table</param>
        /// <returns>A 2D lookup table</returns>
        /// <remarks>
        /// Lookup tables are recycled and only created once per size. You shouldn't modify them.
        /// </remarks>
        public static Texture2D GetLutStrip(int size)
        {
            Texture2D texture;
            if (!m_LutStrips.TryGetValue(size, out texture))
            {
                int width = size * size;
                int height = size;
                var pixels = new Color[width * height];
                float inv = 1f / (size - 1f);

                for (int z = 0; z < size; z++)
                {
                    var offset = z * size;
                    var b = z * inv;

                    for (int y = 0; y < size; y++)
                    {
                        var g = y * inv;

                        for (int x = 0; x < size; x++)
                        {
                            var r = x * inv;
                            pixels[y * width + offset + x] = new Color(r, g, b);
                        }
                    }
                }

                var format = TextureFormat.RGBAHalf;
                if (!format.IsSupported())
                    format = TextureFormat.ARGB32;
                    
                texture = new Texture2D(size * size, size, format, false, true)
                {
                    name = "Strip Lut" + size,
                    hideFlags = HideFlags.DontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0
                };
                texture.SetPixels(pixels);
                texture.Apply();
                m_LutStrips.Add(size, texture);
            }

            return texture;
        }

        #endregion

        #region Rendering

        internal static PostProcessResources s_Resources;

        static Mesh s_FullscreenTriangle;

        /// <summary>
        /// A fullscreen triangle mesh.
        /// </summary>
        public static Mesh fullscreenTriangle
        {
            get
            {
                if (s_FullscreenTriangle != null)
                    return s_FullscreenTriangle;

                s_FullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };

                // Because we have to support older platforms (GLES2/3, DX9 etc) we can't do all of
                // this directly in the vertex shader using vertex ids :(
                s_FullscreenTriangle.SetVertices(new List<Vector3>
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(-1f,  3f, 0f),
                    new Vector3( 3f, -1f, 0f)
                });
                s_FullscreenTriangle.SetIndices(new [] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
                s_FullscreenTriangle.UploadMeshData(false);

                return s_FullscreenTriangle;
            }
        }

        static Material s_CopyStdMaterial;

        /// <summary>
        /// A simple copy material to use with the builtin pipelines.
        /// </summary>
        public static Material copyStdMaterial
        {
            get
            {
                if (s_CopyStdMaterial != null)
                    return s_CopyStdMaterial;

                Assert.IsNotNull(s_Resources);
                var shader = s_Resources.shaders.copyStd;
                s_CopyStdMaterial = new Material(shader)
                {
                    name = "PostProcess - CopyStd",
                    hideFlags = HideFlags.HideAndDontSave
                };

                return s_CopyStdMaterial;
            }
        }

        static Material s_CopyStdFromDoubleWideMaterial;

        /// <summary>
        /// A double-wide copy material to use with VR and the builtin pipelines.
        /// </summary>
        public static Material copyStdFromDoubleWideMaterial
        {
            get
            {
                if (s_CopyStdFromDoubleWideMaterial != null)
                    return s_CopyStdFromDoubleWideMaterial;

                Assert.IsNotNull(s_Resources);
                var shader = s_Resources.shaders.copyStdFromDoubleWide;
                s_CopyStdFromDoubleWideMaterial = new Material(shader)
                {
                    name = "PostProcess - CopyStdFromDoubleWide",
                    hideFlags = HideFlags.HideAndDontSave
                };

                return s_CopyStdFromDoubleWideMaterial;
            }
        }

        static Material s_CopyMaterial;

        /// <summary>
        /// A simple copy material independent from the rendering pipeline.
        /// </summary>
        public static Material copyMaterial
        {
            get
            {
                if (s_CopyMaterial != null)
                    return s_CopyMaterial;

                Assert.IsNotNull(s_Resources);
                var shader = s_Resources.shaders.copy;
                s_CopyMaterial = new Material(shader)
                {
                    name = "PostProcess - Copy",
                    hideFlags = HideFlags.HideAndDontSave
                };

                return s_CopyMaterial;
            }
        }

        static Material s_CopyFromTexArrayMaterial;

        /// <summary>
        /// A copy material with a texture array slice as a source for the builtin pipelines.
        /// </summary>
        public static Material copyFromTexArrayMaterial
        {
            get
            {
                if (s_CopyFromTexArrayMaterial != null)
                    return s_CopyFromTexArrayMaterial;

                Assert.IsNotNull(s_Resources);
                var shader = s_Resources.shaders.copyStdFromTexArray;
                s_CopyFromTexArrayMaterial = new Material(shader)
                {
                    name = "PostProcess - CopyFromTexArray",
                    hideFlags = HideFlags.HideAndDontSave
                };

                return s_CopyFromTexArrayMaterial;
            }
        }

        static PropertySheet s_CopySheet;

        /// <summary>
        /// A pre-configured <see cref="PropertySheet"/> for <see cref="copyMaterial"/>.
        /// </summary>
        public static PropertySheet copySheet
        {
            get
            {
                if (s_CopySheet == null)
                    s_CopySheet = new PropertySheet(copyMaterial);

                return s_CopySheet;
            }
        }

        static PropertySheet s_CopyFromTexArraySheet;

        /// <summary>
        /// A pre-configured <see cref="PropertySheet"/> for <see cref="copyFromTexArrayMaterial"/>.
        /// </summary>
        public static PropertySheet copyFromTexArraySheet
        {
            get
            {
                if (s_CopyFromTexArraySheet == null)
                    s_CopyFromTexArraySheet = new PropertySheet(copyFromTexArrayMaterial);

                return s_CopyFromTexArraySheet;
            }
        }

        /// <summary>
        /// Sets the current render target using specified <see cref="RenderBufferLoadAction"/>.
        /// </summary>
        /// <param name="cmd">The command buffer to set the render target on</param>
        /// <param name="rt">The render target to set</param>
        /// <param name="loadAction">The load action</param>
        /// <param name="storeAction">The store action</param>
        /// <remarks>
        /// <see cref="RenderBufferLoadAction"/> are only used on Unity 2018.2 or newer.
        /// </remarks>
        public static void SetRenderTargetWithLoadStoreAction(this CommandBuffer cmd, RenderTargetIdentifier rt, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction)
        {
            #if UNITY_2018_2_OR_NEWER
            cmd.SetRenderTarget(rt, loadAction, storeAction);
            #else
            cmd.SetRenderTarget(rt);
            #endif
        }

        /// <summary>
        /// Sets the current render target and its depth using specified <see cref="RenderBufferLoadAction"/>.
        /// </summary>
        /// <param name="cmd">The command buffer to set the render target on</param>
        /// <param name="color">The render target to set as color</param>
        /// <param name="colorLoadAction">The load action for the color render target</param>
        /// <param name="colorStoreAction">The store action for the color render target</param>
        /// <param name="depth">The render target to set as depth</param>
        /// <param name="depthLoadAction">The load action for the depth render target</param>
        /// <param name="depthStoreAction">The store action for the depth render target</param>
        public static void SetRenderTargetWithLoadStoreAction(this CommandBuffer cmd,
            RenderTargetIdentifier color, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depth, RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction)
        {
            #if UNITY_2018_2_OR_NEWER
            cmd.SetRenderTarget(color, colorLoadAction, colorStoreAction, depth, depthLoadAction, depthStoreAction);
            #else
            cmd.SetRenderTarget(color, depth);
            #endif
        }

        /// <summary>
        /// Does a copy of source to destination using a fullscreen triangle.
        /// </summary>
        /// <param name="cmd">The command buffer to use</param>
        /// <param name="source">The source render target</param>
        /// <param name="destination">The destination render target</param>
        /// <param name="clear">Should the destination target be cleared?</param>
        /// <param name="viewport">An optional viewport to consider for the blit</param>
        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, bool clear = false, Rect? viewport = null)
        {
            cmd.SetGlobalTexture(ShaderIDs.MainTex, source);
            cmd.SetRenderTargetWithLoadStoreAction(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

            if (viewport != null)
                cmd.SetViewport(viewport.Value);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, copyMaterial, 0, 0);
        }

        /// <summary>
        /// Blits a fullscreen triangle using a given material.
        /// </summary>
        /// <param name="cmd">The command buffer to use</param>
        /// <param name="source">The source render target</param>
        /// <param name="destination">The destination render target</param>
        /// <param name="propertySheet">The property sheet to use</param>
        /// <param name="pass">The pass from the material to use</param>
        /// <param name="loadAction">The load action for this blit</param>
        /// <param name="viewport">An optional viewport to consider for the blit</param>
        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, PropertySheet propertySheet, int pass, RenderBufferLoadAction loadAction, Rect? viewport = null)
        {
            cmd.SetGlobalTexture(ShaderIDs.MainTex, source);
            #if UNITY_2018_2_OR_NEWER
            bool clear = (loadAction == RenderBufferLoadAction.Clear);
            #else
            bool clear = false;
            #endif
            cmd.SetRenderTargetWithLoadStoreAction(destination, clear ? RenderBufferLoadAction.DontCare : loadAction, RenderBufferStoreAction.Store);

            if (viewport != null)
                cmd.SetViewport(viewport.Value);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, propertySheet.material, 0, pass, propertySheet.properties);
        }

        /// <summary>
        /// Blits a fullscreen triangle using a given material.
        /// </summary>
        /// <param name="cmd">The command buffer to use</param>
        /// <param name="source">The source render target</param>
        /// <param name="destination">The destination render target</param>
        /// <param name="propertySheet">The property sheet to use</param>
        /// <param name="pass">The pass from the material to use</param>
        /// <param name="clear">Should the destination target be cleared?</param>
        /// <param name="viewport">An optional viewport to consider for the blit</param>
        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, PropertySheet propertySheet, int pass, bool clear = false, Rect? viewport = null)
        {
            #if UNITY_2018_2_OR_NEWER
            cmd.BlitFullscreenTriangle(source, destination, propertySheet, pass, clear ? RenderBufferLoadAction.Clear : RenderBufferLoadAction.DontCare, viewport);
            #else
            cmd.SetGlobalTexture(ShaderIDs.MainTex, source);
            cmd.SetRenderTargetWithLoadStoreAction(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

            if (viewport != null)
                cmd.SetViewport(viewport.Value);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, propertySheet.material, 0, pass, propertySheet.properties);
            #endif
        }

        /// <summary>
        /// Blits a fullscreen triangle from a double-wide source.
        /// </summary>
        /// <param name="cmd">The command buffer to use</param>
        /// <param name="source">The source render target</param>
        /// <param name="destination">The destination render target</param>
        /// <param name="material">The material to use for the blit</param>
        /// <param name="pass">The pass from the material to use</param>
        /// <param name="eye">The target eye</param>
        public static void BlitFullscreenTriangleFromDoubleWide(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int pass, int eye)
        {
            Vector4 uvScaleOffset = new Vector4(0.5f, 1.0f, 0, 0);

            if (eye == 1)
                uvScaleOffset.z = 0.5f;
            cmd.SetGlobalVector(ShaderIDs.UVScaleOffset, uvScaleOffset);
            cmd.BuiltinBlit(source, destination, material, pass);
        }

        /// <summary>
        /// Blits a fullscreen triangle to a double-wide destination.
        /// </summary>
        /// <param name="cmd">The command buffer to use</param>
        /// <param name="source">The source render target</param>
        /// <param name="destination">The destination render target</param>
        /// <param name="propertySheet">The property sheet to use</param>
        /// <param name="pass">The pass from the material to use</param>
        /// <param name="eye">The target eye</param>
        public static void BlitFullscreenTriangleToDoubleWide(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, PropertySheet propertySheet, int pass, int eye)
        {
            Vector4 posScaleOffset = new Vector4(0.5f, 1.0f, -0.5f, 0);

            if (eye == 1)
                posScaleOffset.z = 0.5f;
            propertySheet.EnableKeyword("STEREO_DOUBLEWIDE_TARGET");
            propertySheet.properties.SetVector(ShaderIDs.PosScaleOffset, posScaleOffset);
            cmd.BlitFullscreenTriangle(source, destination, propertySheet, 0);
        }

        /// <summary>
        /// Blits a fullscreen triangle using a given material.
        /// </summary>
        /// <param name="cmd">The command buffer to use</param>
        /// <param name="source">The source texture array</param>
        /// <param name="destination">The destination render target</param>
        /// <param name="propertySheet">The property sheet to use</param>
        /// <param name="pass">The pass from the material to use</param>
        /// <param name="clear">Should the destination target be cleared?</param>
        /// <param name="depthSlice">The slice to use for the texture array</param>
        public static void BlitFullscreenTriangleFromTexArray(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, PropertySheet propertySheet, int pass, bool clear = false, int depthSlice = -1)
        {
            cmd.SetGlobalTexture(ShaderIDs.MainTex, source);
            cmd.SetGlobalFloat(ShaderIDs.DepthSlice, depthSlice);
            cmd.SetRenderTargetWithLoadStoreAction(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, propertySheet.material, 0, pass, propertySheet.properties);
        }

        /// <summary>
        /// Blits a fullscreen triangle using a given material.
        /// </summary>
        /// <param name="cmd">The command buffer to use</param>
        /// <param name="source">The source render target</param>
        /// <param name="destination">The destination render target</param>
        /// <param name="depth">The depth render target</param>
        /// <param name="propertySheet">The property sheet to use</param>
        /// <param name="pass">The pass from the material to use</param>
        /// <param name="clear">Should the destination target be cleared?</param>
        /// <param name="depthSlice">The array slice to consider as a source</param>
        public static void BlitFullscreenTriangleToTexArray(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, PropertySheet propertySheet, int pass, bool clear = false, int depthSlice = -1)
        {
            cmd.SetGlobalTexture(ShaderIDs.MainTex, source);
            cmd.SetGlobalFloat(ShaderIDs.DepthSlice, depthSlice);
            cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, propertySheet.material, 0, pass, propertySheet.properties);
        }

        /// <summary>
        /// Blits a fullscreen triangle using a given material.
        /// </summary>
        /// <param name="cmd">The command buffer to use</param>
        /// <param name="source">The source render target</param>
        /// <param name="destination">The destination render target</param>
        /// <param name="depth">The depth render target</param>
        /// <param name="propertySheet">The property sheet to use</param>
        /// <param name="pass">The pass from the material to use</param>
        /// <param name="clear">Should the destination target be cleared?</param>
        /// <param name="viewport">An optional viewport to consider for the blit</param>
        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, RenderTargetIdentifier depth, PropertySheet propertySheet, int pass, bool clear = false, Rect? viewport = null)
        {
            cmd.SetGlobalTexture(ShaderIDs.MainTex, source);
            
            if (clear)
            {
                cmd.SetRenderTargetWithLoadStoreAction(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                                                       depth, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.ClearRenderTarget(true, true, Color.clear);
            }
            else
            {
                cmd.SetRenderTargetWithLoadStoreAction(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                                                       depth, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            }

            if (viewport != null)
                cmd.SetViewport(viewport.Value);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, propertySheet.material, 0, pass, propertySheet.properties);
        }

        /// <summary>
        /// Blits a fullscreen triangle using a given material.
        /// </summary>
        /// <param name="cmd">The command buffer to use</param>
        /// <param name="source">The source render target</param>
        /// <param name="destinations">An array of destinations render targets</param>
        /// <param name="depth">The depth render target</param>
        /// <param name="propertySheet">The property sheet to use</param>
        /// <param name="pass">The pass from the material to use</param>
        /// <param name="clear">Should the destination target be cleared?</param>
        /// <param name="viewport">An optional viewport to consider for the blit</param>
        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier[] destinations, RenderTargetIdentifier depth, PropertySheet propertySheet, int pass, bool clear = false, Rect? viewport = null)
        {
            cmd.SetGlobalTexture(ShaderIDs.MainTex, source);
            cmd.SetRenderTarget(destinations, depth);

            if (viewport != null)
                cmd.SetViewport(viewport.Value);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, propertySheet.material, 0, pass, propertySheet.properties);
        }

        /// <summary>
        /// Does a copy of source to destination using the builtin blit command.
        /// </summary>
        /// <param name="cmd">The command buffer to use</param>
        /// <param name="source">The source render target</param>
        /// <param name="destination">The destination render target</param>
        public static void BuiltinBlit(this CommandBuffer cmd, Rendering.RenderTargetIdentifier source, RenderTargetIdentifier destination)
        {
            #if UNITY_2018_2_OR_NEWER
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            destination = BuiltinRenderTextureType.CurrentActive;
            #endif
            cmd.Blit(source, destination);
        }

        /// <summary>
        /// Blits a fullscreen quad using the builtin blit command and a given material.
        /// </summary>
        /// <param name="cmd">The command buffer to use</param>
        /// <param name="source">The source render target</param>
        /// <param name="destination">The destination render target</param>
        /// <param name="mat">The material to use for the blit</param>
        /// <param name="pass">The pass from the material to use</param>
        public static void BuiltinBlit(this CommandBuffer cmd, Rendering.RenderTargetIdentifier source, RenderTargetIdentifier destination, Material mat, int pass = 0)
        {
            #if UNITY_2018_2_OR_NEWER
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            destination = BuiltinRenderTextureType.CurrentActive;
            #endif
            cmd.Blit(source, destination, mat, pass);
        }

        // Fast basic copy texture if available, falls back to blit copy if not
        // Assumes that both textures have the exact same type and format
        /// <summary>
        /// Copies the content of a texture into the other. Both textures must have the same size
        /// and format or this method will fail.
        /// </summary>
        /// <param name="cmd">The command buffer to use</param>
        /// <param name="source">The source render target</param>
        /// <param name="destination">The destination render target</param>
        /// <remarks>
        /// If the CopyTexture command isn't supported on the target platform it will revert to a
        /// fullscreen blit command instead.
        /// </remarks>
        public static void CopyTexture(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination)
        {
            if (SystemInfo.copyTextureSupport > CopyTextureSupport.None)
            {
                cmd.CopyTexture(source, destination);
                return;
            }

            cmd.BlitFullscreenTriangle(source, destination);
        }

        // TODO: Generalize the GetTemporaryRT and Blit commands in order to support
        // RT Arrays for Stereo Instancing/MultiView

        #endregion

        #region Unity specifics & misc methods

        /// <summary>
        /// Returns <c>true</c> if a scriptable render pipeline is currently in use, <c>false</c>
        /// otherwise.
        /// </summary>
        public static bool scriptableRenderPipelineActive
        {
            get { return GraphicsSettings.renderPipelineAsset != null; } // 5.6+ only
        }

        /// <summary>
        /// Returns <c>true</c> if deferred shading is supported on the target platform,
        /// <c>false</c> otherwise.
        /// </summary>
        public static bool supportsDeferredShading
        {
            get { return scriptableRenderPipelineActive || GraphicsSettings.GetShaderMode(BuiltinShaderType.DeferredShading) != BuiltinShaderMode.Disabled; }
        }

        /// <summary>
        /// Returns <c>true</c> if <see cref="DepthTextureMode.DepthNormals"/> is supported on the
        /// target platform, <c>false</c> otherwise.
        /// </summary>
        public static bool supportsDepthNormals
        {
            get { return scriptableRenderPipelineActive || GraphicsSettings.GetShaderMode(BuiltinShaderType.DepthNormals) != BuiltinShaderMode.Disabled; }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Returns <c>true</c> if single-pass stereo rendering is selected, <c>false</c> otherwise.
        /// </summary>
        /// <remarks>
        /// This property only works in the editor.
        /// </remarks>
        public static bool isSinglePassStereoSelected
        {
            get
            {
                return PlayerSettings.virtualRealitySupported
                    && PlayerSettings.stereoRenderingPath == UnityEditor.StereoRenderingPath.SinglePass;
            }
        }
#endif

        /// <summary>
        /// Returns <c>true</c> if single-pass stereo rendering is active, <c>false</c> otherwise.
        /// </summary>
        /// <remarks>
        /// This property only works in the editor.
        /// </remarks>
        // TODO: Check for SPSR support at runtime
        public static bool isSinglePassStereoEnabled
        {
            get
            {
#if UNITY_EDITOR
                return isSinglePassStereoSelected && Application.isPlaying;
#elif UNITY_SWITCH
                return false;
#elif UNITY_2017_2_OR_NEWER
                return UnityEngine.XR.XRSettings.eyeTextureDesc.vrUsage == VRTextureUsage.TwoEyes;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Returns <c>true</c> if VR is enabled, <c>false</c> otherwise.
        /// </summary>
        public static bool isVREnabled
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.PlayerSettings.virtualRealitySupported;
#elif UNITY_XBOXONE || UNITY_SWITCH
                return false;
#elif UNITY_2017_2_OR_NEWER
                return UnityEngine.XR.XRSettings.enabled;
#elif UNITY_5_6_OR_NEWER
                return UnityEngine.VR.VRSettings.enabled;
#endif
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the target platform is Android and the selected API is OpenGL,
        /// <c>false</c> otherwise.
        /// </summary>
        public static bool isAndroidOpenGL
        {
            get { return Application.platform == RuntimePlatform.Android && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan; }
        }

        /// <summary>
        /// Gets the default HDR render texture format for the current target platform.
        /// </summary>
        public static RenderTextureFormat defaultHDRRenderTextureFormat
        {
            get
            {
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SWITCH || UNITY_EDITOR
                RenderTextureFormat format = RenderTextureFormat.RGB111110Float;
#if UNITY_EDITOR
                var target = EditorUserBuildSettings.activeBuildTarget;
                if (target != BuildTarget.Android && target != BuildTarget.iOS && target != BuildTarget.tvOS && target != BuildTarget.Switch)
                    return RenderTextureFormat.DefaultHDR;
#endif // UNITY_EDITOR
                if (format.IsSupported())
                    return format;
#endif // UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SWITCH || UNITY_EDITOR
                return RenderTextureFormat.DefaultHDR;
            }
        }

        /// <summary>
        /// Checks if a given render texture format is a floating-point format.
        /// </summary>
        /// <param name="format">The format to test</param>
        /// <returns><c>true</c> if the format is floating-point, <c>false</c> otherwise</returns>
        public static bool isFloatingPointFormat(RenderTextureFormat format)
        {
            return format == RenderTextureFormat.DefaultHDR || format == RenderTextureFormat.ARGBHalf || format == RenderTextureFormat.ARGBFloat ||
                   format == RenderTextureFormat.RGFloat || format == RenderTextureFormat.RGHalf ||
                   format == RenderTextureFormat.RFloat || format == RenderTextureFormat.RHalf ||
                   format == RenderTextureFormat.RGB111110Float;
        }

        /// <summary>
        /// Properly destroys a given Unity object.
        /// </summary>
        /// <param name="obj">The object to destroy</param>
        public static void Destroy(UnityObject obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    UnityObject.Destroy(obj);
                else
                    UnityObject.DestroyImmediate(obj);
#else
                UnityObject.Destroy(obj);
#endif
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the current color space setting is set to <c>Linear</c>,
        /// <c>false</c> otherwise.
        /// </summary>
        public static bool isLinearColorSpace
        {
            get { return QualitySettings.activeColorSpace == ColorSpace.Linear; }
        }

        /// <summary>
        /// Checks if resolved depth is available on the current target platform.
        /// </summary>
        /// <param name="camera">A rendering camera</param>
        /// <returns><c>true</c> if resolved depth is available, <c>false</c> otherwise</returns>
        public static bool IsResolvedDepthAvailable(Camera camera)
        {
            // AFAIK resolved depth is only available on D3D11/12 via BuiltinRenderTextureType.ResolvedDepth
            // TODO: Is there more proper way to determine this? What about SRPs?
            var gtype = SystemInfo.graphicsDeviceType;
            return camera.actualRenderingPath == RenderingPath.DeferredShading &&
                (gtype == GraphicsDeviceType.Direct3D11 || gtype == GraphicsDeviceType.Direct3D12 || gtype == GraphicsDeviceType.XboxOne);
        }

        /// <summary>
        /// Properly destroys a given profile.
        /// </summary>
        /// <param name="profile">The profile to destroy</param>
        /// <param name="destroyEffects">Should we destroy all the embedded settings?</param>
        public static void DestroyProfile(PostProcessProfile profile, bool destroyEffects)
        {
            if (destroyEffects)
            {
                foreach (var effect in profile.settings)
                    Destroy(effect);
            }

            Destroy(profile);
        }

        /// <summary>
        /// Properly destroys a volume.
        /// </summary>
        /// <param name="volume">The volume to destroy</param>
        /// <param name="destroyProfile">Should we destroy the attached profile?</param>
        /// <param name="destroyGameObject">Should we destroy the volume Game Object?</param>
        public static void DestroyVolume(PostProcessVolume volume, bool destroyProfile, bool destroyGameObject = false)
        {
            if (destroyProfile)
                DestroyProfile(volume.profileRef, true);

            var gameObject = volume.gameObject;
            Destroy(volume);

            if (destroyGameObject)
                Destroy(gameObject);
        }

        /// <summary>
        /// Checks if a post-processing layer is active.
        /// </summary>
        /// <param name="layer">The layer to check; can be <c>null</c></param>
        /// <returns><c>true</c> if the layer is enabled, <c>false</c> otherwise</returns>
        public static bool IsPostProcessingActive(PostProcessLayer layer)
        {
            return layer != null
                && layer.enabled;
        }

        /// <summary>
        /// Checks if temporal anti-aliasing is active on a given post-process layer.
        /// </summary>
        /// <param name="layer">The layer to check</param>
        /// <returns><c>true</c> if temporal anti-aliasing is active, <c>false</c> otherwise</returns>
        public static bool IsTemporalAntialiasingActive(PostProcessLayer layer)
        {
            return IsPostProcessingActive(layer)
                && layer.antialiasingMode == PostProcessLayer.Antialiasing.TemporalAntialiasing
                && layer.temporalAntialiasing.IsSupported();
        }

        /// <summary>
        /// Gets all scene objects in the hierarchy, including inactive objects. This method is slow
        /// on large scenes and should be used with extreme caution.
        /// </summary>
        /// <typeparam name="T">The component to look for</typeparam>
        /// <returns>A list of all components of type <c>T</c> in the scene</returns>
        public static IEnumerable<T> GetAllSceneObjects<T>()
            where T : Component
        {
            var queue = new Queue<Transform>();
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (var root in roots)
            {
                queue.Enqueue(root.transform);
                var comp = root.GetComponent<T>();

                if (comp != null)
                    yield return comp;
            }

            while (queue.Count > 0)
            {
                foreach (Transform child in queue.Dequeue())
                {
                    queue.Enqueue(child);
                    var comp = child.GetComponent<T>();

                    if (comp != null)
                        yield return comp;
                }
            }
        }

        /// <summary>
        /// Creates an instance of a class if it's <c>null</c>.
        /// </summary>
        /// <typeparam name="T">The type to create</typeparam>
        /// <param name="obj">A reference to an instance to check and create if needed</param>
        public static void CreateIfNull<T>(ref T obj)
            where T : class, new()
        {
            if (obj == null)
                obj = new T();
        }

        #endregion

        #region Maths

        /// <summary>
        /// Returns the base-2 exponential function of <paramref name="x"/>, which is <c>2</c>
        /// raised to the power <paramref name="x"/>.
        /// </summary>
        /// <param name="x">Value of the exponent</param>
        /// <returns>The base-2 exponential function of <paramref name="x"/></returns>
        public static float Exp2(float x)
        {
            return Mathf.Exp(x * 0.69314718055994530941723212145818f);
        }

        /// <summary>
        /// Gets a jittered perspective projection matrix for a given camera.
        /// </summary>
        /// <param name="camera">The camera to build the projection matrix for</param>
        /// <param name="offset">The jitter offset</param>
        /// <returns>A jittered projection matrix</returns>
        public static Matrix4x4 GetJitteredPerspectiveProjectionMatrix(Camera camera, Vector2 offset)
        {
            float near = camera.nearClipPlane;
            float far = camera.farClipPlane;

            float vertical = Mathf.Tan(0.5f * Mathf.Deg2Rad * camera.fieldOfView) * near;
            float horizontal = vertical * camera.aspect;

            offset.x *= horizontal / (0.5f * camera.pixelWidth);
            offset.y *= vertical / (0.5f * camera.pixelHeight);

            var matrix = camera.projectionMatrix;

            matrix[0, 2] += offset.x / horizontal;
            matrix[1, 2] += offset.y / vertical;

            return matrix;
        }

        /// <summary>
        /// Gets a jittered orthographic projection matrix for a given camera.
        /// </summary>
        /// <param name="camera">The camera to build the orthographic matrix for</param>
        /// <param name="offset">The jitter offset</param>
        /// <returns>A jittered projection matrix</returns>
        public static Matrix4x4 GetJitteredOrthographicProjectionMatrix(Camera camera, Vector2 offset)
        {
            float vertical = camera.orthographicSize;
            float horizontal = vertical * camera.aspect;

            offset.x *= horizontal / (0.5f * camera.pixelWidth);
            offset.y *= vertical / (0.5f * camera.pixelHeight);

            float left = offset.x - horizontal;
            float right = offset.x + horizontal;
            float top = offset.y + vertical;
            float bottom = offset.y - vertical;

            return Matrix4x4.Ortho(left, right, bottom, top, camera.nearClipPlane, camera.farClipPlane);
        }

        /// <summary>
        /// Gets a jittered perspective projection matrix from an original projection matrix.
        /// </summary>
        /// <param name="context">The current render context</param>
        /// <param name="origProj">The original projection matrix</param>
        /// <param name="jitter">The jitter offset</param>
        /// <returns>A jittered projection matrix</returns>
        public static Matrix4x4 GenerateJitteredProjectionMatrixFromOriginal(PostProcessRenderContext context, Matrix4x4 origProj, Vector2 jitter)
        {
#if UNITY_2017_2_OR_NEWER
            var planes = origProj.decomposeProjection;

            float vertFov = Math.Abs(planes.top) + Math.Abs(planes.bottom);
            float horizFov = Math.Abs(planes.left) + Math.Abs(planes.right);

            var planeJitter = new Vector2(jitter.x * horizFov / context.screenWidth,
                                          jitter.y * vertFov / context.screenHeight);

            planes.left += planeJitter.x;
            planes.right += planeJitter.x;
            planes.top += planeJitter.y;
            planes.bottom += planeJitter.y;

            var jitteredMatrix = Matrix4x4.Frustum(planes);

            return jitteredMatrix;
#else
            var rTan = (1.0f + origProj[0, 2]) / origProj[0, 0];
            var lTan = (-1.0f + origProj[0, 2]) / origProj[0, 0];

            var tTan = (1.0f + origProj[1, 2]) / origProj[1, 1];
            var bTan = (-1.0f + origProj[1, 2]) / origProj[1, 1];

            float tanVertFov = Math.Abs(tTan) + Math.Abs(bTan);
            float tanHorizFov = Math.Abs(lTan) + Math.Abs(rTan);

            jitter.x *= tanHorizFov / context.screenWidth;
            jitter.y *= tanVertFov / context.screenHeight;

            float left = jitter.x + lTan;
            float right = jitter.x + rTan;
            float top = jitter.y + tTan;
            float bottom = jitter.y + bTan;

            var jitteredMatrix = new Matrix4x4();

            jitteredMatrix[0, 0] = 2f / (right - left);
            jitteredMatrix[0, 1] = 0f;
            jitteredMatrix[0, 2] = (right + left) / (right - left);
            jitteredMatrix[0, 3] = 0f;

            jitteredMatrix[1, 0] = 0f;
            jitteredMatrix[1, 1] = 2f / (top - bottom);
            jitteredMatrix[1, 2] = (top + bottom) / (top - bottom);
            jitteredMatrix[1, 3] = 0f;

            jitteredMatrix[2, 0] = 0f;
            jitteredMatrix[2, 1] = 0f;
            jitteredMatrix[2, 2] = origProj[2, 2];
            jitteredMatrix[2, 3] = origProj[2, 3];

            jitteredMatrix[3, 0] = 0f;
            jitteredMatrix[3, 1] = 0f;
            jitteredMatrix[3, 2] = -1f;
            jitteredMatrix[3, 3] = 0f;

            return jitteredMatrix;
#endif
        }

        #endregion

        #region Reflection

        static IEnumerable<Type> m_AssemblyTypes;

        /// <summary>
        /// Gets all currently available assembly types.
        /// </summary>
        /// <returns>A list of all currently available assembly types</returns>
        /// <remarks>
        /// This method is slow and should be use with extreme caution.
        /// </remarks>
        public static IEnumerable<Type> GetAllAssemblyTypes()
        {
            if (m_AssemblyTypes == null)
            {
                m_AssemblyTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(t =>
                    {
                        // Ugly hack to handle mis-versioned dlls
                        var innerTypes = new Type[0];
                        try
                        {
                            innerTypes = t.GetTypes();
                        }
                        catch { }
                        return innerTypes;
                    });
            }

            return m_AssemblyTypes;
        }

        /// <summary>
        /// Helper method to get the first attribute of type <c>T</c> on a given type.
        /// </summary>
        /// <typeparam name="T">The attribute type to look for</typeparam>
        /// <param name="type">The type to explore</param>
        /// <returns>The attribute found</returns>
        public static T GetAttribute<T>(this Type type) where T : Attribute
        {
            Assert.IsTrue(type.IsDefined(typeof(T), false), "Attribute not found");
            return (T)type.GetCustomAttributes(typeof(T), false)[0];
        }

        /// <summary>
        /// Returns all attributes set on a specific member.
        /// </summary>
        /// <typeparam name="TType">The class type where the member is defined</typeparam>
        /// <typeparam name="TValue">The member type</typeparam>
        /// <param name="expr">An expression path to the member</param>
        /// <returns>An array of attributes</returns>
        /// <remarks>
        /// This method doesn't return inherited attributes, only explicit ones.
        /// </remarks>
        public static Attribute[] GetMemberAttributes<TType, TValue>(Expression<Func<TType, TValue>> expr)
        {
            Expression body = expr;

            if (body is LambdaExpression)
                body = ((LambdaExpression)body).Body;

            switch (body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    var fi = (FieldInfo)((MemberExpression)body).Member;
                    return fi.GetCustomAttributes(false).Cast<Attribute>().ToArray();
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Returns a string path from an expression. This is mostly used to retrieve serialized
        /// properties without hardcoding the field path as a string and thus allowing proper
        /// refactoring features.
        /// </summary>
        /// <typeparam name="TType">The class type where the member is defined</typeparam>
        /// <typeparam name="TValue">The member type</typeparam>
        /// <param name="expr">An expression path fo the member</param>
        /// <returns>A string representation of the expression path</returns>
        public static string GetFieldPath<TType, TValue>(Expression<Func<TType, TValue>> expr)
        {
            MemberExpression me;
            switch (expr.Body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    me = expr.Body as MemberExpression;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            var members = new List<string>();
            while (me != null)
            {
                members.Add(me.Member.Name);
                me = me.Expression as MemberExpression;
            }

            var sb = new StringBuilder();
            for (int i = members.Count - 1; i >= 0; i--)
            {
                sb.Append(members[i]);
                if (i > 0) sb.Append('.');
            }

            return sb.ToString();
        }

        #endregion
    }
}
