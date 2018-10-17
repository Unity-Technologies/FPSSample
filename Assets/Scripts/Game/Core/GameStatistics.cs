using UnityEngine;
using System.Collections;
using UnityEngine.Profiling;
using System;
using System.Collections.Generic;

public class GameStatistics
{

    public int rtt;

    private readonly int _no_frames = 128;

    public GameStatistics()
    {
        m_FrequencyMS = System.Diagnostics.Stopwatch.Frequency / 1000;
        m_StopWatch = new System.Diagnostics.Stopwatch();
        m_StopWatch.Start();
        m_LastFrameTicks = m_StopWatch.ElapsedTicks;
        m_FrameTimes = new float[_no_frames];
        m_TicksPerFrame = new float[2][] { new float[_no_frames], new float[_no_frames] };

        m_GraphicsDeviceName = SystemInfo.graphicsDeviceName;

        for (int i = 0; i < recordersList.Length; i++)
        {
            var sampler = Sampler.Get(recordersList[i].name);
            if (sampler != null)
            {
                recordersList[i].recorder = sampler.GetRecorder();
            }
        }

        Console.AddCommand("show.profilers", CmdShowProfilers, "Show available profilers.");
    }

    void CmdShowProfilers(string[] args)
    {
        var names = new List<string>();
        Sampler.GetNames(names);
        string search = args.Length > 0 ? args[0].ToLower() : null;
        for(var i = 0; i < names.Count; i++)
        {
            if(search == null || names[i].ToLower().Contains(search))
                Console.Write(names[i]);
        }
    }

    int m_LastWorldTick;

    void SnapTime()
    {
        long now = m_StopWatch.ElapsedTicks;
        long duration = now - m_LastFrameTicks;

        m_LastFrameTicks = now;

        float d = (float)duration / m_FrequencyMS;
        m_FrameDurationMS = m_FrameDurationMS * 0.9f + 0.1f * d;

        m_FrameTimes[Time.frameCount % m_FrameTimes.Length] = d;
    }

    void RecordTimers()
    {
        int ticks = 0;
        if (GameWorld.s_Worlds.Count > 0)
        {
            var world = GameWorld.s_Worlds[0];

            // Number of ticks in world since last frame.
            ticks = world.worldTime.tick - m_LastWorldTick;
            int l = Time.frameCount % m_TicksPerFrame[0].Length;
            m_TicksPerFrame[0][l] = 1000.0f * world.worldTime.tickInterval * ticks;
            m_LastWorldTick = world.worldTime.tick;
            double lastTickTime = world.nextTickTime - world.worldTime.tickInterval;
            m_TicksPerFrame[1][l] = (float)(1000.0 * (Game.frameTime - lastTickTime));
        }

        // get timing & update average accumulators
        for (int i = 0; i < recordersList.Length; i++)
        {
            recordersList[i].time = recordersList[i].recorder.elapsedNanoseconds / 1000000.0f;
            recordersList[i].count = recordersList[i].recorder.sampleBlockCount;
            recordersList[i].accTime += recordersList[i].time;
            recordersList[i].accCount += recordersList[i].count;
        }

        frameCount++;
        // time to time, update average values & reset accumulators
        if (frameCount >= kAverageFrameCount)
        {
            for (int i = 0; i < recordersList.Length; i++)
            {
                recordersList[i].avgTime = recordersList[i].accTime * (1.0f / kAverageFrameCount);
                recordersList[i].avgCount = recordersList[i].accCount * (1.0f / kAverageFrameCount);
                recordersList[i].accTime = 0.0f;
                recordersList[i].accCount = 0;

            }
            frameCount = 0;
        }
    }

    public void TickLateUpdate()
    {
        SnapTime();
        if(showCompactStats.IntValue > 0)
        {
            DrawCompactStats();
        }
        if (showFPS.IntValue > 0)
        {
            RecordTimers();
            DrawFPS();
        }
    }

    private int frameCount = 0;
    private const int kAverageFrameCount = 64;

    internal class RecorderEntry
    {
        public string name;
        public float time;
        public int count;
        public float avgTime;
        public float avgCount;
        public float accTime;
        public int accCount;
        public Recorder recorder;
    };

    RecorderEntry[] recordersList =
    {
        new RecorderEntry() { name="RenderLoop.Draw" },
        new RecorderEntry() { name="Shadows.Draw" },
        new RecorderEntry() { name="RenderLoopNewBatcher.Draw" },
        new RecorderEntry() { name="ShadowLoopNewBatcher.Draw" },
        new RecorderEntry() { name="RenderLoopDevice.Idle" },
        new RecorderEntry() { name="StaticBatchDraw.Count" },
    };


    char[] buf = new char[256];
    void DrawCompactStats()
    {
        DebugOverlay.AddQuadAbsolute(0, 0, 60, 14, '\0', new Vector4(1.0f, 1.0f, 1.0f, 0.2f));
        var c = StringFormatter.Write(ref buf, 0, "FPS:{0}", Mathf.RoundToInt(1000.0f / m_FrameDurationMS));
        DebugOverlay.WriteAbsolute(2, 2, 8.0f, buf, c);

        DebugOverlay.AddQuadAbsolute(62, 0, 60, 14, '\0', new Vector4(1.0f, 1.0f, 0.0f, 0.2f));
        if (rtt > 0)
            c = StringFormatter.Write(ref buf, 0, "RTT:{0}", rtt);
        else
            c = StringFormatter.Write(ref buf, 0, "RTT:---");
        DebugOverlay.WriteAbsolute(64, 2, 8.0f, buf, c);
    }

    void DrawFPS()
    {
        DebugOverlay.Write(0, 1, "{0} FPS ({1:##.##} ms)", Mathf.RoundToInt(1000.0f / m_FrameDurationMS), m_FrameDurationMS);
        float minDuration = float.MaxValue;
        float maxDuration = float.MinValue;
        float sum = 0;
        for (var i = 0; i < _no_frames; i++)
        {
            var frametime = m_FrameTimes[i];
            sum += frametime;
            if (frametime < minDuration) minDuration = frametime;
            if (frametime > maxDuration) maxDuration = frametime;
        }

        DebugOverlay.Write(Color.green, 0, 2, "{0:##.##}", minDuration);
        DebugOverlay.Write(Color.grey, 6, 2, "{0:##.##}", sum / _no_frames);
        DebugOverlay.Write(Color.red, 12, 2, "{0:##.##}", maxDuration);

        DebugOverlay.Write(0, 3, "Frame #: {0}", Time.frameCount);

        DebugOverlay.Write(0, 4, m_GraphicsDeviceName);


        int y = 6;
        for (int i = 0; i < recordersList.Length; i++)
            DebugOverlay.Write(0, y++, "{0:##.##}ms (*{1:##})  ({2:##.##}ms *{3:##})  {4}", recordersList[i].avgTime, recordersList[i].avgCount, recordersList[i].time, recordersList[i].count, recordersList[i].name);

        if (showFPS.IntValue < 3)
            return;

        y++;
        // Start at framecount+1 so the one we have just recorded will be the last
        DebugOverlay.DrawHist(0, y, 20, 2, m_FrameTimes, Time.frameCount + 1, fpsColor, 20.0f);
        DebugOverlay.DrawHist(0, y + 2, 20, 2, m_TicksPerFrame, Time.frameCount + 1, histColor, 3.0f * 16.0f);

        DebugOverlay.DrawGraph(0, y + 6, 40, 2, m_FrameTimes, Time.frameCount + 1, fpsColor, 20.0f);

        if (GameWorld.s_Worlds.Count > 0)
        {
            var world = GameWorld.s_Worlds[0];
            DebugOverlay.Write(0, y + 8, "Tick: {0:##.#}", 1000.0f * world.worldTime.tickInterval);
        }
    }

    Color fpsColor = new Color(0.5f, 0.0f, 0.2f);
    Color[] histColor = new Color[] { Color.green, Color.grey };

    System.Diagnostics.Stopwatch m_StopWatch;
    long m_LastFrameTicks; // Ticks at start of last frame
    float m_FrameDurationMS;
    float[] m_FrameTimes;
    float[][] m_TicksPerFrame;
    long m_FrequencyMS;
    string m_GraphicsDeviceName;


    [ConfigVar(Name = "show.fps", DefaultValue = "0", Description = "Set to value > 0 to see fps stats.")]
    public static ConfigVar showFPS;

    [ConfigVar(Name = "show.compactstats", DefaultValue = "1", Description = "Set to value > 0 to see compact stats.")]
    public static ConfigVar showCompactStats;
}
