using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.Linq;

namespace AssetBundleBrowser
{
	internal class InspectTreeItem : TreeViewItem
	{
        internal string bundlePath { get; private set; }
            
		internal InspectTreeItem(string path, int depth) : base(path.GetHashCode(), depth, path)
		{
            bundlePath = path;
        }
        internal InspectTreeItem(string path, int depth, string prettyName) : base(path.GetHashCode(), depth, prettyName)
        {
            bundlePath = path;
        }
    }

	class InspectBundleTree : TreeView
	{
		AssetBundleInspectTab m_InspectTab;
		internal InspectBundleTree(TreeViewState s, AssetBundleInspectTab parent) : base(s)
		{
			m_InspectTab = parent;
			showBorder = true;
		}

		protected override TreeViewItem BuildRoot()
		{
			var root = new TreeViewItem(-1, -1);
			root.children = new List<TreeViewItem>();
			if (m_InspectTab == null)
				Debug.Log("Unknown problem in AssetBundle Browser Inspect tab.  Restart Browser and try again, or file ticket on github.");
			else
			{
				foreach (var folder in m_InspectTab.BundleList)
				{
                    if (System.String.IsNullOrEmpty(folder.Key))
                    {
                        foreach(var path in folder.Value)
                            root.AddChild(new InspectTreeItem(path, 0));
                    }
                    else
                    {
                        var folderItem = new TreeViewItem(folder.Key.GetHashCode(), 0, folder.Key);
                        foreach (var path in folder.Value)
                        {

                            var prettyName = path;
                            if (path.StartsWith(folder.Key)) //how could it not?
                                prettyName = path.Remove(0, folder.Key.Length + 1);

                            folderItem.AddChild(new InspectTreeItem(path, 1, prettyName));
                        }
                        root.AddChild(folderItem);
                    }
				}
			}
			return root;
		}

		public override void OnGUI(Rect rect)
		{
			base.OnGUI(rect);
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
			{
				SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
			}
		}

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);
            if (args.item.depth == 0)
            {
                var width = 16;
                var edgeRect = new Rect(args.rowRect.xMax - width, args.rowRect.y, width, args.rowRect.height);
                if (GUI.Button(edgeRect, "-"))
                {
                    if (GetSelection().Contains(args.item.id))
                    {
                        var selection = GetSelection();
                        foreach (var id in selection)
                        {
                            var item = FindItem(id, rootItem);
                            if(item.depth == 0)
                                RemoveItem(item);
                        }
                    }
                    else
                    {
                        RemoveItem(args.item);
                    }
                    m_InspectTab.RefreshBundles();
                }
            }
        }
        private void RemoveItem(TreeViewItem item)
        {
            var inspectItem = item as InspectTreeItem;
            if (inspectItem != null)
                m_InspectTab.RemoveBundlePath(inspectItem.bundlePath);
            else
                m_InspectTab.RemoveBundleFolder(item.displayName);
        }
        protected override void SelectionChanged(IList<int> selectedIds)
		{
			base.SelectionChanged(selectedIds);

            if (selectedIds == null)
                return;

			if (selectedIds.Count > 0)
			{
                m_InspectTab.SetBundleItem(FindRows(selectedIds).Select(tvi => tvi as InspectTreeItem).ToList());
				//m_InspectTab.SetBundleItem(FindItem(selectedIds[0], rootItem) as InspectTreeItem);
			}
			else
            {
				m_InspectTab.SetBundleItem(null);
            }
		}

		protected override bool CanMultiSelect(TreeViewItem item)
		{
			return true;
		}
	}

}
