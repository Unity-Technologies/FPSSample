using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Properties;
using Unity.Properties.Serialization;

namespace Unity.Entities.Properties
{
    public interface IPrimitivePropertyVisitor
    {
        // @TODO decouple from visitor ... 
        HashSet<Type> SupportedPrimitiveTypes();
    }

    public interface IOptimizedVisitor :
        ICustomVisit<bool>,
        ICustomVisit<float>,
        ICustomVisit<double>,
        ICustomVisit<string>,
        ICustomVisit<sbyte>,
        ICustomVisit<byte>,
        ICustomVisit<int>,
        ICustomVisit<float2>,
        ICustomVisit<float3>,
        ICustomVisit<float4>,
        ICustomVisit<float2x2>,
        ICustomVisit<float3x3>,
        ICustomVisit<float4x4>
    { }

    public static class OptimizedVisitor
    {
        public static bool Supports(Type t)
        {
            return s_OptimizedSet.Contains(t);
        }

        public static HashSet<Type> SupportedTypes()
        {
            return s_OptimizedSet;
        }

        private static HashSet<Type> s_OptimizedSet;

        static OptimizedVisitor()
        {
            s_OptimizedSet = new HashSet<Type>();
            foreach (var it in typeof(IOptimizedVisitor).GetInterfaces())
            {
                if (it.IsGenericType && typeof(ICustomVisit<>) == it.GetGenericTypeDefinition())
                {
                    var genArgs = it.GetGenericArguments();
                    if (genArgs.Length == 1)
                    {
                        s_OptimizedSet.Add(genArgs[0]);
                    }
                }
            }
        }
    }

    public static class StringBufferExtensions
    {
        public static void AppendPropertyName(this StringBuffer sb, string propertyName)
        {
            sb.EnsureCapacity(propertyName.Length + 4);

            var buffer = sb.Buffer;
            var position = sb.Length;

            buffer[position++] = '\"';

            var len = propertyName.Length;
            for (var i = 0; i < len; i++)
            {
                buffer[position + i] = propertyName[i];
            }
            position += len;

            buffer[position++] = '\"';
            buffer[position++] = ':';
            buffer[position++] = ' ';

            sb.Length = position;
        }

        public static void AppendFloat2(this StringBuffer sb, float2 value)
        {
            sb.Append(value.x);
            sb.Append(',');
            sb.Append(value.y);
        }

        public static void AppendFloat3(this StringBuffer sb, float3 value)
        {
            sb.Append(value.x);
            sb.Append(',');
            sb.Append(value.y);
            sb.Append(',');
            sb.Append(value.z);
        }

        public static void AppendFloat4(this StringBuffer sb, float4 value)
        {
            sb.Append(value.x);
            sb.Append(',');
            sb.Append(value.y);
            sb.Append(',');
            sb.Append(value.z);
            sb.Append(',');
            sb.Append(value.w);
        }
    }

    public class JsonVisitor : JsonPropertyVisitor, IOptimizedVisitor
    {
        public HashSet<Type> SupportedPrimitiveTypes()
        {
            return OptimizedVisitor.SupportedTypes();
        }

        void ICustomVisit<float2>.CustomVisit(float2 value)
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(Property.Name);
            StringBuffer.Append('[');
            StringBuffer.AppendFloat2(value);
            StringBuffer.Append(']');
            StringBuffer.Append(",\n");
        }

        void ICustomVisit<float3>.CustomVisit(float3 value)
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(Property.Name);
            StringBuffer.Append('[');
            StringBuffer.AppendFloat3(value);
            StringBuffer.Append(']');
            StringBuffer.Append(",\n");
        }

        void ICustomVisit<float4>.CustomVisit(float4 value)
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(Property.Name);
            StringBuffer.Append('[');
            StringBuffer.AppendFloat4(value);
            StringBuffer.Append(']');
            StringBuffer.Append(",\n");
        }

        void ICustomVisit<float2x2>.CustomVisit(float2x2 value)
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(Property.Name);
            StringBuffer.Append('[');
            StringBuffer.AppendFloat2(value.c0);
            StringBuffer.Append(',');
            StringBuffer.AppendFloat2(value.c1);
            StringBuffer.Append(']');
            StringBuffer.Append(",\n");
        }

        void ICustomVisit<sbyte>.CustomVisit(sbyte value)
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(Property.Name);
            StringBuffer.Append(value);
            StringBuffer.Append(",\n");
        }

        void ICustomVisit<byte>.CustomVisit(byte value)
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(Property.Name);
            StringBuffer.Append(value);
            StringBuffer.Append(",\n");
        }

        void ICustomVisit<int>.CustomVisit(int value)
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(Property.Name);
            StringBuffer.Append(value);
            StringBuffer.Append(",\n");
        }
        void ICustomVisit<string>.CustomVisit(string value)
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(Property.Name);
            StringBuffer.Append(value);
            StringBuffer.Append(",\n");
        }

        void ICustomVisit<float3x3>.CustomVisit(float3x3 value)
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(Property.Name);
            StringBuffer.Append('[');
            StringBuffer.AppendFloat3(value.c0);
            StringBuffer.Append(',');
            StringBuffer.AppendFloat3(value.c1);
            StringBuffer.Append(',');
            StringBuffer.AppendFloat3(value.c2);
            StringBuffer.Append(']');
            StringBuffer.Append(",\n");
        }

        void ICustomVisit<float4x4>.CustomVisit(float4x4 value)
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(Property.Name);
            StringBuffer.Append('[');
            StringBuffer.AppendFloat4(value.c0);
            StringBuffer.Append(',');
            StringBuffer.AppendFloat4(value.c1);
            StringBuffer.Append(',');
            StringBuffer.AppendFloat4(value.c2);
            StringBuffer.Append(',');
            StringBuffer.AppendFloat4(value.c3);
            StringBuffer.Append(']');
            StringBuffer.Append(",\n");
        }
    }
}
