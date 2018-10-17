using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Experimental.Rendering.UI;

namespace UnityEngine.Experimental.Rendering
{
    using UnityObject = UnityEngine.Object;

    public sealed partial class DebugManager
    {
        static readonly DebugManager s_Instance = new DebugManager();
        public static DebugManager instance { get { return s_Instance; } }

        // Explicit static constructor to tell the C# compiler not to mark type as beforefieldinit
        static DebugManager() {}

        ReadOnlyCollection<DebugUI.Panel> m_ReadOnlyPanels;
        readonly List<DebugUI.Panel> m_Panels = new List<DebugUI.Panel>();

        public ReadOnlyCollection<DebugUI.Panel> panels
        {
            get
            {
                if (m_ReadOnlyPanels == null)
                    m_ReadOnlyPanels = m_Panels.AsReadOnly();

                return m_ReadOnlyPanels;
            }
        }

        public event Action<bool> onDisplayRuntimeUIChanged = delegate {};
        public event Action onSetDirty = delegate {};

        public bool refreshEditorRequested;

        GameObject m_Root;
        DebugUIHandlerCanvas m_RootUICanvas;

        GameObject m_PersistentRoot;
        DebugUIHandlerPersistentCanvas m_RootUIPersistentCanvas;

        public bool displayRuntimeUI
        {
            get
            {
                var uiManager = UnityObject.FindObjectOfType<DebugUIHandlerCanvas>();

                // Might be needed to update the reference after domain reload
                if (uiManager != null)
                {
                    m_Root = uiManager.gameObject;
                }

                return m_Root != null && m_Root.activeInHierarchy;
            }
            set
            {
                if (value && m_Root == null)
                {
                    var uiManager = UnityObject.FindObjectOfType<DebugUIHandlerCanvas>();

                    if (uiManager != null)
                    {
                        m_Root = uiManager.gameObject;
                        return;
                    }

                    m_Root = UnityObject.Instantiate(Resources.Load<Transform>("DebugUI Canvas")).gameObject;
                    m_Root.name = "[Debug Canvas]";
                    m_Root.transform.localPosition = Vector3.zero;
                    m_RootUICanvas = m_Root.GetComponent<DebugUIHandlerCanvas>();
                }

                if (m_Root != null)
                    m_Root.SetActive(value);

                onDisplayRuntimeUIChanged(value);
            }
        }

        public bool displayPersistentRuntimeUI
        {
            get { return m_RootUIPersistentCanvas != null && m_PersistentRoot.activeInHierarchy; }
            set
            {
                CheckPersistentCanvas();
                m_PersistentRoot.SetActive(value);
            }
        }

        DebugManager()
        {
            RegisterInputs();
            RegisterActions();
        }

        public void RefreshEditor()
        {
            refreshEditorRequested = true;
        }

        public void Reset()
        {
            if (m_Panels != null)
                m_Panels.Clear();
        }

        public int GetState()
        {
            int hash = 17;

            foreach (var panel in m_Panels)
                hash = hash * 23 + panel.GetHashCode();

            return hash;
        }

        internal void ChangeSelection(DebugUIHandlerWidget widget, bool fromNext)
        {
            m_RootUICanvas.ChangeSelection(widget, fromNext);
        }

        void CheckPersistentCanvas()
        {
            if (m_RootUIPersistentCanvas == null)
            {
                var uiManager = UnityObject.FindObjectOfType<DebugUIHandlerPersistentCanvas>();

                if (uiManager == null)
                {
                    m_PersistentRoot = UnityObject.Instantiate(Resources.Load<Transform>("DebugUI Persistent Canvas")).gameObject;
                    m_PersistentRoot.name = "[Debug Canvas - Persistent]";
                    m_PersistentRoot.transform.localPosition = Vector3.zero;
                }
                else
                {
                    m_PersistentRoot = uiManager.gameObject;
                }

                m_RootUIPersistentCanvas = m_PersistentRoot.GetComponent<DebugUIHandlerPersistentCanvas>();
            }
        }

        public void TogglePersistent(DebugUI.Widget widget)
        {
            if (widget == null)
                return;

            var valueWidget = widget as DebugUI.Value;
            if (valueWidget == null)
            {
                Debug.Log("Only DebugUI.Value items can be made persistent.");
                return;
            }

            CheckPersistentCanvas();
            m_RootUIPersistentCanvas.Toggle(valueWidget);
        }

        void OnPanelDirty(DebugUI.Panel panel)
        {
            onSetDirty();
        }

        // TODO: Optimally we should use a query path here instead of a display name
        public DebugUI.Panel GetPanel(string displayName, bool createIfNull = false)
        {
            foreach (var panel in m_Panels)
            {
                if (panel.displayName == displayName)
                    return panel;
            }

            DebugUI.Panel p = null;

            if (createIfNull)
            {
                p = new DebugUI.Panel { displayName = displayName };
                p.onSetDirty += OnPanelDirty;
                m_Panels.Add(p);
                m_ReadOnlyPanels = m_Panels.AsReadOnly();
            }

            return p;
        }

        // TODO: Use a query path here as well instead of a display name
        public void RemovePanel(string displayName)
        {
            DebugUI.Panel panel = null;

            foreach (var p in m_Panels)
            {
                if (p.displayName == displayName)
                {
                    p.onSetDirty -= OnPanelDirty;
                    panel = p;
                    break;
                }
            }

            RemovePanel(panel);
        }

        public void RemovePanel(DebugUI.Panel panel)
        {
            if (panel == null)
                return;

            m_Panels.Remove(panel);
            m_ReadOnlyPanels = m_Panels.AsReadOnly();
        }

        public DebugUI.Widget GetItem(string queryPath)
        {
            foreach (var panel in m_Panels)
            {
                var w = GetItem(queryPath, panel);
                if (w != null)
                    return w;
            }

            return null;
        }

        DebugUI.Widget GetItem(string queryPath, DebugUI.IContainer container)
        {
            foreach (var child in container.children)
            {
                if (child.queryPath == queryPath)
                    return child;

                var containerChild = child as DebugUI.IContainer;
                if (containerChild != null)
                {
                    var w = GetItem(queryPath, containerChild);
                    if (w != null)
                        return w;
                }
            }

            return null;
        }
    }
}
