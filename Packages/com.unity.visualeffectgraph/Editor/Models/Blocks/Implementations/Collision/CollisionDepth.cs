using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Collision")]
    class CollisionDepth : CollisionBase
    {
        enum SurfaceThickness
        {
            Infinite,
            Custom,
        }

        [VFXSetting]
        SurfaceThickness surfaceThickness = SurfaceThickness.Infinite;

        public override string name { get { return "Collider (Depth)"; } }

        public class InputProperties
        {
            public CameraType Camera = CameraType.defaultValue;
            public Texture2D DepthBuffer = null;
        }

        public class ThicknessProperties
        {
            [Min(0.0f)]
            public float surfaceThickness = 1.0f;
        }

        protected override bool allowInvertedCollision { get { return false; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                foreach (var a in base.attributes)
                    yield return a;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var inputs = base.inputProperties;
                if (surfaceThickness == SurfaceThickness.Custom)
                    inputs = inputs.Concat(PropertiesFromType("ThicknessProperties"));
                return inputs;
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var expressions = base.parameters;

                var fov = expressions.First(e => e.name == "Camera_fieldOfView");
                var aspect = expressions.First(e => e.name == "Camera_aspectRatio");
                var near = expressions.First(e => e.name == "Camera_nearPlane");
                var far = expressions.First(e => e.name == "Camera_farPlane");
                var cameraMatrix = expressions.First(e => e.name == "Camera_transform");

                expressions = expressions.Where(t => !(t.Equals(fov) || t.Equals(aspect) || t.Equals(cameraMatrix)));

                foreach (var e in expressions)
                    yield return e;

                VFXExpression ViewToVFX = cameraMatrix.exp;
                VFXExpression VFXToView = new VFXExpressionInverseMatrix(ViewToVFX);
                VFXExpression ViewToClip = VFXOperatorUtility.GetPerspectiveMatrix(fov.exp, aspect.exp, near.exp, far.exp);
                VFXExpression ClipToView = new VFXExpressionInverseMatrix(ViewToClip);

                yield return new VFXNamedExpression(ViewToVFX, "ViewToVFX");
                yield return new VFXNamedExpression(VFXToView, "VFXToView");
                yield return new VFXNamedExpression(ViewToClip, "ViewToClip");
                yield return new VFXNamedExpression(ClipToView, "ClipToView");
            }
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 viewPos = mul(VFXToView,float4(nextPos,1.0f));

float4 projPos = mul(ViewToClip,float4(viewPos,1.0f));
projPos.xyz /= projPos.w;
float2 aProjPos = abs(projPos.xy);

if (aProjPos.x < 1.0f && aProjPos.y < 1.0f) // visible on screen
{
    float2 uv = projPos.xy * 0.5f + 0.5f;
    float depth = LoadTexture(DepthBuffer,int3(uv*Camera_pixelDimensions, 0)).r;
    #if UNITY_REVERSED_Z
    depth = 1.0f - depth; // reversed z
    #endif

    const float n = Camera_nearPlane;
    const float f = Camera_farPlane;
    float linearEyeDepth = n * f / (depth * (n - f) + f);";

                if (surfaceThickness == SurfaceThickness.Infinite)
                    Source += @"
    if (viewPos.z > linearEyeDepth - radius)";
                else
                    Source += @"
    if (viewPos.z > linearEyeDepth - radius && viewPos.z < linearEyeDepth + radius + surfaceThickness)";

                Source += @"
    {
        const float2 pixelOffset = 2.0f / Camera_pixelDimensions;

        float2 projPos10 = projPos.xy + float2(pixelOffset.x,0.0f);
        float2 projPos01 = projPos.xy + float2(0.0f,pixelOffset.y);

        int2 depthPos10 = clamp(int2((projPos10 * 0.5f + 0.5f) * Camera_pixelDimensions), 0, Camera_pixelDimensions - 1);
        int2 depthPos01 = clamp(int2((projPos01 * 0.5f + 0.5f) * Camera_pixelDimensions), 0, Camera_pixelDimensions - 1);

        float depth10 = LoadTexture(DepthBuffer, int3(depthPos10, 0)).r;
        float depth01 = LoadTexture(DepthBuffer, int3(depthPos01, 0)).r;

        #if UNITY_REVERSED_Z
        depth10 = 1.0f - depth10;
        depth01 = 1.0f - depth01;
        #endif

        float4 vPos10 = mul(ClipToView,float4(projPos10,depth10 * 2.0f - 1.0f,1.0f));
        float4 vPos01 = mul(ClipToView,float4(projPos01,depth01 * 2.0f - 1.0f,1.0f));

        vPos10.xyz /= vPos10.w;
        vPos01.xyz /= vPos01.w;

        viewPos *= linearEyeDepth / viewPos.z; // Position on depth surface

        float3 n = normalize(cross(vPos01.xyz - viewPos,vPos10.xyz - viewPos));
        n = normalize(mul((float3x3)ViewToVFX,n));

        viewPos *= 1.0f - radius / linearEyeDepth; // Push based on radius
        position = mul(ViewToVFX,float4(viewPos,1.0f)).xyz;";

                Source += collisionResponseSource;
                Source += @"
    }
}";

                return Source;
            }
        }
    }
}
