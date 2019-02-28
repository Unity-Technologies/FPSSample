using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;
using System.Linq;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;

    [CustomEditorForRenderPipeline(typeof(PlanarReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    class PlanarReflectionProbeEditor : HDProbeEditor
    {
        [DidReloadScripts]
        static void DidReloadScripts()
        {
            foreach (var probe in FindObjectsOfType<PlanarReflectionProbe>())
            {
                if (probe.enabled)
                    ReflectionSystem.RegisterProbe(probe);
            }
        }

        internal override HDProbe GetTarget(Object editorTarget)
        {
            return editorTarget as HDProbe;
        }

        protected override void Draw(HDProbeUI s, SerializedHDProbe serialized, Editor owner)
        {
#pragma warning disable 612 //Draw
            PlanarReflectionProbeUI.Inspector.Draw(s, serialized, owner);
#pragma warning restore 612
        }

        protected override void OnEnable()
        {
            m_SerializedHDProbe = new SerializedPlanarReflectionProbe(serializedObject);
            base.OnEnable();

            PlanarReflectionProbe probe = (PlanarReflectionProbe)target;
            probe.influenceVolume.Init(probe);
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();
            PlanarReflectionProbeUI.DrawHandlesOverride(m_UIState as PlanarReflectionProbeUI, m_SerializedHDProbe as SerializedPlanarReflectionProbe, this);

            SceneViewOverlay_Window(_.GetContent("Planar Probe"), OnOverlayGUI, -100, target);
        }


        const float k_PreviewHeight = 128;
        List<Texture> m_PreviewedTextures = new List<Texture>();

        void OnOverlayGUI(Object target, SceneView sceneView)
        {
            var previewSize = new Rect();
            foreach(PlanarReflectionProbe p in m_TypedTargets)
            {
                if (p.currentTexture == null)
                    continue;

                var factor = k_PreviewHeight / p.currentTexture.height;

                previewSize.x += p.currentTexture.width * factor;
                previewSize.y = k_PreviewHeight;
            }

            // Get and reserve rect
            Rect cameraRect = GUILayoutUtility.GetRect(previewSize.x, previewSize.y);

            if (Event.current.type == EventType.Repaint)
            {
                var c = new Rect(cameraRect);
                foreach(PlanarReflectionProbe p in m_TypedTargets)
                {
                    if (p.currentTexture == null)
                        continue;

                    var factor = k_PreviewHeight / p.currentTexture.height;

                    c.width = p.currentTexture.width * factor;
                    c.height = k_PreviewHeight;
                    Graphics.DrawTexture(c, p.currentTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, CameraEditorUtils.GUITextureBlit2SRGBMaterial);

                    c.x += c.width;
                }
            }
        }

        public override bool HasPreviewGUI()
        {
            foreach(PlanarReflectionProbe p in m_TypedTargets)
            {
                if (p.currentTexture != null)
                    return true;
            }
            return false;
        }

        public override GUIContent GetPreviewTitle()
        {
            return _.GetContent("Planar Reflection");
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            m_PreviewedTextures.Clear();
            foreach (PlanarReflectionProbe p in m_TypedTargets)
            {
                m_PreviewedTextures.Add(p.currentTexture);
            }

            var space = Vector2.one;
            var rowSize = Mathf.CeilToInt(Mathf.Sqrt(m_PreviewedTextures.Count));
            var size = r.size / rowSize - space * (rowSize - 1);

            for (var i = 0; i < m_PreviewedTextures.Count; i++)
            {
                var row = i / rowSize;
                var col = i % rowSize;
                var itemRect = new Rect(
                        r.x + size.x * row + ((row > 0) ? (row - 1) * space.x : 0),
                        r.y + size.y * col + ((col > 0) ? (col - 1) * space.y : 0),
                        size.x,
                        size.y);

                if (m_PreviewedTextures[i] != null)
                    EditorGUI.DrawPreviewTexture(itemRect, m_PreviewedTextures[i], CameraEditorUtils.GUITextureBlit2SRGBMaterial, ScaleMode.ScaleToFit, 0, 1);
                else
                    EditorGUI.LabelField(itemRect, _.GetContent("Not Available"));
            }
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
