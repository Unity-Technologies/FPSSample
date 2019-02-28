using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class OrientationModeProvider : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "mode", Enum.GetValues(typeof(Orient.Mode)).Cast<object>().ToArray() }
                };
            }
        }
    }

    [VFXInfo(category = "Orientation", variantProvider = typeof(OrientationModeProvider))]
    class Orient : VFXBlock
    {
        public enum Mode
        {
            FaceCameraPlane,
            FaceCameraPosition,
            LookAtPosition,
            LookAtLine,
            FixedOrientation,
            FixedAxis,
            AlongVelocity,
        }

        [VFXSetting]
        public Mode mode;

        public override string name { get { return "Orient : " + ObjectNames.NicifyVariableName(mode.ToString()); } }

        public override VFXContextType compatibleContexts   { get { return VFXContextType.kOutput; } }
        public override VFXDataType compatibleData          { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Write);
                if (mode != Mode.FixedOrientation && mode != Mode.FaceCameraPlane)
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                if (mode == Mode.AlongVelocity)
                    yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                switch (mode)
                {
                    case Mode.LookAtPosition:
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Position), "Position"));
                        break;

                    case Mode.LookAtLine:
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Line), "Line"), Line.defaultValue);
                        break;

                    case Mode.FixedOrientation:
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(DirectionType), "Front"), new DirectionType() { direction = Vector3.forward });
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(DirectionType), "Up"), new DirectionType() { direction = Vector3.up });
                        break;

                    case Mode.FixedAxis:
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(DirectionType), "Up"), new DirectionType() { direction = Vector3.up });
                        break;
                }
            }
        }

        public override string source
        {
            get
            {
                switch (mode)
                {
                    case Mode.FaceCameraPlane:
                        return @"
float3x3 viewRot = GetVFXToViewRotMatrix();
axisX = viewRot[0].xyz;
axisY = viewRot[1].xyz;
#if VFX_LOCAL_SPACE // Need to remove potential scale in local transform
axisX = normalize(axisX);
axisY = normalize(axisY);
axisZ = cross(axisX,axisY);
#else
axisZ = -viewRot[2].xyz;
#endif
";

                    case Mode.FaceCameraPosition:
                        return @"
if (unity_OrthoParams.w == 1.0f) // Face plane for ortho
{
    float3x3 viewRot = GetVFXToViewRotMatrix();
    axisX = viewRot[0].xyz;
    axisY = viewRot[1].xyz;
    #if VFX_LOCAL_SPACE // Need to remove potential scale in local transform
    axisX = normalize(axisX);
    axisY = normalize(axisY);
    axisZ = cross(axisX,axisY);
    #else
    axisZ = -viewRot[2].xyz;
    #endif
}
else
{
    axisZ = normalize(position - GetViewVFXPosition());
    axisX = normalize(cross(GetVFXToViewRotMatrix()[1].xyz,axisZ));
    axisY = cross(axisZ,axisX);
}
";

                    case Mode.LookAtPosition:
                        return @"
axisZ = normalize(position - Position);
axisX = normalize(cross(GetVFXToViewRotMatrix()[1].xyz,axisZ));
axisY = cross(axisZ,axisX);
";

                    case Mode.LookAtLine:
                        return @"
float3 lineDir = normalize(Line_end - Line_start);
float3 target = dot(position - Line_start,lineDir) * lineDir + Line_start;
axisZ = normalize(position - target);
axisX = normalize(cross(GetVFXToViewRotMatrix()[1].xyz,axisZ));
axisY = cross(axisZ,axisX);
";

                    case Mode.FixedOrientation:
                        return @"
axisZ = Front;
axisX = normalize(cross(Up,axisZ));
axisY = cross(axisZ,axisX);
";

                    case Mode.FixedAxis:
                        return @"
axisY = Up;
axisZ = position - GetViewVFXPosition();
axisX = normalize(cross(axisY,axisZ));
axisZ = cross(axisX,axisY);
";

                    case Mode.AlongVelocity:
                        return @"
axisY = normalize(velocity);
axisZ = position - GetViewVFXPosition();
axisX = normalize(cross(axisY,axisZ));
axisZ = cross(axisX,axisY);
";

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public override void Sanitize(int version)
        {
            if (mode == Mode.LookAtPosition)
            {
                /* Slot of type position has changed from undefined VFXSlot to VFXSlotPosition*/
                if (GetNbInputSlots() > 0 && !(GetInputSlot(0) is VFXSlotPosition))
                {
                    var oldValue = GetInputSlot(0).value;
                    RemoveSlot(GetInputSlot(0));
                    AddSlot(VFXSlot.Create(new VFXProperty(typeof(Position), "Position"), VFXSlot.Direction.kInput, oldValue));
                }
            }
            base.Sanitize(version);
        }
    }
}
