using System;

namespace VivoxUnity
{
    /// <summary>
    /// Used during initialization to set sdk up with a custom configuration.
    /// </summary>
    public class VivoxConfig
    {
        private vx_sdk_config_t vx_sdk_config = new vx_sdk_config_t();
        
        /// <summary>
        /// Internally used for mapping to Core sdk type
        /// </summary>
        public vx_sdk_config_t ToVx_Sdk_Config()
        {
            return vx_sdk_config;
        }

        /// <summary>
        /// Number of threads used for encoding/decoding audio. Must be 1 for client SDKs.
        /// </summary>
        public int CodecThreads
        {
            get { return vx_sdk_config.num_codec_threads; }
            set { vx_sdk_config.num_codec_threads = value; }
        }

        /// <summary>
        /// Number of threads used for voice processing. Must be 1 for client SDKs.
        /// </summary>
        public int VoiceThreads
        {
            get { return vx_sdk_config.num_voice_threads; }
            set { vx_sdk_config.num_voice_threads = value; }
        }

        /// <summary>
        /// Number of threads used for web requests. Must be 1 for client SDKs.
        /// </summary>
        public int WebThreads
        {
            get { return vx_sdk_config.num_web_threads; }
            set { vx_sdk_config.num_web_threads = value; }
        }

        /// <summary>
        /// Render Source Max Queue Depth.
        /// </summary>
        public int RenderSourceQueueDepthMax
        {
            get { return vx_sdk_config.render_source_queue_depth_max; }
            set { vx_sdk_config.render_source_queue_depth_max = value; }
        }

        /// <summary>
        /// Render Source Initial Buffer Count.
        /// </summary>
        public int RenderSourceInitialBufferCount
        {
            get { return vx_sdk_config.render_source_initial_buffer_count; }
            set { vx_sdk_config.render_source_initial_buffer_count = value; }
        }

        /// <summary>
        /// Upstream jitter frame count
        /// </summary>
        public int UpstreamJitterFrameCount
        {
            get { return vx_sdk_config.upstream_jitter_frame_count; }
            set { vx_sdk_config.upstream_jitter_frame_count = value; }
        }

        /// <summary>
        /// max logins per user
        /// </summary>
        public int MaxLoginsPerUser
        {
            get { return vx_sdk_config.max_logins_per_user; }
            set { vx_sdk_config.max_logins_per_user = value; }
        }

        /// <summary>
        /// Initial Log Level
        /// Severity level of logs: -1 = no logging, 0 = errors only, 1 = warnings, 2 = info, 3 = debug, 4 = trace, 5 = log all
        /// </summary>
        public vx_log_level InitialLogLevel
        {
            get { return vx_sdk_config.initial_log_level; }
            set { vx_sdk_config.initial_log_level = value; }
        }

        /// <summary>
        /// Disable Audio Device Polling Using Timer
        /// </summary>
        public bool DisableDevicePolling
        {
            get { return Convert.ToBoolean(vx_sdk_config.disable_device_polling); }
            set { vx_sdk_config.disable_device_polling = Convert.ToInt32(value); }
        }

        /// <summary>
        /// Diagnostic purposes only.
        /// </summary>
        public bool ForceCaptureSilence
        {
            get { return Convert.ToBoolean(vx_sdk_config.force_capture_silence); }
            set { vx_sdk_config.force_capture_silence = Convert.ToInt32(value); }
        }

        /// <summary>
        /// Enable advanced automatic settings of audio levels
        /// </summary>
        public bool EnableAdvancedAutoLevels
        {
            get { return Convert.ToBoolean(vx_sdk_config.enable_advanced_auto_levels); }
            set { vx_sdk_config.enable_advanced_auto_levels = Convert.ToInt32(value); }
        }

        /// <summary>
        /// Number of 20 millisecond buffers for the capture device.
        /// </summary>
        public int CaptureDeviceBufferSizeIntervals
        {
            get { return vx_sdk_config.capture_device_buffer_size_intervals; }
            set { vx_sdk_config.capture_device_buffer_size_intervals = value; }
        }

        /// <summary>
        /// Number of 20 millisecond buffers for the render device.
        /// </summary>
        public int RenderDeviceBufferSizeIntervals
        {
            get { return vx_sdk_config.render_device_buffer_size_intervals; }
            set { vx_sdk_config.render_device_buffer_size_intervals = value; }
        }

        /// <summary>
        /// XBox One and iOS.
        /// </summary>
        public bool DisableAudioDucking
        {
            get { return Convert.ToBoolean(vx_sdk_config.disable_audio_ducking); }
            set { vx_sdk_config.disable_audio_ducking = Convert.ToInt32(value); }
        }

        /// <summary>
        /// Default of 1 for most platforms. Changes to this value must be coordinated with Vivox.
        /// </summary>
        public bool EnableDtx
        {
            get { return Convert.ToBoolean(vx_sdk_config.enable_dtx); }
            set { vx_sdk_config.enable_dtx = Convert.ToInt32(value); }
        }

        /// <summary>
        /// Default codec mask that will be used to initialize connector's configured_codecs.
        /// codec type none = 0, codec type siren14 = 1, codec type pcmu = 2, codec type nm = 3,
        /// codec type speex = 4, codec type siren7 = 5, codec type opus = 6
        /// </summary>
        public media_codec_type DefaultCodecsMask
        {
            get { return (media_codec_type)vx_sdk_config.default_codecs_mask; }
            set { vx_sdk_config.default_codecs_mask = (uint)value; }
        }

        /// <summary>
        /// Enable Fast Network Change Detection. Default of disable.
        /// </summary>
        public bool EnableFastNetworkChangeDetection
        {
            get { return Convert.ToBoolean(vx_sdk_config.enable_fast_network_change_detection); }
            set { vx_sdk_config.enable_fast_network_change_detection = Convert.ToInt32(value); }
        }

        /// <summary>
        /// Use Operating System Configured Proxy Settings (Windows Only) (default: 0 or 1 if environment variable "VIVOX_USE_OS_PROXY_SETTINGS" is set)
        /// </summary>
        public int UseOsProxySettings
        {
            get { return vx_sdk_config.use_os_proxy_settings; }
            set { vx_sdk_config.use_os_proxy_settings = value; }
        }

        /// <summary>
        /// Enable Dynamic Voice Processing Switching. Default value is true.
        /// If enabled, the SDK will automatically switch between hardware and software AECs.
        /// To disable set value to 0.
        /// </summary>
        public bool DynamicVoiceProcessingSwitching
        {
            get { return Convert.ToBoolean(vx_sdk_config.dynamic_voice_processing_switching); }
            set { vx_sdk_config.dynamic_voice_processing_switching = Convert.ToInt32(value); }
        }

        /// <summary>
        /// Number of millseconds to wait before disconnecting audio due to RTP timeout at initial call time. Zero or negative value turns off the guard (not recommended).
        /// </summary>
        public int NeverRtpTimeoutMs
        {
            get { return vx_sdk_config.never_rtp_timeout_ms; }
            set { vx_sdk_config.never_rtp_timeout_ms = value; }
        }

        /// <summary>
        /// Number of millseconds to wait before disconnecting audio due to RTP timeout after the call has been established. Zero or negative value turns off the guard (not recommended).
        /// </summary>
        public int LostRtpTimeoutMs
        {
            get { return vx_sdk_config.lost_rtp_timeout_ms; }
            set { vx_sdk_config.lost_rtp_timeout_ms = value; }
        }
    }
}
