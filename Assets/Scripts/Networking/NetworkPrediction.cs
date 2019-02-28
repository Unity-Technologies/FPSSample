using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;

public class NetworkPrediction
{
    // Predict snapshot from baselines. Returns true if prediction is different from baseline 0 (if it need to be automatically predicted next frame).
    unsafe public static void PredictSnapshot(uint* outputData, byte[] fieldsChangedPrediction, NetworkSchema schema, uint numBaselines, uint time0, uint* baselineData0, uint time1, uint* baselineData1, uint time2, uint* baselineData2, uint time, byte fieldMask)
    {
        for (int i = 0, l = fieldsChangedPrediction.Length; i < l; ++i)
            fieldsChangedPrediction[i] = 0;

        if (numBaselines < 3)
        {
            for(int i = 0, c = schema.GetByteSize()/4; i <c; i++)
            {
                outputData[i] = baselineData0[i];
            }
            return;
        }

        long timel = time;
        long timel0 = time0;
        long timel1 = time1;
        long timel2 = time2;

        long frac0 = 16 * (timel0 - timel2) / (timel1 - timel2);
        long frac = 16 * (timel - timel1) / (timel0 - timel1);

        fixed(uint* plans = schema.predictPlan)
        {
            int index = 0;
            for(int i = 0, c = schema.numFields; i<c; ++i)
            {
                var plan = plans[i];
                bool masked = ((fieldMask<<2) & plan) != 0;
                bool array = (plan & 1) != 0;
                bool delta = (plan & 2) != 0;

                int count = (int)(plan >> 8);
                if(array)
                {
                    for(int j = 0; j < count; ++j)
                    {
                        outputData[index + j] = baselineData0[index + j];
                    }
                    index += count;
                }
                else
                {
                    for (int j = 0; j < count; j++)
                    {
                        uint baseline0 = baselineData0[index];
                        uint baseline1 = baselineData1[index];
                        uint baseline2 = baselineData2[index];
                        uint prediction = baseline0;

                        if(!masked)
                        {
                            if (delta)
                            {
                                bool predictionLikelyWrong;
                                // Do actual prediction 
                                {
                                    predictionLikelyWrong = false;

                                    if (numBaselines < 3)
                                        prediction = baseline0;

                                    long vl2 = baseline2;
                                    long vl1 = baseline1;
                                    long vl0 = baseline0;

                                    long pl0 = vl2 + (vl1 - vl2) * frac0 / 16;// (timel0 - timel2) / (timel1 - timel2);

                                    long d1 = vl0 - pl0;
                                    long d2 = vl0 - vl1;
                                    d1 = d1 > 0 ? d1 : -d1;
                                    d2 = d2 > 0 ? d2 : -d2;
                                    if(d1 < d2)
                                    {
                                        long pl = vl1 + (vl0 - vl1) * frac / 16;// (timel - timel1) / (timel0 - timel1);
                                        predictionLikelyWrong = pl0 != vl0;
                                        prediction = (uint)pl;
                                    }
                                    else 
                                    {
                                        predictionLikelyWrong = (baseline0 != baseline1) && (baseline1 != baseline2);
                                        prediction = baseline0;
                                    }
                                }
                                if (predictionLikelyWrong)
                                    fieldsChangedPrediction[i>>3] |= (byte)(1 << (i&7));
                            }
                            else
                            {
                                if (baseline0 != baseline1 && baseline1 != baseline2)
                                    fieldsChangedPrediction[i>>3] |= (byte)(1 << (i&7));
                            }
                        }
                        outputData[index] = prediction;
                        index++;
                    }
                }
            }
        }
    }
}
