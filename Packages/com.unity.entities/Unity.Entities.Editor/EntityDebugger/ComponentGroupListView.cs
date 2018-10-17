using System;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEngine;

namespace Unity.Entities.Editor
{
    
    public delegate void SetEntityListSelection(EntityListQuery query);
    
    public class ComponentGroupListView : TreeView {
        private static Dictionary<ComponentSystemBase, List<EntityArchetypeQuery>> queriesBySystem = new Dictionary<ComponentSystemBase, List<EntityArchetypeQuery>>();
        private static readonly Dictionary<ComponentGroup, EntityArchetypeQuery> queriesByGroup = new Dictionary<ComponentGroup, EntityArchetypeQuery>();
        
        private static EntityArchetypeQuery GetQueryForGroup(ComponentGroup group)
        {
            if (!queriesByGroup.ContainsKey(group))
            {
                var query = new EntityArchetypeQuery()
                {
                    All = group.Types.Where(x => x.AccessModeType != ComponentType.AccessMode.Subtractive).ToArray(),
                    Any = new ComponentType[0],
                    None = group.Types.Where(x => x.AccessModeType == ComponentType.AccessMode.Subtractive).ToArray()
                };
                queriesByGroup.Add(group, query);
            }

            return queriesByGroup[group];
        }
        
        private readonly Dictionary<int, ComponentGroup> componentGroupsById = new Dictionary<int, ComponentGroup>();
        private readonly Dictionary<int, EntityArchetypeQuery> queriesById = new Dictionary<int, EntityArchetypeQuery>();
        private readonly Dictionary<int, List<GUIStyle>> stylesById = new Dictionary<int, List<GUIStyle>>();
        private readonly Dictionary<int, List<GUIContent>> namesById = new Dictionary<int, List<GUIContent>>();
        private readonly Dictionary<int, List<Rect>> rectsById = new Dictionary<int, List<Rect>>();
        private readonly Dictionary<int, float> heightsById = new Dictionary<int, float>();

        public ComponentSystemBase SelectedSystem
        {
            get { return selectedSystem; }
            set
            {
                if (selectedSystem != value)
                {
                    selectedSystem = value;
                    Reload();
                }
            }
        }
        private ComponentSystemBase selectedSystem;

        private readonly WorldSelectionGetter getWorldSelection;
        private readonly SetEntityListSelection entityListSelectionCallback;

        private static TreeViewState GetStateForSystem(ComponentSystemBase system, List<TreeViewState> states, List<string> stateNames)
        {
            if (system == null)
                return new TreeViewState();
            
            var currentSystemName = system.GetType().FullName;

            var stateForCurrentSystem = states.Where((t, i) => stateNames[i] == currentSystemName).FirstOrDefault();
            if (stateForCurrentSystem != null)
                return stateForCurrentSystem;
            
            stateForCurrentSystem = new TreeViewState();
            if (system.ComponentGroups != null && system.ComponentGroups.Length > 0)
                stateForCurrentSystem.expandedIDs = new List<int> {1};
            states.Add(stateForCurrentSystem);
            stateNames.Add(currentSystemName);
            return stateForCurrentSystem;
        }

        public static ComponentGroupListView CreateList(ComponentSystemBase system, List<TreeViewState> states, List<string> stateNames,
            SetEntityListSelection entityQuerySelectionCallback, WorldSelectionGetter worldSelectionGetter)
        {
            var state = GetStateForSystem(system, states, stateNames);
            return new ComponentGroupListView(state, system, entityQuerySelectionCallback, worldSelectionGetter);
        }

        public ComponentGroupListView(TreeViewState state, ComponentSystemBase system, SetEntityListSelection entityListSelectionCallback, WorldSelectionGetter worldSelectionGetter) : base(state)
        {
            this.getWorldSelection = worldSelectionGetter;
            this.entityListSelectionCallback = entityListSelectionCallback;
            selectedSystem = system;
            rowHeight += 1;
            showAlternatingRowBackgrounds = true;
            Reload();
        }

        public float Height => Mathf.Max(queriesById.Count + componentGroupsById.Count, 1)*rowHeight;

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            return heightsById.ContainsKey(item.id) ? heightsById[item.id] + 2 : rowHeight;
        }

        private static List<EntityArchetypeQuery> GetQueriesForSystem(ComponentSystemBase system)
        {
            if (queriesBySystem.TryGetValue(system, out var queries))
                return queries;
            
            queries = new List<EntityArchetypeQuery>();

            var currentType = system.GetType();

            while (currentType != null)
            {
                foreach (var field in currentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (field.FieldType == typeof(EntityArchetypeQuery))
                        queries.Add(field.GetValue(system) as EntityArchetypeQuery);
                }

                currentType = currentType.BaseType;
            }

            return queries;
        }

        protected override TreeViewItem BuildRoot()
        {
            componentGroupsById.Clear();
            queriesById.Clear();
            heightsById.Clear();
            var currentId = 0;
            var root  = new TreeViewItem { id = currentId++, depth = -1, displayName = "Root" };
            if (getWorldSelection() == null)
            {
                root.AddChild(new TreeViewItem { id = currentId, displayName = "No world selected"});
            }
            else if (SelectedSystem == null)
            {
                root.AddChild(new TreeViewItem { id = currentId, displayName = "Null System"});
            }
            else
            {
                var queries = GetQueriesForSystem(SelectedSystem);

                foreach (var query in queries)
                {
                    queriesById.Add(currentId, query);
                    var queryItem = new TreeViewItem { id = currentId++ };
                    root.AddChild(queryItem);
                }
                if (SelectedSystem.ComponentGroups != null)
                {
                    foreach (var group in SelectedSystem.ComponentGroups)
                    {
                        componentGroupsById.Add(currentId, group);

                        var groupItem = new TreeViewItem { id = currentId++ };
                        root.AddChild(groupItem);
                    }
                }
                if (queriesById.Count == 0 && componentGroupsById.Count == 0)
                {
                    root.AddChild(new TreeViewItem { id = currentId, displayName = "No Component Groups or Queries in Manager"});
                }
                else
                {
                    SetupDepthsFromParentsAndChildren(root);
                }
            }
            return root;
        }

        private float width;

        private void CalculateDrawingParts(float newWidth)
        {
            width = newWidth;
            stylesById.Clear();
            namesById.Clear();
            rectsById.Clear();
            heightsById.Clear();
            foreach (var idGroupPair in componentGroupsById)
            {
                ComponentGroupGUI.CalculateDrawingParts(new List<ComponentType>(idGroupPair.Value.Types.Skip(1)), false, width, out var height, out var styles, out var names, out var rects);
                stylesById.Add(idGroupPair.Key, styles);
                namesById.Add(idGroupPair.Key, names);
                rectsById.Add(idGroupPair.Key, rects);
                heightsById.Add(idGroupPair.Key, height);
            }
            foreach (var idQueryPair in queriesById)
            {
                var types = new List<ComponentType>();
                types.AddRange(idQueryPair.Value.All);
                types.AddRange(idQueryPair.Value.Any);
                types.AddRange(idQueryPair.Value.None.Select(x => ComponentType.Subtractive(x.GetManagedType())));
                
                ComponentGroupGUI.CalculateDrawingParts(types, true, width, out var height, out var styles, out var names, out var rects);
                stylesById.Add(idQueryPair.Key, styles);
                namesById.Add(idQueryPair.Key, names);
                rectsById.Add(idQueryPair.Key, rects);
                heightsById.Add(idQueryPair.Key, height);
            }
            RefreshCustomRowHeights();
        }

        public override void OnGUI(Rect rect)
        {

            if (getWorldSelection()?.GetExistingManager<EntityManager>()?.IsCreated == true)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    CalculateDrawingParts(rect.width - 60f);
                }
                base.OnGUI(rect);
            }
        }

        protected override void BeforeRowsGUI()
        {
            base.BeforeRowsGUI();
        }

        protected void DrawCount(RowGUIArgs args)
        {
            if (componentGroupsById.TryGetValue(args.item.id, out var componentGroup))
            {
                var countString = componentGroup.CalculateLength().ToString();
                DefaultGUI.LabelRightAligned(args.rowRect, countString, args.selected, args.focused);
            }
            else if (queriesById.TryGetValue(args.item.id, out var query))
            {
                var entityManager = getWorldSelection().GetExistingManager<EntityManager>();
                var chunkArray = entityManager.CreateArchetypeChunkArray(query, Allocator.TempJob);
                var count = chunkArray.Sum(x => x.Count);
                chunkArray.Dispose();
                DefaultGUI.LabelRightAligned(args.rowRect, count.ToString(), args.selected, args.focused);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);
            if (Event.current.type != EventType.Repaint || !heightsById.ContainsKey(args.item.id))
                return;

            var position = args.rowRect.position;
            position.x = GetContentIndent(args.item);
            position.y += 1;
            
            ComponentGroupGUI.DrawComponentList(
                new Rect(position.x, position.y, heightsById[args.item.id], width),
                stylesById[args.item.id],
                namesById[args.item.id],
                rectsById[args.item.id]);

            DrawCount(args);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count > 0)
            {
                if (componentGroupsById.TryGetValue(selectedIds[0], out var componentGroup))
                    entityListSelectionCallback(new EntityListQuery(componentGroup));
                else if (queriesById.TryGetValue(selectedIds[0], out var query))
                    entityListSelectionCallback(new EntityListQuery(query));
            }
            else
            {
                entityListSelectionCallback(null);
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        public void SetEntityListSelection(EntityListQuery newListQuery)
        {
            if (newListQuery == null)
            {
                SetSelection(new List<int>());
                return;
            }
            if (newListQuery.Group != null)
            {
                foreach (var pair in componentGroupsById)
                {
                    if (pair.Value == newListQuery.Group)
                    {
                        SetSelection(new List<int> {pair.Key});
                        return;
                    }
                }
            }
            else
            {
                foreach (var pair in queriesById)
                {
                    if (pair.Value == newListQuery.Query)
                    {
                        SetSelection(new List<int> {pair.Key});
                        return;
                    }
                }
            }
            SetSelection(new List<int>());
        }

        public void SetComponentGroupSelection(ComponentGroup group)
        {
            SetSelection(new List<int>());
        }

        public void TouchSelection()
        {
            SetSelection(GetSelection(), TreeViewSelectionOptions.FireSelectionChanged);
        }

        public void UpdateIfNecessary()
        {
            var expectedGroupCount = SelectedSystem?.ComponentGroups?.Length ?? 0; 
            if (expectedGroupCount != componentGroupsById.Count)
                Reload();
        }
    }
}
