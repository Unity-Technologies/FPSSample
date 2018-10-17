using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace AssetBundleBrowser.AssetBundleModel
{
    internal sealed class BundleTreeItem : TreeViewItem
    {   
        private BundleInfo m_Bundle;
        internal BundleInfo bundle
        {
            get { return m_Bundle; }
        }
        internal BundleTreeItem(BundleInfo b, int depth, Texture2D iconTexture) : base(b.nameHashCode, depth, b.displayName)
        {
            m_Bundle = b;
            icon = iconTexture;
            children = new List<TreeViewItem>();
        }

        internal MessageSystem.Message BundleMessage()
        {
            return m_Bundle.HighestMessage();
        }

        public override string displayName
        {
            get
            {
                return AssetBundleBrowserMain.instance.m_ManageTab.hasSearch ? m_Bundle.m_Name.fullNativeName : m_Bundle.displayName;
            }
        }
    }

    internal class BundleNameData
    {
        private List<string> m_PathTokens;
        private string m_FullBundleName;
        private string m_ShortName;
        private string m_VariantName;
        private string m_FullNativeName;

        //input (received from native) is a string of format:
        //  /folder0/.../folderN/name.variant
        //it's broken into:
        //  /m_pathTokens[0]/.../m_pathTokens[n]/m_shortName.m_variantName
        // and...
        //  m_fullBundleName = /m_pathTokens[0]/.../m_pathTokens[n]/m_shortName
        // and...
        //  m_fullNativeName = m_fullBundleName.m_variantName which is the same as the initial input.
        internal BundleNameData(string name) { SetName(name); }
        internal BundleNameData(string path, string name)
        {
            string finalName = System.String.IsNullOrEmpty(path) ? "" : path + '/';
            finalName += name;
            SetName(finalName);
        }
        public override int GetHashCode()
        {
            return fullNativeName.GetHashCode();
        }
        internal string fullNativeName
        { get { return m_FullNativeName; } }

        internal void SetBundleName(string bundleName, string variantName)
        {
            string name = bundleName;
            name += System.String.IsNullOrEmpty(variantName) ? "" : "." + variantName;
            SetName(name);
        }
        internal string bundleName
        {
            get { return m_FullBundleName; }
            //set { SetName(value); }
        }
        internal string shortName
        {
            get { return m_ShortName; }
        }
        internal string variant
        {
            get { return m_VariantName; }
            set
            {
                m_VariantName = value;
                m_FullNativeName = m_FullBundleName;
                m_FullNativeName += System.String.IsNullOrEmpty(m_VariantName) ? "" : "." + m_VariantName;
            }
        }
        internal List<string> pathTokens
        {
            get { return m_PathTokens; }
            set
            {
                m_PathTokens = value.GetRange(0, value.Count-1);
                SetShortName(value.Last());
                GenerateFullName();
            }
        }

        private void SetName(string name)
        {
            if(m_PathTokens == null)
                m_PathTokens = new List<string>();
            else
                m_PathTokens.Clear();

            int indexOfSlash = name.IndexOf('/');
            int previousIndex = 0;
            while(indexOfSlash != -1)
            {
                m_PathTokens.Add(name.Substring(previousIndex, (indexOfSlash - previousIndex)));
                previousIndex = indexOfSlash + 1;
                indexOfSlash = name.IndexOf('/', previousIndex);
            }
            SetShortName(name.Substring(previousIndex));
            GenerateFullName();
        }
        private void SetShortName(string inputName)
        {
            m_ShortName = inputName;
            int indexOfDot = m_ShortName.LastIndexOf('.');
            if (indexOfDot > -1)
            {
                m_VariantName = m_ShortName.Substring(indexOfDot + 1);
                m_ShortName = m_ShortName.Substring(0, indexOfDot);
            }
            else
                m_VariantName = string.Empty;
        }

        internal void PartialNameChange(string newToken, int indexFromBack)
        {
            if(indexFromBack == 0)
            {
                 SetShortName(newToken);
            }
            else if(indexFromBack-1 < m_PathTokens.Count)
            {
                m_PathTokens[m_PathTokens.Count - indexFromBack] = newToken;
            }
            GenerateFullName();
        }

        private void GenerateFullName()
        {
            m_FullBundleName = string.Empty;
            for(int i = 0; i < m_PathTokens.Count; i++)
            {
                m_FullBundleName += m_PathTokens[i];
                m_FullBundleName += '/';
            }
            m_FullBundleName += m_ShortName;
            m_FullNativeName = m_FullBundleName;
            m_FullNativeName += System.String.IsNullOrEmpty(m_VariantName) ? "" : "." + m_VariantName;
        }
    }

    internal abstract class BundleInfo
    {
        protected BundleFolderInfo m_Parent;
        protected bool m_DoneUpdating;
        protected bool m_Dirty;
        internal BundleNameData m_Name;
        protected MessageSystem.MessageState m_BundleMessages = new MessageSystem.MessageState();
        protected MessageSystem.Message m_CachedHighMessage = null;

        internal BundleInfo(string name, BundleFolderInfo parent)
        {
            m_Name = new BundleNameData(name);
            m_Parent = parent;
        }

        internal BundleFolderInfo parent
        { get { return m_Parent; } }
        internal virtual string displayName
        {
            get { return m_Name.shortName; }
        }
        internal virtual int nameHashCode
        {
            get { return m_Name.GetHashCode(); }
        }
        internal abstract BundleTreeItem CreateTreeView(int depth);

        protected virtual void RefreshMessages()
        {
            RefreshEmptyStatus();
            RefreshDupeAssetWarning();
            var flag = m_BundleMessages.HighestMessageFlag();
            m_CachedHighMessage = MessageSystem.GetMessage(flag);
        }
        internal abstract bool RefreshEmptyStatus();
        internal abstract bool RefreshDupeAssetWarning();
        internal virtual MessageSystem.Message HighestMessage()
        {
            if (m_CachedHighMessage == null)
                RefreshMessages();
            return m_CachedHighMessage;
        }
        internal bool IsMessageSet(MessageSystem.MessageFlag flag)
        {
            return m_BundleMessages.IsSet(flag);
        }
        internal void SetMessageFlag(MessageSystem.MessageFlag flag, bool on)
        {
            m_BundleMessages.SetFlag(flag, on);
        }
        internal List<MessageSystem.Message> GetMessages()
        {
            return m_BundleMessages.GetMessages();
        }
        internal bool HasMessages()
        {
            return m_BundleMessages.HasMessages();
        }

        internal virtual bool HandleRename(string newName, int reverseDepth)
        {
            if (reverseDepth == 0)
            {
                if (!m_Parent.HandleChildRename(m_Name.shortName, newName))
                    return false;
            }
            m_Name.PartialNameChange(newName, reverseDepth);
            return true;
        }
        internal virtual void HandleDelete(bool isRootOfDelete, string forcedNewName="", string forcedNewVariant = "")
        {
            if(isRootOfDelete)
            {
                m_Parent.HandleChildRename(m_Name.shortName, string.Empty);
            }
        }
        abstract internal void RefreshAssetList();
        abstract internal void AddAssetsToNode(AssetTreeItem node);
        abstract internal void Update();
        internal virtual bool doneUpdating
        { get { return m_DoneUpdating; } }
        internal virtual bool dirty
        { get { return m_Dirty; } }
        internal void ForceNeedUpdate()
        {
            m_DoneUpdating = false;
            m_Dirty = true;
        }

        abstract internal void HandleReparent(string parentName, BundleFolderInfo newParent = null);
        abstract internal List<AssetInfo> GetDependencies();

        abstract internal bool DoesItemMatchSearch(string search);
    }

    internal class BundleDataInfo : BundleInfo
    {
        protected List<AssetInfo> m_ConcreteAssets;
        protected List<AssetInfo> m_DependentAssets;
        protected HashSet<string> m_BundleDependencies;
        protected int m_ConcreteCounter;
        protected int m_DependentCounter;
        protected bool m_IsSceneBundle;
        protected long m_TotalSize;

        internal BundleDataInfo(string name, BundleFolderInfo parent) : base(name, parent)
        {
            m_ConcreteAssets = new List<AssetInfo>();
            m_DependentAssets = new List<AssetInfo>();
            m_BundleDependencies = new HashSet<string>();
            m_ConcreteCounter = 0;
            m_DependentCounter = 0;
        }
        ~BundleDataInfo()
        {
            foreach (var asset in m_DependentAssets)
            {
                AssetBundleModel.Model.UnRegisterAsset(asset, m_Name.fullNativeName);
            }
        }
        internal override bool HandleRename(string newName, int reverseDepth)
        { 
            RefreshAssetList();
            if (!base.HandleRename(newName, reverseDepth))
                return false;
            Model.MoveAssetToBundle(m_ConcreteAssets, m_Name.bundleName, m_Name.variant);
            return true;
        }
        internal override void HandleDelete(bool isRootOfDelete, string forcedNewName="", string forcedNewVariant="")
        {
            RefreshAssetList();
            base.HandleDelete(isRootOfDelete);
            Model.MoveAssetToBundle(m_ConcreteAssets, forcedNewName, forcedNewVariant);
        }

        internal string TotalSize()
        {
            if (m_TotalSize == 0)
                return "--";
            return EditorUtility.FormatBytes(m_TotalSize);
        }

        internal override void RefreshAssetList()
        {
            m_BundleMessages.SetFlag(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles, false);
            m_BundleMessages.SetFlag(MessageSystem.MessageFlag.SceneBundleConflict, false);
            m_BundleMessages.SetFlag(MessageSystem.MessageFlag.DependencySceneConflict, false);

            m_ConcreteAssets.Clear();
            m_TotalSize = 0;
            m_IsSceneBundle = false;

            foreach (var asset in m_DependentAssets)
            {
                AssetBundleModel.Model.UnRegisterAsset(asset, m_Name.fullNativeName);
            }
            m_DependentAssets.Clear();
            m_BundleDependencies.Clear();
            
            bool assetInBundle = false;
            bool sceneError = false;
            var assets = AssetBundleModel.Model.DataSource.GetAssetPathsFromAssetBundle(m_Name.fullNativeName);
            foreach(var assetName in assets)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(assetName) == typeof(SceneAsset))
                {
                    m_IsSceneBundle = true;
                    if(assetInBundle)
                        sceneError = true;
                }
                else
                {
                    assetInBundle = true;
                    if (m_IsSceneBundle)
                        sceneError = true;
                }

                var bundleName = Model.GetBundleName(assetName);
                if (System.String.IsNullOrEmpty(bundleName))  
                {
                    ///we get here if the current asset is only added due to being in an explicitly added folder
                    

                    var partialPath = assetName;
                    while(
                        !System.String.IsNullOrEmpty(partialPath) && 
                        partialPath != "Assets" &&
                        System.String.IsNullOrEmpty(bundleName))
                    {
                        partialPath = partialPath.Substring(0, partialPath.LastIndexOf('/'));
                        bundleName = Model.GetBundleName(partialPath);
                    }
                    if(!System.String.IsNullOrEmpty(bundleName))
                    {
                        var folderAsset = Model.CreateAsset(partialPath, bundleName);
                        folderAsset.isFolder = true;
                        if (m_ConcreteAssets.FindIndex(a => a.displayName == folderAsset.displayName) == -1)
                        {
                            m_ConcreteAssets.Add(folderAsset);
                        }
                        
                        m_DependentAssets.Add(Model.CreateAsset(assetName, folderAsset));
                        if (m_DependentAssets != null && m_DependentAssets.Count > 0)
                        {
                            var last = m_DependentAssets.Last();
                            if (last != null)
                                m_TotalSize += last.fileSize;
                        }
                    }
                }
                else
                {
                    var newAsset = Model.CreateAsset (assetName, m_Name.fullNativeName);
                    if (newAsset != null)
                    {
                        m_ConcreteAssets.Add(newAsset);
                        m_TotalSize += m_ConcreteAssets.Last().fileSize;
                        if (AssetDatabase.GetMainAssetTypeAtPath(assetName) == typeof(SceneAsset))
                        {
                            m_IsSceneBundle = true;
                            m_ConcreteAssets.Last().isScene = true;
                        }
                    }
                }
            }
            
            if(sceneError)
            {
                foreach (var asset in m_ConcreteAssets)
                {
                    if (asset.isFolder)
                    {
                        asset.SetMessageFlag(MessageSystem.MessageFlag.DependencySceneConflict, true);
                        m_BundleMessages.SetFlag(MessageSystem.MessageFlag.DependencySceneConflict, true);
                    }
                    else
                    {
                        asset.SetMessageFlag(MessageSystem.MessageFlag.SceneBundleConflict, true);
                        m_BundleMessages.SetFlag(MessageSystem.MessageFlag.SceneBundleConflict, true);
                    }
                }
            }


            m_ConcreteCounter = 0;
            m_DependentCounter = 0;
            m_Dirty = true;
        }

        internal override void AddAssetsToNode(AssetTreeItem node)
        {
            foreach (var asset in m_ConcreteAssets)
                node.AddChild(new AssetTreeItem(asset));

            foreach (var asset in m_DependentAssets)
            {
                if(!node.ContainsChild(asset))
                    node.AddChild(new AssetTreeItem(asset));
            }

            m_Dirty = false;
        }
        internal HashSet<string> GetBundleDependencies()
        {
            return m_BundleDependencies;
        }

        internal override void Update()
        {
            int dependents = m_DependentAssets.Count;
            int bundleDep = m_BundleDependencies.Count;
            if(m_ConcreteCounter < m_ConcreteAssets.Count)
            {
                GatherDependencies(m_ConcreteAssets[m_ConcreteCounter]);
                m_ConcreteCounter++;
                m_DoneUpdating = false;
            }
            else if (m_DependentCounter < m_DependentAssets.Count)
            {
                GatherDependencies(m_DependentAssets[m_DependentCounter], m_Name.fullNativeName);
                m_DependentCounter++;
                m_DoneUpdating = false;
            }
            else
            {
                m_DoneUpdating = true;
            }
            m_Dirty = (dependents != m_DependentAssets.Count) || (bundleDep != m_BundleDependencies.Count);
            if (m_Dirty || m_DoneUpdating)
                RefreshMessages();
        }

        private void GatherDependencies(AssetInfo asset, string parentBundle = "")
        {
            if (System.String.IsNullOrEmpty(parentBundle))
                parentBundle = asset.bundleName;

            if (asset == null)
                return;

            var deps = asset.GetDependencies();
            if (deps == null)
                return;

            foreach (var ai in deps)
            {
                if (ai == asset || m_ConcreteAssets.Contains(ai) || m_DependentAssets.Contains(ai))
                    continue;

                var bundleName = AssetBundleModel.Model.DataSource.GetImplicitAssetBundleName(ai.fullAssetName);
                if (string.IsNullOrEmpty(bundleName))
                {
                    m_DependentAssets.Add(ai);
                    m_TotalSize += ai.fileSize;
                    if (Model.RegisterAsset(ai, parentBundle) > 1)
                    {
                        SetDuplicateWarning();
                    }
                }
                else if(bundleName != m_Name.fullNativeName)
                {
                    m_BundleDependencies.Add(bundleName);
                }
            }
        }

        internal override bool RefreshDupeAssetWarning()
        {
            foreach(var asset in m_DependentAssets)
            {
                if (asset != null && asset.IsMessageSet(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles)) 
                {
                    SetDuplicateWarning();
                    return true;
                }
            }
            return false;
        }

        internal bool IsEmpty()
        {
            return (m_ConcreteAssets.Count == 0);
        }

        internal override bool RefreshEmptyStatus()
        {
            bool empty = IsEmpty();
            m_BundleMessages.SetFlag(MessageSystem.MessageFlag.EmptyBundle, empty);
            return empty;
        }

        protected void SetDuplicateWarning()
        {
            m_BundleMessages.SetFlag(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles, true);
            m_Dirty = true;
        }

        internal bool isSceneBundle
        { get { return m_IsSceneBundle; } }

        internal override BundleTreeItem CreateTreeView(int depth)
        {
            RefreshAssetList();
            RefreshMessages();
            if (isSceneBundle)
                return new BundleTreeItem(this, depth, Model.GetSceneIcon());
            else
                return new BundleTreeItem(this, depth, Model.GetBundleIcon());
        }

        internal override void HandleReparent(string parentName, BundleFolderInfo newParent = null)
        {
            RefreshAssetList();
            string newName = System.String.IsNullOrEmpty(parentName) ? "" : parentName + '/';
            newName += m_Name.shortName;
            if (newName == m_Name.bundleName)
                return;

            if (newParent != null && newParent.GetChild(newName) != null)
            {
                Model.LogWarning("An item named '" + newName + "' already exists at this level in hierarchy.  If your desire is to merge bundles, drag one on top of the other.");
                return;
            }
            
            foreach (var asset in m_ConcreteAssets)
            {
                Model.MoveAssetToBundle(asset, newName, m_Name.variant);
            }

            if (newParent != null)
            {
                m_Parent.HandleChildRename(m_Name.shortName, string.Empty);
                m_Parent = newParent;
                m_Parent.AddChild(this);
            }
            m_Name.SetBundleName(newName, m_Name.variant);
        }

        internal override List<AssetInfo> GetDependencies()
        {
            return m_DependentAssets;
        }

        internal override bool DoesItemMatchSearch(string search)
        {
            foreach(var asset in m_ConcreteAssets)
            {
                if (asset.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            foreach (var asset in m_DependentAssets)
            {
                if (asset.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }
    }

    internal class BundleVariantDataInfo : BundleDataInfo
    {
        protected List<AssetInfo> m_FolderIncludeAssets = new List<AssetInfo>();
        internal BundleVariantDataInfo(string name, BundleFolderInfo parent) : base(name, parent)
        {
        }

        internal override string displayName
        {
            get { return m_Name.variant; }
        }
        internal override void Update()
        {
            base.Update();
            (m_Parent as BundleVariantFolderInfo).ValidateVariants();
        }
        internal override void RefreshAssetList()
        {
            m_FolderIncludeAssets.Clear();
            base.RefreshAssetList();
            if(m_DependentAssets.Count > 0)
                m_FolderIncludeAssets = new List<AssetInfo>(m_DependentAssets);
        }
        internal bool IsSceneVariant()
        {
            RefreshAssetList();
            return isSceneBundle;
        }
        internal override bool HandleRename(string newName, int reverseDepth)
        {
            if (reverseDepth == 0)
            {
                RefreshAssetList();
                if (!m_Parent.HandleChildRename(m_Name.variant, newName))
                    return false;
                m_Name.variant = newName;
                Model.MoveAssetToBundle(m_ConcreteAssets, m_Name.bundleName, m_Name.variant);
            }
            else if (reverseDepth == 1)
            {
                RefreshAssetList();
                m_Name.PartialNameChange(newName + "." + m_Name.variant, 0);
                Model.MoveAssetToBundle(m_ConcreteAssets, m_Name.bundleName, m_Name.variant);
            }
            else
            {
                return base.HandleRename(newName, reverseDepth-1);
            }
            return true;
        }
        internal override void HandleDelete(bool isRootOfDelete, string forcedNewName = "", string forcedNewVariant = "")
        {
            RefreshAssetList();
            if (isRootOfDelete)
            {
                m_Parent.HandleChildRename(m_Name.variant, string.Empty);
            }
            Model.MoveAssetToBundle(m_ConcreteAssets, forcedNewName, forcedNewVariant);
        }

        internal bool FindContentMismatch(BundleVariantDataInfo other)
        {
            bool result = false;

            if (m_FolderIncludeAssets.Count != 0 || other.m_FolderIncludeAssets.Count != 0)
            {
                var myUniqueAssets = new HashSet<string>();
                var otherUniqueAssets = new HashSet<string>(other.m_FolderIncludeAssets.Select(x => x.displayName));

                foreach (var asset in m_FolderIncludeAssets)
                {
                    if (!otherUniqueAssets.Remove(asset.displayName))
                    {
                        myUniqueAssets.Add(asset.displayName);
                    }
                }

                if (myUniqueAssets.Count > 0)
                {
                    m_BundleMessages.SetFlag(MessageSystem.MessageFlag.VariantBundleMismatch, true);
                    result = true;
                }
                if (otherUniqueAssets.Count > 0)
                {
                    other.m_BundleMessages.SetFlag(MessageSystem.MessageFlag.VariantBundleMismatch, true);
                    result = true;
                }
            }
            else //this doesn't cover the super weird case of including a folder and some explicit assets. TODO - fix that.
            {
                var myUniqueAssets = new HashSet<string>();
                var otherUniqueAssets = new HashSet<string>(other.m_ConcreteAssets.Select(x => x.displayName));

                foreach (var asset in m_ConcreteAssets)
                {
                    if (!otherUniqueAssets.Remove(asset.displayName))
                    {
                        myUniqueAssets.Add(asset.displayName);
                    }
                }

                if (myUniqueAssets.Count > 0)
                {
                    m_BundleMessages.SetFlag(MessageSystem.MessageFlag.VariantBundleMismatch, true);
                    result = true;
                }
                if (otherUniqueAssets.Count > 0)
                {
                    other.m_BundleMessages.SetFlag(MessageSystem.MessageFlag.VariantBundleMismatch, true);
                    result = true;
                }
            }
            return result;
        }
    }


    internal abstract class BundleFolderInfo : BundleInfo
    {
        protected Dictionary<string, BundleInfo> m_Children;

        internal BundleFolderInfo(string name, BundleFolderInfo parent) : base(name, parent)
        {
            m_Children = new Dictionary<string, BundleInfo>();
        }
        
        internal BundleFolderInfo(List<string> path, int depth, BundleFolderInfo parent) : base("", parent)
        {
            m_Children = new Dictionary<string, BundleInfo>();
            m_Name = new BundleNameData("");
            m_Name.pathTokens = path.GetRange(0, depth);
        }

        internal BundleInfo GetChild(string name)
        {
            if (name == null)
                return null;

            BundleInfo info = null;
            if (m_Children.TryGetValue(name, out info))
                return info;
            return null;
        }
        internal Dictionary<string, BundleInfo>.ValueCollection GetChildList()
        {
            return m_Children.Values;
        }
        internal abstract void AddChild(BundleInfo info);

        internal override bool HandleRename(string newName, int reverseDepth)
        {
            if (!base.HandleRename(newName, reverseDepth))
                return false;

            foreach (var child in m_Children)
            {
                child.Value.HandleRename(newName, reverseDepth + 1);
            }
            return true;
        }

        internal override void HandleDelete(bool isRootOfDelete, string forcedNewName="", string forcedNewVariant = "")
        {
            base.HandleDelete(isRootOfDelete);
            foreach (var child in m_Children)
            {
                child.Value.HandleDelete(false, forcedNewName, forcedNewVariant);
            }
            m_Children.Clear();
        }

        internal override bool DoesItemMatchSearch(string search)
        {
            return false; //folders don't ever match.
        }

        protected override void RefreshMessages()
        {
            m_BundleMessages.SetFlag(MessageSystem.MessageFlag.ErrorInChildren, false);
            foreach(var child in m_Children)
            {
                if (child.Value.IsMessageSet(MessageSystem.MessageFlag.Error))
                {
                    m_BundleMessages.SetFlag(MessageSystem.MessageFlag.ErrorInChildren, true);
                    break;
                }
            }
            base.RefreshMessages();
        }
        internal override bool RefreshEmptyStatus()
        {
            bool empty = m_Children.Count == 0;
            foreach (var child in m_Children)
            {
                empty |= child.Value.RefreshEmptyStatus();
            }
            m_BundleMessages.SetFlag(MessageSystem.MessageFlag.EmptyFolder, empty);
            return empty;
        }

        internal override void RefreshAssetList()
        {
            foreach (var child in m_Children)
            {
                child.Value.RefreshAssetList();
            }
        }
        internal override bool RefreshDupeAssetWarning()
        {
            bool dupeWarning = false;
            foreach (var child in m_Children)
            {
                dupeWarning |= child.Value.RefreshDupeAssetWarning();
            }
            m_BundleMessages.SetFlag(MessageSystem.MessageFlag.WarningInChildren, dupeWarning);
            return dupeWarning;
        }
        internal override void AddAssetsToNode(AssetTreeItem node)
        {
            foreach (var child in m_Children)
            {
                child.Value.AddAssetsToNode(node);
            }
            m_Dirty = false;
        }
        internal virtual bool HandleChildRename(string oldName, string newName)
        {

            if (!System.String.IsNullOrEmpty(newName) && m_Children.ContainsKey(newName))
            {
                Model.LogWarning("Attempting to name an item '" + newName + "' which matches existing name at this level in hierarchy.  If your desire is to merge bundles, drag one on top of the other.");
                return false;
            }

            BundleInfo info = null;
            if (m_Children.TryGetValue(oldName, out info))
            {
                m_Children.Remove(oldName);
                if (!System.String.IsNullOrEmpty(newName))
                    m_Children.Add(newName, info);
            }
            return true;
        }

        internal override void Update()
        {
            m_Dirty = false;
            m_DoneUpdating = true;
            foreach (var child in m_Children)
            {
                child.Value.Update();
                m_Dirty |= child.Value.dirty;
                m_DoneUpdating &= child.Value.doneUpdating;
            }

            if (m_Dirty || m_DoneUpdating)
                RefreshMessages();
        }
        internal override bool doneUpdating
        {
            get
            {
                foreach (var child in m_Children)
                {
                    m_DoneUpdating &= child.Value.doneUpdating;
                }
                return base.doneUpdating;
            }
        }


        internal override List<AssetInfo> GetDependencies()
        {
            List<AssetInfo> assets = new List<AssetInfo>();
            foreach (var child in m_Children)
            {
                assets.AddRange(child.Value.GetDependencies());
            }
            return assets;
        }
    }

    internal class BundleFolderConcreteInfo : BundleFolderInfo
    {
        internal BundleFolderConcreteInfo(string name, BundleFolderInfo parent) : base(name, parent)
        {
        }

        internal BundleFolderConcreteInfo(List<string> path, int depth, BundleFolderInfo parent) : base(path, depth, parent)
        {
        }

        internal override void AddChild(BundleInfo info)
        {
            m_Children.Add(info.displayName, info);
        }
        internal override BundleTreeItem CreateTreeView(int depth)
        {
            RefreshMessages();
            var result = new BundleTreeItem(this, depth, Model.GetFolderIcon());
            foreach (var child in m_Children)
            {
                result.AddChild(child.Value.CreateTreeView(depth + 1));
            }
            return result;
        }
        internal override void HandleReparent(string parentName, BundleFolderInfo newParent = null)
        {
            string newName = System.String.IsNullOrEmpty(parentName) ? "" : parentName + '/';
            newName += displayName;
            if (newName == m_Name.bundleName)
                return;

            if (newParent != null && newParent.GetChild(newName) != null)
            {
                Model.LogWarning("An item named '" + newName + "' already exists at this level in hierarchy.  If your desire is to merge bundles, drag one on top of the other.");
                return;
            }

            foreach (var child in m_Children)
            {
                child.Value.HandleReparent(newName);
            }

            if (newParent != null)
            {
                m_Parent.HandleChildRename(m_Name.shortName, string.Empty);
                m_Parent = newParent;
                m_Parent.AddChild(this);
            }
            m_Name.SetBundleName(newName, m_Name.variant);
        }
    }


    internal class BundleVariantFolderInfo : BundleFolderInfo
    {
        internal BundleVariantFolderInfo(string name, BundleFolderInfo parent) : base(name, parent)
        {
        }
        internal override void AddChild(BundleInfo info)
        {
            m_Children.Add(info.m_Name.variant, info);
        }
        private bool m_validated;
        internal override void Update()
        {
            m_validated = false;
            base.Update();
            if(!m_validated)
               ValidateVariants();
        }
        internal void ValidateVariants()
        {
            m_validated = true;
            bool childMismatch = false;
            if(m_Children.Count > 1)
            {
                BundleVariantDataInfo goldChild = null;
                foreach(var c in m_Children)
                {
                    var child = c.Value as BundleVariantDataInfo;
                    child.SetMessageFlag(MessageSystem.MessageFlag.VariantBundleMismatch, false);
                    if (goldChild == null)
                    {
                        goldChild = child;
                        continue;
                    }
                    childMismatch |= goldChild.FindContentMismatch(child);
                }
            }
            m_BundleMessages.SetFlag(MessageSystem.MessageFlag.VariantBundleMismatch, childMismatch);

        }

        internal override BundleTreeItem CreateTreeView(int depth)
        {
            RefreshMessages();
            Texture2D icon = null;
            if ((m_Children.Count > 0) &&
                ((m_Children.First().Value as BundleVariantDataInfo).IsSceneVariant()))
            {
                icon = Model.GetSceneIcon();
            }
            else
                icon = Model.GetBundleIcon();

            var result = new BundleTreeItem(this, depth, icon);
            foreach (var child in m_Children)
            {
                result.AddChild(child.Value.CreateTreeView(depth + 1));
            }
            return result;
        }

        internal override void HandleReparent(string parentName, BundleFolderInfo newParent = null)
        {
            string newName = System.String.IsNullOrEmpty(parentName) ? "" : parentName + '/';
            newName += displayName;
            if (newName == m_Name.bundleName)
                return;

            if (newParent != null && newParent.GetChild(newName) != null)
            {
                Model.LogWarning("An item named '" + newName + "' already exists at this level in hierarchy.  If your desire is to merge bundles, drag one on top of the other.");
                return;
            }

            foreach (var child in m_Children)
            {
                child.Value.HandleReparent(parentName);
            }

            if (newParent != null)
            {
                m_Parent.HandleChildRename(m_Name.shortName, string.Empty);
                m_Parent = newParent;
                m_Parent.AddChild(this);
            }
            m_Name.SetBundleName(newName, string.Empty) ;
        }
        internal override bool HandleChildRename(string oldName, string newName)
        {
            var result = base.HandleChildRename(oldName, newName);
            if (m_Children.Count == 0)
                HandleDelete(true);
            return result;
        }
    }

}
