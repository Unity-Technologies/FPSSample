using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Profiling;

namespace UnityEditor.VFX.UI
{
    class ControllerEvent
    {
        public IControlledElement target = null;
    }
    abstract class Controller
    {
        public bool m_DisableCalled = false;
        public virtual void OnDisable()
        {
            if (m_DisableCalled)
                Debug.LogError(GetType().Name + ".Disable called twice");

            m_DisableCalled = true;
            foreach (var element in allChildren)
            {
                Profiler.BeginSample(element.GetType().Name + ".OnDisable");
                element.OnDisable();
                Profiler.EndSample();
            }
        }

        public void RegisterHandler(IControlledElement handler)
        {
            //Debug.Log("RegisterHandler  of " + handler.GetType().Name + " on " + GetType().Name );

            if (m_EventHandlers.Contains(handler))
                Debug.LogError("Handler registered twice");
            else
            {
                m_EventHandlers.Add(handler);

                NotifyEventHandler(handler, AnyThing);
            }
        }

        public void UnregisterHandler(IControlledElement handler)
        {
            m_EventHandlers.Remove(handler);
        }

        public const int AnyThing = -1;

        protected void NotifyChange(int eventID)
        {
            var eventHandlers = m_EventHandlers.ToArray(); // Some notification may trigger Register/Unregister so duplicate the collection.

            foreach (var eventHandler in eventHandlers)
            {
                Profiler.BeginSample("NotifyChange:" + eventHandler.GetType().Name);
                NotifyEventHandler(eventHandler, eventID);
                Profiler.EndSample();
            }
        }

        void NotifyEventHandler(IControlledElement eventHandler, int eventID)
        {
            ControllerChangedEvent e = new ControllerChangedEvent();
            e.controller = this;
            e.target = eventHandler;
            e.change = eventID;
            eventHandler.OnControllerChanged(ref e);
            if (e.isPropagationStopped)
                return;
            if (eventHandler is VisualElement)
            {
                var element = eventHandler as VisualElement;
                eventHandler = element.GetFirstAncestorOfType<IControlledElement>();
                while (eventHandler != null)
                {
                    eventHandler.OnControllerChanged(ref e);
                    if (e.isPropagationStopped)
                        break;
                    eventHandler = (eventHandler as VisualElement).GetFirstAncestorOfType<IControlledElement>();
                }
            }
        }

        public void SendEvent(ControllerEvent e)
        {
            var eventHandlers = m_EventHandlers.ToArray(); // Some notification may trigger Register/Unregister so duplicate the collection.

            foreach (var eventHandler in eventHandlers.OfType<IControllerListener>())
            {
                eventHandler.OnControllerEvent(e);
            }
        }

        public abstract void ApplyChanges();


        public virtual  IEnumerable<Controller> allChildren
        {
            get { return Enumerable.Empty<Controller>(); }
        }

        List<IControlledElement> m_EventHandlers = new List<IControlledElement>();
    }

    abstract class Controller<T> : Controller where T : UnityEngine.Object
    {
        T m_Model;


        public Controller(T model)
        {
            m_Model = model;
        }

        protected abstract void ModelChanged(UnityEngine.Object obj);

        public override void ApplyChanges()
        {
            ModelChanged(model);

            foreach (var controller in allChildren)
            {
                controller.ApplyChanges();
            }
        }

        public T model { get { return m_Model; } }
    }

    abstract class VFXController<T> : Controller<T> where T : VFXModel
    {
        VFXViewController m_ViewController;

        public VFXController(VFXViewController viewController, T model) : base(model)
        {
            m_ViewController = viewController;
            m_ViewController.RegisterNotification(model, OnModelChanged);
        }

        public VFXViewController viewController {get {return m_ViewController; }}

        public override void OnDisable()
        {
            m_ViewController.UnRegisterNotification(model, OnModelChanged);
            base.OnDisable();
        }

        void OnModelChanged()
        {
            ModelChanged(model);
        }

        public virtual string name
        {
            get
            {
                return model.name;
            }
        }
    }

    struct ControllerChangedEvent
    {
        public IControlledElement target;
        public Controller controller;
        public int change;

        bool m_PropagationStopped;
        public void StopPropagation()
        {
            m_PropagationStopped = true;
        }

        public bool isPropagationStopped
        { get { return m_PropagationStopped; } }
    }
}
