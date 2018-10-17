using System;
using System.IO;
using UnityEngine;


namespace UnityEngine.Recorder
{
    [Serializable]
    public struct OutputPath
    {
        public enum ERoot
        {
            Absolute,
            Current,
            PersistentData,
            StreamingAssets,
            TemporaryCache,
            AssetsPath,
        }

        [SerializeField] ERoot m_root;
        [SerializeField] string m_leaf;

        public ERoot root
        {
            get { return m_root; }
            set { m_root = value; }
        }
        public string leaf
        {
            get { return m_leaf; }
            set { m_leaf = value; }
        }

        public static OutputPath FromPath(string path)
        {
            OutputPath result;
            if (path.Contains(Application.streamingAssetsPath))
            {
                result.m_root = ERoot.StreamingAssets;
                result.m_leaf = path.Replace(Application.streamingAssetsPath, "");
            }
            else if (path.Contains(Application.dataPath))
            {
                result.m_root = ERoot.AssetsPath;
                result.m_leaf = path.Replace(Application.dataPath, "");
            }
            else if (path.Contains(Application.persistentDataPath))
            {
                result.m_root = ERoot.PersistentData;
                result.m_leaf = path.Replace(Application.persistentDataPath, "");
            }
            else if (path.Contains(Application.temporaryCachePath))
            {
                result.m_root = ERoot.TemporaryCache;
                result.m_leaf = path.Replace(Application.temporaryCachePath, "");
            }
            else if( path.Contains(Directory.GetCurrentDirectory().Replace(@"\", "/")))
            {
                result.m_root = ERoot.Current;
                result.m_leaf = path.Replace(Directory.GetCurrentDirectory().Replace(@"\", "/"), "");
            }
            else
            {
                result.m_root = ERoot.Absolute;
                result.m_leaf = path;
            }

            return result;
        }

        public static string GetFullPath(ERoot root, string leaf)
        {
            if (root == ERoot.Absolute)
            {
                return leaf;
            }
            if (root == ERoot.Current)
            {
                return string.IsNullOrEmpty(leaf) ? "." : "./" + leaf;
            }

            string ret = "";
            switch (root)
            {
                case ERoot.PersistentData:
                    ret = Application.persistentDataPath;
                    break;
                case ERoot.StreamingAssets:
                    ret = Application.streamingAssetsPath;
                    break;
                case ERoot.TemporaryCache:
                    ret = Application.temporaryCachePath;
                    break;
                case ERoot.AssetsPath:
                    ret = Application.dataPath;
                    break;
            }

            if (!leaf.StartsWith("/"))
            {
                ret += "/";
            }
            ret += leaf;
            return ret;            
        }

        public string GetFullPath()
        {
            return GetFullPath(m_root, m_leaf);
        }

        public void CreateDirectory()
        {
            var path = GetFullPath();
            if(path.Length > 0 && !System.IO.Directory.Exists(path) )
            {
                System.IO.Directory.CreateDirectory(path);
            }
        }
    }
}