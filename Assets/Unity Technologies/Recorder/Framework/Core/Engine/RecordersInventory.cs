using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

namespace UnityEngine.Recorder
{
    public class RecorderInfo
    {
        public Type recorderType;
        public Type settingsClass;
        public Type settingsEditorClass;
        public string category;
        public string displayName;
    }


    /// <summary>
    /// What is this: 
    /// Motivation  : 
    /// Notes: 
    /// </summary>    

    // to be internal once inside unity code base
    public static class RecordersInventory
    {
        internal static SortedDictionary<string, RecorderInfo> m_Recorders { get; private set; }


        static IEnumerable<KeyValuePair<Type, object[]>> FindRecorders()
        {
            var attribType = typeof(RecorderAttribute);
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types = null;
                try
                {
                    types = a.GetTypes();
                }
                catch (Exception)
                {
                    Debug.LogError( "Failed reflecting assembly: " + a.FullName );
                    continue;
                }

                foreach (var t in types)
                {
                    var attributes = t.GetCustomAttributes(attribType, false);
                    if (attributes.Length != 0)
                        yield return new KeyValuePair<Type, object[]>(t, attributes);
                }
            }
        }

        static void Init()
        {
#if UNITY_EDITOR
            if (m_Recorders != null)
                return;

            m_Recorders = new SortedDictionary<string, RecorderInfo>();
            foreach (var recorder in FindRecorders() )
                AddRecorder(recorder.Key);
#endif
        }

#if UNITY_EDITOR
        static SortedDictionary<string, List<RecorderInfo>> m_RecordersByCategory;

        public static SortedDictionary<string, List<RecorderInfo>> recordersByCategory
        {
            get
            {
                Init();
                return m_RecordersByCategory;
            }
        }

        static string[] m_AvailableCategories;
        public static string[] availableCategories
        {
            get
            {
                if (m_AvailableCategories == null)
                {
                    m_AvailableCategories = RecordersInventory.ListRecorders()
                        .GroupBy(x => x.category)
                        .Select(x => x.Key)
                        .OrderBy(x => x)
                        .ToArray();
                }
                return m_AvailableCategories;
            }
        }
#endif

        static bool AddRecorder(Type recorderType)
        {
            var recorderAttribs = recorderType.GetCustomAttributes(typeof(RecorderAttribute), false);
            if (recorderAttribs.Length == 1)
            {
                var recorderAttrib = recorderAttribs[0] as RecorderAttribute;
            
                if (m_Recorders == null)
                    m_Recorders = new SortedDictionary<string, RecorderInfo>();

                var info = new RecorderInfo()
                {
                    recorderType = recorderType,
                    settingsClass = recorderAttrib.settings,
                    category = recorderAttrib.category,
                    displayName = recorderAttrib.displayName
                };

                m_Recorders.Add(info.recorderType.FullName, info);

#if UNITY_EDITOR
                if (m_RecordersByCategory == null)
                    m_RecordersByCategory = new SortedDictionary<string, List<RecorderInfo>>();

                if (!m_RecordersByCategory.ContainsKey(info.category))
                    m_RecordersByCategory.Add(info.category, new List<RecorderInfo>());

                m_RecordersByCategory[info.category].Add(info);


                // Find associated editor to recorder's settings type.

                 

#endif
                return true;
            }
            else
            {
                Debug.LogError(String.Format("The class '{0}' does not have a FrameRecorderAttribute attached to it. ", recorderType.FullName));
            }

            return false;
        }

        public static RecorderInfo GetRecorderInfo<TRecorder>() where TRecorder : class
        {
            return GetRecorderInfo(typeof(TRecorder));
        }

        public static RecorderInfo GetRecorderInfo(Type recorderType)
        {
            Init();
            if (m_Recorders.ContainsKey(recorderType.FullName))
                return m_Recorders[recorderType.FullName];

#if UNITY_EDITOR
            return null;
#else
            if (AddRecorder(recorderType))
                return m_Recorders[recorderType.FullName];
            else
                return null;
#endif
        }

        public static IEnumerable<RecorderInfo> ListRecorders()
        {
            Init();

            foreach (var recorderInfo in m_Recorders)
            {
                yield return recorderInfo.Value;
            }
        }

        public static Recorder GenerateNewRecorder(Type recorderType, RecorderSettings settings)
        {
            Init();
            var factory = GetRecorderInfo(recorderType);
            if (factory != null)
            {
                var recorder = ScriptableObject.CreateInstance(recorderType) as Recorder;
                recorder.Reset();
                recorder.settings = settings;
                return recorder;
            }
            else
                throw new ArgumentException("No factory was registered for " + recorderType.Name);
        }

#if UNITY_EDITOR
        public static RecorderSettings GenerateRecorderInitialSettings(UnityEngine.Object parent, Type recorderType)
        {
            Init();
            var recorderinfo = GetRecorderInfo(recorderType);
            if (recorderinfo != null)
            {
                RecorderSettings settings = null;
                settings = ScriptableObject.CreateInstance(recorderinfo.settingsClass) as RecorderSettings;
                settings.name = "Recorder Settings";
                settings.recorderType = recorderType;

                AssetDatabase.AddObjectToAsset(settings, parent);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                settings.assetID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(settings));
                settings.inputsSettings.AddRange( settings.GetDefaultInputSettings() );

                return settings;
            }
            else
                throw new ArgumentException("No factory was registered for " + recorderType.Name);            
        }
#endif

    }
}
