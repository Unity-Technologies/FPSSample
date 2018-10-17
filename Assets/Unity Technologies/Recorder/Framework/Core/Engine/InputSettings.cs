using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Recorder
{
    /// <summary>
    /// What is this: 
    /// Motivation  : 
    /// Notes: 
    /// </summary>    
    public abstract class  RecorderInputSetting : ScriptableObject
    {
        public abstract Type inputType { get; }
        public abstract bool ValidityCheck(List<string> errors);
        public string m_DisplayName;
        public string m_Id;

        protected virtual void OnEnable()
        {
            if (string.IsNullOrEmpty(m_Id))
                m_Id = Guid.NewGuid().ToString();
        }


        public bool storeInScene
        {
            get { return Attribute.GetCustomAttribute(GetType(), typeof(StoreInSceneAttribute)) != null; }
        }
    }

}
