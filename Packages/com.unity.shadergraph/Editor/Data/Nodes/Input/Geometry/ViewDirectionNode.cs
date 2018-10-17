using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEngine.MaterialGraph.ViewDirectionNode")]
    [Title("Input", "Geometry", "View Direction")]
    public class ViewDirectionNode : GeometryNode, IMayRequireViewDirection
    {
        private const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Out";

        public ViewDirectionNode()
        {
            name = "View Direction";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/View-Direction-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(
                    kOutputSlotId,
                    kOutputSlotName,
                    kOutputSlotName,
                    SlotType.Output,
                    Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return string.Format("IN.{0}", space.ToVariableName(InterpolatorType.ViewDirection));
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            return space.ToNeededCoordinateSpace();
        }
    }
}
