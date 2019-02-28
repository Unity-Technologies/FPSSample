using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    // TODO Move this
    // Must match enum in C++
    public enum VFXCoordinateSpace
    {
        Local = 0,
        World = 1,
    }

    // TODO Move this
    public interface ISpaceable
    {
        VFXCoordinateSpace space { get; set; }
    }

    abstract class VFXData : VFXModel
    {
        public abstract VFXDataType type { get; }

        public virtual uint sourceCount
        {
            get
            {
                return 0u;
            }
        }

        public IEnumerable<VFXContext> owners
        {
            get { return m_Owners; }
        }

        public string title
        {
            get;set;
        }

        public int index
        {
            get
            {
                if ( m_Parent == null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(this);
                    m_Parent = VisualEffectResource.GetResourceAtPath(assetPath).GetOrCreateGraph();
                }
                
                VFXGraph graph = GetGraph();

                HashSet<VFXData> datas = new HashSet<VFXData>();

                foreach (var child in graph.children.OfType<VFXContext>())
                {
                        VFXData data = (child as VFXContext).GetData();
                        if (data != null)
                            datas.Add(data);
                        if (data == this)
                            return datas.Count();
                }
                throw new InvalidOperationException("Can't determine index of a VFXData without context");
            }
        }

        public string fileName {
            get {

                int i = this.index;
                if (i < 0)
                    return string.Empty;
                return string.IsNullOrEmpty(title)?string.Format("System {0}",index):title;
            }
        }

        public IEnumerable<VFXContext> implicitContexts
        {
            get { return Enumerable.Empty<VFXContext>(); }
        }

        public static VFXData CreateDataType(VFXGraph graph,VFXDataType type)
        {
            VFXData newVFXData;
            switch (type)
            {
                case VFXDataType.kParticle:
                    newVFXData = ScriptableObject.CreateInstance<VFXDataParticle>();
                    break;
                case VFXDataType.kMesh:
                    newVFXData = ScriptableObject.CreateInstance<VFXDataMesh>();
                    break;
                default:                        return null;
            }
            newVFXData.m_Parent = graph;
            return newVFXData;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_Owners == null)
                m_Owners = new List<VFXContext>();
            else
            {
                // Remove bad references if any
                // The code below was replaced because it caused some strange crashes for unknown reasons
                //int nbRemoved = m_Owners.RemoveAll(o => o == null);
                int nbRemoved = 0;
                for (int i = 0; i < m_Owners.Count; ++i)
                    if (m_Owners[i] == null)
                    {
                        m_Owners.RemoveAt(i--);
                        ++nbRemoved;
                    }

                if (nbRemoved > 0)
                    Debug.Log(String.Format("Remove {0} owners that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }
        }

        public override void Sanitize(int version)
        {
            base.Sanitize(version);

            if( m_Parent == null)
            {
                string assetPath = AssetDatabase.GetAssetPath(this);
                m_Parent = VisualEffectResource.GetResourceAtPath(assetPath).GetOrCreateGraph();
            }
        }

        public abstract void CopySettings<T>(T dst) where T : VFXData;

        public virtual bool CanBeCompiled()
        {
            return true;
        }

        public virtual void FillDescs(
            List<VFXGPUBufferDesc> outBufferDescs,
            List<VFXEditorSystemDesc> outSystemDescs,
            VFXExpressionGraph expressionGraph,
            Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData,
            Dictionary<VFXContext, int> contextSpawnToBufferIndex,
            Dictionary<VFXData, int> attributeBuffer,
            Dictionary<VFXData, int> eventBuffer)
        {
            // Empty implementation by default
        }

        // Never call this directly ! Only context must call this through SetData
        public void OnContextAdded(VFXContext context)
        {
            if (context == null)
                throw new ArgumentNullException();
            if (m_Owners.Contains(context))
                throw new ArgumentException(string.Format("{0} is already in the owner list of {1}", context, this));

            m_Owners.Add(context);
        }

        // Never call this directly ! Only context must call this through SetData
        public void OnContextRemoved(VFXContext context)
        {
            if (!m_Owners.Remove(context))
                throw new ArgumentException(string.Format("{0} is not in the owner list of {1}", context, this));
        }

        public bool IsCurrentAttributeRead(VFXAttribute attrib)                        { return (GetAttributeMode(attrib) & VFXAttributeMode.Read) != 0; }
        public bool IsCurrentAttributeWritten(VFXAttribute attrib)                     { return (GetAttributeMode(attrib) & VFXAttributeMode.Write) != 0; }

        public bool IsCurrentAttributeRead(VFXAttribute attrib, VFXContext context)     { return (GetAttributeMode(attrib, context) & VFXAttributeMode.Read) != 0; }
        public bool IsCurrentAttributeWritten(VFXAttribute attrib, VFXContext context) { return (GetAttributeMode(attrib, context) & VFXAttributeMode.Write) != 0; }

        public bool IsAttributeUsed(VFXAttribute attrib)                                { return GetAttributeMode(attrib) != VFXAttributeMode.None; }
        public bool IsAttributeUsed(VFXAttribute attrib, VFXContext context)            { return GetAttributeMode(attrib, context) != VFXAttributeMode.None; }

        public bool IsCurrentAttributeUsed(VFXAttribute attrib)                         { return (GetAttributeMode(attrib) & VFXAttributeMode.ReadWrite) != 0; }

        public bool IsSourceAttributeUsed(VFXAttribute attrib)                          { return (GetAttributeMode(attrib) & VFXAttributeMode.ReadSource) != 0; }
        public bool IsSourceAttributeUsed(VFXAttribute attrib, VFXContext context)      { return (GetAttributeMode(attrib, context) & VFXAttributeMode.ReadSource) != 0; }

        public bool IsAttributeLocal(VFXAttribute attrib)                               { return m_LocalCurrentAttributes.Contains(attrib); }
        public bool IsAttributeStored(VFXAttribute attrib)                              { return m_StoredCurrentAttributes.ContainsKey(attrib); }

        public VFXAttributeMode GetAttributeMode(VFXAttribute attrib, VFXContext context)
        {
            Dictionary<VFXContext, VFXAttributeMode> contexts;
            if (m_AttributesToContexts.TryGetValue(attrib, out contexts))
            {
                foreach (var c in contexts)
                    if (c.Key == context)
                        return c.Value;
            }

            return VFXAttributeMode.None;
        }

        public VFXAttributeMode GetAttributeMode(VFXAttribute attrib)
        {
            VFXAttributeMode mode = VFXAttributeMode.None;
            Dictionary<VFXContext, VFXAttributeMode> contexts;
            if (m_AttributesToContexts.TryGetValue(attrib, out contexts))
            {
                foreach (var context in contexts)
                    mode |= context.Value;
            }

            return mode;
        }

        public int GetNbAttributes()
        {
            return m_AttributesToContexts.Count;
        }

        public IEnumerable<VFXAttributeInfo> GetAttributes()
        {
            foreach (var attrib in m_AttributesToContexts)
            {
                VFXAttributeInfo info;
                info.attrib = attrib.Key;
                info.mode = VFXAttributeMode.None;

                foreach (var context in attrib.Value)
                    info.mode |= context.Value;

                yield return info;
            }
        }

        public IEnumerable<VFXAttributeInfo> GetAttributesForContext(VFXContext context)
        {
            Dictionary<VFXAttribute, VFXAttributeMode> attribs;
            if (m_ContextsToAttributes.TryGetValue(context, out attribs))
            {
                foreach (var attrib in attribs)
                {
                    VFXAttributeInfo info;
                    info.attrib = attrib.Key;
                    info.mode = attrib.Value;
                    yield return info;
                }
            }
            else
                throw new ArgumentException("Context does not exist");
        }

        private struct VFXAttributeInfoContext
        {
            public VFXAttributeInfo[] attributes;
            public VFXContext context;
        }

        public abstract VFXDeviceTarget GetCompilationTarget(VFXContext context);

        // Create implicit contexts and initialize cached contexts list
        public virtual IEnumerable<VFXContext> InitImplicitContexts()
        {
            m_Contexts = m_Owners;
            return Enumerable.Empty<VFXContext>();
        }

        public void CollectAttributes()
        {
            if (m_Contexts == null) // Context hasnt been initialized (may happen in unity tests but not during actual compilation)
                InitImplicitContexts();

            m_DependenciesIn = new HashSet<VFXData>(
                m_Contexts.Where(c => c.contextType == VFXContextType.kInit)
                    .SelectMany(c => c.inputContexts.Where(i => i.contextType == VFXContextType.kSpawnerGPU))
                    .SelectMany(c => c.allLinkedInputSlot)
                    .Where(s =>
                    {
                        if (s.owner is VFXBlock)
                        {
                            VFXBlock block = (VFXBlock)(s.owner);
                            if (block.enabled)
                                return true;
                        }
                        else if (s.owner is VFXContext)
                        {
                            return true;
                        }

                        return false;
                    })
                    .Select(s => ((VFXModel)s.owner).GetFirstOfType<VFXContext>())
                    .Where(c => c.CanBeCompiled())
                    .Select(c => c.GetData())
            );

            m_DependenciesOut = new HashSet<VFXData>(
                owners.SelectMany(o => o.allLinkedOutputSlot)
                    .Select(s => (VFXContext)s.owner)
                    .Where(c => c.CanBeCompiled())
                    .SelectMany(c => c.outputContexts)
                    .Where(c => c.CanBeCompiled())
                    .Select(c => c.GetData())
            );

            m_ContextsToAttributes.Clear();
            m_AttributesToContexts.Clear();
            var processedExp = new HashSet<VFXExpression>();

            bool changed = true;
            int count = 0;
            while (changed)
            {
                ++count;
                var attributeContexts = new List<VFXAttributeInfoContext>();
                foreach (var context in m_Contexts)
                {
                    processedExp.Clear();

                    var attributes = Enumerable.Empty<VFXAttributeInfo>();
                    attributes = attributes.Concat(context.attributes);
                    foreach (var block in context.activeChildrenWithImplicit)
                        attributes = attributes.Concat(block.attributes);

                    var mapper = context.GetExpressionMapper(GetCompilationTarget(context));
                    if (mapper != null)
                        foreach (var exp in mapper.expressions)
                            attributes = attributes.Concat(CollectInputAttributes(exp, processedExp));

                    attributeContexts.Add(new VFXAttributeInfoContext
                    {
                        attributes = attributes.ToArray(),
                        context = context
                    });
                }

                changed = false;
                foreach (var context in attributeContexts)
                {
                    foreach (var attribute in context.attributes)
                    {
                        if (AddAttribute(context.context, attribute))
                        {
                            changed = true;
                        }
                    }
                }
            }

            ProcessAttributes();

            //TMP Debug only
            DebugLogAttributes();
        }

        public void ProcessDependencies()
        {
            ComputeLayer();

            // Update attributes
            foreach (var childData in m_DependenciesOut)
            {
                foreach (var attrib in childData.m_ReadSourceAttributes)
                { 
                    if (!m_StoredCurrentAttributes.ContainsKey(attrib))
                    {
                        m_LocalCurrentAttributes.Remove(attrib);
                        m_StoredCurrentAttributes.Add(attrib, 0);
                    }
                }
            }
        }

        private static uint ComputeLayer(IEnumerable<VFXData> dependenciesIn)
        {
            if (dependenciesIn.Any())
            {
                return 1u + ComputeLayer(dependenciesIn.SelectMany(o => o.m_DependenciesIn));
            }
            return 0u;
        }

        private void ComputeLayer()
        {
            if (!m_DependenciesIn.Any() && !m_DependenciesOut.Any())
            {
                m_Layer = uint.MaxValue; //Completely independent system
            }
            else
            {
                m_Layer = ComputeLayer(m_DependenciesIn);
            }
        }

        protected bool HasImplicitInit(VFXAttribute attrib)
        {
            return (attrib.Equals(VFXAttribute.Seed) || attrib.Equals(VFXAttribute.ParticleId));
        }

        private void ProcessAttributes()
        {
            m_StoredCurrentAttributes.Clear();
            m_LocalCurrentAttributes.Clear();
            m_ReadSourceAttributes.Clear();
            if (type == VFXDataType.kParticle)
            {
                m_ReadSourceAttributes.Add(new VFXAttribute("spawnCount", VFXValueType.Float)); // TODO dirty
            }

            int contextCount = m_Contexts.Count;
            if (contextCount > 16)
                throw new InvalidOperationException(string.Format("Too many contexts that use particle data {0} > 16", contextCount));

            foreach (var kvp in m_AttributesToContexts)
            {
                bool local = false;
                var attribute = kvp.Key;
                int key = 0;

                bool onlyInit = true;
                bool onlyOutput = true;
                bool onlyUpdateRead = true;
                bool onlyUpdateWrite = true;
                bool needsSpecialInit = HasImplicitInit(attribute);
                bool writtenInInit = needsSpecialInit;
                bool readSourceInInit = false;

                foreach (var kvp2 in kvp.Value)
                {
                    var context = kvp2.Key;
                    if (context.contextType == VFXContextType.kInit
                        &&  (kvp2.Value & VFXAttributeMode.ReadSource) != 0)
                    {
                        readSourceInInit = true;
                    }

                    if (kvp2.Value == VFXAttributeMode.None)
                    {
                        throw new InvalidOperationException("Unexpected attribute mode : " + attribute);
                    }

                    if (kvp2.Value == VFXAttributeMode.ReadSource)
                    {
                        continue;
                    }

                    if (context.contextType != VFXContextType.kInit)
                        onlyInit = false;
                    if (context.contextType != VFXContextType.kOutput)
                        onlyOutput = false;
                    if (context.contextType != VFXContextType.kUpdate)
                    {
                        onlyUpdateRead = false;
                        onlyUpdateWrite = false;
                    }
                    else
                    {
                        if ((kvp2.Value & VFXAttributeMode.Read) != 0)
                            onlyUpdateWrite = false;
                        if ((kvp2.Value & VFXAttributeMode.Write) != 0)
                            onlyUpdateRead = false;
                    }

                    if (context.contextType != VFXContextType.kInit) // Init isnt taken into account for key computation
                    {
                        int shift = m_Contexts.IndexOf(context) << 1;
                        int value = 0;
                        if ((kvp2.Value & VFXAttributeMode.Read) != 0)
                            value |= 0x01;
                        if (((kvp2.Value & VFXAttributeMode.Write) != 0) && context.contextType == VFXContextType.kUpdate)
                            value |= 0x02;
                        key |= (value << shift);
                    }
                    else if ((kvp2.Value & VFXAttributeMode.Write) != 0)
                        writtenInInit = true;
                }

                if ((key & ~0xAAAAAAAA) == 0) // no read
                    local = true;
                if (onlyUpdateWrite || onlyInit || (!needsSpecialInit && (onlyUpdateRead || onlyOutput))) // no shared atributes
                    local = true;
                if (!writtenInInit && (key & 0xAAAAAAAA) == 0) // no write mask
                    local = true;
                if (VFXAttribute.AllAttributeLocalOnly.Contains(attribute))
                    local = true;

                if (local)
                    m_LocalCurrentAttributes.Add(attribute);
                else
                    m_StoredCurrentAttributes.Add(attribute, key);

                if (readSourceInInit)
                    m_ReadSourceAttributes.Add(attribute);
            }
        }

        public abstract void GenerateAttributeLayout();

        public abstract string GetAttributeDataDeclaration(VFXAttributeMode mode);
        public abstract string GetLoadAttributeCode(VFXAttribute attrib, VFXAttributeLocation location);
        public abstract string GetStoreAttributeCode(VFXAttribute attrib, string value);

        private bool AddAttribute(VFXContext context, VFXAttributeInfo attribInfo)
        {
            if (attribInfo.mode == VFXAttributeMode.None)
                throw new ArgumentException("Cannot add an attribute without mode");

            Dictionary<VFXAttribute, VFXAttributeMode> attribs;
            if (!m_ContextsToAttributes.TryGetValue(context, out attribs))
            {
                attribs = new Dictionary<VFXAttribute, VFXAttributeMode>();
                m_ContextsToAttributes.Add(context, attribs);
            }

            var attrib = attribInfo.attrib;
            var mode = attribInfo.mode;

            bool hasChanged = false;
            if (attribs.ContainsKey(attrib))
            {
                var oldMode = attribs[attrib];
                mode |= attribs[attrib];
                if (mode != oldMode)
                {
                    attribs[attrib] = mode;
                    hasChanged = true;
                }
            }
            else
            {
                attribs[attrib] = mode;
                hasChanged = true;
            }

            if (hasChanged)
            {
                Dictionary<VFXContext, VFXAttributeMode> contexts;
                if (!m_AttributesToContexts.TryGetValue(attrib, out contexts))
                {
                    contexts = new Dictionary<VFXContext, VFXAttributeMode>();
                    m_AttributesToContexts.Add(attrib, contexts);
                }
                contexts[context] = mode;
            }

            return hasChanged;
        }

        // Collect attribute expressions recursively
        private IEnumerable<VFXAttributeInfo> CollectInputAttributes(VFXExpression exp, HashSet<VFXExpression> processed)
        {
            if (!processed.Contains(exp) && exp.Is(VFXExpression.Flags.PerElement)) // Testing per element allows to early out as it is propagated
            {
                processed.Add(exp);

                foreach (var info in exp.GetNeededAttributes())
                    yield return info;

                foreach (var parent in exp.parents)
                {
                    foreach (var info in CollectInputAttributes(parent, processed))
                        yield return info;
                }
            }
        }

        private void DebugLogAttributes()
        {
            if (!VFXViewPreference.advancedLogs)
                return;

            var builder = new StringBuilder();

            builder.AppendLine(string.Format("Attributes for data {0} of type {1}", GetHashCode(), GetType()));
            foreach (var context in m_Contexts)
            {
                Dictionary<VFXAttribute, VFXAttributeMode> attributeInfos;
                if (m_ContextsToAttributes.TryGetValue(context, out attributeInfos))
                {
                    builder.AppendLine(string.Format("\tContext {1} {0}", context.GetHashCode(), context.contextType));
                    foreach (var kvp in attributeInfos)
                        builder.AppendLine(string.Format("\t\tAttribute {0} {1} {2}", kvp.Key.name, kvp.Key.type, kvp.Value));
                }
            }

            if (m_StoredCurrentAttributes.Count > 0)
            {
                builder.AppendLine("--- STORED CURRENT ATTRIBUTES ---");
                foreach (var kvp in m_StoredCurrentAttributes)
                    builder.AppendLine(string.Format("\t\tAttribute {0} {1} {2}", kvp.Key.name, kvp.Key.type, kvp.Value));
            }

            if (m_AttributesToContexts.Count > 0)
            {
                builder.AppendLine("--- LOCAL CURRENT ATTRIBUTES ---");
                foreach (var attrib in m_LocalCurrentAttributes)
                    builder.AppendLine(string.Format("\t\tAttribute {0} {1}", attrib.name, attrib.type));
            }

            Debug.Log(builder.ToString());
        }

        public uint layer
        {
            get
            {
                return m_Layer;
            }
        }

        public IEnumerable<VFXData> dependenciesIn
        {
            get
            {
                return m_DependenciesIn;
            }
        }

        public IEnumerable<VFXData> dependenciesOut
        {
            get
            {
                return m_DependenciesOut;
            }
        }

        [SerializeField]
        protected List<VFXContext> m_Owners;

        [NonSerialized]
        protected List<VFXContext> m_Contexts;

        [NonSerialized]
        protected Dictionary<VFXContext, Dictionary<VFXAttribute, VFXAttributeMode>> m_ContextsToAttributes = new Dictionary<VFXContext, Dictionary<VFXAttribute, VFXAttributeMode>>();
        [NonSerialized]
        protected Dictionary<VFXAttribute, Dictionary<VFXContext, VFXAttributeMode>> m_AttributesToContexts = new Dictionary<VFXAttribute, Dictionary<VFXContext, VFXAttributeMode>>();

        [NonSerialized]
        protected Dictionary<VFXAttribute, int> m_StoredCurrentAttributes = new Dictionary<VFXAttribute, int>();
        [NonSerialized]
        protected HashSet<VFXAttribute> m_LocalCurrentAttributes = new HashSet<VFXAttribute>();

        [NonSerialized]
        protected HashSet<VFXAttribute> m_ReadSourceAttributes = new HashSet<VFXAttribute>();

        [NonSerialized]
        protected HashSet<VFXData> m_DependenciesIn = new HashSet<VFXData>();

        [NonSerialized]
        protected HashSet<VFXData> m_DependenciesOut = new HashSet<VFXData>();

        [NonSerialized]
        protected uint m_Layer;
    }
}
