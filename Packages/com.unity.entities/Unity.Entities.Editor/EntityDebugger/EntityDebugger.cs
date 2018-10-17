using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;

namespace Unity.Entities.Editor
{
    public class EntityDebugger : EditorWindow
    {
        private const float kSystemListWidth = 350f;

        [MenuItem("Window/Analysis/Entity Debugger", false)]
        private static void OpenWindow()
        {
            GetWindow<EntityDebugger>("Entity Debugger");
        }

        private static GUIStyle Box
        {
            get
            {
                if (box == null)
                {
                    box = new GUIStyle(GUI.skin.box)
                    {
                        margin = new RectOffset(),
                        padding = new RectOffset(1, 0, 1, 0),
                        overflow = new RectOffset(0, 1, 0, 1)
                    };
                }

                return box;
            }
        }

        private static GUIStyle box;

        public ScriptBehaviourManager SystemSelection { get; private set; }

        public World SystemSelectionWorld
        {
            get => systemSelectionWorld?.IsCreated == true ? systemSelectionWorld : null;
            private set => systemSelectionWorld = value;
        }

        public void SetSystemSelection(ScriptBehaviourManager manager, World world, bool updateList, bool propagate)
        {
            if (manager != null && world == null)
                throw new ArgumentNullException("System cannot have null world");
            SystemSelection = manager;
            SystemSelectionWorld = world;
            if (updateList)
                systemListView.SetSystemSelection(manager, world);
            CreateComponentGroupListView();
            if (propagate)
            {
                if (SystemSelection is ComponentSystemBase)
                    componentGroupListView.TouchSelection();
                else
                    ApplyAllEntitiesFilter();
            }
        }

        public EntityListQuery EntityListQuerySelection { get; private set; }

        public void SetEntityListSelection(EntityListQuery newSelection, bool updateList, bool propagate)
        {
            EntityListQuerySelection = newSelection;
            if (updateList)
                componentGroupListView.SetEntityListSelection(newSelection);
            entityListView.SelectedEntityQuery = newSelection;
            if (propagate)
                entityListView.TouchSelection();
        }

        public Entity EntitySelection => selectionProxy.Entity;

        internal void SetEntitySelection(Entity newSelection, bool updateList)
        {
            if (updateList)
                entityListView.SetEntitySelection(newSelection);
            if (WorldSelection != null && newSelection != Entity.Null)
            {
                selectionProxy.SetEntity(WorldSelection, newSelection);
                Selection.activeObject = selectionProxy;
            }
            else if (Selection.activeObject == selectionProxy)
            {
                Selection.activeObject = null;
            }
        }

        internal static void SetAllSelections(World world, ComponentSystemBase system, EntityListQuery entityQuery,
            Entity entity)
        {
            if (Instance == null)
                return;
            Instance.SetWorldSelection(world, false);
            Instance.SetSystemSelection(system, world, true, false);
            Instance.SetEntityListSelection(entityQuery, true, false);
            Instance.SetEntitySelection(entity, true);
            Instance.entityListView.FrameSelection();
        }

        private static EntityDebugger Instance { get; set; }

        private EntitySelectionProxy selectionProxy;
        
        [SerializeField] private List<TreeViewState> componentGroupListStates = new List<TreeViewState>();
        [SerializeField] private List<string> componentGroupListStateNames = new List<string>();
        private ComponentGroupListView componentGroupListView;
        
        [SerializeField] private List<TreeViewState> systemListStates = new List<TreeViewState>();
        [SerializeField] private List<string> systemListStateNames = new List<string>();
        private SystemListView systemListView;

        [SerializeField] private TreeViewState entityListState = new TreeViewState();
        private EntityListView entityListView;

        internal WorldPopup m_WorldPopup;
        
        private ComponentTypeFilterUI filterUI;
        
        public World WorldSelection
        {
            get
            {
                if (worldSelection != null && worldSelection.IsCreated)
                    return worldSelection;
                return null;
            }
        }
        
        
        [SerializeField] private string lastEditModeWorldSelection = WorldPopup.kNoWorldName;
        [SerializeField] private string lastPlayModeWorldSelection = WorldPopup.kNoWorldName;
        [SerializeField] private bool showingPlayerLoop;
        

        public void SetWorldSelection(World selection, bool propagate)
        {
            if (worldSelection != selection)
            {
                worldSelection = selection;
                showingPlayerLoop = worldSelection == null;
                if (worldSelection != null)
                {
                    if (EditorApplication.isPlaying)
                        lastPlayModeWorldSelection = worldSelection.Name;
                    else
                        lastEditModeWorldSelection = worldSelection.Name;
                }
                    
                CreateSystemListView();
                if (propagate)
                    systemListView.TouchSelection();
            }
        }

        private void CreateEntityListView()
        {
            entityListView?.Dispose();
            entityListView = new EntityListView(entityListState, EntityListQuerySelection, x => SetEntitySelection(x, false), () => SystemSelectionWorld ?? WorldSelection, () => SystemSelection);
        }

        private void CreateSystemListView()
        {
            systemListView = SystemListView.CreateList(systemListStates, systemListStateNames, (system, world) => SetSystemSelection(system, world, false, true), () => WorldSelection);
            systemListView.multiColumnHeader.ResizeToFit();
        }

        private void CreateComponentGroupListView()
        {
            componentGroupListView = ComponentGroupListView.CreateList(SystemSelection as ComponentSystemBase, componentGroupListStates, componentGroupListStateNames, x => SetEntityListSelection(x, false, true), () => SystemSelectionWorld);
        }

        private void CreateWorldPopup()
        {
            m_WorldPopup = new WorldPopup(() => WorldSelection, x => SetWorldSelection(x, true));
        }

        private World worldSelection;

        private void OnEnable()
        {
            Instance = this;
            selectionProxy = ScriptableObject.CreateInstance<EntitySelectionProxy>();
            selectionProxy.hideFlags = HideFlags.HideAndDontSave;
            filterUI = new ComponentTypeFilterUI(SetAllEntitiesFilter, () => WorldSelection);
            CreateWorldPopup();
            CreateSystemListView();
            CreateComponentGroupListView();
            CreateEntityListView();
            systemListView.TouchSelection();
            EditorApplication.playModeStateChanged += OnPlayModeStateChange;
        }

        private void OnDestroy()
        {
            entityListView?.Dispose();
        }

        private void OnDisable()
        {
            entityListView?.Dispose();
            if (Instance == this)
                Instance = null;
            if (selectionProxy)
                DestroyImmediate(selectionProxy);
            
            EditorApplication.playModeStateChanged -= OnPlayModeStateChange;
        }

        private void OnPlayModeStateChange(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
                SetAllEntitiesFilter(null);
            if (change == PlayModeStateChange.ExitingPlayMode && Selection.activeObject == selectionProxy)
                Selection.activeObject = null;
        }
        
        private float lastUpdate;

        private void Update()
        {
            systemListView.UpdateTimings();
            
            if (Time.realtimeSinceStartup > lastUpdate + 0.5f) 
            { 
                Repaint(); 
            }
        }

        private void ShowWorldPopup()
        {
            m_WorldPopup.OnGUI(showingPlayerLoop, EditorApplication.isPlaying ? lastPlayModeWorldSelection : lastEditModeWorldSelection);
        }

        private void SystemList()
        {
            var rect = GUIHelpers.GetExpandingRect();
            if (World.AllWorlds.Count != 0)
            {
                systemListView.OnGUI(rect);
            }
            else
            {
                GUIHelpers.ShowCenteredNotification(rect, "No systems (Try pushing Play)");
            }
        }

        private void SystemHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Systems", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            AlignHeader(ShowWorldPopup);
            GUILayout.EndHorizontal();
        }

        private void EntityHeader()
        {
            if (WorldSelection == null && SystemSelectionWorld == null)
                return;
            GUILayout.BeginHorizontal();
            if (SystemSelection == null)
            {
                GUILayout.Label("All Entities", EditorStyles.boldLabel);
            }
            else
            {
                var type = SystemSelection.GetType();
                AlignHeader(() => GUILayout.Label(type.Namespace, EditorStyles.label));
                GUILayout.Label(type.Name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                var system = SystemSelection as ComponentSystemBase;
                if (system != null)
                {
                    var running = system.Enabled && system.ShouldRunSystem();
                    AlignHeader(() => GUILayout.Label($"running: {(running ? "yes" : "no")}"));
                }
            }
            GUILayout.EndHorizontal();
        }

        private void ComponentGroupList()
        {
            if (SystemSelection is ComponentSystemBase)
            {
                GUILayout.BeginVertical(Box, GUILayout.Height(componentGroupListView.Height + Box.padding.bottom + Box.padding.top));

                componentGroupListView.OnGUI(GUIHelpers.GetExpandingRect());
                GUILayout.EndVertical();
            }
            else if (WorldSelection != null)
            {
                GUILayout.BeginHorizontal();
                filterUI.OnGUI();
                GUILayout.FlexibleSpace();
                GUILayout.Label(entityListView.EntityCount.ToString());
                GUILayout.EndHorizontal();
            }
        }

        private EntityListQuery filterQuery;
        private World systemSelectionWorld;

        public void SetAllEntitiesFilter(EntityListQuery entityQuery)
        {
            filterQuery = entityQuery;
            if (WorldSelection == null || SystemSelection is ComponentSystemBase)
                return;
            ApplyAllEntitiesFilter();
        }
        
        private void ApplyAllEntitiesFilter()
        {
            SetEntityListSelection(filterQuery, false, true);
        }

        void EntityList()
        {
            GUILayout.BeginVertical(Box);
            entityListView.OnGUI(GUIHelpers.GetExpandingRect());
            GUILayout.EndVertical();
        }

        private void AlignHeader(System.Action header)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(6f);
            header();
            GUILayout.EndVertical();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject != selectionProxy)
            {
                entityListView.SelectNothing();
            }
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                systemListView.UpdateIfNecessary();
                componentGroupListView.UpdateIfNecessary();
                filterUI.GetTypes();
                entityListView.UpdateIfNecessary();
            }
            
            if (Selection.activeObject == selectionProxy)
            {
                if (!selectionProxy.Exists)
                {
                    Selection.activeObject = null;
                    entityListView.SelectNothing();
                }
            }

            GUILayout.BeginHorizontal();
            
            GUILayout.BeginVertical(GUILayout.Width(kSystemListWidth)); // begin System side
            SystemHeader();
            
            GUILayout.BeginVertical(Box);
            SystemList();
            GUILayout.EndVertical();
            
            GUILayout.EndVertical(); // end System side
            
            GUILayout.BeginVertical(GUILayout.Width(position.width - kSystemListWidth)); // begin Entity side

            EntityHeader();
            ComponentGroupList();
            EntityList();
            
            GUILayout.EndVertical(); // end Entity side
            
            GUILayout.EndHorizontal();

            lastUpdate = Time.realtimeSinceStartup;
        }
    }
}