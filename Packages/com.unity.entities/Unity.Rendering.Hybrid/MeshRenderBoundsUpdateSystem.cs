using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Rendering
{
    [UpdateAfter(typeof(EndFrameBarrier))]
    [UpdateBefore(typeof(MeshInstanceRendererSystem))]
    public class MeshRenderBoundsUpdateSystem : JobComponentSystem
    {
        [RequireSubtractiveComponent(typeof(Frozen))]
        [BurstCompile]
        struct BoundsJob : IJobProcessComponentData<MeshRenderBounds, LocalToWorld, WorldMeshRenderBounds>
        {
            public void Execute([ChangedFilter] [ReadOnly] ref MeshRenderBounds inLocalBounds, [ChangedFilter] [ReadOnly] ref LocalToWorld inTransform, ref WorldMeshRenderBounds outWorldBounds)
            {
                outWorldBounds.Center = math.mul(inTransform.Value, new float4(inLocalBounds.Center,1.0f)).xyz;
                float3 scaleSqr = new float3(
                    math.lengthSquared(inTransform.Value.c0),
                    math.lengthSquared(inTransform.Value.c1),
                    math.lengthSquared(inTransform.Value.c2)
                );
                float largestScaleSqr = math.cmax(scaleSqr);
                float largestScale = math.sqrt(largestScaleSqr);
                outWorldBounds.Radius = largestScale * inLocalBounds.Radius;
            }
        }

        protected override JobHandle OnUpdate(JobHandle dependency)
        {
            var boundsJob = new BoundsJob { };
            dependency = boundsJob.Schedule(this, dependency);
            
            return dependency;
        }
    }
}
