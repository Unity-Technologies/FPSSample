using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Orientation")]
    class ConnectTarget : VFXBlock
    {
        public enum OrientMode
        {
            Camera,
            Direction,
            LookAtPosition
        }

        [VFXSetting]
        public OrientMode Orientation = OrientMode.Camera;

        public override string name { get { return "Connect Target"; } }

        public override VFXContextType compatibleContexts { get { return VFXContextType.kOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public class InputProperties
        {
            [Tooltip("The position that corresponds to the top end of the particle")]
            public Position TargetPosition = Position.defaultValue;
            [Tooltip("The direction that the particle face towards")]
            public DirectionType LookDirection = DirectionType.defaultValue;
            [Tooltip("The position that the particle look at")]
            public Position LookAtPosition = Position.defaultValue;
            [Range(0.0f, 1.0f), Tooltip("The position (relative to the segment) that act as a pivot.")]
            public float PivotShift = 0.5f;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var property in PropertiesFromType(GetInputPropertiesTypeName()))
                {
                    if (Orientation != OrientMode.Direction && property.property.name == "LookDirection") continue;
                    if (Orientation != OrientMode.LookAtPosition && property.property.name == "LookAtPosition") continue;

                    yield return property;
                }
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Write);

                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Write);
            }
        }

        public override string source
        {
            get
            {
                string orient = string.Empty;

                switch (Orientation)
                {
                    case OrientMode.Camera: orient = "position - GetViewVFXPosition()"; break;
                    case OrientMode.Direction: orient = "LookDirection"; break;
                    case OrientMode.LookAtPosition: orient = "position - LookAtPosition"; break;
                }

                return string.Format(@"
axisY = TargetPosition-position;
float len = length(axisY);
scaleY = len / size;
axisY /= len;
axisZ = {0};
axisX = normalize(cross(axisY,axisZ));
axisZ = cross(axisX,axisY);

position = lerp(position, TargetPosition, PivotShift);
pivotY = PivotShift - 0.5;
", orient);
            }
        }
    }
}
