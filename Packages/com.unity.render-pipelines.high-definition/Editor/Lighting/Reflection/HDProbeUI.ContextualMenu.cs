using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDProbeUI
    {
        [MenuItem("GameObject/3D Object/Mirror", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateMirrorGameObject(MenuCommand menuCommand)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObjectUtility.SetParentAndAlign(plane, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(plane, "Create " + plane.name);
            Selection.activeObject = plane;

            var planarProbe = plane.AddComponent<PlanarReflectionProbe>();
            planarProbe.influenceVolume.boxSize = new Vector3(10, 0.01f, 10);

            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            var material = hdrp != null ? hdrp.GetDefaultMirrorMaterial() : null;

            if (material)
            {
                plane.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        [MenuItem("GameObject/Light/Planar Reflection Probe", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreatePlanarReflectionGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CoreEditorUtils.CreateGameObject(parent, "Planar Reflection");
            var planarProbe = go.AddComponent<PlanarReflectionProbe>();
            planarProbe.influenceVolume.boxSize = new Vector3(1, 0.01f, 1);
            // Ensure it gets re-parented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
}
