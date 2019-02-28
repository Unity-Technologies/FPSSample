using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

public static class RenderSettings
{
    [ConfigVar(Name = "show.quality", DefaultValue = "0", Description = "Show quality setting debug overlay")]
    static ConfigVar showQuality;
    [ConfigVar(Name = "r.quality", DefaultValue = "Ultra", Description = "Overall rendering quality", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rQuality;
    [ConfigVar(Name = "r.vsync", DefaultValue = "1", Description = "Number of v-blanks to wait for each frame. 0 means no sync", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rVSync;
    [ConfigVar(Name = "r.fullscreen", DefaultValue = "3", Description = "Full screen mode (0: exclusive, 1: full, 3: windowed)", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rFullscreen;
    [ConfigVar(Name = "r.aamode", DefaultValue = "taa", Description = "AA mode: off, fxaa, smaa, taa", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rAAMode;
    [ConfigVar(Name = "r.aaquality", DefaultValue = "high", Description = "AA quality: low, med, high", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rAAQuality;
    [ConfigVar(Name = "r.bloom", DefaultValue = "1", Description = "Enable bloom", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rBloom;
    [ConfigVar(Name = "r.motionblur", DefaultValue = "1", Description = "Enable motion blur", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rMotionBlur;
    [ConfigVar(Name = "r.ssao", DefaultValue = "1", Description = "Enable ssao", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rSSAO;
    [ConfigVar(Name = "r.grain", DefaultValue = "1", Description = "Enable grain", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rGrain;
    [ConfigVar(Name = "r.ssr", DefaultValue = "1", Description = "Enable screen space reflections", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rSSR;
    [ConfigVar(Name = "r.sss", DefaultValue = "1", Description = "Enable subsurface scattering", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rSSS;
    [ConfigVar(Name = "r.roughrefraction", DefaultValue = "1", Description = "Enable rough refraction", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rRoughRefraction;
    [ConfigVar(Name = "r.distortion", DefaultValue = "1", Description = "Enable distortion", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rDistortion;
    [ConfigVar(Name = "r.shadowdistmult", DefaultValue = "1.0", Description = "Shadow distance multiplier", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rShadowDistMult;
    [ConfigVar(Name = "r.decaldist", DefaultValue = "200", Description = "Decal draw distance", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rDecalDist;
    [ConfigVar(Name = "r.gamma", DefaultValue = "1", Description = "User gamma correction", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rGamma;
    [ConfigVar(Name = "r.resolution", DefaultValue = "", Description = "Screen resolution", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar rResolution;
    [ConfigVar(Name = "r.latesync", DefaultValue = "1", Description = "Sync with render thread late", Flags = ConfigVar.Flags.None)]
    public static ConfigVar rLateSync;
    [ConfigVar(Name = "r.occlusionthreshold", DefaultValue = "50", Description = "Occlusion threshold", Flags = ConfigVar.Flags.None)]
    public static ConfigVar rOcclusionThreshold;

    public static void Init()
    {
        Console.AddCommand("r_resolution", CmdResolution, "Display or set resolution, e.g. 1280x720");
        Console.AddCommand("r_quality", CmdQuality, "Set the render quality");
        Console.AddCommand("r_maxqueue", CmdMaxQueue, "Max queued frames");
        Console.AddCommand("r_srpbatching", CmdSrpBatching, "Use 0 or 1 to disable or enable SRP batching");

        if (rResolution.Value == "")
            rResolution.Value = Screen.currentResolution.width + "x" + Screen.currentResolution.height + "@" + Screen.currentResolution.refreshRate;
    }

    /*
     * TODO:
     * SSS Sample count
     * MSAA Sample count
     * */

    static int currentResX, currentResY, currentResRate;
    static int currentQualityIdx;

    public static void Update()
    {
        if (rLateSync.ChangeCheck())
        {
            GraphicsDeviceSettings.waitForPresentSyncPoint = rLateSync.IntValue > 0 ? WaitForPresentSyncPoint.EndFrame : WaitForPresentSyncPoint.BeginFrame;
        }
        if(rOcclusionThreshold.ChangeCheck())
        {
            HDRenderPipeline.s_OcclusionThreshold = rOcclusionThreshold.FloatValue;
        }

        bool updateAAFlags = false;
        bool updateFrameSettings = false;

        if (rResolution.ChangeCheck())
            CmdResolution(new string[] { rResolution.Value });
        else
        {
            if (currentResX != Screen.width || currentResY != Screen.height)
            {
                currentResX = Screen.width;
                currentResY = Screen.height;
                rResolution.Value = currentResX + "x" + currentResY;
            }
        }

        if (rQuality.ChangeCheck())
        {
            var names = QualitySettings.names;
            bool set = false;
            for (int i = 0; i < names.Length; i++)
            {
                if (rQuality.Value == names[i])
                {
                    QualitySettings.SetQualityLevel(i);
                    set = true;
                    break;
                }
            }
            if (!set)
            {
                rQuality.Value = QualitySettings.names[QualitySettings.GetQualityLevel()];
                Console.Write("Unknown quality setting. Reverting to " + rQuality.Value);
            }
        }
        else
        {
            var currentIdx = QualitySettings.GetQualityLevel();
            if(currentQualityIdx != currentIdx)
            {
                currentQualityIdx = currentIdx;
                rQuality.Value = QualitySettings.names[currentIdx];
            }
        }

        if (showQuality.IntValue > 0)
            DrawQualityOverlay();

        if (rVSync.ChangeCheck())
            QualitySettings.vSyncCount = rVSync.IntValue;

        if (rFullscreen.ChangeCheck())
            Screen.fullScreenMode = (FullScreenMode)rFullscreen.IntValue;

        //        if (rNewPrepareLights.ChangeCheck())
        //            LightLoop.useNewPrepareLights = (rNewPrepareLights.IntValue != 0);

        if (rAAMode.ChangeCheck())
            updateAAFlags = true;

        if (rAAQuality.ChangeCheck())
            updateAAFlags = true;

        // Post effect flags
        if (rBloom.ChangeCheck())
            Bloom.globalEnable = rBloom.IntValue > 0;

        if (rMotionBlur.ChangeCheck())
        {
            MotionBlur.globalEnable = rMotionBlur.IntValue > 0;
            updateFrameSettings = true;
        }

        if (rSSAO.ChangeCheck())
        {
            AmbientOcclusion.globalEnable = rSSAO.IntValue > 0;
            updateFrameSettings = true;
        }

        if (rGrain.ChangeCheck())
            Grain.globalEnable = rGrain.IntValue > 0;

        if (rSSR.ChangeCheck())
        {
            ScreenSpaceReflections.globalEnable = rSSR.IntValue > 0;
            updateFrameSettings = true;
        }

        if (rSSS.ChangeCheck())
            updateFrameSettings = true;

        if (rRoughRefraction.ChangeCheck())
            updateFrameSettings = true;

        if (rDistortion.ChangeCheck())
            updateFrameSettings = true;

        if (rShadowDistMult.ChangeCheck())
            HDShadowSettings.shadowDistanceMultiplier = Mathf.Clamp(rShadowDistMult.FloatValue, 0.5f, 4.0f);

        if (rDecalDist.ChangeCheck())
        {
            var hdasset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            hdasset.renderPipelineSettings.decalSettings.drawDistance = rDecalDist.IntValue;
        }

        if (rGamma.ChangeCheck())
            ColorGrading.globalGamma = Mathf.Clamp(rGamma.FloatValue, 0.1f, 5.0f);

        if (updateAAFlags)
            UpdateAAFlags(Game.game.TopCamera());

        if (updateFrameSettings)
            UpdateFrameSettings(Game.game.TopCamera());
    }

    internal static void UpdateCameraSettings(Camera cam)
    {
        UpdateAAFlags(cam);
        UpdateFrameSettings(cam);
    }

    static void DrawQualityOverlay()
    {
        DebugOverlay.Write(2, 2, "Quality Settings");
        DebugOverlay.Write(2, 4, "Current camera: {0}", Game.game.TopCamera() != null ? Game.game.TopCamera().name : "(null)");
        //DebugOverlay.Write(2, 5, "Post volume:    {0}", "");// Game.game.TopCamera() != null ? Game.game.TopCamera().name : "(null)");

        DebugOverlay.Write(2, 7, "r.aamode:          {0}", rAAMode.Value);
        DebugOverlay.Write(2, 8, "r.aaquality:       {0}", rAAQuality.Value);
        DebugOverlay.Write(2, 9, "r.bloom:           {0}", rBloom.IntValue);
        DebugOverlay.Write(2, 10, "r.motionblur:      {0}", rMotionBlur.IntValue);
        DebugOverlay.Write(2, 11, "r.ssao:            {0}", rSSAO.IntValue);
        DebugOverlay.Write(2, 12, "r.grain:           {0}", rGrain.IntValue);
        DebugOverlay.Write(2, 13, "r.ssr:             {0}", rSSR.IntValue);

        DebugOverlay.Write(2, 15, "r.sss:             {0}", rSSS.IntValue);
        DebugOverlay.Write(2, 16, "r.roughrefraction: {0}", rRoughRefraction.IntValue);
        DebugOverlay.Write(2, 17, "r.distortion:      {0}", rDistortion.IntValue);

        DebugOverlay.Write(2, 19, "r.decaldist:       {0}", rDecalDist.IntValue);
        DebugOverlay.Write(2, 20, "r.shadowdistmult:  {0}", rShadowDistMult.FloatValue);
    }

    static void UpdateFrameSettings(Camera c)
    {
        var hdCam = c.GetComponent<HDAdditionalCameraData>();
        if (hdCam == null)
            return;

        hdCam.GetFrameSettings().enableSubsurfaceScattering = rSSS.IntValue > 0;
        hdCam.GetFrameSettings().enableMotionVectors = rMotionBlur.IntValue > 0;
        hdCam.GetFrameSettings().enableObjectMotionVectors = rMotionBlur.IntValue > 0;
        hdCam.GetFrameSettings().enableSSAO = rSSAO.IntValue > 0;
        hdCam.GetFrameSettings().enableSSR = rSSR.IntValue > 0;
        hdCam.GetFrameSettings().enableRoughRefraction = rRoughRefraction.IntValue > 0;
        hdCam.GetFrameSettings().enableDistortion = rDistortion.IntValue > 0;
    }

    static void UpdateAAFlags(Camera c)
    {
        if (c == null)
            return;

        var ppl = c.GetComponent<PostProcessLayer>();
        if (ppl == null)
            return;

        if (rAAMode.Value == "off")
            ppl.antialiasingMode = PostProcessLayer.Antialiasing.None;
        else if (rAAMode.Value == "fxaa")
            ppl.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
        else if (rAAMode.Value == "smaa")
        {
            ppl.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;

            if (rAAQuality.Value == "low") ppl.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.Low;
            else if (rAAQuality.Value == "med") ppl.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.Medium;
            else if (rAAQuality.Value == "high") ppl.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.High;
            else GameDebug.Log("Unknown AA quality: " + rAAQuality.Value);
        }
        else if (rAAMode.Value == "taa")
            ppl.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;
        else
            GameDebug.Log("Unknown aa mode: " + rAAMode.Value);

    }

    static void CmdResolution(string[] arguments)
    {
        var resolutions = Screen.resolutions;

        if (arguments.Length > 0)
        {
            var wantedResolution = arguments[0];

            int[] res = new int[3];
            int a = 0;
            for (int i = 0; i < wantedResolution.Length; i++)
            {
                var c = wantedResolution[i];
                if (c >= '0' && c <= '9')
                    res[a] = res[a] * 10 + (c - '0');
                else if ((c == 'x' || c == '@') && a < 2)
                    a++;
                else
                    break;
            }

            int width = res[0];
            int height = res[1];
            int refresh = res[2] > 0 ? res[2] : Screen.currentResolution.refreshRate;
            if (width > 100 && height > 100)
            {
                Screen.SetResolution(width, height, Screen.fullScreenMode, refresh);
                return;
            }
            else
            {
                Console.Write("Invalid resolution. Use <width>x<height>[@<refresh>] with width and height > 100");
            }
        }

        Console.Write("Resolutions supported by monitor:");
        for (var i = 0; i < resolutions.Length; i++)
        {
            var r = resolutions[i];
            Console.Write(r.width + "x" + r.height + "@" + r.refreshRate);
        }

        Console.Write("Fullscreen-mode: " + (int)Screen.fullScreenMode + "(" + Screen.fullScreenMode.ToString() + ")");
        Console.Write("Current window resolution: " + Screen.width + "x" + Screen.height);
        Console.Write("Current screen resolution: " + Screen.currentResolution.width + "x" + Screen.currentResolution.height + "@" + Screen.currentResolution.refreshRate);
    }

    static void CmdQuality(string[] arguments)
    {
        var names = QualitySettings.names;
        Console.Write("Current setting: " + names[QualitySettings.GetQualityLevel()]);
        Console.Write("Known settings:");
        for (var i = 0; i < names.Length; i++)
        {
            Console.Write(" " + names[i]);
        }
    }

    static void CmdMaxQueue(string[] arguments)
    {
        if (arguments.Length != 1)
        {
            Console.Write("Max queued frames: " + QualitySettings.maxQueuedFrames);
            return;
        }
        QualitySettings.maxQueuedFrames = int.Parse(arguments[0]);
    }

    static void CmdSrpBatching(string[] args)
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = (args.Length == 1 && args[0] == "1");
        Console.Write("SrpBatching " + (GraphicsSettings.useScriptableRenderPipelineBatching ? "on" : "off"));
    }

}
