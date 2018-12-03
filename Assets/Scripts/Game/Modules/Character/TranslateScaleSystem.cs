using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using Unity.Mathematics;


[DisableAutoCreation]
public class HandleTranslateScaleSpawns : InitializeComponentGroupSystem<TranslateScale, HandleTranslateScaleSpawns.Initialized>
{
    ComponentGroup Group;

    public struct Initialized : IComponentData {}
    
    public HandleTranslateScaleSpawns(GameWorld world) : base(world) { }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(TranslateScale), ComponentType.Subtractive<DespawningEntity>());
    }
    
    protected override void Initialize(ref ComponentGroup group)
    {
        // Get all components of type, not just spawned/de-spawned ones
        var array = Group.GetComponentArray<TranslateScale>();
        TranslateScaleSystem.SetupTranslateScaleComponents(ref array);
    }
}

[DisableAutoCreation]
public class HandleTranslateScaleDespawns : DeinitializeComponentGroupSystem<TranslateScale>
{
    ComponentGroup Group;

    public HandleTranslateScaleDespawns(GameWorld world) : base(world) { }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(TranslateScale), ComponentType.Subtractive<DespawningEntity>());
    }
    
    
    protected override void Deinitialize(ref ComponentGroup group)
    {
        // Get all components of type, not just spawned/de-spawned ones
        var array = Group.GetComponentArray<TranslateScale>();
        TranslateScaleSystem.SetupTranslateScaleComponents(ref array);
    }
}


public class TranslateScaleSystem
{
    public TranslateScaleSystem(GameWorld world)
    {
        m_HandleTranslateScaleSpawns = world.GetECSWorld().CreateManager<HandleTranslateScaleSpawns>(world);
        m_HandleTranslateScaleDespawns = world.GetECSWorld().CreateManager<HandleTranslateScaleDespawns>(world);
        m_World = world;

        s_SourceJoints = new TransformAccessArray(k_MaxDrivers, 1);
        s_SourceData = new NativeArray<SourceData>(k_MaxDrivers, Allocator.Persistent);
        s_DrivenJoints = new TransformAccessArray(k_MaxDrivenJoints, 1);
        s_TargetData = new NativeArray<TargetData>(k_MaxDrivenJoints, Allocator.Persistent);
    }

    public void ShutDown()
    {
        Complete();
        s_SourceData.Dispose();
        s_SourceJoints.Dispose();
        s_DrivenJoints.Dispose();
        s_TargetData.Dispose();
        m_World.GetECSWorld().DestroyManager(m_HandleTranslateScaleSpawns);
        m_World.GetECSWorld().DestroyManager(m_HandleTranslateScaleDespawns);
    }

    public void HandleSpawning()
    {
        m_HandleTranslateScaleSpawns.Update();
    }

    public void HandleDepawning()
    {
        m_HandleTranslateScaleDespawns.Update();
    }

    public static void SetupTranslateScaleComponents(ref ComponentArray<TranslateScale> translateScaleComponents)
    {
        s_DriverIndex = 0;
        s_DrivenIndex = 0;
        s_SourceJoints.SetTransforms(null);
        s_DrivenJoints.SetTransforms(null);

        for (var i = 0; i < translateScaleComponents.Length; i++)
        {
            for (var j = 0; j < translateScaleComponents[i].chains.Count; j++)
            {
                AddTranslateScaleChains(translateScaleComponents[i].chains[j]);
            }
        }
    }

    static void AddTranslateScaleChains(TranslateScale.TranslateScaleChain translateScaleChain)
    {
        if (!translateScaleChain.HasValidData())
            return;

        GameDebug.Assert(s_SourceJoints.length < k_MaxDrivers, "You are trying to add more translate scale chains then there is allocated space for.");
        GameDebug.Assert(s_DrivenJoints.length + translateScaleChain.drivenJoints.Count <= k_MaxDrivenJoints, "You are trying to add more driven joint then there is allocated space for.");

        s_SourceJoints.Add(translateScaleChain.driver);
        s_SourceData[s_DriverIndex] = new SourceData
        {
            bindpose = translateScaleChain.bindpose
        };

        for (var j = 0; j < translateScaleChain.drivenJoints.Count; j++)
        {
            if (translateScaleChain.drivenJoints[j].joint != null)
            {
                s_DrivenJoints.Add(translateScaleChain.drivenJoints[j].joint);
                s_TargetData[s_DrivenIndex] = new TargetData
                {
                    sourceIndex = s_DriverIndex,
                    bindpose = translateScaleChain.drivenJoints[j].bindpose,
                    stretchFactor = translateScaleChain.drivenJoints[j].strectchFactor,
                    scaleFactor = translateScaleChain.drivenJoints[j].scaleFactor
                    
                };
                s_DrivenIndex++;
            }
        }

        s_DriverIndex++;
    }

    public JobHandle Schedule()
    {
        Profiler.BeginSample("TranslateScaleSystem.Schedule");

            var readJob = new ReadJob(s_SourceData);
            var readHandle = readJob.Schedule(s_SourceJoints);

            var writeJob = new WriteJob(s_SourceData, s_TargetData);
            m_WriteHandle = writeJob.Schedule(s_DrivenJoints, readHandle);

            Profiler.EndSample();

        return m_WriteHandle;
    }

    public JobHandle Schedule(JobHandle dependency)
    {

        Profiler.BeginSample("TranslateScaleSystem.Schedule");

        var readJob = new ReadJob(s_SourceData);
        var readHandle = readJob.Schedule(s_SourceJoints, dependency);

        var writeJob = new WriteJob(s_SourceData, s_TargetData);
        m_WriteHandle = writeJob.Schedule(s_DrivenJoints, readHandle);

        Profiler.EndSample();

        return m_WriteHandle;
    }

    public void Complete()
    {
        m_WriteHandle.Complete();
    }

    struct SourceData
    {
        public Vector3 bindpose;
        public float stretchOffset;
        public float stretchFactor;
    }

    struct TargetData
    {
        public int sourceIndex;
        public float3 bindpose;
        public float scaleFactor;
        public float stretchFactor;
    }

    
    // TODO: Check the performance delta by using burst
    [Unity.Burst.BurstCompile(CompileSynchronously = true)]
    struct ReadJob : IJobParallelForTransform
    {
        NativeArray<SourceData> m_SourceData;

        public ReadJob(NativeArray<SourceData> sourceData)
        {
            m_SourceData = sourceData;
        }

        public void Execute(int i, TransformAccess transform)
        {
            var data = m_SourceData[i];
            data.stretchOffset = transform.localPosition.y - m_SourceData[i].bindpose.y;
            data.stretchFactor = transform.localPosition.y / m_SourceData[i].bindpose.y;
            m_SourceData[i] = data;
        }
    }

    [Unity.Burst.BurstCompile(CompileSynchronously = true)]
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
            // todo: Get rid of any unneeded allocations
            // todo: new style math and burst?                                                
            transform.localPosition = m_TargetData[i].bindpose + new float3(0f, m_SourceData[m_TargetData[i].sourceIndex].stretchOffset * m_TargetData[i].stretchFactor, 0f);
            var volumeScale = -m_TargetData[i].scaleFactor * m_SourceData[m_TargetData[i].sourceIndex].stretchFactor  + 1 + m_TargetData[i].scaleFactor;
            transform.localScale = new Vector3(volumeScale, 1f, volumeScale);
        }
    }

    const int k_MaxDrivers = 1200;
    const int k_MaxDrivenJoints = 4800;

    static int s_DriverIndex;
    static int s_DrivenIndex;

    // TODO: (sunke) Move these onto unique component in the world?
    static TransformAccessArray s_SourceJoints;
    static TransformAccessArray s_DrivenJoints;
    static NativeArray<SourceData> s_SourceData;
    static NativeArray<TargetData> s_TargetData;

    JobHandle m_WriteHandle;

    protected GameWorld m_World;
    readonly HandleTranslateScaleSpawns m_HandleTranslateScaleSpawns;
    readonly HandleTranslateScaleDespawns m_HandleTranslateScaleDespawns;
}
