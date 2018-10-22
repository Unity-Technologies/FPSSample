using System;
using UnityEngine;


public struct DeltaReader
{
    static byte[] fieldsNotPredicted = new byte[(NetworkConfig.maxFieldsPerSchema + 7 ) / 8];
    public static int Read<TInputStream>(ref TInputStream input, NetworkSchema schema, byte[] outputData, byte[] baselineData, byte[] fieldsChangedPrediction, byte fieldMask, ref uint hash) where TInputStream : NetworkCompression.IInputStream
    {
        GameDebug.Assert(baselineData != null);
        var outputStream = new ByteOutputStream(outputData);
        var baselineStream = new ByteInputStream(baselineData);

        int numFields = schema.fields.Count;

        int skipContext = schema.id * NetworkConfig.maxContextsPerSchema + NetworkConfig.firstSchemaContext;

        for(int i = 0; i * 8 < numFields; i++)
        {
            uint value = input.ReadPackedNibble(skipContext + 2*i + 0);
            value |= input.ReadPackedNibble(skipContext + 2*i + 1) << 4;
            fieldsNotPredicted[i] = (byte)(value ^fieldsChangedPrediction[i]);
        }
       
        for (int i = 0; i < numFields; ++i)
        {
            GameDebug.Assert(schema.fields[i].byteOffset == baselineStream.GetBytePosition());
            int fieldStartContext = schema.fields[i].startContext;

            var field = schema.fields[i];

            byte fieldByteOffset = (byte)((uint)i >> 3);
            byte fieldBitOffset = (byte)((uint)i & 0x7);

            bool skip = (fieldsNotPredicted[fieldByteOffset] & (1 << fieldBitOffset)) == 0;
            bool masked = ((field.fieldMask & fieldMask) != 0);

            skip = skip || masked;

            switch (field.fieldType)
            {
                case NetworkSchema.FieldType.Bool:
                    {
                        uint value = baselineStream.ReadUInt8();
                        if(!skip)
                            value = input.ReadRawBits(1);

                        if (!masked)
                            hash = NetworkUtils.SimpleHashStreaming(hash, value);
                        
                        outputStream.WriteUInt8((byte)value);
                        break;
                    }

                case NetworkSchema.FieldType.UInt:
                case NetworkSchema.FieldType.Int:
                case NetworkSchema.FieldType.Float:
                    {
                        uint baseline = (uint)baselineStream.ReadBits(field.bits);

                        uint value = baseline;
                        if (!skip)
                        {
                            if (field.delta)
                            {
                                value = input.ReadPackedUIntDelta(baseline, fieldStartContext);
                            }
                            else
                            {
                                value = input.ReadRawBits(field.bits);
                            }
                        }

                        if (!masked)
                            hash = NetworkUtils.SimpleHashStreaming(hash, value);

                        outputStream.WriteBits(value, field.bits);  //RUTODO: fix this
                        break;
                    }

                case NetworkSchema.FieldType.Vector2:
                    {
                        uint bx = baselineStream.ReadUInt32();
                        uint by = baselineStream.ReadUInt32();

                        uint vx = bx;
                        uint vy = by;
                        if(!skip)
                        {
                            if (field.delta)
                            {
                                vx = input.ReadPackedUIntDelta(bx, fieldStartContext + 0);
                                vy = input.ReadPackedUIntDelta(by, fieldStartContext + 1);
                            }
                            else
                            {
                                vx = input.ReadRawBits(field.bits);
                                vy = input.ReadRawBits(field.bits);
                            }
                        }

                        if (!masked)
                        {
                            hash = NetworkUtils.SimpleHashStreaming(hash, vx);
                            hash = NetworkUtils.SimpleHashStreaming(hash, vy);
                        }

                        outputStream.WriteUInt32(vx);
                        outputStream.WriteUInt32(vy);

                        break;
                    }

                case NetworkSchema.FieldType.Vector3:
                    {
                        uint bx = baselineStream.ReadUInt32();
                        uint by = baselineStream.ReadUInt32();
                        uint bz = baselineStream.ReadUInt32();
                        
                        uint vx = bx;
                        uint vy = by;
                        uint vz = bz;

                        if (!skip)
                        {
                            if (field.delta)
                            {
                                vx = input.ReadPackedUIntDelta(bx, fieldStartContext + 0);
                                vy = input.ReadPackedUIntDelta(by, fieldStartContext + 1);
                                vz = input.ReadPackedUIntDelta(bz, fieldStartContext + 2);
                            }
                            else
                            {
                                vx = input.ReadRawBits(field.bits);
                                vy = input.ReadRawBits(field.bits);
                                vz = input.ReadRawBits(field.bits);
                            }
                        }

                        if (!masked)
                        {
                            hash = NetworkUtils.SimpleHashStreaming(hash, vx);
                            hash = NetworkUtils.SimpleHashStreaming(hash, vy);
                            hash = NetworkUtils.SimpleHashStreaming(hash, vz);
                        }

                        outputStream.WriteUInt32(vx);
                        outputStream.WriteUInt32(vy);
                        outputStream.WriteUInt32(vz);
                        break;
                    }

                case NetworkSchema.FieldType.Quaternion:
                    {
                        uint bx = baselineStream.ReadUInt32();
                        uint by = baselineStream.ReadUInt32();
                        uint bz = baselineStream.ReadUInt32();
                        uint bw = baselineStream.ReadUInt32();
                        
                        uint vx = bx;
                        uint vy = by;
                        uint vz = bz;
                        uint vw = bw;

                        if (!skip)
                        {
                            if(field.delta)
                            {
                                vx = input.ReadPackedUIntDelta(bx, fieldStartContext + 0);
                                vy = input.ReadPackedUIntDelta(by, fieldStartContext + 1);
                                vz = input.ReadPackedUIntDelta(bz, fieldStartContext + 2);
                                vw = input.ReadPackedUIntDelta(bw, fieldStartContext + 3);
                                //RUTODO: normalize
                            }
                            else
                            {
                                vx = input.ReadRawBits(field.bits);
                                vy = input.ReadRawBits(field.bits);
                                vz = input.ReadRawBits(field.bits);
                                vw = input.ReadRawBits(field.bits);
                            }
                        }

                        if (!masked)
                        {
                            hash = NetworkUtils.SimpleHashStreaming(hash, vx);
                            hash = NetworkUtils.SimpleHashStreaming(hash, vy);
                            hash = NetworkUtils.SimpleHashStreaming(hash, vz);
                            hash = NetworkUtils.SimpleHashStreaming(hash, vw);
                        }

                        outputStream.WriteUInt32(vx);
                        outputStream.WriteUInt32(vy);
                        outputStream.WriteUInt32(vz);
                        outputStream.WriteUInt32(vw);
                        break;
                    }

                case NetworkSchema.FieldType.String:
                case NetworkSchema.FieldType.ByteArray:
                    {
                        // TODO : Do a better job with deltaing strings and buffers
                        if (!skip)
                        {
                            baselineStream.SkipByteArray(field.arraySize);
                            outputStream.CopyByteArray<TInputStream>(ref input, field.arraySize, fieldStartContext);
                        }
                        else
                        { 
                            outputStream.CopyByteArray(ref baselineStream, field.arraySize);
                        }

                        if (!masked)
                        {
                            hash += 0; // TODO (hash strings and bytearrays as well)
                        }
                    }
                    break;
            }
        }
        outputStream.Flush();
        return outputStream.GetBytePosition();
    }
}
