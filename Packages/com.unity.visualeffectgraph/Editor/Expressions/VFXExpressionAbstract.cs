using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    public static class VFXReflectionHelper
    {
        public static T[] CollectStaticReadOnlyExpression<T>(Type expressionType, System.Reflection.BindingFlags additionnalFlag = System.Reflection.BindingFlags.Public)
        {
            var members = expressionType.GetFields(System.Reflection.BindingFlags.Static | additionnalFlag)
                .Where(m => m.IsInitOnly && m.FieldType == typeof(T))
                .ToArray();
            var expressions = members.Select(m => (T)m.GetValue(null)).ToArray();
            return expressions;
        }
    }

    abstract partial class VFXExpression
    {
        public struct Operands
        {
            public static readonly int OperandCount = 4;

            int data0;
            int data1;
            int data2;
            int data3;

            public Operands(int defaultValue)
            {
                data0 = defaultValue;
                data1 = defaultValue;
                data2 = defaultValue;
                data3 = defaultValue;
            }

            // This ugly code is for optimization purpose (no garbage created)
            public int this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return data0;
                        case 1: return data1;
                        case 2: return data2;
                        case 3: return data3;
                        default: throw new IndexOutOfRangeException();
                    }
                }
                set
                {
                    switch (index)
                    {
                        case 0: data0 = value; break;
                        case 1: data1 = value; break;
                        case 2: data2 = value; break;
                        case 3: data3 = value; break;
                        default: throw new IndexOutOfRangeException();
                    }
                }
            }

            public int[] ToArray()
            {
                return new int[] { data0, data1, data2, data3 };
            }
        }

        [Flags]
        public enum Flags
        {
            None =          0,
            Value =         1 << 0, // Expression is a value, get/set can be called on it
            Foldable =      1 << 1, // Expression is not a constant but can be folded anyway
            Constant =      1 << 2, // Expression is a constant, it can be folded
            InvalidOnGPU =  1 << 3, // Expression can be evaluated on GPU
            InvalidOnCPU =  1 << 4, // Expression can be evaluated on CPU
            PerElement =    1 << 5, // Expression is per element
            NotCompilableOnCPU = InvalidOnCPU | PerElement //Helper to filter out invalid expression on CPU
        }

        public static bool IsFloatValueType(VFXValueType valueType)
        {
            return valueType == VFXValueType.Float
                || valueType == VFXValueType.Float2
                || valueType == VFXValueType.Float3
                || valueType == VFXValueType.Float4;
        }

        public static bool IsUIntValueType(VFXValueType valueType)
        {
            return valueType == VFXValueType.Uint32;
        }

        public static bool IsIntValueType(VFXValueType valueType)
        {
            return valueType == VFXValueType.Int32;
        }

        public static bool IsBoolValueType(VFXValueType valueType)
        {
            return valueType == VFXValueType.Boolean;
        }

        public static int TypeToSize(VFXValueType type)
        {
            return VFXExpressionHelper.GetSizeOfType(type);
        }

        public static string TypeToCode(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.Float: return "float";
                case VFXValueType.Float2: return "float2";
                case VFXValueType.Float3: return "float3";
                case VFXValueType.Float4: return "float4";
                case VFXValueType.Int32: return "int";
                case VFXValueType.Uint32: return "uint";
                case VFXValueType.Texture2D: return "Texture2D";
                case VFXValueType.Texture2DArray: return "Texture2DArray";
                case VFXValueType.Texture3D: return "Texture3D";
                case VFXValueType.TextureCube: return "TextureCube";
                case VFXValueType.TextureCubeArray: return "TextureCubeArray";
                case VFXValueType.Matrix4x4: return "float4x4";
                case VFXValueType.Boolean: return "bool";
            }
            throw new NotImplementedException(type.ToString());
        }

        // As certain type of uniforms are not handled in material, we need to use floats instead
        public static string TypeToUniformCode(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.Float: return "float";
                case VFXValueType.Float2: return "float2";
                case VFXValueType.Float3: return "float3";
                case VFXValueType.Float4: return "float4";
                case VFXValueType.Int32: return "float";
                case VFXValueType.Uint32: return "float";
                case VFXValueType.Matrix4x4: return "float4x4";
                case VFXValueType.Boolean: return "float";
            }
            throw new NotImplementedException(type.ToString());
        }

        public static Type TypeToType(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.Float: return typeof(float);
                case VFXValueType.Float2: return typeof(Vector2);
                case VFXValueType.Float3: return typeof(Vector3);
                case VFXValueType.Float4: return typeof(Vector4);
                case VFXValueType.Int32: return typeof(int);
                case VFXValueType.Uint32: return typeof(uint);
                case VFXValueType.Texture2D: return typeof(Texture);
                case VFXValueType.Texture2DArray: return typeof(Texture);
                case VFXValueType.Texture3D: return typeof(Texture);
                case VFXValueType.TextureCube: return typeof(Texture);
                case VFXValueType.TextureCubeArray: return typeof(Texture);
                case VFXValueType.Matrix4x4: return typeof(Matrix4x4);
                case VFXValueType.Mesh: return typeof(Mesh);
                case VFXValueType.Curve: return typeof(AnimationCurve);
                case VFXValueType.ColorGradient: return typeof(Gradient);
                case VFXValueType.Boolean: return typeof(bool);
            }
            throw new NotImplementedException(type.ToString());
        }

        public static bool IsTypeValidOnGPU(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.Float:
                case VFXValueType.Float2:
                case VFXValueType.Float3:
                case VFXValueType.Float4:
                case VFXValueType.Int32:
                case VFXValueType.Uint32:
                case VFXValueType.Texture2D:
                case VFXValueType.Texture2DArray:
                case VFXValueType.Texture3D:
                case VFXValueType.TextureCube:
                case VFXValueType.TextureCubeArray:
                case VFXValueType.Matrix4x4:
                case VFXValueType.Boolean:
                    return true;
            }

            return false;
        }

        public static bool IsTexture(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.Texture2D:
                case VFXValueType.Texture2DArray:
                case VFXValueType.Texture3D:
                case VFXValueType.TextureCube:
                case VFXValueType.TextureCubeArray:
                    return true;
            }

            return false;
        }

        public static bool IsUniform(VFXValueType type)
        {
            switch (type)
            {
                case VFXValueType.Float:
                case VFXValueType.Float2:
                case VFXValueType.Float3:
                case VFXValueType.Float4:
                case VFXValueType.Int32:
                case VFXValueType.Uint32:
                case VFXValueType.Matrix4x4:
                case VFXValueType.Boolean:
                    return true;
            }
            return false;
        }

        public static Type GetMatchingScalar(Type type)
        {
            var vfxType = GetVFXValueTypeFromType(type);
            if (vfxType == VFXValueType.None)
            {
                var affinityFallback = VFXOperatorDynamicOperand.GetTypeAffinityList(type).GetEnumerator();
                while (affinityFallback.MoveNext() && vfxType == VFXValueType.None)
                {
                    vfxType = GetVFXValueTypeFromType(affinityFallback.Current);
                }
            }
            return TypeToType(GetMatchingScalar(vfxType));
        }

        public static VFXValueType GetMatchingScalar(VFXValueType type)
        {
            if (IsFloatValueType(type))
                return VFXValueType.Float;
            if (IsUIntValueType(type))
                return VFXValueType.Uint32;
            if (IsIntValueType(type))
                return VFXValueType.Int32;

            return VFXValueType.None;
        }

        public static VFXValueType GetVFXValueTypeFromType(Type type)
        {
            if (type == typeof(float)) return VFXValueType.Float;
            if (type == typeof(Vector2)) return VFXValueType.Float2;
            if (type == typeof(Vector3)) return VFXValueType.Float3;
            if (type == typeof(Vector4)) return VFXValueType.Float4;
            if (type == typeof(Color)) return VFXValueType.Float4;
            if (type == typeof(int)) return VFXValueType.Int32;
            if (type == typeof(uint)) return VFXValueType.Uint32;
            if (type == typeof(Texture2D)) return VFXValueType.Texture2D;
            if (type == typeof(Texture2DArray)) return VFXValueType.Texture2DArray;
            if (type == typeof(Texture3D)) return VFXValueType.Texture3D;
            if (type == typeof(Cubemap)) return VFXValueType.TextureCube;
            if (type == typeof(CubemapArray)) return VFXValueType.TextureCubeArray;
            if (type == typeof(Matrix4x4)) return VFXValueType.Matrix4x4;
            if (type == typeof(AnimationCurve)) return VFXValueType.Curve;
            if (type == typeof(Gradient)) return VFXValueType.ColorGradient;
            if (type == typeof(Mesh)) return VFXValueType.Mesh;
            if (type == typeof(List<Vector3>)) return VFXValueType.Spline;
            if (type == typeof(bool)) return VFXValueType.Boolean;
            return VFXValueType.None;
        }

        private static Dictionary<VFXExpression, VFXExpression> s_ExpressionCache = new Dictionary<VFXExpression, VFXExpression>();

        public static void ClearCache()
        {
            s_ExpressionCache.Clear();
        }

        //Ideally, we should use HashSet<T>.TryGetValue https://msdn.microsoft.com/en-us/library/mt829070(v=vs.110).aspx
        //but it's available only in 4.7, Dictionary<T, T> is a workaround, sensible same performance but there is a waste of memory
        private void SimplifyWithCacheParents()
        {
            for (int i = 0; i < m_Parents.Length; ++i)
            {
                VFXExpression parentEq;
                if (!s_ExpressionCache.TryGetValue(parents[i], out parentEq))
                {
                    s_ExpressionCache.Add(parents[i], parents[i]);
                }
                else
                {
                    m_Parents[i] = parentEq;
                }
            }
        }

        protected VFXExpression(Flags flags, params VFXExpression[] parents)
        {
            m_Parents = parents;
            SimplifyWithCacheParents();

            m_Flags = flags;
            PropagateParentsFlags();
        }

        // Only do that when constructing an instance if needed
        private void Initialize(Flags additionalFlags, VFXExpression[] parents)
        {
            m_Parents = parents;
            SimplifyWithCacheParents();

            m_Flags |= additionalFlags;
            PropagateParentsFlags();
            m_HashCodeCached = false; // as expression is mutated
        }

        //Helper using reflection to recreate a concrete type from an abstract class (useful with reduce behavior)
        private static VFXExpression CreateNewInstance(Type expressionType)
        {
            var allconstructors = expressionType.GetConstructors().ToArray();
            if (allconstructors.Length == 0)
                return null; //Only static readonly expression allowed, constructors are private (attribute or builtIn)

            var constructor =   allconstructors
                .OrderBy(o => o.GetParameters().Count())                 //promote simplest (or default) constructors
                .First();
            var param = constructor.GetParameters().Select(o =>
            {
                var type = o.GetType();
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }).ToArray();
            return (VFXExpression)constructor.Invoke(param);
        }

        private VFXExpression CreateNewInstance()
        {
            return CreateNewInstance(GetType());
        }

        // Reduce the expression
        protected virtual VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            if (reducedParents.Length == 0)
                return this;

            var reduced = CreateNewInstance();
            reduced.Initialize(m_Flags, reducedParents);
            return reduced;
        }

        // Evaluate the expression
        protected virtual VFXExpression Evaluate(VFXExpression[] constParents)
        {
            throw new NotImplementedException();
        }

        // Get the HLSL code snippet
        public virtual string GetCodeString(string[] parents)
        {
            throw new NotImplementedException(GetType().ToString());
        }

        // Get the operands for the runtime evaluation
        public Operands GetOperands(VFXExpressionGraph graph)
        {
            var addOperands = additionnalOperands;
            if (parents.Length + addOperands.Length > 4)
                throw new Exception("Too much parameter for expression : " + this);

            var data = new Operands(-1);
            if (graph != null)
                for (int i = 0; i < parents.Length; ++i)
                    data[i] = graph.GetFlattenedIndex(parents[i]);

            for (int i = 0; i < addOperands.Length; ++i)
                data[Operands.OperandCount - addOperands.Length + i] = addOperands[i];

            return data;
        }

        public virtual IEnumerable<VFXAttributeInfo> GetNeededAttributes()
        {
            return Enumerable.Empty<VFXAttributeInfo>();
        }

        public bool Is(Flags flag)      { return (m_Flags & flag) == flag; }
        public bool IsAny(Flags flag)   { return (m_Flags & flag) != 0; }

        public virtual VFXValueType valueType
        {
            get
            {
                var data = GetOperands(null);
                return VFXExpressionHelper.GetTypeOfOperation(operation, data[0], data[1], data[2], data[3]);
            }
        }
        public abstract VFXExpressionOperation operation { get; }

        public VFXExpression[] parents { get { return m_Parents; } }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            var other = obj as VFXExpression;
            if (other == null)
                return false;

            if (GetType() != other.GetType())
                return false;

            if (operation != other.operation)
                return false;

            if (valueType != other.valueType)
                return false;

            if (m_Flags != other.m_Flags)
                return false;

            if (GetHashCode() != other.GetHashCode())
                return false;

            var operands = additionnalOperands;
            var otherOperands = other.additionnalOperands;

            if (operands.Length != otherOperands.Length)
                return false;

            for (int i = 0; i < operands.Length; ++i)
                if (operands[i] != otherOperands[i])
                    return false;

            var thisParents = parents;
            var otherParents = other.parents;

            if (thisParents == null && otherParents == null)
                return true;
            if (thisParents == null || otherParents == null)
                return false;
            if (thisParents.Length != otherParents.Length)
                return false;

            for (int i = 0; i < thisParents.Length; ++i)
                if (!thisParents[i].Equals(otherParents[i]))
                    return false;

            return true;
        }

        public override sealed int GetHashCode()
        {
            if (!m_HashCodeCached)
            {
                m_HashCode = GetInnerHashCode();
                m_HashCodeCached = true;
            }

            return m_HashCode;
        }

        protected virtual int GetInnerHashCode()
        {
            int hash = GetType().GetHashCode();

            var parents = this.parents;
            for (int i = 0; i < parents.Length; ++i)
                hash = (hash * 397) ^ parents[i].GetHashCode(); // 397 taken from resharper

            var operands = additionnalOperands;
            for (int i = 0; i < operands.Length; ++i)
                hash = (hash * 397) ^ operands[i].GetHashCode();

            hash = (hash * 397) ^ m_Flags.GetHashCode();
            hash = (hash * 397) ^ valueType.GetHashCode();
            hash = (hash * 397) ^ operation.GetHashCode();

            return hash;
        }

        private static readonly int[] k_EmptyOperands = Enumerable.Empty<int>().ToArray();

        protected virtual int[] additionnalOperands { get { return k_EmptyOperands; } }
        public virtual T Get<T>()
        {
            var value = (this as VFXValue<T>);
            if (value == null)
            {
                throw new ArgumentException(string.Format("Get isn't available for {0} with {1}", typeof(T).FullName, GetType().FullName));
            }
            return value.Get();
        }

        public virtual object GetContent()
        {
            throw new ArgumentException(string.Format("GetContent isn't available for {0}", GetType().FullName));
        }

        private void PropagateParentsFlags()
        {
            if (m_Parents.Length > 0)
            {
                bool foldable = true;
                foreach (var parent in m_Parents)
                {
                    foldable &= parent.Is(Flags.Foldable);
                    m_Flags |= (parent.m_Flags & (Flags.NotCompilableOnCPU));
                    if (parent.IsAny(Flags.NotCompilableOnCPU) && parent.Is(Flags.InvalidOnGPU))
                        m_Flags |= Flags.InvalidOnGPU; // Only propagate GPU validity for per element expressions
                }
                if (foldable)
                    m_Flags |= Flags.Foldable;
                else
                    m_Flags &= ~Flags.Foldable;
            }
        }

        public static VFXExpression operator*(VFXExpression a, VFXExpression b) { return new VFXExpressionMul(a, b); }
        public static VFXExpression operator/(VFXExpression a, VFXExpression b) { return new VFXExpressionDivide(a, b); }
        public static VFXExpression operator+(VFXExpression a, VFXExpression b) { return new VFXExpressionAdd(a, b); }
        public static VFXExpression operator-(VFXExpression a, VFXExpression b) { return new VFXExpressionSubtract(a, b); }

        public static VFXExpression operator|(VFXExpression a, VFXExpression b) { return new VFXExpressionBitwiseOr(a, b); }
        public static VFXExpression operator&(VFXExpression a, VFXExpression b) { return new VFXExpressionBitwiseAnd(a, b); }
        public static VFXExpression operator|(VFXExpression a, uint b) { return new VFXExpressionBitwiseOr(a, VFXValue.Constant(b)); }
        public static VFXExpression operator&(VFXExpression a, uint b) { return new VFXExpressionBitwiseAnd(a, VFXValue.Constant(b)); }
        public static VFXExpression operator<<(VFXExpression a, int shift) { return new VFXExpressionBitwiseLeftShift(a, VFXValue.Constant((uint)shift)); }
        public static VFXExpression operator>>(VFXExpression a, int shift) { return new VFXExpressionBitwiseRightShift(a, VFXValue.Constant((uint)shift)); }

        public VFXExpression this[int index] { get { return new VFXExpressionExtractComponent(this, index); } }
        public VFXExpression x { get { return new VFXExpressionExtractComponent(this, 0); }  }
        public VFXExpression y { get { return new VFXExpressionExtractComponent(this, 1); }  }
        public VFXExpression z { get { return new VFXExpressionExtractComponent(this, 2); }  }
        public VFXExpression w { get { return new VFXExpressionExtractComponent(this, 3); }  }
        public VFXExpression xxx { get { return new VFXExpressionCombine(x, x, x); } }
        public VFXExpression yyy { get { return new VFXExpressionCombine(y, y, y); } }
        public VFXExpression zzz { get { return new VFXExpressionCombine(z, z, z); } }

        private Flags m_Flags = Flags.None;
        private VFXExpression[] m_Parents;

        private int m_HashCode;
        private bool m_HashCodeCached = false;
    }
}
