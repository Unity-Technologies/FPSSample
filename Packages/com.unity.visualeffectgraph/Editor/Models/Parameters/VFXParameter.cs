using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.ObjectModel;

namespace UnityEditor.VFX
{
    class VFXParameter : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        protected VFXParameter()
        {
            m_exposedName = "exposedName";
            m_exposed = false;
            m_UICollapsed = false;
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField]
        private string m_exposedName;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        private bool m_exposed;
        [SerializeField]
        private int m_Order;
        [SerializeField]
        private string m_Category;
        [VFXSetting, SerializeField]
        public VFXSerializableObject m_Min;
        [VFXSetting, SerializeField]
        public VFXSerializableObject m_Max;

        public bool canHaveRange
        {
            get
            {
                return type == typeof(float) || type == typeof(int) || type == typeof(uint);
            }
        }

        public bool hasRange
        {
            get { return canHaveRange && m_Min != null && m_Min.type != null && m_Max != null && m_Max.type != null; }

            set
            {
                if (value != hasRange)
                {
                    if (value)
                    {
                        m_Min = new VFXSerializableObject(type);
                        m_Max = new VFXSerializableObject(type);
                    }
                    else
                    {
                        m_Min = null;
                        m_Max = null;
                    }
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        [SerializeField]
        string m_Tooltip;

        public string tooltip
        {
            get
            {
                return m_Tooltip;
            }

            set
            {
                m_Tooltip = value;
                Invalidate(InvalidationCause.kUIChanged);
            }
        }

        [System.Serializable]
        public struct NodeLinkedSlot
        {
            public VFXSlot outputSlot; // some slot from the parameter
            public VFXSlot inputSlot;
        }

        [System.Serializable]
        public class Node
        {
            public Node(int id)
            {
                m_Id = id;
            }

            [SerializeField]
            private int m_Id;

            public int id { get { return m_Id; } }

            public List<NodeLinkedSlot> linkedSlots;
            public Vector2 position;
            public List<VFXSlot> expandedSlots;
            public bool expanded;


            //Should only be called by ValidateNodes if something very wrong happened with serialization
            internal void ChangeId(int newId)
            {
                m_Id = newId;
            }
        }

        [SerializeField]
        protected List<Node> m_Nodes;

        [NonSerialized]
        int m_IDCounter = 0;

        public string exposedName
        {
            get
            {
                return m_exposedName;
            }
        }

        public bool exposed
        {
            get
            {
                return m_exposed;
            }
        }

        public int order
        {
            get { return m_Order; }
            set
            {
                if (m_Order != value)
                {
                    m_Order = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        public string category
        {
            get { return m_Category; }
            set
            {
                if (m_Category != value)
                {
                    m_Category = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                yield return "m_Min";
                yield return "m_Max";
            }
        }

        public Type type
        {
            get { return outputSlots[0].property.type; }
        }

        public object value
        {
            get { return outputSlots[0].value; }
            set { outputSlots[0].value = value; }
        }


        public ReadOnlyCollection<Node> nodes
        {
            get
            {
                if (m_Nodes == null)
                {
                    m_Nodes = new List<Node>();
                }
                return m_Nodes.AsReadOnly();
            }
        }


        public Node GetNode(int id)
        {
            return m_Nodes.FirstOrDefault(t => t.id == id);
        }

        protected sealed override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            base.OnInvalidate(model, cause);

            if (cause == InvalidationCause.kSettingChanged)
            {
                var valueExpr = m_ExprSlots.Select(t => t.DefaultExpression(valueMode)).ToArray();
                bool valueExprChanged = true;
                if (m_ValueExpr.Length == valueExpr.Length)
                {
                    valueExprChanged = false;
                    for (int i = 0; i < m_ValueExpr.Length; ++i)
                    {
                        if (m_ValueExpr[i].ValueMode != valueExpr[i].ValueMode
                            ||  m_ValueExpr[i].valueType != valueExpr[i].valueType)
                        {
                            valueExprChanged = true;
                            break;
                        }
                    }
                }

                if (valueExprChanged)
                {
                    m_ValueExpr = valueExpr;
                    outputSlots[0].InvalidateExpressionTree();
                    Invalidate(InvalidationCause.kExpressionGraphChanged); // As we need to update exposed list event if not connected to a compilable context
                }
                /* TODO : Allow VisualEffectApi to update only exposed name */
                else if (exposed)
                {
                    Invalidate(InvalidationCause.kExpressionGraphChanged);
                }
            }

            if (cause == InvalidationCause.kParamChanged)
            {
                UpdateDefaultExpressionValue();
            }
        }

        public void UpdateDefaultExpressionValue()
        {
            for (int i = 0; i < m_ExprSlots.Length; ++i)
            {
                m_ValueExpr[i].SetContent(m_ExprSlots[i].value);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties { get { return PropertiesFromSlotsOrDefaultFromClass(VFXSlot.Direction.kOutput); } }

        public void Init(Type _type)
        {
            if (_type != null && outputSlots.Count == 0)
            {
                VFXSlot slot = VFXSlot.Create(new VFXProperty(_type, "o"), VFXSlot.Direction.kOutput);
                AddSlot(slot);

                if (!typeof(UnityEngine.Object).IsAssignableFrom(_type))
                    slot.value = System.Activator.CreateInstance(_type);
            }
            else
            {
                throw new InvalidOperationException("Cannot init VFXParameter");
            }
            m_ExprSlots = outputSlots[0].GetVFXValueTypeSlots().ToArray();
            m_ValueExpr = m_ExprSlots.Select(t => t.DefaultExpression(valueMode)).ToArray();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            if (outputSlots.Count != 0)
            {
                m_ExprSlots = outputSlots[0].GetVFXValueTypeSlots().ToArray();
                m_ValueExpr = m_ExprSlots.Select(t => t.DefaultExpression(valueMode)).ToArray();
            }
            else
            {
                m_ExprSlots = new VFXSlot[0];
                m_ValueExpr = new VFXValue[0];
            }

            if (m_Nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (m_IDCounter < node.id + 1)
                    {
                        m_IDCounter = node.id + 1;
                    }
                }
            }
        }

        Node NewNode()
        {
            return new Node(m_IDCounter++);
        }

        public int AddNode(Vector2 pos)
        {
            Node info = NewNode();

            info.position = pos;

            if (m_Nodes == null)
            {
                m_Nodes = new List<Node>();
            }

            m_Nodes.Add(info);

            Invalidate(InvalidationCause.kUIChanged);

            return info.id;
        }

        public void RemoveNode(Node info)
        {
            if (m_Nodes.Contains(info))
            {
                if (info.linkedSlots != null)
                {
                    foreach (var slots in info.linkedSlots)
                    {
                        slots.outputSlot.Unlink(slots.inputSlot);
                    }
                }
                m_Nodes.Remove(info);

                Invalidate(InvalidationCause.kUIChanged);
            }
        }

        //AddNodeRange will take ownership of the Nodes instead of copying them
        public void AddNodeRange(IEnumerable<Node> infos)
        {
            foreach (var info in infos)
            {
                if (m_Nodes.Any(t => t.id == info.id))
                {
                    info.ChangeId(m_IDCounter++);
                }
                m_Nodes.Add(info);
            }

            Invalidate(InvalidationCause.kUIChanged);
        }

        //SetNodes will take ownership of the Nodes instead of copying them
        public void SetNodes(IEnumerable<Node> infos)
        {
            m_Nodes = infos.ToList();

            ValidateNodes();

            Invalidate(InvalidationCause.kUIChanged);
        }

        void GetAllLinks(List<NodeLinkedSlot> list, VFXSlot slot)
        {
            list.AddRange(slot.LinkedSlots.Select(t => new NodeLinkedSlot() { outputSlot = slot, inputSlot = t }));
            foreach (var child in slot.children)
            {
                GetAllLinks(list, child);
            }
        }

        void GetAllExpandedSlots(List<VFXSlot> list, VFXSlot slot)
        {
            if (!slot.collapsed)
                list.Add(slot);
            foreach (var child in slot.children)
            {
                GetAllExpandedSlots(list, child);
            }
        }

        public void ValidateNodes()
        {
            // Case of the old VFXParameter we create a new one on the same place with all the Links
            if (position != Vector2.zero && nodes.Count == 0)
            {
                CreateDefaultNode(position);
            }
            else
            {
                // the linked slot of the outSlot decides so make sure that all appear once and only once in all the nodes
                List<NodeLinkedSlot> links = new List<NodeLinkedSlot>();
                GetAllLinks(links, outputSlots[0]);
                HashSet<int> usedIds = new HashSet<int>();
                foreach (var info in nodes)
                {
                    // Check linkedSlots
                    if (info.linkedSlots == null)
                    {
                        info.linkedSlots = new List<NodeLinkedSlot>();
                    }
                    else
                    {
                        // first remove linkedSlots that are not existing
                        var intersect = info.linkedSlots.Intersect(links);
                        if (intersect.Count() != info.linkedSlots.Count())
                            info.linkedSlots = info.linkedSlots.Intersect(links).ToList();
                    }

                    //Check that all slots needed for
                    if (info.expandedSlots == null)
                    {
                        info.expandedSlots = new List<VFXSlot>();
                    }

                    if (usedIds.Contains(info.id))
                    {
                        info.ChangeId(m_IDCounter++);
                    }
                    usedIds.Add(info.id);

                    foreach (var slot in info.linkedSlots)
                    {
                        links.Remove(slot);
                    }
                }
                // if there are some links in the output slots that are in none of the infos, create a default param with them
                if (links.Count > 0)
                {
                    var newInfos = NewNode();
                    newInfos.position = Vector2.zero;
                    newInfos.linkedSlots = links;
                    newInfos.expandedSlots = new List<VFXSlot>();
                    m_Nodes.Add(newInfos);
                }
            }
            position = Vector2.zero; // Set that as a marker that the parameter has been touched by the new code.
        }

        public void CreateDefaultNode(Vector2 position)
        {
            if (m_Nodes != null && m_Nodes.Count != 0)
            {
                Debug.LogError("CreateDefaultNode must only be called with an empty parameter");
                return;
            }
            var newInfos = NewNode();
            newInfos.position = position;


            newInfos.linkedSlots = new List<NodeLinkedSlot>();
            GetAllLinks(newInfos.linkedSlots, outputSlots[0]);
            newInfos.expandedSlots = new List<VFXSlot>();
            GetAllExpandedSlots(newInfos.expandedSlots, outputSlots[0]);
            if (m_Nodes == null)
            {
                m_Nodes = new List<Node>();
            }
            m_Nodes.Add(newInfos);
        }

        public override void UpdateOutputExpressions()
        {
            for (int i = 0; i < m_ExprSlots.Length; ++i)
            {
                m_ValueExpr[i].SetContent(m_ExprSlots[i].value);
                m_ExprSlots[i].SetExpression(m_ValueExpr[i]);
            }
        }

        public override void OnCopyLinksOtherSlot(VFXSlot mySlot, VFXSlot prevOtherSlot, VFXSlot newOtherSlot)
        {
            foreach (var node in nodes)
            {
                if (node.linkedSlots != null)
                {
                    for (int i = 0; i < node.linkedSlots.Count; ++i)
                    {
                        if (node.linkedSlots[i].outputSlot == mySlot && node.linkedSlots[i].inputSlot == prevOtherSlot)
                        {
                            node.linkedSlots[i] = new NodeLinkedSlot() { outputSlot = mySlot, inputSlot = newOtherSlot };
                            return;
                        }
                    }
                }
            }
        }

        public override void OnCopyLinksMySlot(VFXSlot myPrevSlot, VFXSlot myNewSlot, VFXSlot otherSlot)
        {
            foreach (var node in nodes)
            {
                if (node.linkedSlots != null)
                {
                    for (int i = 0; i < node.linkedSlots.Count; ++i)
                    {
                        if (node.linkedSlots[i].outputSlot == myPrevSlot && node.linkedSlots[i].inputSlot == otherSlot)
                        {
                            node.linkedSlots[i] = new NodeLinkedSlot() { outputSlot = myNewSlot, inputSlot = otherSlot };
                            return;
                        }
                    }
                }
            }
        }

        private VFXValue.Mode valueMode
        {
            get
            {
                return exposed ? VFXValue.Mode.Variable : VFXValue.Mode.FoldableVariable;
            }
        }

        [NonSerialized]
        private VFXSlot[] m_ExprSlots;

        [NonSerialized]
        private VFXValue[] m_ValueExpr;
    }
}
