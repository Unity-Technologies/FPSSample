using System;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Entities.Editor
{
    
    public delegate void EntitySelectionCallback(Entity selection);
    public delegate World WorldSelectionGetter();
    public delegate ScriptBehaviourManager SystemSelectionGetter();
    
    public class EntityListView : TreeView, IDisposable {

        public EntityListQuery SelectedEntityQuery
        {
            get => selectedEntityQuery;
            set
            {
                if (value == null || selectedEntityQuery != value)
                {
                    selectedEntityQuery = value;
                    Reload();
                }
            }
        }
        private EntityListQuery selectedEntityQuery;

        private readonly EntitySelectionCallback setEntitySelection;
        private readonly WorldSelectionGetter getWorldSelection;
        private readonly SystemSelectionGetter getSystemSelection;
        
        private readonly EntityArrayListAdapter rows;
        private NativeArray<ArchetypeChunk> chunkArray;

        public EntityListView(TreeViewState state, EntityListQuery entityQuery, EntitySelectionCallback entitySelectionCallback, WorldSelectionGetter getWorldSelection, SystemSelectionGetter getSystemSelection) : base(state)
        {
            this.setEntitySelection = entitySelectionCallback;
            this.getWorldSelection = getWorldSelection;
            this.getSystemSelection = getSystemSelection;
            selectedEntityQuery = entityQuery;
            rows = new EntityArrayListAdapter();
            getNewSelectionOverride = (item, selection, shift) => new List<int>() {item.id};
            Reload();
        }

        internal bool ShowingSomething => getWorldSelection() != null &&
                                       (selectedEntityQuery != null || !(getSystemSelection() is ComponentSystemBase));

        public void UpdateIfNecessary()
        {
            if (ShowingSomething)
                Reload();
        }

        public int EntityCount => rows.Count;

        protected override TreeViewItem BuildRoot()
        {
            var root  = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };
            
            return root;
        }

        private readonly EntityArchetypeQuery allQuery = new EntityArchetypeQuery()
        {
            All = new ComponentType[0],
            Any = new ComponentType[0],
            None = new ComponentType[0]
        };
        
        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if (!ShowingSomething)
                return new List<TreeViewItem>();
            
            var entityManager = getWorldSelection().GetExistingManager<EntityManager>();
            
            if (chunkArray.IsCreated)
                chunkArray.Dispose();
            var query = SelectedEntityQuery?.Query ?? allQuery;
            
            entityManager.CompleteAllJobs();
            chunkArray = entityManager.CreateArchetypeChunkArray(query, Allocator.Persistent);

            rows.SetSource(chunkArray, entityManager);
            return rows;
        }

        protected override IList<int> GetAncestors(int id)
        {
            return id == -1 ? new List<int>() : new List<int>() {-1};
        }

        protected override IList<int> GetDescendantsThatHaveChildren(int id)
        {
            return new List<int>();
        }

        public override void OnGUI(Rect rect)
        {
            if (getWorldSelection()?.GetExistingManager<EntityManager>()?.IsCreated == true)
                base.OnGUI(rect);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count > 0)
            {
                if (rows.GetById(selectedIds[0], out var selectedEntity))
                    setEntitySelection(selectedEntity);
            }
            else
            {
                setEntitySelection(Entity.Null);
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        public void SelectNothing()
        {
            SetSelection(new List<int>());
        }

        public void SetEntitySelection(Entity entitySelection)
        {
            if (entitySelection != Entity.Null && getWorldSelection().GetExistingManager<EntityManager>().Exists(entitySelection))
                SetSelection(new List<int>{entitySelection.Index});
        }

        public void TouchSelection()
        {
            SetSelection(GetSelection(), TreeViewSelectionOptions.FireSelectionChanged);
        }

        public void FrameSelection()
        {
            var selection = GetSelection();
            if (selection.Count > 0)
            {
                FrameItem(selection[0]);
            }
        }

        public void Dispose()
        {
            if (chunkArray.IsCreated)
                chunkArray.Dispose();
        }
    }
}
