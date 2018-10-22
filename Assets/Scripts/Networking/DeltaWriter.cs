using UnityEngine;


public struct DeltaWriter
{
    static byte[] fieldsNotPredicted = new byte[(NetworkConfig.maxFieldsPerSchema + 7) / 8];
    static public unsafe void Write<TOutputStream>(ref TOutputStream output, NetworkSchema schema, byte[] inputData, byte[] baselineData, byte[] fieldsChangedPrediction, byte fieldMask, ref uint entity_hash) where TOutputStream : NetworkCompression.IOutputStream
    {
        GameDebug.Assert(baselineData != null);
        var inputStream = new ByteInputStream(inputData);
        var baselineStream = new ByteInputStream(baselineData);

        int numFields = schema.fields.Count;
        GameDebug.Assert(fieldsChangedPrediction.Length >= numFields / 8, "Not enough bits in fieldsChangedPrediction for all fields");

        for (int i = 0, l = fieldsNotPredicted.Length; i < l; ++i)
            fieldsNotPredicted[i] = 0;

        // calculate bitmask of fields that need to be encoded
        for (int fieldIndex = 0; fieldIndex < schema.fields.Count; ++fieldIndex)
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
                        uint value = inputStream.ReadBits(1);
                        uint baseline = baselineStream.ReadUInt8();

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
                        uint value = inputStream.ReadBits(field.bits);
                        uint baseline = (uint)baselineStream.ReadBits(field.bits);

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
                        uint value = inputStream.ReadBits(field.bits);
                        uint baseline = (uint)baselineStream.ReadBits(field.bits);

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
                        uint value = inputStream.ReadBits(field.bits);
                        uint baseline = (uint)baselineStream.ReadBits(field.bits);

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
                        uint vx = inputStream.ReadBits(field.bits);
                        uint vy = inputStream.ReadBits(field.bits);

                        uint bx = baselineStream.ReadUInt32();
                        uint by = baselineStream.ReadUInt32();

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
                        uint vx = inputStream.ReadBits(field.bits);
                        uint vy = inputStream.ReadBits(field.bits);
                        uint vz = inputStream.ReadBits(field.bits);

                        uint bx = baselineStream.ReadUInt32();
                        uint by = baselineStream.ReadUInt32();
                        uint bz = baselineStream.ReadUInt32();

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
                        uint vx = inputStream.ReadBits(field.bits);
                        uint vy = inputStream.ReadBits(field.bits);
                        uint vz = inputStream.ReadBits(field.bits);
                        uint vw = inputStream.ReadBits(field.bits);

                        uint bx = baselineStream.ReadUInt32();
                        uint by = baselineStream.ReadUInt32();
                        uint bz = baselineStream.ReadUInt32();
                        uint bw = baselineStream.ReadUInt32();

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
                        // TODO : Do a better job of string and buffer diffs?
                        byte[] valueBuffer;
                        int valueOffset;
                        int valueLength;
                        inputStream.GetByteArray(out valueBuffer, out valueOffset, out valueLength, field.arraySize);

                        byte[] baselineBuffer = null;
                        int baselineOffset = 0;
                        int baselineLength = 0;
                        baselineStream.GetByteArray(out baselineBuffer, out baselineOffset, out baselineLength, field.arraySize);

                        if (!masked)
                        {
                            entity_hash += 0; // TODO client side has no easy way to hash strings. enable this when possible: NetworkUtils.SimpleHash(valueBuffer, valueLength);
                            if (valueLength != baselineLength || NetworkUtils.MemCmp(valueBuffer, valueOffset, baselineBuffer, baselineOffset, valueLength) != 0)
                            {
                                fieldsNotPredicted[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                        }
                    }
                    break;
            }
        }
    
        inputStream.Reset();
        baselineStream.Reset();

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
                        uint value = inputStream.ReadBits(1);
                        /*uint unused_baseline = */baselineStream.ReadUInt8();

                        if(notPredicted)
                        {
                            output.WriteRawBits(value, 1);
                            NetworkSchema.AddStatsToFieldBool(field, (value != 0), false, output.GetBitPosition2() - startBitPosition);
                        }
                        break;
                    }

                case NetworkSchema.FieldType.Int:
                    {
                        uint value = inputStream.ReadBits(field.bits);
                        uint baseline = (uint)baselineStream.ReadBits(field.bits);

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
                        uint value = inputStream.ReadBits(field.bits);
                        uint baseline = (uint)baselineStream.ReadBits(field.bits);

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
                        uint value = inputStream.ReadBits(field.bits);
                        uint baseline = (uint)baselineStream.ReadBits(field.bits);

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
                        uint vx = inputStream.ReadBits(field.bits);
                        uint vy = inputStream.ReadBits(field.bits);

                        uint bx = baselineStream.ReadUInt32();
                        uint by = baselineStream.ReadUInt32();

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
                        uint vx = inputStream.ReadBits(field.bits);
                        uint vy = inputStream.ReadBits(field.bits);
                        uint vz = inputStream.ReadBits(field.bits);

                        uint bx = baselineStream.ReadUInt32();
                        uint by = baselineStream.ReadUInt32();
                        uint bz = baselineStream.ReadUInt32();

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
                        uint vx = inputStream.ReadBits(field.bits);
                        uint vy = inputStream.ReadBits(field.bits);
                        uint vz = inputStream.ReadBits(field.bits);
                        uint vw = inputStream.ReadBits(field.bits);

                        uint bx = baselineStream.ReadUInt32();
                        uint by = baselineStream.ReadUInt32();
                        uint bz = baselineStream.ReadUInt32();
                        uint bw = baselineStream.ReadUInt32();

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
                        // TODO : Do a better job of string and buffer diffs?
                        byte[] valueBuffer;
                        int valueOffset;
                        int valueLength;
                        inputStream.GetByteArray(out valueBuffer, out valueOffset, out valueLength, field.arraySize);

                        byte[] baselineBuffer = null;
                        int baselineOffset = 0;
                        int baselineLength = 0;
                        baselineStream.GetByteArray(out baselineBuffer, out baselineOffset, out baselineLength, field.arraySize);

                        if(notPredicted)
                        {
                            output.WritePackedUInt((uint)valueLength, fieldStartContext);
                            output.WriteRawBytes(valueBuffer, valueOffset, valueLength);

                            if(field.fieldType == NetworkSchema.FieldType.String)
                            {
                                NetworkSchema.AddStatsToFieldString(field, valueBuffer, valueOffset, valueLength, output.GetBitPosition2() - startBitPosition);
                            }
                            else
                            {
                                NetworkSchema.AddStatsToFieldByteArray(field, valueBuffer, valueOffset, valueLength, output.GetBitPosition2() - startBitPosition);
                            }
                        }
                    }
                    break;
            }
        }
    }
}
