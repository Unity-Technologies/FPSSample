using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Profiling;

namespace Unity.Entities
{
    internal unsafe class ComponentJobSafetyManager
    {
        private const int kMaxReadJobHandles = 17;
        private const int kMaxTypes = TypeManager.MaximumTypesCount;

        private readonly JobHandle* m_JobDependencyCombineBuffer;
        private readonly int m_JobDependencyCombineBufferCount;
        private ComponentSafetyHandle* m_ComponentSafetyHandles;

        private JobHandle m_ExclusiveTransactionDependency;

        private bool m_HasCleanHandles;

        private JobHandle* m_ReadJobFences;

        public ComponentJobSafetyManager()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_TempSafety = AtomicSafetyHandle.Create();
#endif


            m_ReadJobFences = (JobHandle*) UnsafeUtility.Malloc(sizeof(JobHandle) * kMaxReadJobHandles * kMaxTypes, 16,
                Allocator.Persistent);
            UnsafeUtility.MemClear(m_ReadJobFences, sizeof(JobHandle) * kMaxReadJobHandles * kMaxTypes);

            m_ComponentSafetyHandles =
                (ComponentSafetyHandle*) UnsafeUtility.Malloc(sizeof(ComponentSafetyHandle) * kMaxTypes, 16,
                    Allocator.Persistent);
            UnsafeUtility.MemClear(m_ComponentSafetyHandles, sizeof(ComponentSafetyHandle) * kMaxTypes);

            m_JobDependencyCombineBufferCount = 4 * 1024;
            m_JobDependencyCombineBuffer = (JobHandle*) UnsafeUtility.Malloc(
                sizeof(ComponentSafetyHandle) * m_JobDependencyCombineBufferCount, 16, Allocator.Persistent);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            for (var i = 0; i != kMaxTypes; i++)
            {
                m_ComponentSafetyHandles[i].SafetyHandle = AtomicSafetyHandle.Create();
                AtomicSafetyHandle.SetAllowSecondaryVersionWriting(m_ComponentSafetyHandles[i].SafetyHandle, false);
                m_ComponentSafetyHandles[i].BufferHandle = AtomicSafetyHandle.Create();
            }
#endif

            m_HasCleanHandles = true;
        }

        public bool IsInTransaction { get; private set; }

        public JobHandle ExclusiveTransactionDependency
        {
            get { return m_ExclusiveTransactionDependency; }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!IsInTransaction)
                    throw new InvalidOperationException(
                        "EntityManager.TransactionDependency can only after EntityManager.BeginExclusiveEntityTransaction has been called.");

                if (!JobHandle.CheckFenceIsDependencyOrDidSyncFence(m_ExclusiveTransactionDependency, value))
                    throw new InvalidOperationException(
                        "EntityManager.TransactionDependency must depend on the Entity Transaction job.");
#endif
                m_ExclusiveTransactionDependency = value;
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public AtomicSafetyHandle ExclusiveTransactionSafety { get; private set; }
#endif

        //@TODO: Optimize as one function call to in batch bump version on every single handle...
        public void CompleteAllJobsAndInvalidateArrays()
        {
            if (m_HasCleanHandles)
                return;

            Profiler.BeginSample("CompleteAllJobsAndInvalidateArrays");

            var count = TypeManager.GetTypeCount();
            for (var t = 0; t != count; t++)
            {
                m_ComponentSafetyHandles[t].WriteFence.Complete();

                var readFencesCount = m_ComponentSafetyHandles[t].NumReadFences;
                var readFences = m_ReadJobFences + t * kMaxReadJobHandles;
                for (var r = 0; r != readFencesCount; r++)
                    readFences[r].Complete();
                m_ComponentSafetyHandles[t].NumReadFences = 0;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            for (var i = 0; i != count; i++)
            {
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_ComponentSafetyHandles[i].SafetyHandle);
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_ComponentSafetyHandles[i].BufferHandle);
            }

            for (var i = 0; i != count; i++)
            {
                AtomicSafetyHandle.Release(m_ComponentSafetyHandles[i].SafetyHandle);
                m_ComponentSafetyHandles[i].SafetyHandle = AtomicSafetyHandle.Create();
                AtomicSafetyHandle.SetAllowSecondaryVersionWriting(m_ComponentSafetyHandles[i].SafetyHandle, false);

                AtomicSafetyHandle.Release(m_ComponentSafetyHandles[i].BufferHandle);
                m_ComponentSafetyHandles[i].BufferHandle = AtomicSafetyHandle.Create();
            }
#endif

            m_HasCleanHandles = true;

            Profiler.EndSample();
        }

        public void Dispose()
        {
            for (var i = 0; i < kMaxTypes; i++)
                m_ComponentSafetyHandles[i].WriteFence.Complete();

            for (var i = 0; i < kMaxTypes * kMaxReadJobHandles; i++)
                m_ReadJobFences[i].Complete();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            for (var i = 0; i < kMaxTypes; i++)
            {
                var res0 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease(m_ComponentSafetyHandles[i].SafetyHandle);
                var res1 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease(m_ComponentSafetyHandles[i].BufferHandle);

                if (res0 == EnforceJobResult.DidSyncRunningJobs || res1 == EnforceJobResult.DidSyncRunningJobs)
                    Debug.LogError(
                        "Disposing EntityManager but a job is still running against the ComponentData. It appears the job has not been registered with JobComponentSystem.AddDependency.");
            }

            AtomicSafetyHandle.Release(m_TempSafety);
#endif

            UnsafeUtility.Free(m_JobDependencyCombineBuffer, Allocator.Persistent);

            UnsafeUtility.Free(m_ComponentSafetyHandles, Allocator.Persistent);
            m_ComponentSafetyHandles = null;

            UnsafeUtility.Free(m_ReadJobFences, Allocator.Persistent);
            m_ReadJobFences = null;
        }

        public void CompleteDependenciesNoChecks(int* readerTypes, int readerTypesCount, int* writerTypes, int writerTypesCount)
        {
            for (var i = 0; i != writerTypesCount; i++)
                CompleteReadAndWriteDependencyNoChecks(writerTypes[i]);

            for (var i = 0; i != readerTypesCount; i++)
                CompleteWriteDependencyNoChecks(readerTypes[i]);
        }

        internal void PreDisposeCheck()
        {
            for (var i = 0; i < kMaxTypes; i++)
                m_ComponentSafetyHandles[i].WriteFence.Complete();

            for (var i = 0; i < kMaxTypes * kMaxReadJobHandles; i++)
                m_ReadJobFences[i].Complete();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            for (var i = 0; i < kMaxTypes; i++)
            {
                var res0 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted(m_ComponentSafetyHandles[i].SafetyHandle);
                var res1 = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted(m_ComponentSafetyHandles[i].BufferHandle);
                if (res0 == EnforceJobResult.DidSyncRunningJobs || res1 == EnforceJobResult.DidSyncRunningJobs)
                    Debug.LogError(
                        "Disposing EntityManager but a job is still running against the ComponentData. It appears the job has not been registered with JobComponentSystem.AddDependency.");
            }
#endif
        }

            public bool HasReaderOrWriterDependency(int type, JobHandle dependency)
        {
            var writer = m_ComponentSafetyHandles[type].WriteFence;
            if (JobHandle.CheckFenceIsDependencyOrDidSyncFence(dependency, writer))
                return true;

            var count = m_ComponentSafetyHandles[type].NumReadFences;
            for (var r = 0; r < count; r++)
            {
                var reader = m_ReadJobFences[type * kMaxReadJobHandles + r];
                if (JobHandle.CheckFenceIsDependencyOrDidSyncFence(dependency, reader))
                    return true;
            }

            return false;
        }

        public JobHandle GetDependency(int* readerTypes, int readerTypesCount, int* writerTypes, int writerTypesCount)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (readerTypesCount * kMaxReadJobHandles + writerTypesCount > m_JobDependencyCombineBufferCount)
                throw new ArgumentException("Too many readers & writers in GetDependency");
#endif

            var count = 0;
            for (var i = 0; i != readerTypesCount; i++)
                m_JobDependencyCombineBuffer[count++] = m_ComponentSafetyHandles[readerTypes[i]].WriteFence;

            for (var i = 0; i != writerTypesCount; i++)
            {
                var writerType = writerTypes[i];

                m_JobDependencyCombineBuffer[count++] = m_ComponentSafetyHandles[writerType].WriteFence;

                var numReadFences = m_ComponentSafetyHandles[writerType].NumReadFences;
                for (var j = 0; j != numReadFences; j++)
                    m_JobDependencyCombineBuffer[count++] = m_ReadJobFences[writerType * kMaxReadJobHandles + j];
            }

            return JobHandleUnsafeUtility.CombineDependencies(m_JobDependencyCombineBuffer,
                count);
        }

        public JobHandle AddDependency(int* readerTypes, int readerTypesCount, int* writerTypes, int writerTypesCount,
            JobHandle dependency)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            JobHandle* combinedDependencies = null;
            var combinedDependenciesCount = 0;
#endif

            for (var i = 0; i != writerTypesCount; i++)
            {
                var writer = writerTypes[i];
                m_ComponentSafetyHandles[writer].WriteFence = dependency;
            }


            for (var i = 0; i != readerTypesCount; i++)
            {
                var reader = readerTypes[i];
                m_ReadJobFences[reader * kMaxReadJobHandles + m_ComponentSafetyHandles[reader].NumReadFences] =
                    dependency;
                m_ComponentSafetyHandles[reader].NumReadFences++;

                if (m_ComponentSafetyHandles[reader].NumReadFences == kMaxReadJobHandles)
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    var combined = CombineReadDependencies(reader);
                    if (combinedDependencies == null)
                    {
                        JobHandle* temp = stackalloc JobHandle[readerTypesCount];
                        combinedDependencies = temp;
                    }

                    combinedDependencies[combinedDependenciesCount++] = combined;
#else
                    CombineReadDependencies(reader);
                    #endif
                }
            }

            if (readerTypesCount != 0 || writerTypesCount != 0)
                m_HasCleanHandles = false;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (combinedDependencies != null)
                return JobHandleUnsafeUtility.CombineDependencies(combinedDependencies, combinedDependenciesCount);
            return dependency;
#else
            return dependency;
#endif
        }

        public void CompleteWriteDependencyNoChecks(int type)
        {
            m_ComponentSafetyHandles[type].WriteFence.Complete();
        }

        public void CompleteReadAndWriteDependencyNoChecks(int type)
        {
            for (var i = 0; i < m_ComponentSafetyHandles[type].NumReadFences; ++i)
                m_ReadJobFences[type * kMaxReadJobHandles + i].Complete();
            m_ComponentSafetyHandles[type].NumReadFences = 0;

            m_ComponentSafetyHandles[type].WriteFence.Complete();
        }

        public void CompleteWriteDependency(int type)
        {
            CompleteWriteDependencyNoChecks(type);
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_ComponentSafetyHandles[type].SafetyHandle);
            AtomicSafetyHandle.CheckReadAndThrow(m_ComponentSafetyHandles[type].BufferHandle);
#endif
            
        }

        public void CompleteReadAndWriteDependency(int type)
        {
            CompleteReadAndWriteDependencyNoChecks(type);
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS            
            AtomicSafetyHandle.CheckWriteAndThrow(m_ComponentSafetyHandles[type].SafetyHandle);
            AtomicSafetyHandle.CheckWriteAndThrow(m_ComponentSafetyHandles[type].BufferHandle);
#endif
        }
        
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        
        
        public AtomicSafetyHandle GetSafetyHandle(int type, bool isReadOnly)
        {
            m_HasCleanHandles = false;
            var handle = m_ComponentSafetyHandles[type].SafetyHandle;
            if (isReadOnly)
                AtomicSafetyHandle.UseSecondaryVersion(ref handle);

            return handle;
        }

        public AtomicSafetyHandle GetBufferSafetyHandle(int type)
        {
            m_HasCleanHandles = false;

            return m_ComponentSafetyHandles[type].BufferHandle;
        }
#endif

        private JobHandle CombineReadDependencies(int type)
        {
            var combined = JobHandleUnsafeUtility.CombineDependencies(
                m_ReadJobFences + type * kMaxReadJobHandles, m_ComponentSafetyHandles[type].NumReadFences);

            m_ReadJobFences[type * kMaxReadJobHandles] = combined;
            m_ComponentSafetyHandles[type].NumReadFences = 1;

            return combined;
        }

        public void BeginExclusiveTransaction()
        {
            if (IsInTransaction)
                return;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            for (var i = 0; i != TypeManager.GetTypeCount(); i++)
            {
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_ComponentSafetyHandles[i].SafetyHandle);
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_ComponentSafetyHandles[i].BufferHandle);
            }
#endif

            IsInTransaction = true;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            ExclusiveTransactionSafety = AtomicSafetyHandle.Create();
#endif
            m_ExclusiveTransactionDependency = GetAllDependencies();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            for (var i = 0; i != TypeManager.GetTypeCount(); i++)
            {
                AtomicSafetyHandle.Release(m_ComponentSafetyHandles[i].SafetyHandle);
                AtomicSafetyHandle.Release(m_ComponentSafetyHandles[i].BufferHandle);
            }
#endif
        }

        public void EndExclusiveTransaction()
        {
            if (!IsInTransaction)
                return;

            m_ExclusiveTransactionDependency.Complete();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var res = AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease(ExclusiveTransactionSafety);
            if (res != EnforceJobResult.AllJobsAlreadySynced)
                //@TODO: Better message
                Debug.LogError("ExclusiveEntityTransaction job has not been registered");
#endif
            IsInTransaction = false;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            for (var i = 0; i != TypeManager.GetTypeCount(); i++)
            {
                m_ComponentSafetyHandles[i].SafetyHandle = AtomicSafetyHandle.Create();
                AtomicSafetyHandle.SetAllowSecondaryVersionWriting(m_ComponentSafetyHandles[i].SafetyHandle, false);

                m_ComponentSafetyHandles[i].BufferHandle = AtomicSafetyHandle.Create();
            }
#endif
        }

        private JobHandle GetAllDependencies()
        {
            var jobHandles =
                new NativeArray<JobHandle>(TypeManager.GetTypeCount() * (kMaxReadJobHandles + 1), Allocator.Temp);

            var count = 0;
            for (var i = 0; i != TypeManager.GetTypeCount(); i++)
            {
                jobHandles[count++] = m_ComponentSafetyHandles[i].WriteFence;

                var numReadFences = m_ComponentSafetyHandles[i].NumReadFences;
                for (var j = 0; j != numReadFences; j++)
                    jobHandles[count++] = m_ReadJobFences[i * kMaxReadJobHandles + j];
            }

            var combined = JobHandle.CombineDependencies(jobHandles);
            jobHandles.Dispose();

            return combined;
        }

        private struct ComponentSafetyHandle
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public AtomicSafetyHandle SafetyHandle;
            public AtomicSafetyHandle BufferHandle;
#endif
            public JobHandle WriteFence;
            public int NumReadFences;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly AtomicSafetyHandle m_TempSafety;
#endif
    }
}
