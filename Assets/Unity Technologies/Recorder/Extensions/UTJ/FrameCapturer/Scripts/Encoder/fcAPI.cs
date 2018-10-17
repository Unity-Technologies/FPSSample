using System;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UTJ.FrameCapturer
{
    public static class fcAPI
    {
        // -------------------------------------------------------------
        // Foundation
        // -------------------------------------------------------------

        public enum fcPixelFormat
        {
            Unknown = 0,

            ChannelMask = 0xF,
            TypeMask = 0xF << 4,
            Type_f16 = 0x1 << 4,
            Type_f32 = 0x2 << 4,
            Type_u8  = 0x3 << 4,
            Type_i16 = 0x4 << 4,
            Type_i32 = 0x5 << 4,

            Rf16     = Type_f16 | 1,
            RGf16    = Type_f16 | 2,
            RGBf16   = Type_f16 | 3,
            RGBAf16  = Type_f16 | 4,
            Rf32     = Type_f32 | 1,
            RGf32    = Type_f32 | 2,
            RGBf32   = Type_f32 | 3,
            RGBAf32  = Type_f32 | 4,
            Ru8      = Type_u8  | 1,
            RGu8     = Type_u8  | 2,
            RGBu8    = Type_u8  | 3,
            RGBAu8   = Type_u8  | 4,
            Ri16     = Type_i16 | 1,
            RGi16    = Type_i16 | 2,
            RGBi16   = Type_i16 | 3,
            RGBAi16  = Type_i16 | 4,
            Ri32     = Type_i32 | 1,
            RGi32    = Type_i32 | 2,
            RGBi32   = Type_i32 | 3,
            RGBAi32  = Type_i32 | 4,
        };

        public enum fcBitrateMode
        {
            CBR,
            VBR,
        }

        public enum fcAudioBitsPerSample
        {
            _8Bits = 8,
            _16Bits = 16,
            _24Bits = 24,
        }



        [DllImport ("fccore")] public static extern void         fcSetModulePath(string path);
        [DllImport ("fccore")] public static extern double       fcGetTime();


        public struct fcDeferredCall
        {
            public int handle;
            public void Release() { fcReleaseDeferredCall(this); handle = 0; }
            public static implicit operator int(fcDeferredCall v) { return v.handle; }
        }

        public struct fcStream
        {
            public IntPtr ptr;
            public void Release() { fcReleaseStream(this); ptr = IntPtr.Zero; }
            public static implicit operator bool(fcStream v) { return v.ptr != IntPtr.Zero; }
        }
        [DllImport ("fccore")] public static extern fcStream     fcCreateFileStream(string path);
        [DllImport ("fccore")] public static extern fcStream     fcCreateMemoryStream();
        [DllImport ("fccore")] private static extern void        fcReleaseStream(fcStream s);
        [DllImport ("fccore")] public static extern ulong        fcStreamGetWrittenSize(fcStream s);

        [DllImport ("fccore")] public static extern void         fcGuardBegin();
        [DllImport ("fccore")] public static extern void         fcGuardEnd();
        [DllImport ("fccore")] public static extern fcDeferredCall fcAllocateDeferredCall();
        [DllImport ("fccore")] private static extern void        fcReleaseDeferredCall(fcDeferredCall dc);
        [DllImport ("fccore")] public static extern IntPtr       fcGetRenderEventFunc();

        public static void fcGuard(Action body)
        {
            fcGuardBegin();
            body.Invoke();
            fcGuardEnd();
        }

        public static fcPixelFormat fcGetPixelFormat(RenderTextureFormat v)
        {
            switch (v)
            {
                case RenderTextureFormat.ARGB32:    return fcPixelFormat.RGBAu8;
                case RenderTextureFormat.ARGBHalf:  return fcPixelFormat.RGBAf16;
                case RenderTextureFormat.RGHalf:    return fcPixelFormat.RGf16;
                case RenderTextureFormat.RHalf:     return fcPixelFormat.Rf16;
                case RenderTextureFormat.ARGBFloat: return fcPixelFormat.RGBAf32;
                case RenderTextureFormat.RGFloat:   return fcPixelFormat.RGf32;
                case RenderTextureFormat.RFloat:    return fcPixelFormat.Rf32;
                case RenderTextureFormat.ARGBInt:   return fcPixelFormat.RGBAi32;
                case RenderTextureFormat.RGInt:     return fcPixelFormat.RGi32;
                case RenderTextureFormat.RInt:      return fcPixelFormat.Ri32;
            }
            return fcPixelFormat.Unknown;
        }

        public static fcPixelFormat fcGetPixelFormat(TextureFormat v)
        {
            switch (v)
            {
                case TextureFormat.Alpha8:      return fcPixelFormat.Ru8;
                case TextureFormat.RGB24:       return fcPixelFormat.RGBu8;
                case TextureFormat.RGBA32:      return fcPixelFormat.RGBAu8;
                case TextureFormat.ARGB32:      return fcPixelFormat.RGBAu8;
                case TextureFormat.RGBAHalf:    return fcPixelFormat.RGBAf16;
                case TextureFormat.RGHalf:      return fcPixelFormat.RGf16;
                case TextureFormat.RHalf:       return fcPixelFormat.Rf16;
                case TextureFormat.RGBAFloat:   return fcPixelFormat.RGBAf32;
                case TextureFormat.RGFloat:     return fcPixelFormat.RGf32;
                case TextureFormat.RFloat:      return fcPixelFormat.Rf32;
            }
            return fcPixelFormat.Unknown;
        }

        public static int fcGetNumAudioChannels()
        {
            switch (AudioSettings.speakerMode)
            {
                case AudioSpeakerMode.Mono:         return 1;
                case AudioSpeakerMode.Stereo:       return 2;
                case AudioSpeakerMode.Quad:         return 4;
                case AudioSpeakerMode.Surround:     return 5;
                case AudioSpeakerMode.Mode5point1:  return 6;
                case AudioSpeakerMode.Mode7point1:  return 8;
                case AudioSpeakerMode.Prologic:     return 6;
            }
            return 0;
        }


        [DllImport ("fccore")] public static extern void fcEnableAsyncReleaseContext(Bool v);
        [DllImport ("fccore")] public static extern void fcWaitAsyncDelete();
        [DllImport ("fccore")] public static extern void fcReleaseContext(IntPtr ctx);


        // -------------------------------------------------------------
        // PNG Exporter
        // -------------------------------------------------------------

        public enum fcPngPixelFormat
        {
            Auto, // select optimal one for input data
            UInt8,
            UInt16,
        };

        [Serializable]
        public struct fcPngConfig
        {
            public fcPngPixelFormat pixelFormat;
            [Range(1, 32)] public int maxTasks;
            // C# ext
            [HideInInspector] public int width;
            [HideInInspector] public int height;
            [HideInInspector] public int channels;

            public static fcPngConfig default_value
            {
                get
                {
                    return new fcPngConfig
                    {
                        pixelFormat = fcPngPixelFormat.Auto,
                        maxTasks = 2,
                    };
                }
            }
        };

        public struct fcPngContext
        {
            public IntPtr ptr;
            public void Release() { fcReleaseContext(ptr); ptr = IntPtr.Zero; }
            public static implicit operator bool(fcPngContext v) { return v.ptr != IntPtr.Zero; }
        }

        [DllImport ("fccore")] public static extern Bool         fcPngIsSupported();
        [DllImport ("fccore")] public static extern fcPngContext fcPngCreateContext(ref fcPngConfig conf);
        [DllImport ("fccore")] public static extern Bool         fcPngExportPixels(fcPngContext ctx, string path, byte[] pixels, int width, int height, fcPixelFormat fmt, int num_channels);


        // -------------------------------------------------------------
        // EXR Exporter
        // -------------------------------------------------------------

        public enum fcExrPixelFormat
        {
            Auto, // select optimal one for input data
            Half,
            Float,
            Int,
        };

        public enum fcExrCompression
        {
            None,
            RLE,
            ZipS, // par-line
            Zip,  // block
            PIZ,
        };

        [Serializable]
        public struct fcExrConfig
        {
            public fcExrPixelFormat pixelFormat;
            public fcExrCompression compression;
            [Range(1, 32)] public int maxTasks;
            // C# ext
            [HideInInspector] public int width;
            [HideInInspector] public int height;
            [HideInInspector] public int channels;

            public static fcExrConfig default_value
            {
                get
                {
                    return new fcExrConfig
                    {
                        pixelFormat = fcExrPixelFormat.Auto,
                        compression = fcExrCompression.Zip,
                        maxTasks = 2,
                    };
                }
            }
        };

        public struct fcExrContext
        {
            public IntPtr ptr;
            public void Release() { fcReleaseContext(ptr); ptr = IntPtr.Zero; }
            public static implicit operator bool(fcExrContext v) { return v.ptr != IntPtr.Zero; }
        }

        [DllImport ("fccore")] public static extern Bool         fcExrIsSupported();
        [DllImport ("fccore")] public static extern fcExrContext fcExrCreateContext(ref fcExrConfig conf);
        [DllImport ("fccore")] public static extern Bool         fcExrBeginImage(fcExrContext ctx, string path, int width, int height);
        [DllImport ("fccore")] public static extern Bool         fcExrAddLayerPixels(fcExrContext ctx, byte[] pixels, fcPixelFormat fmt, int ch, string name);
        [DllImport ("fccore")] public static extern Bool         fcExrEndImage(fcExrContext ctx);


        // -------------------------------------------------------------
        // GIF Exporter
        // -------------------------------------------------------------

        [Serializable]
        public struct fcGifConfig
        {
            [HideInInspector] public int width;
            [HideInInspector] public int height;
            [Range(1, 256)] public int numColors;
            [Range(1, 120)] public int keyframeInterval;
            [Range(1, 32)] public int maxTasks;

            public static fcGifConfig default_value
            {
                get
                {
                    return new fcGifConfig
                    {
                        numColors = 256,
                        maxTasks = 8,
                        keyframeInterval = 30,
                    };
                }
            }
        };
        public struct fcGifContext
        {
            public IntPtr ptr;
            public void Release() { fcReleaseContext(ptr); ptr = IntPtr.Zero; }
            public static implicit operator bool(fcGifContext v) { return v.ptr != IntPtr.Zero; }
        }

        [DllImport ("fccore")] public static extern Bool         fcGifIsSupported();
        [DllImport ("fccore")] public static extern fcGifContext fcGifCreateContext(ref fcGifConfig conf);
        [DllImport ("fccore")] public static extern void         fcGifAddOutputStream(fcGifContext ctx, fcStream stream);
        [DllImport ("fccore")] public static extern Bool         fcGifAddFramePixels(fcGifContext ctx, byte[] pixels, fcPixelFormat fmt, double timestamp = -1.0);


        // -------------------------------------------------------------
        // MP4 Exporter
        // -------------------------------------------------------------

        public enum fcMP4VideoFlags
        {
            H264NVIDIA  = 1 << 1,
            H264AMD     = 1 << 2,
            H264IntelHW = 1 << 3,
            H264IntelSW = 1 << 4,
            H264OpenH264= 1 << 5,
            H264Mask = H264NVIDIA | H264AMD | H264IntelHW | H264IntelSW | H264OpenH264,
        };

        public enum fcMP4AudioFlags
        {
            AACIntel = 1 << 1,
            AACFAAC  = 1 << 2,
            AACMask = AACIntel | AACFAAC,
        };

        [Serializable]
        public struct fcMP4Config
        {
            [HideInInspector] public Bool video;
            [HideInInspector] public int videoWidth;
            [HideInInspector] public int videoHeight;
            [HideInInspector] public int videoTargetFramerate;
            public fcBitrateMode videoBitrateMode;
            public int videoTargetBitrate;
            [HideInInspector] public int videoFlags;
            [Range(1, 32)] public int videoMaxTasks;

            [HideInInspector] public Bool audio;
            [HideInInspector] public int audioSampleRate;
            [HideInInspector] public int audioNumChannels;
            public fcBitrateMode audioBitrateMode;
            public int audioTargetBitrate;
            [HideInInspector] public int audioFlags;
            [Range(1, 32)] public int audioMaxTasks;

            public static fcMP4Config default_value
            {
                get
                {
                    return new fcMP4Config
                    {
                        video = true,
                        videoBitrateMode = fcBitrateMode.VBR,
                        videoTargetBitrate = 1024 * 1000,
                        videoTargetFramerate = 30,
                        videoFlags = (int)fcMP4VideoFlags.H264Mask,
                        videoMaxTasks = 4,

                        audio = true,
                        audioSampleRate = 48000,
                        audioNumChannels = 2,
                        audioBitrateMode = fcBitrateMode.VBR,
                        audioTargetBitrate = 128 * 1000,
                        audioFlags = (int)fcMP4AudioFlags.AACMask,
                        audioMaxTasks = 4,
                    };
                }
            }
        };
        public struct fcMP4Context
        {
            public IntPtr ptr;
            public void Release() { fcReleaseContext(ptr); ptr = IntPtr.Zero; }
            public static implicit operator bool(fcMP4Context v) { return v.ptr != IntPtr.Zero; }
        }

        [DllImport ("fccore")] public static extern Bool             fcMP4IsSupported();
        [DllImport ("fccore")] public static extern Bool             fcMP4OSIsSupported();
        [DllImport ("fccore")] public static extern fcMP4Context     fcMP4CreateContext(ref fcMP4Config conf);
        [DllImport ("fccore")] public static extern fcMP4Context     fcMP4OSCreateContext(ref fcMP4Config conf, string path);
        [DllImport ("fccore")] public static extern void             fcMP4AddOutputStream(fcMP4Context ctx, fcStream s);
        [DllImport ("fccore")] private static extern IntPtr          fcMP4GetAudioEncoderInfo(fcMP4Context ctx);
        [DllImport ("fccore")] private static extern IntPtr          fcMP4GetVideoEncoderInfo(fcMP4Context ctx);
        [DllImport ("fccore")] public static extern Bool             fcMP4AddVideoFramePixels(fcMP4Context ctx, byte[] pixels, fcPixelFormat fmt, double timestamp = -1.0);
        [DllImport ("fccore")] public static extern Bool             fcMP4AddAudioSamples(fcMP4Context ctx, float[] samples, int num_samples);

        public static string fcMP4GetAudioEncoderInfoS(fcMP4Context ctx)
        {
            return Marshal.PtrToStringAnsi(fcMP4GetAudioEncoderInfo(ctx));
        }

        public static string fcMP4GetVideoEncoderInfoS(fcMP4Context ctx)
        {
            return Marshal.PtrToStringAnsi(fcMP4GetVideoEncoderInfo(ctx));
        }


        // -------------------------------------------------------------
        // WebM Exporter
        // -------------------------------------------------------------

        public struct fcWebMContext
        {
            public IntPtr ptr;
            public void Release() { fcReleaseContext(ptr); ptr = IntPtr.Zero; }
            public static implicit operator bool(fcWebMContext v) { return v.ptr != IntPtr.Zero; }
        }

        public enum fcWebMVideoEncoder
        {
            VP8,
            VP9,
            VP9LossLess,
        };
        public enum fcWebMAudioEncoder
        {
            Vorbis,
            Opus,
        };

        [Serializable]
        public struct fcWebMConfig
        {
            [HideInInspector] public Bool video;
            public fcWebMVideoEncoder videoEncoder;
            [HideInInspector] public int videoWidth;
            [HideInInspector] public int videoHeight;
            [HideInInspector] public int videoTargetFramerate;
            public fcBitrateMode videoBitrateMode;
            public int videoTargetBitrate;
            [Range(1, 32)] public int videoMaxTasks;

            [HideInInspector] public Bool audio;
            public fcWebMAudioEncoder audioEncoder;
            [HideInInspector] public int audioSampleRate;
            [HideInInspector] public int audioNumChannels;
            public fcBitrateMode audioBitrateMode;
            public int audioTargetBitrate;
            [Range(1, 32)] public int audioMaxTasks;

            public static fcWebMConfig default_value
            {
                get
                {
                    return new fcWebMConfig
                    {
                        video = true,
                        videoEncoder = fcWebMVideoEncoder.VP8,
                        videoTargetFramerate = 60,
                        videoBitrateMode = fcBitrateMode.VBR,
                        videoTargetBitrate = 1024 * 1000,
                        videoMaxTasks = 4,

                        audio = true,
                        audioEncoder = fcWebMAudioEncoder.Vorbis,
                        audioSampleRate = 48000,
                        audioNumChannels = 2,
                        audioBitrateMode = fcBitrateMode.VBR,
                        audioTargetBitrate = 128 * 1000,
                        audioMaxTasks = 4,
                    };
                }
            }
        }

        [DllImport ("fccore")] public static extern Bool fcWebMIsSupported();
        [DllImport ("fccore")] public static extern fcWebMContext fcWebMCreateContext(ref fcWebMConfig conf);
        [DllImport ("fccore")] public static extern void fcWebMAddOutputStream(fcWebMContext ctx, fcStream stream);
        // timestamp=-1 is treated as current time.
        [DllImport ("fccore")] public static extern Bool fcWebMAddVideoFramePixels(fcWebMContext ctx, byte[] pixels, fcPixelFormat fmt, double timestamp = -1.0);
        // timestamp=-1 is treated as current time.
        [DllImport ("fccore")] public static extern Bool fcWebMAddAudioSamples(fcWebMContext ctx, float[] samples, int num_samples);


        // -------------------------------------------------------------
        // Wave Exporter
        // -------------------------------------------------------------
        public struct fcWaveContext
        {
            public IntPtr ptr;
            public void Release() { fcReleaseContext(ptr); ptr = IntPtr.Zero; }
            public static implicit operator bool(fcWaveContext v) { return v.ptr != IntPtr.Zero; }
        }

        [Serializable]
        public struct fcWaveConfig
        {
            [HideInInspector] public int sampleRate;
            [HideInInspector] public int numChannels;
            public fcAudioBitsPerSample bitsPerSample;
            [Range(1, 32)] public int maxTasks;

            public static fcWaveConfig default_value
            {
                get
                {
                    return new fcWaveConfig
                    {
                        sampleRate = 48000,
                        numChannels = 2,
                        bitsPerSample = fcAudioBitsPerSample._16Bits,
                        maxTasks = 2,
                    };
                }
            }
        }

        [DllImport ("fccore")] public static extern Bool fcWaveIsSupported();
        [DllImport ("fccore")] public static extern fcWaveContext fcWaveCreateContext(ref fcWaveConfig conf);
        [DllImport ("fccore")] public static extern void fcWaveAddOutputStream(fcWaveContext ctx, fcStream stream);
        [DllImport ("fccore")] public static extern Bool fcWaveAddAudioSamples(fcWaveContext ctx, float[] samples, int num_samples);

        

        // -------------------------------------------------------------
        // Ogg Exporter
        // -------------------------------------------------------------
        public struct fcOggContext
        {
            public IntPtr ptr;
            public void Release() { fcReleaseContext(ptr); ptr = IntPtr.Zero; }
            public static implicit operator bool(fcOggContext v) { return v.ptr != IntPtr.Zero; }
        }

        [Serializable]
        public struct fcOggConfig
        {
            [HideInInspector] public int sampleRate;
            [HideInInspector] public int numChannels;
            public fcBitrateMode bitrateMode;
            public int targetBitrate;
            [Range(1, 32)] public int maxTasks;

            public static fcOggConfig default_value
            {
                get
                {
                    return new fcOggConfig
                    {
                        sampleRate = 48000,
                        numChannels = 2,
                        bitrateMode = fcBitrateMode.VBR,
                        targetBitrate = 128 * 1000,
                        maxTasks = 2,
                    };
                }
            }
        }

        [DllImport ("fccore")] public static extern Bool fcOggIsSupported();
        [DllImport ("fccore")] public static extern fcOggContext fcOggCreateContext(ref fcOggConfig conf);
        [DllImport ("fccore")] public static extern void fcOggAddOutputStream(fcOggContext ctx, fcStream stream);
        [DllImport ("fccore")] public static extern Bool fcOggAddAudioSamples(fcOggContext ctx, float[] samples, int num_samples);


        // -------------------------------------------------------------
        // Flac Exporter
        // -------------------------------------------------------------
        public struct fcFlacContext
        {
            public IntPtr ptr;
            public void Release() { fcReleaseContext(ptr); ptr = IntPtr.Zero; }
            public static implicit operator bool(fcFlacContext v) { return v.ptr != IntPtr.Zero; }
        }

        [Serializable]
        public struct fcFlacConfig
        {
            [HideInInspector] public int sampleRate;
            [HideInInspector] public int numChannels;
            public fcAudioBitsPerSample bitsPerSample;
            [Range(0,9)] public int compressionLevel;
            public int blockSize;
            [HideInInspector] public Bool verify;
            [Range(1, 32)] public int maxTasks;

            public static fcFlacConfig default_value
            {
                get
                {
                    return new fcFlacConfig
                    {
                        sampleRate = 48000,
                        numChannels = 2,
                        bitsPerSample = fcAudioBitsPerSample._16Bits,
                        compressionLevel = 5,
                        blockSize = 0,
                        verify = false,
                        maxTasks = 2,
                    };
                }
            }
        }

        [DllImport ("fccore")] public static extern Bool fcFlacIsSupported();
        [DllImport ("fccore")] public static extern fcFlacContext fcFlacCreateContext(ref fcFlacConfig conf);
        [DllImport ("fccore")] public static extern void fcFlacAddOutputStream(fcFlacContext ctx, fcStream stream);
        [DllImport ("fccore")] public static extern Bool fcFlacAddAudioSamples(fcFlacContext ctx, float[] samples, int num_samples);




        public static void fcLock(RenderTexture src, TextureFormat dstfmt, Action<byte[], fcPixelFormat> body)
        {
            var tex = new Texture2D(src.width, src.height, dstfmt, false);
            RenderTexture.active = src;
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
            tex.Apply();
            body(tex.GetRawTextureData(), fcGetPixelFormat(tex.format));
            UnityEngine.Object.Destroy(tex);
        }

        public static void fcLock(RenderTexture src, Action<byte[], fcPixelFormat> body)
        {
            TextureFormat dstfmt = TextureFormat.RGBA32;
            switch (src.format)
            {
                case RenderTextureFormat.DefaultHDR:
                case RenderTextureFormat.ARGB2101010:
                case RenderTextureFormat.RGB111110Float:
                case RenderTextureFormat.ARGBHalf:
                case RenderTextureFormat.RGHalf:
                case RenderTextureFormat.Depth:
                case RenderTextureFormat.Shadowmap:
                case RenderTextureFormat.RHalf: dstfmt = TextureFormat.RGBAHalf; break;
                case RenderTextureFormat.ARGBFloat:
                case RenderTextureFormat.RGFloat:
                case RenderTextureFormat.RFloat: dstfmt = TextureFormat.RGBAFloat; break;
            }
            fcLock(src, dstfmt, body);
        }

        public static Mesh CreateFullscreenQuad()
        {
            var r = new Mesh();
            r.vertices = new Vector3[4] {
                    new Vector3( 1.0f, 1.0f, 0.0f),
                    new Vector3(-1.0f, 1.0f, 0.0f),
                    new Vector3(-1.0f,-1.0f, 0.0f),
                    new Vector3( 1.0f,-1.0f, 0.0f),
                };
            r.triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
            r.UploadMeshData(true);
            return r;
        }

#if UNITY_EDITOR
        //public static bool IsRenderingPathDeferred(Camera cam)
        //{
        //    if (cam.renderingPath == RenderingPath.DeferredShading ||
        //        (cam.renderingPath == RenderingPath.UsePlayerSettings &&
        //        UnityEditor.Rendering.EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, Graphics.activeTier).renderingPath == RenderingPath.DeferredShading))
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        public static Shader GetFrameBufferCopyShader()
        {
            return AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("2283fb92223c7914c9096670e29202c8"));
        }
#endif

    }
}
