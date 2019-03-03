using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionAABox : PositionBase
    {
        public override string name { get { return "Position (AABox)"; } }

        public class InputProperties
        {
            [Tooltip("The box used for positioning particles.")]
            public AABox Box = new AABox() { size = Vector3.one };
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this).Where(e => e.name != "Thickness"))
                    yield return p;

                if (positionMode == PositionMode.ThicknessAbsolute || positionMode == PositionMode.ThicknessRelative)
                {
                    VFXExpression factor = VFXValue.Constant(Vector3.zero);
                    VFXExpression boxSize = inputSlots[0][1].GetExpression();

                    switch (positionMode)
                    {
                        case PositionMode.ThicknessAbsolute:
                            factor = VFXOperatorUtility.Clamp(VFXOperatorUtility.CastFloat(inputSlots[1].GetExpression() * VFXValue.Constant(2.0f), VFXValueType.Float3), VFXValue.Constant(0.0f), boxSize);
                            break;
                        case PositionMode.ThicknessRelative:
                            factor = VFXOperatorUtility.CastFloat(VFXOperatorUtility.Saturate(inputSlots[1].GetExpression()), VFXValueType.Float3) * boxSize;
                            break;
                    }

                    factor = new VFXExpressionMax(factor, VFXValue.Constant(new Vector3(0.0001f, 0.0001f, 0.0001f)));

                    VFXExpression volumeXY = new VFXExpressionCombine(boxSize.x, boxSize.y, factor.z);
                    VFXExpression volumeXZ = new VFXExpressionCombine(boxSize.x, boxSize.z - factor.z, factor.y);
                    VFXExpression volumeYZ = new VFXExpressionCombine(boxSize.y - factor.y, boxSize.z - factor.z, factor.x);

                    VFXExpression volumes = new VFXExpressionCombine(
                        volumeXY.x * volumeXY.y * volumeXY.z,
                        volumeXZ.x * volumeXZ.y * volumeXZ.z,
                        volumeYZ.x * volumeYZ.y * volumeYZ.z
                    );
                    VFXExpression cumulativeVolumes = new VFXExpressionCombine(
                        volumes.x,
                        volumes.x + volumes.y,
                        volumes.x + volumes.y + volumes.z
                    );

                    yield return new VFXNamedExpression(volumeXY, "volumeXY");
                    yield return new VFXNamedExpression(volumeXZ, "volumeXZ");
                    yield return new VFXNamedExpression(volumeYZ, "volumeYZ");
                    yield return new VFXNamedExpression(cumulativeVolumes, "cumulativeVolumes");
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                yield return "spawnMode";
            }
        }

        public override string source
        {
            get
            {
                if (positionMode == PositionMode.Volume)
                {
                    return @"position = Box_size * (RAND3 - 0.5f) + Box_center;";
                }
                else if (positionMode == PositionMode.Surface)
                {
                    return @"
float areaXY = max(Box_size.x * Box_size.y, VFX_EPSILON);
float areaXZ = max(Box_size.x * Box_size.z, VFX_EPSILON);
float areaYZ = max(Box_size.y * Box_size.z, VFX_EPSILON);

float face = RAND * (areaXY + areaXZ + areaYZ);
float flip = (RAND >= 0.5f) ? 0.5f : -0.5f;
float3 cube = float3(RAND2 - 0.5f, flip);

if (face < areaXY)
    cube = cube.xyz;
else if(face < areaXY + areaXZ)
    cube = cube.xzy;
else
    cube = cube.zxy;

position = cube * Box_size + Box_center;
";
                }
                else
                {
                    return @"
float face = RAND * cumulativeVolumes.z;
float flip = (RAND >= 0.5f) ? 1.0f : -1.0f;
float3 cube = float3(RAND2 * 2.0f - 1.0f, -RAND);

if (face < cumulativeVolumes.x)
{
    cube = (cube * volumeXY).xyz + float3(0.0f, 0.0f, Box_size.z);
    cube.z *= flip;
}
else if(face < cumulativeVolumes.y)
{
    cube = (cube * volumeXZ).xzy + float3(0.0f, Box_size.y, 0.0f);
    cube.y *= flip;
}
else
{
    cube = (cube * volumeYZ).zxy + float3(Box_size.x, 0.0f, 0.0f);
    cube.x *= flip;
}

position = cube * 0.5f + Box_center;
";
                }
            }
        }
    }
}
