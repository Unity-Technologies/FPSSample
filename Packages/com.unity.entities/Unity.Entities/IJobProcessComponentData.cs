using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Assertions;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Scripting;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace Unity.Entities
{
    //@TODO: What about change or add?
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ChangedFilterAttribute : Attribute
    {
    }
    
    [AttributeUsage(AttributeTargets.Struct)]
    public class RequireComponentTagAttribute : Attribute
    {
        public Type[] TagComponents;

        public RequireComponentTagAttribute(params Type[] tagComponents)
        {
            TagComponents = tagComponents;
        }
    }

    [AttributeUsage(AttributeTargets.Struct)]
    public class RequireSubtractiveComponentAttribute : Attribute
    {
        public Type[] SubtractiveComponents;

        public RequireSubtractiveComponentAttribute(params Type[] subtractiveComponents)
        {
            SubtractiveComponents = subtractiveComponents;
        }
    }

    //@TODO: It would be nice to get rid of these interfaces completely.
    //Right now implementation needs it, but they pollute public API in annoying ways.

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBaseJobProcessComponentData
    {
    }
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBaseJobProcessComponentData_1 : IBaseJobProcessComponentData
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBaseJobProcessComponentData_2 : IBaseJobProcessComponentData
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBaseJobProcessComponentData_3 : IBaseJobProcessComponentData
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBaseJobProcessComponentData_4 : IBaseJobProcessComponentData
    {
    }
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBaseJobProcessComponentData_1_WE : IBaseJobProcessComponentData
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBaseJobProcessComponentData_2_WE : IBaseJobProcessComponentData
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBaseJobProcessComponentData_3_WE : IBaseJobProcessComponentData
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBaseJobProcessComponentData_4_WE : IBaseJobProcessComponentData
    {
    }

    
    [JobProducerType(typeof(JobProcessComponentDataExtensions.JobStruct_Process1<,>))]
    public interface IJobProcessComponentData<T0> : IBaseJobProcessComponentData_1
        where T0 : struct, IComponentData
    {
        void Execute(ref T0 data);
    }

    [JobProducerType(typeof(JobProcessComponentDataExtensions.JobStruct_Process2<,,>))]
    public interface IJobProcessComponentData<T0, T1> : IBaseJobProcessComponentData_2
        where T0 : struct, IComponentData
        where T1 : struct, IComponentData
    {
        void Execute(ref T0 data0, ref T1 data1);
    }

    [JobProducerType(typeof(JobProcessComponentDataExtensions.JobStruct_Process3<,,,>))]
    public interface IJobProcessComponentData<T0, T1, T2> : IBaseJobProcessComponentData_3
        where T0 : struct, IComponentData
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
    {
        void Execute(ref T0 data0, ref T1 data1, ref T2 data2);
    }

    [JobProducerType(typeof(JobProcessComponentDataExtensions.JobStruct_Process4<,,,,>))]
    public interface IJobProcessComponentData<T0, T1, T2, T3> : IBaseJobProcessComponentData_4
        where T0 : struct, IComponentData
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        where T3 : struct, IComponentData
    {
        void Execute(ref T0 data0, ref T1 data1, ref T2 data2, ref T3 data3);
    }

    
    [JobProducerType(typeof(JobProcessComponentDataExtensions.JobStruct_Process1_WE<,>))]
    public interface IJobProcessComponentDataWithEntity<T0> : IBaseJobProcessComponentData_1_WE
        where T0 : struct, IComponentData
    {
        void Execute(Entity entity, int index, ref T0 data);
    }

    [JobProducerType(typeof(JobProcessComponentDataExtensions.JobStruct_Process2_WE<,,>))]
    public interface IJobProcessComponentDataWithEntity<T0, T1> : IBaseJobProcessComponentData_2_WE
        where T0 : struct, IComponentData
        where T1 : struct, IComponentData
    {
        void Execute(Entity entity, int index, ref T0 data0, ref T1 data1);
    }

    [JobProducerType(typeof(JobProcessComponentDataExtensions.JobStruct_Process3_WE<,,,>))]
    public interface IJobProcessComponentDataWithEntity<T0, T1, T2> : IBaseJobProcessComponentData_3_WE
        where T0 : struct, IComponentData
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
    {
        void Execute(Entity entity, int index, ref T0 data0, ref T1 data1, ref T2 data2);
    }

    [JobProducerType(typeof(JobProcessComponentDataExtensions.JobStruct_Process4_WE<,,,,>))]
    public interface IJobProcessComponentDataWithEntity<T0, T1, T2, T3> : IBaseJobProcessComponentData_4_WE
        where T0 : struct, IComponentData
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        where T3 : struct, IComponentData
    {
        void Execute(Entity entity, int index, ref T0 data0, ref T1 data1, ref T2 data2, ref T3 data3);
    }
    
    internal struct JobProcessComponentDataCache
    {
        public IntPtr JobReflectionData;
        public IntPtr JobReflectionDataParallelFor;
        public ComponentType[] Types;
        public ComponentType[] FilterChanged;

        public int ProcessTypesCount;

        public ComponentGroup ComponentGroup;
        public ComponentSystemBase ComponentSystem;
    }

    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessIterationData
    {
        public ComponentChunkIterator Iterator;
        public int IndexInGroup0;
        public int IndexInGroup1;
        public int IndexInGroup2;
        public int IndexInGroup3;

        public int IsReadOnly0;
        public int IsReadOnly1;
        public int IsReadOnly2;
        public int IsReadOnly3;
        
        public bool m_IsParallelFor;

        public int m_Length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS

        public int m_MinIndex;
        public int m_MaxIndex;

#pragma warning disable 414
        public int m_SafetyReadOnlyCount;
        public int m_SafetyReadWriteCount;
        public AtomicSafetyHandle m_Safety0;
        public AtomicSafetyHandle m_Safety1;
        public AtomicSafetyHandle m_Safety2;
        public AtomicSafetyHandle m_Safety3;
#pragma warning restore
#endif
    }

    internal static class IJobProcessComponentDataUtility
    {
        public static ComponentType[] GetComponentTypes(Type jobType)
        {
            var interfaceType = GetIJobProcessComponentDataInterface(jobType);
            if (interfaceType != null)
            {
                int temp;
                ComponentType[] temp2;
                return GetComponentTypes(jobType, interfaceType, out temp, out temp2);
            }

            return null;
        }

        private static ComponentType[] GetComponentTypes(Type jobType, Type interfaceType, out int processCount,
            out ComponentType[] changedFilter)
        {
            var genericArgs = interfaceType.GetGenericArguments();

            var executeMethodParameters = jobType.GetMethod("Execute").GetParameters();
            
            var componentTypes = new List<ComponentType>();
            var changedFilterTypes = new List<ComponentType>();

            
            // void Execute(Entity entity, int index, ref T0 data0, ref T1 data1, ref T2 data2);
            // First two parameters are optional, depending on the interface name used.
            var methodParameterOffset = genericArgs.Length != executeMethodParameters.Length ? 2 : 0;
            
            for (var i = 0; i < genericArgs.Length; i++)
            {
                var isReadonly = executeMethodParameters[i + methodParameterOffset].GetCustomAttribute(typeof(ReadOnlyAttribute)) != null;
                
                var type = new ComponentType(genericArgs[i],
                    isReadonly ? ComponentType.AccessMode.ReadOnly : ComponentType.AccessMode.ReadWrite);
                componentTypes.Add(type);

                var isChangedFilter = executeMethodParameters[i + methodParameterOffset].GetCustomAttribute(typeof(ChangedFilterAttribute)) != null;
                if (isChangedFilter)
                    changedFilterTypes.Add(type);
            }

            var subtractive = jobType.GetCustomAttribute<RequireSubtractiveComponentAttribute>();
            if (subtractive != null)
                foreach (var type in subtractive.SubtractiveComponents)
                    componentTypes.Add(ComponentType.Subtractive(type));

            var requiredTags = jobType.GetCustomAttribute<RequireComponentTagAttribute>();
            if (requiredTags != null)
                foreach (var type in requiredTags.TagComponents)
                    componentTypes.Add(ComponentType.ReadOnly(type));

            processCount = genericArgs.Length;
            changedFilter = changedFilterTypes.ToArray();
            return componentTypes.ToArray();
        }

        private static IntPtr GetJobReflection(Type jobType, Type wrapperJobType, Type interfaceType,
            bool isIJobParallelFor)
        {
            Assert.AreNotEqual(null, wrapperJobType);
            Assert.AreNotEqual(null, interfaceType);

            var genericArgs = interfaceType.GetGenericArguments();

            var jobTypeAndGenericArgs = new List<Type>();
            jobTypeAndGenericArgs.Add(jobType);
            jobTypeAndGenericArgs.AddRange(genericArgs);
            var resolvedWrapperJobType = wrapperJobType.MakeGenericType(jobTypeAndGenericArgs.ToArray());

            object[] parameters = {isIJobParallelFor ? JobType.ParallelFor : JobType.Single};
            var reflectionDataRes = resolvedWrapperJobType.GetMethod("Initialize").Invoke(null, parameters);
            return (IntPtr) reflectionDataRes;
        }

        private static Type GetIJobProcessComponentDataInterface(Type jobType)
        {
            foreach (var iType in jobType.GetInterfaces())
                if (iType.Assembly == typeof(IBaseJobProcessComponentData).Assembly &&
                    iType.Name.StartsWith("IJobProcessComponentData"))
                    return iType;

            return null;
        }

        internal static void PrepareComponentGroup(ComponentSystemBase system, Type jobType)
        {
            var iType = GetIJobProcessComponentDataInterface(jobType);
            
            ComponentType[] filterChanged;
            int processTypesCount;
            var types = GetComponentTypes(jobType, iType, out processTypesCount, out filterChanged);
            system.GetComponentGroupInternal(types);
        }

        internal static unsafe void Initialize(ComponentSystemBase system, Type jobType, Type wrapperJobType,
            bool isParallelFor, ref JobProcessComponentDataCache cache, out ProcessIterationData iterator)
        {
            if (isParallelFor && cache.JobReflectionDataParallelFor == IntPtr.Zero ||
                !isParallelFor && cache.JobReflectionData == IntPtr.Zero)
            {
                var iType = GetIJobProcessComponentDataInterface(jobType);
                if (cache.Types == null)
                    cache.Types = GetComponentTypes(jobType, iType, out cache.ProcessTypesCount,
                        out cache.FilterChanged);

                var res = GetJobReflection(jobType, wrapperJobType, iType, isParallelFor);

                if (isParallelFor)
                    cache.JobReflectionDataParallelFor = res;
                else
                    cache.JobReflectionData = res;
            }

            if (cache.ComponentSystem != system)
            {
                cache.ComponentGroup = system.GetComponentGroupInternal(cache.Types);
                if (cache.FilterChanged.Length != 0)
                    cache.ComponentGroup.SetFilterChanged(cache.FilterChanged);
                else
                    cache.ComponentGroup.ResetFilter();

                cache.ComponentSystem = system;
            }

            var group = cache.ComponentGroup;

            // Readonly
            iterator.IsReadOnly0 = iterator.IsReadOnly1 = iterator.IsReadOnly2 = iterator.IsReadOnly3 = 0;
            fixed (int* isReadOnly = &iterator.IsReadOnly0)
            {
                for (var i = 0; i != cache.ProcessTypesCount; i++)
                    isReadOnly[i] = cache.Types[i].AccessModeType == ComponentType.AccessMode.ReadOnly ? 1 : 0;
            }

            // Iterator
            group.GetComponentChunkIterator(out iterator.Iterator);

            iterator.IndexInGroup0 = iterator.IndexInGroup1 = iterator.IndexInGroup2 = iterator.IndexInGroup3 = -1;
            fixed (int* groupIndices = &iterator.IndexInGroup0)
            {
                for (var i = 0; i != cache.ProcessTypesCount; i++)
                    groupIndices[i] = group.GetIndexInComponentGroup(cache.Types[i].TypeIndex);
            }

            iterator.m_IsParallelFor = isParallelFor;
            iterator.m_Length = group.CalculateNumberOfChunksWithoutFiltering();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            iterator.m_MaxIndex = iterator.m_Length - 1;
            iterator.m_MinIndex = 0;

            // Safety
            iterator.m_Safety0 = iterator.m_Safety1 = iterator.m_Safety2 = iterator.m_Safety3 = default(AtomicSafetyHandle);

            iterator.m_SafetyReadOnlyCount = 0;
            fixed (AtomicSafetyHandle* safety = &iterator.m_Safety0)
            {
                for (var i = 0; i != cache.ProcessTypesCount; i++)
                    if (cache.Types[i].AccessModeType == ComponentType.AccessMode.ReadOnly)
                    {
                        safety[iterator.m_SafetyReadOnlyCount] = group.GetSafetyHandle(group.GetIndexInComponentGroup(cache.Types[i].TypeIndex));
                        iterator.m_SafetyReadOnlyCount++;
                    }
            }

            iterator.m_SafetyReadWriteCount = 0;
            fixed (AtomicSafetyHandle* safety = &iterator.m_Safety0)
            {
                for (var i = 0; i != cache.ProcessTypesCount; i++)
                    if (cache.Types[i].AccessModeType == ComponentType.AccessMode.ReadWrite)
                    {
                        safety[iterator.m_SafetyReadOnlyCount + iterator.m_SafetyReadWriteCount] = group.GetSafetyHandle(group.GetIndexInComponentGroup(cache.Types[i].TypeIndex));
                        iterator.m_SafetyReadWriteCount++;
                    }
            }

            Assert.AreEqual(cache.ProcessTypesCount, iterator.m_SafetyReadWriteCount + iterator.m_SafetyReadOnlyCount);
#endif
        }
    }

    public static class JobProcessComponentDataExtensions
    {
        public static ComponentGroup GetComponentGroupForIJobProcessComponentData(this ComponentSystemBase system, Type jobType)
        {
            var types = IJobProcessComponentDataUtility.GetComponentTypes(jobType);
            if (types != null)
                return system.GetComponentGroupInternal(types);
            else
                return null;
        }

        //NOTE: It would be much better if C# could resolve the branch with generic resolving,
        //      but apparently the interface constraint is not enough..

        public static void PrepareComponentGroup<T>(this T jobData, ComponentSystemBase system)
            where T : struct, IBaseJobProcessComponentData
        {
            IJobProcessComponentDataUtility.PrepareComponentGroup(system, typeof(T));
        }

        public static JobHandle Schedule<T>(this T jobData, ComponentSystemBase system, JobHandle dependsOn = default(JobHandle))
            where T : struct, IBaseJobProcessComponentData
        {
            var typeT = typeof(T);
            if (typeof(IBaseJobProcessComponentData_1).IsAssignableFrom(typeT))
                return ScheduleInternal_1(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            if (typeof(IBaseJobProcessComponentData_1_WE).IsAssignableFrom(typeT))
                return ScheduleInternal_1_WE(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);

            if (typeof(IBaseJobProcessComponentData_2).IsAssignableFrom(typeT))
                return ScheduleInternal_2(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            if (typeof(IBaseJobProcessComponentData_2_WE).IsAssignableFrom(typeT))
                return ScheduleInternal_2_WE(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            
            if (typeof(IBaseJobProcessComponentData_3).IsAssignableFrom(typeT))
                return ScheduleInternal_3(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            if (typeof(IBaseJobProcessComponentData_3_WE).IsAssignableFrom(typeT))
                return ScheduleInternal_3_WE(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);

            if (typeof(IBaseJobProcessComponentData_4).IsAssignableFrom(typeT))
                return ScheduleInternal_4(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            if (typeof(IBaseJobProcessComponentData_4_WE).IsAssignableFrom(typeT))
                return ScheduleInternal_4_WE(ref jobData, system, 1, dependsOn, ScheduleMode.Batched);
            throw new System.ArgumentException("Not supported");
        }

        public static JobHandle ScheduleSingle<T>(this T jobData, ComponentSystemBase system,
            JobHandle dependsOn = default(JobHandle))
            where T : struct, IBaseJobProcessComponentData
        {
            var typeT = typeof(T);
            if (typeof(IBaseJobProcessComponentData_1).IsAssignableFrom(typeT))
                return ScheduleInternal_1(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            if (typeof(IBaseJobProcessComponentData_1_WE).IsAssignableFrom(typeT))
                return ScheduleInternal_1_WE(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            
            if (typeof(IBaseJobProcessComponentData_2).IsAssignableFrom(typeT))
                return ScheduleInternal_2(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            if (typeof(IBaseJobProcessComponentData_2_WE).IsAssignableFrom(typeT))
                return ScheduleInternal_2_WE(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            
            if (typeof(IBaseJobProcessComponentData_3).IsAssignableFrom(typeT))
                return ScheduleInternal_3(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            if (typeof(IBaseJobProcessComponentData_3_WE).IsAssignableFrom(typeT))
                return ScheduleInternal_3_WE(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);

            if (typeof(IBaseJobProcessComponentData_4).IsAssignableFrom(typeT))
                return ScheduleInternal_4(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);
            if (typeof(IBaseJobProcessComponentData_4_WE).IsAssignableFrom(typeT))
                return ScheduleInternal_4_WE(ref jobData, system, -1, dependsOn, ScheduleMode.Batched);

            throw new System.ArgumentException("Not supported");
        }

        public static void Run<T>(this T jobData, ComponentSystemBase system)
            where T : struct, IBaseJobProcessComponentData
        {
            var typeT = typeof(T);
            if (typeof(IBaseJobProcessComponentData_1).IsAssignableFrom(typeT))
                ScheduleInternal_1(ref jobData, system, -1, default(JobHandle), ScheduleMode.Run);
            else if (typeof(IBaseJobProcessComponentData_1_WE).IsAssignableFrom(typeT))
                ScheduleInternal_1_WE(ref jobData, system, -1, default(JobHandle), ScheduleMode.Run);
            
            
            else if (typeof(IBaseJobProcessComponentData_2).IsAssignableFrom(typeT))
                ScheduleInternal_2(ref jobData, system, -1, default(JobHandle), ScheduleMode.Run);
            else if (typeof(IBaseJobProcessComponentData_2_WE).IsAssignableFrom(typeT))
                ScheduleInternal_2_WE(ref jobData, system, -1, default(JobHandle), ScheduleMode.Run);
            
            else if (typeof(IBaseJobProcessComponentData_3).IsAssignableFrom(typeT))
                ScheduleInternal_3(ref jobData, system, -1, default(JobHandle), ScheduleMode.Run);
            else if (typeof(IBaseJobProcessComponentData_3_WE).IsAssignableFrom(typeT))
                ScheduleInternal_3_WE(ref jobData, system, -1, default(JobHandle), ScheduleMode.Run);
            
            else if (typeof(IBaseJobProcessComponentData_4).IsAssignableFrom(typeT))
                ScheduleInternal_4(ref jobData, system, -1, default(JobHandle), ScheduleMode.Run);
            else if (typeof(IBaseJobProcessComponentData_4_WE).IsAssignableFrom(typeT))
                ScheduleInternal_4_WE(ref jobData, system, -1, default(JobHandle), ScheduleMode.Run);
            else
                throw new System.ArgumentException("Not supported");
        }

        private static unsafe JobHandle Schedule(void* fullData, int length, int innerloopBatchCount,
            bool isParallelFor, ref JobProcessComponentDataCache cache, JobHandle dependsOn, ScheduleMode mode)
        {
            if (isParallelFor)
            {
                var scheduleParams =
                    new JobsUtility.JobScheduleParameters(fullData, cache.JobReflectionDataParallelFor, dependsOn,
                        mode);
                return JobsUtility.ScheduleParallelFor(ref scheduleParams, length, innerloopBatchCount);
            }
            else
            {
                var scheduleParams =
                    new JobsUtility.JobScheduleParameters(fullData, cache.JobReflectionData, dependsOn, mode);
                return JobsUtility.Schedule(ref scheduleParams);
            }
        }

        internal static unsafe JobHandle ScheduleInternal_1<T>(ref T jobData, ComponentSystemBase system,
            int innerloopBatchCount,
            JobHandle dependsOn, ScheduleMode mode)
            where T : struct
        {
            JobStruct_ProcessInfer_1<T> fullData;
            fullData.Data = jobData;

            var isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process1<,>), isParallelFor,
                ref JobStruct_ProcessInfer_1<T>.Cache, out fullData.Iterator);
            return Schedule(UnsafeUtility.AddressOf(ref fullData), fullData.Iterator.m_Length, innerloopBatchCount,
                isParallelFor, ref JobStruct_ProcessInfer_1<T>.Cache, dependsOn, mode);
        }

        internal static unsafe JobHandle ScheduleInternal_1_WE<T>(ref T jobData, ComponentSystemBase system,
            int innerloopBatchCount,
            JobHandle dependsOn, ScheduleMode mode)
            where T : struct
        {
            JobStruct_ProcessInfer_1_WE<T> fullData;
            fullData.Data = jobData;

            var isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process1_WE<,>), isParallelFor,
                ref JobStruct_ProcessInfer_1_WE<T>.Cache, out fullData.Iterator);
            return Schedule(UnsafeUtility.AddressOf(ref fullData), fullData.Iterator.m_Length, innerloopBatchCount,
                isParallelFor, ref JobStruct_ProcessInfer_1_WE<T>.Cache, dependsOn, mode);
        }

        
        internal static unsafe JobHandle ScheduleInternal_2<T>(ref T jobData, ComponentSystemBase system,
            int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode)
            where T : struct
        {
            JobStruct_ProcessInfer_2<T> fullData;
            fullData.Data = jobData;

            var isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process2<,,>), isParallelFor,
                ref JobStruct_ProcessInfer_2<T>.Cache, out fullData.Iterator);
            return Schedule(UnsafeUtility.AddressOf(ref fullData), fullData.Iterator.m_Length, innerloopBatchCount,
                isParallelFor, ref JobStruct_ProcessInfer_2<T>.Cache, dependsOn, mode);
        }

        internal static unsafe JobHandle ScheduleInternal_2_WE<T>(ref T jobData, ComponentSystemBase system,
            int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode)
            where T : struct
        {
            JobStruct_ProcessInfer_2_WE<T> fullData;
            fullData.Data = jobData;

            var isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process2_WE<,,>), isParallelFor,
                ref JobStruct_ProcessInfer_2_WE<T>.Cache, out fullData.Iterator);
            return Schedule(UnsafeUtility.AddressOf(ref fullData), fullData.Iterator.m_Length, innerloopBatchCount,
                isParallelFor, ref JobStruct_ProcessInfer_2_WE<T>.Cache, dependsOn, mode);
        }
        
        internal static unsafe JobHandle ScheduleInternal_3<T>(ref T jobData, ComponentSystemBase system,
            int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode)
            where T : struct
        {
            JobStruct_ProcessInfer_3<T> fullData;
            fullData.Data = jobData;

            var isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process3<,,,>),
                isParallelFor, ref JobStruct_ProcessInfer_3<T>.Cache, out fullData.Iterator);
            return Schedule(UnsafeUtility.AddressOf(ref fullData), fullData.Iterator.m_Length, innerloopBatchCount,
                isParallelFor, ref JobStruct_ProcessInfer_3<T>.Cache, dependsOn, mode);
        }

        internal static unsafe JobHandle ScheduleInternal_3_WE<T>(ref T jobData, ComponentSystemBase system,
            int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode)
            where T : struct
        {
            JobStruct_ProcessInfer_3_WE<T> fullData;
            fullData.Data = jobData;

            var isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process3_WE<,,,>),
                isParallelFor, ref JobStruct_ProcessInfer_3_WE<T>.Cache, out fullData.Iterator);
            return Schedule(UnsafeUtility.AddressOf(ref fullData), fullData.Iterator.m_Length, innerloopBatchCount,
                isParallelFor, ref JobStruct_ProcessInfer_3_WE<T>.Cache, dependsOn, mode);
        }
        
        internal static unsafe JobHandle ScheduleInternal_4<T>(ref T jobData, ComponentSystemBase system,
            int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode)
            where T : struct
        {
            JobStruct_ProcessInfer_4<T> fullData;
            fullData.Data = jobData;

            var isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process4<,,,,>),
                isParallelFor, ref JobStruct_ProcessInfer_4<T>.Cache, out fullData.Iterator);
            return Schedule(UnsafeUtility.AddressOf(ref fullData), fullData.Iterator.m_Length, innerloopBatchCount,
                isParallelFor, ref JobStruct_ProcessInfer_4<T>.Cache, dependsOn, mode);
        }

        internal static unsafe JobHandle ScheduleInternal_4_WE<T>(ref T jobData, ComponentSystemBase system,
            int innerloopBatchCount, JobHandle dependsOn, ScheduleMode mode)
            where T : struct
        {
            JobStruct_ProcessInfer_4_WE<T> fullData;
            fullData.Data = jobData;

            var isParallelFor = innerloopBatchCount != -1;
            IJobProcessComponentDataUtility.Initialize(system, typeof(T), typeof(JobStruct_Process4_WE<,,,,>),
                isParallelFor, ref JobStruct_ProcessInfer_4_WE<T>.Cache, out fullData.Iterator);
            return Schedule(UnsafeUtility.AddressOf(ref fullData), fullData.Iterator.m_Length, innerloopBatchCount,
                isParallelFor, ref JobStruct_ProcessInfer_4_WE<T>.Cache, dependsOn, mode);
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_1<T> where T : struct
        {
            public static JobProcessComponentDataCache Cache;

            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process1<T, U0>
            where T : struct, IJobProcessComponentData<U0>
            where U0 : struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;

            [Preserve]
            public static IntPtr Initialize(JobType jobType)
            {
                return JobsUtility.CreateJobReflectionData(typeof(JobStruct_Process1<T, U0>), typeof(T), jobType,
                    (ExecuteJobFunction) Execute);
            }

            private delegate void ExecuteJobFunction(ref JobStruct_Process1<T, U0> data, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            static unsafe void ExecuteChunk(ref JobStruct_Process1<T, U0> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                ComponentChunkCache cache0;

                for (var blockIndex = begin; blockIndex != end; ++blockIndex)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(blockIndex);

                    var processBlock = jobData.Iterator.Iterator.MatchesFilter();

                    if (!processBlock)
                        continue;

                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache0, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                    var ptr0 = UnsafeUtilityEx.RestrictNoAlias(cache0.CachedPtr);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), cache0.CachedBeginIndex, cache0.CachedEndIndex - cache0.CachedBeginIndex);
#endif
                    
                    for (var i = cache0.CachedBeginIndex; i != cache0.CachedEndIndex; i++)
                    {
#if CSHARP_7_OR_LATER
                        ref var value0 = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr0, i);
                        jobData.Data.Execute(ref value0);
#else
                        var value0 = UnsafeUtility.ReadArrayElement<U0>(ptr0, i);

                        jobData.Data.Execute(ref value0);

                        if (jobData.Iterator.IsReadOnly0 == 0)
                            UnsafeUtility.WriteArrayElement(ptr0, i, value0);
#endif
                    }
                }
            }

            public static unsafe void Execute(ref JobStruct_Process1<T, U0> jobData, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (jobData.Iterator.m_IsParallelFor)
                {
                    int begin;
                    int end;
                    while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        ExecuteChunk(ref jobData, bufferRangePatchData, begin, end);
                    }
                }
                else
                {
                    ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_1_WE<T> where T : struct
        {
            public static JobProcessComponentDataCache Cache;

            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process1_WE<T, U0>
            where T : struct, IJobProcessComponentDataWithEntity<U0>
            where U0 : struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;

            [Preserve]
            public static IntPtr Initialize(JobType jobType)
            {
                return JobsUtility.CreateJobReflectionData(typeof(JobStruct_Process1_WE<T, U0>), typeof(T), jobType,
                    (ExecuteJobFunction) Execute);
            }

            private delegate void ExecuteJobFunction(ref JobStruct_Process1_WE<T, U0> data, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            static unsafe void ExecuteChunk(ref JobStruct_Process1_WE<T, U0> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                ComponentChunkCache cacheE;
                ComponentChunkCache cache0;

                for (var blockIndex = begin; blockIndex != end; ++blockIndex)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(blockIndex);

                    var processBlock = jobData.Iterator.Iterator.MatchesFilter();

                    if (!processBlock)
                        continue;

                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache0, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                    var ptr0 = UnsafeUtilityEx.RestrictNoAlias(cache0.CachedPtr);

                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cacheE, false, 0);
                    var ptrE = (Entity*)UnsafeUtilityEx.RestrictNoAlias(cacheE.CachedPtr);
                    
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), cache0.CachedBeginIndex, cache0.CachedEndIndex - cache0.CachedBeginIndex);
#endif
                    
                    for (var i = cache0.CachedBeginIndex; i != cache0.CachedEndIndex; i++)
                    {
#if CSHARP_7_OR_LATER
                        ref var value0 = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr0, i);
                        jobData.Data.Execute(ptrE[i], i, ref value0);
#else
                        var value0 = UnsafeUtility.ReadArrayElement<U0>(ptr0, i);

                        jobData.Data.Execute(ptrE[i], i, ref value0);

                        if (jobData.Iterator.IsReadOnly0 == 0)
                            UnsafeUtility.WriteArrayElement(ptr0, i, value0);
#endif
                    }
                }
            }

            public static unsafe void Execute(ref JobStruct_Process1_WE<T, U0> jobData, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (jobData.Iterator.m_IsParallelFor)
                {
                    int begin;
                    int end;
                    while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        ExecuteChunk(ref jobData, bufferRangePatchData, begin, end);
                    }
                }
                else
                {
                    ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
            }
        }
        
        
        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_2<T> where T : struct
        {
            public static JobProcessComponentDataCache Cache;

            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process2<T, U0, U1>
            where T : struct, IJobProcessComponentData<U0, U1>
            where U0 : struct, IComponentData
            where U1 : struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;

            [Preserve]
            public static IntPtr Initialize(JobType jobType)
            {
                return JobsUtility.CreateJobReflectionData(typeof(JobStruct_Process2<T, U0, U1>), typeof(T), jobType,
                    (ExecuteJobFunction) Execute);
            }

            private delegate void ExecuteJobFunction(ref JobStruct_Process2<T, U0, U1> data, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);


            static unsafe void ExecuteChunk(ref JobStruct_Process2<T, U0, U1> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                ComponentChunkCache cache0;
                ComponentChunkCache cache1;

                for (var blockIndex = begin; blockIndex != end; ++blockIndex)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(blockIndex);

                    var processBlock = jobData.Iterator.Iterator.MatchesFilter();

                    if (!processBlock)
                        continue;

                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache0, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache1, jobData.Iterator.IsReadOnly1 == 0, jobData.Iterator.IndexInGroup1);
                    var ptr0 = UnsafeUtilityEx.RestrictNoAlias(cache0.CachedPtr);
                    var ptr1 = UnsafeUtilityEx.RestrictNoAlias(cache1.CachedPtr);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), cache0.CachedBeginIndex, cache0.CachedEndIndex - cache0.CachedBeginIndex);
#endif
                    
                    for (var i = cache0.CachedBeginIndex; i != cache0.CachedEndIndex; i++)
                    {
#if CSHARP_7_OR_LATER
                        ref var value0 = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr0, i);
                        ref var value1 = ref UnsafeUtilityEx.ArrayElementAsRef<U1>(ptr1, i);
                        jobData.Data.Execute(ref value0, ref value1);
#else
                        var value0 = UnsafeUtility.ReadArrayElement<U0>(ptr0, i);
                        var value1 = UnsafeUtility.ReadArrayElement<U1>(ptr1, i);

                        jobData.Data.Execute(ref value0, ref value1);

                        if (jobData.Iterator.IsReadOnly0 == 0)
                            UnsafeUtility.WriteArrayElement(ptr0, i, value0);
                        if (jobData.Iterator.IsReadOnly1 == 0)
                            UnsafeUtility.WriteArrayElement(ptr1, i, value1);
#endif
                    }
                }
            }

            public static unsafe void Execute(ref JobStruct_Process2<T, U0, U1> jobData, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (jobData.Iterator.m_IsParallelFor)
                {
                    int begin;
                    int end;
                    while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        ExecuteChunk(ref jobData, bufferRangePatchData, begin, end);
                    }
                }
                else
                {
                    ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_2_WE<T> where T : struct
        {
            public static JobProcessComponentDataCache Cache;

            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process2_WE<T, U0, U1>
            where T : struct, IJobProcessComponentDataWithEntity<U0, U1>
            where U0 : struct, IComponentData
            where U1 : struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;

            [Preserve]
            public static IntPtr Initialize(JobType jobType)
            {
                return JobsUtility.CreateJobReflectionData(typeof(JobStruct_Process2_WE<T, U0, U1>), typeof(T), jobType,
                    (ExecuteJobFunction) Execute);
            }

            private delegate void ExecuteJobFunction(ref JobStruct_Process2_WE<T, U0, U1> data, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);


            static unsafe void ExecuteChunk(ref JobStruct_Process2_WE<T, U0, U1> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                ComponentChunkCache cache0;
                ComponentChunkCache cache1;
                ComponentChunkCache cacheE;

                for (var blockIndex = begin; blockIndex != end; ++blockIndex)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(blockIndex);

                    var processBlock = jobData.Iterator.Iterator.MatchesFilter();

                    if (!processBlock)
                        continue;

                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache0, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache1, jobData.Iterator.IsReadOnly1 == 0, jobData.Iterator.IndexInGroup1);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cacheE, false, 0);

                    var ptr0 = UnsafeUtilityEx.RestrictNoAlias(cache0.CachedPtr);
                    var ptr1 = UnsafeUtilityEx.RestrictNoAlias(cache1.CachedPtr);
                    var ptrE = (Entity*)UnsafeUtilityEx.RestrictNoAlias(cacheE.CachedPtr);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), cache0.CachedBeginIndex, cache0.CachedEndIndex - cache0.CachedBeginIndex);
#endif
                    
                    for (var i = cache0.CachedBeginIndex; i != cache0.CachedEndIndex; i++)
                    {
#if CSHARP_7_OR_LATER
                        ref var value0 = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr0, i);
                        ref var value1 = ref UnsafeUtilityEx.ArrayElementAsRef<U1>(ptr1, i);
                        jobData.Data.Execute(ptrE[i], i, ref value0, ref value1);
#else
                        var value0 = UnsafeUtility.ReadArrayElement<U0>(ptr0, i);
                        var value1 = UnsafeUtility.ReadArrayElement<U1>(ptr1, i);

                        jobData.Data.Execute(ptrE[i], i, ref value0, ref value1);

                        if (jobData.Iterator.IsReadOnly0 == 0)
                            UnsafeUtility.WriteArrayElement(ptr0, i, value0);
                        if (jobData.Iterator.IsReadOnly1 == 0)
                            UnsafeUtility.WriteArrayElement(ptr1, i, value1);
#endif
                    }
                }
            }

            public static unsafe void Execute(ref JobStruct_Process2_WE<T, U0, U1> jobData, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (jobData.Iterator.m_IsParallelFor)
                {
                    int begin;
                    int end;
                    while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        ExecuteChunk(ref jobData, bufferRangePatchData, begin, end);
                    }
                }
                else
                {
                    ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_3<T> where T : struct
        {
            public static JobProcessComponentDataCache Cache;

            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process3<T, U0, U1, U2>
            where T : struct, IJobProcessComponentData<U0, U1, U2>
            where U0 : struct, IComponentData
            where U1 : struct, IComponentData
            where U2 : struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;

            [Preserve]
            public static IntPtr Initialize(JobType jobType)
            {
                return JobsUtility.CreateJobReflectionData(typeof(JobStruct_Process3<T, U0, U1, U2>), typeof(T),
                    jobType, (ExecuteJobFunction) Execute);
            }

            private delegate void ExecuteJobFunction(ref JobStruct_Process3<T, U0, U1, U2> data, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            static unsafe void ExecuteChunk(ref JobStruct_Process3<T, U0, U1, U2> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                ComponentChunkCache cache0;
                ComponentChunkCache cache1;
                ComponentChunkCache cache2;

                for (var blockIndex = begin; blockIndex != end; ++blockIndex)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(blockIndex);

                    var processBlock = jobData.Iterator.Iterator.MatchesFilter();

                    if (!processBlock)
                        continue;

                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache0, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache1, jobData.Iterator.IsReadOnly1 == 0, jobData.Iterator.IndexInGroup1);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache2, jobData.Iterator.IsReadOnly2 == 0, jobData.Iterator.IndexInGroup2);
                    var ptr0 = UnsafeUtilityEx.RestrictNoAlias(cache0.CachedPtr);
                    var ptr1 = UnsafeUtilityEx.RestrictNoAlias(cache1.CachedPtr);
                    var ptr2 = UnsafeUtilityEx.RestrictNoAlias(cache2.CachedPtr);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), cache0.CachedBeginIndex, cache0.CachedEndIndex - cache0.CachedBeginIndex);
#endif
                    
                    for (var i = cache0.CachedBeginIndex; i != cache0.CachedEndIndex; i++)
                    {
#if CSHARP_7_OR_LATER
                        ref var value0 = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr0, i);
                        ref var value1 = ref UnsafeUtilityEx.ArrayElementAsRef<U1>(ptr1, i);
                        ref var value2 = ref UnsafeUtilityEx.ArrayElementAsRef<U2>(ptr2, i);
                        jobData.Data.Execute(ref value0, ref value1, ref value2);
#else
                        var value0 = UnsafeUtility.ReadArrayElement<U0>(ptr0, i);
                        var value1 = UnsafeUtility.ReadArrayElement<U1>(ptr1, i);
                        var value2 = UnsafeUtility.ReadArrayElement<U2>(ptr2, i);

                        jobData.Data.Execute(ref value0, ref value1, ref value2);

                        if (jobData.Iterator.IsReadOnly0 == 0)
                            UnsafeUtility.WriteArrayElement(ptr0, i, value0);
                        if (jobData.Iterator.IsReadOnly1 == 0)
                            UnsafeUtility.WriteArrayElement(ptr1, i, value1);
                        if (jobData.Iterator.IsReadOnly2 == 0)
                            UnsafeUtility.WriteArrayElement(ptr2, i, value2);
#endif
                    }
                }
            }

            public static unsafe void Execute(ref JobStruct_Process3<T, U0, U1, U2> jobData, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (jobData.Iterator.m_IsParallelFor)
                {
                    int begin;
                    int end;
                    while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        ExecuteChunk(ref jobData, bufferRangePatchData, begin, end);
                    }
                }
                else
                {
                    ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_3_WE<T> where T : struct
        {
            public static JobProcessComponentDataCache Cache;

            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process3_WE<T, U0, U1, U2>
            where T : struct, IJobProcessComponentDataWithEntity<U0, U1, U2>
            where U0 : struct, IComponentData
            where U1 : struct, IComponentData
            where U2 : struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;

            [Preserve]
            public static IntPtr Initialize(JobType jobType)
            {
                return JobsUtility.CreateJobReflectionData(typeof(JobStruct_Process3_WE<T, U0, U1, U2>), typeof(T),
                    jobType, (ExecuteJobFunction) Execute);
            }

            private delegate void ExecuteJobFunction(ref JobStruct_Process3_WE<T, U0, U1, U2> data, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            static unsafe void ExecuteChunk(ref JobStruct_Process3_WE<T, U0, U1, U2> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                ComponentChunkCache cache0;
                ComponentChunkCache cache1;
                ComponentChunkCache cache2;
                ComponentChunkCache cacheE;

                for (var blockIndex = begin; blockIndex != end; ++blockIndex)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(blockIndex);

                    var processBlock = jobData.Iterator.Iterator.MatchesFilter();

                    if (!processBlock)
                        continue;

                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache0, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache1, jobData.Iterator.IsReadOnly1 == 0, jobData.Iterator.IndexInGroup1);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache2, jobData.Iterator.IsReadOnly2 == 0, jobData.Iterator.IndexInGroup2);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cacheE, false, 0);
                    var ptr0 = UnsafeUtilityEx.RestrictNoAlias(cache0.CachedPtr);
                    var ptr1 = UnsafeUtilityEx.RestrictNoAlias(cache1.CachedPtr);
                    var ptr2 = UnsafeUtilityEx.RestrictNoAlias(cache2.CachedPtr);
                    var ptrE = (Entity*)UnsafeUtilityEx.RestrictNoAlias(cacheE.CachedPtr);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), cache0.CachedBeginIndex, cache0.CachedEndIndex - cache0.CachedBeginIndex);
#endif
                    
                    for (var i = cache0.CachedBeginIndex; i != cache0.CachedEndIndex; i++)
                    {
#if CSHARP_7_OR_LATER
                        ref var value0 = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr0, i);
                        ref var value1 = ref UnsafeUtilityEx.ArrayElementAsRef<U1>(ptr1, i);
                        ref var value2 = ref UnsafeUtilityEx.ArrayElementAsRef<U2>(ptr2, i);
                        jobData.Data.Execute(ptrE[i], i, ref value0, ref value1, ref value2);
#else
                        var value0 = UnsafeUtility.ReadArrayElement<U0>(ptr0, i);
                        var value1 = UnsafeUtility.ReadArrayElement<U1>(ptr1, i);
                        var value2 = UnsafeUtility.ReadArrayElement<U2>(ptr2, i);

                        jobData.Data.Execute(ptrE[i], i, ref value0, ref value1, ref value2);

                        if (jobData.Iterator.IsReadOnly0 == 0)
                            UnsafeUtility.WriteArrayElement(ptr0, i, value0);
                        if (jobData.Iterator.IsReadOnly1 == 0)
                            UnsafeUtility.WriteArrayElement(ptr1, i, value1);
                        if (jobData.Iterator.IsReadOnly2 == 0)
                            UnsafeUtility.WriteArrayElement(ptr2, i, value2);
#endif
                    }
                }
            }

            public static unsafe void Execute(ref JobStruct_Process3_WE<T, U0, U1, U2> jobData, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (jobData.Iterator.m_IsParallelFor)
                {
                    int begin;
                    int end;
                    while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        ExecuteChunk(ref jobData, bufferRangePatchData, begin, end);
                    }
                }
                else
                {
                    ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_4<T> where T : struct
        {
            public static JobProcessComponentDataCache Cache;

            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process4<T, U0, U1, U2, U3>
            where T : struct, IJobProcessComponentData<U0, U1, U2, U3>
            where U0 : struct, IComponentData
            where U1 : struct, IComponentData
            where U2 : struct, IComponentData
            where U3 : struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;

            [Preserve]
            public static IntPtr Initialize(JobType jobType)
            {
                return JobsUtility.CreateJobReflectionData(typeof(JobStruct_Process4<T, U0, U1, U2, U3>), typeof(T),
                    jobType, (ExecuteJobFunction) Execute);
            }

            private delegate void ExecuteJobFunction(ref JobStruct_Process4<T, U0, U1, U2, U3> data, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            static unsafe void ExecuteChunk(ref JobStruct_Process4<T, U0, U1, U2, U3> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                ComponentChunkCache cache0;
                ComponentChunkCache cache1;
                ComponentChunkCache cache2;
                ComponentChunkCache cache3;

                for (var blockIndex = begin; blockIndex != end; ++blockIndex)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(blockIndex);

                    var processBlock = jobData.Iterator.Iterator.MatchesFilter();

                    if (!processBlock)
                        continue;

                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache0, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache1, jobData.Iterator.IsReadOnly1 == 0, jobData.Iterator.IndexInGroup1);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache2, jobData.Iterator.IsReadOnly2 == 0, jobData.Iterator.IndexInGroup2);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache3, jobData.Iterator.IsReadOnly3 == 0, jobData.Iterator.IndexInGroup3);
                    var ptr0 = UnsafeUtilityEx.RestrictNoAlias(cache0.CachedPtr);
                    var ptr1 = UnsafeUtilityEx.RestrictNoAlias(cache1.CachedPtr);
                    var ptr2 = UnsafeUtilityEx.RestrictNoAlias(cache2.CachedPtr);
                    var ptr3 = UnsafeUtilityEx.RestrictNoAlias(cache3.CachedPtr);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), cache0.CachedBeginIndex, cache0.CachedEndIndex - cache0.CachedBeginIndex);
#endif
                    
                    for (var i = cache0.CachedBeginIndex; i != cache0.CachedEndIndex; i++)
                    {
#if CSHARP_7_OR_LATER
                        ref var value0 = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr0, i);
                        ref var value1 = ref UnsafeUtilityEx.ArrayElementAsRef<U1>(ptr1, i);
                        ref var value2 = ref UnsafeUtilityEx.ArrayElementAsRef<U2>(ptr2, i);
                        ref var value3 = ref UnsafeUtilityEx.ArrayElementAsRef<U3>(ptr3, i);
                        jobData.Data.Execute(ref value0, ref value1, ref value2, ref value3);
#else
                        var value0 = UnsafeUtility.ReadArrayElement<U0>(ptr0, i);
                        var value1 = UnsafeUtility.ReadArrayElement<U1>(ptr1, i);
                        var value2 = UnsafeUtility.ReadArrayElement<U2>(ptr2, i);
                        var value3 = UnsafeUtility.ReadArrayElement<U3>(ptr3, i);

                        jobData.Data.Execute(ref value0, ref value1, ref value2, ref value3);

                        if (jobData.Iterator.IsReadOnly0 == 0)
                            UnsafeUtility.WriteArrayElement(ptr0, i, value0);
                        if (jobData.Iterator.IsReadOnly1 == 0)
                            UnsafeUtility.WriteArrayElement(ptr1, i, value1);
                        if (jobData.Iterator.IsReadOnly2 == 0)
                            UnsafeUtility.WriteArrayElement(ptr2, i, value2);
                        if (jobData.Iterator.IsReadOnly3 == 0)
                            UnsafeUtility.WriteArrayElement(ptr3, i, value3);
#endif
                    }
                }
            }

            public static unsafe void Execute(ref JobStruct_Process4<T, U0, U1, U2, U3> jobData, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (jobData.Iterator.m_IsParallelFor)
                {
                    int begin;
                    int end;
                    while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        ExecuteChunk(ref jobData, bufferRangePatchData, begin, end);
                    }
                }
                else
                {
                    ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct JobStruct_ProcessInfer_4_WE<T> where T : struct
        {
            public static JobProcessComponentDataCache Cache;

            public ProcessIterationData Iterator;
            public T Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobStruct_Process4_WE<T, U0, U1, U2, U3>
            where T : struct, IJobProcessComponentDataWithEntity<U0, U1, U2, U3>
            where U0 : struct, IComponentData
            where U1 : struct, IComponentData
            where U2 : struct, IComponentData
            where U3 : struct, IComponentData
        {
            public ProcessIterationData Iterator;
            public T Data;

            [Preserve]
            public static IntPtr Initialize(JobType jobType)
            {
                return JobsUtility.CreateJobReflectionData(typeof(JobStruct_Process4_WE<T, U0, U1, U2, U3>), typeof(T),
                    jobType, (ExecuteJobFunction) Execute);
            }

            private delegate void ExecuteJobFunction(ref JobStruct_Process4_WE<T, U0, U1, U2, U3> data, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            static unsafe void ExecuteChunk(ref JobStruct_Process4_WE<T, U0, U1, U2, U3> jobData, IntPtr bufferRangePatchData, int begin, int end)
            {
                ComponentChunkCache cache0;
                ComponentChunkCache cache1;
                ComponentChunkCache cache2;
                ComponentChunkCache cache3;
                ComponentChunkCache cacheE;

                for (var blockIndex = begin; blockIndex != end; ++blockIndex)
                {
                    jobData.Iterator.Iterator.MoveToChunkWithoutFiltering(blockIndex);

                    var processBlock = jobData.Iterator.Iterator.MatchesFilter();

                    if (!processBlock)
                        continue;

                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache0, jobData.Iterator.IsReadOnly0 == 0, jobData.Iterator.IndexInGroup0);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache1, jobData.Iterator.IsReadOnly1 == 0, jobData.Iterator.IndexInGroup1);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache2, jobData.Iterator.IsReadOnly2 == 0, jobData.Iterator.IndexInGroup2);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cache3, jobData.Iterator.IsReadOnly3 == 0, jobData.Iterator.IndexInGroup3);
                    jobData.Iterator.Iterator.UpdateCacheToCurrentChunk(out cacheE, false, 0);
                    var ptr0 = UnsafeUtilityEx.RestrictNoAlias(cache0.CachedPtr);
                    var ptr1 = UnsafeUtilityEx.RestrictNoAlias(cache1.CachedPtr);
                    var ptr2 = UnsafeUtilityEx.RestrictNoAlias(cache2.CachedPtr);
                    var ptr3 = UnsafeUtilityEx.RestrictNoAlias(cache3.CachedPtr);
                    var ptrE = (Entity*)UnsafeUtilityEx.RestrictNoAlias(cacheE.CachedPtr);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), cache0.CachedBeginIndex, cache0.CachedEndIndex - cache0.CachedBeginIndex);
#endif
                    
                    for (var i = cache0.CachedBeginIndex; i != cache0.CachedEndIndex; i++)
                    {
#if CSHARP_7_OR_LATER
                        ref var value0 = ref UnsafeUtilityEx.ArrayElementAsRef<U0>(ptr0, i);
                        ref var value1 = ref UnsafeUtilityEx.ArrayElementAsRef<U1>(ptr1, i);
                        ref var value2 = ref UnsafeUtilityEx.ArrayElementAsRef<U2>(ptr2, i);
                        ref var value3 = ref UnsafeUtilityEx.ArrayElementAsRef<U3>(ptr3, i);
                        jobData.Data.Execute(ptrE[i], i, ref value0, ref value1, ref value2, ref value3);
#else
                        var value0 = UnsafeUtility.ReadArrayElement<U0>(ptr0, i);
                        var value1 = UnsafeUtility.ReadArrayElement<U1>(ptr1, i);
                        var value2 = UnsafeUtility.ReadArrayElement<U2>(ptr2, i);
                        var value3 = UnsafeUtility.ReadArrayElement<U3>(ptr3, i);

                        jobData.Data.Execute(ptrE[i], i, ref value0, ref value1, ref value2, ref value3);

                        if (jobData.Iterator.IsReadOnly0 == 0)
                            UnsafeUtility.WriteArrayElement(ptr0, i, value0);
                        if (jobData.Iterator.IsReadOnly1 == 0)
                            UnsafeUtility.WriteArrayElement(ptr1, i, value1);
                        if (jobData.Iterator.IsReadOnly2 == 0)
                            UnsafeUtility.WriteArrayElement(ptr2, i, value2);
                        if (jobData.Iterator.IsReadOnly3 == 0)
                            UnsafeUtility.WriteArrayElement(ptr3, i, value3);
#endif
                    }
                }
            }

            public static unsafe void Execute(ref JobStruct_Process4_WE<T, U0, U1, U2, U3> jobData, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (jobData.Iterator.m_IsParallelFor)
                {
                    int begin;
                    int end;
                    while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        ExecuteChunk(ref jobData, bufferRangePatchData, begin, end);
                    }
                }
                else
                {
                    ExecuteChunk(ref jobData, bufferRangePatchData, 0, jobData.Iterator.m_Length);
                }
            }
        }
    }
}
