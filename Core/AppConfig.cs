using Newtonsoft.Json;
using System.ComponentModel;

namespace ScrcpyController.Core
{
    /// <summary>
    /// Application configuration data class
    /// </summary>
    public class AppConfig : INotifyPropertyChanged
    {
        private string _bitrate = "20";
        private int _framerate = 60;
        private bool _fullscreenEnabled = false;
        private string _audioSource = "Audio Playback";
        private bool _autoReconnectEnabled = false;
        private bool _noControlEnabled = false;
        private string _videoResolution = "Original Device Resolution";
        private int _reconnectMaxAttempts = 0; // 0 = unlimited
        private double _reconnectDelay = 3.0;
        private bool _skipProcessCheck = true; // Skip scrcpy process detection (enabled by default)
        private int _windowWidth = 400;
        private int _windowHeight = 600;
        private string _theme = "light";
        private string _lastSelectedDevice = "";
        private double _deviceRefreshInterval = 3.0;
        private List<string> _additionalScrcpyArgs = new();

        // Video settings
        [JsonProperty("bitrate")]
        public string Bitrate
        {
            get => _bitrate;
            set { _bitrate = value; OnPropertyChanged(); }
        }

        [JsonProperty("framerate")]
        public int Framerate
        {
            get => _framerate;
            set { _framerate = value; OnPropertyChanged(); }
        }

        [JsonProperty("fullscreen_enabled")]
        public bool FullscreenEnabled
        {
            get => _fullscreenEnabled;
            set { _fullscreenEnabled = value; OnPropertyChanged(); }
        }

        // Audio settings
        [JsonProperty("audio_source")]
        public string AudioSource
        {
            get => _audioSource;
            set { _audioSource = value; OnPropertyChanged(); }
        }

        // Connection settings
        [JsonProperty("auto_reconnect_enabled")]
        public bool AutoReconnectEnabled
        {
            get => _autoReconnectEnabled;
            set { _autoReconnectEnabled = value; OnPropertyChanged(); }
        }

        [JsonProperty("no_control_enabled")]
        public bool NoControlEnabled
        {
            get => _noControlEnabled;
            set { _noControlEnabled = value; OnPropertyChanged(); }
        }

        [JsonProperty("video_resolution")]
        public string VideoResolution
        {
            get => _videoResolution;
            set { _videoResolution = value; OnPropertyChanged(); }
        }

        [JsonProperty("reconnect_max_attempts")]
        public int ReconnectMaxAttempts
        {
            get => _reconnectMaxAttempts;
            set { _reconnectMaxAttempts = value; OnPropertyChanged(); }
        }

        [JsonProperty("reconnect_delay")]
        public double ReconnectDelay
        {
            get => _reconnectDelay;
            set { _reconnectDelay = value; OnPropertyChanged(); }
        }

        // Process management settings
        [JsonProperty("skip_process_check")]
        public bool SkipProcessCheck
        {
            get => _skipProcessCheck;
            set { _skipProcessCheck = value; OnPropertyChanged(); }
        }

        // UI settings
        [JsonProperty("window_width")]
        public int WindowWidth
        {
            get => _windowWidth;
            set { _windowWidth = value; OnPropertyChanged(); }
        }

        [JsonProperty("window_height")]
        public int WindowHeight
        {
            get => _windowHeight;
            set { _windowHeight = value; OnPropertyChanged(); }
        }

        [JsonProperty("theme")]
        public string Theme
        {
            get => _theme;
            set { _theme = value; OnPropertyChanged(); }
        }

        // Device settings
        [JsonProperty("last_selected_device")]
        public string LastSelectedDevice
        {
            get => _lastSelectedDevice;
            set { _lastSelectedDevice = value; OnPropertyChanged(); }
        }

        [JsonProperty("device_refresh_interval")]
        public double DeviceRefreshInterval
        {
            get => _deviceRefreshInterval;
            set { _deviceRefreshInterval = value; OnPropertyChanged(); }
        }

        // Advanced settings
        [JsonProperty("additional_scrcpy_args")]
        public List<string> AdditionalScrcpyArgs
        {
            get => _additionalScrcpyArgs;
            set { _additionalScrcpyArgs = value ?? new(); OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Get internal audio source value for scrcpy command
        /// </summary>
        public string GetAudioSourceInternal()
        {
            return AudioSource switch
            {
                "Audio Playback" => "playback",
                "Microphone" => "mic-voice-communication",
                "No audio" => "none",
                _ => "playback"
            };
        }
    }
}