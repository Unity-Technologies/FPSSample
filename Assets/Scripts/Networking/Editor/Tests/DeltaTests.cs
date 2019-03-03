using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using NetworkCompression;

namespace NetcodeTests
{
    [TestFixture]
    public class DeltaTests
    {
        [Test]
        public void Delta_FloatCompressedNoBaseline_Raw()
        {
            Delta_FloatCompressedNoBaseline<RawInputStream, RawOutputStream>();
        }

        [Test]
        public void Delta_FloatCompressed_Raw()
        {
            Delta_FloatCompressed<RawInputStream, RawOutputStream>();
        }

        [Test]
        public void Delta_FloatCompressedNoDrift_Raw()
        {
            Delta_FloatCompressedNoDrift<RawInputStream, RawOutputStream>();
        }

        [Test]
        public void Delta_Vector3Compressed_Raw()
        {
            Delta_Vector3Compressed<RawInputStream, RawOutputStream>();
        }

        [Test]
        public void Delta_RandomBaseline_Raw()
        {
            Delta_RandomBaseline<RawInputStream, RawOutputStream>();
        }


        [Test]
        public void Delta_FloatCompressedNoBaseline_Huffman()
        {
            Delta_FloatCompressedNoBaseline<HuffmanInputStream, HuffmanOutputStream>();
        }

        [Test]
        public void Delta_FloatCompressed_Huffman()
        {
            Delta_FloatCompressed<HuffmanInputStream, HuffmanOutputStream>();
        }

        [Test]
        public void Delta_FloatCompressedNoDrift_Huffman()
        {
            Delta_FloatCompressedNoDrift<HuffmanInputStream, HuffmanOutputStream>();
        }

        [Test]
        public void Delta_Vector3Compressed_Huffman()
        {
            Delta_Vector3Compressed<HuffmanInputStream, HuffmanOutputStream>();
        }

        [Test]
        public void Delta_RandomBaseline_Huffman()
        {
            Delta_RandomBaseline<HuffmanInputStream, HuffmanOutputStream>();
        }

        public void Delta_FloatCompressedNoBaseline<TInputStream, TOutputStream>() where TInputStream : NetworkCompression.IInputStream, new()
                                                                                   where TOutputStream : NetworkCompression.IOutputStream, new()
        {
            var schema = new NetworkSchema(0);
            schema.AddField( new NetworkSchema.FieldInfo() { name = "field_0", fieldType = NetworkSchema.FieldType.Float, bits = 32, delta = true, precision = 3 });
            schema.Finalize();
            var values = new List<object>() { 0.123f };
            TestDelta<TInputStream,TOutputStream>(schema, values, null);
        }

        public void Delta_FloatCompressed<TInputStream, TOutputStream>() where TInputStream : NetworkCompression.IInputStream, new()
                                                                         where TOutputStream : NetworkCompression.IOutputStream, new()
        {
            var schema = new NetworkSchema(0);
            schema.AddField(new NetworkSchema.FieldInfo() { name = "field_0", fieldType = NetworkSchema.FieldType.Float, bits = 32, delta = true, precision = 3 });
            schema.Finalize();

            var values = new List<object>() { 0.637160838f };
            var baseline = new List<object>() { 0.538469732f };
            TestDelta<TInputStream, TOutputStream>(schema, values, baseline);
        }

        public void Delta_FloatCompressedNoDrift<TInputStream, TOutputStream>() where TInputStream : NetworkCompression.IInputStream, new()
                                                                                where TOutputStream : NetworkCompression.IOutputStream, new()
        {
            var schema = new NetworkSchema(0);
            schema.AddField(new NetworkSchema.FieldInfo() { name = "field_0", fieldType = NetworkSchema.FieldType.Float, bits = 32, delta = true, precision = 3 });
            schema.Finalize();

            var values = new List<object>(1);
            var baseline = new List<object>(1);

            values.Add(0.0f);
            baseline.Add(0.0f);

            for (int i = 0; i < 1024; ++i)
            {
                values[0] = (float)Math.Sin(i / 1024.0f * Math.PI);
                TestDelta<TInputStream, TOutputStream>(schema, values, baseline);
                baseline[0] = values[0];
            }
        }

        public void Delta_Vector3Compressed<TInputStream, TOutputStream>() where TInputStream : NetworkCompression.IInputStream, new()
                                                                           where TOutputStream : NetworkCompression.IOutputStream, new()
        {
            var schema = new NetworkSchema(0);
            schema.AddField(new NetworkSchema.FieldInfo() { name = "field_0", fieldType = NetworkSchema.FieldType.Vector3, bits = 32, delta = true, precision = 3 });
            schema.Finalize();

            var values = new List<object>() { new Vector3() { x = 0.07870922f, y = 0.0479902327f, z = 0.16897355f } };
            var baseline = new List<object>() { new Vector3() { x = -122.123f, y = 112.32112f, z = 0.0235f } };
            TestDelta<TInputStream, TOutputStream>(schema, values, baseline);
        }

        public void Delta_RandomBaseline<TInputStream, TOutputStream>() where TInputStream : NetworkCompression.IInputStream, new()
                                                                        where TOutputStream : NetworkCompression.IOutputStream, new()
        {
            var random = new System.Random(12091);
            for (int i = 0; i < 1024; ++i)
            {
                var schema = NetworkTestUtils.GenerateRandomSchema(64, random.Next());
                var values = NetworkTestUtils.GenerateRandomValues(schema, random.Next());
                var baselineValues = NetworkTestUtils.GenerateRandomValues(schema, random.Next());

                TestDelta<TInputStream, TOutputStream>(schema, values, baselineValues);
            }
        }

        unsafe static void TestDelta<TInputStream, TOutputStream>(NetworkSchema schema, List<object> values, List<object> baselineValues)  where TInputStream : NetworkCompression.IInputStream, new()
                                                                                                                                    where TOutputStream : NetworkCompression.IOutputStream, new()
        {
            var inputBuffer = new uint[1024 * 64];
            var baselineBuffer = new uint[1024 * 64];
            var deltaBuffer = new byte[1024 * 64];
            var outputBuffer = new uint[1024 * 64];

            NetworkTestUtils.WriteValues(values, inputBuffer, schema);

            if (baselineValues != null)
                NetworkTestUtils.WriteValues(baselineValues, baselineBuffer, schema);
            else
                baselineBuffer = new uint[1024 * 64];

            var outputStream = new TOutputStream();
            outputStream.Initialize(NetworkCompressionModel.DefaultModel, deltaBuffer, 0, null);
            uint hash = 0;
            fixed (uint* inputBufferp = inputBuffer, baselineBufferp = baselineBuffer)
            {
                DeltaWriter.Write(ref outputStream, schema, inputBufferp, baselineBufferp, zeroFieldsChanged, 0, ref hash);
            }
            outputStream.Flush();

            var inputStream = new TInputStream();
            inputStream.Initialize(NetworkCompressionModel.DefaultModel, deltaBuffer, 0);
            hash = 0;
            DeltaReader.Read(ref inputStream, schema, outputBuffer, baselineBuffer, zeroFieldsChanged, 0, ref hash);

            NetworkTestUtils.ReadAndAssertValues(values, outputBuffer, schema);
        }
        static byte[] zeroFieldsChanged = new byte[(NetworkConfig.maxFieldsPerSchema + 7) / 8];
    }
}
