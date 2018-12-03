using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

[DisableAutoCreation]
public class HandleFanSpawns : InitializeComponentGroupSystem<Fan, HandleFanSpawns.Initialized>
{
    ComponentGroup Group;
    
    public struct Initialized : IComponentData {}
    
    public HandleFanSpawns(GameWorld world) : base(world) { }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(Fan), ComponentType.Subtractive<DespawningEntity>());
    }
    
    protected override void Initialize(ref ComponentGroup group)
    {
        // Get all components of type, not just spawned/de-spawned ones
        var componentArray = Group.GetComponentArray<Fan>();
        FanSystem.SetupFanComponents(ref componentArray);
    }
}

[DisableAutoCreation]
public class HandleFanDespawns : DeinitializeComponentGroupSystem<Fan>
{
    ComponentGroup Group;

    public HandleFanDespawns(GameWorld world) : base(world) { }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(Fan), ComponentType.Subtractive<DespawningEntity>());
    }
    
    protected override void Deinitialize(ref ComponentGroup group)
    {
        // Get all components of type, not just spawned/de-spawned ones
        var componentArray = Group.GetComponentArray<Fan>();
        FanSystem.SetupFanComponents(ref componentArray);
    }
}



public class FanSystem
{
    public FanSystem(GameWorld world)
    {
        m_HandleFanSpawns = world.GetECSWorld().CreateManager<HandleFanSpawns>(world);
        m_HandleFanDespawns = world.GetECSWorld().CreateManager<HandleFanDespawns>(world);
        m_World = world;

        s_SourceJoints = new TransformAccessArray(k_MaxFanJoints / 2, 1);
        s_SourceRotations = new NativeArray<quaternion>(k_MaxFanJoints / 2, Allocator.Persistent);
        s_FanJoints = new TransformAccessArray(k_MaxFanJoints, 1);
    }

    public void ShutDown()
    {
        Complete();
        s_SourceJoints.Dispose();
        s_SourceRotations.Dispose();
        s_FanJoints.Dispose();

        m_World.GetECSWorld().DestroyManager(m_HandleFanSpawns);
        m_World.GetECSWorld().DestroyManager(m_HandleFanDespawns);
    }

    public void HandleSpawning()
    {
        m_HandleFanSpawns.Update();
    }

    public void HandleDespawning()
    {
        m_HandleFanDespawns.Update();
    }
    
    
    public static void SetupFanComponents(ref ComponentArray<Fan> fanComponents)
    {
        s_SourceJoints.SetTransforms(null);
        s_FanJoints.SetTransforms(null);
        
        for (var i = 0; i < fanComponents.Length; i++)
        {
            foreach (var fanData in fanComponents[i].fanDatas)
            {
                AddFanJoint(fanData); 
            }
        }
    }

    public static void AddFanJoint(Fan.FanData fanData)
    {
        GameDebug.Assert(s_FanJoints.length < k_MaxFanJoints, "You are trying to add more fan joints then there is allocated space for.");

        if (fanData.HasValidData())
        {
            s_SourceJoints.Add(fanData.driverA);
            s_SourceJoints.Add(fanData.driverB);
            s_FanJoints.Add(fanData.driven);  
        }
    }

    public JobHandle Schedule()
    {
        Profiler.BeginSample("FanSystem.Schedule");
        var readJob = new ReadJob(s_SourceRotations);
        var readHandle = readJob.Schedule(s_SourceJoints);

        var writeJob = new WriteJob(s_SourceRotations);
        m_WriteHandle = writeJob.Schedule(s_FanJoints, readHandle);

        Profiler.EndSample();
        return m_WriteHandle;
    }

    public JobHandle Schedule(JobHandle dependency)
    {
        Profiler.BeginSample("FanSystem.Schedule");

        var readJob = new ReadJob(s_SourceRotations);
        var readHandle = readJob.Schedule(s_SourceJoints, dependency);

        var writeJob = new WriteJob(s_SourceRotations);
        m_WriteHandle = writeJob.Schedule(s_FanJoints, readHandle);

        Profiler.EndSample();
        return m_WriteHandle;
    }


    public void Complete()
    {
        m_WriteHandle.Complete();
    }

    [BurstCompile(CompileSynchronously = true)]
    struct ReadJob : IJobParallelForTransform
    {
        NativeArray<quaternion> m_SourceRotations;

        public ReadJob(NativeArray<quaternion> sourceRotations)
        {
            m_SourceRotations = sourceRotations;
        }

        public void Execute(int i, TransformAccess transform)
        {
            m_SourceRotations[i] = transform.rotation;
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct WriteJob : IJobParallelForTransform
    {
        [ReadOnly]
        NativeArray<quaternion> m_SourceRotations;

        public WriteJob(NativeArray<quaternion> sourceRotations)
        {
            m_SourceRotations = sourceRotations;
        }

        public void Execute(int i, TransformAccess transform)
        {
            transform.rotation = math.slerp(m_SourceRotations[i * 2], m_SourceRotations[i * 2 + 1], 0.5f);
        }
    }

    const int k_MaxFanJoints = 2000;

    static TransformAccessArray s_SourceJoints;
    static NativeArray<quaternion> s_SourceRotations;
    static TransformAccessArray s_FanJoints;

    protected GameWorld m_World;
    readonly HandleFanSpawns m_HandleFanSpawns;
    readonly HandleFanDespawns m_HandleFanDespawns;
                        
    JobHandle m_WriteHandle;
}
