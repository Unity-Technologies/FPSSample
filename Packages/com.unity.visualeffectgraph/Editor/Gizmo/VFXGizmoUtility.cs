using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Linq;
using System.Reflection;
using Type = System.Type;
using Delegate = System.Delegate;

namespace UnityEditor.VFX.UI
{
    public static class VFXGizmoUtility
    {
        static Dictionary<System.Type, GizmoContext> s_DrawFunctions;

        internal class Property<T> : VFXGizmo.IProperty<T>
        {
            public Property(IPropertyRMProvider controller, bool editable)
            {
                m_Controller = controller;
                m_Editable = editable;
            }

            IPropertyRMProvider m_Controller;

            bool m_Editable;

            public bool isEditable
            {
                get { return m_Editable; }
            }
            public void SetValue(T value)
            {
                if (m_Editable)
                    m_Controller.value = value;
            }
        }

        internal class NullProperty<T> : VFXGizmo.IProperty<T>
        {
            public bool isEditable
            {
                get { return false; }
            }
            public void SetValue(T value)
            {
            }

            public static NullProperty<T> defaultProperty = new NullProperty<T>();
        }

        public abstract class Context : VFXGizmo.IContext
        {
            public abstract Type portType
            {
                get;
            }

            bool m_Prepared;

            public void Unprepare()
            {
                m_Prepared = false;
            }

            public bool Prepare()
            {
                if (m_Prepared)
                    return false;
                m_Prepared = true;
                m_Indeterminate = false;
                m_PropertyCache.Clear();
                InternalPrepare();
                return true;
            }

            protected abstract void InternalPrepare();

            public const string separator = ".";

            public abstract object value
            {
                get;
            }
            public abstract VFXCoordinateSpace space
            {
                get;
            }
            public virtual bool spaceLocalByDefault
            {
                get { return false; }
            }

            protected Dictionary<string, object> m_PropertyCache = new Dictionary<string, object>();

            public abstract VFXGizmo.IProperty<T> RegisterProperty<T>(string member);

            protected bool m_Indeterminate;

            public bool IsIndeterminate()
            {
                return m_Indeterminate;
            }
        }

        static VFXGizmoUtility()
        {
            s_DrawFunctions = new Dictionary<System.Type, GizmoContext>();

            foreach (Type type in typeof(VFXGizmoUtility).Assembly.GetTypes()) // TODO put all user assemblies instead
            {
                var attributes = type.GetCustomAttributes(false);
                if (attributes != null)
                {
                    var gizmoAttribute = attributes.OfType<VFXGizmoAttribute>().FirstOrDefault();
                    if (gizmoAttribute != null)
                    {
                        s_DrawFunctions[gizmoAttribute.type] = new GizmoContext() { gizmo = (VFXGizmo)System.Activator.CreateInstance(type) };
                    }
                }
            }
        }

        public static bool HasGizmo(Type type)
        {
            return s_DrawFunctions.ContainsKey(type);
        }

        static Type GetGizmoType(Type type)
        {
            if (type.IsAbstract)
                return null;
            Type baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && !baseType.IsGenericTypeDefinition && baseType.GetGenericTypeDefinition() == typeof(VFXGizmo<>))
                {
                    return baseType.GetGenericArguments()[0];
                }
                baseType = baseType.BaseType;
            }
            return null;
        }

        public static VFXGizmo CreateGizmoInstance(Context context)
        {
            GizmoContext gizmo;
            if (s_DrawFunctions.TryGetValue(context.portType, out gizmo))
            {
                return (VFXGizmo)System.Activator.CreateInstance(gizmo.gizmo.GetType());
            }
            return null;
        }

        struct GizmoContext
        {
            public Context lastContext;
            public VFXGizmo gizmo;
        }

        static internal void Draw(Context context, VisualEffect component)
        {
            GizmoContext gizmo;
            if (s_DrawFunctions.TryGetValue(context.portType, out gizmo))
            {
                bool forceRegister = false;
                if (gizmo.lastContext != context)
                {
                    forceRegister = true;
                    s_DrawFunctions[context.portType] = new GizmoContext() { gizmo = gizmo.gizmo, lastContext = context };
                }
                Draw(context, component, gizmo.gizmo, forceRegister);
            }
        }

        static internal bool NeedsComponent(Context context)
        {
            GizmoContext gizmo;
            if (s_DrawFunctions.TryGetValue(context.portType, out gizmo))
            {
                gizmo.gizmo.currentSpace = context.space;
                gizmo.gizmo.spaceLocalByDefault = context.spaceLocalByDefault;
                return gizmo.gizmo.needsComponent;
            }
            return false;
        }

        static internal Bounds GetGizmoBounds(Context context, VisualEffect component)
        {
            GizmoContext gizmo;
            if (s_DrawFunctions.TryGetValue(context.portType, out gizmo))
            {
                bool forceRegister = false;
                if (gizmo.lastContext != context)
                {
                    forceRegister = true;
                    s_DrawFunctions[context.portType] = new GizmoContext() { gizmo = gizmo.gizmo, lastContext = context };
                }
                return GetGizmoBounds(context, component, gizmo.gizmo, forceRegister);
            }

            return new Bounds();
        }

        static internal Bounds GetGizmoBounds(Context context, VisualEffect component, VFXGizmo gizmo, bool forceRegister = false)
        {
            if (context.Prepare() || forceRegister)
            {
                gizmo.RegisterEditableMembers(context);
            }
            if (!context.IsIndeterminate())
            {
                gizmo.component = component;
                gizmo.currentSpace = context.space;
                gizmo.spaceLocalByDefault = context.spaceLocalByDefault;
                Bounds bounds = gizmo.CallGetGizmoBounds(context.value);
                gizmo.component = null;

                return bounds;
            }

            return new Bounds();
        }

        static internal void Draw(Context context, VisualEffect component, VFXGizmo gizmo, bool forceRegister = false)
        {
            if (context.Prepare() || forceRegister)
            {
                gizmo.RegisterEditableMembers(context);
            }
            if (!context.IsIndeterminate())
            {
                gizmo.component = component;
                gizmo.currentSpace = context.space;
                gizmo.spaceLocalByDefault = context.spaceLocalByDefault;

                gizmo.CallDrawGizmo(context.value);
                gizmo.component = null;
            }
        }
    }
}
