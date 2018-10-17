using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Recorder;

namespace UnityEditor.Experimental.Recorder.Input
{
    [Serializable]
    [StoreInScene]
    public class AnimationInputSettings : RecorderInputSetting
    {
        public GameObject gameObject;
        public bool enabled = false;
        public bool recursive = true;

        public bool fold = true;
                   
        [HideInInspector]
        public List<string> bindingTypeName = new List<string>();       
        public List<Type> bindingType
        {
            get
            {
                var ret = new List<Type>(bindingTypeName.Count);
                foreach (var t in bindingTypeName)
                {
                    ret.Add( Type.GetType(t));
                }
                return ret;
            }
        }

        public override Type inputType
        {
            get { return typeof(AnimationInput); }
        }

        public override bool ValidityCheck( List<string> errors )
        {
            var ok = true;
#if !UNITY_2018_2_OR_NEWER
            if (enabled)
            {

                if (gameObject != null
                    && bindingType.Count > 0
                    && bindingType.Any(x => typeof(MonoBehaviour).IsAssignableFrom(x) || typeof(ScriptableObject).IsAssignableFrom(x))
                )
                {
                    ok = false;
                    errors.Add("Monobehaviours and ScriptableObjects are not supported inputs.");
                }
            }
#endif
            return ok;
        }
    }
}
