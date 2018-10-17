using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;

namespace AssetBundleBrowser
{
    [System.Serializable]
    internal class AssetBundleInspectTab
    {
        Rect m_Position;

        [SerializeField]
        private InspectTabData m_Data;
        

        private Dictionary<string, List<string> > m_BundleList;
        private InspectBundleTree m_BundleTreeView;
        [SerializeField]
        private TreeViewState m_BundleTreeState;

        internal Editor m_Editor = null;

        private SingleBundleInspector m_SingleInspector;

        /// <summary>
        /// Collection of loaded asset bundle records indexed by bundle name
        /// </summary>
        private Dictionary<string, AssetBundleRecord> m_loadedAssetBundles;

        /// <summary>
        /// Returns the record for a loaded asset bundle by name if it exists in our container.
        /// </summary>
        /// <returns>Asset bundle record instance if loaded, otherwise null.</returns>
        /// <param name="bundleName">Name of the loaded asset bundle, excluding the variant extension</param>
        private AssetBundleRecord GetLoadedBundleRecordByName(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                return null;
            }

            if (!m_loadedAssetBundles.ContainsKey(bundleName))
            {
                return null;
            }

            return m_loadedAssetBundles[bundleName];
        }

        internal AssetBundleInspectTab()
        {
            m_BundleList = new Dictionary<string, List<string>>();
            m_SingleInspector = new SingleBundleInspector();
            m_loadedAssetBundles = new Dictionary<string, AssetBundleRecord>();
        }

        internal void OnEnable(Rect pos)
        {
            m_Position = pos;
            if (m_Data == null)
                m_Data = new InspectTabData();

            //LoadData...
            var dataPath = System.IO.Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/Library/AssetBundleBrowserInspect.dat";

            if (File.Exists(dataPath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(dataPath, FileMode.Open);
                var data = bf.Deserialize(file) as InspectTabData;
                if (data != null)
                    m_Data = data;
                file.Close();
            }


            if (m_BundleList == null)
                m_BundleList = new Dictionary<string, List<string>>();

            if (m_BundleTreeState == null)
                m_BundleTreeState = new TreeViewState();
            m_BundleTreeView = new InspectBundleTree(m_BundleTreeState, this);


            RefreshBundles();
        }

        internal void OnDisable()
        {
            ClearData();

            var dataPath = System.IO.Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/Library/AssetBundleBrowserInspect.dat";

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(dataPath);

            bf.Serialize(file, m_Data);
            file.Close();
        }

        internal void OnGUI(Rect pos)
        {
            m_Position = pos;

            if (Application.isPlaying)
            {
                var style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.wordWrap = true;
                GUI.Label(
                    new Rect(m_Position.x + 1f, m_Position.y + 1f, m_Position.width - 2f, m_Position.height - 2f),
                    new GUIContent("Inspector unavailable while in PLAY mode"),
                    style);
            }
            else
            {
                OnGUIEditor();
            }
        }

        private void OnGUIEditor()
        {
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Add File", GUILayout.MaxWidth(75f)))
            {
                BrowseForFile();
            }
            if (GUILayout.Button("Add Folder", GUILayout.MaxWidth(75f)))
            {
                BrowseForFolder();
            }

            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (m_BundleList.Count > 0)
            {
                int halfWidth = (int)(m_Position.width / 2.0f);
                m_BundleTreeView.OnGUI(new Rect(m_Position.x, m_Position.y + 30, halfWidth, m_Position.height - 30));
                m_SingleInspector.OnGUI(new Rect(m_Position.x + halfWidth, m_Position.y + 30, halfWidth, m_Position.height - 30));
            }
        }

        internal void RemoveBundlePath(string pathToRemove)
        {
            UnloadBundle(pathToRemove);
            m_Data.RemovePath(pathToRemove);
        }
        internal void RemoveBundleFolder(string pathToRemove)
        {
            List<string> paths = null;
            if(m_BundleList.TryGetValue(pathToRemove, out paths))
            {
                foreach(var p in paths)
                {
                    UnloadBundle(p);
                }
            }
            m_Data.RemoveFolder(pathToRemove);
        }

        private void BrowseForFile()
        {
            var newPath = EditorUtility.OpenFilePanelWithFilters("Bundle Folder", string.Empty, new string[] { });
            if (!string.IsNullOrEmpty(newPath))
            {
                var gamePath = System.IO.Path.GetFullPath(".");//TODO - FileUtil.GetProjectRelativePath??
                gamePath = gamePath.Replace("\\", "/");
                if (newPath.StartsWith(gamePath))
                    newPath = newPath.Remove(0, gamePath.Length + 1);

                m_Data.AddPath(newPath);

                RefreshBundles();
            }
        }

        //TODO - this is largely copied from BuildTab, should maybe be shared code.
        private void BrowseForFolder(string folderPath = null)
        {
           folderPath = EditorUtility.OpenFolderPanel("Bundle Folder", string.Empty, string.Empty);
            if (!string.IsNullOrEmpty(folderPath))
            {
                var gamePath = System.IO.Path.GetFullPath(".");//TODO - FileUtil.GetProjectRelativePath??
                gamePath = gamePath.Replace("\\", "/");
                if (folderPath.StartsWith(gamePath))
                    folderPath = folderPath.Remove(0, gamePath.Length + 1);

                AddBundleFolder(folderPath);

                RefreshBundles();
            }
        }

        internal void AddBundleFolder(string folderPath)
        {
            m_Data.AddFolder(folderPath);
        }

        private void ClearData()
        {
            m_SingleInspector.SetBundle(null);

            if (null != m_loadedAssetBundles)
            {
                List<AssetBundleRecord> records = new List<AssetBundleRecord>(m_loadedAssetBundles.Values);
                foreach (AssetBundleRecord record in records)
                {
                    record.bundle.Unload(true);
                }

                m_loadedAssetBundles.Clear();
            }
        }

        internal void RefreshBundles()
        {
            ClearData();


            if (m_Data.BundlePaths == null)
                return;

            //find assets
            if (m_BundleList == null)
                m_BundleList = new Dictionary<string, List<string>>();

            m_BundleList.Clear();
            var pathsToRemove = new List<string>();
            foreach(var filePath in m_Data.BundlePaths)
            {
                if(File.Exists(filePath))
                {
                    AddBundleToList(string.Empty, filePath);
                }
                else
                {
                    Debug.Log("Expected bundle not found: " + filePath);
                    pathsToRemove.Add(filePath);
                }
            }
            foreach(var path in pathsToRemove)
            {
                m_Data.RemovePath(path);
            }
            pathsToRemove.Clear();

            foreach(var folder in m_Data.BundleFolders)
            {
                if(Directory.Exists(folder.path))
                {
                    AddFilePathToList(folder.path, folder.path);
                }
                else
                {
                    Debug.Log("Expected folder not found: " + folder);
                    pathsToRemove.Add(folder.path);
                }
            }
            foreach (var path in pathsToRemove)
            {
                m_Data.RemoveFolder(path);
            }

            m_BundleTreeView.Reload();
        }

        private void AddBundleToList(string parent, string bundlePath)
        {
            List<string> bundles = null;
            m_BundleList.TryGetValue(parent, out bundles);

            if(bundles == null)
            {
                bundles = new List<string>();
                m_BundleList.Add(parent, bundles);
            }
            bundles.Add(bundlePath);
        }
        private void AddFilePathToList(string rootPath, string path)
        {
            var notAllowedExtensions = new string[] { ".meta", ".manifest", ".dll", ".cs", ".exe", ".js" };
            foreach (var file in Directory.GetFiles(path))
            {
                var ext = Path.GetExtension(file);
                if(!notAllowedExtensions.Contains(ext))
                {
                    var f = file.Replace('\\', '/');
                    if (File.Exists(file) && !m_Data.FolderIgnoresFile(rootPath, f))
                    {
                        AddBundleToList(rootPath, f);
                    }
                }
            }

            foreach (var dir in Directory.GetDirectories(path))
            {
                AddFilePathToList(rootPath, dir);
            }
        }

        internal Dictionary<string, List<string>> BundleList
        { get { return m_BundleList; } }


        internal void SetBundleItem(IList<InspectTreeItem> selected)
        {
            //m_SelectedBundleTreeItems = selected;
            if (selected == null || selected.Count == 0 || selected[0] == null)
            {
                m_SingleInspector.SetBundle(null);
            }
            else if(selected.Count == 1)
            {
                AssetBundle bundle = LoadBundle(selected[0].bundlePath);
                m_SingleInspector.SetBundle(bundle, selected[0].bundlePath, m_Data, this);
            }
            else
            {
                m_SingleInspector.SetBundle(null);

                //perhaps there should be a way to set a message in the inspector, to tell it...
                //var style = GUI.skin.label;
                //style.alignment = TextAnchor.MiddleCenter;
                //style.wordWrap = true;
                //GUI.Label(
                //    inspectorRect,
                //    new GUIContent("Multi-select inspection not supported"),
                //    style);
            }
        }

        [System.Serializable]
        internal class InspectTabData
        {
            [SerializeField]
            private List<string> m_BundlePaths = new List<string>();
            [SerializeField]
            private List<BundleFolderData> m_BundleFolders = new List<BundleFolderData>();

            internal IList<string> BundlePaths { get { return m_BundlePaths.AsReadOnly(); } }
            internal IList<BundleFolderData> BundleFolders { get { return m_BundleFolders.AsReadOnly(); } }

            internal void AddPath(string newPath)
            {
                if (!m_BundlePaths.Contains(newPath))
                {
                    var possibleFolderData = FolderDataContainingFilePath(newPath);
                    if(possibleFolderData == null)
                    {
                        m_BundlePaths.Add(newPath);
                    }
                    else
                    {
                        possibleFolderData.ignoredFiles.Remove(newPath);
                    }
                }
            }

            internal void AddFolder(string newPath)
            {
                if (!BundleFolderContains(newPath))
                    m_BundleFolders.Add(new BundleFolderData(newPath));
            }

            internal void RemovePath(string pathToRemove)
            {
                m_BundlePaths.Remove(pathToRemove);
            }

            internal void RemoveFolder(string pathToRemove)
            {
                m_BundleFolders.Remove(BundleFolders.FirstOrDefault(bfd => bfd.path == pathToRemove));
            }

            internal bool FolderIgnoresFile(string folderPath, string filePath)
            {
                if (BundleFolders == null)
                    return false;
                var bundleFolderData = BundleFolders.FirstOrDefault(bfd => bfd.path == folderPath);
                return bundleFolderData != null && bundleFolderData.ignoredFiles.Contains(filePath);
            }

            internal BundleFolderData FolderDataContainingFilePath(string filePath)
            {
                foreach (var bundleFolderData in BundleFolders)
                {
                    if (Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(bundleFolderData.path)))
                    {
                        return bundleFolderData;
                    }
                }
                return null;
            }

            private bool BundleFolderContains(string folderPath)
            {
                foreach(var bundleFolderData in BundleFolders)
                {
                    if(Path.GetFullPath(bundleFolderData.path) == Path.GetFullPath(folderPath))
                    {
                        return true;
                    }
                }
                return false;
            }

            [System.Serializable]
            internal class BundleFolderData
            {
                [SerializeField]
                internal string path;

                [SerializeField]
                private List<string> m_ignoredFiles;
                internal List<string> ignoredFiles
                {
                    get
                    {
                        if (m_ignoredFiles == null)
                            m_ignoredFiles = new List<string>();
                        return m_ignoredFiles;
                    }
                }

                internal BundleFolderData(string p)
                {
                    path = p;
                }
            }
        }

        /// <summary>
        /// Returns the bundle at the specified path, loading it if necessary.
        /// Unloads previously loaded bundles if necessary when dealing with variants.
        /// </summary>
        /// <returns>Returns the loaded bundle, null if it could not be loaded.</returns>
        /// <param name="path">Path of bundle to get</param>
        private AssetBundle LoadBundle(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string extension = Path.GetExtension(path);

            string bundleName = path.Substring(0, path.Length - extension.Length);

            // Check if we have a record for this bundle
            AssetBundleRecord record = GetLoadedBundleRecordByName(bundleName);
            AssetBundle bundle = null;
            if (null != record)
            {
                // Unload existing bundle if variant names differ, otherwise use existing bundle
                if (!record.path.Equals(path))
                {
                    UnloadBundle(bundleName);
                }
                else
                {
                    bundle = record.bundle;
                }
            }
                
            if (null == bundle)
            {
                // Load the bundle
                bundle = AssetBundle.LoadFromFile(path);
                if (null == bundle)
                {
                    return null;
                }

                m_loadedAssetBundles[bundleName] = new AssetBundleRecord(path, bundle);

                // Load the bundle's assets
                string[] assetNames = bundle.GetAllAssetNames();
                foreach (string name in assetNames)
                {
                    bundle.LoadAsset(name);
                }
            }

            return bundle;
        }

        /// <summary>
        /// Unloads the bundle with the given name.
        /// </summary>
        /// <param name="bundleName">Name of the bundle to unload without variant extension</param>
        private void UnloadBundle(string bundleName)
        {
            AssetBundleRecord record = this.GetLoadedBundleRecordByName(bundleName);
            if (null == record)
            {
                return;
            }

            record.bundle.Unload(true);
            m_loadedAssetBundles.Remove(bundleName);
        }
    }
}