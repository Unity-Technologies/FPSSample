using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


public struct DeltaWriter
{
    static byte[] fieldsNotPredicted = new byte[(NetworkConfig.maxFieldsPerSchema + 7) / 8];
    unsafe static public void Write<TOutputStream>(ref TOutputStream output, NetworkSchema schema, uint* inputData, uint* baselineData, byte[] fieldsChangedPrediction, byte fieldMask, ref uint entity_hash) where TOutputStream : NetworkCompression.IOutputStream
    {
        GameDebug.Assert(baselineData != null);

        int numFields = schema.numFields;
        GameDebug.Assert(fieldsChangedPrediction.Length >= numFields / 8, "Not enough bits in fieldsChangedPrediction for all fields");

        for (int i = 0, l = fieldsNotPredicted.Length; i < l; ++i)
            fieldsNotPredicted[i] = 0;

        int index = 0;

        // calculate bitmask of fields that need to be encoded
        for (int fieldIndex = 0; fieldIndex < numFields; ++fieldIndex)
        {
            var field = schema.fields[fieldIndex];

            // Skip fields that are masked out
            bool masked = (field.fieldMask & fieldMask) != 0;

            byte fieldByteOffset = (byte)((uint)fieldIndex >> 3);
            byte fieldBitOffset = (byte)((uint)fieldIndex & 0x7);

            switch (field.fieldType)
            {
                case NetworkSchema.FieldType.Bool:
                    {
                        uint value = inputData[index];
                        uint baseline = baselineData[index];
                        index++;

                        if(!masked)
                        {
                            entity_hash = NetworkUtils.SimpleHashStreaming(entity_hash, value);
                            if (value != baseline)
                            {
                                fieldsNotPredicted[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                        }

                        break;
                    }

                case NetworkSchema.FieldType.Int:
                    {
                        uint value = inputData[index];
                        uint baseline = baselineData[index];
                        index++;

                        if (!masked)
                        {
                            entity_hash = NetworkUtils.SimpleHashStreaming(entity_hash, value);
                            if (value != baseline)
                            {
                                fieldsNotPredicted[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                        }
                        break;
                    }
                case NetworkSchema.FieldType.UInt:
                    {
                        uint value = inputData[index];
                        uint baseline = baselineData[index];
                        index++;

                        if (!masked)
                        {
                            entity_hash = NetworkUtils.SimpleHashStreaming(entity_hash, value);
                            if (value != baseline)
                            {
                                fieldsNotPredicted[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                        }
                        break;
                    }
                case NetworkSchema.FieldType.Float:
                    {
                        uint value = inputData[index];
                        uint baseline = baselineData[index];
                        index++;

                        if (!masked)
                        {
                            entity_hash = NetworkUtils.SimpleHashStreaming(entity_hash, value);
                            if (value != baseline)
                            {
                                fieldsNotPredicted[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                        }
                        break;
                    }

                case NetworkSchema.FieldType.Vector2:
                    {
                        uint vx = inputData[index];
                        uint bx = baselineData[index];
                        index++;

                        uint vy = inputData[index];
                        uint by = baselineData[index];
                        index++;

                        if (!masked)
                        {
                            entity_hash = NetworkUtils.SimpleHashStreaming(entity_hash, vx);
                            entity_hash = NetworkUtils.SimpleHashStreaming(entity_hash, vy);
                            if (vx != bx || vy != by)
                            {
                                fieldsNotPredicted[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                        }
                        break;
                    }

                case NetworkSchema.FieldType.Vector3:
                    {
                        uint vx = inputData[index];
                        uint bx = baselineData[index];
                        index++;

                        uint vy = inputData[index];
                        uint by = baselineData[index];
                        index++;

                        uint vz = inputData[index];
                        uint bz = baselineData[index];
                        index++;

                        if (!masked)
                        {
                            entity_hash = NetworkUtils.SimpleHashStreaming(entity_hash, vx);
                            entity_hash = NetworkUtils.SimpleHashStreaming(entity_hash, vy);
                            entity_hash = NetworkUtils.SimpleHashStreaming(entity_hash, vz);
                            if (vx != bx || vy != by || vz != bz)
                            {
                                fieldsNotPredicted[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                        }
                        break;
                    }


                case NetworkSchema.FieldType.Quaternion:
                    {
                        uint vx = inputData[index];
                        uint bx = baselineData[index];
                        index++;

                        uint vy = inputData[index];
                        uint by = baselineData[index];
                        index++;

                        uint vz = inputData[index];
                        uint bz = baselineData[index];
                        index++;

                        uint vw = inputData[index];
                        uint bw = baselineData[index];
                        index++;



                        if (!masked)
                        {
                            entity_hash = NetworkUtils.SimpleHashStreaming(entity_hash, vx);
                            entity_hash = NetworkUtils.SimpleHashStreaming(entity_hash, vy);
                            entity_hash = NetworkUtils.SimpleHashStreaming(entity_hash, vz);
                            entity_hash = NetworkUtils.SimpleHashStreaming(entity_hash, vw);
                            if (vx != bx || vy != by || vz != bz || vw != bw)
                            {
                                fieldsNotPredicted[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                        }
                        break;
                    }


                case NetworkSchema.FieldType.String:
                case NetworkSchema.FieldType.ByteArray:
                    {
                        if (!masked)
                        {
                            entity_hash += 0; // TODO client side has no easy way to hash strings. enable this when possible: NetworkUtils.SimpleHash(valueBuffer, valueLength);
                            bool same = true;
                            for(int i = 0; i < field.arraySize; i++)
                            {
                                if(inputData[index+i] != baselineData[index+i])
                                {
                                    same = false;
                                    break;
                                }
                            }
                            if (!same)
                            {
                                fieldsNotPredicted[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                        }
                        index += field.arraySize/4 + 1;
                    }
                    break;
            }
        }

        index = 0;

        int skipContext = schema.id * NetworkConfig.maxContextsPerSchema + NetworkConfig.firstSchemaContext;

        // Client needs fieldsNotPredicted. We send the delta between it and fieldsChangedPrediction
        {
            for(int i = 0; i * 8 < numFields; i++)
            {
                byte deltaFields = (byte) (fieldsNotPredicted[i] ^ fieldsChangedPrediction[i]);
                output.WritePackedNibble((uint)(deltaFields & 0xF), skipContext + i*2);
                output.WritePackedNibble((uint)((deltaFields>>4) & 0xF), skipContext + i*2 + 1);
            }
        }
        
        int startBitPosition = 0;
        for (int fieldIndex = 0; fieldIndex < numFields; ++fieldIndex)
        {
            var field = schema.fields[fieldIndex];
            int fieldStartContext = field.startContext;
            startBitPosition = output.GetBitPosition2();

            byte fieldByteOffset = (byte)((uint)fieldIndex >> 3);
            byte fieldBitOffset = (byte)((uint)fieldIndex & 0x7);
            var notPredicted = ((fieldsNotPredicted[fieldByteOffset] & (1 << fieldBitOffset)) != 0);

            switch (field.fieldType)
            {
                case NetworkSchema.FieldType.Bool:
                    {
                        uint value = inputData[index];
                        index++;

                        if(notPredicted)
                        {
                            output.WriteRawBits(value, 1);
                            NetworkSchema.AddStatsToFieldBool(field, (value != 0), false, output.GetBitPosition2() - startBitPosition);
                        }
                        break;
                    }

                case NetworkSchema.FieldType.Int:
                    {
                        uint value = inputData[index];
                        uint baseline = baselineData[index];
                        index++;

                        if(notPredicted)
                        {
                            if (field.delta)
                            {
                                output.WritePackedUIntDelta(value, baseline, fieldStartContext);
                                NetworkSchema.AddStatsToFieldInt(field, (int)value, (int)baseline, output.GetBitPosition2() - startBitPosition);
                            }
                            else
                            {
                                output.WriteRawBits(value, field.bits);
                                NetworkSchema.AddStatsToFieldInt(field, (int)value, 0, output.GetBitPosition2() - startBitPosition);
                            }
                        }
                        break;
                    }
                case NetworkSchema.FieldType.UInt:
                    {
                        uint value = inputData[index];
                        uint baseline = baselineData[index];
                        index++;

                        if(notPredicted)
                        {
                            if (field.delta)
                            {
                                output.WritePackedUIntDelta(value, baseline, fieldStartContext);
                                NetworkSchema.AddStatsToFieldUInt(field, value, baseline, output.GetBitPosition2() - startBitPosition);
                            }
                            else
                            {
                                output.WriteRawBits(value, field.bits);
                                NetworkSchema.AddStatsToFieldUInt(field, value, 0, output.GetBitPosition2() - startBitPosition);
                            }
                        }
                        break;
                    }
                case NetworkSchema.FieldType.Float:
                    {
                        uint value = inputData[index];
                        uint baseline = baselineData[index];
                        index++;

                        if(notPredicted)
                        {
                            if (field.delta)
                            {
                                output.WritePackedUIntDelta(value, baseline, fieldStartContext);
                                NetworkSchema.AddStatsToFieldFloat(field, value, baseline, output.GetBitPosition2() - startBitPosition);
                            }
                            else
                            {
                                output.WriteRawBits(value, field.bits);
                                NetworkSchema.AddStatsToFieldFloat(field, value, 0, output.GetBitPosition2() - startBitPosition);
                            }
                        }
                        break;
                    }

                case NetworkSchema.FieldType.Vector2:
                    {
                        uint vx = inputData[index];
                        uint bx = baselineData[index];
                        index++;

                        uint vy = inputData[index];
                        uint by = baselineData[index];
                        index++;

                        if(notPredicted)
                        {
                            if (field.delta)
                            {
                                output.WritePackedUIntDelta(vx, bx, fieldStartContext + 0);
                                output.WritePackedUIntDelta(vy, by, fieldStartContext + 1);
                                NetworkSchema.AddStatsToFieldVector2(field, vx, vy, bx, by, output.GetBitPosition2() - startBitPosition);
                            }
                            else
                            {
                                output.WriteRawBits(vx, field.bits);
                                output.WriteRawBits(vy, field.bits);
                                NetworkSchema.AddStatsToFieldVector2(field, vx, vy, 0, 0, output.GetBitPosition2() - startBitPosition);
                            }
                        }
                        break;
                    }

                case NetworkSchema.FieldType.Vector3:
                    {
                        uint vx = inputData[index];
                        uint bx = baselineData[index];
                        index++;

                        uint vy = inputData[index];
                        uint by = baselineData[index];
                        index++;

                        uint vz = inputData[index];
                        uint bz = baselineData[index];
                        index++;

                        if(notPredicted)
                        {
                            if (field.delta)
                            {
                                output.WritePackedUIntDelta(vx, bx, fieldStartContext + 0);
                                output.WritePackedUIntDelta(vy, by, fieldStartContext + 1);
                                output.WritePackedUIntDelta(vz, bz, fieldStartContext + 2);
                                NetworkSchema.AddStatsToFieldVector3(field, vx, vy, vz, bx, by, bz, output.GetBitPosition2() - startBitPosition);
                            }
                            else
                            {
                                output.WriteRawBits(vx, field.bits);
                                output.WriteRawBits(vy, field.bits);
                                output.WriteRawBits(vz, field.bits);
                                NetworkSchema.AddStatsToFieldVector3(field, vx, vy, vz, 0, 0, 0, output.GetBitPosition2() - startBitPosition);
                            }
                        }
                        break;
                    }


                case NetworkSchema.FieldType.Quaternion:
                    {
                        // TODO : Figure out what to do with quaternions
                        uint vx = inputData[index];
                        uint bx = baselineData[index];
                        index++;

                        uint vy = inputData[index];
                        uint by = baselineData[index];
                        index++;

                        uint vz = inputData[index];
                        uint bz = baselineData[index];
                        index++;

                        uint vw = inputData[index];
                        uint bw = baselineData[index];
                        index++;

                        if(notPredicted)
                        {
                            if(field.delta)
                            {
                                output.WritePackedUIntDelta(vx, bx, fieldStartContext + 0);
                                output.WritePackedUIntDelta(vy, by, fieldStartContext + 1);
                                output.WritePackedUIntDelta(vz, bz, fieldStartContext + 2);
                                output.WritePackedUIntDelta(vw, bw, fieldStartContext + 3);
                                NetworkSchema.AddStatsToFieldQuaternion(field, vx, vy, vz, vw, bx, by, bz, bw, output.GetBitPosition2() - startBitPosition);
                            }
                            else
                            {
                                output.WriteRawBits(vx, field.bits);
                                output.WriteRawBits(vy, field.bits);
                                output.WriteRawBits(vz, field.bits);
                                output.WriteRawBits(vw, field.bits);
                                NetworkSchema.AddStatsToFieldQuaternion(field, vx, vy, vz, vw, 0, 0, 0, 0, output.GetBitPosition2() - startBitPosition);
                            }
                        }
                        break;
                    }


                case NetworkSchema.FieldType.String:
                case NetworkSchema.FieldType.ByteArray:
                    {
                        uint valueLength = inputData[index];
                        index++;

                        if(notPredicted)
                        {
                            output.WritePackedUInt(valueLength, fieldStartContext);
                            byte* bytes = (byte*)(inputData + index);
                            output.WriteRawBytes(bytes, (int)valueLength);

                            if(field.fieldType == NetworkSchema.FieldType.String)
                            {
                                NetworkSchema.AddStatsToFieldString(field, bytes, (int)valueLength, output.GetBitPosition2() - startBitPosition);
                            }
                            else
                            {
                                NetworkSchema.AddStatsToFieldByteArray(field, bytes, (int)valueLength, output.GetBitPosition2() - startBitPosition);
                            }
                        }
                        index += field.arraySize / 4;
                    }
                    break;
            }
        }
    }
}
