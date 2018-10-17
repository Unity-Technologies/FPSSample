public static class StopWatchExtensions
{
    public static float GetTicksDeltaAsMilliseconds(this System.Diagnostics.Stopwatch stopWatch, long previousTicks)
    {
        return (float)((double)(stopWatch.ElapsedTicks - previousTicks) / FrequencyMilliseconds);
    }

    public static long FrequencyMilliseconds = System.Diagnostics.Stopwatch.Frequency / 1000;
}
