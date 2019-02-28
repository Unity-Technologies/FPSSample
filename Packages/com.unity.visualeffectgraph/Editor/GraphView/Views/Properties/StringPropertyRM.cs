using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;
using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;
using VFXVector2Field = UnityEditor.VFX.UIElements.VFXVector2Field;
using VFXVector4Field = UnityEditor.VFX.UIElements.VFXVector4Field;

namespace UnityEditor.VFX
{
    interface IStringProvider
    {
        string[] GetAvailableString();
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class StringProviderAttribute : PropertyAttribute
    {
        public StringProviderAttribute(Type providerType)
        {
            if (!typeof(IStringProvider).IsAssignableFrom(providerType))
                throw new InvalidCastException("StringProviderAttribute excepts a type which implements interface IStringProvider : " + providerType);
            this.providerType = providerType;
        }

        public Type providerType { get; private set; }
    }

    interface IPushButtonBehavior
    {
        void OnClicked(string currentValue);
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class PushButtonAttribute : PropertyAttribute
    {
        public PushButtonAttribute(Type pushButtonProvider, string buttonName)
        {
            if (!typeof(IPushButtonBehavior).IsAssignableFrom(pushButtonProvider))
                throw new InvalidCastException("PushButtonAttribute excepts a type which implements interface IPushButtonBehavior : " + pushButtonProvider);
            this.pushButtonProvider = pushButtonProvider;
            this.buttonName = buttonName;
        }

        public Type pushButtonProvider { get; private set; }
        public string buttonName { get; private set; }
    }
}

namespace UnityEditor.VFX.UI
{
    class StringPropertyRM : SimplePropertyRM<string>
    {
        public StringPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 140;
        }

        public static Func<string[]> FindStringProvider(object[] customAttributes)
        {
            if (customAttributes != null)
            {
                foreach (var attribute in customAttributes)
                {
                    if (attribute is StringProviderAttribute)
                    {
                        var instance = Activator.CreateInstance((attribute as StringProviderAttribute).providerType);
                        var stringProvider = instance as IStringProvider;
                        return () => stringProvider.GetAvailableString();
                    }
                }
            }
            return null;
        }

        public struct StringPushButtonInfo
        {
            public Action<string> action;
            public string buttonName;
        }

        public static StringPushButtonInfo FindPushButtonBehavior(object[] customAttributes)
        {
            if (customAttributes != null)
            {
                foreach (var attribute in customAttributes)
                {
                    if (attribute is PushButtonAttribute)
                    {
                        var instance = Activator.CreateInstance((attribute as PushButtonAttribute).pushButtonProvider);
                        var pushButtonBehavior = instance as IPushButtonBehavior;
                        return new StringPushButtonInfo() {action = (a) => pushButtonBehavior.OnClicked(a), buttonName = (attribute as PushButtonAttribute).buttonName};
                    }
                }
            }
            return new StringPushButtonInfo();
        }

        VFXStringField m_StringField;
        VFXStringFieldPushButton m_StringFieldPushButton;

        VFXStringFieldProvider m_StringFieldProvider;


        protected override void UpdateIndeterminate()
        {
            if (m_StringField != null)
            {
                m_StringField.indeterminate = indeterminate;
            }
        }

        public override ValueControl<string> CreateField()
        {
            var stringProvider = FindStringProvider(m_Provider.customAttributes);
            var pushButtonProvider = FindPushButtonBehavior(m_Provider.customAttributes);
            if (stringProvider != null)
            {
                m_StringFieldProvider = new VFXStringFieldProvider(m_Label, stringProvider);
                return m_StringFieldProvider;
            }
            else if (pushButtonProvider.action != null)
            {
                m_StringFieldPushButton = new VFXStringFieldPushButton(m_Label, pushButtonProvider.action, pushButtonProvider.buttonName);
                if (isDelayed)
                {
                    m_StringFieldPushButton.textfield.RegisterCallback<BlurEvent>(OnFocusLost);
                    m_StringFieldPushButton.textfield.RegisterCallback<KeyDownEvent>(OnKeyDown);
                }
                return m_StringFieldPushButton;
            }
            else
            {
                m_StringField = new VFXStringField(m_Label);
                if (isDelayed)
                {
                    m_StringField.textfield.RegisterCallback<BlurEvent>(OnFocusLost);
                    m_StringField.textfield.RegisterCallback<KeyDownEvent>(OnKeyDown);
                }
                return m_StringField;
            }
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.character == '\n')
            {
                if (isDelayed && hasChangeDelayed)
                {
                    NotifyValueChanged();
                }
                UpdateGUI(true);
            }
        }

        void OnFocusLost(BlurEvent e)
        {
            if (isDelayed && hasChangeDelayed)
            {
                NotifyValueChanged();
            }
            UpdateGUI(true);
        }

        public override bool IsCompatible(IPropertyRMProvider provider)
        {
            if (!base.IsCompatible(provider)) return false;

            var stringProvider = FindStringProvider(m_Provider.customAttributes);
            var pushButtonInfo = FindPushButtonBehavior(m_Provider.customAttributes);

            if (stringProvider != null)
            {
                return m_Field is VFXStringFieldProvider && (m_Field as VFXStringFieldProvider).stringProvider == stringProvider;
            }
            else if (pushButtonInfo.action != null)
            {
                return m_Field is VFXStringFieldPushButton && (m_Field as VFXStringFieldPushButton).pushButtonProvider == pushButtonInfo.action;
            }

            return !(m_Field is VFXStringFieldProvider) && !(m_Field is VFXStringFieldPushButton);
        }
    }
}
