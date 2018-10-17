using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NetworkPrediction
{
    public static uint PredictUint(uint numBaselines, uint t0, uint vi0, uint t1, uint vi1, uint t2, uint vi2, uint t, out bool predictionLikelyWrong)
    {
        predictionLikelyWrong = false;
        if (numBaselines < 3)
            return vi0;

        //RUTODO: implement in integer instead of double!
        GameDebug.Assert(t0 >= t1 && t1 >= t2);

        double v0 = vi0;
        double v1 = vi1;
        double v2 = vi2;

        double s0 = (t0 - t2) / (double)(t1 - t2);
        double s = (t - t1) / (double)(t0 - t1);

        double p0 = s0 * (v1 - v2) + v2;
        double p = s * (v0 - v1) + v1;


        uint r = vi0;
        if (Abs(v0 - p0) < Abs(v0 - v1))
        {
            r = (uint)p;
            predictionLikelyWrong = ((uint)p0 != vi0);
        }
        else
        {
            predictionLikelyWrong = (vi0 != vi1) && (vi1 != vi2);
        }
        //if(Game.IsServer())
        //Console.Log("" + vi2 + ", " + vi1 + ", " + vi0 + ": " + r + ": " + vref + "(" +  (int)(vref - r) + ")");

        return r;
    }

    private static double Abs(double x)
    {
        return x < 0.0 ? -x : x;
    }


    // Predict snapshot from baselines. Returns true if prediction is different from baseline 0 (if it need to be automatically predicted next frame).
    public static void PredictSnapshot(byte[] outputData, byte[] fieldsChangedPrediction, NetworkSchema schema, uint numBaselines, uint time0, byte[] baselineData0, uint time1, byte[] baselineData1, uint time2, byte[] baselineData2, uint time, byte fieldMask)
    {
        for (int i = 0, l = fieldsChangedPrediction.Length; i < l; ++i)
            fieldsChangedPrediction[i] = 0;

        if (numBaselines < 3)
        {
            System.Array.Copy(baselineData0, outputData, schema.GetByteSize());
            return;
        }

        var baselineStream0 = new ByteInputStream(baselineData0);
        var baselineStream1 = new ByteInputStream(baselineData1);
        var baselineStream2 = new ByteInputStream(baselineData2);
        var outputStream = new ByteOutputStream(outputData);

        for (int i = 0; i < schema.fields.Count; ++i)
        {
            GameDebug.Assert(schema.fields[i].byteOffset == baselineStream0.GetBytePosition());
            GameDebug.Assert(schema.fields[i].byteOffset == baselineStream1.GetBytePosition());
            GameDebug.Assert(schema.fields[i].byteOffset == baselineStream2.GetBytePosition());

            var field = schema.fields[i];

            byte fieldByteOffset = (byte)((uint)i >> 3);
            byte fieldBitOffset = (byte)((uint)i & 0x7);

            bool masked = (field.fieldMask & fieldMask) != 0;

            switch (field.fieldType)
            {
                case NetworkSchema.FieldType.Bool:
                    {
                        uint baseline0 = baselineStream0.ReadUInt8();
                        uint baseline1 = baselineStream1.ReadUInt8();
                        uint baseline2 = baselineStream2.ReadUInt8();
                        uint prediction = baseline0;

                        if (!masked)
                        {
                            if (baseline0 != baseline1 && baseline1 != baseline2)
                                fieldsChangedPrediction[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                        }

                        outputStream.WriteUInt8((byte)prediction);
                        break;
                    }

                case NetworkSchema.FieldType.UInt:
                case NetworkSchema.FieldType.Int:
                case NetworkSchema.FieldType.Float:
                    {
                        uint baseline0 = (uint)baselineStream0.ReadBits(field.bits);
                        uint baseline1 = (uint)baselineStream1.ReadBits(field.bits);
                        uint baseline2 = (uint)baselineStream2.ReadBits(field.bits);

                        uint prediction = baseline0;
                        if (!masked)
                        {
                            if (field.delta)
                            {
                                bool predictionLikelyWrong;
                                prediction = NetworkPrediction.PredictUint(numBaselines, time0, baseline0, time1, baseline1, time2, baseline2, time, out predictionLikelyWrong);
                                if (predictionLikelyWrong)
                                    fieldsChangedPrediction[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                            else
                            {
                                if (baseline0 != baseline1 && baseline1 != baseline2)
                                    fieldsChangedPrediction[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                        }

                        outputStream.WriteBits(prediction, field.bits);  //RUTODO: fix this
                        break;
                    }

                case NetworkSchema.FieldType.Vector2:
                    {
                        uint bx0 = baselineStream0.ReadUInt32();
                        uint by0 = baselineStream0.ReadUInt32();

                        uint bx1 = baselineStream1.ReadUInt32();
                        uint by1 = baselineStream1.ReadUInt32();

                        uint bx2 = baselineStream2.ReadUInt32();
                        uint by2 = baselineStream2.ReadUInt32();

                        uint px = bx0;
                        uint py = by0;
                        if (!masked)
                        {
                            if (field.delta)
                            {
                                bool predictionLikelyWrongX;
                                bool predictionLikelyWrongY;
                                px = NetworkPrediction.PredictUint(numBaselines, time0, bx0, time1, bx1, time2, bx2, time, out predictionLikelyWrongX);
                                py = NetworkPrediction.PredictUint(numBaselines, time0, by0, time1, by1, time2, by2, time, out predictionLikelyWrongY);
                                if (predictionLikelyWrongX || predictionLikelyWrongY)
                                    fieldsChangedPrediction[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                            else
                            {
                                if ((bx0 != bx1 || by0 != by1) && (bx1 != bx2 || by1 != by2))
                                    fieldsChangedPrediction[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                        }

                        outputStream.WriteUInt32(px);
                        outputStream.WriteUInt32(py);
                        break;
                    }

                case NetworkSchema.FieldType.Vector3:
                    {
                        uint bx0 = baselineStream0.ReadUInt32();
                        uint by0 = baselineStream0.ReadUInt32();
                        uint bz0 = baselineStream0.ReadUInt32();

                        uint bx1 = baselineStream1.ReadUInt32();
                        uint by1 = baselineStream1.ReadUInt32();
                        uint bz1 = baselineStream1.ReadUInt32();

                        uint bx2 = baselineStream2.ReadUInt32();
                        uint by2 = baselineStream2.ReadUInt32();
                        uint bz2 = baselineStream2.ReadUInt32();

                        uint px = bx0;
                        uint py = by0;
                        uint pz = bz0;

                        if (!masked)
                        {
                            if (field.delta)
                            {
                                bool predictionLikelyWrongX;
                                bool predictionLikelyWrongY;
                                bool predictionLikelyWrongZ;
                                px = NetworkPrediction.PredictUint(numBaselines, time0, bx0, time1, bx1, time2, bx2, time, out predictionLikelyWrongX);
                                py = NetworkPrediction.PredictUint(numBaselines, time0, by0, time1, by1, time2, by2, time, out predictionLikelyWrongY);
                                pz = NetworkPrediction.PredictUint(numBaselines, time0, bz0, time1, bz1, time2, bz2, time, out predictionLikelyWrongZ);

                                if (predictionLikelyWrongX || predictionLikelyWrongY || predictionLikelyWrongZ)
                                    fieldsChangedPrediction[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                            else
                            {
                                if ((bx0 != bx1 || by0 != by1 || bz0 != bz1) && (bx1 != bx2 || by1 != by2 || bz1 != bz2))
                                    fieldsChangedPrediction[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                        }

                        outputStream.WriteUInt32(px);
                        outputStream.WriteUInt32(py);
                        outputStream.WriteUInt32(pz);
                        break;
                    }

                case NetworkSchema.FieldType.Quaternion:
                    {
                        uint bx0 = baselineStream0.ReadUInt32();
                        uint by0 = baselineStream0.ReadUInt32();
                        uint bz0 = baselineStream0.ReadUInt32();
                        uint bw0 = baselineStream0.ReadUInt32();

                        uint bx1 = baselineStream1.ReadUInt32();
                        uint by1 = baselineStream1.ReadUInt32();
                        uint bz1 = baselineStream1.ReadUInt32();
                        uint bw1 = baselineStream1.ReadUInt32();

                        uint bx2 = baselineStream2.ReadUInt32();
                        uint by2 = baselineStream2.ReadUInt32();
                        uint bz2 = baselineStream2.ReadUInt32();
                        uint bw2 = baselineStream2.ReadUInt32();

                        uint px = bx0;
                        uint py = by0;
                        uint pz = bz0;
                        uint pw = bw0;

                        if (!masked)
                        {
                            if (field.delta)
                            {
                                bool predictionLikelyWrongX;
                                bool predictionLikelyWrongY;
                                bool predictionLikelyWrongZ;
                                bool predictionLikelyWrongW;
                                px = NetworkPrediction.PredictUint(numBaselines, time0, bx0, time1, bx1, time2, bx2, time, out predictionLikelyWrongX);
                                py = NetworkPrediction.PredictUint(numBaselines, time0, by0, time1, by1, time2, by2, time, out predictionLikelyWrongY);
                                pz = NetworkPrediction.PredictUint(numBaselines, time0, bz0, time1, bz1, time2, bz2, time, out predictionLikelyWrongZ);
                                pw = NetworkPrediction.PredictUint(numBaselines, time0, bw0, time1, bw1, time2, bw2, time, out predictionLikelyWrongW);

                                if (predictionLikelyWrongX || predictionLikelyWrongY || predictionLikelyWrongZ || predictionLikelyWrongW)
                                    fieldsChangedPrediction[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                            else
                            {
                                if ((bx0 != bx1 || by0 != by1 || bz0 != bz1 || bw0 != bw1) && (bx1 != bx2 || by1 != by2 || bz1 != bz2 || bw1 != bw2))
                                    fieldsChangedPrediction[fieldByteOffset] |= (byte)(1 << fieldBitOffset);
                            }
                        }

                        outputStream.WriteUInt32(px);
                        outputStream.WriteUInt32(py);
                        outputStream.WriteUInt32(pz);
                        outputStream.WriteUInt32(pw);
                        break;
                    }

                case NetworkSchema.FieldType.String:
                case NetworkSchema.FieldType.ByteArray:
                    {
                        baselineStream1.SkipByteArray(field.arraySize);
                        baselineStream2.SkipByteArray(field.arraySize);
                        outputStream.CopyByteArray(ref baselineStream0, field.arraySize);
                        //TODO: predict me!
                    }
                    break;
            }
        }

        //fieldsChangedPrediction = 0;

        outputStream.Flush();
    }
}
