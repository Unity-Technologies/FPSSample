//#define OLD_COPY_PASTE
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Profiling;

namespace UnityEditor.VFX.UI
{
    class GroupNodeAdder
    {
    }

    class VFXNodeProvider : VFXAbstractProvider<VFXNodeProvider.Descriptor>
    {
        public class Descriptor
        {
            public object modelDescriptor;
            public string category;
            public string name;
        }

        Func<Descriptor, bool> m_Filter;
        IEnumerable<Type> m_AcceptedTypes;
        VFXViewController m_Controller;

        public VFXNodeProvider(VFXViewController controller, Action<Descriptor, Vector2> onAddBlock, Func<Descriptor, bool> filter = null, IEnumerable<Type> acceptedTypes = null) : base(onAddBlock)
        {
            m_Filter = filter;
            m_AcceptedTypes = acceptedTypes;
            m_Controller = controller;
        }

        protected override string GetCategory(Descriptor desc)
        {
            return desc.category;
        }

        protected override string GetName(Descriptor desc)
        {
            return desc.name;
        }

        protected override string title
        {
            get {return "Node"; }
        }

        string ComputeCategory<T>(string type, VFXModelDescriptor<T> model) where T : VFXModel
        {
            if (model.info != null && model.info.category != null)
            {
                if (m_AcceptedTypes != null && m_AcceptedTypes.Count() == 1)
                {
                    return model.info.category;
                }
                else
                {
                    return string.Format("{0}/{1}", type, model.info.category);
                }
            }
            else
            {
                return type;
            }
        }

        protected override IEnumerable<Descriptor> GetDescriptors()
        {
            IEnumerable<Descriptor> descs = Enumerable.Empty<Descriptor>();

            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXContext)))
            {
                var descriptorsContext = VFXLibrary.GetContexts().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = ComputeCategory("Context", o),
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

                descs = descs.Concat(descriptorsContext);
            }
            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXOperator)))
            {
                var descriptorsOperator = VFXLibrary.GetOperators().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = ComputeCategory("Operator", o),
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

                descs = descs.Concat(descriptorsOperator);
            }
            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXParameter)))
            {
                var parameterDescriptors = m_Controller.parameterControllers.Select(t =>
                    new Descriptor
                    {
                        modelDescriptor = t,
                        category = string.IsNullOrEmpty(t.model.category) ? "Parameter" : string.Format("Parameter/{0}", t.model.category),
                        name = t.exposedName
                    }
                    ).OrderBy(t => t.category);
                descs = descs.Concat(parameterDescriptors);
            }
            if (m_AcceptedTypes == null)
            {
                var systemFiles = System.IO.Directory.GetFiles(VisualEffectAssetEditorUtility.templatePath, "*.vfx").Select(t => t.Replace("\\", "/"));
                var systemDesc = systemFiles.Select(t => new Descriptor() { modelDescriptor = t.Replace(VisualEffectGraphPackageInfo.fileSystemPackagePath, VisualEffectGraphPackageInfo.assetPackagePath), category = "System", name = System.IO.Path.GetFileNameWithoutExtension(t) });

                descs = descs.Concat(systemDesc);
            }
            var groupNodeDesc = new Descriptor()
            {
                modelDescriptor = new GroupNodeAdder(),
                category = "Misc",
                name = "Group Node"
            };

            descs = descs.Concat(Enumerable.Repeat(groupNodeDesc, 1));

            if (m_Filter == null)
                return descs;
            else
                return descs.Where(t => m_Filter(t));
        }
    }
}
