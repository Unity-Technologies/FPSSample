using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    interface ILayoutProvider
    {
        void GenerateAttributeLayout(uint capacity, Dictionary<VFXAttribute, int> storedAttribute);
        string GetCodeOffset(VFXAttribute attrib, string index);
        uint GetBufferSize(uint capacity);

        VFXGPUBufferDesc GetBufferDesc(uint capacity);
    }

    class StructureOfArrayProvider : ILayoutProvider
    {
        private struct AttributeLayout
        {
            public int bucket;
            public int offset;

            public AttributeLayout(int bucket, int offset)
            {
                this.bucket = bucket;
                this.offset = offset;
            }
        }

        // return size
        private int GenerateBucketLayout(List<VFXAttribute> attributes, int bucketId)
        {
            var sortedAttrib = attributes.OrderByDescending(a => VFXValue.TypeToSize(a.type));

            var attribBlocks = new List<List<VFXAttribute>>();
            foreach (var value in sortedAttrib)
            {
                var block = attribBlocks.FirstOrDefault(b => b.Sum(a => VFXValue.TypeToSize(a.type)) + VFXValue.TypeToSize(value.type) <= 4);
                if (block != null)
                    block.Add(value);
                else
                    attribBlocks.Add(new List<VFXAttribute>() { value });
            }

            int currentOffset = 0;
            int minAlignment = 0;
            foreach (var block in attribBlocks)
            {
                foreach (var attrib in block)
                {
                    int size = VFXValue.TypeToSize(attrib.type);
                    int alignment = size > 2 ? 4 : size;
                    minAlignment = Math.Max(alignment, minAlignment);
                    // align offset
                    currentOffset = (currentOffset + alignment - 1) & ~(alignment - 1);
                    m_AttributeLayout.Add(attrib, new AttributeLayout(bucketId, currentOffset));
                    currentOffset += size;
                }
            }

            return (currentOffset + minAlignment - 1) & ~(minAlignment - 1);
        }

        public void GenerateAttributeLayout(uint capacity, Dictionary<VFXAttribute, int> storedAttribute)
        {
            m_BucketSizes.Clear();
            m_AttributeLayout.Clear();
            m_BucketOffsets.Clear();

            var attributeBuckets = new Dictionary<int, List<VFXAttribute>>();
            foreach (var kvp in storedAttribute)
            {
                List<VFXAttribute> attributes;
                if (!attributeBuckets.ContainsKey(kvp.Value))
                {
                    attributes = new List<VFXAttribute>();
                    attributeBuckets[kvp.Value] = attributes;
                }
                else
                    attributes = attributeBuckets[kvp.Value];

                attributes.Add(kvp.Key);
            }

            int bucketId = 0;
            foreach (var bucket in attributeBuckets)
            {
                int bucketOffset = bucketId == 0 ? 0 : m_BucketOffsets[bucketId - 1] + (int)capacity * m_BucketSizes[bucketId - 1];
                m_BucketOffsets.Add((bucketOffset + 3) & ~3); // align on dword;
                m_BucketSizes.Add(GenerateBucketLayout(bucket.Value, bucketId));
                ++bucketId;
            }

            // Debug log
            if (VFXViewPreference.advancedLogs)
            {
                var builder = new StringBuilder();
                builder.AppendLine("ATTRIBUTE LAYOUT");
                builder.Append(string.Format("NbBuckets:{0} ( ", m_BucketSizes.Count));
                foreach (int size in m_BucketSizes)
                    builder.Append(size + " ");
                builder.AppendLine(")");
                foreach (var kvp in m_AttributeLayout)
                    builder.AppendLine(string.Format("Attrib:{0} type:{1} bucket:{2} offset:{3}", kvp.Key.name, kvp.Key.type, kvp.Value.bucket, kvp.Value.offset));
                Debug.Log(builder.ToString());
            }
        }

        public string GetCodeOffset(VFXAttribute attrib, string index)
        {
            AttributeLayout layout;
            if (!m_AttributeLayout.TryGetValue(attrib, out layout))
            {
                throw new InvalidOperationException(string.Format("Cannot find attribute {0}", attrib.name));
            }
            return string.Format("({2} * 0x{0:X} + 0x{1:X}) << 2", m_BucketSizes[layout.bucket], m_BucketOffsets[layout.bucket] + layout.offset, index);
        }

        public uint GetBufferSize(uint capacity)
        {
            return (uint)m_BucketOffsets.LastOrDefault() + capacity * (uint)m_BucketSizes.LastOrDefault();
        }

        public VFXGPUBufferDesc GetBufferDesc(uint capacity)
        {
            var layout = m_AttributeLayout.Select(o => new VFXLayoutElementDesc()
            {
                name = o.Key.name,
                type = o.Key.type,
                offset = new VFXLayoutOffset()
                {
                    structure = (uint)m_BucketSizes[o.Value.bucket],
                    bucket = (uint)m_BucketOffsets[o.Value.bucket],
                    element = (uint)o.Value.offset
                }
            });
            return new VFXGPUBufferDesc()
            {
                type = ComputeBufferType.Raw,
                size = GetBufferSize(capacity),
                stride = 4,
                capacity = capacity,
                layout = layout.ToArray()
            };
        }

        public struct BucketInfo
        {
            public int size;
            public int usedSize;
            public VFXAttribute[] attributes;
            public int[] channels;
        }

        public BucketInfo[] GetBucketLayoutInfo()
        {
            int count = m_BucketSizes.Count;
            BucketInfo[] buckets = new BucketInfo[count];
            for (int i = 0; i < count; i++)
            {
                int size = m_BucketSizes[i];
                buckets[i].size = size;
                buckets[i].usedSize = 0;
                buckets[i].attributes = new VFXAttribute[size];
                buckets[i].channels = new int[size];
            }

            foreach (var kvp in m_AttributeLayout)
            {
                var attrib = kvp.Key;
                int size = VFXValue.TypeToSize(attrib.type);
                int offset = kvp.Value.offset;
                for (int i = 0; i < size; i++)
                {
                    buckets[kvp.Value.bucket].attributes[i + offset] = attrib;
                    buckets[kvp.Value.bucket].channels[i + offset] = i;
                    buckets[kvp.Value.bucket].usedSize = Math.Max(buckets[kvp.Value.bucket].usedSize, i + offset + 1);
                }
            }

            return buckets;
        }

        private Dictionary<VFXAttribute, AttributeLayout> m_AttributeLayout = new Dictionary<VFXAttribute, AttributeLayout>();
        private List<int> m_BucketSizes = new List<int>();
        private List<int> m_BucketOffsets = new List<int>();
    }

    class VFXDataParticle : VFXData, ISpaceable
    {
        public override VFXDataType type { get { return VFXDataType.kParticle; } }

        public uint capacity
        {
            get { return m_Capacity; }
            set { m_Capacity = value; }
        }

        private uint alignedCapacity
        {
            get
            {
                uint capacity = m_Capacity;
                const uint kThreadPerGroup = 64;
                if (capacity > kThreadPerGroup)
                    capacity = (uint)((capacity + kThreadPerGroup - 1) & ~(kThreadPerGroup - 1)); // multiple of kThreadPerGroup
                return (capacity + 3u) & ~3u; // Align on 4 boundary
            }
        }

        public override uint sourceCount
        {
            get
            {
                var init = owners.FirstOrDefault(o => o.contextType == VFXContextType.kInit);

                if (init == null)
                    return 0u;

                var cpuCount = init.inputContexts.Where(o => o.contextType == VFXContextType.kSpawner).Count();
                var gpuCount = init.inputContexts.Where(o => o.contextType == VFXContextType.kSpawnerGPU).Count();

                if (cpuCount != 0 && gpuCount != 0)
                {
                    throw new InvalidOperationException("Cannot mix GPU & CPU spawners in init");
                }

                if (cpuCount > 0)
                {
                    return (uint)cpuCount;
                }
                else if (gpuCount > 0)
                {
                    if (gpuCount > 1)
                    {
                        throw new InvalidOperationException("Don't support multiple GPU event (for now)");
                    }
                    var parent = m_DependenciesIn.OfType<VFXDataParticle>().FirstOrDefault();
                    return parent != null ? parent.m_Capacity : 0u;
                }
                return init != null ? (uint)init.inputContexts.Where(o => o.contextType == VFXContextType.kSpawner /* Explicitly ignore spawner gpu */).Count() : 0u;
            }
        }

        public uint attributeBufferSize
        {
            get
            {
                return m_layoutAttributeCurrent.GetBufferSize(alignedCapacity);
            }
        }

        public VFXGPUBufferDesc attributeBufferDesc
        {
            get
            {
                return m_layoutAttributeCurrent.GetBufferDesc(capacity);
            }
        }

        public VFXCoordinateSpace space
        {
            get { return m_Space; }
            set { m_Space = value; Modified(); }
        }

        public override bool CanBeCompiled()
        {
            // Has enough contexts and capacity
            if (m_Owners.Count < 1 || m_Capacity <= 0)
                return false;

            // Has a initialize
            if (m_Owners[0].contextType != VFXContextType.kInit)
                return false;

            // Has a spawner
            if (m_Owners[0].inputContexts.FirstOrDefault() == null)
                return false;

            // Has an output
            if (m_Owners.Last().contextType == VFXContextType.kOutput)
                return true;

            // Has a least one dependent compilable system
            if (m_Owners.SelectMany(c => c.allLinkedOutputSlot)
                .Select(s => ((VFXModel)s.owner).GetFirstOfType<VFXContext>())
                .Any(c => c.CanBeCompiled()))
                return true;

            return false;
        }

        public override VFXDeviceTarget GetCompilationTarget(VFXContext context)
        {
            return VFXDeviceTarget.GPU;
        }

        public override void GenerateAttributeLayout()
        {
            m_layoutAttributeCurrent.GenerateAttributeLayout(alignedCapacity, m_StoredCurrentAttributes);
            var parent = m_DependenciesIn.OfType<VFXDataParticle>().FirstOrDefault();
            if (parent != null)
            {
                m_layoutAttributeSource.GenerateAttributeLayout(parent.alignedCapacity, parent.m_StoredCurrentAttributes);
                m_ownAttributeSourceBuffer = false;
            }
            else
            {
                var readSourceAttribute = m_ReadSourceAttributes.ToDictionary(o => o, _ => (int)VFXAttributeMode.ReadSource);
                m_layoutAttributeSource.GenerateAttributeLayout(sourceCount, readSourceAttribute);
                m_ownAttributeSourceBuffer = true;
            }
        }

        public override string GetAttributeDataDeclaration(VFXAttributeMode mode)
        {
            if (m_StoredCurrentAttributes.Count == 0)
                return string.Empty;
            else if ((mode & VFXAttributeMode.Write) != 0)
                return "RWByteAddressBuffer attributeData;";
            else
                return "ByteAddressBuffer attributeData;";
        }

        private string GetCastAttributePrefix(VFXAttribute attrib)
        {
            if (VFXExpression.IsFloatValueType(attrib.type))
                return "asfloat";
            return "";
        }

        private string GetByteAddressBufferMethodSuffix(VFXAttribute attrib)
        {
            int size = VFXExpression.TypeToSize(attrib.type);
            if (size == 1)
                return string.Empty;
            else if (size <= 4)
                return size.ToString();
            else
                throw new ArgumentException(string.Format("Attribute {0} of type {1} cannot be handled in ByteAddressBuffer due to its size of {2}", attrib.name, attrib.type, size));
        }

        public override string GetLoadAttributeCode(VFXAttribute attrib, VFXAttributeLocation location)
        {
            var attributeStore = location == VFXAttributeLocation.Current ? m_layoutAttributeCurrent : m_layoutAttributeSource;
            var attributeBuffer = location == VFXAttributeLocation.Current ? "attributeBuffer" : "sourceAttributeBuffer";
            var index = location == VFXAttributeLocation.Current ? "index" : "sourceIndex";

            if (location == VFXAttributeLocation.Current && !m_StoredCurrentAttributes.ContainsKey(attrib))
                throw new ArgumentException(string.Format("Attribute {0} does not exist in data layout", attrib.name));

            if (location == VFXAttributeLocation.Source && !m_ReadSourceAttributes.Any(a => a.name == attrib.name))
                throw new ArgumentException(string.Format("Attribute {0} does not exist in data layout", attrib.name));

            return string.Format("{0}({3}.Load{1}({2}))", GetCastAttributePrefix(attrib), GetByteAddressBufferMethodSuffix(attrib), attributeStore.GetCodeOffset(attrib, index), attributeBuffer);
        }

        public override string GetStoreAttributeCode(VFXAttribute attrib, string value)
        {
            if (!m_StoredCurrentAttributes.ContainsKey(attrib))
                throw new ArgumentException(string.Format("Attribute {0} does not exist in data layout", attrib.name));

            return string.Format("attributeBuffer.Store{0}({1},{3}({2}))", GetByteAddressBufferMethodSuffix(attrib), m_layoutAttributeCurrent.GetCodeOffset(attrib, "index"), value, attrib.type == VFXValueType.Boolean ? "uint" : "asuint");
        }

        public override IEnumerable<VFXContext> InitImplicitContexts()
        {
            if (!NeedsSort())
            {
                m_Contexts = m_Owners;
                return Enumerable.Empty<VFXContext>();
            }

            m_Contexts = new List<VFXContext>(m_Owners.Count + 1);
            int index = 0;

            // First add init and updates
            for (index = 0; index < m_Owners.Count; ++index)
            {
                if ((m_Owners[index].contextType == VFXContextType.kOutput))
                    break;
                m_Contexts.Add(m_Owners[index]);
            }

            // Then the camera sort
            var cameraSort = VFXContext.CreateImplicitContext<VFXCameraSort>(this);
            m_Contexts.Add(cameraSort);

            // And finally output
            for (; index < m_Owners.Count; ++index)
                m_Contexts.Add(m_Owners[index]);

            return new VFXContext[] { cameraSort };
        }

        public bool NeedsIndirectBuffer()
        {
            return owners.OfType<VFXAbstractParticleOutput>().Any(o => o.HasIndirectDraw());
        }

        public bool NeedsSort()
        {
            return owners.OfType<VFXAbstractParticleOutput>().Any(o => o.HasSorting());
        }

        public override void FillDescs(
            List<VFXGPUBufferDesc> outBufferDescs,
            List<VFXEditorSystemDesc> outSystemDescs,
            VFXExpressionGraph expressionGraph,
            Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData,
            Dictionary<VFXContext, int> contextSpawnToBufferIndex,
            Dictionary<VFXData, int> attributeBuffer,
            Dictionary<VFXData, int> eventBuffer)
        {
            bool hasKill = IsAttributeStored(VFXAttribute.Alive);

            var deadListBufferIndex = -1;
            var deadListCountIndex = -1;

            var systemBufferMappings = new List<VFXMapping>();
            var systemValueMappings = new List<VFXMapping>();

            var attributeBufferIndex = attributeBuffer[this];

            int attributeSourceBufferIndex = -1;
            int eventGPUFrom = -1;
            if (m_DependenciesIn.Any())
            {
                if (m_DependenciesIn.Count != 1)
                {
                    throw new InvalidOperationException("Unexpected multiple input dependency for GPU event");
                }
                attributeSourceBufferIndex = attributeBuffer[m_DependenciesIn.FirstOrDefault()];
                eventGPUFrom = eventBuffer[this];
            }

            if (attributeBufferIndex != -1)
            {
                outBufferDescs.Add(m_layoutAttributeCurrent.GetBufferDesc(alignedCapacity));
                systemBufferMappings.Add(new VFXMapping("attributeBuffer", attributeBufferIndex));
            }

            if (m_ownAttributeSourceBuffer && m_layoutAttributeSource.GetBufferSize(sourceCount) > 0u)
            {
                if (attributeSourceBufferIndex != -1)
                {
                    throw new InvalidOperationException("Unexpected source while filling description of data particle");
                }

                attributeSourceBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(m_layoutAttributeSource.GetBufferDesc(sourceCount));
            }

            if (attributeSourceBufferIndex != -1)
            {
                systemBufferMappings.Add(new VFXMapping("sourceAttributeBuffer", attributeSourceBufferIndex));
            }

            var systemFlag = VFXSystemFlag.SystemDefault;
            if (eventGPUFrom != -1)
            {
                systemFlag |= VFXSystemFlag.SystemReceivedEventGPU;
                systemBufferMappings.Add(new VFXMapping("eventList", eventGPUFrom));
            }

            if (hasKill)
            {
                systemFlag |= VFXSystemFlag.SystemHasKill;

                deadListBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Counter, size = capacity, stride = 4 });
                systemBufferMappings.Add(new VFXMapping("deadList", deadListBufferIndex));

                deadListCountIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Raw, size = 1, stride = 4 });
                systemBufferMappings.Add(new VFXMapping("deadListCount", deadListCountIndex));
            }

            var initContext = m_Contexts.FirstOrDefault(o => o.contextType == VFXContextType.kInit);
            if (initContext != null)
                systemBufferMappings.AddRange(initContext.inputContexts.Where(o => o.contextType == VFXContextType.kSpawner).Select(o => new VFXMapping("spawner_input", contextSpawnToBufferIndex[o])));
            if (m_Contexts.Count() > 0 && m_Contexts.First().contextType == VFXContextType.kInit) // TODO This test can be removed once we ensure priorly the system is valid
            {
                var mapper = contextToCompiledData[m_Contexts.First()].cpuMapper;

                var boundsCenterExp = mapper.FromNameAndId("bounds_center", -1);
                var boundsSizeExp = mapper.FromNameAndId("bounds_size", -1);

                int boundsCenterIndex = boundsCenterExp != null ? expressionGraph.GetFlattenedIndex(boundsCenterExp) : -1;
                int boundsSizeIndex = boundsSizeExp != null ? expressionGraph.GetFlattenedIndex(boundsSizeExp) : -1;

                if (boundsCenterIndex != -1 && boundsSizeIndex != -1)
                {
                    systemValueMappings.Add(new VFXMapping("bounds_center", boundsCenterIndex));
                    systemValueMappings.Add(new VFXMapping("bounds_size", boundsSizeIndex));
                }
            }

            int indirectBufferIndex = -1;
            bool needsIndirectBuffer = NeedsIndirectBuffer();
            if (needsIndirectBuffer)
            {
                systemFlag |= VFXSystemFlag.SystemHasIndirectBuffer;
                indirectBufferIndex = outBufferDescs.Count;
                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Counter, size = capacity, stride = 4 });
                systemBufferMappings.Add(new VFXMapping("indirectBuffer", indirectBufferIndex));
            }

            // sort buffers
            int sortBufferAIndex = -1;
            int sortBufferBIndex = -1;
            bool needsSort = NeedsSort();
            if (needsSort)
            {
                sortBufferAIndex = outBufferDescs.Count;
                sortBufferBIndex = sortBufferAIndex + 1;

                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = capacity, stride = 8 });
                systemBufferMappings.Add(new VFXMapping("sortBufferA", sortBufferAIndex));

                outBufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Default, size = capacity, stride = 8 });
                systemBufferMappings.Add(new VFXMapping("sortBufferB", sortBufferBIndex));
            }


            var taskDescs = new List<VFXEditorTaskDesc>();
            var bufferMappings = new List<VFXMapping>();
            var uniformMappings = new List<VFXMapping>();

            for (int i = 0; i < m_Contexts.Count; ++i)
            {
                var context = m_Contexts[i];
                var contextData = contextToCompiledData[context];

                var taskDesc = new VFXEditorTaskDesc();
                taskDesc.type = context.taskType;

                bufferMappings.Clear();

                if (attributeBufferIndex != -1)
                    bufferMappings.Add(new VFXMapping("attributeBuffer", attributeBufferIndex));

                if (eventGPUFrom != -1 && context.contextType == VFXContextType.kInit)
                    bufferMappings.Add(new VFXMapping("eventList", eventGPUFrom));

                if (deadListBufferIndex != -1 && context.contextType != VFXContextType.kOutput && context.taskType != VFXTaskType.CameraSort)
                    bufferMappings.Add(new VFXMapping(context.contextType == VFXContextType.kUpdate ? "deadListOut" : "deadListIn", deadListBufferIndex));

                if (deadListCountIndex != -1 && context.contextType == VFXContextType.kInit)
                    bufferMappings.Add(new VFXMapping("deadListCount", deadListCountIndex));

                if (attributeSourceBufferIndex != -1 && context.contextType == VFXContextType.kInit)
                    bufferMappings.Add(new VFXMapping("sourceAttributeBuffer", attributeSourceBufferIndex));

                if (indirectBufferIndex != -1 &&
                    (context.contextType == VFXContextType.kUpdate ||
                     (context.contextType == VFXContextType.kOutput && (context as VFXAbstractParticleOutput).HasIndirectDraw())))
                {
                    bufferMappings.Add(new VFXMapping(context.taskType == VFXTaskType.CameraSort ? "inputBuffer" : "indirectBuffer", indirectBufferIndex));
                }

                if (deadListBufferIndex != -1 && context.contextType == VFXContextType.kOutput && (context as VFXAbstractParticleOutput).NeedsDeadListCount())
                    bufferMappings.Add(new VFXMapping("deadListCount", deadListCountIndex));

                if (context.taskType == VFXTaskType.CameraSort)
                {
                    bufferMappings.Add(new VFXMapping("outputBuffer", sortBufferAIndex));
                    if (deadListCountIndex != -1)
                        bufferMappings.Add(new VFXMapping("deadListCount", deadListCountIndex));
                }

                var gpuTarget = context.allLinkedOutputSlot.SelectMany(o => (o.owner as VFXContext).outputContexts)
                    .Where(c => c.CanBeCompiled())
                    .Select(o => eventBuffer[o.GetData()])
                    .ToArray();
                for (uint indexTarget = 0; indexTarget < (uint)gpuTarget.Length; ++indexTarget)
                {
                    var prefix = VFXCodeGeneratorHelper.GeneratePrefix(indexTarget);
                    bufferMappings.Add(new VFXMapping(string.Format("eventListOut_{0}", prefix), gpuTarget[indexTarget]));
                }

                uniformMappings.Clear();
                foreach (var uniform in contextData.uniformMapper.uniforms.Concat(contextData.uniformMapper.textures))
                    uniformMappings.Add(new VFXMapping(contextData.uniformMapper.GetName(uniform), expressionGraph.GetFlattenedIndex(uniform)));

                // Retrieve all cpu mappings at context level (-1)
                var cpuMappings = contextData.cpuMapper.CollectExpression(-1).Select(exp => new VFXMapping(exp.name, expressionGraph.GetFlattenedIndex(exp.exp))).ToArray();

                //Check potential issue with invalid operation on CPU
                foreach (var mapping in cpuMappings)
                {
                    if (mapping.index < 0)
                    {
                        throw new InvalidOperationException("Unable to compute CPU expression for mapping : " + mapping.name);
                    }
                }

                taskDesc.buffers = bufferMappings.ToArray();
                taskDesc.values = uniformMappings.ToArray();
                taskDesc.parameters = cpuMappings.Concat(contextData.parameters).ToArray();
                taskDesc.shaderSourceIndex = contextToCompiledData[context].indexInShaderSource;

                taskDescs.Add(taskDesc);
            }

            outSystemDescs.Add(new VFXEditorSystemDesc()
            {
                flags = systemFlag,
                tasks = taskDescs.ToArray(),
                capacity = capacity,
                buffers = systemBufferMappings.ToArray(),
                values = systemValueMappings.ToArray(),
                type = VFXSystemType.Particle,
                layer = m_Layer
            });
        }

        public override void CopySettings<T>(T dst)
        {
            var instance = dst as VFXDataParticle;
            instance.m_Capacity = m_Capacity;
            instance.m_Space = m_Space;
        }

        public StructureOfArrayProvider.BucketInfo[] GetCurrentAttributeLayout()
        {
            return m_layoutAttributeCurrent.GetBucketLayoutInfo();
        }

        public StructureOfArrayProvider.BucketInfo[] GetSourceAttributeLayout()
        {
            return m_layoutAttributeSource.GetBucketLayoutInfo();
        }

        [SerializeField]
        private uint m_Capacity = 65536;
        [SerializeField]
        private VFXCoordinateSpace m_Space;
        [NonSerialized]
        private StructureOfArrayProvider m_layoutAttributeCurrent = new StructureOfArrayProvider();
        [NonSerialized]
        private StructureOfArrayProvider m_layoutAttributeSource = new StructureOfArrayProvider();
        [NonSerialized]
        private bool m_ownAttributeSourceBuffer;
    }
}
