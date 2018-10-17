using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using NetworkCompression;


public class NetworkSchema
{
    public enum FieldType
    {
        Bool,
        Int,
        UInt,
        Float,
        Vector2,
        Vector3,
        Quaternion,
        String,
        ByteArray
    }

    public class FieldInfo
    {
        public string name;
        public FieldType fieldType;
        public int bits;
        public bool delta;
        public int precision;
        public int arraySize;
        public int byteOffset;
        public int startContext;
        public byte fieldMask;

        public FieldStatsBase stats;
    }

    public interface IFieldValue<T>
    {
        T Min(T other);
        T Max(T other);

        T Sub(T other);

        string ToString(FieldInfo fieldInfo, bool showRaw);
    }

    public struct FieldValueBool : IFieldValue<FieldValueBool>
    {
        public FieldValueBool(bool value)
        {
            m_Value = value;
        }

        public FieldValueBool Min(FieldValueBool other)
        {
            bool otherValue = ((FieldValueBool)other).m_Value;
            return new FieldValueBool(m_Value && otherValue);
        }

        public FieldValueBool Max(FieldValueBool other)
        {
            bool otherValue = ((FieldValueBool)other).m_Value;
            return new FieldValueBool(m_Value || otherValue);
        }

        public FieldValueBool Sub(FieldValueBool other)
        {
            bool otherValue = ((FieldValueBool)other).m_Value;
            return new FieldValueBool(m_Value != otherValue);
        }

        public string ToString(FieldInfo fieldInfo, bool showRaw) { return "" + m_Value; }

        bool m_Value;
    }

    public struct FieldValueInt : IFieldValue<FieldValueInt>
    {
        public FieldValueInt(int value)
        {
            m_Value = value;
        }

        public FieldValueInt Min(FieldValueInt other)
        {
            int otherValue = ((FieldValueInt)other).m_Value;
            return new FieldValueInt(m_Value < otherValue ? m_Value : otherValue);
        }

        public FieldValueInt Max(FieldValueInt other)
        {
            int otherValue = ((FieldValueInt)other).m_Value;
            return new FieldValueInt(m_Value > otherValue ? m_Value : otherValue);
        }

        public FieldValueInt Sub(FieldValueInt other)
        {
            int otherValue = ((FieldValueInt)other).m_Value;
            return new FieldValueInt(m_Value - otherValue);
        }

        public string ToString(FieldInfo fieldInfo, bool showRaw)
        {
            return "" + m_Value;
        }

        public int m_Value;
    }

    public struct FieldValueUInt : IFieldValue<FieldValueUInt>
    {
        public FieldValueUInt(uint value)
        {
            m_Value = value;
        }

        public FieldValueUInt Min(FieldValueUInt other)
        {
            uint otherValue = ((FieldValueUInt)other).m_Value;
            return new FieldValueUInt(m_Value < otherValue ? m_Value : otherValue);
        }

        public FieldValueUInt Max(FieldValueUInt other)
        {
            uint otherValue = ((FieldValueUInt)other).m_Value;
            return new FieldValueUInt(m_Value > otherValue ? m_Value : otherValue);
        }

        public FieldValueUInt Sub(FieldValueUInt other)
        {
            uint otherValue = ((FieldValueUInt)other).m_Value;
            return new FieldValueUInt(m_Value - otherValue);
        }

        public string ToString(FieldInfo fieldInfo, bool showRaw)
        {
            return "" + m_Value;
        }

        public uint m_Value;
    }

    public struct FieldValueFloat : IFieldValue<FieldValueFloat>
    {
        public FieldValueFloat(uint value)
        {
            m_Value = value;
        }

        public FieldValueFloat Min(FieldValueFloat other)
        {
            uint otherValue = ((FieldValueFloat)other).m_Value;
            return new FieldValueFloat(m_Value < otherValue ? m_Value : otherValue);
        }

        public FieldValueFloat Max(FieldValueFloat other)
        {
            uint otherValue = ((FieldValueFloat)other).m_Value;
            return new FieldValueFloat(m_Value > otherValue ? m_Value : otherValue);
        }

        public FieldValueFloat Sub(FieldValueFloat other)
        {
            uint otherValue = ((FieldValueFloat)other).m_Value;
            return new FieldValueFloat(m_Value - otherValue);
        }

        public string ToString(FieldInfo fieldInfo, bool showRaw)
        {
            if(showRaw)
            {
                return "" + m_Value;
            }
            else
            {
                if (fieldInfo.delta)
                    return "" + ((int)m_Value * NetworkConfig.decoderPrecisionScales[fieldInfo.precision]);
                else
                    return "" + ConversionUtility.UInt32ToFloat(m_Value);
            }
        }

        public uint m_Value;
    }

    public struct FieldValueVector2 : IFieldValue<FieldValueVector2>
    {
        public FieldValueVector2(uint x, uint y)
        {
            m_ValueX = x;
            m_ValueY = y;
        }

        public FieldValueVector2 Min(FieldValueVector2 other)
        {
            return new FieldValueVector2(0, 0);
        }

        public FieldValueVector2 Max(FieldValueVector2 other)
        {
            return new FieldValueVector2(0, 0);
        }

        public FieldValueVector2 Sub(FieldValueVector2 other)
        {
            var o = (FieldValueVector2)other;
            return new FieldValueVector2(m_ValueX - o.m_ValueX, m_ValueX - o.m_ValueY);
        }

        public string ToString(FieldInfo fieldInfo, bool showRaw)
        {
            if (fieldInfo.delta)
            {
                float scale = NetworkConfig.decoderPrecisionScales[fieldInfo.precision];
                return "(" + ((int)m_ValueX * scale) + ", " + ((int)m_ValueY * scale) + ")";
            }

            else
                return "(" + ConversionUtility.UInt32ToFloat(m_ValueX) + ", " + ConversionUtility.UInt32ToFloat(m_ValueY) + ")";
        }

        uint m_ValueX, m_ValueY;
    }

    public struct FieldValueVector3 : IFieldValue<FieldValueVector3>
    {
        public FieldValueVector3(uint x, uint y, uint z)
        {
            m_ValueX = x;
            m_ValueY = y;
            m_ValueZ = z;
        }

        public FieldValueVector3 Min(FieldValueVector3 other)
        {
            return new FieldValueVector3(0, 0, 0);
        }

        public FieldValueVector3 Max(FieldValueVector3 other)
        {
            return new FieldValueVector3(0, 0, 0);
        }

        public FieldValueVector3 Sub(FieldValueVector3 other)
        {
            var o = (FieldValueVector3)other;
            return new FieldValueVector3(m_ValueX - o.m_ValueX, m_ValueY - o.m_ValueY, m_ValueZ - o.m_ValueZ);
        }

        public string ToString(FieldInfo fieldInfo, bool showRaw)
        {
            if (fieldInfo.delta)
            {
                float scale = NetworkConfig.decoderPrecisionScales[fieldInfo.precision];
                return "(" + ((int)m_ValueX * scale) + ", " + ((int)m_ValueY * scale) + ", " + ((int)m_ValueZ * scale) + ")";
            }

            else
                return "(" + ConversionUtility.UInt32ToFloat(m_ValueX) + ", " + ConversionUtility.UInt32ToFloat(m_ValueY) + ", " + ConversionUtility.UInt32ToFloat(m_ValueZ) + ")";
        }

        public uint m_ValueX, m_ValueY, m_ValueZ;
    }

    public struct FieldValueQuaternion : IFieldValue<FieldValueQuaternion>
    {
        public FieldValueQuaternion(uint x, uint y, uint z, uint w)
        {
            m_ValueX = x;
            m_ValueY = y;
            m_ValueZ = z;
            m_ValueW = w;
        }

        public FieldValueQuaternion Min(FieldValueQuaternion other)
        {
            return new FieldValueQuaternion(0, 0, 0, 0);
        }

        public FieldValueQuaternion Max(FieldValueQuaternion other)
        {
            return new FieldValueQuaternion(0, 0, 0, 0);
        }

        public FieldValueQuaternion Sub(FieldValueQuaternion other)
        {
            var o = (FieldValueQuaternion)other;
            return new FieldValueQuaternion(m_ValueX - o.m_ValueX, m_ValueY - o.m_ValueY, m_ValueZ - o.m_ValueZ, m_ValueW - o.m_ValueW);
        }

        public string ToString(FieldInfo fieldInfo, bool showRaw)
        {
            if (fieldInfo.delta)
            {
                float scale = NetworkConfig.decoderPrecisionScales[fieldInfo.precision];
                return "(" + ((int)m_ValueX * scale) + ", " + ((int)m_ValueY * scale) + ", " + ((int)m_ValueZ * scale) + ", " + ((int)m_ValueW * scale) + ")";
            }

            else
                return "(" + ConversionUtility.UInt32ToFloat(m_ValueX) + ", " + ConversionUtility.UInt32ToFloat(m_ValueY) + ", " + ConversionUtility.UInt32ToFloat(m_ValueZ) + ", " + ConversionUtility.UInt32ToFloat(m_ValueW) + ")";
        }

        uint m_ValueX, m_ValueY, m_ValueZ, m_ValueW;
    }

    public struct FieldValueString : IFieldValue<FieldValueString>
    {
        public FieldValueString(string value)
        {
            m_Value = value;
        }

        public FieldValueString(byte[] valueBuffer, int valueOffset, int valueLength)
        {
            if(valueBuffer != null)
            {
                int numChars = NetworkConfig.encoding.GetChars(valueBuffer, valueOffset, valueLength, s_CharBuffer, 0);
                m_Value = new string(s_CharBuffer, 0, numChars);
            }
            else
            {
                m_Value = "";
            }
        }

        public FieldValueString Min(FieldValueString other) { return EmptyStringValue; }
        public FieldValueString Max(FieldValueString other) { return EmptyStringValue; }
        public FieldValueString Sub(FieldValueString other) { return EmptyStringValue; }

        public string ToString(FieldInfo fieldInfo, bool showRaw) { return m_Value; }


        public readonly static FieldValueString EmptyStringValue = new FieldValueString("");

        string m_Value;
        static char[] s_CharBuffer = new char[1024 * 32];
    }

    public struct FieldValueByteArray : IFieldValue<FieldValueByteArray>
    {
        public FieldValueByteArray(byte[] value, int valueOffset, int valueLength)
        {
            if(value != null)
            {
                m_Value = new byte[valueLength];
                Array.Copy(value, valueOffset, m_Value, 0, valueLength);
            }
            else
            {
                m_Value = null;
            }
        }

        public FieldValueByteArray Min(FieldValueByteArray other) { return EmptyByteArrayValue; }
        public FieldValueByteArray Max(FieldValueByteArray other) { return EmptyByteArrayValue; }
        public FieldValueByteArray Sub(FieldValueByteArray other) { return EmptyByteArrayValue; }

        public string ToString(FieldInfo fieldInfo, bool showRaw) { return ""; }


        public readonly static FieldValueByteArray EmptyByteArrayValue = new FieldValueByteArray(null, 0, 0);

        byte[] m_Value;
    }

    public abstract class FieldStatsBase
    {
        public abstract string GetValue(bool showRaw);
        public abstract string GetValueMin(bool showRaw);
        public abstract string GetValueMax(bool showRaw);

        public abstract string GetPrediction(bool showRaw);
        public abstract string GetPredictionMin(bool showRaw);
        public abstract string GetPredictionMax(bool showRaw);

        public abstract string GetDelta(bool showRaw);
        public abstract string GetDeltaMin(bool showRaw);
        public abstract string GetDeltaMax(bool showRaw);

        public static FieldStatsBase CreateFieldStats(FieldInfo fieldInfo)
        {
            switch (fieldInfo.fieldType)
            {
                case FieldType.Bool:
                    return new FieldStats<FieldValueBool>(fieldInfo);
                case FieldType.Int:
                    return new FieldStats<FieldValueInt>(fieldInfo);
                case FieldType.UInt:
                    return new FieldStats<FieldValueUInt>(fieldInfo);
                case FieldType.Float:
                    return new FieldStats<FieldValueFloat>(fieldInfo);
                case FieldType.Vector2:
                    return new FieldStats<FieldValueVector2>(fieldInfo);
                case FieldType.Vector3:
                    return new FieldStats<FieldValueVector3>(fieldInfo);
                case FieldType.Quaternion:
                    return new FieldStats<FieldValueQuaternion>(fieldInfo);
                case FieldType.String:
                    return new FieldStats<FieldValueString>(fieldInfo);
                case FieldType.ByteArray:
                    return new FieldStats<FieldValueByteArray>(fieldInfo);
                default:
                    GameDebug.Assert(false);
                    return null;
            }
        }

        public int GetNumWrites() { return m_NumWrites; }

        public int GetNumBitsWritten() { return m_NumBitsWritten; }

        protected int m_NumWrites;
        protected int m_NumBitsWritten;
    }

    
    public class FieldStats<T> : FieldStatsBase where T : IFieldValue<T>
    {
        public T value;
        public T valueMin;
        public T valueMax;
        public T prediction;
        public T predictionMin;
        public T predictionMax;

        public T delta;
        public T deltaMin;
        public T deltaMax;

        public FieldStats(FieldInfo fieldInfo)
        {
            m_FieldInfo = fieldInfo;
        }
        
        public void Add(T value, T prediction, int bitsWritten)
        {
            this.value = value;
            this.prediction = prediction;
            this.delta = (T)value.Sub(prediction);
            
            if (m_NumWrites > 0)
            {
                valueMin = (T)valueMin.Min(value);
                valueMax = (T)valueMin.Max(value);
                predictionMin = (T)predictionMin.Min(prediction);
                predictionMax = (T)predictionMax.Max(prediction);
                deltaMin = (T)deltaMin.Min(delta);
                deltaMax = (T)deltaMax.Max(delta);
            }
            else
            {
                valueMin = value;
                valueMax = value;
                predictionMin = prediction;
                predictionMax = prediction;
            }

            this.m_NumBitsWritten += bitsWritten;
            this.m_NumWrites++;
        }

        public override string GetValue(bool showRaw) { return ((T)value).ToString(m_FieldInfo, showRaw); }
        public override string GetValueMin(bool showRaw) { return ((T)valueMin).ToString(m_FieldInfo, showRaw); }

        public override string GetValueMax(bool showRaw) { return ((T)valueMax).ToString(m_FieldInfo, showRaw); }

        public override string GetPrediction(bool showRaw) { return ((T)prediction).ToString(m_FieldInfo, showRaw); }
        public override string GetPredictionMin(bool showRaw) { return ((T)predictionMin).ToString(m_FieldInfo, showRaw); }
        public override string GetPredictionMax(bool showRaw) { return ((T)predictionMax).ToString(m_FieldInfo, showRaw); }

        public override string GetDelta(bool showRaw) { return ((T)delta).ToString(m_FieldInfo, showRaw); }
        public override string GetDeltaMin(bool showRaw) { return ((T)deltaMin).ToString(m_FieldInfo, showRaw); }
        public override string GetDeltaMax(bool showRaw) { return ((T)deltaMax).ToString(m_FieldInfo, showRaw); }

        FieldInfo m_FieldInfo;
    }

    // Functions for updating stats on a field that can be conditionally excluded from the build
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    static public void AddStatsToFieldBool(FieldInfo fieldInfo, bool value, bool prediction, int numBits) { ((NetworkSchema.FieldStats<NetworkSchema.FieldValueBool>)fieldInfo.stats).Add(new NetworkSchema.FieldValueBool(value), new NetworkSchema.FieldValueBool(prediction), numBits); }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    static public void AddStatsToFieldInt(FieldInfo fieldInfo, int value, int prediction, int numBits) { ((NetworkSchema.FieldStats<NetworkSchema.FieldValueInt>)fieldInfo.stats).Add(new NetworkSchema.FieldValueInt(value), new NetworkSchema.FieldValueInt(prediction), numBits); }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    static public void AddStatsToFieldUInt(FieldInfo fieldInfo, uint value, uint prediction, int numBits) { ((NetworkSchema.FieldStats<NetworkSchema.FieldValueUInt>)fieldInfo.stats).Add(new NetworkSchema.FieldValueUInt(value), new NetworkSchema.FieldValueUInt(prediction), numBits); }
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    static public void AddStatsToFieldFloat(FieldInfo fieldInfo, uint value, uint prediction, int numBits) { ((NetworkSchema.FieldStats<NetworkSchema.FieldValueFloat>)fieldInfo.stats).Add(new NetworkSchema.FieldValueFloat(value), new NetworkSchema.FieldValueFloat(prediction), numBits); }
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    static public void AddStatsToFieldVector2(FieldInfo fieldInfo, uint vx, uint vy, uint px, uint py, int numBits) { ((NetworkSchema.FieldStats<NetworkSchema.FieldValueVector2>)fieldInfo.stats).Add(new NetworkSchema.FieldValueVector2(vx, vy), new NetworkSchema.FieldValueVector2(px, py), numBits); }
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    static public void AddStatsToFieldVector3(FieldInfo fieldInfo, uint vx, uint vy, uint vz, uint px, uint py, uint pz, int numBits) { ((NetworkSchema.FieldStats<NetworkSchema.FieldValueVector3>)fieldInfo.stats).Add(new NetworkSchema.FieldValueVector3(vx, vy, vz), new NetworkSchema.FieldValueVector3(px, py, pz), numBits); }
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    static public void AddStatsToFieldQuaternion(FieldInfo fieldInfo, uint vx, uint vy, uint vz, uint vw, uint px, uint py, uint pz, uint pw, int numBits) { ((NetworkSchema.FieldStats<NetworkSchema.FieldValueQuaternion>)fieldInfo.stats).Add(new NetworkSchema.FieldValueQuaternion(vx, vy, vz, vw), new NetworkSchema.FieldValueQuaternion(px, py, pz, pw), numBits); }
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    static public void AddStatsToFieldString(FieldInfo fieldInfo, byte[] valueBuffer, int valueOffset, int valueLength, int numBits) { ((NetworkSchema.FieldStats<FieldValueString>)fieldInfo.stats).Add(new NetworkSchema.FieldValueString(valueBuffer, valueOffset, valueLength), NetworkSchema.FieldValueString.EmptyStringValue, numBits); }
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    static public void AddStatsToFieldByteArray(FieldInfo fieldInfo, byte[] valueBuffer, int valueOffset, int valueLength, int numBits) { ((NetworkSchema.FieldStats<FieldValueByteArray>)fieldInfo.stats).Add(new NetworkSchema.FieldValueByteArray(valueBuffer, valueOffset, valueLength), NetworkSchema.FieldValueByteArray.EmptyByteArrayValue, numBits); }

    public List<FieldInfo> fields = new List<FieldInfo>();
    public int nextFieldOffset = 0;
    public int id;
    
    public NetworkSchema(int id)
    {
        GameDebug.Assert(id >= 0 && id < NetworkConfig.maxSchemaIds);
        this.id = id;
    }

    public int GetByteSize()
    {
        return nextFieldOffset;
    }

    public void AddField(FieldInfo field)
    {
        GameDebug.Assert(fields.Count < NetworkConfig.maxFieldsPerSchema);
        field.byteOffset = nextFieldOffset;
        field.stats = FieldStatsBase.CreateFieldStats(field);
        fields.Add(field);
        nextFieldOffset += CalculateFieldByteSize(field);
    }
    
    public static int CalculateFieldByteSize(FieldInfo field)
    {
        int size = 0;
        switch (field.fieldType)
        {
            case FieldType.Bool:
                size = 1;
                break;
            case FieldType.Int:
            case FieldType.UInt:
            case FieldType.Float:
                size = (field.bits + 7) / 8;
                break;
            case FieldType.Vector2:
                size = (field.bits + 7) / 8 * 2;
                break;
            case FieldType.Vector3:
                size = (field.bits + 7) / 8 * 3;
                break;
            case FieldType.Quaternion:
                size = (field.bits + 7) / 8 * 4;
                break;
            case FieldType.String:
            case FieldType.ByteArray:
                size = 2 + field.arraySize;
                break;
            default:
                GameDebug.Assert(false);
                break;

        }
        return size;
    }

    public static NetworkSchema ReadSchema<TInputStream>(ref TInputStream input) where TInputStream : NetworkCompression.IInputStream
    {
        int count = (int)input.ReadPackedUInt(NetworkConfig.miscContext);
        int id = (int)input.ReadPackedUInt(NetworkConfig.miscContext);
        var schema = new NetworkSchema(id);
        for (int i = 0; i < count; ++i)
        {
            var field = new FieldInfo();
            field.fieldType = (FieldType) input.ReadRawBits(4);
            field.delta = input.ReadRawBits(1) != 0;
            field.bits = (int)input.ReadRawBits(6);
            field.precision = (int)input.ReadRawBits(2);
            field.arraySize = (int)input.ReadRawBits(16);
            field.startContext = schema.fields.Count * NetworkConfig.maxContextsPerField + schema.id * NetworkConfig.maxContextsPerSchema + NetworkConfig.firstSchemaContext;
            field.fieldMask = (byte)input.ReadRawBits(8);
            schema.AddField(field);
        }
        return schema;
    }

    public static void WriteSchema<TOutputStream>(NetworkSchema schema, ref TOutputStream output) where TOutputStream : NetworkCompression.IOutputStream
    {
        output.WritePackedUInt((uint)schema.fields.Count, NetworkConfig.miscContext);
        output.WritePackedUInt((uint)schema.id, NetworkConfig.miscContext);
        foreach (var field in schema.fields)
        {
            output.WriteRawBits((uint)field.fieldType, 4);
            output.WriteRawBits(field.delta ? 1U : 0, 1);
            output.WriteRawBits((uint)field.bits, 6);
            output.WriteRawBits((uint)field.precision, 2);
            output.WriteRawBits((uint)field.arraySize, 16);
            output.WriteRawBits((uint)field.fieldMask, 8);
        }
    }
    
    public static void CopyFieldsFromBuffer<TOutputStream>(NetworkSchema schema, byte[] inputBuffer, ref TOutputStream output) where TOutputStream : NetworkCompression.IOutputStream
    {
        var input = new ByteInputStream(inputBuffer);

        int fieldIndex = 0;
        for (; fieldIndex < schema.fields.Count; ++fieldIndex)
        {
            var field = schema.fields[fieldIndex];
            switch (field.fieldType)
            {
                case NetworkSchema.FieldType.Bool:
                case NetworkSchema.FieldType.UInt:
                case NetworkSchema.FieldType.Int:
                case NetworkSchema.FieldType.Float:
                    output.WriteRawBits(input.ReadBits(field.bits), field.bits);
                    break;

                case NetworkSchema.FieldType.Vector2:
                    output.WriteRawBits(input.ReadUInt32(), field.bits);
                    output.WriteRawBits(input.ReadUInt32(), field.bits);
                    break;

                case NetworkSchema.FieldType.Vector3:
                    output.WriteRawBits(input.ReadUInt32(), field.bits);
                    output.WriteRawBits(input.ReadUInt32(), field.bits);
                    output.WriteRawBits(input.ReadUInt32(), field.bits);
                    break;

                case NetworkSchema.FieldType.Quaternion:
                    output.WriteRawBits(input.ReadUInt32(), field.bits);
                    output.WriteRawBits(input.ReadUInt32(), field.bits);
                    output.WriteRawBits(input.ReadUInt32(), field.bits);
                    output.WriteRawBits(input.ReadUInt32(), field.bits);
                    break;

                case NetworkSchema.FieldType.String:
                case NetworkSchema.FieldType.ByteArray:
                    {
                        byte[] data;
                        int dataIndex;
                        int dataSize;
                        input.GetByteArray(out data, out dataIndex, out dataSize, field.arraySize);
                        output.WritePackedUInt((uint)dataSize, field.startContext);
                        output.WriteRawBytes(data, dataIndex, dataSize);
                    }
                    break;

                default: GameDebug.Assert(false); break;
            }
        }
    }

    public static void CopyFieldsToBuffer<TInputStream>(NetworkSchema schema, ref TInputStream input, byte[] outputBuffer) where TInputStream : NetworkCompression.IInputStream
    {
        var output = new ByteOutputStream(outputBuffer);
        for (var fieldIndex = 0; fieldIndex < schema.fields.Count; ++fieldIndex)
        {
            var field = schema.fields[fieldIndex];
            switch (field.fieldType)
            {
                case NetworkSchema.FieldType.Bool:
                case NetworkSchema.FieldType.UInt:
                case NetworkSchema.FieldType.Int:
                case NetworkSchema.FieldType.Float:
                    output.WriteBits(input.ReadRawBits(field.bits), field.bits);                    
                    break;

                case NetworkSchema.FieldType.Vector2:
                    output.WriteUInt32(input.ReadRawBits(field.bits));
                    output.WriteUInt32(input.ReadRawBits(field.bits));
                    break;

                case NetworkSchema.FieldType.Vector3:
                    output.WriteUInt32(input.ReadRawBits(field.bits));
                    output.WriteUInt32(input.ReadRawBits(field.bits));
                    output.WriteUInt32(input.ReadRawBits(field.bits));
                    break;

                case NetworkSchema.FieldType.Quaternion:
                    output.WriteUInt32(input.ReadRawBits(field.bits));
                    output.WriteUInt32(input.ReadRawBits(field.bits));
                    output.WriteUInt32(input.ReadRawBits(field.bits));
                    output.WriteUInt32(input.ReadRawBits(field.bits));
                    break;

                case NetworkSchema.FieldType.String:
                case NetworkSchema.FieldType.ByteArray:
                    output.CopyByteArray(ref input, field.arraySize, field.startContext);
                    break;

                default: GameDebug.Assert(false); break;
            }
        }
    }

    public static void SkipFields<TInputStream>(NetworkSchema schema, ref TInputStream input) where TInputStream : NetworkCompression.IInputStream
    {
        for (var fieldIndex = 0; fieldIndex < schema.fields.Count; ++fieldIndex)
        {
            var field = schema.fields[fieldIndex];
            switch (field.fieldType)
            {
                case NetworkSchema.FieldType.Bool:
                case NetworkSchema.FieldType.UInt:
                case NetworkSchema.FieldType.Int:
                case NetworkSchema.FieldType.Float:
                    input.ReadRawBits(field.bits);
                    break;

                case NetworkSchema.FieldType.Vector2:
                    input.ReadRawBits(field.bits);
                    input.ReadRawBits(field.bits);
                    break;

                case NetworkSchema.FieldType.Vector3:
                    input.ReadRawBits(field.bits);
                    input.ReadRawBits(field.bits);
                    input.ReadRawBits(field.bits);
                    break;

                case NetworkSchema.FieldType.Quaternion:
                    input.ReadRawBits(field.bits);
                    input.ReadRawBits(field.bits);
                    input.ReadRawBits(field.bits);
                    input.ReadRawBits(field.bits);
                    break;

                case NetworkSchema.FieldType.String:
                case NetworkSchema.FieldType.ByteArray:
                    input.SkipRawBytes((int)input.ReadPackedUInt(field.startContext));
                    break;

                default: GameDebug.Assert(false); break;
            }
        }
    }
}

