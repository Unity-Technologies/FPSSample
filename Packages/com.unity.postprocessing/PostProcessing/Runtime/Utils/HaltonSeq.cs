namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Halton sequence utility.
    /// </summary>
    public static class HaltonSeq
    {
        /// <summary>
        /// Gets a value from the Halton sequence for a given index and radix.
        /// </summary>
        /// <param name="index">The sequence index</param>
        /// <param name="radix">The sequence base</param>
        /// <returns>A number from the Halton sequence between 0 and 1.</returns>
        public static float Get(int index, int radix)
        {
            float result = 0f;
            float fraction = 1f / (float)radix;

            while (index > 0)
            {
                result += (float)(index % radix) * fraction;

                index /= radix;
                fraction /= (float)radix;
            }

            return result;
        }
    }
}
