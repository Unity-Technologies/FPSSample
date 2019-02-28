#if false
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Collections.Generic;
using System.Linq;


namespace UnityEditor.VFX.UIElements
{
    class ObjectDropper : Manipulator
    {
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<IMGUIEvent>(OnIMGUIEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<IMGUIEvent>(OnIMGUIEvent);
        }

        protected void OnIMGUIEvent(IMGUIEvent e)
        {
            Event evt = e.imguiEvent;


            VFXObjectField target = this.target as VFXObjectField;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                {
                    var matchingObjects = DragAndDrop.objectReferences.Where(t => target.editedType.IsAssignableFrom(t.GetType())).ToArray();

                    if (matchingObjects.Length > 0)
                    {
                        //target.AddToClassList("droppable");
                        DragAndDrop.visualMode = evt.control ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
                        e.StopPropagation();
                    }

                    break;
                }

                case EventType.DragExited:
                {
                    //target.RemoveFromClassList("droppable");
                    break;
                }

                case EventType.DragPerform:
                {
                    //target.RemoveFromClassList("droppable");
                    var matchingObjects = DragAndDrop.objectReferences.Where(t => target.editedType.IsAssignableFrom(t.GetType())).ToArray();

                    if (matchingObjects.Length > 0)
                    {
                        target.ValueChanged(matchingObjects[0]);
                        e.StopPropagation();
                    }
                    break;
                }
            }
        }
    }
    class VFXObjectField : ValueControl<Object>
    {
        VisualElement m_IconContainer;
        Label m_NameContainer;
        VisualElement m_SelectContainer;

        class Receiver : ObjectSelectorReceiver
        {
            public VFXObjectField m_ObjectField;


            public override void OnSelectionChanged(Object selection)
            {
                m_ObjectField.ValueChanged(selection);
            }

            public override void OnSelectionClosed(Object selection)
            {
                ObjectSelector.get.objectSelectorReceiver = null;
            }
        }


        Receiver m_Reciever;


        public System.Type editedType { get; set; }


        void OnShowObjects()
        {
            ObjectSelector.get.Show(GetValue(), editedType, null, false);
            ObjectSelector.get.objectSelectorReceiver = m_Reciever;
        }

        void OnSelect()
        {
            panel.focusController.SwitchFocus(this);

            Object value = GetValue();

            Selection.activeObject = value;
            EditorGUIUtility.PingObject(value);
        }

        public VFXObjectField(string label) : base(label)
        {
            Setup();
        }

        public VFXObjectField(Label existingLabel) : base(existingLabel)
        {
            Setup();
        }

        public void ValueChanged(Object value)
        {
            SetValue(value);
            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        void Setup()
        {
            m_NameContainer = new Label();
            m_NameContainer.name = "name";

            m_IconContainer = new VisualElement();
            m_IconContainer.name = "icon";


            m_SelectContainer = new VisualElement();
            m_SelectContainer.name = "select";
            Add(m_IconContainer);
            Add(m_NameContainer);
            Add(m_SelectContainer);

            m_SelectContainer.AddManipulator(new Clickable(OnShowObjects));
            this.AddManipulator(new Clickable(OnSelect));
            this.AddManipulator(new ShortcutHandler(new Dictionary<Event, ShortcutDelegate>
            {
                { Event.KeyboardEvent("delete"), SetToNull },
                { Event.KeyboardEvent("backspace"), SetToNull }
            }));


            this.AddManipulator(new ObjectDropper());

            m_Reciever = Receiver.CreateInstance<Receiver>();
            m_Reciever.hideFlags = HideFlags.HideAndDontSave;
            m_Reciever.m_ObjectField = this;

            focusIndex = 0;
        }

        EventPropagation SetToNull()
        {
            ValueChanged(null);

            return EventPropagation.Stop;
        }

        protected override void ValueToGUI(bool force)
        {
            Object value = GetValue();
            var temp = EditorGUIUtility.ObjectContent(value, editedType);

            m_IconContainer.style.backgroundImage = temp.image as Texture2D;

            m_IconContainer.style.width = m_IconContainer.style.backgroundImage.value == null ? 0 : 18;
            m_NameContainer.text = value == null ? "null" : value.name;
        }

/*
        private void HandleDropEvent(IMGUIEvent evt, List<ISelectable> selection, IDropTarget dropTarget)
        {
            if (dropTarget == null)
                return;

            switch ((EventType)evt.imguiEvent.type)
            {
                case EventType.DragUpdated:
                    dropTarget.DragUpdated(evt, selection, dropTarget);
                    break;
                case EventType.DragExited:
                    dropTarget.DragExited();
                    break;
                case EventType.DragPerform:
                    dropTarget.DragPerform(evt, selection, dropTarget);
                    break;
            }
        }
        */
    }
}
#endif
