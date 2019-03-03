using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;
using UnityEngine.Profiling;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.UI
{
    interface IPropertyRMProvider
    {
        bool expanded { get; }
        bool expandable { get; }
        object value { get; set; }
        bool spaceableAndMasterOfSpace { get; }
        VFXCoordinateSpace space { get; set; }
        bool IsSpaceInherited();
        string name { get; }
        VFXPropertyAttribute[] attributes { get; }
        object[] customAttributes { get; }
        Type portType { get; }
        int depth {get; }
        bool editable { get; }
        void RetractPath();
        void ExpandPath();
    }


    class SimplePropertyRMProvider<T> : IPropertyRMProvider
    {
        System.Func<T> m_Getter;
        System.Action<T> m_Setter;
        string m_Name;

        public SimplePropertyRMProvider(string name, System.Func<T> getter, System.Action<T> setter)
        {
            m_Getter = getter;
            m_Setter = setter;
            m_Name = name;
        }

        VFXCoordinateSpace IPropertyRMProvider.space { get { return VFXCoordinateSpace.Local; } set {} }

        bool IPropertyRMProvider.IsSpaceInherited() { return false; }

        bool IPropertyRMProvider.spaceableAndMasterOfSpace { get { return false; } }

        bool IPropertyRMProvider.expanded { get { return false; } }
        bool IPropertyRMProvider.expandable { get { return false; } }
        object IPropertyRMProvider.value
        {
            get
            {
                return m_Getter();
            }

            set
            {
                m_Setter((T)value);
            }
        }
        string IPropertyRMProvider.name
        {
            get { return m_Name; }
        }
        VFXPropertyAttribute[] IPropertyRMProvider.attributes { get { return new VFXPropertyAttribute[0]; } }
        object[] IPropertyRMProvider.customAttributes { get { return null; } }
        Type IPropertyRMProvider.portType
        {
            get
            {
                return typeof(T);
            }
        }
        int IPropertyRMProvider.depth { get { return 0; } }
        bool IPropertyRMProvider.editable { get { return true; } }
        void IPropertyRMProvider.RetractPath()
        {}
        void IPropertyRMProvider.ExpandPath()
        {}
    }

    abstract class PropertyRM : VisualElement
    {
        public abstract void SetValue(object obj);
        public abstract object GetValue();
        public virtual void SetMultiplier(object obj) {}

        public VisualElement m_Icon;
        Clickable m_IconClickable;

        static Texture2D[] m_IconStates;

        public Label m_Label;


        public bool m_PropertyEnabled;

        public bool propertyEnabled
        {
            get { return m_PropertyEnabled; }

            set
            {
                m_PropertyEnabled = value;
                UpdateEnabled();
            }
        }
        public bool m_Indeterminate;

        public bool indeterminate
        {
            get { return m_Indeterminate; }

            set
            {
                m_Indeterminate = value;
                UpdateIndeterminate();
            }
        }
        public bool isDelayed { get; set; }

        protected bool hasChangeDelayed { get; set; }


        public virtual bool IsCompatible(IPropertyRMProvider provider)
        {
            return GetType() == GetPropertyType(provider);
        }

        public const float depthOffset = 8;

        public virtual float GetPreferredLabelWidth()
        {
            if (m_Label.panel == null) return 40;

            VisualElement element = this;
            while (element != null && element.style.font.value == null)
            {
                element = element.parent;
            }
            if (element != null)
            {
                m_Label.style.font = element.style.font;
                return m_Label.MeasureTextSize(m_Label.text, -1, MeasureMode.Undefined, m_Label.style.height, MeasureMode.Exactly).x + m_Provider.depth * depthOffset;
            }
            return 40 + m_Provider.depth * depthOffset;
        }

        public abstract float GetPreferredControlWidth();

        public void SetLabelWidth(float label)
        {
            m_labelWidth = label;
            m_Label.style.width = effectiveLabelWidth - m_Provider.depth * depthOffset;
        }

        protected abstract void UpdateEnabled();

        protected abstract void UpdateIndeterminate();


        public void ForceUpdate()
        {
            SetValue(m_Provider.value);
            UpdateGUI(true);
        }

        public abstract void UpdateGUI(bool force);


        public void UpdateValue()
        {
            object value = m_Provider.value;
            SetValue(value);
        }

        public void Update()
        {
            Profiler.BeginSample("PropertyRM.Update");

            Profiler.BeginSample("PropertyRM.Update:Angle");
            if (VFXPropertyAttribute.IsAngle(m_Provider.attributes))
                SetMultiplier(Mathf.PI / 180.0f);
            Profiler.EndSample();


            Profiler.BeginSample("PropertyRM.Update:GetValue:");
            object value = m_Provider.value;
            Profiler.EndSample();
            Profiler.BeginSample("PropertyRM.Update:Regex");

            if (value != null)
            {
                string regex = VFXPropertyAttribute.ApplyRegex(m_Provider.attributes, value);
                if (regex != null)
                    value = m_Provider.value = regex;
            }
            Profiler.EndSample();

            UpdateExpandable();

            Profiler.BeginSample("PropertyRM.Update:SetValue");

            SetValue(value);

            Profiler.EndSample();


            Profiler.BeginSample("PropertyRM.Update:Name");

            string text = ObjectNames.NicifyVariableName(m_Provider.name);
            string tooltip = null;
            VFXPropertyAttribute.ApplyToGUI(m_Provider.attributes, ref text, ref tooltip);
            m_Label.text = text;

            m_Label.tooltip = tooltip;
            Profiler.EndSample();
            Profiler.EndSample();
        }

        bool m_IconClickableAdded;

        void UpdateExpandable()
        {
            if (m_Provider.expandable)
            {
                if (!m_IconClickableAdded)
                {
                    m_Icon.AddManipulator(m_IconClickable);
                    m_IconClickableAdded = false;
                }
                if (m_Provider.expanded)
                {
                    AddToClassList("icon-expanded");
                }
                else
                {
                    RemoveFromClassList("icon-expanded");
                }
                    AddToClassList("icon-expandable");
            }
            else
            {
                if (m_IconClickableAdded)
                {
                    m_Icon.RemoveManipulator(m_IconClickable);
                    m_IconClickableAdded = false;
                }

                m_Icon.style.backgroundImage = null;
            }
        }

        public PropertyRM(IPropertyRMProvider provider, float labelWidth)
        {
            this.AddStyleSheetPathWithSkinVariant("VFXControls");
            this.AddStyleSheetPathWithSkinVariant("PropertyRM");

            m_Provider = provider;
            m_labelWidth = labelWidth;

            m_IconClickable = new Clickable(OnExpand);

            isDelayed = VFXPropertyAttribute.IsDelayed(m_Provider.attributes);

            if (VFXPropertyAttribute.IsAngle(provider.attributes))
                SetMultiplier(Mathf.PI / 180.0f);

            string labelText = provider.name;
            string labelTooltip = null;
            VFXPropertyAttribute.ApplyToGUI(provider.attributes, ref labelText, ref labelTooltip);
            m_Label = new Label() { name = "label", text = labelText };
            m_Label.tooltip = labelTooltip;

            if (provider.depth != 0)
            {
                for (int i = 0; i < provider.depth; ++i)
                {
                    VisualElement line = new VisualElement();
                    line.style.width = 1;
                    line.name = "line";
                    line.style.marginLeft =  depthOffset + (i == 0 ? -2 : 0);
                    line.style.marginRight = ((i == provider.depth - 1) ? 2 : 0);

                    Add(line);
                }
            }
            m_Icon = new VisualElement() { name = "icon" };
            Add(m_Icon);

            m_Label.style.width = effectiveLabelWidth - provider.depth * depthOffset;
            Add(m_Label);

            AddToClassList("propertyrm");


            RegisterCallback<MouseDownEvent>(OnCatchMouse);

            UpdateExpandable();
        }

        void OnCatchMouse(MouseDownEvent e)
        {
            var node = GetFirstAncestorOfType<VFXNodeUI>();
            if (node != null)
            {
                node.OnSelectionMouseDown(e);
            }
            e.StopPropagation();
        }

        protected float m_labelWidth = 100;

        public virtual float effectiveLabelWidth
        {
            get
            {
                return m_labelWidth;
            }
        }

        static readonly Dictionary<Type, Type> m_TypeDictionary =  new Dictionary<Type, Type>
        {
            {typeof(Vector), typeof(VectorPropertyRM)},
            {typeof(Position), typeof(PositionPropertyRM)},
            {typeof(DirectionType), typeof(DirectionPropertyRM)},
            {typeof(bool), typeof(BoolPropertyRM)},
            {typeof(float), typeof(FloatPropertyRM)},
            {typeof(int), typeof(IntPropertyRM)},
            {typeof(uint), typeof(UintPropertyRM)},
            {typeof(FlipBook), typeof(FlipBookPropertyRM)},
            {typeof(Vector2), typeof(Vector2PropertyRM)},
            {typeof(Vector3), typeof(Vector3PropertyRM)},
            {typeof(Vector4), typeof(Vector4PropertyRM)},
            {typeof(Matrix4x4), typeof(Matrix4x4PropertyRM)},
            {typeof(Color), typeof(ColorPropertyRM)},
            {typeof(Gradient), typeof(GradientPropertyRM)},
            {typeof(AnimationCurve), typeof(CurvePropertyRM)},
            {typeof(Object), typeof(ObjectPropertyRM)},
            {typeof(string), typeof(StringPropertyRM)}
        };

        static Type GetPropertyType(IPropertyRMProvider controller)
        {
            Type propertyType = null;
            Type type = controller.portType;

            if (type != null)
            {
                if (type.IsEnum)
                {
                    propertyType = typeof(EnumPropertyRM);
                }
                else if (controller.spaceableAndMasterOfSpace)
                {
                    if (!m_TypeDictionary.TryGetValue(type, out propertyType))
                    {
                        propertyType = typeof(SpaceablePropertyRM<object>);
                    }
                }
                else
                {
                    while (type != typeof(object) && type != null)
                    {
                        if (!m_TypeDictionary.TryGetValue(type, out propertyType))
                        {
                            /*foreach (var inter in type.GetInterfaces())
                            {
                                if (m_TypeDictionary.TryGetValue(inter, out propertyType))
                                {
                                    break;
                                }
                            }*/
                        }
                        if (propertyType != null)
                        {
                            break;
                        }
                        type = type.BaseType;
                    }
                }
            }
            if (propertyType == null)
            {
                propertyType = typeof(EmptyPropertyRM);
            }

            return propertyType;
        }

        public static PropertyRM Create(IPropertyRMProvider controller, float labelWidth)
        {
            Type propertyType = GetPropertyType(controller);


            Profiler.BeginSample(propertyType.Name + ".CreateInstance");
            PropertyRM result = System.Activator.CreateInstance(propertyType, new object[] { controller, labelWidth }) as PropertyRM;
            Profiler.EndSample();

            return result;
        }

        public virtual object FilterValue(object value)
        {
            return value;
        }

        protected void NotifyValueChanged()
        {
            object value = GetValue();
            value = FilterValue(value);
            m_Provider.value = value;
            hasChangeDelayed = false;
        }

        void OnExpand()
        {
            if (m_Provider.expanded)
            {
                m_Provider.RetractPath();
            }
            else
            {
                m_Provider.ExpandPath();
            }
        }

        protected IPropertyRMProvider m_Provider;

        public abstract bool showsEverything { get; }
    }

    abstract class PropertyRM<T> : PropertyRM
    {
        public PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {}
        public override void SetValue(object obj)
        {
            if (obj != null)
            {
                if (m_Provider.portType == typeof(Transform) && obj is Matrix4x4)
                {
                    // do nothing
                }
                else
                {
                    try
                    {
                        m_Value = (T)obj;
                    }
                    catch (System.Exception)
                    {
                        Debug.Log("Error Trying to convert" + (obj != null ? obj.GetType().Name : "null") + " to " +  typeof(T).Name);
                    }
                }
            }

            UpdateGUI(false);
        }

        public override object GetValue()
        {
            return m_Value;
        }

        protected T m_Value;
    }

    abstract class SimplePropertyRM<T> : PropertyRM<T>
    {
        public abstract ValueControl<T> CreateField();

        public SimplePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_Field = CreateField();
            m_Field.AddToClassList("fieldContainer");
            m_Field.OnValueChanged += OnValueChanged;
            Add(m_Field);

            //m_Field.SetEnabled(enabledSelf);
        }

        public void OnValueChanged()
        {
            T newValue = m_Field.GetValue();
            if (!newValue.Equals(m_Value))
            {
                m_Value = newValue;
                if (!isDelayed)
                    NotifyValueChanged();
                else
                    hasChangeDelayed = true;
            }
        }

        protected override void UpdateEnabled()
        {
            m_Field.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            m_Field.visible = !indeterminate;
        }

        protected ValueControl<T> m_Field;
        public override void UpdateGUI(bool force)
        {
            m_Field.SetValue(m_Value);
        }

        public override bool showsEverything { get { return true; } }

        public override void SetMultiplier(object obj)
        {
            try
            {
                m_Field.SetMultiplier((T)obj);
            }
            catch (System.Exception)
            {
            }
        }
    }


    abstract class SimpleUIPropertyRM<T, U> : PropertyRM<T>
    {
        public abstract INotifyValueChanged<U> CreateField();

        public SimpleUIPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_Field = CreateField();
            isDelayed = VFXPropertyAttribute.IsDelayed(m_Provider.attributes);

            VisualElement fieldElement = m_Field as VisualElement;
            fieldElement.AddToClassList("fieldContainer");
            fieldElement.RegisterCallback<ChangeEvent<U>>(OnValueChanged);
            Add(fieldElement);
        }

        public virtual T Convert(object value)
        {
            return (T)System.Convert.ChangeType(m_Field.value, typeof(T));
        }

        public void OnValueChanged(ChangeEvent<U> e)
        {
            try
            {
                T newValue = Convert(m_Field.value);
                if (!newValue.Equals(m_Value))
                {
                    m_Value = newValue;
                    if (!isDelayed)
                        NotifyValueChanged();
                    else
                        hasChangeDelayed = true;
                }
                else
                {
                    UpdateGUI(false);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Catching exception to not break graph in OnValueChanged" + ex.Message);
            }
        }

        protected override void UpdateEnabled()
        {
            (m_Field as VisualElement).SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            (m_Field as VisualElement).visible = !indeterminate;
        }

        INotifyValueChanged<U> m_Field;


        protected INotifyValueChanged<U> field
        {
            get { return m_Field; }
        }

        protected virtual bool HasFocus() { return false; }
        public override void UpdateGUI(bool force)
        {
            if (!HasFocus() || force)
            {
                try
                {
                    {
                        try
                        {
                            m_Field.SetValueWithoutNotify((U)System.Convert.ChangeType(m_Value, typeof(U)));
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError("Catching exception to not break graph in UpdateGUI" + ex.Message);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Catching exception to not break graph in UpdateGUI" + ex.Message);
                }
            }
        }

        public override bool showsEverything { get { return true; } }
    }

    abstract class SimpleVFXUIPropertyRM<T, U> : SimpleUIPropertyRM<U, U> where T : VFXControl<U>, new()
    {
        public SimpleVFXUIPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override INotifyValueChanged<U> CreateField()
        {
            var field = new VFXLabeledField<T, U>(m_Label);

            return field;
        }

        protected T fieldControl
        {
            get { return (base.field as VFXLabeledField<T, U>).control; }
        }
        protected override void UpdateIndeterminate()
        {
            fieldControl.indeterminate = indeterminate;
        }

        public override void UpdateGUI(bool force)
        {
            base.UpdateGUI(force);
            if (force)
                fieldControl.ForceUpdate();
        }
    }


    class EmptyPropertyRM : PropertyRM
    {
        public override float GetPreferredControlWidth()
        {
            return 0;
        }

        public override void SetValue(object obj)
        {
        }

        public override object GetValue()
        {
            return null;
        }

        protected override void UpdateEnabled()
        {
        }

        protected override void UpdateIndeterminate()
        {
        }

        public EmptyPropertyRM(IPropertyRMProvider provider, float labelWidth) : base(provider, labelWidth)
        {
        }

        public override void UpdateGUI(bool force)
        {
        }

        public override bool showsEverything { get { return true; } }
    }
}
