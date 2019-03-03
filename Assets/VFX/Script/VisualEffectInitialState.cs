using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.VFX.Utils
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(VisualEffect))]
    public class VisualEffectInitialState : MonoBehaviour
    {
        public enum DefaultState
        {
            Play = 0,
            Stop = 1,
            CustomEvent = 2
        }

        public DefaultState defaultState = DefaultState.Play;
        public string customEventName = "CustomEvent";

        private void Awake()
        {
            ProcessInitialState();
        }

        void ProcessInitialState()
        {
            var component = GetComponent<VisualEffect>();

            component.Reinit();

            switch (defaultState)
            {
                case DefaultState.Play:
                    component.Play();
//                    Debug.Log("Play");
                    break;
                case DefaultState.Stop:
                    component.Stop();
//                    Debug.Log("Stop");
                    break;
                case DefaultState.CustomEvent:
                    component.SendEvent(customEventName);
//                    Debug.Log(customEventName);
                    break;
            }
        }


        private void OnEnable()
        {
            ProcessInitialState();
        }
    }
}


