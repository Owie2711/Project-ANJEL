using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ScrcpyController.Core
{
    /// <summary>
    /// Device information data class
    /// </summary>
    public class DeviceInfo
    {
        public string DeviceId { get; set; } = "";
        public string Status { get; set; } = "device";
        public string Name { get; set; } = "";
        public bool IsConnected => Status == "device";

        public DeviceInfo(string deviceId, string status = "device", string? name = null)
        {
            DeviceId = deviceId;
            Status = status;
            Name = name ?? deviceId;
        }

        public override string ToString()
        {
            return Name != DeviceId ? $"{Name} ({DeviceId})" : DeviceId;
        }

        public override bool Equals(object? obj)
        {
            return obj is DeviceInfo other && DeviceId == other.DeviceId;
        }

        public override int GetHashCode()
        {
            return DeviceId.GetHashCode();
        }
    }

    /// <summary>
    /// Interface for device connection event listeners
    /// </summary>
    public interface IDeviceConnectionListener
    {
        void OnDevicesChanged(List<string> devices);
        void OnDeviceConnected(string deviceId);
        void OnDeviceDisconnected(string deviceId);
    }

    /// <summary>
    /// ADB path resolver utility
    /// </summary>
    public static class ADBPathResolver
    {
        public static string GetApplicationDirectory()
        {
            if (Application.ExecutablePath != null)
            {
                return Path.GetDirectoryName(Application.ExecutablePath) ?? Environment.CurrentDirectory;
            }
            return Environment.CurrentDirectory;
        }

        public static string FindAdbExecutable()
        {
            string appDir = GetApplicationDirectory();
            
            // Check for ADB in the same directory as the application
            string[] localAdbPaths = {
                Path.Combine(appDir, "platform-tools", "adb.exe"),
                Path.Combine(appDir, "adb.exe"),
                Path.Combine(appDir, "bin", "adb.exe"),
                Path.Combine(appDir, "scrcpy", "adb.exe")
            };

            foreach (string path in localAdbPaths)
            {
                if (File.Exists(path))
                {
                    Debug.WriteLine($"Found local ADB at: {path}");
                    return path;
                }
            }

            // Fallback to system PATH
            Debug.WriteLine("Local ADB not found, using system PATH");
            return "adb";
        }
    }

    /// <summary>
    /// ADB operations manager
    /// </summary>
    public static class ADBManager
    {
        public static bool IsAdbAvailable()
        {
            try
            {
                string adbPath = ADBPathResolver.FindAdbExecutable();
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = "version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    process.WaitForExit(5000);
                    return process.ExitCode == 0;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] IsAdbAvailable: {ex.Message}");
                return false;
            }
        }

        public static async Task<List<DeviceInfo>> GetConnectedDevicesAsync(int timeoutMs = 10000)
        {
            try
            {
                string adbPath = ADBPathResolver.FindAdbExecutable();
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = "devices",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return new List<DeviceInfo>();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                
                if (await Task.Run(() => process.WaitForExit(timeoutMs)))
                {
                    if (process.ExitCode != 0)
                        return new List<DeviceInfo>();

                    string output = await outputTask;
                    return ParseDevicesOutput(output);
                }
                
                return new List<DeviceInfo>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetConnectedDevicesAsync: {ex.Message}");
                return new List<DeviceInfo>();
            }
        }

        public static List<DeviceInfo> GetConnectedDevices(int timeoutMs = 10000)
        {
            return GetConnectedDevicesAsync(timeoutMs).GetAwaiter().GetResult();
        }

        public static async Task<string?> GetDeviceNameAsync(string deviceId)
        {
            try
            {
                string adbPath = ADBPathResolver.FindAdbExecutable();
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = $"-s {deviceId} shell getprop ro.product.model",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return null;

                var outputTask = process.StandardOutput.ReadToEndAsync();
                
                if (await Task.Run(() => process.WaitForExit(5000)))
                {
                    if (process.ExitCode == 0)
                    {
                        string output = await outputTask;
                        return output.Trim();
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetDeviceNameAsync: {ex.Message}");
                return null;
            }
        }

        public static bool IsDeviceConnected(string deviceId)
        {
            var devices = GetConnectedDevices();
            return devices.Any(d => d.DeviceId == deviceId && d.IsConnected);
        }

        private static List<DeviceInfo> ParseDevicesOutput(string output)
        {
            var devices = new List<DeviceInfo>();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines.Skip(1)) // Skip "List of devices attached"
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || !trimmedLine.Contains('\t'))
                    continue;

                var parts = trimmedLine.Split('\t');
                if (parts.Length >= 2)
                {
                    string deviceId = parts[0].Trim();
                    string status = parts[1].Trim();
                    devices.Add(new DeviceInfo(deviceId, status));
                }
            }

            return devices;
        }
    }

    /// <summary>
    /// High-level device management with automatic monitoring
    /// </summary>
    public class DeviceManager : IDisposable
    {
        private readonly System.Threading.Timer _monitorTimer;
        private readonly List<IDeviceConnectionListener> _listeners;
        private readonly Dictionary<string, string> _deviceNameCache;
        private List<DeviceInfo> _currentDevices;
        private string? _selectedDevice;
        private bool _isMonitoring;
        private readonly double _refreshInterval;

        public event EventHandler<List<DeviceInfo>>? DevicesChanged;
        public event EventHandler<string>? DeviceConnected;
        public event EventHandler<string>? DeviceDisconnected;

        public List<DeviceInfo> CurrentDevices => new(_currentDevices);
        public string? SelectedDevice
        {
            get => _selectedDevice;
            set => _selectedDevice = value;
        }

        public DeviceManager(double refreshInterval = 3.0)
        {
            _refreshInterval = refreshInterval;
            _listeners = new List<IDeviceConnectionListener>();
            _deviceNameCache = new Dictionary<string, string>();
            _currentDevices = new List<DeviceInfo>();
            
            // Create timer but don't start it yet
            _monitorTimer = new System.Threading.Timer(MonitorDevices, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void AddListener(IDeviceConnectionListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void RemoveListener(IDeviceConnectionListener listener)
        {
            _listeners.Remove(listener);
        }

        public void StartMonitoring()
        {
            if (_isMonitoring)
                return;

            _isMonitoring = true;
            int intervalMs = (int)(_refreshInterval * 1000);
            _monitorTimer.Change(0, intervalMs); // Start immediately, then repeat
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;

            _isMonitoring = false;
            _monitorTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public async Task RefreshDevicesAsync()
        {
            try
            {
                var oldDevices = new HashSet<DeviceInfo>(_currentDevices ?? new List<DeviceInfo>());
                var newDevicesRaw = await ADBManager.GetConnectedDevicesAsync();
                
                if (newDevicesRaw == null)
                {
                    Debug.WriteLine("Failed to get devices - ADB may not be available");
                    return;
                }
                
                // Enhance device info with names
                var newDevices = new List<DeviceInfo>();
                foreach (var device in newDevicesRaw)
                {
                    if (device == null || string.IsNullOrEmpty(device.DeviceId))
                        continue;
                        
                    try
                    {
                        if (!_deviceNameCache.ContainsKey(device.DeviceId))
                        {
                            string? name = await ADBManager.GetDeviceNameAsync(device.DeviceId);
                            if (!string.IsNullOrEmpty(name))
                                _deviceNameCache[device.DeviceId] = name;
                        }

                        if (_deviceNameCache.TryGetValue(device.DeviceId, out string? cachedName))
                            device.Name = cachedName;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error getting device name for {device.DeviceId}: {ex.Message}");
                        // Continue with default device name
                    }

                    newDevices.Add(device);
                }

                var newDevicesSet = new HashSet<DeviceInfo>(newDevices);
                _currentDevices = newDevices;

                // Notify about changes if monitoring
                if (_isMonitoring)
                {
                    try
                    {
                        NotifyDeviceChanges(oldDevices, newDevicesSet);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error notifying device changes: {ex.Message}");
                    }
                }

                // Raise event safely
                try
                {
                    DevicesChanged?.Invoke(this, newDevices);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error raising DevicesChanged event: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Critical error refreshing devices: {ex.Message}");
                // Don't throw - just log the error to prevent cascading failures
            }
        }

        public void RefreshDevices()
        {
            Task.Run(RefreshDevicesAsync);
        }

        public List<string> GetDeviceIds()
        {
            return _currentDevices.Where(d => d.IsConnected).Select(d => d.DeviceId).ToList();
        }

        public bool IsDeviceConnected(string deviceId)
        {
            return _currentDevices.Any(d => d.DeviceId == deviceId && d.IsConnected);
        }

        private async void MonitorDevices(object? state)
        {
            if (!_isMonitoring)
                return;

            try
            {
                await RefreshDevicesAsync();
            }
            catch (ObjectDisposedException)
            {
                // Timer was disposed - stop monitoring
                _isMonitoring = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in device monitoring: {ex.Message}");
                // Continue monitoring even after errors - don't stop the timer
            }
        }

        private void NotifyDeviceChanges(HashSet<DeviceInfo> oldDevices, HashSet<DeviceInfo> newDevices)
        {
            try
            {
                // Determine connected and disconnected devices
                var connected = newDevices.Except(oldDevices);
                var disconnected = oldDevices.Except(newDevices);

                // Notify about changes
                if (connected.Any() || disconnected.Any())
                {
                    var deviceIds = newDevices.Where(d => d.IsConnected).Select(d => d.DeviceId).ToList();
                    
                    foreach (var listener in _listeners)
                    {
                        try
                        {
                            listener.OnDevicesChanged(deviceIds);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error notifying listener: {ex.Message}");
                        }
                    }
                }

                // Notify about individual connections
                foreach (var device in connected.Where(d => d.IsConnected))
                {
                    DeviceConnected?.Invoke(this, device.DeviceId);
                    
                    foreach (var listener in _listeners)
                    {
                        try
                        {
                            listener.OnDeviceConnected(device.DeviceId);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error notifying listener: {ex.Message}");
                        }
                    }
                }

                // Notify about individual disconnections
                foreach (var device in disconnected)
                {
                    DeviceDisconnected?.Invoke(this, device.DeviceId);
                    
                    foreach (var listener in _listeners)
                    {
                        try
                        {
                            listener.OnDeviceDisconnected(device.DeviceId);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error notifying listener: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error notifying device changes: {ex.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                StopMonitoring();
                
                try
                {
                    _monitorTimer?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error disposing monitor timer: {ex.Message}");
                }
                
                // Clear listeners safely
                try
                {
                    _listeners?.Clear();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error clearing listeners: {ex.Message}");
                }
                
                // Clear cache safely
                try
                {
                    _deviceNameCache?.Clear();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error clearing device cache: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DeviceManager disposal: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Device validation utilities
    /// </summary>
    public static class DeviceValidator
    {
        public static (bool IsValid, string ErrorMessage) ValidateDeviceSelection(string? deviceId, List<string> availableDevices)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return (false, "No device selected");

            if (deviceId == "Connected Devices:")
                return (false, "Please select a device from the dropdown");

            if (!availableDevices.Contains(deviceId))
                return (false, $"Device '{deviceId}' is not available or disconnected");

            return (true, "Device selection is valid");
        }

        public static (bool IsValid, string ErrorMessage) CheckAdbRequirements()
        {
            if (!ADBManager.IsAdbAvailable())
                return (false, "ADB not found. Please install Android SDK platform-tools and add to PATH");

            return (true, "ADB is available");
        }
    }
}