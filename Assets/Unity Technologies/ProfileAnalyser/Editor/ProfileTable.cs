using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace ProfileAnalyser
{
    class ProfileTreeViewItem: TreeViewItem
    {
        public MarkerData data { get; set; }

        public ProfileTreeViewItem(int id, int depth, string displayName, MarkerData data) : base(id, depth, displayName)
        {
            this.data = data;
        }
    }

    class ProfileTable : TreeView
    {
        ProfileAnalysis m_model;
        ProfileAnalyserWindow m_profileAnalyserWindow;
        float m_maxMedian;

        const float kRowHeights = 20f;
        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);

        // All columns
        public enum MyColumns
        {
            Name,
            Depth,
            Median,
            MedianBar,
            Average,
            Min,
            Max,
            Range,
            Count,
            CountAverage,
            FirstFrame,
            AtMedian,
        }

        public enum SortOption
        {
            Name,
            Depth,
            Median,
            Average,
            Min,
            Max,
            Range,
            Count,
            FirstFrame,
            AtMedian,
        }

        // Sort options per column
        SortOption[] m_SortOptions =
        {
            SortOption.Name,
            SortOption.Depth,
            SortOption.Median,
            SortOption.Median,
            SortOption.Average,
            SortOption.Min,
            SortOption.Max,
            SortOption.Range,
            SortOption.Count,
            SortOption.Count,
            SortOption.FirstFrame,
            SortOption.AtMedian,
        };


        public ProfileTable(TreeViewState state, MultiColumnHeader multicolumnHeader, ProfileAnalysis model, ProfileAnalyserWindow profileAnalyserWindow) : base(state, multicolumnHeader)
        {
            m_model = model;
            m_profileAnalyserWindow = profileAnalyserWindow;

            Assert.AreEqual(m_SortOptions.Length, Enum.GetValues(typeof(MyColumns)).Length, "Ensure number of sort options are in sync with number of MyColumns enum values");

            // Custom setup
            rowHeight = kRowHeights;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
            // extraSpaceBeforeIconAndLabel = 0;
            multicolumnHeader.sortingChanged += OnSortingChanged;
            multicolumnHeader.visibleColumnsChanged += OnVisibleColumnsChanged;

            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            int idForhiddenRoot = -1;
            int depthForHiddenRoot = -1;
            ProfileTreeViewItem root = new ProfileTreeViewItem(idForhiddenRoot, depthForHiddenRoot, "root", null);

            m_maxMedian = 0.0f;
            int index = 0;
            foreach (var marker in m_model.GetMarkers())
            {
                if (m_profileAnalyserWindow.CheckMarkerValid(marker))
                {
                    var item = new ProfileTreeViewItem(index, 0, marker.name, marker);
                    root.AddChild(item);
                    float ms = item.data.msMedian;
                    if (ms > m_maxMedian)
                        m_maxMedian = ms;
                }
                // Maintain index to map to main markers
                index += 1;
            }

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();

            if (rootItem!=null && rootItem.children!=null)
            {
                foreach (ProfileTreeViewItem node in rootItem.children)
                {
                    if (m_profileAnalyserWindow.CheckMarkerValid(node.data))
                        m_Rows.Add(node);
                }
            }

            SortIfNeeded(m_Rows);

            return m_Rows;
        }


        void OnSortingChanged(MultiColumnHeader _multiColumnHeader)
        {
            SortIfNeeded(GetRows());
        }

        protected virtual void OnVisibleColumnsChanged(MultiColumnHeader multiColumnHeader)
        {
            m_profileAnalyserWindow.SetMode(Mode.Custom);
        }

        void SortIfNeeded(IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
            {
                return;
            }

            if (multiColumnHeader.sortedColumnIndex == -1)
            {
                return; // No column to sort for (just use the order the data are in)
            }

            // Sort the roots of the existing tree items
            SortByMultipleColumns();

            // Update the data with the sorted content
            rows.Clear();
            foreach (ProfileTreeViewItem node in rootItem.children)
            {
                rows.Add(node);
            }

            Repaint();
        }

        void SortByMultipleColumns()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0)
            {
                return;
            }

            var myTypes = rootItem.children.Cast<ProfileTreeViewItem>();
            var orderedQuery = InitialOrder(myTypes, sortedColumns);
            for (int i = 1; i < sortedColumns.Length; i++)
            {
                SortOption sortOption = m_SortOptions[sortedColumns[i]];
                bool ascending = multiColumnHeader.IsSortedAscending(sortedColumns[i]);

                switch (sortOption)
                {
                    case SortOption.Name:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.name, ascending);
                        break;
                    case SortOption.Depth:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.minDepth, ascending);
                        break;
                    case SortOption.Average:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.msFrameAverage, ascending);
                        break;
                    case SortOption.Median:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.msMedian, ascending);
                        break;
                    case SortOption.Min:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.msMin, ascending);
                        break;
                    case SortOption.Max:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.msMax, ascending);
                        break;
                    case SortOption.Range:
                        orderedQuery = orderedQuery.ThenBy(l => (l.data.msMax - l.data.msMin), ascending);
                        break;
                    case SortOption.Count:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.count, ascending);
                        break;
                    case SortOption.FirstFrame:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.firstFrameIndex, ascending);
                        break;
                    case SortOption.AtMedian:
                        orderedQuery = orderedQuery.ThenBy(l => l.data.msAtMedian, ascending);
                        break;
                }
            }

            rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<ProfileTreeViewItem> InitialOrder(IEnumerable<ProfileTreeViewItem> myTypes, int[] history)
        {
            SortOption sortOption = m_SortOptions[history[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(history[0]);
            switch (sortOption)
            {
                case SortOption.Name:
                    return myTypes.Order(l => l.data.name, ascending);
                case SortOption.Depth:
                    return myTypes.Order(l => l.data.minDepth, ascending);
                case SortOption.Average:
                    return myTypes.Order(l => l.data.msFrameAverage, ascending);
                case SortOption.Median:
                    return myTypes.Order(l => l.data.msMedian, ascending);
                case SortOption.Min:
                    return myTypes.Order(l => l.data.msMin, ascending);
                case SortOption.Max:
                    return myTypes.Order(l => l.data.msMax, ascending);
                case SortOption.Range:
                    return myTypes.Order(l => (l.data.msMax - l.data.msMin), ascending);
                case SortOption.Count:
                    return myTypes.Order(l => l.data.count, ascending);
                case SortOption.FirstFrame:
                    return myTypes.Order(l => l.data.firstFrameIndex, ascending);
                case SortOption.AtMedian:
                    return myTypes.Order(l => l.data.msAtMedian, ascending);
                default:
                    Assert.IsTrue(false, "Unhandled enum");
                    break;
            }

            // default
            return myTypes.Order(l => l.data.name, ascending);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (ProfileTreeViewItem)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (MyColumns)args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, ProfileTreeViewItem item, MyColumns column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case MyColumns.Name:
                    {
                        args.rowRect = cellRect;
                        base.RowGUI(args);
                    }
                    break;

                case MyColumns.Average:
                    EditorGUI.LabelField(cellRect, string.Format("{0:f2}", item.data.msFrameAverage));
                    break;
                case MyColumns.Depth:
                    if (m_profileAnalyserWindow.GetDepthFilter() >= 0 || item.data.minDepth == item.data.maxDepth)
                        EditorGUI.LabelField(cellRect, string.Format("{0}", item.data.minDepth));
                    else 
                        EditorGUI.LabelField(cellRect, string.Format("{0}-{1}", item.data.minDepth, item.data.maxDepth));
                    break;
                case MyColumns.Median:
                    EditorGUI.LabelField(cellRect, string.Format("{0:f2}", item.data.msMedian));
                    break;
                case MyColumns.MedianBar:
                    {
                        float ms = item.data.msMedian;
                        if (ms > 0.0f)
                        {
                            if (m_profileAnalyserWindow.DrawStart(cellRect))
                            {
                                float w = cellRect.width * ms / m_maxMedian;
                                m_profileAnalyserWindow.DrawBar(0, 1, w, cellRect.height - 1, m_profileAnalyserWindow.m_colorBar);
                                m_profileAnalyserWindow.DrawEnd();
                            }
                        }
                        GUI.Label(cellRect, new GUIContent("", string.Format("{0:f2}", item.data.msMedian)));
                    }
                    break;
                case MyColumns.Min:
                    EditorGUI.LabelField(cellRect, string.Format("{0:f2}", item.data.msMin));
                    break;
                case MyColumns.Max:
                    EditorGUI.LabelField(cellRect, string.Format("{0:f2}", item.data.msMax));
                    break;
                case MyColumns.Range:
                    EditorGUI.LabelField(cellRect, string.Format("{0:f2}", item.data.msMax - item.data.msMin));
                    break;
                case MyColumns.Count:
                    EditorGUI.LabelField(cellRect, string.Format("{0}", item.data.count));
                    break;
                case MyColumns.CountAverage:
                    EditorGUI.LabelField(cellRect, string.Format("{0}", item.data.count / m_model.GetFrameSummary().count));
                    break;
                case MyColumns.FirstFrame:
                    if (!m_profileAnalyserWindow.IsProfilerWindowOpen())
                        GUI.enabled = false;
                    if (GUI.Button(cellRect, new GUIContent(item.data.firstFrameIndex.ToString())))
                    {
                        m_profileAnalyserWindow.SelectMarker(item.id);
                        m_profileAnalyserWindow.JumpToFrame(item.data.firstFrameIndex);
                    }

                    GUI.enabled = true;
                    break;
                case MyColumns.AtMedian:
                    EditorGUI.LabelField(cellRect, string.Format("{0:f2}", item.data.msAtMedian));
                    break;
            }
        }


        // Misc
        //--------

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columnList = new List<MultiColumnHeaderState.Column>();
            columnList.Add(new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Name"),
                headerTextAlignment = TextAlignment.Left,
                sortedAscending = true,
                sortingArrowAlignment = TextAlignment.Left,
                width = 300,
                minWidth = 100,
                autoResize = false,
                allowToggleVisibility = false
            });
            string[] names = {"Depth","Median","Median","Average","Min","Max","Range","Count","Frm Av.", "1st", "At med"};
            foreach (var name in names)
            {
                var column = new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(name),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 50,
                    minWidth = 30,
                    autoResize = true
                };
                columnList.Add(column);
            };
            var columns = columnList.ToArray();

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(MyColumns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            SetMode(Mode.All, state);
            return state;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            if (selectedIds.Count>0)
                m_profileAnalyserWindow.SelectMarker(selectedIds[0]);
        }

        private static void SetMode(Mode mode, MultiColumnHeaderState state)
        {
            switch (mode)
            {
                case Mode.All:
                    state.visibleColumns = new int[] {
                        (int)MyColumns.Name,
                        (int)MyColumns.Depth,
                        (int)MyColumns.Median,
                        (int)MyColumns.MedianBar,
                        (int)MyColumns.Average,
                        (int)MyColumns.Min,
                        (int)MyColumns.Max,
                        (int)MyColumns.Range,
                        //(int)MyColumns.FirstFrame,
                        (int)MyColumns.Count,
                        (int)MyColumns.CountAverage,
                        (int)MyColumns.AtMedian
                    };
                    break;
                case Mode.Time:
                    state.visibleColumns = new int[] {
                        (int)MyColumns.Name,
                        (int)MyColumns.Depth,
                        (int)MyColumns.Median,
                        (int)MyColumns.MedianBar,
                        //(int)MyColumns.Average,
                        (int)MyColumns.Min,
                        (int)MyColumns.Max,
                        (int)MyColumns.Range,
                        //(int)MyColumns.FirstFrame,
                        (int)MyColumns.AtMedian
                    };
                    break;
                case Mode.Count:
                    state.visibleColumns = new int[] {
                        (int)MyColumns.Name,
                        (int)MyColumns.Depth,
                        (int)MyColumns.Count,
                        (int)MyColumns.CountAverage,
                        //(int)MyColumns.FirstFrame,
                    };
                    break;
            }
        }

        public void SetMode(Mode mode)
        {
            SetMode(mode, multiColumnHeader.state);
            multiColumnHeader.ResizeToFit();
        }
    }

    static class MyExtensionMethods
    {
        public static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.OrderBy(selector);
            }
            else
            {
                return source.OrderByDescending(selector);
            }
        }

        public static IOrderedEnumerable<T> ThenBy<T, TKey>(this IOrderedEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.ThenBy(selector);
            }
            else
            {
                return source.ThenByDescending(selector);
            }
        }
    }
}
