using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using Object = System.Object;

namespace UnityEditor.VFX
{
    abstract class VariantProvider
    {
        protected virtual Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, Object[]>();
            }
        }

        public virtual IEnumerable<IEnumerable<KeyValuePair<string, object>>> ComputeVariants()
        {
            //Default behavior : Cartesian product
            IEnumerable<IEnumerable<object>> empty = new[] { Enumerable.Empty<object>() };
            var arrVariants = variants.Select(o => o.Value as IEnumerable<Object>);
            var combinations = arrVariants.Aggregate(empty, (x, y) => x.SelectMany(accSeq => y.Select(item => accSeq.Concat(new[] { item }))));
            foreach (var combination in combinations)
            {
                var variant = combination.Select((o, i) => new KeyValuePair<string, object>(variants.ElementAt(i).Key, o));
                yield return variant;
            }
        }
    };

    // Attribute used to register VFX type to library
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    class VFXInfoAttribute : Attribute
    {
        public VFXInfoAttribute()
        {
            this.autoRegister = true;
            this.category = "";
            this.type = null;
        }

        public bool autoRegister
        {
            get;
            set;
        }
        public string category
        {
            get;
            set;
        }
        public Type type
        {
            get;
            set;
        }

        public Type variantProvider
        {
            get;
            set;
        }

        public bool experimental
        {
            get;
            set;
        }

        public static VFXInfoAttribute Get(Type type)
        {
            var attribs = type.GetCustomAttributes(typeof(VFXInfoAttribute), false);
            return attribs.Length == 1 ? (VFXInfoAttribute)attribs[0] : null;
        }
    }

    class VFXModelDescriptor
    {
        protected VFXModelDescriptor(VFXModel template, IEnumerable<KeyValuePair<string, Object>> variants = null)
        {
            m_Template = template;
            m_Variants = variants == null ? Enumerable.Empty<KeyValuePair<string, object>>() : variants;
            ApplyVariant(m_Template);
        }

        public bool AcceptParent(VFXModel parent, int index = -1)
        {
            return parent.AcceptChild(m_Template, index);
        }

        protected void ApplyVariant(VFXModel model)
        {
            foreach (var variant in m_Variants)
            {
                model.SetSettingValue(variant.Key, variant.Value);
            }
        }

        private IEnumerable<KeyValuePair<string, object>> m_Variants;
        protected VFXModel m_Template;

        virtual public string name { get { return m_Template.libraryName; } }
        public VFXInfoAttribute info { get { return VFXInfoAttribute.Get(m_Template.GetType()); } }
        public Type modelType { get { return m_Template.GetType(); } }
        public VFXModel model
        {
            get { return m_Template; }
        }
    }

    class VFXModelDescriptor<T> : VFXModelDescriptor where T : VFXModel
    {
        public VFXModelDescriptor(T template, IEnumerable<KeyValuePair<string, Object>> variants = null) : base(template, variants)
        {
        }

        virtual public T CreateInstance()
        {
            var instance = (T)ScriptableObject.CreateInstance(m_Template.GetType());
            ApplyVariant(instance);
            return instance;
        }

        public new T model
        {
            get { return (T)m_Template; }
        }
    }

    class VFXModelDescriptorParameters : VFXModelDescriptor<VFXParameter>
    {
        private string m_name;
        public override string name
        {
            get
            {
                return m_name;
            }
        }

        public VFXModelDescriptorParameters(Type type) : base(ScriptableObject.CreateInstance<VFXParameter>())
        {
            model.Init(type);
            m_name = type.UserFriendlyName();
        }

        public override VFXParameter CreateInstance()
        {
            var instance = base.CreateInstance();
            instance.Init(model.outputSlots[0].property.type);
            return instance;
        }
    }

    static class VFXLibrary
    {
        public static IEnumerable<VFXModelDescriptor<VFXContext>> GetContexts()     { LoadIfNeeded(); return VFXViewPreference.displayExperimentalOperator ? m_ContextDescs : m_ContextDescs.Where(o => !o.info.experimental); }
        public static IEnumerable<VFXModelDescriptor<VFXBlock>> GetBlocks()         { LoadIfNeeded(); return VFXViewPreference.displayExperimentalOperator ? m_BlockDescs : m_BlockDescs.Where(o => !o.info.experimental); }
        public static IEnumerable<VFXModelDescriptor<VFXOperator>> GetOperators()   { LoadIfNeeded(); return VFXViewPreference.displayExperimentalOperator ? m_OperatorDescs : m_OperatorDescs.Where(o => !o.info.experimental); }
        public static IEnumerable<VFXModelDescriptor<VFXSlot>> GetSlots()           { LoadSlotsIfNeeded(); return m_SlotDescs.Values; }
        public static IEnumerable<Type> GetSlotsType()                              { LoadSlotsIfNeeded(); return m_SlotDescs.Keys; }
        public static IEnumerable<VFXModelDescriptorParameters> GetParameters()     { LoadIfNeeded(); return m_ParametersDescs; }

        public static VFXModelDescriptor<VFXSlot> GetSlot(System.Type type)
        {
            LoadSlotsIfNeeded();
            VFXModelDescriptor<VFXSlot> desc;
            m_SlotDescs.TryGetValue(type, out desc);
            return desc;
        }

        public static void ClearLibrary()
        {
            lock (m_Lock)
            {
                if (m_Loaded)
                {
                    Clear(m_ContextDescs);
                    Clear(m_BlockDescs);
                    Clear(m_OperatorDescs);
                    Clear(m_SlotDescs.Values);
                    Clear(m_ContextDescs);
                    Clear(m_ParametersDescs.Cast<VFXModelDescriptor<VFXParameter>>());
                    m_Loaded = false;
                }
            }
        }

        static void Clear<T>(IEnumerable<VFXModelDescriptor<T>> descriptors) where T : VFXModel
        {
            HashSet<ScriptableObject> dependencies = new HashSet<ScriptableObject>();
            foreach (var model in descriptors)
            {
                model.model.CollectDependencies(dependencies);
                dependencies.Add(model.model);
            }
            foreach (var obj in dependencies)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        public static void LoadIfNeeded()
        {
            if (m_Loaded)
                return;

            lock (m_Lock)
            {
                if (!m_Loaded)
                    Load();
            }
        }

        public static void Load()
        {
            LoadSlotsIfNeeded();

            lock (m_Lock)
            {
                ScriptableObject.CreateInstance<LibrarySentinel>();
                m_ContextDescs = LoadModels<VFXContext>();
                m_BlockDescs = LoadModels<VFXBlock>();
                m_OperatorDescs = LoadModels<VFXOperator>();
                m_ParametersDescs = m_SlotDescs.Select(s =>
                {
                    var desc = new VFXModelDescriptorParameters(s.Key);
                    return desc;
                }).ToList();

                m_Loaded = true;
            }
        }

        private static void LoadSlotsIfNeeded()
        {
            if (m_SlotLoaded)
                return;

            lock (m_Lock)
            {
                if (!m_SlotLoaded)
                {
                    m_SlotDescs = LoadSlots();
                    m_SlotLoaded = true;
                }
            }
        }

        private static List<VFXModelDescriptor<T>> LoadModels<T>() where T : VFXModel
        {
            var modelTypes = FindConcreteSubclasses(typeof(T), typeof(VFXInfoAttribute));
            var modelDescs = new List<VFXModelDescriptor<T>>();
            foreach (var modelType in modelTypes)
            {
                try
                {
                    T instance = (T)ScriptableObject.CreateInstance(modelType);
                    var modelDesc = new VFXModelDescriptor<T>(instance);
                    if (modelDesc.info.autoRegister)
                    {
                        if (modelDesc.info.variantProvider != null)
                        {
                            var provider = Activator.CreateInstance(modelDesc.info.variantProvider) as VariantProvider;
                            foreach (var variant in provider.ComputeVariants())
                            {
                                var variantArray = variant.ToArray();
                                modelDescs.Add(new VFXModelDescriptor<T>((T)ScriptableObject.CreateInstance(modelType), variant));
                            }
                        }
                        else
                        {
                            modelDescs.Add(modelDesc);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while loading model from type " + modelType + ": " + e);
                }
            }

            return modelDescs.OrderBy(o => o.name).ToList();
        }

        class LibrarySentinel : ScriptableObject
        {
            void OnDisable()
            {
                VFXLibrary.ClearLibrary();
            }
        }

        private static Dictionary<Type, VFXModelDescriptor<VFXSlot>> LoadSlots()
        {
            // First find concrete slots
            var slotTypes = FindConcreteSubclasses(typeof(VFXSlot), typeof(VFXInfoAttribute));
            var dictionary = new Dictionary<Type, VFXModelDescriptor<VFXSlot>>();
            foreach (var slotType in slotTypes)
            {
                try
                {
                    Type boundType = VFXInfoAttribute.Get(slotType).type; // Not null as it was filtered before
                    if (boundType != null)
                    {
                        if (dictionary.ContainsKey(boundType))
                            throw new Exception(boundType + " was already bound to a slot type");

                        VFXSlot instance = (VFXSlot)ScriptableObject.CreateInstance(slotType);
                        dictionary[boundType] = new VFXModelDescriptor<VFXSlot>(instance);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while loading slot from type " + slotType + ": " + e);
                }
            }

            // Then find types that needs a generic slot
            var vfxTypes = FindConcreteSubclasses(null, typeof(VFXTypeAttribute));
            foreach (var type in vfxTypes)
            {
                if (!dictionary.ContainsKey(type)) // If a slot was not already explicitly declared
                {
                    VFXSlot instance = ScriptableObject.CreateInstance<VFXSlot>();
                    dictionary[type] = new VFXModelDescriptor<VFXSlot>(instance);
                }
            }

            return dictionary;
        }

        public static IEnumerable<Type> FindConcreteSubclasses(Type objectType = null, Type attributeType = null)
        {
            List<Type> types = new List<Type>();
            foreach (var domainAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] assemblyTypes = null;
                try
                {
                    assemblyTypes = domainAssembly.GetTypes();
                }
                catch (Exception)
                {
                    Debug.Log("Cannot access assembly: " + domainAssembly);
                    assemblyTypes = null;
                }
                if (assemblyTypes != null)
                    foreach (var assemblyType in assemblyTypes)
                        if ((objectType == null || assemblyType.IsSubclassOf(objectType)) && !assemblyType.IsAbstract)
                            types.Add(assemblyType);
            }
            return types.Where(type => attributeType == null || type.GetCustomAttributes(attributeType, false).Length == 1);
        }

        private static volatile List<VFXModelDescriptor<VFXContext>> m_ContextDescs;
        private static volatile List<VFXModelDescriptor<VFXOperator>> m_OperatorDescs;
        private static volatile List<VFXModelDescriptor<VFXBlock>> m_BlockDescs;
        private static volatile List<VFXModelDescriptorParameters> m_ParametersDescs;
        private static volatile Dictionary<Type, VFXModelDescriptor<VFXSlot>> m_SlotDescs;

        private static Object m_Lock = new Object();
        private static volatile bool m_Loaded = false;
        private static volatile bool m_SlotLoaded = false;
    }
}
