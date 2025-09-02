using Newtonsoft.Json;
using System.Reflection;

namespace ScrcpyController.Core
{
    /// <summary>
    /// Configuration manager with validation and auto-save functionality
    /// </summary>
    public class ConfigManager : IDisposable
    {
        private readonly string _configFileName;
        private AppConfig _config;
        private readonly Dictionary<string, IConfigValidator> _validators;
        private readonly List<Action<string, object?>> _changeListeners;
        private readonly SemaphoreSlim _saveSemaphore = new(1, 1);
        private volatile bool _isSaving = false;
        private bool _disposed = false;

        public AppConfig Config => _config;

        public ConfigManager(string configFileName = "config.json")
        {
            _configFileName = configFileName;
            _config = new AppConfig();
            _validators = new Dictionary<string, IConfigValidator>();
            _changeListeners = new List<Action<string, object?>>();
            
            SetupDefaultValidators();
            
            // Listen to property changes for auto-save
            _config.PropertyChanged += async (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.PropertyName) && !_isSaving)
                {
                    try
                    {
                        var value = GetConfigValue(e.PropertyName);
                        NotifyChangeListeners(e.PropertyName, value);
                        await SaveConfigAsync(); // Auto-save with proper async handling
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in auto-save: {ex.Message}");
                    }
                }
            };
        }

        private void SetupDefaultValidators()
        {
            _validators["Bitrate"] = new BitrateValidator();
            _validators["Framerate"] = new RangeValidator<int>(1, 240);
            _validators["AudioSource"] = new ChoiceValidator<string>(new[] { "Audio Playback", "Microphone", "No audio" });
            _validators["ReconnectMaxAttempts"] = new RangeValidator<int>(0, 100);
            _validators["ReconnectDelay"] = new RangeValidator<double>(0.1, 60.0);
            _validators["WindowWidth"] = new RangeValidator<int>(300, 2000);
            _validators["WindowHeight"] = new RangeValidator<int>(400, 1500);
            _validators["DeviceRefreshInterval"] = new RangeValidator<double>(0.5, 30.0);
        }

        public string GetConfigPath()
        {
            string appDir;
            
            if (Application.ExecutablePath != null)
            {
                // Running as compiled executable
                appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? Environment.CurrentDirectory;
            }
            else
            {
                // Running in development
                appDir = Environment.CurrentDirectory;
            }
            
            return Path.Combine(appDir, _configFileName);
        }

        public bool LoadConfig()
        {
            try
            {
                string configPath = GetConfigPath();
                
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var loadedConfig = JsonConvert.DeserializeObject<AppConfig>(json);
                    
                    if (loadedConfig != null)
                    {
                        // Validate loaded configuration
                        if (ValidateConfig(loadedConfig))
                        {
                            _config = loadedConfig;
                            System.Diagnostics.Debug.WriteLine("Configuration loaded and validated successfully");
                            return true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Configuration validation failed, using defaults");
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
                return false;
            }
        }

        public bool SaveConfig()
        {
            try
            {
                string configPath = GetConfigPath();
                string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(configPath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveConfigAsync()
        {
            // Prevent concurrent saves and avoid recursive calls
            if (_isSaving || !await _saveSemaphore.WaitAsync(100))
                return false;

            try
            {
                _isSaving = true;
                return await Task.Run(SaveConfig);
            }
            finally
            {
                _isSaving = false;
                _saveSemaphore.Release();
            }
        }

        public T Get<T>(string key, T defaultValue = default!)
        {
            try
            {
                var value = GetConfigValue(key);
                if (value is T typedValue)
                    return typedValue;
                
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public void Set<T>(string key, T value)
        {
            try
            {
                SetConfigValue(key, value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting config value: {ex.Message}");
            }
        }

        public (bool IsValid, string ErrorMessage) ValidateField(string fieldName, object? value)
        {
            if (_validators.TryGetValue(fieldName, out var validator))
            {
                return validator.Validate(value);
            }
            
            return (true, "Valid");
        }

        public bool ValidateConfig(AppConfig config)
        {
            // Validate all fields using reflection
            var properties = typeof(AppConfig).GetProperties();
            
            foreach (var property in properties)
            {
                if (_validators.TryGetValue(property.Name, out var validator))
                {
                    var value = property.GetValue(config);
                    var (isValid, _) = validator.Validate(value);
                    
                    if (!isValid)
                        return false;
                }
            }
            
            return true;
        }

        public void AddChangeListener(Action<string, object?> listener)
        {
            _changeListeners.Add(listener);
        }

        public void RemoveChangeListener(Action<string, object?> listener)
        {
            _changeListeners.Remove(listener);
        }

        private void NotifyChangeListeners(string propertyName, object? value)
        {
            foreach (var listener in _changeListeners)
            {
                try
                {
                    listener(propertyName, value);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in change listener: {ex.Message}");
                }
            }
        }

        private object? GetConfigValue(string propertyName)
        {
            var property = typeof(AppConfig).GetProperty(propertyName);
            return property?.GetValue(_config);
        }

        private void SetConfigValue(string propertyName, object? value)
        {
            var property = typeof(AppConfig).GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(_config, value);
            }
        }

        /// <summary>
        /// Dispose of resources and save config one final time
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // Save config one final time
                SaveConfig();
                
                // Dispose semaphore
                _saveSemaphore?.Dispose();
                
                // Clear listeners
                _changeListeners?.Clear();
                _validators?.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during ConfigManager disposal: {ex.Message}");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}