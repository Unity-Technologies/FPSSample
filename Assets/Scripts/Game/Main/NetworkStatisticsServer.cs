using UnityEngine;

internal class NetworkStatisticsServer
{
    private NetworkServer m_NetworkServer;

    public NetworkStatisticsServer(NetworkServer networkServer)
    {
        m_NetworkServer = networkServer;
    }

    public void Update()
    {
        m_StatsDeltaTime.Update(Time.deltaTime);

        m_ServerSimTime.Update(m_NetworkServer.serverSimTime);

        /*
        var counters = m_NetworkServer.GetCounters();
        m_BytesIn.Update(clientCounters != null ? clientCounters.bytesIn : 0);
        m_PackagesIn.Update(clientCounters != null ? clientCounters.packagesIn : 0);
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
        */

        switch (NetworkConfig.netStats.IntValue)
        {
            case 1: DrawStats(); break;
            case 2: DrawCounters(); break;
        }

        if(NetworkConfig.netPrintStats.IntValue > 0)
        {
            UpdateStats();
            if(Time.frameCount % NetworkConfig.netPrintStats.IntValue == 0)
            {
                PrintStats();
            }
        }
    }

    void UpdateStats()
    {
        foreach(var client in m_NetworkServer.GetConnections())
            client.Value.counters.UpdateAverages();
    }

    double lastStatsTime = 0;
    void PrintStats()
    {
        double timePassed = Game.frameTime - lastStatsTime;
        lastStatsTime = Game.frameTime;
        GameDebug.Log("Network stats");
        GameDebug.Log("=============");
        GameDebug.Log("Tick rate  : " + Game.serverTickRate.IntValue);
        GameDebug.Log("Num netents: " + m_NetworkServer.NumEntities);
        Console.Write("--------------");
        Console.Write("Connections:");
        Console.Write("------------");
        Console.Write(string.Format("   {0,2} {1,-5}, {2,-5} {3,-5} {4,-5} {5,-5} {6,-5} {7,-5} {8,-5} {9,-5} {10,-5}",
            "ID", "RTT", "ISEQ", "ITIM", "OSEQ", "OACK", "ppsI", "bpsI", "ppsO", "bpsO", "frag"));
        Console.Write("-------------------");
        int byteOutSum = 0;
        int byteOutCount = 0;
        foreach(var c in m_NetworkServer.GetConnections())
        {
            var client = c.Value;
            Console.Write(string.Format("   {0:00} {1,5} {2,5} {3,5} {4,5} {5,5} {6:00.00} {7,5} {8:00.00} {9,5} {10,5}",
                client.connectionId, client.rtt, client.inSequence, client.inSequenceTime, client.outSequence, client.outSequenceAck,
                (client.counters.avgPackagesIn.graph.average * Game.serverTickRate.FloatValue),
                (int)(client.counters.avgBytesIn.graph.average * Game.serverTickRate.FloatValue),
                (client.counters.avgPackagesOut.graph.average * Game.serverTickRate.FloatValue),
                (int)(client.counters.avgBytesOut.graph.average * Game.serverTickRate.FloatValue),
                client.counters.fragmentedPackagesOut
                ));
            byteOutSum += (int)(client.counters.avgBytesOut.graph.average * Game.serverTickRate.FloatValue);
            byteOutCount++;
        }
        if(byteOutCount > 0)
            Console.Write("Avg bytes out: " + (byteOutSum / byteOutCount));

        Console.Write("-------------------");
        var freq = NetworkConfig.netPrintStats.IntValue;
        GameDebug.Log("Entity snapshots generated /frame : " + m_NetworkServer.statsGeneratedEntitySnapshots / freq);
        GameDebug.Log("Generated worldsnapsize    /frame : " + m_NetworkServer.statsGeneratedSnapshotSize / freq);
        GameDebug.Log("Entity snapshots total size/frame : " + m_NetworkServer.statsSnapshotData / freq);
        GameDebug.Log("Updates sent               /frame : " + m_NetworkServer.statsSentUpdates / freq);
        GameDebug.Log("Processed data outgoing      /sec : " + m_NetworkServer.statsProcessedOutgoing / timePassed);
        GameDebug.Log("Sent data outgoing         /frame : " + m_NetworkServer.statsSentOutgoing / freq);
        m_NetworkServer.statsGeneratedEntitySnapshots = 0;
        m_NetworkServer.statsGeneratedSnapshotSize = 0;
        m_NetworkServer.statsSnapshotData = 0;
        m_NetworkServer.statsSentUpdates = 0;
        m_NetworkServer.statsProcessedOutgoing = 0;
        m_NetworkServer.statsSentOutgoing= 0;
        Console.Write("-------------------");
    }


    float[] snapsPerFrame = new float[64];
    int totalSnapshotsLastFrame = 0;
    void DrawStats()
    {
        int y = 2;
        DebugOverlay.Write(2, y++, "  tick rate: {0}", Game.serverTickRate.IntValue);
        DebugOverlay.Write(2, y++, "  entities:  {0}", m_NetworkServer.NumEntities);

        DebugOverlay.Write(2, y++, "  sim  : {0:0.0} / {1:0.0} / {2:0.0} ({3:0.0})",
            m_ServerSimTime.min,
            m_ServerSimTime.min,
            m_ServerSimTime.max,
            m_ServerSimTime.stdDeviation);


        int totalSnapshots = 0;
        foreach(var c in m_NetworkServer.GetConnections())
        {
            totalSnapshots += c.Value.counters.snapshotsOut;
        }
        snapsPerFrame[Time.frameCount % snapsPerFrame.Length] = (totalSnapshots - totalSnapshotsLastFrame);
        totalSnapshotsLastFrame = totalSnapshots;
        DebugOverlay.DrawHist(2, 10, 60, 5, snapsPerFrame, Time.frameCount % snapsPerFrame.Length, new Color(0.5f, 0.5f, 1.0f));

        //for(var i = 0; i < counters.)

        //DebugOverlay.Write(2, y++, "  entities: {0}", counters.numEntities);

        return;
        /*

        DebugOverlay.Write(2, y++, "^FF0  lat  : {0:0.0} / {1:0.0} / {2:0.0}", m_Latency.min, m_Latency.average, m_Latency.max);
        DebugOverlay.Write(2, y++, "^0FF  rtt  : {0:0.0} / {1:0.0} / {2:0.0}", m_RTT.min, m_RTT.average, m_RTT.max);
        DebugOverlay.Write(2, y++, "^0F0  cmdq : {0:0.0} / {1:0.0} / {2:0.0}", m_CMDQ.min, m_CMDQ.average, m_CMDQ.max);
        DebugOverlay.Write(2, y++, "^F0F  intp : {0:0.0} / {1:0.0} / {2:0.0}", m_Interp.min, m_Interp.average, m_Interp.max);

        var samplesPerSecond = 1.0f / m_StatsDeltaTime.average;

        y++;
        DebugOverlay.Write(2, y++, "^00F  bps (in):  {0:00.0} ({1})", m_BytesIn.graph.average * samplesPerSecond, m_NetworkClient.updateRate);
        DebugOverlay.Write(2, y++, "^F00  bps (out): {0:00.0}", m_BytesOut.graph.average * samplesPerSecond);
        DebugOverlay.Write(2, y++, "  pps (in):  {0:00.0}", m_PackagesIn.graph.average * samplesPerSecond);
        DebugOverlay.Write(2, y++, "  pps (out): {0:00.0}", m_PackagesOut.graph.average * samplesPerSecond);
        DebugOverlay.Write(2, y++, "  pl% (in):  {0:00.0}", m_PackageLossPctIn.average);
        DebugOverlay.Write(2, y++, "  pl% (out): {0:00.0}", m_PackageLossPctOut.average);

        y++;
        DebugOverlay.Write(2, y++, "  upd_srate: {0:00.0} ({1})", m_SnapshotsIn.graph.average * samplesPerSecond, m_NetworkClient.updateSendRate);
        DebugOverlay.Write(2, y++, "  cmd_srate: {0:00.0} ({1})", m_CommandsOut.graph.average * samplesPerSecond, m_NetworkClient.commandSendRate);
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
        */
    }

    void DrawCounters()
    {
        return;
        //var counters = m_NetworkServer.GetCounters();
        /*
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
        */
    }

    /*
    float[][] m_2GraphData = new float[2][];
    Color[] m_BytesGraphColors = new Color[] { Color.blue, Color.red };

    float m_NextLossCalc;

    float m_PackagesLostPctIn;
    int m_PackageCountPrevIn;
    int m_PackageLossPrevIn;

    float m_PackagesLostPctOut;
    int m_PackageCountPrevOut;
    int m_PackageLossPrevOut;
    */

    const int k_WindowSize = 120;

    FloatRollingAverage m_StatsDeltaTime = new FloatRollingAverage(k_WindowSize);

    FloatRollingAverage m_ServerSimTime = new FloatRollingAverage(k_WindowSize);

    /*
    FloatRollingAverage m_Latency = new FloatRollingAverage(k_WindowSize);
    FloatRollingAverage m_RTT = new FloatRollingAverage(k_WindowSize);
    FloatRollingAverage m_CMDQ = new FloatRollingAverage(k_WindowSize);
    FloatRollingAverage m_Interp = new FloatRollingAverage(k_WindowSize);
    CircularList<float> m_HardCatchup = new CircularList<float>(k_WindowSize);

    Aggregator m_BytesIn = new Aggregator();
    Aggregator m_PackagesIn = new Aggregator();

    Aggregator m_SnapshotsIn = new Aggregator();
    Aggregator m_EventsIn = new Aggregator();

    Aggregator m_BytesOut = new Aggregator();
    Aggregator m_PackagesOut = new Aggregator();

    FloatRollingAverage m_PackageLossPctIn = new FloatRollingAverage(k_WindowSize);
    FloatRollingAverage m_PackageLossPctOut = new FloatRollingAverage(k_WindowSize);

    Aggregator m_CommandsOut = new Aggregator();
    Aggregator m_EventsOut = new Aggregator();
    */
}




