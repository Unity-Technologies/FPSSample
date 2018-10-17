using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Unity.Entities
{
    /// <summary>
    ///     Copy ComponentDataArray to NativeArray Job.
    /// </summary>
    /// <typeparam name="T">Component data type stored in ComponentDataArray to be copied to NativeArray<T></typeparam>
    [BurstCompile]
    public struct CopyComponentData<T> : IJobParallelFor
        where T : struct, IComponentData
    {
        [ReadOnly] public ComponentDataArray<T> Source;
        public NativeArray<T> Results;

        public void Execute(int index)
        {
            Results[index] = Source[index];
        }
    }

    /// <summary>
    ///     Assign Value to each element of NativeArray
    /// </summary>
    /// <typeparam name="T">Type of element in NativeArray</typeparam>
    [BurstCompile]
    public struct MemsetNativeArray<T> : IJobParallelFor
        where T : struct
    {
        public NativeArray<T> Source;
        public T Value;

        // #todo Need equivalent of IJobParallelFor that's per-chunk so we can do memset per chunk here.
        public void Execute(int index)
        {
            Source[index] = Value;
        }
    }

    /// <summary>
    ///     Copy Entities from EntityArray to NativeArray<Entity>
    /// </summary>
    [BurstCompile]
    public struct CopyEntities : IJobParallelFor
    {
        [ReadOnly] public EntityArray Source;
        public NativeArray<Entity> Results;

        public void Execute(int index)
        {
            Results[index] = Source[index];
        }
    }
}
