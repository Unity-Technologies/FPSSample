using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Normal", "Normal Reconstruct Z")]
    public class NormalReconstructZNode : CodeFunctionNode
    {
        public NormalReconstructZNode()
        {
            name = "Normal Reconstruct Z";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Normal-Reconstruct-Z-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("NormalReconstructZ", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string NormalReconstructZ(
            [Slot(0, Binding.None)] Vector2 In,
            [Slot(2, Binding.None, ShaderStageCapability.Fragment)] out Vector3 Out)
            
        {
            Out = Vector3.zero;
            return
                @"
{
    {precision} reconstructZ = sqrt(1 - ( In.x * In.x + In.y * In.y));
    {precision}3 normalVector = {precision}3(In.x, In.y, reconstructZ);
    Out = normalize(normalVector);
}";
        }
    }
}
