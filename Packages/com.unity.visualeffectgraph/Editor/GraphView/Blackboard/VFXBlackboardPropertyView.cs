using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;

using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.Text;
using UnityEditor.Graphs;
using UnityEditor.SceneManagement;

namespace  UnityEditor.VFX.UI
{
    class VFXBlackboardPropertyView : VisualElement, IControlledElement<VFXParameterController>
    {
        public VFXBlackboardPropertyView()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        public VFXBlackboardRow owner
        {
            get; set;
        }

        Controller IControlledElement.controller
        {
            get { return owner.controller; }
        }
        public VFXParameterController controller
        {
            get { return owner.controller; }
        }

        PropertyRM m_Property;
        PropertyRM m_MinProperty;
        PropertyRM m_MaxProperty;
        List<PropertyRM> m_SubProperties;
        StringPropertyRM m_TooltipProperty;

        IEnumerable<PropertyRM> allProperties
        {
            get
            {
                var result = Enumerable.Empty<PropertyRM>();

                if (m_ExposedProperty != null)
                    result = result.Concat(Enumerable.Repeat<PropertyRM>(m_ExposedProperty, 1));
                if (m_Property != null)
                    result = result.Concat(Enumerable.Repeat(m_Property, 1));
                if (m_SubProperties != null)
                    result = result.Concat(m_SubProperties);
                if (m_TooltipProperty != null)
                    result = result.Concat(Enumerable.Repeat<PropertyRM>(m_TooltipProperty, 1));
                if (m_RangeProperty != null)
                    result = result.Concat(Enumerable.Repeat<PropertyRM>(m_RangeProperty, 1));
                if (m_MinProperty != null)
                    result = result.Concat(Enumerable.Repeat(m_MinProperty, 1));
                if (m_MaxProperty != null)
                    result = result.Concat(Enumerable.Repeat(m_MaxProperty, 1));

                return result;
            }
        }


        void GetPreferedWidths(ref float labelWidth)
        {
            foreach (var port in allProperties)
            {
                float portLabelWidth = port.GetPreferredLabelWidth() + 5;

                if (labelWidth < portLabelWidth)
                {
                    labelWidth = portLabelWidth;
                }
            }
        }

        void ApplyWidths(float labelWidth)
        {
            foreach (var port in allProperties)
            {
                port.SetLabelWidth(labelWidth);
            }
        }

        void CreateSubProperties(ref int insertIndex, List<int> fieldPath)
        {
            var subControllers = controller.GetSubControllers(fieldPath);

            var subFieldPath = new List<int>();
            int cpt = 0;
            foreach (var subController in subControllers)
            {
                PropertyRM prop = PropertyRM.Create(subController, 85);
                if (prop != null)
                {
                    m_SubProperties.Add(prop);
                    Insert(insertIndex++, prop);
                }
                if (prop == null || !prop.showsEverything)
                {
                    subFieldPath.Clear();
                    subFieldPath.AddRange(fieldPath);
                    subFieldPath.Add(cpt);
                    CreateSubProperties(ref insertIndex, subFieldPath);
                }
                ++cpt;
            }
        }

        BoolPropertyRM m_RangeProperty;
        BoolPropertyRM m_ExposedProperty;

        IPropertyRMProvider m_RangeProvider;

        public new void Clear()
        {
            m_ExposedProperty = null;
            m_RangeProperty = null;
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e) {}

        public void SelfChange(int change)
        {
            if (change == VFXParameterController.ValueChanged)
            {
                foreach (var prop in allProperties)
                {
                    prop.Update();
                }
                return;
            }

            int insertIndex = 0;

            if (m_ExposedProperty == null)
            {
                m_ExposedProperty = new BoolPropertyRM(new SimplePropertyRMProvider<bool>("Exposed", () => controller.exposed, t => controller.exposed = t), 55);
                Insert(insertIndex++, m_ExposedProperty);
            }
            else
            {
                insertIndex++;
            }

            if (m_Property == null || !m_Property.IsCompatible(controller))
            {
                if (m_Property != null)
                {
                    m_Property.RemoveFromHierarchy();
                }
                m_Property = PropertyRM.Create(controller, 55);
                if (m_Property != null)
                {
                    Insert(insertIndex++, m_Property);

                    if (m_SubProperties != null)
                    {
                        foreach (var prop in m_SubProperties)
                        {
                            prop.RemoveFromHierarchy();
                        }
                    }
                    m_SubProperties = new List<PropertyRM>();
                    List<int> fieldpath = new List<int>();
                    if (!m_Property.showsEverything)
                    {
                        CreateSubProperties(ref insertIndex, fieldpath);
                    }
                    if (m_TooltipProperty == null)
                    {
                        m_TooltipProperty = new StringPropertyRM(new SimplePropertyRMProvider<string>("Tooltip", () => controller.model.tooltip, t => controller.model.tooltip = t), 55);
                    }
                    Insert(insertIndex++, m_TooltipProperty);
                }
                else
                {
                    m_TooltipProperty = null;
                }
            }
            else
            {
                insertIndex += 1 + m_SubProperties.Count + 1; //main property + subproperties + tooltip
            }

            if (controller.canHaveRange)
            {
                if (m_MinProperty == null || !m_MinProperty.IsCompatible(controller.minController))
                {
                    if (m_MinProperty != null)
                        m_MinProperty.RemoveFromHierarchy();
                    m_MinProperty = PropertyRM.Create(controller.minController, 55);
                }
                if (m_MaxProperty == null || !m_MaxProperty.IsCompatible(controller.minController))
                {
                    if (m_MaxProperty != null)
                        m_MaxProperty.RemoveFromHierarchy();
                    m_MaxProperty = PropertyRM.Create(controller.maxController, 55);
                }

                if (m_RangeProperty == null)
                {
                    m_RangeProperty = new BoolPropertyRM(new SimplePropertyRMProvider<bool>("Range", () => controller.hasRange, t => controller.hasRange = t), 55);
                }
                Insert(insertIndex++, m_RangeProperty);

                if (controller.hasRange)
                {
                    if (m_MinProperty.parent == null)
                    {
                        Insert(insertIndex++, m_MinProperty);
                        Insert(insertIndex++, m_MaxProperty);
                    }
                }
                else if (m_MinProperty.parent != null)
                {
                    m_MinProperty.RemoveFromHierarchy();
                    m_MaxProperty.RemoveFromHierarchy();
                }
            }
            else
            {
                if (m_MinProperty != null)
                {
                    m_MinProperty.RemoveFromHierarchy();
                    m_MinProperty = null;
                }
                if (m_MaxProperty != null)
                {
                    m_MaxProperty.RemoveFromHierarchy();
                    m_MaxProperty = null;
                }
                if (m_RangeProperty != null)
                {
                    m_RangeProperty.RemoveFromHierarchy();
                    m_RangeProperty = null;
                }
            }


            foreach (var prop in allProperties)
            {
                prop.Update();
            }
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            if (panel != null)
            {
                float labelWidth = 70;
                GetPreferedWidths(ref labelWidth);
                ApplyWidths(labelWidth);
            }
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
    }
}
