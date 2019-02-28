using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using System.Reflection;
using UnityObject = UnityEngine.Object;
using NodeID = System.UInt32;

namespace UnityEditor.VFX.UI
{
    class VFXCopyPasteCommon
    {
        protected const NodeID TypeMask = 3u << 30;
        protected const NodeID ParameterFlag = 1u << 30;
        protected const NodeID ContextFlag = 2u << 30;
        protected const NodeID OperatorFlag = 3u << 30;
        protected const NodeID BlockFlag = 0u << 30;
        protected const NodeID InvalidID = 0xFFFFFFFF;

        //NodeID are 32 identifiers to any node that can be in a groupNode or have links
        //The two high bits are use with the above flags to give the type.
        // For operators and context all the 30 remaining bit are used as an index
        // For blocks the high bits 2 to 13 are a block index in the context the 18 remaining bits are an index in the contexts
        // For parameters the high bits 2 to 13 are a parameter node index the 18 remaining are an index in the parameters
        // Therefore you can copy :
        // Up to 2^30 operators
        // Up to 2^30 contexts, but only the first 2^18 can have blocks with links
        // Up to 2^30 parameters, but only the first 2^18 can have nodes with links
        // Up to 2^11 block per context can have links
        // Up to 2^11 parameter nodes per parameter can have links
        // Note : 2^30 > 1 billion, 2^18 = 262144, 2^11 = 2048


        [Serializable]
        protected struct DataAnchor
        {
            public NodeID targetIndex;
            public int[] slotPath;
        }

        [Serializable]
        protected struct DataEdge
        {
            public DataAnchor input;
            public DataAnchor output;
        }

        [Serializable]
        protected struct FlowAnchor
        {
            public NodeID contextIndex;
            public int flowIndex;
        }


        [Serializable]
        protected struct FlowEdge
        {
            public FlowAnchor input;
            public FlowAnchor output;
        }

        [Serializable]
        protected struct Property
        {
            public string name;
            public VFXSerializableObject value;
        }

        [Serializable]
        protected struct Node
        {
            public Vector2 position;

            [Flags]
            public enum Flags
            {
                Collapsed = 1 << 0,
                SuperCollapsed = 1 << 1,
                Enabled = 1 << 2
            }
            public Flags flags;

            public SerializableType type;
            public Property[] settings;
            public Property[] inputSlots;
            public string[] expandedInputs;
            public string[] expandedOutputs;
        }


        [Serializable]
        protected struct Context
        {
            public Node node;
            public int dataIndex;
            public string label;
            public Node[] blocks;
        }

        [Serializable]
        protected struct Data
        {
            public Property[] settings;
        }

        [Serializable]
        protected struct ParameterNode
        {
            public Vector2 position;
            public bool collapsed;
            public string[] expandedOutput;
        }

        [Serializable]
        protected struct Parameter
        {
            public int originalInstanceID;
            public string name;
            public VFXSerializableObject value;
            public bool exposed;
            public bool range;
            public VFXSerializableObject min;
            public VFXSerializableObject max;
            public string tooltip;
            public ParameterNode[] nodes;
        }

        [Serializable]
        protected struct GroupNode
        {
            public VFXUI.UIInfo infos;
            public NodeID[] contents;
            public int stickNodeCount;
        }

        [Serializable]
        protected class SerializableGraph
        {
            public Rect bounds;

            public bool blocksOnly;

            public Context[] contexts;
            public Node[] operatorsOrBlocks; // this contains blocks if blocksOnly else it contains operators and blocks are included in their respective contexts
            public Data[] datas;

            public Parameter[] parameters;

            public DataEdge[] dataEdges;
            public FlowEdge[] flowEdges;

            public VFXUI.StickyNoteInfo[] stickyNotes;
            public GroupNode[] groupNodes;
        }

        static Dictionary<Type, List<FieldInfo>> s_SerializableFieldByType = new Dictionary<Type, List<FieldInfo>>();
        protected static List<FieldInfo> GetFields(Type type)
        {
            List<FieldInfo> fields = null;
            if (!s_SerializableFieldByType.TryGetValue(type, out fields))
            {
                fields = new List<FieldInfo>();
                while (type != typeof(VFXContext) && type != typeof(VFXOperator) && type != typeof(VFXBlock) && type != typeof(VFXData))
                {
                    var typeFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                    foreach (var field in typeFields)
                    {
                        if (!field.IsPublic)
                        {
                            object[] attributes = field.GetCustomAttributes(true);

                            if (!attributes.Any(t => t is VFXSettingAttribute || t is SerializeField))
                                continue;
                        }

                        if (field.IsNotSerialized)
                            continue;

                        if (!field.FieldType.IsSerializable && !typeof(UnityObject).IsAssignableFrom(field.FieldType)) // Skip field that are not serializable except for UnityObject that are anyway
                            continue;

                        if (typeof(VFXModel).IsAssignableFrom(field.FieldType) || field.FieldType.IsGenericType && typeof(VFXModel).IsAssignableFrom(field.FieldType.GetGenericArguments()[0]))
                            continue;

                        fields.Add(field);
                    }
                    type = type.BaseType;
                }
                s_SerializableFieldByType[type] = fields;
            }

            return fields;
        }

        protected static IEnumerable<VFXSlot> AllSlots(IEnumerable<VFXSlot> slots)
        {
            foreach (var slot in slots)
            {
                yield return slot;

                foreach (var child in AllSlots(slot.children))
                {
                    yield return child;
                }
            }
        }

        protected static NodeID GetBlockID(uint contextIndex, uint blockIndex)
        {
            if (contextIndex < (1 << 18) && blockIndex < (1 << 11))
            {
                return BlockFlag | (blockIndex << 18) | contextIndex;
            }
            return InvalidID;
        }

        protected static NodeID GetParameterNodeID(uint parameterIndex, uint nodeIndex)
        {
            if (parameterIndex < (1 << 18) && nodeIndex < (1 << 11))
            {
                return ParameterFlag | (nodeIndex << 18) | parameterIndex;
            }
            return InvalidID;
        }
    }
}
