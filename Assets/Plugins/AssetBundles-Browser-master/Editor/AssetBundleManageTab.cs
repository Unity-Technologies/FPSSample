using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.Collections.Generic;


namespace AssetBundleBrowser
{
    [System.Serializable]
    internal class AssetBundleManageTab 
    {
        [SerializeField]
        TreeViewState m_BundleTreeState;
        [SerializeField]
        TreeViewState m_AssetListState;
        [SerializeField]
        MultiColumnHeaderState m_AssetListMCHState;
        [SerializeField]
        TreeViewState m_BundleDetailState;

        Rect m_Position;

        AssetBundleTree m_BundleTree;
        AssetListTree m_AssetList;
        MessageList m_MessageList;
        BundleDetailList m_DetailsList;
        bool m_ResizingHorizontalSplitter = false;
        bool m_ResizingVerticalSplitterRight = false;
        bool m_ResizingVerticalSplitterLeft = false;
        Rect m_HorizontalSplitterRect, m_VerticalSplitterRectRight, m_VerticalSplitterRectLeft;
        [SerializeField]
        float m_HorizontalSplitterPercent;
        [SerializeField]
        float m_VerticalSplitterPercentRight;
        [SerializeField]
        float m_VerticalSplitterPercentLeft;
        const float k_SplitterWidth = 3f;
        private static float s_UpdateDelay = 0f;

        SearchField m_searchField;

        EditorWindow m_Parent = null;

        internal AssetBundleManageTab()
        {
            m_HorizontalSplitterPercent = 0.4f;
            m_VerticalSplitterPercentRight = 0.7f;
            m_VerticalSplitterPercentLeft = 0.85f;
        }

        internal void OnEnable(Rect pos, EditorWindow parent)
        {
            m_Parent = parent;
            m_Position = pos;
            m_HorizontalSplitterRect = new Rect(
                (int)(m_Position.x + m_Position.width * m_HorizontalSplitterPercent),
                m_Position.y,
                k_SplitterWidth,
                m_Position.height);
            m_VerticalSplitterRectRight = new Rect(
                m_HorizontalSplitterRect.x,
                (int)(m_Position.y + m_HorizontalSplitterRect.height * m_VerticalSplitterPercentRight),
                (m_Position.width - m_HorizontalSplitterRect.width) - k_SplitterWidth,
                k_SplitterWidth);
            m_VerticalSplitterRectLeft = new Rect(
                m_Position.x,
                (int)(m_Position.y + m_HorizontalSplitterRect.height * m_VerticalSplitterPercentLeft),
                (m_HorizontalSplitterRect.width) - k_SplitterWidth,
                k_SplitterWidth);

            m_searchField = new SearchField();
        }



        internal void Update()
        {
            var t = Time.realtimeSinceStartup;
            if (t - s_UpdateDelay > 0.1f ||
                s_UpdateDelay > t) //something went strangely wrong if this second check is true.
            {
                s_UpdateDelay = t - 0.001f;

                if(AssetBundleModel.Model.Update())
                {
                    m_Parent.Repaint();
                }

                if (m_DetailsList != null)
                    m_DetailsList.Update();

                if (m_AssetList != null)
                    m_AssetList.Update();

            }
        }

        internal void ForceReloadData()
        {
            UpdateSelectedBundles(new List<AssetBundleModel.BundleInfo>());
            SetSelectedItems(new List<AssetBundleModel.AssetInfo>());
            m_BundleTree.SetSelection(new int[0]);
            AssetBundleModel.Model.ForceReloadData(m_BundleTree);
            m_Parent.Repaint();
        }

        internal void OnGUI(Rect pos)
        {
            m_Position = pos;

            if(m_BundleTree == null)
            {
                if (m_AssetListState == null)
                    m_AssetListState = new TreeViewState();

                var headerState = AssetListTree.CreateDefaultMultiColumnHeaderState();// multiColumnTreeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_AssetListMCHState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_AssetListMCHState, headerState);
                m_AssetListMCHState = headerState;


                m_AssetList = new AssetListTree(m_AssetListState, m_AssetListMCHState, this);
                m_AssetList.Reload();
                m_MessageList = new MessageList();

                if (m_BundleDetailState == null)
                    m_BundleDetailState = new TreeViewState();
                m_DetailsList = new BundleDetailList(m_BundleDetailState);
                m_DetailsList.Reload();

                if (m_BundleTreeState == null)
                    m_BundleTreeState = new TreeViewState();
                m_BundleTree = new AssetBundleTree(m_BundleTreeState, this);
                m_BundleTree.Refresh();
                m_Parent.Repaint();
            }
            
            HandleHorizontalResize();
            HandleVerticalResize();


            if (AssetBundleModel.Model.BundleListIsEmpty())
            {
                m_BundleTree.OnGUI(m_Position);
                var style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.wordWrap = true;
                GUI.Label(
                    new Rect(m_Position.x + 1f, m_Position.y + 1f, m_Position.width - 2f, m_Position.height - 2f), 
                    new GUIContent(AssetBundleModel.Model.GetEmptyMessage()),
                    style);
            }
            else
            {

                //Left half
                var bundleTreeRect = new Rect(
                    m_Position.x,
                    m_Position.y,
                    m_HorizontalSplitterRect.x,
                    m_VerticalSplitterRectLeft.y - m_Position.y);
                
                m_BundleTree.OnGUI(bundleTreeRect);
                m_DetailsList.OnGUI(new Rect(
                    bundleTreeRect.x,
                    bundleTreeRect.y + bundleTreeRect.height + k_SplitterWidth,
                    bundleTreeRect.width,
                    m_Position.height - bundleTreeRect.height - k_SplitterWidth*2));


                //Right half.
                float panelLeft = m_HorizontalSplitterRect.x + k_SplitterWidth;
                float panelWidth = m_VerticalSplitterRectRight.width - k_SplitterWidth * 2;
                float searchHeight = 20f;
                float panelTop = m_Position.y + searchHeight;
                float panelHeight = m_VerticalSplitterRectRight.y - panelTop;
                OnGUISearchBar(new Rect(panelLeft, m_Position.y, panelWidth, searchHeight));
                m_AssetList.OnGUI(new Rect(
                    panelLeft,
                    panelTop,
                    panelWidth,
                    panelHeight));
                m_MessageList.OnGUI(new Rect(
                    panelLeft,
                    panelTop + panelHeight + k_SplitterWidth,
                    panelWidth,
                    (m_Position.height - panelHeight) - k_SplitterWidth * 2));

                if (m_ResizingHorizontalSplitter || m_ResizingVerticalSplitterRight || m_ResizingVerticalSplitterLeft)
                    m_Parent.Repaint();
            }
        }

        void OnGUISearchBar(Rect rect)
        {
            m_BundleTree.searchString = m_searchField.OnGUI(rect, m_BundleTree.searchString);
            m_AssetList.searchString = m_BundleTree.searchString;
        }

        public bool hasSearch
        {
            get { return m_BundleTree.hasSearch;  }
        }

        private void HandleHorizontalResize()
        {
            m_HorizontalSplitterRect.x = (int)(m_Position.width * m_HorizontalSplitterPercent);
            m_HorizontalSplitterRect.height = m_Position.height;

            EditorGUIUtility.AddCursorRect(m_HorizontalSplitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && m_HorizontalSplitterRect.Contains(Event.current.mousePosition))
                m_ResizingHorizontalSplitter = true;

            if (m_ResizingHorizontalSplitter)
            {
                m_HorizontalSplitterPercent = Mathf.Clamp(Event.current.mousePosition.x / m_Position.width, 0.1f, 0.9f);
                m_HorizontalSplitterRect.x = (int)(m_Position.width * m_HorizontalSplitterPercent);
            }

            if (Event.current.type == EventType.MouseUp)
                m_ResizingHorizontalSplitter = false;
        }

        private void HandleVerticalResize()
        {
            m_VerticalSplitterRectRight.x = m_HorizontalSplitterRect.x;
            m_VerticalSplitterRectRight.y = (int)(m_HorizontalSplitterRect.height * m_VerticalSplitterPercentRight);
            m_VerticalSplitterRectRight.width = m_Position.width - m_HorizontalSplitterRect.x;
            m_VerticalSplitterRectLeft.y = (int)(m_HorizontalSplitterRect.height * m_VerticalSplitterPercentLeft);
            m_VerticalSplitterRectLeft.width = m_VerticalSplitterRectRight.width;


            EditorGUIUtility.AddCursorRect(m_VerticalSplitterRectRight, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.MouseDown && m_VerticalSplitterRectRight.Contains(Event.current.mousePosition))
                m_ResizingVerticalSplitterRight = true;

            EditorGUIUtility.AddCursorRect(m_VerticalSplitterRectLeft, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.MouseDown && m_VerticalSplitterRectLeft.Contains(Event.current.mousePosition))
                m_ResizingVerticalSplitterLeft = true;


            if (m_ResizingVerticalSplitterRight)
            {
                m_VerticalSplitterPercentRight = Mathf.Clamp(Event.current.mousePosition.y / m_HorizontalSplitterRect.height, 0.2f, 0.98f);
                m_VerticalSplitterRectRight.y = (int)(m_HorizontalSplitterRect.height * m_VerticalSplitterPercentRight);
            }
            else if (m_ResizingVerticalSplitterLeft)
            {
                m_VerticalSplitterPercentLeft = Mathf.Clamp(Event.current.mousePosition.y / m_HorizontalSplitterRect.height, 0.25f, 0.98f);
                m_VerticalSplitterRectLeft.y = (int)(m_HorizontalSplitterRect.height * m_VerticalSplitterPercentLeft);
            }


            if (Event.current.type == EventType.MouseUp)
            {
                m_ResizingVerticalSplitterRight = false;
                m_ResizingVerticalSplitterLeft = false;
            }
        }

        internal void UpdateSelectedBundles(IEnumerable<AssetBundleModel.BundleInfo> bundles)
        {
            AssetBundleModel.Model.AddBundlesToUpdate(bundles);
            m_AssetList.SetSelectedBundles(bundles);
            m_DetailsList.SetItems(bundles);
            m_MessageList.SetItems(null);
        }

        internal void SetSelectedItems(IEnumerable<AssetBundleModel.AssetInfo> items)
        {
            m_MessageList.SetItems(items);
        }
    }
}