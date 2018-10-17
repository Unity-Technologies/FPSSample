using System;
using UnityEngine;


namespace UTJ.FrameCapturer
{
    [Serializable]
    public class DataPath
    {
        public enum Root
        {
            Absolute,
            Current,
            PersistentData,
            StreamingAssets,
            TemporaryCache,
            DataPath,
        }

        [SerializeField] Root m_root = Root.Current;
        [SerializeField] string m_leaf = string.Empty;
#if UNITY_EDITOR
        [SerializeField] bool m_readOnly = false; // just for inspector
#endif

        public Root root
        {
            get { return m_root; }
            set { m_root = value; }
        }
        public string leaf
        {
            get { return m_leaf; }
            set { m_leaf = value; }
        }
        public bool readOnly
        {
#if UNITY_EDITOR
            get { return m_readOnly; }
            set { m_readOnly = value; }
#else
            get { return false; }
            set { }
#endif
        }

        public DataPath() { }
        public DataPath(Root root, string leaf)
        {
            m_root = root;
            m_leaf = leaf;
        }

        public DataPath(string path)
        {
            if (path.Contains(Application.streamingAssetsPath))
            {
                m_root = Root.StreamingAssets;
                m_leaf = path.Replace(Application.streamingAssetsPath, "").TrimStart('/');
            }
            else if (path.Contains(Application.dataPath))
            {
                m_root = Root.DataPath;
                m_leaf = path.Replace(Application.dataPath, "").TrimStart('/');
            }
            else if (path.Contains(Application.persistentDataPath))
            {
                m_root = Root.PersistentData;
                m_leaf = path.Replace(Application.persistentDataPath, "").TrimStart('/');
            }
            else if (path.Contains(Application.temporaryCachePath))
            {
                m_root = Root.TemporaryCache;
                m_leaf = path.Replace(Application.temporaryCachePath, "").TrimStart('/');
            }
            else
            {
                var cur = System.IO.Directory.GetCurrentDirectory().Replace("\\", "/");
                if (path.Contains(cur))
                {
                    m_root = Root.Current;
                    m_leaf = path.Replace(cur, "").TrimStart('/');
                }
                else
                {
                    m_root = Root.Absolute;
                    m_leaf = path;
                }
            }
        }

        public string GetFullPath()
        {
            if (m_root == Root.Absolute)
            {
                return m_leaf;
            }
            if (m_root == Root.Current)
            {
                return m_leaf.Length == 0 ? "." : "./" + m_leaf;
            }

            string ret = "";
            switch (m_root)
            {
                case Root.PersistentData:
                    ret = Application.persistentDataPath;
                    break;
                case Root.StreamingAssets:
                    ret = Application.streamingAssetsPath;
                    break;
                case Root.TemporaryCache:
                    ret = Application.temporaryCachePath;
                    break;
                case Root.DataPath:
                    ret = Application.dataPath;
                    break;
            }

            if (!m_leaf.StartsWith("/"))
            {
                ret += "/";
            }
            ret += m_leaf;
            return ret;
        }

        public void CreateDirectory()
        {
            try
            {
                var path = GetFullPath();
                if(path.Length > 0)
                {
                    System.IO.Directory.CreateDirectory(path);
                }
            }
            catch(Exception)
            {
            }
        }
    }
}