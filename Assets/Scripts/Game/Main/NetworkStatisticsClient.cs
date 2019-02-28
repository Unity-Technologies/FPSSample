using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class NetworkStatisticsClient
{
    public FloatRollingAverage rtt { get { return m_RTT; } }

    public bool notifyHardCatchup;        // Set to true to record hard catchup for this update

    public NetworkStatisticsClient(NetworkClient networkClient)
    {
        m_NetworkClient = networkClient;
        //for (var i = 0; i < m_PackageContentStatistics.Length; i++)
        //m_PackageContentStatistics[i] = new int[(int)NetworkCompressionReader.Type._NumTypes];
    }

    public void Update(float frameTimeScale, float interpTime)
    {
        Profiler.BeginSample("NetworkStatisticsClient.Update");

        var clientCounters = m_NetworkClient.counters;

        // Grab package content stats into our circular buffer
        //if (clientCounters != null && m_PackageContentStatisticsLastSequence < clientCounters.packageContentStatsPackageSequence)
        //{
        //    m_PackageContentStatisticsLastSequence = clientCounters.packageContentStatsPackageSequence;
        //    var i = m_PackageContentStatisticsLastSequence % m_PackageContentStatistics.Length;
        //    var packStats = clientCounters.packageContentStats;
        //    if (packStats != null)
        //        packStats.CopyTo(m_PackageContentStatistics[i], 0);
        //}


        m_FrameTimeScale = frameTimeScale;
        m_StatsDeltaTime.Update(Time.deltaTime);

        m_HardCatchup.Add(notifyHardCatchup ? 100 : 0);
        notifyHardCatchup = false;

        m_ServerSimTime.Update(m_NetworkClient.serverSimTime);

        m_BytesIn.Update(clientCounters != null ? clientCounters.bytesIn : 0);
        m_PackagesIn.Update(clientCounters != null ? clientCounters.packagesIn : 0);

        m_HeaderBitsIn.Update(clientCounters != null ? clientCounters.headerBitsIn : 0);

        m_BytesOut.Update(clientCounters != null ? clientCounters.bytesOut : 0);
        m_PackagesOut.Update(clientCounters != null ? clientCounters.packagesOut : 0);

        m_Latency.Update(m_NetworkClient.timeSinceSnapshot);
        m_RTT.Update(m_NetworkClient.rtt);
        m_CMDQ.Update(m_NetworkClient.lastAcknowlegdedCommandTime - m_NetworkClient.serverTime);
        m_Interp.Update(interpTime * 1000);

        m_SnapshotsIn.Update(clientCounters != null ? clientCounters.snapshotsIn : 0);
        m_CommandsOut.Update(clientCounters != null ? clientCounters.commandsOut : 0);
        m_EventsIn.Update(clientCounters != null ? clientCounters.eventsIn : 0);
        m_EventsOut.Update(clientCounters != null ? clientCounters.eventsOut : 0);

        // Calculate package loss pct
        if (clientCounters != null && Time.time > m_NextLossCalc)
        {
            m_NextLossCalc = Time.time + 0.2f;

            var packagesIn = clientCounters.packagesIn - m_PackageCountPrevIn;
            m_PackageCountPrevIn = clientCounters.packagesIn;

            var loss = clientCounters.packagesLostIn - m_PackageLossPrevIn;
            m_PackageLossPrevIn = clientCounters.packagesLostIn;

            var totalIn = packagesIn + loss;
            m_PackagesLostPctIn = totalIn != 0 ? loss * 100 / totalIn : 0;

            var packagesOut = clientCounters.packagesOut - m_PackageCountPrevOut;
            m_PackageCountPrevOut = clientCounters.packagesOut;

            loss = clientCounters.packagesLostOut - m_PackageLossPrevOut;
            m_PackageLossPrevOut = clientCounters.packagesLostOut;

            var totalOut = packagesOut + loss;
            m_PackagesLostPctOut = totalOut != 0 ? loss * 100 / totalOut : 0;
        }

        m_PackageLossPctIn.Update(m_PackagesLostPctIn);
        m_PackageLossPctOut.Update(m_PackagesLostPctOut);

        switch (NetworkConfig.netStats.IntValue)
        {
            case 1: DrawCompactStats(); break;
            case 2: DrawStats(); break;
            case 3: DrawCounters(); break;
            case 4: DrawPackageStatistics(); break;
        }

        if (NetworkConfig.netPrintStats.IntValue > 0)
        {
            if (Time.frameCount % NetworkConfig.netPrintStats.IntValue == 0)
            {
                PrintStats();
            }
        }

        // Pass on a few key stats to gamestatistics
        if (Game.game.m_GameStatistics != null)
        {
            Game.game.m_GameStatistics.rtt = Mathf.RoundToInt(m_RTT.average);
        }

        Profiler.EndSample();
    }

    void PrintStats()
    {
        GameDebug.Log("Network stats");
        GameDebug.Log("=============");
        GameDebug.Log("Tick rate : " + Game.serverTickRate.IntValue);
        GameDebug.Log("clientID  : " + m_NetworkClient.clientId);
        GameDebug.Log("rtt       : " + m_NetworkClient.rtt);
        GameDebug.Log("LastPkgSeq: " + m_NetworkClient.counters.packageContentStatsPackageSequence);
        GameDebug.Log("ServerTime: " + m_NetworkClient.serverTime);
        Console.Write("-------------------");
    }

    float bitheight = 0.01f;
    void DrawPackageStatistics()
    {
        float x = DebugOverlay.Width - 20;
        float y = DebugOverlay.Height - 8;
        float dx = 1.0f;  // bar spacing
        float w = 1.0f;  // width of bars
        int maxbits = 0;
        var stats = m_NetworkClient.counters.packageContentStats;
        var last = m_NetworkClient.counters.packagesIn;
        for (var i = last; i > 0 && i > last - stats.Length; --i)
        {
            var s = stats[i % stats.Length];
            if (s == null)
                continue;
            var barx = x + (i-last) * dx;

            for(int j = 0, c = s.Count; j < c; ++j)
            {
                var stat = s[j];
                DebugOverlay.DrawRect(barx, y - (stat.sectionStart + stat.sectionLength) * bitheight, w, stat.sectionLength * bitheight, stat.color);
            }

            var lastStat = s[s.Count - 1];
            if (lastStat.sectionStart + lastStat.sectionLength > maxbits)
                maxbits = lastStat.sectionStart + lastStat.sectionLength;
        }

        int maxbytes = (maxbits + 7) / 8;
        int step = Mathf.Max(1, maxbytes >> 4) * 16;
        for (var i = 0; i <= maxbytes; i += step)
        {
            DebugOverlay.Write(x - 4, y - i * 8 * bitheight - 0.5f, "{0:###}b", i);
        }

        bitheight = Mathf.Min(0.01f, 10.0f / maxbits);
    }

    void DrawCompactStats()
    {
        var samplesPerSecond = 1.0f / m_StatsDeltaTime.average;

        DebugOverlay.Write(-50, -4, "pps (in/out): {0} / {1}", m_PackagesIn.graph.average * samplesPerSecond, m_PackagesOut.graph.average * samplesPerSecond);
        DebugOverlay.Write(-50, -3, "bps (in/out): {0:00.0} / {1:00.0}", m_BytesIn.graph.average * samplesPerSecond, m_BytesOut.graph.average * samplesPerSecond);
        var startIndex = m_BytesIn.graph.GetData().HeadIndex;
        DebugOverlay.DrawHist(-50, -2, 20, 2, m_BytesIn.graph.GetData().GetArray(), startIndex, Color.blue, 5.0f);
    }

    void DrawStats()
    {
        var samplesPerSecond = 1.0f / m_StatsDeltaTime.average;
        int y = 2;
        DebugOverlay.Write(2, y++, "  tick rate: {0}", m_NetworkClient.serverTickRate);
        DebugOverlay.Write(2, y++, "  frame timescale: {0}", m_FrameTimeScale);

        DebugOverlay.Write(2, y++, "  sim  : {0:0.0} / {1:0.0} / {2:0.0} ({3:0.0})",
            m_ServerSimTime.min,
            m_ServerSimTime.min,
            m_ServerSimTime.max,
            m_ServerSimTime.stdDeviation);

        DebugOverlay.Write(2, y++, "^FF0  lat  : {0:0.0} / {1:0.0} / {2:0.0}", m_Latency.min, m_Latency.average, m_Latency.max);
        DebugOverlay.Write(2, y++, "^0FF  rtt  : {0:0.0} / {1:0.0} / {2:0.0}", m_RTT.min, m_RTT.average, m_RTT.max);
        DebugOverlay.Write(2, y++, "^0F0  cmdq : {0:0.0} / {1:0.0} / {2:0.0}", m_CMDQ.min, m_CMDQ.average, m_CMDQ.max);
        DebugOverlay.Write(2, y++, "^F0F  intp : {0:0.0} / {1:0.0} / {2:0.0}", m_Interp.min, m_Interp.average, m_Interp.max);


        y++;
        DebugOverlay.Write(2, y++, "^22F  header/payload/total bps (in):");
        DebugOverlay.Write(2, y++, "^22F   {0:00.0} / {1:00.0} / {2:00.0} ({3})", m_HeaderBitsIn.graph.average / 8.0f * samplesPerSecond,
                                                                                    (m_BytesIn.graph.average - m_HeaderBitsIn.graph.average / 8.0f) * samplesPerSecond,
                                                                                    m_BytesIn.graph.average * samplesPerSecond,
                                                                                    m_NetworkClient.clientConfig.serverUpdateRate);
        DebugOverlay.Write(2, y++, "^F00  bps (out): {0:00.0}", m_BytesOut.graph.average * samplesPerSecond);
        DebugOverlay.Write(2, y++, "  pps (in):  {0:00.0}", m_PackagesIn.graph.average * samplesPerSecond);
        DebugOverlay.Write(2, y++, "  pps (out): {0:00.0}", m_PackagesOut.graph.average * samplesPerSecond);
        DebugOverlay.Write(2, y++, "  pl% (in):  {0:00.0}", m_PackageLossPctIn.average);
        DebugOverlay.Write(2, y++, "  pl% (out): {0:00.0}", m_PackageLossPctOut.average);

        y++;
        DebugOverlay.Write(2, y++, "  upd_srate: {0:00.0} ({1})", m_SnapshotsIn.graph.average * samplesPerSecond, m_NetworkClient.clientConfig.serverUpdateInterval);
        DebugOverlay.Write(2, y++, "  cmd_srate: {0:00.0} ({1})", m_CommandsOut.graph.average * samplesPerSecond, m_NetworkClient.serverTickRate);
        DebugOverlay.Write(2, y++, "  ev (in):   {0:00.0}", m_EventsIn.graph.average * samplesPerSecond);
        DebugOverlay.Write(2, y++, "  ev (out):  {0:00.0}", m_EventsOut.graph.average * samplesPerSecond);

        var startIndex = m_BytesIn.graph.GetData().HeadIndex;
        var graphY = 5;

        DebugOverlay.DrawGraph(38, graphY, 60, 5, m_Latency.GetData().GetArray(), startIndex, Color.yellow, 100);
        DebugOverlay.DrawGraph(38, graphY, 60, 5, m_RTT.GetData().GetArray(), startIndex, Color.cyan, 100);
        DebugOverlay.DrawGraph(38, graphY, 60, 5, m_CMDQ.GetData().GetArray(), startIndex, Color.green, 10);
        DebugOverlay.DrawGraph(38, graphY, 60, 5, m_Interp.GetData().GetArray(), startIndex, Color.magenta, 100);
        DebugOverlay.DrawHist(38, graphY, 60, 5, m_HardCatchup.GetArray(), startIndex, Color.red, 100);

        m_2GraphData[0] = m_BytesIn.graph.GetData().GetArray();
        m_2GraphData[1] = m_BytesOut.graph.GetData().GetArray();

        graphY += 7;
        DebugOverlay.DrawGraph(38, graphY, 60, 5, m_2GraphData, startIndex, m_BytesGraphColors);

        graphY += 6;
        DebugOverlay.DrawHist(38, graphY++, 60, 1, m_SnapshotsIn.graph.GetData().GetArray(), startIndex, Color.blue, 5.0f);
        DebugOverlay.DrawHist(38, graphY++, 60, 1, m_CommandsOut.graph.GetData().GetArray(), startIndex, Color.red, 5.0f);
        DebugOverlay.DrawHist(38, graphY++, 60, 1, m_EventsIn.graph.GetData().GetArray(), startIndex, Color.yellow, 5.0f);
        DebugOverlay.DrawHist(38, graphY++, 60, 1, m_EventsOut.graph.GetData().GetArray(), startIndex, Color.green, 5.0f);
    }

    void DrawCounters()
    {
        var counters = m_NetworkClient.counters;
        if (counters == null)
            return;

        int y = 2;
        DebugOverlay.Write(2, y++, "  Bytes in     : {0}", counters.bytesIn);
        DebugOverlay.Write(2, y++, "  Bytes out    : {0}", counters.bytesOut);
        DebugOverlay.Write(2, y++, "  Packages in  : {0}", counters.packagesIn);
        DebugOverlay.Write(2, y++, "  Packages out : {0}", counters.packagesOut);

        y++;
        DebugOverlay.Write(2, y++, "  Stale packages        : {0}", counters.packagesStaleIn);
        DebugOverlay.Write(2, y++, "  Duplicate packages    : {0}", counters.packagesDuplicateIn);
        DebugOverlay.Write(2, y++, "  Out of order packages : {0}", counters.packagesOutOfOrderIn);

        y++;
        DebugOverlay.Write(2, y++, "  Lost packages in      : {0}", counters.packagesLostIn);
        DebugOverlay.Write(2, y++, "  Lost packages out     : {0}", counters.packagesLostOut);

        y++;
        DebugOverlay.Write(2, y++, "  Fragmented packages in       : {0}", counters.fragmentedPackagesIn);
        DebugOverlay.Write(2, y++, "  Fragmented packages out      : {0}", counters.fragmentedPackagesOut);

        DebugOverlay.Write(2, y++, "  Fragmented packages lost in  : {0}", counters.fragmentedPackagesLostIn);
        DebugOverlay.Write(2, y++, "  Fragmented packages lost out : {0}", counters.fragmentedPackagesLostOut);

        y++;
        DebugOverlay.Write(2, y++, "  Choked packages lost : {0}", counters.chokedPackagesOut);
    }

    float m_FrameTimeScale;
    NetworkClient m_NetworkClient;

    float[][] m_2GraphData = new float[2][];
    Color[] m_BytesGraphColors = new Color[] { Color.blue, Color.red };
    //int[][] m_PackageContentStatistics = new int[k_NumPackageContentStats][];
    //int m_PackageContentStatisticsLastSequence;

    float m_NextLossCalc;

    float m_PackagesLostPctIn;
    int m_PackageCountPrevIn;
    int m_PackageLossPrevIn;

    float m_PackagesLostPctOut;
    int m_PackageCountPrevOut;
    int m_PackageLossPrevOut;

    const int k_WindowSize = 120;
    const int k_NumPackageContentStats = 16;

    class Aggregator
    {
        public float previousValue;
        public FloatRollingAverage graph = new FloatRollingAverage(k_WindowSize);

        public void Update(float value)
        {
            graph.Update(value - previousValue);
            previousValue = value;
        }
    }

    FloatRollingAverage m_StatsDeltaTime = new FloatRollingAverage(k_WindowSize);

    FloatRollingAverage m_ServerSimTime = new FloatRollingAverage(k_WindowSize);

    FloatRollingAverage m_Latency = new FloatRollingAverage(k_WindowSize);
    FloatRollingAverage m_RTT = new FloatRollingAverage(k_WindowSize);
    FloatRollingAverage m_CMDQ = new FloatRollingAverage(k_WindowSize);
    FloatRollingAverage m_Interp = new FloatRollingAverage(k_WindowSize);
    CircularList<float> m_HardCatchup = new CircularList<float>(k_WindowSize);

    Aggregator m_BytesIn = new Aggregator();
    Aggregator m_PackagesIn = new Aggregator();

    Aggregator m_HeaderBitsIn = new Aggregator();

    Aggregator m_SnapshotsIn = new Aggregator();
    Aggregator m_EventsIn = new Aggregator();

    Aggregator m_BytesOut = new Aggregator();
    Aggregator m_PackagesOut = new Aggregator();

    FloatRollingAverage m_PackageLossPctIn = new FloatRollingAverage(k_WindowSize);
    FloatRollingAverage m_PackageLossPctOut = new FloatRollingAverage(k_WindowSize);

    Aggregator m_CommandsOut = new Aggregator();
    Aggregator m_EventsOut = new Aggregator();
}
