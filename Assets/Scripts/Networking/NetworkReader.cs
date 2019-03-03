using System.Collections.Generic;
using UnityEngine;


unsafe public struct NetworkReader
{
    public NetworkReader(uint* buffer, NetworkSchema schema)
    {
        m_Input = buffer;
        m_Position = 0;
        m_Schema = schema;
        m_CurrentField = null;
        m_NextFieldIndex = 0;
    }

    public bool ReadBoolean()
    {
        ValidateSchema(NetworkSchema.FieldType.Bool, 1, false);
        return m_Input[m_Position++] == 1;
    }

    public byte ReadByte()
    {
        ValidateSchema(NetworkSchema.FieldType.UInt, 8, true);
        return (byte)m_Input[m_Position++];
    }

    public short ReadInt16()
    {
        ValidateSchema(NetworkSchema.FieldType.Int, 16, true);
        return (short)m_Input[m_Position++];
    }

    public ushort ReadUInt16()
    {
        ValidateSchema(NetworkSchema.FieldType.UInt, 16, true);
        return (ushort)m_Input[m_Position++];
    }

    public int ReadInt32()
    {
        ValidateSchema(NetworkSchema.FieldType.Int, 32, true);
        return (int)m_Input[m_Position++];
    }

    public uint ReadUInt32()
    {
        ValidateSchema(NetworkSchema.FieldType.UInt, 32, true);
        return m_Input[m_Position++];
    }

    public float ReadFloat()
    {
        ValidateSchema(NetworkSchema.FieldType.Float, 32, false);
        return NetworkUtils.UInt32ToFloat(m_Input[m_Position++]);
    }

    public float ReadFloatQ()
    {
        GameDebug.Assert(m_Schema != null, "Schema required for reading quantizied values");
        ValidateSchema(NetworkSchema.FieldType.Float, 32, true);
        return (int)m_Input[m_Position++] * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
    }

    public string ReadString(int maxLength = 64)
    {
        ValidateSchema(NetworkSchema.FieldType.String, 0, false, maxLength);

        uint count = m_Input[m_Position++];
        GameDebug.Assert(count <= maxLength);
        byte* data = (byte*)(m_Input + m_Position);

        m_Position += maxLength / 4;

        if (count == 0)
            return "";

        fixed(char* dest = s_CharBuffer)
        {
            var numChars = NetworkConfig.encoding.GetChars(data, (int)count, dest, s_CharBuffer.Length);
            return new string(s_CharBuffer, 0, numChars);
        }
    }

    public Vector2 ReadVector2()
    {
        ValidateSchema(NetworkSchema.FieldType.Vector2, 32, false);

        Vector2 result;
        result.x = NetworkUtils.UInt32ToFloat(m_Input[m_Position++]);
        result.y = NetworkUtils.UInt32ToFloat(m_Input[m_Position++]);
        return result;
    }

    public Vector2 ReadVector2Q()
    {
        GameDebug.Assert(m_Schema != null, "Schema required for reading quantizied values");
        ValidateSchema(NetworkSchema.FieldType.Vector2, 32, true);

        Vector2 result;
        result.x = (int)m_Input[m_Position++] * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        result.y = (int)m_Input[m_Position++] * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        return result;
    }

    public Vector3 ReadVector3()
    {
        ValidateSchema(NetworkSchema.FieldType.Vector3, 32, false);

        Vector3 result;
        result.x = NetworkUtils.UInt32ToFloat(m_Input[m_Position++]);
        result.y = NetworkUtils.UInt32ToFloat(m_Input[m_Position++]);
        result.z = NetworkUtils.UInt32ToFloat(m_Input[m_Position++]);
        return result;
    }

    public Vector3 ReadVector3Q()
    {
        GameDebug.Assert(m_Schema != null, "Schema required for reading quantizied values");
        ValidateSchema(NetworkSchema.FieldType.Vector3, 32, true);

        Vector3 result;
        result.x = (int)m_Input[m_Position++] * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        result.y = (int)m_Input[m_Position++] * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        result.z = (int)m_Input[m_Position++] * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        return result;
    }

    public Quaternion ReadQuaternion()
    {
        ValidateSchema(NetworkSchema.FieldType.Quaternion, 32, false);

        Quaternion result;
        result.x = NetworkUtils.UInt32ToFloat(m_Input[m_Position++]);
        result.y = NetworkUtils.UInt32ToFloat(m_Input[m_Position++]);
        result.z = NetworkUtils.UInt32ToFloat(m_Input[m_Position++]);
        result.w = NetworkUtils.UInt32ToFloat(m_Input[m_Position++]);
        return result;
    }

    public Quaternion ReadQuaternionQ()
    {
        GameDebug.Assert(m_Schema != null, "Schema required for reading quantizied values");
        ValidateSchema(NetworkSchema.FieldType.Quaternion, 32, true);

        Quaternion result;
        result.x = (int)m_Input[m_Position++] * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        result.y = (int)m_Input[m_Position++] * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        result.z = (int)m_Input[m_Position++] * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        result.w = (int)m_Input[m_Position++] * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        return result;
    }

    unsafe public int ReadBytes(byte[] value, int dstIndex, int maxLength)
    {
        ValidateSchema(NetworkSchema.FieldType.ByteArray, 0, false, maxLength);
        uint count = m_Input[m_Position++];
        byte* src= (byte*)(m_Input+m_Position);
        for (int i = 0; i < count; ++i)
            value[i] = *src++;
        m_Position += maxLength / 4;
        return (int)count;
    }

    void ValidateSchema(NetworkSchema.FieldType type, int bits, bool delta, int arraySize = 0)
    {
        if (m_Schema == null)
            return;

        m_CurrentField = m_Schema.fields[m_NextFieldIndex];
        GameDebug.Assert(type == m_CurrentField.fieldType,"Property:{0} has unexpected field type:{1} Expected:{2}", m_CurrentField.name, type, m_CurrentField.fieldType);
        GameDebug.Assert(bits == m_CurrentField.bits);
        GameDebug.Assert(arraySize == m_CurrentField.arraySize);

        ++m_NextFieldIndex;
    }

    uint* m_Input;
    int m_Position;
    NetworkSchema m_Schema;
    NetworkSchema.FieldInfo m_CurrentField;
    int m_NextFieldIndex;

    static char[] s_CharBuffer = new char[1024 * 32];
}
