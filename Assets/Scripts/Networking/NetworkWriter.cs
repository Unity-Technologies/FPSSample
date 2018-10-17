using System;
using System.Collections.Generic;
using System.Xml.Schema;
using UnityEngine;


public struct NetworkWriter
{
    public NetworkWriter(byte[] buffer, NetworkSchema schema, bool generateSchema = false)
    {
        m_Output = new ByteOutputStream(buffer);
        m_Schema = schema;
        m_CurrentField = null;
        m_NextFieldIndex = 0;
        m_GenerateSchema = generateSchema;
        m_FieldMask = 0;
    }

    public int GetLength()
    {
        return m_Output.GetBytePosition();
    }

    void ValidateOrGenerateSchema(string name, NetworkSchema.FieldType type, int bits = 0, bool delta = false, int precision = 0, int arraySize = 0)
    {
        // TOULF precision is amount of digits (10^-3)
        GameDebug.Assert(precision < 4);
        if (m_GenerateSchema == true)
        {
            // TOULF m_Scheme will contain scheme for ALL of the *entity* (not component)
            m_Schema.AddField(new NetworkSchema.FieldInfo()
            {
                name = name,
                fieldType = type,
                bits = bits,
                delta = delta,
                precision = precision,
                arraySize = arraySize,
                fieldMask = m_FieldMask,
                startContext = m_Schema.fields.Count * NetworkConfig.maxContextsPerField + m_Schema.id * NetworkConfig.maxContextsPerSchema + NetworkConfig.firstSchemaContext
            });
        }
        else if (m_Schema != null)
        {
            m_CurrentField = m_Schema.fields[m_NextFieldIndex];
            GameDebug.Assert(m_CurrentField.name == name);
            GameDebug.Assert(m_CurrentField.fieldType == type);
            GameDebug.Assert(m_CurrentField.bits == bits);
            GameDebug.Assert(m_CurrentField.delta == delta);
            GameDebug.Assert(m_CurrentField.precision == precision);
            GameDebug.Assert(m_CurrentField.arraySize == arraySize);
            GameDebug.Assert(m_CurrentField.fieldMask == m_FieldMask);

            ++m_NextFieldIndex;
        }
        // TOULF when is it ok that m_Scheme being null?
    }

    public enum FieldSectionType
    {
        OnlyPredicting,
        OnlyNotPredicting
    }

    public void SetFieldSection(FieldSectionType type)
    {
        GameDebug.Assert(m_FieldMask == 0, "Field masks cannot be combined.");
        if (type == FieldSectionType.OnlyNotPredicting)
            m_FieldMask = 0x1;
        else
            m_FieldMask = 0x2;
    }

    public void ClearFieldSection()
    {
        GameDebug.Assert(m_FieldMask != 0, "Trying to clear a field mask but none has been set.");
        m_FieldMask = 0;
    }

    public void WriteBoolean(string name, bool value)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.Bool, 1, false);
        m_Output.WriteUInt8(value ? (byte)1 : (byte)0);
    }

    public void WriteByte(string name, byte value)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.UInt, 8, true);
        m_Output.WriteUInt8(value);
    }

    public void WriteInt16(string name, short value)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.Int, 16, true);
        m_Output.WriteUInt16((ushort)value);
    }

    public void WriteUInt16(string name, ushort value)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.UInt, 16, true);
        m_Output.WriteUInt16(value);
    }

    public void WriteInt32(string name, int value)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.Int, 32, true);
        m_Output.WriteUInt32((uint)value);
    }

    public void WriteUInt32(string name, uint value)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.UInt, 32, true);
        m_Output.WriteUInt32(value);
    }

    public void WriteFloat(string name, float value)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.Float, 32, false);
        m_Output.WriteUInt32(NetworkUtils.FloatToUInt32(value));
    }

    public void WriteFloatQ(string name, float value, int precision = 3)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.Float, 32, true, precision);
        m_Output.WriteUInt32((uint)Mathf.RoundToInt(value * NetworkConfig.encoderPrecisionScales[precision]));
    }

    public enum OverrunBehaviour
    {
        AssertMaxLength,
        WarnAndTrunc,
        SilentTrunc
    }

    static char[] _writeStringBuf = new char[64];
    public void WriteString(string name, string value, int maxLength = 64, OverrunBehaviour overrunBehaviour = OverrunBehaviour.WarnAndTrunc)
    {
        if (value == null)
            value = "";

        if (value.Length > _writeStringBuf.Length)
            _writeStringBuf = new char[_writeStringBuf.Length * 2];

        value.CopyTo(0, _writeStringBuf, 0, value.Length);
        WriteString(name, _writeStringBuf, value.Length, maxLength, overrunBehaviour);
    }

    public void WriteString(string name, char[] value, int length, int maxLength, OverrunBehaviour overrunBehaviour)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.String, 0, false, 0, maxLength);
        if (length == 0)
        {
            m_Output.WriteByteArray(null, 0, 0, maxLength);
            return;
        }

        GameDebug.Assert(maxLength <= s_ByteBuffer.Length, "NetworkWriter: Max length has to be less than {0}", s_ByteBuffer.Length);

        // Ensure the (utf-8) *encoded* string is not too big. If it is, cut it off,
        // convert back to unicode and then back again to utf-8. This little dance gives
        // a valid utf-8 string within the buffer size.
        var byteCount = NetworkConfig.encoding.GetBytes(value, 0, length, s_ByteBuffer, 0);
        if (byteCount > maxLength)
        {
            if (overrunBehaviour == OverrunBehaviour.AssertMaxLength)
            {
                GameDebug.Assert(false, "NetworkWriter : string {0} too long. (Using {1}/{2} allowed encoded bytes): ", value, byteCount, maxLength);
            }
            // truncate
            var truncWithBadEnd = NetworkConfig.encoding.GetString(s_ByteBuffer, 0, maxLength);
            var truncOk = truncWithBadEnd.Substring(0, truncWithBadEnd.Length - 1);
            var newbyteCount = NetworkConfig.encoding.GetBytes(truncOk, 0, truncOk.Length, s_ByteBuffer, 0);

            if (overrunBehaviour == OverrunBehaviour.WarnAndTrunc)
            {
                GameDebug.LogWarning(string.Format("NetworkWriter : truncated string with {0} bytes. (result: {1})", byteCount - newbyteCount, truncOk));
            }
            byteCount = newbyteCount;
            GameDebug.Assert(byteCount <= maxLength);
        }

        m_Output.WriteByteArray(s_ByteBuffer, 0, byteCount, maxLength);
    }

    public void WriteVector2(string name, Vector2 value)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.Vector2, 32);
        m_Output.WriteUInt32(NetworkUtils.FloatToUInt32(value.x));
        m_Output.WriteUInt32(NetworkUtils.FloatToUInt32(value.y));
    }

    public void WriteVector2Q(string name, Vector2 value, int precision = 3)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.Vector2, 32, true, precision);
        m_Output.WriteUInt32((uint)Mathf.RoundToInt(value.x * NetworkConfig.encoderPrecisionScales[precision]));
        m_Output.WriteUInt32((uint)Mathf.RoundToInt(value.y * NetworkConfig.encoderPrecisionScales[precision]));
    }

    public void WriteVector3(string name, Vector3 value)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.Vector3, 32);
        m_Output.WriteUInt32(NetworkUtils.FloatToUInt32(value.x));
        m_Output.WriteUInt32(NetworkUtils.FloatToUInt32(value.y));
        m_Output.WriteUInt32(NetworkUtils.FloatToUInt32(value.z));
    }

    public void WriteVector3Q(string name, Vector3 value, int precision = 3)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.Vector3, 32, true, precision);
        m_Output.WriteUInt32((uint)Mathf.RoundToInt(value.x * NetworkConfig.encoderPrecisionScales[precision]));
        m_Output.WriteUInt32((uint)Mathf.RoundToInt(value.y * NetworkConfig.encoderPrecisionScales[precision]));
        m_Output.WriteUInt32((uint)Mathf.RoundToInt(value.z * NetworkConfig.encoderPrecisionScales[precision]));
    }

    public void WriteQuaternion(string name, Quaternion value)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.Quaternion, 32);
        m_Output.WriteUInt32(NetworkUtils.FloatToUInt32(value.x));
        m_Output.WriteUInt32(NetworkUtils.FloatToUInt32(value.y));
        m_Output.WriteUInt32(NetworkUtils.FloatToUInt32(value.z));
        m_Output.WriteUInt32(NetworkUtils.FloatToUInt32(value.w));
    }
    public void WriteQuaternionQ(string name, Quaternion value, int precision = 3)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.Quaternion, 32, true, precision);
        m_Output.WriteUInt32((uint)Mathf.RoundToInt(value.x * NetworkConfig.encoderPrecisionScales[precision]));
        m_Output.WriteUInt32((uint)Mathf.RoundToInt(value.y * NetworkConfig.encoderPrecisionScales[precision]));
        m_Output.WriteUInt32((uint)Mathf.RoundToInt(value.z * NetworkConfig.encoderPrecisionScales[precision]));
        m_Output.WriteUInt32((uint)Mathf.RoundToInt(value.w * NetworkConfig.encoderPrecisionScales[precision]));
    }

    public void WriteBytes(string name, byte[] value, int srcIndex, int count, int maxCount)
    {
        ValidateOrGenerateSchema(name, NetworkSchema.FieldType.ByteArray, 0, false, 0, maxCount);
        if (count > ushort.MaxValue)
            throw new System.ArgumentException("NetworkWriter : Byte buffer too big : " + count);
        m_Output.WriteByteArray(value, srcIndex, count, maxCount);
    }

    public void Flush()
    {
    }

    static byte[] s_ByteBuffer = new byte[1024 * 32];

    NetworkSchema m_Schema;
    NetworkSchema.FieldInfo m_CurrentField;
    ByteOutputStream m_Output;
    int m_NextFieldIndex;
    byte m_FieldMask;
    bool m_GenerateSchema;
}
