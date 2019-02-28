#define DEBUG_LOGGING
using UnityEngine;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System;
using System.Globalization;
using UnityEngine.Rendering.PostProcessing;
using SQP;
#if UNITY_EDITOR
using UnityEditor;
#endif


public struct GameTime
{
    /// <summary>Number of ticks per second.</summary>
    public int tickRate
    {
        get { return m_tickRate; }
        set
        {
            m_tickRate = value;
            tickInterval = 1.0f / m_tickRate;
        }
    }

    /// <summary>Length of each world tick at current tickrate, e.g. 0.0166s if ticking at 60fps.</summary>
    public float tickInterval { get; private set; }     // Time between ticks
    public int tick;                    // Current tick   
    public float tickDuration;          // Duration of current tick

    public GameTime(int tickRate)    
    {
        this.m_tickRate = tickRate;
        this.tickInterval = 1.0f / m_tickRate;
        this.tick = 1;
        this.tickDuration = 0;
    }

    public float TickDurationAsFraction
    {
        get { return tickDuration / tickInterval; }
    }

    public void SetTime(int tick, float tickDuration)
    {
        this.tick = tick;
        this.tickDuration = tickDuration;
    }

    public float DurationSinceTick(int tick)
    {
        return (this.tick - tick) * tickInterval + tickDuration;
    }

    public void AddDuration(float duration)
    {
        tickDuration += duration;
        int deltaTicks = Mathf.FloorToInt(tickDuration * (float)tickRate);
        tick += deltaTicks;
        tickDuration = tickDuration % tickInterval;
    }

    public static float GetDuration(GameTime start, GameTime end)
    {
        if(start.tickRate != end.tickRate)     
        {
            GameDebug.LogError("Trying to compare time with different tick rates (" + start.tickRate + " and " + end.tickRate + ")");
            return 0;
        }

        float result = (end.tick - start.tick) * start.tickInterval + end.tickDuration - start.tickDuration;
        return result;
    }

    int m_tickRate;
}

public class EnumeratedArrayAttribute : PropertyAttribute
{
    public readonly string[] names;
    public EnumeratedArrayAttribute(Type enumtype)
    {
        names = Enum.GetNames(enumtype);
    }
}


[DefaultExecutionOrder(-1000)]
public class Game : MonoBehaviour
{
    public delegate void UpdateDelegate(); 

    public WeakAssetReference movableBoxPrototype;

    // Color scheme configurable? (cvars?)
    public enum GameColor
    {
        Friend,
        Enemy
    }
    [EnumeratedArray(typeof(GameColor))]
    public Color[] gameColors;

    public GameStatistics m_GameStatistics { get; private set; }

    public static class Input
    {
        [Flags]
        public enum Blocker
        {
            None = 0,
            Console = 1,
            Chat = 2,
            Debug = 4,
        }
        static Blocker blocks;

        public static void SetBlock(Blocker b, bool value)
        {
            if (value)
                blocks |= b;
            else
                blocks &= ~b;
        }

        internal static float GetAxisRaw(string axis)
        {
            return blocks != Blocker.None ? 0.0f : UnityEngine.Input.GetAxisRaw(axis);
        }

        internal static bool GetKey(KeyCode key)
        {
            return blocks != Blocker.None ? false : UnityEngine.Input.GetKey(key);
        }

        internal static bool GetKeyDown(KeyCode key)
        {
            return blocks != Blocker.None ? false : UnityEngine.Input.GetKeyDown(key);
        }

        internal static bool GetMouseButton(int button)
        {
            return blocks != Blocker.None ? false : UnityEngine.Input.GetMouseButton(button);
        }

        internal static bool GetKeyUp(KeyCode key)
        {
            return blocks != Blocker.None ? false : UnityEngine.Input.GetKeyUp(key);
        }
    }

    public interface IGameLoop
    {
        bool Init(string[] args);
        void Shutdown();

        void Update();
        void FixedUpdate();
        void LateUpdate();
    }

    public static Game game;
    public event UpdateDelegate endUpdateEvent;

    // Vars owned by server and replicated to clients
    [ConfigVar(Name = "server.tickrate", DefaultValue = "60", Description = "Tickrate for server", Flags = ConfigVar.Flags.ServerInfo)]
    public static ConfigVar serverTickRate;

    [ConfigVar(Name = "config.fov", DefaultValue = "60", Description = "Field of view", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar configFov;

    [ConfigVar(Name = "config.mousesensitivity", DefaultValue = "1.5", Description = "Mouse sensitivity", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar configMouseSensitivity;

    [ConfigVar(Name = "config.inverty", DefaultValue = "0", Description = "Invert y mouse axis", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar configInvertY;

    [ConfigVar(Name = "debug.catchloop", DefaultValue = "1", Description = "Catch exceptions in gameloop and pause game", Flags = ConfigVar.Flags.None)]
    public static ConfigVar debugCatchLoop;

    [ConfigVar(Name = "chartype", DefaultValue = "-1", Description = "Character to start with (-1 uses default character)")]
    public static ConfigVar characterType;

    [ConfigVar(Name = "allowcharchange", DefaultValue = "1", Description = "Is changing character allowed")]
    public static ConfigVar allowCharChange;

    [ConfigVar(Name = "debug.cpuprofile", DefaultValue = "0", Description = "Profile and dump cpu usage")]
    public static ConfigVar debugCpuProfile;

    [ConfigVar(Name = "net.dropevents", DefaultValue = "0", Description = "Drops a fraction of all packages containing events!!")]
    public static ConfigVar netDropEvents;
    
    static readonly string k_UserConfigFilename = "user.cfg";
    public static readonly string k_BootConfigFilename = "boot.cfg";

    public static GameConfiguration config;
    public static InputSystem inputSystem;

    public UnityEngine.Audio.AudioMixer audioMixer;
    public SoundBank defaultBank;
    public Camera bootCamera;
    
    public LevelManager levelManager;
    public SQPClient sqpClient;

    public static double frameTime;

    public static bool IsHeadless()
    {
        return game.m_isHeadless;
    }

    public static ISoundSystem SoundSystem
    {
        get { return game.m_SoundSystem; }
    }
    
    public static int GameLoopCount {
        get { return game == null ? 0 : 1; }
    }

    public static T GetGameLoop<T>() where T : class
    {
        if (game == null)
            return null;
        foreach (var gameLoop in game.m_gameLoops)
        {
            T result = gameLoop as T;
            if (result != null)
                return result;
        }
        return null;
    }
    
    public static System.Diagnostics.Stopwatch Clock
    {
        get { return game.m_Clock; }
    }

    public string buildId
    {
        get { return _buildId; }
    }
    string _buildId = "NoBuild";

    public void RequestGameLoop(System.Type type, string[] args)
    {
        GameDebug.Assert(typeof(IGameLoop).IsAssignableFrom(type));
        
        m_RequestedGameLoopTypes.Add(type);
        m_RequestedGameLoopArguments.Add(args);
        GameDebug.Log("Game loop " + type + " requested");
    }

    // Pick argument for argument(!). Given list of args return null if option is
    // not found. Return argument following option if found or empty string if none given.
    // Options are expected to be prefixed with + or -
    public static string ArgumentForOption(List<string> args, string option)
    {
        var idx = args.IndexOf(option);
        if (idx < 0)
            return null;
        if (idx < args.Count - 1)
            return args[idx + 1];
        return "";
    }

    public void Awake()
    {
        GameDebug.Assert(game == null);
        DontDestroyOnLoad(gameObject);
        game = this;

        m_StopwatchFrequency = System.Diagnostics.Stopwatch.Frequency;
        m_Clock = new System.Diagnostics.Stopwatch();
        m_Clock.Start();

        var buildInfo = FindObjectOfType<BuildInfo>();
        if (buildInfo != null)
            _buildId = buildInfo.buildId;

        var commandLineArgs = new List<string>(System.Environment.GetCommandLineArgs());

#if UNITY_STANDALONE_LINUX
        m_isHeadless = true;
#else
        m_isHeadless = commandLineArgs.Contains("-batchmode");
#endif
        var consoleRestoreFocus = commandLineArgs.Contains("-consolerestorefocus");

        if (m_isHeadless)
        {
#if UNITY_STANDALONE_WIN
            string consoleTitle;

            var overrideTitle = ArgumentForOption(commandLineArgs, "-title");
            if (overrideTitle != null)
                consoleTitle = overrideTitle;
            else
                consoleTitle = Application.productName + " Console";

            consoleTitle += " ["+System.Diagnostics.Process.GetCurrentProcess().Id+"]";

            var consoleUI = new ConsoleTextWin(consoleTitle, consoleRestoreFocus);
#elif UNITY_STANDALONE_LINUX
            var consoleUI = new ConsoleTextLinux();
#else
            UnityEngine.Debug.Log("WARNING: starting without a console");
            var consoleUI = new ConsoleNullUI();
#endif
            Console.Init(consoleUI);
        }
        else
        {
            var consoleUI = Instantiate(Resources.Load<ConsoleGUI>("Prefabs/ConsoleGUI"));
            DontDestroyOnLoad(consoleUI);
            Console.Init(consoleUI);

            m_DebugOverlay = Instantiate(Resources.Load<DebugOverlay>("DebugOverlay"));
            DontDestroyOnLoad(m_DebugOverlay);
            m_DebugOverlay.Init();

            var hdpipe = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdpipe != null)
            {
                hdpipe.DebugLayer2DCallback = DebugOverlay.Render;
                hdpipe.DebugLayer3DCallback = DebugOverlay.Render3D;
            }

            m_GameStatistics = new GameStatistics();
        }

        // If -logfile was passed, we try to put our own logs next to the engine's logfile
        var engineLogFileLocation = ".";
        var logfileArgIdx = commandLineArgs.IndexOf("-logfile");
        if(logfileArgIdx >= 0 && commandLineArgs.Count >= logfileArgIdx)
        {
            engineLogFileLocation = System.IO.Path.GetDirectoryName(commandLineArgs[logfileArgIdx + 1]);
        }

        var logName = m_isHeadless ? "game_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") : "game";
        GameDebug.Init(engineLogFileLocation, logName);

        ConfigVar.Init();

        // Support -port and -query_port as per Multiplay standard
        var serverPort = ArgumentForOption(commandLineArgs, "-port");
        if (serverPort != null)
            Console.EnqueueCommandNoHistory("server.port " + serverPort);

        var sqpPort = ArgumentForOption(commandLineArgs, "-query_port");
        if (sqpPort != null)
            Console.EnqueueCommandNoHistory("server.sqp_port " + sqpPort);

        Console.EnqueueCommandNoHistory("exec -s " + k_UserConfigFilename);

        // Default is to allow no frame cap, i.e. as fast as possible if vsync is disabled
        Application.targetFrameRate = -1;

        if (m_isHeadless)
        {
            Application.targetFrameRate = serverTickRate.IntValue;
            QualitySettings.vSyncCount = 0; // Needed to make targetFramerate work; even in headless mode

#if !UNITY_STANDALONE_LINUX
            if (!commandLineArgs.Contains("-nographics"))
                GameDebug.Log("WARNING: running -batchmod without -nographics");
#endif
        }
        else
        {
            RenderSettings.Init();
        }

        // Out of the box game behaviour is driven by boot.cfg unless you ask it not to
        if(!commandLineArgs.Contains("-noboot"))
        {
            Console.EnqueueCommandNoHistory("exec -s " + k_BootConfigFilename);
        }


        if(m_isHeadless)
        {
            m_SoundSystem = new SoundSystemNull();
        }
        else
        {
            m_SoundSystem = new SoundSystem();
            m_SoundSystem.Init(audioMixer);
            m_SoundSystem.MountBank(defaultBank);

            GameObject go = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/ClientFrontend", typeof(GameObject)));
            UnityEngine.Object.DontDestroyOnLoad(go);
            clientFrontend = go.GetComponentInChildren<ClientFrontend>();
        }

        sqpClient = new SQP.SQPClient();

        GameDebug.Log("FPS Sample initialized");
#if UNITY_EDITOR
        GameDebug.Log("Build type: editor");
#elif DEVELOPMENT_BUILD
        GameDebug.Log("Build type: development");
#else
        GameDebug.Log("Build type: release");
#endif
        GameDebug.Log("BuildID: " + buildId);
        GameDebug.Log("Cwd: " + System.IO.Directory.GetCurrentDirectory());

        SimpleBundleManager.Init();
        GameDebug.Log("SimpleBundleManager initialized");

        levelManager = new LevelManager();
        levelManager.Init();
        GameDebug.Log("LevelManager initialized");

        inputSystem = new InputSystem();
        GameDebug.Log("InputSystem initialized");

        // TODO (petera) added Instantiate here to avoid making changes to asset file.
        // Feels like maybe SO is not really the right tool here.
        config = Instantiate((GameConfiguration)Resources.Load("GameConfiguration"));
        GameDebug.Log("Loaded game config");

        // Game loops
        Console.AddCommand("preview", CmdPreview, "Start preview mode");
        Console.AddCommand("serve", CmdServe, "Start server listening");
        Console.AddCommand("client", CmdClient, "client: Enter client mode.");
        Console.AddCommand("thinclient", CmdThinClient, "client: Enter thin client mode.");
        Console.AddCommand("boot", CmdBoot, "Go back to boot loop");
        Console.AddCommand("connect", CmdConnect, "connect <ip>: Connect to server on ip (default: localhost)");

        Console.AddCommand("menu", CmdMenu, "show the main menu");
        Console.AddCommand("load", CmdLoad, "Load level");
        Console.AddCommand("quit", CmdQuit, "Quits");
        Console.AddCommand("screenshot", CmdScreenshot, "Capture screenshot. Optional argument is destination folder or filename.");
        Console.AddCommand("crashme", (string[] args) => { GameDebug.Assert(false); }, "Crashes the game next frame ");
        Console.AddCommand("saveconfig", CmdSaveConfig, "Save the user config variables");
        Console.AddCommand("loadconfig", CmdLoadConfig, "Load the user config variables");

#if UNITY_STANDALONE_WIN
        Console.AddCommand("windowpos", CmdWindowPosition, "Position of window. e.g. windowpos 100,100");
#endif

        Console.SetOpen(true);
        Console.ProcessCommandLineArguments(commandLineArgs.ToArray());

        PushCamera(bootCamera);
    }

    public Camera TopCamera()
    {
        var c = m_CameraStack.Count;
        return c == 0 ? null : m_CameraStack[c - 1];
    }

    public void PushCamera(Camera cam)
    {
        if (m_CameraStack.Count > 0)
            SetCameraEnabled(m_CameraStack[m_CameraStack.Count - 1],false);
        m_CameraStack.Add(cam);
        SetCameraEnabled(cam,true);
        m_ExposureReleaseCount = 10;
    }

    public void BlackFade(bool enabled)
    {
        if(m_Exposure != null)
            m_Exposure.active = enabled;
    }

    public void PopCamera(Camera cam)
    {
        GameDebug.Assert(m_CameraStack.Count > 1, "Trying to pop last camera off stack!");
        GameDebug.Assert(cam == m_CameraStack[m_CameraStack.Count - 1]);
        if(cam != null)
            SetCameraEnabled(cam,false);
        m_CameraStack.RemoveAt(m_CameraStack.Count - 1);
        SetCameraEnabled(m_CameraStack[m_CameraStack.Count - 1],true);
    }

    void SetCameraEnabled(Camera cam, bool enabled)
    {
        if (enabled)
            RenderSettings.UpdateCameraSettings(cam);

        cam.enabled = enabled;
        var audioListener = cam.GetComponent<AudioListener>();
        if(audioListener != null)
        {
            audioListener.enabled = enabled;
            if(SoundSystem != null)
                SoundSystem.SetCurrentListener(enabled ? audioListener : null);
        }
    }
    
    void OnDestroy()
    {
        GameDebug.Shutdown();
        Console.Shutdown();
        if (m_DebugOverlay != null)
            m_DebugOverlay.Shutdown();
    }

    bool pipeSetup = false;
    public void Update()
    {
        if (!m_isHeadless)
            RenderSettings.Update();

        // TODO (petera) remove this hack once we know exactly when renderer is available...
        if (!pipeSetup)
        {
            var hdpipe = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdpipe != null)
            {
                hdpipe.DebugLayer2DCallback = DebugOverlay.Render;
                hdpipe.DebugLayer3DCallback = DebugOverlay.Render3D;

                var layer = LayerMask.NameToLayer("PostProcess Volumes");
                if (layer == -1)
                    GameDebug.LogWarning("Unable to find layer mask for camera fader");
                else
                {
                    m_Exposure = ScriptableObject.CreateInstance<AutoExposure>();
                    m_Exposure.active = false;
                    m_Exposure.enabled.Override(true);
                    m_Exposure.keyValue.Override(0);
                    m_ExposureVolume = PostProcessManager.instance.QuickVolume(layer, 100.0f, m_Exposure);
                }

                pipeSetup = true;
            }

        }
        if(m_ExposureReleaseCount > 0)
        {
            m_ExposureReleaseCount--;
            if (m_ExposureReleaseCount == 0)
                BlackFade(false);
        }

        // Verify if camera was somehow destroyed and pop it
        if(m_CameraStack.Count > 1 && m_CameraStack[m_CameraStack.Count-1] == null)
        {
            PopCamera(null);
        }

#if UNITY_EDITOR
        // Ugly hack to force focus to game view when using scriptable renderloops.
        if (Time.frameCount < 4)
        {
            try
            {
                var gameViewType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");
                var gameView = (EditorWindow)Resources.FindObjectsOfTypeAll(gameViewType)[0];
                gameView.Focus();
            }
            catch (System.Exception) { /* too bad */ }
        }
#endif

        frameTime = (double)m_Clock.ElapsedTicks / m_StopwatchFrequency;

        // Switch game loop if needed
        if (m_RequestedGameLoopTypes.Count > 0)
        {
            // Multiple running gameloops only allowed in editor
#if !UNITY_EDITOR
            ShutdownGameLoops();
#endif
            bool initSucceeded = false;
            for(int i=0;i<m_RequestedGameLoopTypes.Count;i++)
            {
                try
                {
                    IGameLoop gameLoop = (IGameLoop)System.Activator.CreateInstance(m_RequestedGameLoopTypes[i]);
                    initSucceeded = gameLoop.Init(m_RequestedGameLoopArguments[i]);
                    if (!initSucceeded)
                        break;
                    
                    m_gameLoops.Add(gameLoop);
                }
                catch (System.Exception e)
                {
                    GameDebug.Log(string.Format("Game loop initialization threw exception : ({0})\n{1}", e.Message, e.StackTrace));
                }
            }
            
            
            if (!initSucceeded)
            {
                ShutdownGameLoops();

                GameDebug.Log("Game loop initialization failed ... reverting to boot loop");
            }

            m_RequestedGameLoopTypes.Clear();
            m_RequestedGameLoopArguments.Clear();
        }

        try
        {
            if (!m_ErrorState)
            {
                foreach (var gameLoop in m_gameLoops)
                {
                    gameLoop.Update();
                }
                levelManager.Update();
            }
        }
        catch (System.Exception e)
        {
            HandleGameloopException(e);
            throw;
        }

        if (m_SoundSystem != null)
            m_SoundSystem.Update();

        if (clientFrontend != null)
            clientFrontend.UpdateGame();

        Console.ConsoleUpdate();

        WindowFocusUpdate();

        UpdateCPUStats();

        sqpClient.Update();

        endUpdateEvent?.Invoke();
    }

    bool m_ErrorState;

    public void FixedUpdate()
    {
        foreach (var gameLoop in m_gameLoops)
        {
            gameLoop.FixedUpdate();
        }
       
    }

    public void LateUpdate()
    {
        try
        {
            if (!m_ErrorState)
            {
                foreach (var gameLoop in m_gameLoops)
                {
                    gameLoop.LateUpdate();
                }
                Console.ConsoleLateUpdate();
            }
        }
        catch (System.Exception e)
        {
            HandleGameloopException(e);
            throw;
        }

        if (m_GameStatistics != null)
            m_GameStatistics.TickLateUpdate();

        if (m_DebugOverlay != null)
            m_DebugOverlay.TickLateUpdate();
    }

    void OnApplicationQuit()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        GameDebug.Log("Farewell, cruel world...");
        System.Diagnostics.Process.GetCurrentProcess().Kill();
#endif
        ShutdownGameLoops();
    }

    float m_NextCpuProfileTime = 0;
    double m_LastCpuUsage = 0;
    double m_LastCpuUsageUser = 0;
    void UpdateCPUStats()
    {
        if(debugCpuProfile.IntValue > 0)
        {
            if(Time.time > m_NextCpuProfileTime)
            {
                const float interval = 5.0f;
                m_NextCpuProfileTime = Time.time + interval;
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var user = process.UserProcessorTime.TotalMilliseconds;
                var total = process.TotalProcessorTime.TotalMilliseconds;
                float userUsagePct = (float)(user - m_LastCpuUsageUser) / 10.0f / interval;
                float totalUsagePct = (float)(total- m_LastCpuUsage) / 10.0f / interval;
                m_LastCpuUsage = total;
                m_LastCpuUsageUser = user;
                GameDebug.Log(string.Format("CPU Usage {0}% (user: {1}%)", totalUsagePct, userUsagePct));
            }
        }
    }


    public void LoadLevel(string levelname)
    {
        if (!Game.game.levelManager.CanLoadLevel(levelname))
        {
            GameDebug.Log("ERROR : Cannot load level : " + levelname);
            return;
        }

        Game.game.levelManager.LoadLevel(levelname);
    }

    void UnloadLevel()
    {
        // TODO
    }

    void HandleGameloopException(System.Exception e)
    {
        if (debugCatchLoop.IntValue > 0)
        {
            GameDebug.Log("EXCEPTION " + e.Message + "\n" + e.StackTrace);
            Console.SetOpen(true);
            m_ErrorState = true;
        }
    }

    string FindNewFilename(string pattern)
    {
        for(var i = 0; i < 10000; i++)
        {
            var f = string.Format(pattern, i);
            if (System.IO.File.Exists(string.Format(pattern, i)))
                continue;
            return f;
        }
        return null;
    }

    void ShutdownGameLoops()
    {
        foreach (var gameLoop in m_gameLoops)
            gameLoop.Shutdown();    
        m_gameLoops.Clear();   
    }
    
    void CmdPreview(string[] args)
    {
        RequestGameLoop(typeof(PreviewGameLoop), args);
        Console.s_PendingCommandsWaitForFrames = 1;
    }

    void CmdServe(string[] args)
    {
        RequestGameLoop( typeof(ServerGameLoop) , args);
        Console.s_PendingCommandsWaitForFrames = 1;
    }

    void CmdLoad(string[] args)
    {
        LoadLevel(args[0]);
        Console.SetOpen(false);
    }

    void CmdBoot(string[] args)
    {
        clientFrontend.ShowMenu(ClientFrontend.MenuShowing.None);
        levelManager.UnloadLevel();
        ShutdownGameLoops();
        Console.s_PendingCommandsWaitForFrames = 1;
        Console.SetOpen(true);
    }

    void CmdClient(string[] args)
    {
        RequestGameLoop( typeof(ClientGameLoop), args);
        Console.s_PendingCommandsWaitForFrames = 1;
    }

    void CmdConnect(string[] args)
    {
        // Special hack to allow "connect a.b.c.d" as shorthand
        if (m_gameLoops.Count == 0)
        {
            RequestGameLoop( typeof(ClientGameLoop), args);
            Console.s_PendingCommandsWaitForFrames = 1;
            return;
        }

        ClientGameLoop clientGameLoop = GetGameLoop<ClientGameLoop>();
        ThinClientGameLoop thinClientGameLoop = GetGameLoop<ThinClientGameLoop>();
        if (clientGameLoop != null)
            clientGameLoop.CmdConnect(args);
        else if (thinClientGameLoop != null)
            thinClientGameLoop.CmdConnect(args);
        else
            GameDebug.Log("Cannot connect from current gamemode");
    }

    void CmdThinClient(string[] args)
    {
        RequestGameLoop( typeof(ThinClientGameLoop), args);
        Console.s_PendingCommandsWaitForFrames = 1;
    }

    void CmdQuit(string[] args)
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void CmdScreenshot(string[] arguments)
    {
        string filename = null;
        var root = System.IO.Path.GetFullPath(".");
        if (arguments.Length == 0)
            filename = FindNewFilename(root+"/screenshot{0}.png");
        else if (arguments.Length == 1)
        {
            var a = arguments[0];
            if (System.IO.Directory.Exists(a))
                filename = FindNewFilename(a + "/screenshot{0}.png");
            else if (!System.IO.File.Exists(a))
                filename = a;
            else
            {
                Console.Write("File " + a + " already exists");
                return;
            }
        }
        if (filename != null)
        {
            GameDebug.Log("Saving screenshot to " + filename);
            Console.SetOpen(false);
            ScreenCapture.CaptureScreenshot(filename);
        }
    }

    public ClientFrontend clientFrontend;
    private void CmdMenu(string[] args)
    {
        float fadeTime = 0.0f;
        ClientFrontend.MenuShowing show = ClientFrontend.MenuShowing.Main;
        if(args.Length > 0)
        {
            if (args[0] == "0")
                show = ClientFrontend.MenuShowing.None;
            else if (args[0] == "2")
                show = ClientFrontend.MenuShowing.Ingame;
        }
        if(args.Length > 1)
        {
            float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out fadeTime);
        }
        clientFrontend.ShowMenu(show, fadeTime);
        Console.SetOpen(false);
    }

    void CmdSaveConfig(string[] arguments)
    {
        ConfigVar.Save(k_UserConfigFilename);
    }

    void CmdLoadConfig(string[] arguments)
    {
        Console.EnqueueCommandNoHistory("exec " + k_UserConfigFilename);
    }

#if UNITY_STANDALONE_WIN
    void CmdWindowPosition(string[] arguments)
    {
        if (arguments.Length == 1)
        {
            string[] cords = arguments[0].Split(',');
            if (cords.Length == 2)
            {
                int x, y;
                var xParsed = int.TryParse(cords[0], out x);
                var yParsed = int.TryParse(cords[1], out y);
                if (xParsed && yParsed)
                {
                    WindowsUtil.SetWindowPosition(x, y);
                    return;
                }
            }
        }
        Console.Write("Usage: windowpos <x,y>");
    }

#endif

    public static void RequestMousePointerLock()
    {
        s_bMouseLockFrameNo = Time.frameCount + 1;
    }

    public static void SetMousePointerLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        s_bMouseLockFrameNo = Time.frameCount; // prevent default handling in WindowFocusUpdate overriding requests
    }

    public static bool GetMousePointerLock()
    {
        return Cursor.lockState == CursorLockMode.Locked;
    }

    void WindowFocusUpdate()
    {
        bool menusShowing = (clientFrontend != null && clientFrontend.menuShowing != ClientFrontend.MenuShowing.None);
        bool lockWhenClicked = !menusShowing && !Console.IsOpen();

        if(s_bMouseLockFrameNo == Time.frameCount)
        {
            SetMousePointerLock(true);
            return;
        }

        if (lockWhenClicked)
        {
            // Default behaviour when no menus or anything. Catch mouse on click, release on escape.
            if (UnityEngine.Input.GetMouseButtonUp(0) && !GetMousePointerLock())
                SetMousePointerLock(true);

            if (UnityEngine.Input.GetKeyUp(KeyCode.Escape) && GetMousePointerLock())
                SetMousePointerLock(false);
        }
        else
        {
            // When menu or console open, release lock
            if (GetMousePointerLock())
            {
                SetMousePointerLock(false);
            }
        }
    }

    List<Type> m_RequestedGameLoopTypes = new List<System.Type>();
    private List<string[]> m_RequestedGameLoopArguments = new List<string[]>();

    // Global camera handling
    List<Camera> m_CameraStack = new List<Camera>();
    AutoExposure m_Exposure;
    PostProcessVolume m_ExposureVolume;
    int m_ExposureReleaseCount;

    List<IGameLoop> m_gameLoops = new List<IGameLoop>();
    DebugOverlay m_DebugOverlay;
    ISoundSystem m_SoundSystem;

    bool m_isHeadless;
    long m_StopwatchFrequency;
    System.Diagnostics.Stopwatch m_Clock;

    static int s_bMouseLockFrameNo;
}
