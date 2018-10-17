using System.ComponentModel;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Unity.Entities.Tests
{
    public class NativeArraySharedValuesTests
    {
        [Test]
        public void NativeArraySharedValuesResultInOrderNoRemainder()
        {
            int count = 1024;
            var source = new NativeArray<int>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                source[i] = count - (i / 2);
            }
            var sharedValues = new NativeArraySharedValues<int>(source, Allocator.TempJob);
            var sharedValuesJobHandle = sharedValues.Schedule(default(JobHandle));
            sharedValuesJobHandle.Complete();
            var sortedIndices = sharedValues.GetSortedIndices();

            var lastIndex = sortedIndices[0];
            var lastValue = source[lastIndex];

            for (int i = 1; i < count; i++)
            {
                var index = sortedIndices[i];
                var value = source[index];

                Assert.GreaterOrEqual(value,lastValue);

                lastIndex = index;
                lastValue = value;
            }
            sharedValues.Dispose();
            source.Dispose();
        }

        [Test]
        public void NativeArraySharedValuesResultInOrderLargeRemainder()
        {
            int count = 1024 + 1023;
            var source = new NativeArray<int>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                source[i] = count - (i / 2);
            }
            var sharedValues = new NativeArraySharedValues<int>(source, Allocator.TempJob);
            var sharedValuesJobHandle = sharedValues.Schedule(default(JobHandle));
            sharedValuesJobHandle.Complete();
            var sortedIndices = sharedValues.GetSortedIndices();

            var lastIndex = sortedIndices[0];
            var lastValue = source[lastIndex];

            for (int i = 1; i < count; i++)
            {
                var index = sortedIndices[i];
                var value = source[index];

                Assert.GreaterOrEqual(value,lastValue);

                lastIndex = index;
                lastValue = value;
            }
            sharedValues.Dispose();
            source.Dispose();
        }

        [Test]
        public void NativeArraySharedValuesFoundAllValues()
        {
            int count = 1024 + 1023;
            // int count = 32 + 31;
            var source = new NativeArray<int>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                source[i] = count - (i / 2);
            }
            var sharedValues = new NativeArraySharedValues<int>(source, Allocator.TempJob);
            var sharedValuesJobHandle = sharedValues.Schedule(default(JobHandle));
            sharedValuesJobHandle.Complete();

            var sortedIndices = sharedValues.GetSortedIndices();
            for (int i = 0; i < count; i++)
            {
                var foundValue = false;
                for (int j = 0; j < sortedIndices.Length; j++)
                {
                    if (sortedIndices[j] == i)
                    {
                        foundValue = true;
                        break;
                    }
                }
                Assert.AreEqual(true, foundValue);
            }
            sharedValues.Dispose();
            source.Dispose();
        }

        [Test]
        public void NativeArraySharedValuesSameValues()
        {
            int count = 1024 + 1023;
            var source = new NativeArray<int>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                source[i] = count - (i / 2);
            }
            var sharedValues = new NativeArraySharedValues<int>(source, Allocator.TempJob);
            var sharedValuesJobHandle = sharedValues.Schedule(default(JobHandle));
            sharedValuesJobHandle.Complete();

            for (int i = 0; i < count; i++)
            {
                var sharedValueIndices = sharedValues.GetSharedValueIndicesBySourceIndex(i);
                var sourceValue = source[i];
                Assert.GreaterOrEqual(sharedValueIndices.Length,1);
                for (int j = 0; j < sharedValueIndices.Length; j++)
                {
                    var otherIndex = sharedValueIndices[j];
                    var otherValue = source[otherIndex];
                    Assert.AreEqual(sourceValue,otherValue);
                }
            }
            sharedValues.Dispose();
            source.Dispose();
        }
    }
}
