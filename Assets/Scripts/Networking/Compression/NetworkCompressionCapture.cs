using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NetworkCompression
{
    public class NetworkCompressionCapture
    {
        public NetworkCompressionCapture(int numContexts, NetworkCompressionModel model)
        {
            m_Model = model;
            uintData = new List<uint>[numContexts];
            nibbleData = new List<byte>[numContexts];
            rawData = new List<byte>();
            for (int i = 0; i < numContexts; i++)
            {
                uintData[i] = new List<uint>();
                nibbleData[i] = new List<byte>();
            }
        }

        public void AddUInt(int context, uint value)
        {
            uintData[context].Add(value);
            rawData.Add((byte)((value) & 0xFF));
            rawData.Add((byte)((value >> 8) & 0xFF));
            rawData.Add((byte)((value >> 16) & 0xFF));
            rawData.Add((byte)((value >> 24) & 0xFF));
        }

        public void AddNibble(int context, uint value)
        {
            Debug.Assert(value < 16);
            nibbleData[context].Add((byte)value);
            rawData.Add((byte)value);
        }
        
        public byte[] AnalyzeAndGenerateModel()
        {
            int alphabetSize = 16;
            var model = m_Model;
            int numContexts = uintData.Length;
            const int numBuckets = NetworkCompressionConstants.k_NumBuckets;

            var stringWriter = new StringWriter();

            int[] histogram = new int[alphabetSize];
            int[] safeHistogram = new int[alphabetSize];
            int[,] histogram2 = new int[alphabetSize, alphabetSize];
            int[,] safeHistogram2 = new int[alphabetSize, alphabetSize];

            Directory.CreateDirectory("capture");
            stringWriter.WriteLine("NetworkProfile:");
            var combinedSOAFile = System.IO.File.OpenWrite("capture/combined_soa.dat");

            List<byte> modelData = new List<byte>();

            modelData.Add(16);
            modelData.AddRange(new byte[] { 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6, 6, 6, 6 });

            int numContextsOffset = modelData.Count;
            modelData.Add(0);   //num contexts
            modelData.Add(0);

            int totalNumValues = 0;
            int gammaTotalCost = 0;
            int currentTotalCost = 0;
            int optimizedTotalCost = 0;
            double entropyTotalCost = 0.0;
            double entropy2TotalCost = 0.0;
            int numUsedContexts = 0;
            List<string> optimizedTrees = new List<string>();
            for (int context = 0; context < numContexts; context++)
            {
                if (uintData[context].Count == 0 && nibbleData[context].Count == 0)
                    continue;


                bool isUInt = uintData[context].Count > 0;

                
                int numValues = 0;
                if (isUInt)
                {
                    Debug.Assert(nibbleData[context].Count == 0);
                    alphabetSize = numBuckets;
                    numValues = uintData[context].Count;
                }
                else
                {
                    Debug.Assert(uintData[context].Count == 0);
                    Debug.Assert(nibbleData[context].Count > 0);
                    alphabetSize = 16;
                    numValues = nibbleData[context].Count;
                }

                // build histograms
                for (int i = 0; i < numBuckets; i++)
                    histogram[i] = 0;

                for (int i = 0; i < alphabetSize; i++)
                    for (int j = 0; j < alphabetSize; j++)
                        histogram2[i, j] = 0;

                combinedSOAFile.WriteByte((byte)(numValues & 0xFF));
                combinedSOAFile.WriteByte((byte)((numValues >> 8) & 0xFF));
                combinedSOAFile.WriteByte((byte)((numValues >> 16) & 0xFF));
                combinedSOAFile.WriteByte((byte)((numValues >> 24) & 0xFF));

                var contextFile = System.IO.File.OpenWrite("capture/context" + context);

                int gammaCost = 0;
                int prevSymbol = 0;
                for (int i = 0; i < numValues; i++)
                {
                    uint value;
                    if (isUInt)
                    {
                        value = uintData[context][i];
                        int bucket = NetworkCompressionUtils.CalculateBucket(value);
                        histogram[bucket]++;
                        histogram2[prevSymbol, bucket]++;
                        prevSymbol = bucket;
                    }
                    else
                    {
                        value = nibbleData[context][i];
                        histogram[value]++;
                        histogram2[prevSymbol, value]++;
                        prevSymbol = (int)value;
                    }

                    gammaCost += NetworkCompressionUtils.CalculateNumGammaBits(value);

                    combinedSOAFile.WriteByte((byte)(value & 0xFF));
                    combinedSOAFile.WriteByte((byte)((value >> 8) & 0xFF));
                    combinedSOAFile.WriteByte((byte)((value >> 16) & 0xFF));
                    combinedSOAFile.WriteByte((byte)((value >> 24) & 0xFF));

                    contextFile.WriteByte((byte)(value & 0xFF));
                    contextFile.WriteByte((byte)((value >> 8) & 0xFF));
                    contextFile.WriteByte((byte)((value >> 16) & 0xFF));
                    contextFile.WriteByte((byte)((value >> 24) & 0xFF));
                }
                contextFile.Close();

                // safe histogram where all values have at least one occurrence
                int safeNumValues = numValues;
                for(int i = 0; i < alphabetSize; i++)
                {
                    int n = histogram[i];
                    if (n == 0)
                    {
                        n = 1;
                        safeNumValues++;
                    }
                    safeHistogram[i] = n;
                }


                byte[] optimizedSymbolLengths = new byte[alphabetSize];
                NetworkCompressionUtils.GenerateLengthLimitedHuffmanCodeLengths(optimizedSymbolLengths, 0, safeHistogram, alphabetSize, NetworkCompressionConstants.k_MaxHuffmanSymbolLength);
                modelData.Add((byte)(context & 0xFF));
                modelData.Add((byte)(context >> 8));
                modelData.Add((byte)alphabetSize);
                for (int i = 0; i < alphabetSize; i++)
                    modelData.Add((byte)optimizedSymbolLengths[i]);

                int currentCost = 0;
                int optimizedCost = 0;
                double entropyCost = 0.0;
                
                for (int i = 0; i < alphabetSize; i++)
                {
                    int n = histogram[i];
                    if (n > 0)
                    {
                        int currentBitLength = model.encodeTable[context, i] & 0xFF;
                        int optimizedBitLength = optimizedSymbolLengths[i] & 0xFF;
                        currentCost += n * currentBitLength;
                        optimizedCost += n * optimizedBitLength;
                        double p = n / (double)safeNumValues;
                        entropyCost += n * -Math.Log(p, 2.0);
                        if (isUInt)
                        {
                            currentCost += n * NetworkCompressionConstants.k_BucketSizes[i];
                            optimizedCost += n * NetworkCompressionConstants.k_BucketSizes[i];
                            entropyCost += n * NetworkCompressionConstants.k_BucketSizes[i];
                        }
                    }
                }

                double entropy2Cost = 0.0;
                for (int i = 0; i < alphabetSize; i++)
                {
                    int total = 0;
                    for (int j = 0; j < alphabetSize; j++)
                    {
                        int n = histogram2[i, j];
                        if (n == 0)
                        {
                            n = 1;
                        }
                        safeHistogram2[i, j] = n;
                        total += n;
                    }

                    for (int j = 0; j < alphabetSize; j++)
                    {
                        int n = histogram2[i, j];
                        if(n > 0)
                        {
                            double p = n / (double)total;
                            entropy2Cost += n * -Math.Log(p, 2.0);
                            if (isUInt)
                                entropy2Cost += n * NetworkCompressionConstants.k_BucketSizes[j];
                        }
                    }
                }
                
                totalNumValues += numValues;
                gammaTotalCost += gammaCost;
                currentTotalCost += currentCost;
                optimizedTotalCost += optimizedCost;
                entropyTotalCost += entropyCost;
                entropy2TotalCost += entropy2Cost;
                var l = new List<byte>(optimizedSymbolLengths);
                string symLengths = string.Join(":", l);
                stringWriter.WriteLine("{0,4}:   {1,8} {2,8:0.00} {3,8:0.00} {4,8:0.00} {5,8:0.00} {6,8:0.00}   {7}", context, numValues, gammaCost / 8.0f, currentCost / 8.0f, optimizedCost / 8.0f, entropyCost / 8.0, entropy2Cost / 8.0, symLengths);
                optimizedTrees.Add(""+string.Format("{0,10:000000}", currentCost-optimizedCost)+" " + symLengths);

                numUsedContexts++;
            }

            optimizedTrees.Sort();
            foreach(var l in optimizedTrees)
                GameDebug.Log("  " + l);
            
            stringWriter.WriteLine("Total: {0,8} {1,8:0.00} {2,8:0.00} {3,8:0.00} {4,8:0.00} {5,8:0.00}", totalNumValues, gammaTotalCost / 8.0f, currentTotalCost / 8.0f, optimizedTotalCost / 8.0f, entropyTotalCost / 8.0, entropy2TotalCost / 8.0);
            stringWriter.WriteLine("Num used contexts: {0}", numUsedContexts);
            GameDebug.Log(stringWriter.ToString());

            combinedSOAFile.Close();

            System.IO.File.WriteAllBytes("capture/combined_aos.dat", rawData.ToArray());


            modelData[numContextsOffset + 0] = (byte)(numUsedContexts & 0xFF);
            modelData[numContextsOffset + 1] = (byte)(numUsedContexts >> 8);

            return modelData.ToArray();
        }


        public NetworkCompressionModel m_Model;
        List<uint>[] uintData;
        List<byte>[] nibbleData;
        List<byte> rawData;
    }
}