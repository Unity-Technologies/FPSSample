using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    #pragma warning disable 0659
    abstract class VFXValue : VFXExpression
    {
        public enum Mode
        {
            Variable, // Variable that should never be folded
            FoldableVariable, // Variable that can be folded
            Constant, // Immutable value
        }

        // Syntactic sugar method to create a constant value

        static public VFXValue<Texture> Constant(Texture2D value)
        {
            return new VFXTexture2DValue(value, Mode.Constant);
        }

        static public VFXValue<Texture> Constant(Texture3D value)
        {
            return new VFXTexture3DValue(value, Mode.Constant);
        }

        static public VFXValue<Texture> Constant(Cubemap value)
        {
            return new VFXTextureCubeValue(value, Mode.Constant);
        }

        static public VFXValue<Texture> Constant(Texture2DArray value)
        {
            return new VFXTexture2DArrayValue(value, Mode.Constant);
        }

        static public VFXValue<Texture> Constant(CubemapArray value)
        {
            return new VFXTextureCubeArrayValue(value, Mode.Constant);
        }

        static public VFXValue<T> Constant<T>(T value = default(T))
        {
            return new VFXValue<T>(value, Mode.Constant);
        }

        private static Flags GetFlagsFromMode(Mode mode, Flags flags)
        {
            flags |= Flags.Value;
            if (mode != Mode.Variable)
            {
                flags |= Flags.Foldable;
                if (mode == Mode.Constant)
                    flags |= Flags.Constant;
            }
            return flags;
        }

        protected VFXValue(Mode mode, Flags flags)
            : base(GetFlagsFromMode(mode, flags))
        {
            m_Mode = mode;
        }

        public Mode ValueMode { get { return m_Mode; } }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.Value; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            if (m_Mode == Mode.Constant)
                return this;

            return CopyExpression(Mode.Constant);
        }

        public override string GetCodeString(string[] parents)
        {
            if (Is(Flags.InvalidOnGPU) || !Is(Flags.Constant))
                throw new InvalidOperationException(string.Format("Type {0} is either not valid on GPU or expression is not constant", valueType));

            return VFXShaderWriter.GetValueString(valueType, GetContent());
        }

        abstract public VFXValue CopyExpression(Mode mode);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (m_Mode == Mode.Constant)
            {
                var val = obj as VFXValue;
                if (val == null)
                    return false;

                if (val.m_Mode != Mode.Constant)
                    return false;

                if (valueType != val.valueType)
                    return false;

                var content = GetContent();
                var otherContent = val.GetContent();

                if (content == null)
                    return otherContent == null;

                return content.Equals(otherContent);
            }
            else
                return false;
        }

        protected override int GetInnerHashCode()
        {
            if (m_Mode == Mode.Constant)
            {
                int hashCode = valueType.GetHashCode();
                var content = GetContent();

                if (content != null)
                    hashCode ^= content.GetHashCode();

                return hashCode;
            }

            return RuntimeHelpers.GetHashCode(this);
        }

        public abstract void SetContent(object value);

        private Mode m_Mode;
    }

    class VFXValue<T> : VFXValue
    {
        private static Flags GetFlagsFromType(VFXValueType valueType)
        {
            var flags = Flags.None;
            if (!IsTypeValidOnGPU(valueType))
                flags |= VFXExpression.Flags.InvalidOnGPU;
            return flags;
        }

        public VFXValue(T content = default(T), Mode mode = Mode.FoldableVariable) : base(mode, GetFlagsFromType(ToValueType()))
        {
            m_Content = content;
        }

        public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXValue<T>(m_Content, mode);
            return copy;
        }

        private static readonly VFXValue s_Default = new VFXValue<T>(default(T), VFXValue.Mode.Constant);
        public static VFXValue Default { get { return s_Default; } }

        public T Get()
        {
            return m_Content;
        }

        public override object GetContent()
        {
            return Get();
        }

        public override void SetContent(object value)
        {
            if (ValueMode == Mode.Constant)
                throw new InvalidOperationException("Cannot set content of an immutable value");

            m_Content = default(T);

            if (value == null || (value is Object && !(value as Object))) //if object is a deleted asset, value == null will be false, but cast to Object will return null
            {
                return;
            }

            var fromType = value.GetType();
            var toType = typeof(T);

            if (typeof(Texture).IsAssignableFrom(toType) && toType.IsAssignableFrom(fromType))
            {
                m_Content = (T)value;
            }
            else if (fromType == toType || toType.IsAssignableFrom(fromType))
            {
                m_Content = (T)Convert.ChangeType(value, toType);
            }
            else
            {
                var implicitMethod = fromType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == "op_Implicit" && m.ReturnType == toType);
                if (implicitMethod != null)
                {
                    m_Content = (T)implicitMethod.Invoke(null, new object[] { value });
                }
                else
                {
                    Debug.LogErrorFormat("Cannot cast from {0} to {1}", fromType, toType);
                }
            }
        }

        private T m_Content;

        private static VFXValueType ToValueType()
        {
            Type t = typeof(T);
            if (typeof(Texture).IsAssignableFrom(t))
            {
                return VFXValueType.None;
            }
            var valueType = GetVFXValueTypeFromType(t);
            if (valueType == VFXValueType.None)
                throw new ArgumentException("Invalid type");
            return valueType;
        }

        static private readonly VFXValueType s_ValueType = ToValueType();
        protected override int[] additionnalOperands
        {
            get
            {
                return new int[] { (int)s_ValueType };
            }
        }
    }

#pragma warning restore 0659
}
