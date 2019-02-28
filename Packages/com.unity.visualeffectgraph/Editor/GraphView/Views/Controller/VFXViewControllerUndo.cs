using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEditor.Experimental.VFX;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXGraphUndoStack
    {
        public VFXGraphUndoStack(VFXGraph initialState)
        {
            m_graphUndoCursor = ScriptableObject.CreateInstance<VFXGraphUndoCursor>();

            m_graphUndoCursor.hideFlags = HideFlags.HideAndDontSave;
            m_undoStack = new SortedDictionary<int, SerializedState>();

            m_graphUndoCursor.index = 0;
            m_lastGraphUndoCursor = 0;
            m_undoStack.Add(0, new SerializedState() {serializedGraph = initialState.Backup()});
            m_Graph = initialState;
        }

        VFXGraph m_Graph;

        public void IncrementGraphState()
        {
            Undo.RecordObject(m_graphUndoCursor, string.Format("VFXGraph ({0})", m_graphUndoCursor.index + 1));
            m_graphUndoCursor.index = m_graphUndoCursor.index + 1;
        }

        public bool IsDirtyState()
        {
            return m_lastGraphUndoCursor != m_graphUndoCursor.index;
        }

        public void FlushAndPushGraphState(VFXGraph graph)
        {
            int lastCursorInStack = m_undoStack.Last().Key;
            while (lastCursorInStack > m_lastGraphUndoCursor)
            {
                //An action has been performed which overwrite
                m_undoStack.Remove(lastCursorInStack);
                lastCursorInStack = m_undoStack.Last().Key;
            }
            m_undoStack.Add(m_graphUndoCursor.index, new SerializedState() {serializedGraph = graph.Backup()});
        }

        public void CleanDirtyState()
        {
            m_lastGraphUndoCursor = m_graphUndoCursor.index;
        }

        public void RestoreCurrentGraphState()
        {
            SerializedState refGraph = null;
            if (!m_undoStack.TryGetValue(m_graphUndoCursor.index, out refGraph))
            {
                throw new Exception(string.Format("Unable to retrieve current state at : {0} (max {1})", m_graphUndoCursor.index, m_undoStack.Last().Key));
            }
            m_Graph.Restore(refGraph.serializedGraph);
            if (refGraph.slotDeltas != null)
            {
                foreach (var kv in refGraph.slotDeltas)
                {
                    kv.Key.value = kv.Value;
                }
            }
        }

        public void AddSlotDelta(VFXSlot slot)
        {
            SerializedState state = null;
            if (m_undoStack.TryGetValue(m_lastGraphUndoCursor, out state))
            {
                if (state.slotDeltas == null)
                    state.slotDeltas = new Dictionary<VFXSlot, object>();
                state.slotDeltas[slot] = slot.value;
            }
        }

        class SerializedState
        {
            public object serializedGraph;
            public Dictionary<VFXSlot, object> slotDeltas;
        }

        [NonSerialized]
        private SortedDictionary<int, SerializedState> m_undoStack;

        [NonSerialized]
        private VFXGraphUndoCursor m_graphUndoCursor;
        [NonSerialized]
        private int m_lastGraphUndoCursor;
    }

    partial class VFXViewController : Controller<VisualEffectResource>
    {
        [NonSerialized]
        private bool m_reentrant;
        [NonSerialized]
        private VFXGraphUndoStack m_graphUndoStack;

        private void InitializeUndoStack()
        {
            m_graphUndoStack = new VFXGraphUndoStack(graph);
        }

        private void ReleaseUndoStack()
        {
            m_graphUndoStack = null;
        }

        public void IncremenentGraphUndoRedoState(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (cause == VFXModel.InvalidationCause.kParamChanged && model is VFXSlot)
            {
                m_graphUndoStack.AddSlotDelta(model as VFXSlot);
                return;
            }

            if (cause != VFXModel.InvalidationCause.kStructureChanged &&
                cause != VFXModel.InvalidationCause.kConnectionChanged &&
                cause != VFXModel.InvalidationCause.kParamChanged &&
                cause != VFXModel.InvalidationCause.kSettingChanged &&
                cause != VFXModel.InvalidationCause.kUIChanged)
                return;

            if (m_reentrant)
            {
                m_reentrant = false;
                throw new InvalidOperationException("Reentrant undo/redo, this is not supposed to happen!");
            }

            if (m_graphUndoStack != null)
            {
                if (m_graphUndoStack == null)
                {
                    Debug.LogError("Unexpected IncrementGraphState (not initialize)");
                    return;
                }

                m_graphUndoStack.IncrementGraphState();
            }
        }

        public bool m_InLiveModification;

        public void BeginLiveModification()
        {
            if (m_InLiveModification == true)
                throw new InvalidOperationException("BeginLiveModification is not reentrant");
            m_InLiveModification = true;
        }

        public void EndLiveModification()
        {
            if (m_InLiveModification == false)
                throw new InvalidOperationException("EndLiveModification is not reentrant");
            m_InLiveModification = false;
            if (m_graphUndoStack.IsDirtyState())
            {
                m_graphUndoStack.FlushAndPushGraphState(graph);
                m_graphUndoStack.CleanDirtyState();
            }
        }

        private void WillFlushUndoRecord()
        {
            if (m_graphUndoStack == null)
            {
                return;
            }

            if (!m_InLiveModification)
            {
                if (m_graphUndoStack.IsDirtyState())
                {
                    m_graphUndoStack.FlushAndPushGraphState(graph);
                    m_graphUndoStack.CleanDirtyState();
                }
            }
        }

        private void SynchronizeUndoRedoState()
        {
            if (m_graphUndoStack == null)
            {
                return;
            }

            if (m_graphUndoStack.IsDirtyState())
            {
                try
                {
                    m_graphUndoStack.RestoreCurrentGraphState();
                    m_reentrant = true;
                    ExpressionGraphDirty = true;
                    model.GetOrCreateGraph().UpdateSubAssets();
                    NotifyUpdate();
                    m_reentrant = false;
                    m_graphUndoStack.CleanDirtyState();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    Undo.ClearAll();
                    m_graphUndoStack = new VFXGraphUndoStack(graph);
                }
            }
            else
            {
                //The graph didn't change by this undo, only, potentially the slot values.
                // Any undo could be a slot value undo: update input slot expressions and expression graph values
                ExpressionGraphDirty = true;
                ExpressionGraphDirtyParamOnly = true;

                if (model != null && graph != null)
                {
                    foreach (var element in AllSlotContainerControllers)
                    {
                        foreach (var slot in (element.model as IVFXSlotContainer).inputSlots)
                        {
                            slot.UpdateDefaultExpressionValue();
                        }
                    }

                    foreach (var parameter in m_ParameterControllers.Keys)
                    {
                        parameter.UpdateDefaultExpressionValue();
                    }
                    graph.SetExpressionValueDirty();
                }
            }
        }
    }
}
