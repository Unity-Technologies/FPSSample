using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class ProjectOnDepth : VFXBlock
    {
		public enum PositionMode
		{
			Random,
			Sequential,
			Custom,
		}

        public enum CullMode
        {
            None,
            FarPlane,
            Range,
        }
		
        public class InputProperties
        {
            public CameraType Camera = CameraType.defaultValue;
            public float ZMultiplier = 1.0f;
            public Texture2D DepthBuffer = null;
        }

        public class SceneColorInputProperties
        {
            public Texture2D ColorBuffer = null;
        }

        public class SequentialInputProperties
        {
            public uint GridStep = 1;
        }
		
		public class CustomInputProperties
		{
            [Range(0.0f, 1.0f)]
            public Vector2 UVSpawn;
		}

        public class RangeInputProperties
        {
            [Range(0.0f,1.0f)]
            public Vector2 DepthRange = new Vector2(0.0f,1.0f);
        }

        [VFXSetting]
		public PositionMode mode;

        [VFXSetting]
        public CullMode cullMode;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public bool inheritSceneColor = false;

        public override string name { get { return "Project On Depth"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInit; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Write);

                if (inheritSceneColor)
                    yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Write);
				
                if (mode == PositionMode.Sequential)
                    yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
                else if (mode == PositionMode.Random)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);

                if (cullMode != CullMode.None)
                    yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Write);   
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var inputs = PropertiesFromType("InputProperties");
                if (inheritSceneColor)
                    inputs = inputs.Concat(PropertiesFromType("SceneColorInputProperties"));
                if (mode == PositionMode.Sequential)
                    inputs = inputs.Concat(PropertiesFromType("SequentialInputProperties"));
				else if (mode == PositionMode.Custom)
					inputs = inputs.Concat(PropertiesFromType("CustomInputProperties"));
                if (cullMode == CullMode.Range)
                    inputs = inputs.Concat(PropertiesFromType("RangeInputProperties"));
                return inputs;
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var expressions = GetExpressionsFromSlots(this);

                var fov = expressions.First(e => e.name == "Camera_fieldOfView");
                var aspect = expressions.First(e => e.name == "Camera_aspectRatio");
                var near = expressions.First(e => e.name == "Camera_nearPlane");
                var far = expressions.First(e => e.name == "Camera_farPlane");
                var cameraMatrix = expressions.First(e => e.name == "Camera_transform");

                expressions = expressions.Where(t => !(t.Equals(fov) || t.Equals(aspect) || t.Equals(near) || t.Equals(far) || t.Equals(cameraMatrix)));

                foreach (var input in expressions)
                    yield return input;

                var clipToVFX = new VFXExpressionTransformMatrix(cameraMatrix.exp, new VFXExpressionInverseMatrix(VFXOperatorUtility.GetPerspectiveMatrix(fov.exp, aspect.exp, near.exp, far.exp)));

                yield return new VFXNamedExpression(clipToVFX, "clipToVFX");
            }
        }

        public override string source
        {
            get
            {
                string source = "";              

				switch(mode)
				{
					case PositionMode.Random:
						source += @"
float2 uvs = RAND2;
";
					break;
					
					case PositionMode.Sequential:
						source += @"
// Pixel perfect spawn
uint2 sSize = Camera_pixelDimensions / GridStep;
uint nbPixels = sSize.x * sSize.y;
uint id = particleId % nbPixels;
uint2 ids = uint2(id % sSize.x,id / sSize.x) * GridStep + (GridStep >> 1);
float2 uvs = (ids + 0.5f) / Camera_pixelDimensions;
";
					break;
					
					case PositionMode.Custom:
						source += @"
float2 uvs = UVSpawn;
";
					break;
				}

                source += @"
float2 projpos = uvs * 2.0f - 1.0f;
				
float depth = LoadTexture(DepthBuffer,int3(uvs*Camera_pixelDimensions, 0)).r;
#if UNITY_REVERSED_Z
depth = 1.0f - depth; // reversed z
#endif";

                if (cullMode == CullMode.FarPlane)
                    source += @"
// cull on far plane
if (depth >= 1.0f - VFX_EPSILON)
{
    alive = false;
    return;
}
                ";

                if (cullMode == CullMode.Range)
                    source += @"
// filter based on depth
if (depth < DepthRange.x || depth > DepthRange.y)
{
    alive = false;
    return;
}
";
            source += @"
float4 clipPos = float4(projpos,depth * ZMultiplier * 2.0f - 1.0f,1.0f);
float4 vfxPos = mul(clipToVFX,clipPos);
position = vfxPos.xyz / vfxPos.w;
";

                if (inheritSceneColor)
                    source += @"
color = SampleTexture(ColorBuffer,uvs).rgb;
";

                return source;
            }
        }

    }
}
