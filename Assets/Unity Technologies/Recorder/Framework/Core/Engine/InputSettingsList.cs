using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEngine.Recorder
{
    [Serializable]
    public class InputSettingsList : IEnumerable<RecorderInputSetting>
    {
        [SerializeField]
        List<RecorderInputSetting> m_InputsSettingsAssets;
        List<RecorderInputSetting> m_InputsSettings;

        public string ownerRecorderSettingsAssetId;

        public void OnEnable( string ownerSettingsAssetId )
        {
            ownerRecorderSettingsAssetId = ownerSettingsAssetId;
            Reset();
        }

        public void Reset()
        {
            if(m_InputsSettingsAssets == null)
                m_InputsSettingsAssets = new List<RecorderInputSetting>();

            Rebuild();
        }

        public void Rebuild()
        {
            m_InputsSettings = new List<RecorderInputSetting>();

            foreach (var inputAsset in m_InputsSettingsAssets)
            {
                if (inputAsset is InputBinder)
                {
                    var sceneInputs = SceneHook.GetInputsComponent(ownerRecorderSettingsAssetId);
                    bool found = false;
                    foreach (var input in sceneInputs.m_Settings)
                    {
                        if (input.m_Id == inputAsset.m_Id)
                        {
                            m_InputsSettings.Add(input);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        var binder = inputAsset as InputBinder;
                        if( string.IsNullOrEmpty( binder.typeName) )
                            Debug.LogError("Recorder Input asset in invalid!");
                        else
                        {
                            if( Application.isPlaying )
                                Debug.LogError("Recorder input setting missing from scene, adding with default state.");
                            else if( Verbose.enabled )
                                Debug.Log("Recorder input setting missing from scene, adding with default state.");
                            var replacementInput = ScriptableObject.CreateInstance(binder.inputType) as RecorderInputSetting;
                            replacementInput.m_Id = inputAsset.m_Id;
                            m_InputsSettings.Add(replacementInput);
                        }
                    }
                }
                else
                    m_InputsSettings.Add(inputAsset);
            }            
        }

        public bool ValidityCheck( List<string> errors  )
        {
            foreach( var x in m_InputsSettings )
                if (!x.ValidityCheck(errors))
                    return false;
            return true;
        }

        public bool hasBrokenBindings
        {
            get
            {
                foreach( var x in m_InputsSettings.ToList() )
                    if (x == null || x is InputBinder)
                        return true;
                return false;
            }
        }

        public RecorderInputSetting this [int index]
        {
            get
            {
                return m_InputsSettings[index]; 
            }

            set
            {
                ReplaceAt(index, value);
            }
        }

        public IEnumerator<RecorderInputSetting> GetEnumerator()
        {
            return ((IEnumerable<RecorderInputSetting>)m_InputsSettings).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddRange(List<RecorderInputSetting> list)
        {
            foreach (var value in list)
                Add(value);
        }

        public void Add(RecorderInputSetting input)
        {
            m_InputsSettings.Add(null);
            m_InputsSettingsAssets.Add(null);
            ReplaceAt(m_InputsSettings.Count - 1, input);
        }

        public int Count
        {
            get
            {
                return m_InputsSettings.Count; 
            }
        }

        public void Rebind(RecorderInputSetting input)
        {
            if (input is InputBinder)
            {
                Debug.LogError("Cannot bind a InputBinder object!");
                return;
            }

            for (int i = 0; i < m_InputsSettings.Count; i++)
            {
                var x = m_InputsSettings[i];
                var ib = x as InputBinder;
                if ( ib != null && ib.m_Id == input.m_Id) 
                {
                    m_InputsSettings[i] = input;
                    return;
                }
            }
        }

        public void Remove(RecorderInputSetting input)
        {
            for (int i = 0; i < m_InputsSettings.Count; i++)
            {
                if (m_InputsSettings[i] == input)
                {
                    ReleaseAt(i);
                    m_InputsSettings.RemoveAt(i);
                    m_InputsSettingsAssets.RemoveAt(i);
                }
            }
        }

        public void ReplaceAt(int index, RecorderInputSetting input)
        {
            if (m_InputsSettingsAssets == null || m_InputsSettings.Count <= index)
                throw new ArgumentException("Index out of range");

            // Release input
            ReleaseAt(index);

            m_InputsSettings[index] = input;
            if (input.storeInScene)
            {
                var binder = ScriptableObject.CreateInstance<InputBinder>();
                binder.name = "Scene-Stored";
                binder.m_DisplayName = input.m_DisplayName;
                binder.typeName = input.GetType().AssemblyQualifiedName;
                binder.m_Id = input.m_Id;
                m_InputsSettingsAssets[index] = binder;
                SceneHook.RegisterInputSettingObj(ownerRecorderSettingsAssetId, input);

#if UNITY_EDITOR
                var assetPath = AssetDatabase.GUIDToAssetPath(ownerRecorderSettingsAssetId);
                AssetDatabase.AddObjectToAsset(binder, assetPath);
                AssetDatabase.SaveAssets();
#endif

            }
            else
            {
                m_InputsSettingsAssets[index] = input;
#if UNITY_EDITOR
                AssetDatabase.AddObjectToAsset(input, AssetDatabase.GUIDToAssetPath(ownerRecorderSettingsAssetId));
                AssetDatabase.SaveAssets();
#endif
            }
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif

        }

        void ReleaseAt(int index)
        {
#if UNITY_EDITOR
            bool isBinder = m_InputsSettingsAssets[index] is InputBinder;
            if ( isBinder ) 
                SceneHook.UnregisterInputSettingObj(ownerRecorderSettingsAssetId, m_InputsSettings[index]);
#endif
            UnityHelpers.Destroy(m_InputsSettingsAssets[index],true);
#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
#endif

            m_InputsSettings[index] = null;
            m_InputsSettingsAssets[index] = null;
        }

#if UNITY_EDITOR
        public void RepareMissingBindings()
        {
            for (int i = 0; i < m_InputsSettingsAssets.Count; i++)
            {
                var ib = m_InputsSettingsAssets[i] as InputBinder;
                if (ib != null && m_InputsSettings[i] == null)
                {
                    var newInput = ScriptableObject.CreateInstance(ib.inputType) as RecorderInputSetting;
                    newInput.m_DisplayName = ib.m_DisplayName;
                    newInput.m_Id = ib.m_Id;
                    m_InputsSettings[i] = newInput;
                    SceneHook.RegisterInputSettingObj(ownerRecorderSettingsAssetId, newInput);
                }
            }            
        }
#endif

        public void OnDestroy()
        {
            for (int i = 0; i < m_InputsSettingsAssets.Count; i++)
            {
                if (m_InputsSettingsAssets[i] is InputBinder)
                    SceneHook.UnregisterInputSettingObj(ownerRecorderSettingsAssetId, m_InputsSettings[i]);

                UnityHelpers.Destroy(m_InputsSettingsAssets[i], true);
            }
        }

    }
}