using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class VolumetricMenuItems
    {
        [MenuItem("GameObject/Rendering/Density Volume", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateDensityVolumeGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var densityVolume = CoreEditorUtils.CreateGameObject(parent, "Density Volume");
            GameObjectUtility.SetParentAndAlign(densityVolume, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(densityVolume, "Create " + densityVolume.name);
            Selection.activeObject = densityVolume;

            densityVolume.AddComponent<DensityVolume>();
        }
    }
}
