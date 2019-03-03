using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
using System;

namespace UnityEditor.VFX.UI
{
    abstract class VFXUIController<T> : Controller<VFXUI> where T : VFXUI.UIInfo
    {
        protected int m_Index;

        protected VFXUI m_UI;

        public void Remove()
        {
            m_Index = -1;
        }

        abstract protected T[] infos {get; }

        protected VFXViewController m_ViewController;

        public int index
        {
            get { return m_Index; }
            set { m_Index = value; }
        }
        void OnModelChanged()
        {
            ModelChanged(m_UI);
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            if (m_Index == -1) return;

            NotifyChange(AnyThing);
        }

        public override void OnDisable()
        {
            m_ViewController.UnRegisterNotification(m_UI, OnModelChanged);
            base.OnDisable();
        }

        public VFXUIController(VFXViewController viewController, VFXUI ui, int index) : base(ui)
        {
            m_UI = ui;
            viewController.RegisterNotification(m_UI, OnModelChanged);
            m_Index = index;
            m_ViewController = viewController;
        }

        protected void ValidateRect(ref Rect r)
        {
            if (float.IsInfinity(r.x) || float.IsNaN(r.x))
            {
                r.x = 0;
            }
            if (float.IsInfinity(r.y) || float.IsNaN(r.y))
            {
                r.y = 0;
            }
            if (float.IsInfinity(r.width) || float.IsNaN(r.width))
            {
                r.width = 100;
            }
            if (float.IsInfinity(r.height) || float.IsNaN(r.height))
            {
                r.height = 100;
            }

            r.x = Mathf.Round(r.x);
            r.y = Mathf.Round(r.y);
            r.width = Mathf.Round(r.width);
            r.height = Mathf.Round(r.height);
        }

        public Rect position
        {
            get
            {
                if (m_Index < 0)
                {
                    return Rect.zero;
                }
                Rect result = infos[m_Index].position;
                ValidateRect(ref result);
                return result;
            }
            set
            {
                if (m_Index < 0) return;

                ValidateRect(ref value);

                infos[m_Index].position = value;
                Modified();
            }
        }
        public string title
        {
            get
            {
                if (m_Index < 0)
                {
                    return "";
                }
                return infos[m_Index].title;
            }
            set
            {
                if (title != value && m_Index >= 0)
                {
                    infos[m_Index].title = value;
                    Modified();
                }
            }
        }

        protected void Modified()
        {
            m_UI.Modified();
            m_ViewController.IncremenentGraphUndoRedoState(null, VFXModel.InvalidationCause.kUIChanged);
        }

        public override void ApplyChanges()
        {
            if (m_Index == -1) return;

            ModelChanged(model);
        }
    }

    class VFXGroupNodeController : VFXUIController<VFXUI.GroupInfo>
    {
        public VFXGroupNodeController(VFXViewController viewController, VFXUI ui, int index) : base(viewController, ui, index)
        {
        }

        public IEnumerable<Controller> nodes
        {
            get
            {
                if (m_Index == -1) return Enumerable.Empty<Controller>();

                if (m_UI.groupInfos[m_Index].contents != null)
                    return m_UI.groupInfos[m_Index].contents.Where(t => t.isStickyNote || t.model != null).Select(t => t.isStickyNote ? (Controller)m_ViewController.GetStickyNoteController(t.id) : (Controller)m_ViewController.GetRootNodeController(t.model, t.id)).Where(t => t != null);
                return Enumerable.Empty<Controller>();
            }
        }


        override protected VFXUI.GroupInfo[] infos {get {return m_UI.groupInfos; }}


        void AddNodeID(VFXNodeID nodeID)
        {
            if (m_Index < 0)
                return;

            if (m_UI.groupInfos[m_Index].contents != null)
                m_UI.groupInfos[m_Index].contents = m_UI.groupInfos[m_Index].contents.Concat(Enumerable.Repeat(nodeID, 1)).Distinct().ToArray();
            else
                m_UI.groupInfos[m_Index].contents = new VFXNodeID[] { nodeID };
        }

        public void AddNodes(IEnumerable<VFXNodeController> controllers)
        {
            if (m_Index < 0)
                return;

            foreach (var controller in controllers)
            {
                AddNodeID(new VFXNodeID(controller.model, controller.id));
            }

            Modified();
        }

        public void AddNode(VFXNodeController controller)
        {
            if (m_Index < 0)
                return;

            AddNodeID(new VFXNodeID(controller.model, controller.id));

            Modified();
        }

        public void AddStickyNotes(IEnumerable<VFXStickyNoteController> notes)
        {
            if (m_Index < 0)
                return;

            foreach (var note in notes)
            {
                AddNodeID(new VFXNodeID(note.index));
            }

            Modified();
        }

        public void AddStickyNote(VFXStickyNoteController note)
        {
            if (m_Index < 0)
                return;

            AddNodeID(new VFXNodeID(note.index));

            Modified();
        }

        public void RemoveNodes(IEnumerable<VFXNodeController> nodeControllers)
        {
            if (m_Index < 0)
                return;

            if (m_UI.groupInfos[m_Index].contents == null)
                return;

            bool oneFound = false;

            foreach (var nodeController in nodeControllers)
            {
                int id = nodeController.id;
                var model = nodeController.model;
                if (!m_UI.groupInfos[m_Index].contents.Any(t => t.model == model && t.id == id))
                    continue;
                m_UI.groupInfos[m_Index].contents = m_UI.groupInfos[m_Index].contents.Where(t => t.model != model || t.id != id).ToArray();
                oneFound = true;
            }
            if(oneFound)
                Modified();
        }

        public void RemoveNode(VFXNodeController nodeController)
        {
            if (m_Index < 0)
                return;

            if (m_UI.groupInfos[m_Index].contents == null)
                return;

            int id = nodeController.id;
            var model = nodeController.model;
            if (!m_UI.groupInfos[m_Index].contents.Any(t => t.model == model && t.id == id))
                return;
            m_UI.groupInfos[m_Index].contents = m_UI.groupInfos[m_Index].contents.Where(t => t.model != model || t.id != id).ToArray();

            Modified();
        }

        public void RemoveStickyNotes(IEnumerable<VFXStickyNoteController> stickyNodeControllers)
        {
            if (m_Index < 0)
                return;
            if (m_UI.groupInfos[m_Index].contents == null)
                return;

            foreach (var stickyNoteController in stickyNodeControllers)
            {
                if (!m_UI.groupInfos[m_Index].contents.Any(t => t.isStickyNote && t.id == stickyNoteController.index))
                    return;

                m_UI.groupInfos[m_Index].contents = m_UI.groupInfos[m_Index].contents.Where(t => !t.isStickyNote || t.id != stickyNoteController.index).ToArray();
            }

            Modified();
        }

        public bool ContainsNode(VFXNodeController controller)
        {
            if (m_Index == -1) return false;
            if (m_UI.groupInfos[m_Index].contents != null)
            {
                return m_UI.groupInfos[m_Index].contents.Contains(new VFXNodeID(controller.model, controller.id));
            }
            return false;
        }
    }
}
