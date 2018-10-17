using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Unity.Rendering
{
    public class LODGroupSystem : JobComponentSystem
    {
        public Camera ActiveCamera;

        [Inject]
        ComponentDataFromEntity<ActiveLODGroupMask> m_ActiveLODGroupMask;
        
        [BurstCompile]
        struct LODGroupJob : IJobProcessComponentData<MeshLODGroupComponent, ActiveLODGroupMask>
        {
            public LODGroupExtensions.LODParams LODParams;
            [ReadOnly]
            public ComponentDataFromEntity<ActiveLODGroupMask> HLODActiveMask;  
            
            unsafe public void Execute([ReadOnly]ref MeshLODGroupComponent lodGroup, [ReadOnly]ref ActiveLODGroupMask activeMask)
            {
                if (lodGroup.ParentGroup != Entity.Null)
                {
                    var parentMask = HLODActiveMask[lodGroup.ParentGroup].LODMask;
                    if ((parentMask & lodGroup.ParentMask) == 0)
                    {
                        activeMask.LODMask = 0;
                        return;
                    }
                }

                activeMask.LODMask = LODGroupExtensions.CalculateCurrentLODMask(lodGroup.LODDistances, lodGroup.WorldReferencePoint, ref LODParams);
            }
        }

        //@TODO: Would be nice if I could specify additional filter without duplicating this code...
        [RequireComponentTag(typeof(HLODComponent))]
        [BurstCompile]
        struct HLODGroupJob : IJobProcessComponentData<MeshLODGroupComponent, ActiveLODGroupMask>
        {
            public LODGroupExtensions.LODParams LODParams;  
            
            unsafe public void Execute([ReadOnly]ref MeshLODGroupComponent lodGroup, [ReadOnly]ref ActiveLODGroupMask activeMask)
            {
                activeMask.LODMask = LODGroupExtensions.CalculateCurrentLODMask(lodGroup.LODDistances, lodGroup.WorldReferencePoint, ref LODParams);
            }
        }

        protected override JobHandle OnUpdate(JobHandle dependency)
        {
            if (ActiveCamera != null)
            {
                var hlodJob = new HLODGroupJob { LODParams = LODGroupExtensions.CalculateLODParams(ActiveCamera)};
                dependency = hlodJob.Schedule(this, dependency);
                
                var lodJob = new LODGroupJob { LODParams = LODGroupExtensions.CalculateLODParams(ActiveCamera), HLODActiveMask = m_ActiveLODGroupMask };
                dependency = lodJob.Schedule(this, dependency);
            }

            return dependency;
        }
    }
}