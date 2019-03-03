using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental;
using UnityEditor.VFX.Block;

namespace UnityEditor.VFX.UI
{
    abstract class VFXAbstractProvider<T> : VFXFilterWindow.IProvider
    {
        Action<T, Vector2> m_onSpawnDesc;

        protected class VFXBlockElement : VFXFilterWindow.Element
        {
            public T descriptor { get; private set; }

            public VFXBlockElement(int level, T desc, string category, string name)
            {
                this.level = level;
                var str = name;
                if (!string.IsNullOrEmpty(category))
                    str += " (" + category.Replace("/", " ") + ") ";
                content = new GUIContent(str /*, VFXEditor.styles.GetIcon(desc.Icon)*/);
                descriptor = desc;
            }
        }

        protected VFXAbstractProvider(Action<T, Vector2> onSpawnDesc)
        {
            m_onSpawnDesc = onSpawnDesc;
        }

        protected abstract IEnumerable<T> GetDescriptors();
        protected abstract string GetName(T desc);
        protected abstract string GetCategory(T desc);

        protected abstract string title
        {
            get;
        }

        public void CreateComponentTree(List<VFXFilterWindow.Element> tree)
        {
            tree.Add(new VFXFilterWindow.GroupElement(0, title));
            var descriptors = GetDescriptors();

            string prevCategory = "";
            int depth = 1;

            foreach (var desc in descriptors)
            {
                var category = GetCategory(desc);
                if (category == null)
                    category = "";

                if (category != prevCategory)
                {
                    depth = 0;

                    var split = category.Split('/').Where(o => o != "").ToArray();
                    var prevSplit = prevCategory.Split('/').Where(o => o != "").ToArray();

                    while ((depth < split.Length) && (depth < prevSplit.Length) && (split[depth] == prevSplit[depth]))
                        depth++;

                    while (depth < split.Length)
                    {
                        tree.Add(new VFXFilterWindow.GroupElement(depth + 1, split[depth]));
                        depth++;
                    }

                    depth++;
                }

                tree.Add(new VFXBlockElement(depth, desc, category, GetName(desc)));
                prevCategory = category;
            }
        }

        public bool GoToChild(VFXFilterWindow.Element element, bool addIfComponent)
        {
            if (element is VFXBlockElement)
            {
                var blockElem = element as VFXBlockElement;
                m_onSpawnDesc(blockElem.descriptor, position);
                return true;
            }
            return false;
        }

        public Vector2 position
        {
            get; set;
        }
    }

    class VFXBlockProvider : VFXAbstractProvider<VFXModelDescriptor<VFXBlock>>
    {
        VFXContextController m_ContextController;
        public VFXBlockProvider(VFXContextController context, Action<VFXModelDescriptor<VFXBlock>, Vector2> onAddBlock) : base(onAddBlock)
        {
            m_ContextController = context;
        }

        protected override string GetCategory(VFXModelDescriptor<VFXBlock> desc)
        {
            return desc.info.category;
        }

        protected override string GetName(VFXModelDescriptor<VFXBlock> desc)
        {
            return desc.name;
        }

        protected override string title
        {
            get {return "Block"; }
        }

        protected override IEnumerable<VFXModelDescriptor<VFXBlock>> GetDescriptors()
        {
            var blocks = new List<VFXModelDescriptor<VFXBlock>>(VFXLibrary.GetBlocks());
            var filteredBlocks = blocks.Where(b => b.AcceptParent(m_ContextController.model)).ToList();
            filteredBlocks.Sort((blockA, blockB) =>
            {
                var infoA = blockA.info;
                var infoB = blockB.info;
                int res = infoA.category.CompareTo(infoB.category);
                return res != 0 ? res : blockA.name.CompareTo(blockB.name);
            });
            return filteredBlocks;
        }
    }
}
