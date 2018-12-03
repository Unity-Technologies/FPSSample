using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;


[DisableAutoCreation]
public class HandleTwistSpawns : InitializeComponentGroupSystem<Twist, HandleTwistSpawns.Initialized>
{
    ComponentGroup Group;

    public struct Initialized : IComponentData {}
    
    public HandleTwistSpawns(GameWorld world) : base(world) { }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(Twist), ComponentType.Subtractive<DespawningEntity>());
    }

    protected override void Initialize(ref ComponentGroup group)
    {
        // Get all components of type, not just spawned/de-spawned ones
        var array = Group.GetComponentArray<Twist>();
        TwistSystem.SetupTwistComponents(ref array);
    }
}

[DisableAutoCreation]
public class HandleTwistDespawns : DeinitializeComponentGroupSystem<Twist>
{
    ComponentGroup Group;
    
    public HandleTwistDespawns(GameWorld world) : base(world) { }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(Twist), ComponentType.Subtractive<DespawningEntity>());
    }
    
    protected override void Deinitialize(ref ComponentGroup group)
    {
        // Get all components of type, not just spawned/de-spawned ones
        var array = Group.GetComponentArray<Twist>();
        TwistSystem.SetupTwistComponents(ref array);
    }
}


public class TwistSystem
{
    public TwistSystem(GameWorld world)
    {
        m_HandleTwistSpawns = world.GetECSWorld().CreateManager<HandleTwistSpawns>(world);
        m_HandleTwistDespawns = world.GetECSWorld().CreateManager<HandleTwistDespawns>(world);
        m_World = world;

        s_SourceJoints = new TransformAccessArray(k_MaxSetups, 1);
        s_SourceData = new NativeArray<SourceData>(k_MaxSetups, Allocator.Persistent);
        s_TwistJoints = new TransformAccessArray(k_MaxTwistJoints, 1);
        s_TargetData = new NativeArray<TargetData>(k_MaxTwistJoints, Allocator.Persistent);
    }

    public void ShutDown()
    {
        Complete();
        s_SourceData.Dispose();
        s_SourceJoints.Dispose();
        s_TwistJoints.Dispose();
        s_TargetData.Dispose();
        m_World.GetECSWorld().DestroyManager(m_HandleTwistSpawns);
        m_World.GetECSWorld().DestroyManager(m_HandleTwistDespawns);
    }

    public void HandleSpawning()
    {
        m_HandleTwistSpawns.Update();
    }

    public void HandleDespawning()
    {
        m_HandleTwistDespawns.Update();
    }
              
    public static void SetupTwistComponents(ref ComponentArray<Twist> twistComponents)
    {
        s_SetupIndex = 0;
        s_TwistIndex = 0;
        s_SourceJoints.SetTransforms(null);
        s_TwistJoints.SetTransforms(null);

        for (var i = 0; i < twistComponents.Length; i++)
        {
            for (var j = 0; j < twistComponents[i].twistChains.Count; j++)
            {
                AddTwistChain(twistComponents[i].twistChains[j]);
            }
        }
    }

    static void AddTwistChain(Twist.TwistChain twistChain)
    {
        if (!twistChain.HasValidData())
            return;

        GameDebug.Assert(s_SourceJoints.length < k_MaxSetups, "You are trying to add more twist joint chains then there is allocated space for.");
        GameDebug.Assert(s_TwistJoints.length + twistChain.twistJoints.Count <= k_MaxTwistJoints, "You are trying to add more twist joint then there is allocated space for.");

        s_SourceJoints.Add(twistChain.driver);
        s_SourceData[s_SetupIndex] = new SourceData
        {
            bindpose = twistChain.bindpose
        };
        
        for (var j = 0; j < twistChain.twistJoints.Count; j++)
        {
            if (twistChain.twistJoints[j].joint != null)
            {
                s_TwistJoints.Add(twistChain.twistJoints[j].joint);
                s_TargetData[s_TwistIndex] = new TargetData
                {
                    sourceIndex = s_SetupIndex,
                    twistFactor = twistChain.twistJoints[j].factor
                };
                s_TwistIndex++;
            }
        }

        s_SetupIndex++;
    }

    public JobHandle Schedule()
    {
        Profiler.BeginSample("TwistSystem.Schedule");

            var readJob = new ReadJob(s_SourceData);
            var readHandle = readJob.Schedule(s_SourceJoints);

            var writeJob = new WriteJob(s_SourceData, s_TargetData);
            m_WriteHandle = writeJob.Schedule(s_TwistJoints, readHandle);

            Profiler.EndSample();

        return m_WriteHandle;
    }

    public JobHandle Schedule(JobHandle dependency)
    {

        Profiler.BeginSample("TwistSystem.Schedule");

        var readJob = new ReadJob(s_SourceData);
        var readHandle = readJob.Schedule(s_SourceJoints, dependency);

        var writeJob = new WriteJob(s_SourceData, s_TargetData);
        m_WriteHandle = writeJob.Schedule(s_TwistJoints, readHandle);

        Profiler.EndSample();

        return m_WriteHandle;
    }

    public void Complete()
    {
        m_WriteHandle.Complete();
    }

    struct SourceData
    {
        public quaternion bindpose;
        public quaternion twist;
    }

    struct TargetData
    {
        public int sourceIndex;
        public float twistFactor;
    }

    [BurstCompile(CompileSynchronously = true)]
    struct ReadJob : IJobParallelForTransform
    {
        NativeArray<SourceData> m_SourceData;

        public ReadJob(NativeArray<SourceData> sourceData)
        {
            m_SourceData = sourceData;
        }

        public void Execute(int i, TransformAccess transform)
        {
            var delta = math.inverse(m_SourceData[i].bindpose) * transform.localRotation;
            var data = m_SourceData[i];

            data.twist = new quaternion(0.0f, delta.y, 0.0f, delta.w);
            m_SourceData[i] = data;
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct WriteJob : IJobParallelForTransform
    {
        [ReadOnly]
        NativeArray<SourceData> m_SourceData;
        [ReadOnly]
        NativeArray<TargetData> m_TargetData;

        public WriteJob(NativeArray<SourceData> sourceData, NativeArray<TargetData> targetData)
        {
            m_SourceData = sourceData;
            m_TargetData = targetData;
        }

        public void Execute(int i, TransformAccess transform)
        {
            var twistRotation = math.slerp(quaternion.identity, m_SourceData[m_TargetData[i].sourceIndex].twist, m_TargetData[i].twistFactor);
            transform.localRotation = twistRotation;
        }
    }

    const int k_MaxSetups = 1200;
    const int k_MaxTwistJoints = 4800;

    static int s_SetupIndex;
    static int s_TwistIndex;

    // TODO: (sunke) Move these onto unique component in the world?
    static TransformAccessArray s_SourceJoints;
    static TransformAccessArray s_TwistJoints;
    static NativeArray<SourceData> s_SourceData;
    static NativeArray<TargetData> s_TargetData;

    JobHandle m_WriteHandle;

    protected GameWorld m_World;
    readonly HandleTwistSpawns m_HandleTwistSpawns;   
    readonly HandleTwistDespawns m_HandleTwistDespawns;
}
