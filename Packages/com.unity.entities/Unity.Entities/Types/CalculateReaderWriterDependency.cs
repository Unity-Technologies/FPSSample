using Unity.Collections;

namespace Unity.Entities
{
    internal static class CalculateReaderWriterDependency
    {
        public static bool Add(ComponentType type, NativeList<int> reading, NativeList<int> writing)
        {
            if (!type.RequiresJobDependency)
                return false;

            if (type.AccessModeType == ComponentType.AccessMode.ReadOnly)
            {
                if (reading.Contains(type.TypeIndex))
                    return false;
                if (writing.Contains(type.TypeIndex))
                    return false;

                reading.Add(type.TypeIndex);
                return true;
            }

            var readingIndex = reading.IndexOf(type.TypeIndex);
            if (readingIndex != -1)
                reading.RemoveAtSwapBack(readingIndex);
            if (writing.Contains(type.TypeIndex))
                return false;

            writing.Add(type.TypeIndex);
            return true;
        }
    }
}
