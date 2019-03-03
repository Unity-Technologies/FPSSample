using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    #pragma warning disable 414

    [Serializable]
    sealed class WidgetStateDictionary : SerializedDictionary<string, DebugState> {}

    sealed class DebugWindowSettings : ScriptableObject
    {
        // Keep these settings in a separate scriptable object so we can handle undo/redo on them
        // without the rest of the debug window interfering
        public int currentStateHash;
        public int selectedPanel;

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }
    }

    public sealed class DebugWindow : EditorWindow
    {
        static Styles s_Styles;
        static GUIStyle s_SplitterLeft;

        static float splitterPos = 150f;
        const float minSideBarWidth = 100;
        const float minContentWidth = 100;
        bool dragging = false;

        [SerializeField]
        WidgetStateDictionary m_WidgetStates;

        [SerializeField]
        DebugWindowSettings m_Settings;

        [SerializeField]
        int m_DebugTreeState;

        bool m_IsDirty;

        Vector2 m_PanelScroll;
        Vector2 m_ContentScroll;

        static bool s_TypeMapDirty;
        static Dictionary<Type, Type> s_WidgetStateMap; // DebugUI.Widget type -> DebugState type
        static Dictionary<Type, DebugUIDrawer> s_WidgetDrawerMap; // DebugUI.Widget type -> DebugUIDrawer
        
        [DidReloadScripts]
        static void OnEditorReload()
        {
            s_TypeMapDirty = true;
        }

        static void RebuildTypeMaps()
        {
            var assemblyTypes = CoreUtils.GetAllAssemblyTypes();

            // Map states to widget (a single state can map to several widget types if the value to
            // serialize is the same)
            var attrType = typeof(DebugStateAttribute);
            var stateTypes = assemblyTypes
                .Where(
                    t => t.IsSubclassOf(typeof(DebugState))
                    && t.IsDefined(attrType, false)
                    && !t.IsAbstract
                    );

            s_WidgetStateMap = new Dictionary<Type, Type>();

            foreach (var stateType in stateTypes)
            {
                var attr = (DebugStateAttribute)stateType.GetCustomAttributes(attrType, false)[0];

                foreach (var t in attr.types)
                    s_WidgetStateMap.Add(t, stateType);
            }

            // Drawers
            attrType = typeof(DebugUIDrawerAttribute);
            var types = assemblyTypes
                .Where(
                    t => t.IsSubclassOf(typeof(DebugUIDrawer))
                    && t.IsDefined(attrType, false)
                    && !t.IsAbstract
                    );

            s_WidgetDrawerMap = new Dictionary<Type, DebugUIDrawer>();

            foreach (var t in types)
            {
                var attr = (DebugUIDrawerAttribute)t.GetCustomAttributes(attrType, false)[0];
                var inst = (DebugUIDrawer)Activator.CreateInstance(t);
                s_WidgetDrawerMap.Add(attr.type, inst);
            }

            // Done
            s_TypeMapDirty = false;
        }

        [MenuItem("Window/Analysis/Render Pipeline Debug", priority = 112)] // 112 is hardcoded number given by the UxTeam to fit correctly in the Windows menu
        static void Init()
        {
            var window = GetWindow<DebugWindow>();
            window.titleContent = new GUIContent("Debug");
        }

        void OnEnable()
        {
            DebugManager.instance.refreshEditorRequested = false;

            hideFlags = HideFlags.HideAndDontSave;
            autoRepaintOnSceneChange = true;

            if (m_Settings == null)
                m_Settings = CreateInstance<DebugWindowSettings>();

            // States are ScriptableObjects (necessary for Undo/Redo) but are not saved on disk so when the editor is closed then reopened, any existing debug window will have its states set to null
            // Since we don't care about persistance in this case, we just re-init everything.
            if (m_WidgetStates == null || !AreWidgetStatesValid())
                m_WidgetStates = new WidgetStateDictionary();

            if (s_WidgetStateMap == null || s_WidgetDrawerMap == null || s_TypeMapDirty)
                RebuildTypeMaps();

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            DebugManager.instance.onSetDirty += MarkDirty;

            // First init
            m_DebugTreeState = DebugManager.instance.GetState();
            UpdateWidgetStates();

            EditorApplication.update -= Repaint;
            var panels = DebugManager.instance.panels;
            var selectedPanelIndex = m_Settings.selectedPanel;
            if (selectedPanelIndex >= 0
                && selectedPanelIndex < panels.Count
                && panels[selectedPanelIndex].editorForceUpdate)
                EditorApplication.update += Repaint;
        }

        // Note: this won't get called if the window is opened when the editor itself is closed
        void OnDestroy()
        {
            DebugManager.instance.onSetDirty -= MarkDirty;
            Undo.ClearUndo(m_Settings);

            DestroyWidgetStates();
        }

        public void DestroyWidgetStates()
        {
            if (m_WidgetStates != null)
            {
                // Clear all the states from memory
                foreach (var state in m_WidgetStates)
                {
                    var s = state.Value;
                    Undo.ClearUndo(s); // Don't leave dangling states in the global undo/redo stack
                    DestroyImmediate(s);
                }

                m_WidgetStates.Clear();
            }
        }

        bool AreWidgetStatesValid()
        {
            foreach (var state in m_WidgetStates)
            {
                if (state.Value == null)
                {
                    return false;
                }
            }
            return true;
        }

        void MarkDirty()
        {
            m_IsDirty = true;
        }

        // We use item states to keep a cached value of each serializable debug items in order to
        // handle domain reloads, play mode entering/exiting and undo/redo
        // Note: no removal of orphan states
        void UpdateWidgetStates()
        {
            foreach (var panel in DebugManager.instance.panels)
                UpdateWidgetStates(panel);
        }

        void UpdateWidgetStates(DebugUI.IContainer container)
        {
            // Skip runtime only containers, we won't draw them so no need to serialize them either
            var actualWidget = container as DebugUI.Widget;
            if (actualWidget != null && actualWidget.isRuntimeOnly)
                return;

            // Recursively update widget states
            foreach (var widget in container.children)
            {
                // Skip non-serializable widgets but still traverse them in case one of their
                // children needs serialization support
                var valueField = widget as DebugUI.IValueField;
                if (valueField != null)
                {
                    // Skip runtime & readonly only items
                    if (widget.isRuntimeOnly)
                        return;

                    var widgetType = widget.GetType();
                    string guid = widget.queryPath;
                    Type stateType;
                    s_WidgetStateMap.TryGetValue(widgetType, out stateType);

                    // Create missing states & recreate the ones that are null
                    if (stateType != null)
                    {
                        if (!m_WidgetStates.ContainsKey(guid) || m_WidgetStates[guid] == null)
                        {
                            var inst = (DebugState)CreateInstance(stateType);
                            inst.queryPath = guid;
                            inst.SetValue(valueField.GetValue(), valueField);
                            m_WidgetStates[guid] = inst;
                        }
                    }
                }

                // Recurse if the widget is a container
                var containerField = widget as DebugUI.IContainer;
                if (containerField != null)
                    UpdateWidgetStates(containerField);
            }
        }

        public void ApplyStates(bool forceApplyAll = false)
        {
            if (!forceApplyAll && DebugState.m_CurrentDirtyState != null)
            {
                ApplyState(DebugState.m_CurrentDirtyState.queryPath, DebugState.m_CurrentDirtyState);
                DebugState.m_CurrentDirtyState = null;
                return;
            }

            foreach (var state in m_WidgetStates)
                ApplyState(state.Key, state.Value);

            DebugState.m_CurrentDirtyState = null;
        }

        void ApplyState(string queryPath, DebugState state)
        {
            var widget = DebugManager.instance.GetItem(queryPath) as DebugUI.IValueField;

            if (widget == null)
                return;

            widget.SetValue(state.GetValue());
        }

        void OnUndoRedoPerformed()
        {
            int stateHash = ComputeStateHash();

            // Something has been undone / redone, re-apply states to the debug tree
            if (stateHash != m_Settings.currentStateHash)
            {
                ApplyStates(true);
                m_Settings.currentStateHash = stateHash;
            }

            Repaint();
        }

        int ComputeStateHash()
        {
            unchecked
            {
                int hash = 13;

                foreach (var state in m_WidgetStates)
                    hash = hash * 23 + state.Value.GetHashCode();

                return hash;
            }
        }

        void Update()
        {
            // If the render pipeline asset has been reloaded we force-refresh widget states in case
            // some debug values need to be refresh/recreated as well (e.g. frame settings on HD)
            if (DebugManager.instance.refreshEditorRequested)
            {
                DestroyWidgetStates();
                DebugManager.instance.refreshEditorRequested = false;
            }

            int treeState = DebugManager.instance.GetState();

            if (m_DebugTreeState != treeState || m_IsDirty)
            {
                UpdateWidgetStates();
                ApplyStates();
                m_DebugTreeState = treeState;
                m_IsDirty = false;
            }
        }

        void OnGUI()
        {
            if (s_Styles == null)
            {
                s_Styles = new Styles();
                s_SplitterLeft = new GUIStyle();
            }

            var panels = DebugManager.instance.panels;
            int itemCount = panels.Count(x => !x.isRuntimeOnly && x.children.Count(w => !w.isRuntimeOnly) > 0);

            if (itemCount == 0)
            {
                EditorGUILayout.HelpBox("No debug item found.", MessageType.Info);
                return;
            }

            // Background color
            var wrect = position;
            wrect.x = 0;
            wrect.y = 0;
            var oldColor = GUI.color;
            GUI.color = s_Styles.skinBackgroundColor;
            GUI.DrawTexture(wrect, EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;

            using (new EditorGUILayout.HorizontalScope())
            {
                // Side bar
                using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_PanelScroll, s_Styles.sectionScrollView, GUILayout.Width(splitterPos)))
                {
                    GUILayout.Space(40f);

                    if (m_Settings.selectedPanel >= panels.Count)
                        m_Settings.selectedPanel = 0;

                    // Validate container id
                    while (panels[m_Settings.selectedPanel].isRuntimeOnly || panels[m_Settings.selectedPanel].children.Count(x => !x.isRuntimeOnly) == 0)
                    {
                        m_Settings.selectedPanel++;

                        if (m_Settings.selectedPanel >= panels.Count)
                            m_Settings.selectedPanel = 0;
                    }

                    // Root children are containers
                    for (int i = 0; i < panels.Count; i++)
                    {
                        var panel = panels[i];

                        if (panel.isRuntimeOnly)
                            continue;

                        if (panel.children.Count(x => !x.isRuntimeOnly) == 0)
                            continue;

                        var elementRect = GUILayoutUtility.GetRect(CoreEditorUtils.GetContent(panel.displayName), s_Styles.sectionElement, GUILayout.ExpandWidth(true));

                        if (m_Settings.selectedPanel == i && Event.current.type == EventType.Repaint)
                            s_Styles.selected.Draw(elementRect, false, false, false, false);

                        EditorGUI.BeginChangeCheck();
                        GUI.Toggle(elementRect, m_Settings.selectedPanel == i, panel.displayName, s_Styles.sectionElement);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RegisterCompleteObjectUndo(m_Settings, "Debug Panel Selection");
                            var previousPanel = m_Settings.selectedPanel >= 0 && m_Settings.selectedPanel < panels.Count
                                ? panels[m_Settings.selectedPanel]
                                : null;
                            if (previousPanel != null && previousPanel.editorForceUpdate && !panel.editorForceUpdate)
                                EditorApplication.update -= Repaint;
                            else if ((previousPanel == null || !previousPanel.editorForceUpdate) && panel.editorForceUpdate)
                                EditorApplication.update += Repaint;
                            m_Settings.selectedPanel = i;
                        }
                    }

                    m_PanelScroll = scrollScope.scrollPosition;
                }
                
                Rect splitterRect = new Rect(splitterPos - 3, 0, 6, Screen.height);
                GUI.Box(splitterRect, "", s_SplitterLeft);

                GUILayout.Space(10f);

                // Main section - traverse current container
                using (var changedScope = new EditorGUI.ChangeCheckScope())
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        var selectedPanel = panels[m_Settings.selectedPanel];

                        GUILayout.Label(selectedPanel.displayName, s_Styles.sectionHeader);
                        GUILayout.Space(10f);

                        using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_ContentScroll))
                        {
                            TraverseContainerGUI(selectedPanel);
                            m_ContentScroll = scrollScope.scrollPosition;
                        }
                    }

                    if (changedScope.changed)
                        m_Settings.currentStateHash = ComputeStateHash();
                }

                // Splitter events
                if (Event.current != null)
                {
                    switch (Event.current.rawType)
                    {
                        case EventType.MouseDown:
                            if (splitterRect.Contains(Event.current.mousePosition))
                            {
                                dragging = true;
                            }
                            break;
                        case EventType.MouseDrag:
                            if (dragging)
                            {
                                splitterPos += Event.current.delta.x;
                                splitterPos = Mathf.Clamp(splitterPos, minSideBarWidth, Screen.width - minContentWidth);
                                Repaint();
                            }
                            break;
                        case EventType.MouseUp:
                            if (dragging)
                            {
                                dragging = false;
                            }
                            break;
                    }
                }
                EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            }
        }

        void OnWidgetGUI(DebugUI.Widget widget)
        {
            if (widget.isRuntimeOnly)
                return;

            DebugState state; // State will be null for stateless widget
            m_WidgetStates.TryGetValue(widget.queryPath, out state);

            DebugUIDrawer drawer;

            if (!s_WidgetDrawerMap.TryGetValue(widget.GetType(), out drawer))
            {
                EditorGUILayout.LabelField("Drawer not found (" + widget.GetType() + ").");
            }
            else
            {
                drawer.Begin(widget, state);

                if (drawer.OnGUI(widget, state))
                {
                    var container = widget as DebugUI.IContainer;

                    if (container != null)
                        TraverseContainerGUI(container);
                }

                drawer.End(widget, state);
            }
        }

        void TraverseContainerGUI(DebugUI.IContainer container)
        {
            // /!\ SHAAAAAAAME ALERT /!\
            // A container can change at runtime because of the way IMGUI works and how we handle
            // onValueChanged on widget so we have to take this into account while iterating
            try
            {
                foreach (var widget in container.children)
                    OnWidgetGUI(widget);
            }
            catch (InvalidOperationException)
            {
                Repaint();
            }
        }

        public class Styles
        {
            public static float s_DefaultLabelWidth = 0.5f;

            public readonly GUIStyle sectionScrollView = "PreferencesSectionBox";
            public readonly GUIStyle sectionElement = new GUIStyle("PreferencesSection");
            public readonly GUIStyle selected = "OL SelectedRow";
            public readonly GUIStyle sectionHeader = new GUIStyle(EditorStyles.largeLabel);
            public readonly Color skinBackgroundColor;

            public Styles()
            {
                sectionScrollView = new GUIStyle(sectionScrollView);
                sectionScrollView.overflow.bottom += 1;

                sectionElement.alignment = TextAnchor.MiddleLeft;

                sectionHeader.fontStyle = FontStyle.Bold;
                sectionHeader.fontSize = 18;
                sectionHeader.margin.top = 10;
                sectionHeader.margin.left += 1;
                sectionHeader.normal.textColor = !EditorGUIUtility.isProSkin
                    ? new Color(0.4f, 0.4f, 0.4f, 1.0f)
                    : new Color(0.7f, 0.7f, 0.7f, 1.0f);

                if (EditorGUIUtility.isProSkin)
                {
                    sectionHeader.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);
                    skinBackgroundColor = Color.gray * new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
                else
                {
                    sectionHeader.normal.textColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);
                    skinBackgroundColor = Color.gray * new Color(1f, 1f, 1f, 0.32f);
                }
            }
        }
    }

    #pragma warning restore 414
}
