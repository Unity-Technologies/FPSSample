using System;
using UnityEngine;


public struct DeltaReader
{
    static byte[] fieldsNotPredicted = new byte[(NetworkConfig.maxFieldsPerSchema + 7 ) / 8];
    unsafe public static int Read<TInputStream>(ref TInputStream input, NetworkSchema schema, uint[] outputData, uint[] baselineData, byte[] fieldsChangedPrediction, byte fieldMask, ref uint hash) where TInputStream : NetworkCompression.IInputStream
    {
        GameDebug.Assert(baselineData != null);

        var index = 0;

        int numFields = schema.numFields;

        int skipContext = schema.id * NetworkConfig.maxContextsPerSchema + NetworkConfig.firstSchemaContext;

        for(int i = 0; i * 8 < numFields; i++)
        {
            uint value = input.ReadPackedNibble(skipContext + 2*i + 0);
            value |= input.ReadPackedNibble(skipContext + 2*i + 1) << 4;
            fieldsNotPredicted[i] = (byte)(value ^fieldsChangedPrediction[i]);
        }
       
        for (int i = 0; i < numFields; ++i)
        {
            var field = schema.fields[i];

            GameDebug.Assert(field.byteOffset == index * 4);
            int fieldStartContext = field.startContext;


            byte fieldByteOffset = (byte)((uint)i >> 3);
            byte fieldBitOffset = (byte)((uint)i & 0x7);

            bool skip = (fieldsNotPredicted[fieldByteOffset] & (1 << fieldBitOffset)) == 0;
            bool masked = ((field.fieldMask & fieldMask) != 0);

            skip = skip || masked;

            switch (field.fieldType)
            {
                case NetworkSchema.FieldType.Bool:
                    {
                        uint value = baselineData[index];
                        if(!skip)
                            value = input.ReadRawBits(1);

                        if (!masked)
                            hash = NetworkUtils.SimpleHashStreaming(hash, value);

                        outputData[index] = value;
                        index++;
                        break;
                    }

                case NetworkSchema.FieldType.UInt:
                case NetworkSchema.FieldType.Int:
                case NetworkSchema.FieldType.Float:
                    {
                        uint baseline = (uint)baselineData[index];

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

                        outputData[index] = value;
                        index++;
                        break;
                    }

                case NetworkSchema.FieldType.Vector2:
                    {
                        uint bx = baselineData[index];
                        uint by = baselineData[index + 1];

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

                        outputData[index] = vx;
                        outputData[index + 1] = vy;
                        index += 2;

                        break;
                    }

                case NetworkSchema.FieldType.Vector3:
                    {
                        uint bx = baselineData[index];
                        uint by = baselineData[index+1];
                        uint bz = baselineData[index+2];
                        
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

                        outputData[index] = vx;
                        outputData[index+1] = vy;
                        outputData[index+2] = vz;
                        index += 3;
                        break;
                    }

                case NetworkSchema.FieldType.Quaternion:
                    {
                        uint bx = baselineData[index];
                        uint by = baselineData[index+1];
                        uint bz = baselineData[index+2];
                        uint bw = baselineData[index+3];
                        
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

                        outputData[index] = vx;
                        outputData[index+1] = vy;
                        outputData[index+2] = vz;
                        outputData[index+3] = vw;
                        index += 4;
                        break;
                    }

                case NetworkSchema.FieldType.String:
                case NetworkSchema.FieldType.ByteArray:
                    {
                        // TODO : Do a better job with deltaing strings and buffers
                        if (!skip)
                        {
                            uint count = input.ReadPackedUInt(fieldStartContext);
                            outputData[index] = count;
                            index++;
                            fixed(uint* buf = outputData)
                            {
                                byte* dst = (byte*)(buf + index);
                                int idx = 0;
                                for (; idx < count; ++idx)
                                    *dst++ = (byte)input.ReadRawBits(8);
                                for (; idx < field.arraySize / 4; ++idx)
                                    *dst++ = 0;
                            }
                            index += field.arraySize / 4;
                        }
                        else
                        {
                            for (int idx = 0, c = field.arraySize / 4; idx < c; ++idx)
                                outputData[index + idx] = baselineData[index + idx];
                            index += field.arraySize / 4 + 1;
                        }

                        if (!masked)
                        {
                            hash += 0; // TODO (hash strings and bytearrays as well)
                        }
                    }
                    break;
            }
        }
        return index * 4;
    }
}
