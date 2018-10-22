using System.Collections.Generic;
using UnityEngine;


public struct NetworkReader
{
    public NetworkReader(byte[] buffer, NetworkSchema schema)
    {
        m_Input = new ByteInputStream(buffer);
        m_Schema = schema;
        m_CurrentField = null;
        m_NextFieldIndex = 0;
    }

    public bool ReadBoolean()
    {
        ValidateSchema(NetworkSchema.FieldType.Bool, 1, false);
        return m_Input.ReadUInt8() == 1;
    }

    public byte ReadByte()
    {
        ValidateSchema(NetworkSchema.FieldType.UInt, 8, true);
        return m_Input.ReadUInt8();
    }

    public short ReadInt16()
    {
        ValidateSchema(NetworkSchema.FieldType.Int, 16, true);
        return (short)m_Input.ReadUInt16();
    }

    public ushort ReadUInt16()
    {
        ValidateSchema(NetworkSchema.FieldType.UInt, 16, true);
        return (ushort)m_Input.ReadUInt16();
    }

    public int ReadInt32()
    {
        ValidateSchema(NetworkSchema.FieldType.Int, 32, true);
        return (int)m_Input.ReadUInt32();
    }

    public uint ReadUInt32()
    {
        ValidateSchema(NetworkSchema.FieldType.UInt, 32, true);
        return m_Input.ReadUInt32();
    }

    public float ReadFloat()
    {
        ValidateSchema(NetworkSchema.FieldType.Float, 32, false);
        return NetworkUtils.UInt32ToFloat(m_Input.ReadUInt32());
    }

    public float ReadFloatQ()
    {
        GameDebug.Assert(m_Schema != null, "Schema required for reading quantizied values");
        ValidateSchema(NetworkSchema.FieldType.Float, 32, true);
        return (int)m_Input.ReadUInt32() * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
    }

    public string ReadString(int maxLength = 64)
    {
        ValidateSchema(NetworkSchema.FieldType.String, 0, false, maxLength);

        byte[] buffer;
        int srcIndex;
        int count;
        m_Input.GetByteArray(out buffer, out srcIndex, out count, maxLength);
        GameDebug.Assert(count < short.MaxValue);

        if (count == 0)
            return "";

        var numChars = NetworkConfig.encoding.GetChars(buffer, srcIndex, count, s_CharBuffer, 0);
        return new string(s_CharBuffer, 0, numChars);
    }

    public Vector2 ReadVector2()
    {
        ValidateSchema(NetworkSchema.FieldType.Vector2, 32, false);

        Vector2 result;
        result.x = NetworkUtils.UInt32ToFloat(m_Input.ReadUInt32());
        result.y = NetworkUtils.UInt32ToFloat(m_Input.ReadUInt32());
        return result;
    }

    public Vector2 ReadVector2Q()
    {
        GameDebug.Assert(m_Schema != null, "Schema required for reading quantizied values");
        ValidateSchema(NetworkSchema.FieldType.Vector2, 32, true);

        Vector2 result;
        result.x = (int)m_Input.ReadUInt32() * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        result.y = (int)m_Input.ReadUInt32() * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        return result;
    }

    public Vector3 ReadVector3()
    {
        ValidateSchema(NetworkSchema.FieldType.Vector3, 32, false);

        Vector3 result;
        result.x = NetworkUtils.UInt32ToFloat(m_Input.ReadUInt32());
        result.y = NetworkUtils.UInt32ToFloat(m_Input.ReadUInt32());
        result.z = NetworkUtils.UInt32ToFloat(m_Input.ReadUInt32());
        return result;
    }

    public Vector3 ReadVector3Q()
    {
        GameDebug.Assert(m_Schema != null, "Schema required for reading quantizied values");
        ValidateSchema(NetworkSchema.FieldType.Vector3, 32, true);

        Vector3 result;
        result.x = (int)m_Input.ReadUInt32() * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        result.y = (int)m_Input.ReadUInt32() * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        result.z = (int)m_Input.ReadUInt32() * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        return result;
    }

    public Quaternion ReadQuaternion()
    {
        ValidateSchema(NetworkSchema.FieldType.Quaternion, 32, false);

        Quaternion result;
        result.x = NetworkUtils.UInt32ToFloat(m_Input.ReadUInt32());
        result.y = NetworkUtils.UInt32ToFloat(m_Input.ReadUInt32());
        result.z = NetworkUtils.UInt32ToFloat(m_Input.ReadUInt32());
        result.w = NetworkUtils.UInt32ToFloat(m_Input.ReadUInt32());
        return result;
    }

    public Quaternion ReadQuaternionQ()
    {
        GameDebug.Assert(m_Schema != null, "Schema required for reading quantizied values");
        ValidateSchema(NetworkSchema.FieldType.Quaternion, 32, true);

        Quaternion result;
        result.x = (int)m_Input.ReadUInt32() * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        result.y = (int)m_Input.ReadUInt32() * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        result.z = (int)m_Input.ReadUInt32() * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        result.w = (int)m_Input.ReadUInt32() * NetworkConfig.decoderPrecisionScales[m_CurrentField.precision];
        return result;
    }

    public int ReadBytes(byte[] value, int dstIndex, int maxLength)
    {
        ValidateSchema(NetworkSchema.FieldType.ByteArray, 0, false, maxLength);
        return m_Input.ReadByteArray(value, dstIndex, maxLength);
    }

    public bool ReadCheck()
    {
        // TODO : Add conditional so we can ship without this
        var check = m_Input.ReadUInt32();
        return check == 0x12345678;
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

    ByteInputStream m_Input;
    NetworkSchema m_Schema;
    NetworkSchema.FieldInfo m_CurrentField;
    int m_NextFieldIndex;

    static char[] s_CharBuffer = new char[1024 * 32];
}
