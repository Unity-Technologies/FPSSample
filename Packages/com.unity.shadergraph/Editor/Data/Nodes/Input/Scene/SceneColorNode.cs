using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Scene Color")]
    public sealed class SceneColorNode : CodeFunctionNode, IMayRequireCameraOpaqueTexture
    {
        const string kScreenPositionSlotName = "UV";
        const string kOutputSlotName = "Out";

        public const int ScreenPositionSlotId = 0;
        public const int OutputSlotId = 1;

        public SceneColorNode()
        {
            name = "Scene Color";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Scene-Color-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_SceneColor", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_SceneColor(
            [Slot(0, Binding.ScreenPosition)] Vector3 UV,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out Vector3 Out)
        {
            Out = Vector3.one;
            return
                @"
{
    Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV);
}
";
        }

        public bool RequiresCameraOpaqueTexture(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
