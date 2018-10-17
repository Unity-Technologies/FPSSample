using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace ProfileAnalyser
{
    class ComparisonTreeViewItem : TreeViewItem
    {
        public MarkerPairing data { get; set; }

        public ComparisonTreeViewItem(int id, int depth, string displayName, MarkerPairing data) : base(id, depth, displayName)
        {
            this.data = data;
        }
    }

    class ComparisonTable : TreeView
    {
        ProfileAnalysis m_left;
        ProfileAnalysis m_right;
        List<MarkerPairing> m_pairings;
        ProfileAnalyserWindow m_profileAnalyserWindow;
        float m_minDiff;
        float m_maxDiff;

        const float kRowHeights = 20f;
        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);

        // All columns
        public enum MyColumns
        {
            Name,
            LeftMedian,
            Left,
            Right,
            RightMedian,
            Diff,
            AbsDiff,
            LeftCount,
            RightCount,
            CountDiff,
        }

        public enum SortOption
        {
            Name,
            LeftMedian,
            RightMedian,
            Diff,
            AbsDiff,
            LeftCount,
            RightCount,
            CountDiff,
        }

        // Sort options per column
        SortOption[] m_SortOptions =
        {
            SortOption.Name,
            SortOption.LeftMedian,
            SortOption.Diff,
            SortOption.Diff,
            SortOption.RightMedian,
            SortOption.Diff,
            SortOption.AbsDiff,
            SortOption.LeftCount,
            SortOption.RightCount,
            SortOption.CountDiff,
        };

        public ComparisonTable(TreeViewState state, MultiColumnHeader multicolumnHeader, ProfileAnalysis left, ProfileAnalysis right, List<MarkerPairing> pairings, ProfileAnalyserWindow profileAnalyserWindow) : base(state, multicolumnHeader)
        {
            m_left = left;
            m_right = right;
            m_pairings = pairings;
            m_profileAnalyserWindow = profileAnalyserWindow;

            Assert.AreEqual(m_SortOptions.Length, Enum.GetValues(typeof(MyColumns)).Length, "Ensure number of sort options are in sync with number of MyColumns enum values");

            // Custom setup
            rowHeight = kRowHeights;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
            // extraSpaceBeforeIconAndLabel = 0;
            multicolumnHeader.sortingChanged += OnSortingChanged;
            multiColumnHeader.visibleColumnsChanged += OnVisibleColumnsChanged;

            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            int idForhiddenRoot = -1;
            int depthForHiddenRoot = -1;
            ProfileTreeViewItem root = new ProfileTreeViewItem(idForhiddenRoot, depthForHiddenRoot, "root", null);

            int depthFilter = m_profileAnalyserWindow.GetDepthFilter();

            int index = 0;
            m_minDiff = float.MaxValue;
            m_maxDiff = 0.0f;
            foreach (var pairing in m_pairings)
            {
                if (depthFilter >= 0)
                {
                    if (pairing.leftIndex>=0 && m_left.GetMarkers()[pairing.leftIndex].minDepth != depthFilter)
                    {
                        index += 1; // Keep index mapping to main pairing list
                        continue;
                    }
                    if (pairing.rightIndex>=0 && m_right.GetMarkers()[pairing.rightIndex].minDepth != depthFilter)
                    {
                        index += 1; // Keep index mapping to main pairing list
                        continue;
                    }
                }
                var item = new ComparisonTreeViewItem(index, 0, pairing.name, pairing);
                root.AddChild(item);
                float diff = Diff(item);
                if (diff < m_minDiff)
                    m_minDiff = diff;
                if (diff > m_maxDiff && diff<float.MaxValue)
                    m_maxDiff = diff;
                index += 1;
            }

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();

            int depthFilter = m_profileAnalyserWindow.GetDepthFilter();

            if (rootItem != null && rootItem.children != null)
            {
                foreach (ComparisonTreeViewItem node in rootItem.children)
                {
                    if (depthFilter >= 0)
                    {
                        if (node.data.leftIndex >= 0 && m_left.GetMarkers()[node.data.leftIndex].minDepth != depthFilter)
                            continue;
                        if (node.data.rightIndex >= 0 && m_right.GetMarkers()[node.data.rightIndex].minDepth != depthFilter)
                            continue;
                    }
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
            foreach (var node in rootItem.children)
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

            var myTypes = rootItem.children.Cast<ComparisonTreeViewItem>();
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
                    case SortOption.LeftMedian:
                        orderedQuery = orderedQuery.ThenBy(l => LeftMedian(l), ascending);
                        break;
                    case SortOption.RightMedian:
                        orderedQuery = orderedQuery.ThenBy(l => RightMedian(l), ascending);
                        break;
                    case SortOption.Diff:
                        orderedQuery = orderedQuery.ThenBy(l => Diff(l), ascending);
                        break;
                    case SortOption.AbsDiff:
                        orderedQuery = orderedQuery.ThenBy(l => AbsDiff(l), ascending);
                        break;
                    case SortOption.LeftCount:
                        orderedQuery = orderedQuery.ThenBy(l => LeftCount(l), ascending);
                        break;
                    case SortOption.RightCount:
                        orderedQuery = orderedQuery.ThenBy(l => RightCount(l), ascending);
                        break;
                    case SortOption.CountDiff:
                        orderedQuery = orderedQuery.ThenBy(l => CountDiff(l), ascending);
                        break;
                }
            }

            rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
        }

        float LeftMedian(ComparisonTreeViewItem item)
        {
            if (item.data.leftIndex < 0)
                return 0.0f;
            
            List<MarkerData> markers = m_left.GetMarkers();
            if (item.data.leftIndex >= markers.Count)
                return 0.0f;
                
            return markers[item.data.leftIndex].msMedian;
        }
        float RightMedian(ComparisonTreeViewItem item)
        {
            if (item.data.rightIndex < 0)
                return 0.0f;

            List<MarkerData> markers = m_right.GetMarkers();
            if (item.data.rightIndex >= markers.Count)
                return 0.0f;

            return markers[item.data.rightIndex].msMedian;
        }
        float Diff(ComparisonTreeViewItem item)
        {
            return RightMedian(item) - LeftMedian(item);
        }
        float AbsDiff(ComparisonTreeViewItem item)
        {
            return Math.Abs(Diff(item));
        }
        float LeftCount(ComparisonTreeViewItem item)
        {
            if (item.data.leftIndex < 0)
                return 0.0f;

            List<MarkerData> markers = m_left.GetMarkers();
            if (item.data.leftIndex >= markers.Count)
                return 0.0f;

            return markers[item.data.leftIndex].count;
        }
        float RightCount(ComparisonTreeViewItem item)
        {
            if (item.data.rightIndex < 0)
                return 0.0f;

            List<MarkerData> markers = m_right.GetMarkers();
            if (item.data.rightIndex >= markers.Count)
                return 0.0f;

            return markers[item.data.rightIndex].count;
        }
        float CountDiff(ComparisonTreeViewItem item)
        {
            return RightCount(item) - LeftCount(item);
        }

        IOrderedEnumerable<ComparisonTreeViewItem> InitialOrder(IEnumerable<ComparisonTreeViewItem> myTypes, int[] history)
        {
            SortOption sortOption = m_SortOptions[history[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(history[0]);
            switch (sortOption)
            {
                case SortOption.Name:
                    return myTypes.Order(l => l.data.name, ascending);
                case SortOption.LeftMedian:
                    return myTypes.Order(l => LeftMedian(l), ascending);
                case SortOption.RightMedian:
                    return myTypes.Order(l => RightMedian(l), ascending);
                case SortOption.Diff:
                    return myTypes.Order(l => Diff(l), ascending);
                case SortOption.AbsDiff:
                    return myTypes.Order(l => AbsDiff(l), ascending);
                case SortOption.LeftCount:
                    return myTypes.Order(l => LeftCount(l), ascending);
                case SortOption.RightCount:
                    return myTypes.Order(l => RightCount(l), ascending);
                case SortOption.CountDiff:
                    return myTypes.Order(l => CountDiff(l), ascending);
                default:
                    Assert.IsTrue(false, "Unhandled enum");
                    break;
            }

            // default
            return myTypes.Order(l => l.data.name, ascending);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (ComparisonTreeViewItem)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (MyColumns)args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, ComparisonTreeViewItem item, MyColumns column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);

            // Find largest of min/max and use that range and both the -ve and +ve extends for the bar graphs.
            float min = Math.Abs(m_minDiff);
            float max = Math.Abs(m_maxDiff);
            float range = Math.Max(min, max);
                
            switch (column)
            {
                case MyColumns.Name:
                    {
                        args.rowRect = cellRect;
                        base.RowGUI(args);
                    }
                    break;
                case MyColumns.LeftMedian:
                    if (item.data.leftIndex<0)
                        EditorGUI.LabelField(cellRect, "-");
                    else
                        EditorGUI.LabelField(cellRect, string.Format("{0:f2}", LeftMedian(item)));
                    break;
                case MyColumns.Left:
                    {
                        float diff = Diff(item);
                        if (diff < 0.0f)
                        {
                            if (m_profileAnalyserWindow.DrawStart(cellRect))
                            {
                                float w = cellRect.width * -diff / range;
                                m_profileAnalyserWindow.DrawBar(cellRect.width - w, 1, w, cellRect.height - 1, m_profileAnalyserWindow.m_colorLeft);
                                m_profileAnalyserWindow.DrawEnd();
                            }
                        }
                        GUI.Label(cellRect, new GUIContent("", string.Format("{0:f2}", Diff(item))));
                    }
                    break;
                case MyColumns.Diff:
                    EditorGUI.LabelField(cellRect, string.Format("{0:f2}", Diff(item)));
                    break;
                case MyColumns.Right:
                    {
                        float diff = Diff(item);
                        if (diff > 0.0f)
                        {
                            if (m_profileAnalyserWindow.DrawStart(cellRect))
                            {
                                float w = cellRect.width * diff / range;
                                m_profileAnalyserWindow.DrawBar(0, 1, w, cellRect.height - 1, m_profileAnalyserWindow.m_colorRight);
                                m_profileAnalyserWindow.DrawEnd();
                            }
                        }
                        GUI.Label(cellRect, new GUIContent("", string.Format("{0:f2}", Diff(item))));
                    }
                    break;
                case MyColumns.RightMedian:
                    if (item.data.rightIndex < 0)
                        EditorGUI.LabelField(cellRect, "-");
                    else
                        EditorGUI.LabelField(cellRect, string.Format("{0:f2}", RightMedian(item)));
                    break;
                case MyColumns.AbsDiff:
                    EditorGUI.LabelField(cellRect, string.Format("{0:f2}", AbsDiff(item)));
                    break;
                case MyColumns.LeftCount:
                    if (item.data.leftIndex < 0)
                        EditorGUI.LabelField(cellRect, "-");
                    else
                        EditorGUI.LabelField(cellRect, string.Format("{0}", LeftCount(item)));
                    break;
                case MyColumns.RightCount:
                    if (item.data.rightIndex < 0)
                        EditorGUI.LabelField(cellRect, "-");
                    else
                        EditorGUI.LabelField(cellRect, string.Format("{0}", RightCount(item)));
                    break;
                case MyColumns.CountDiff:
                    if (item.data.leftIndex < 0 && item.data.rightIndex < 0)
                        EditorGUI.LabelField(cellRect, "-");
                    else
                        EditorGUI.LabelField(cellRect, string.Format("{0}", CountDiff(item)));
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
            string[] names = { "Left", "<", ">", "Right", "Diff", "Abs Diff", "L Count", "R Count", "D Count" };
            foreach (var name in names)
            {
                columnList.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(name),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 50,
                    minWidth = 30,
                    autoResize = true,
                });
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

            if (selectedIds.Count > 0)
                m_profileAnalyserWindow.SelectPairing(selectedIds[0]);
        }

        private static void SetMode(Mode mode, MultiColumnHeaderState state)
        {
            switch (mode)
            {
                case Mode.All:
                    state.visibleColumns = new int[] {
                        (int)MyColumns.Name,
                        (int)MyColumns.LeftMedian,
                        (int)MyColumns.Left,
                        (int)MyColumns.Right,
                        (int)MyColumns.RightMedian,
                        (int)MyColumns.Diff,
                        (int)MyColumns.AbsDiff,
                        (int)MyColumns.LeftCount,
                        (int)MyColumns.RightCount,
                        (int)MyColumns.CountDiff,
                    };
                    break;
                case Mode.Time:
                    state.visibleColumns = new int[] {
                        (int)MyColumns.Name,
                        (int)MyColumns.LeftMedian,
                        (int)MyColumns.Left,
                        (int)MyColumns.Right,
                        (int)MyColumns.RightMedian,
                        //(int)MyColumns.Diff,
                        (int)MyColumns.AbsDiff,
                    };
                    break;
                case Mode.Count:
                    state.visibleColumns = new int[] {
                        (int)MyColumns.Name,
                        (int)MyColumns.LeftCount,
                        (int)MyColumns.RightCount,
                        (int)MyColumns.CountDiff,
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
}
