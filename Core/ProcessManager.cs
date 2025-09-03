using System.Diagnostics;
using System.Management;

namespace ScrcpyController.Core
{
    /// <summary>
    /// SCRCPY configuration for process execution
    /// </summary>
    public class ScrcpyConfig
    {
        public string DeviceId { get; set; } = "";
        public string Bitrate { get; set; } = "20M";
        public int Framerate { get; set; } = 60;
        public bool Fullscreen { get; set; } = false;
        public bool NoControl { get; set; } = false;
        public string VideoResolution { get; set; } = "Original Device Resolution";
        public string AudioSource { get; set; } = "playback";
        public List<string> AdditionalArgs { get; set; } = new();

        public ScrcpyConfig(string deviceId)
        {
            DeviceId = deviceId;
        }

        public List<string> ToCommandArgs()
        {
            string scrcpyPath = ScrcpyPathResolver.FindScrcpyExecutable();
            var cmd = new List<string> { scrcpyPath, "-s", DeviceId, "-b", Bitrate };

            // Add framerate
            cmd.AddRange(new[] { "--max-fps", Framerate.ToString() });

            // Add video resolution
            if (VideoResolution != "Original Device Resolution")
            {
                string maxSize = VideoResolution switch
                {
                    "720p" => "1280",
                    "1080p" => "1920",
                    "4K" => "3840",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(maxSize))
                {
                    cmd.AddRange(new[] { "--max-size", maxSize });
                }
            }

            // Add window positioning to ensure visibility (but keep original device size)
            cmd.AddRange(new[] { "--window-x=100", "--window-y=100" });

            // Add fullscreen if enabled
            if (Fullscreen)
                cmd.Add("-f");

            // Add audio settings
            switch (AudioSource)
            {
                case "playbook":
                    cmd.Add("--audio-source=playbook");
                    break;
                case "mic-voice-communication":
                    cmd.Add("--audio-source=mic-voice-communication");
                    break;
                case "none":
                    cmd.Add("--no-audio");
                    break;
            }

            // Add no-control if enabled
            if (NoControl)
                cmd.Add("--no-control");

            // Add any additional arguments
            cmd.AddRange(AdditionalArgs);

            return cmd;
        }
    }

    /// <summary>
    /// Process status enumeration
    /// </summary>
    public enum ProcessStatus
    {
        Stopped,
        Starting,
        Running,
        Stopping,
        Reconnecting,
        Error
    }

    /// <summary>
    /// Interface for process event listeners
    /// </summary>
    public interface IProcessEventListener
    {
        void OnProcessStarted(ScrcpyConfig config);
        void OnProcessStopped(int? exitCode);
        void OnProcessError(Exception error);
        void OnReconnectAttempt(int attempt);
        void OnReconnectSuccess();
    }

    /// <summary>
    /// SCRCPY path resolver utility
    /// </summary>
    public static class ScrcpyPathResolver
    {
        public static string GetApplicationDirectory()
        {
            if (Application.ExecutablePath != null)
            {
                return Path.GetDirectoryName(Application.ExecutablePath) ?? Environment.CurrentDirectory;
            }
            return Environment.CurrentDirectory;
        }

        public static string FindScrcpyExecutable()
        {
            string appDir = GetApplicationDirectory();

            // Check for scrcpy in the same directory as the application
            string[] localScrcpyPaths = {
                Path.Combine(appDir, "scrcpy", "scrcpy.exe"),
                Path.Combine(appDir, "scrcpy.exe"),
                Path.Combine(appDir, "bin", "scrcpy.exe")
            };

            foreach (string path in localScrcpyPaths)
            {
                if (File.Exists(path))
                {
                    Debug.WriteLine($"Found local scrcpy at: {path}");
                    return path;
                }
            }

            // Fallback to system PATH
            Debug.WriteLine("Local scrcpy not found, using system PATH");
            return "scrcpy";
        }
    }

    /// <summary>
    /// System-wide scrcpy process checker
    /// </summary>
    public static class SystemProcessChecker
    {
        public static List<int> GetScrcpyProcesses()
        {
            var processes = new List<int>();

            try
            {
                // First try using Process.GetProcessesByName
                var scrcpyProcesses = Process.GetProcessesByName("scrcpy");
                foreach (var proc in scrcpyProcesses)
                {
                    try
                    {
                        if (!proc.HasExited)
                            processes.Add(proc.Id);
                    }
                    catch
                    {
                        // Process might have exited between calls
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }

                // If no processes found, try WMI (more thorough but slower)
                if (processes.Count == 0)
                {
                    try
                    {
                        using var searcher = new ManagementObjectSearcher("SELECT ProcessId FROM Win32_Process WHERE Name = 'scrcpy.exe'");
                        using var results = searcher.Get();
                        
                        foreach (ManagementObject result in results)
                        {
                            if (result["ProcessId"] is uint processId)
                                processes.Add((int)processId);
                        }
                    }
                    catch
                    {
                        // WMI might not be available or accessible
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking scrcpy processes: {ex.Message}");
            }

            return processes;
        }

        public static bool HasRunningScrcpy()
        {
            return GetScrcpyProcesses().Count > 0;
        }

        public static int KillExistingScrcpyProcesses()
        {
            int killedCount = 0;

            try
            {
                var processIds = GetScrcpyProcesses();
                
                foreach (int processId in processIds)
                {
                    try
                    {
                        using var process = Process.GetProcessById(processId);
                        if (!process.HasExited)
                        {
                            process.Kill();
                            process.WaitForExit(3000);
                            killedCount++;
                            Debug.WriteLine($"Killed scrcpy process PID: {processId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error killing process {processId}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error killing scrcpy processes: {ex.Message}");
            }

            return killedCount;
        }
    }

    /// <summary>
    /// Single scrcpy process instance manager
    /// </summary>
    public class ScrcpyProcess : IDisposable
    {
        private Process? _process;
        private ProcessStatus _status;
        private readonly ScrcpyConfig _config;
        private CancellationTokenSource? _monitorCancellation;

        public ProcessStatus Status => _status;
        public ScrcpyConfig Config => _config;

        public event EventHandler<int>? ProcessExited;

        public ScrcpyProcess(ScrcpyConfig config)
        {
            _config = config;
            _status = ProcessStatus.Stopped;
        }

        public Task<bool> StartAsync()
        {
            if (_status != ProcessStatus.Stopped)
                return Task.FromResult(false);

            try
            {
                _status = ProcessStatus.Starting;
                var args = _config.ToCommandArgs();
                
                Debug.WriteLine($"Starting scrcpy with command: {string.Join(" ", args)}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = args[0],
                    Arguments = string.Join(" ", args.Skip(1).Select(arg => $"\"{arg}\"")),
                    UseShellExecute = false,
                    CreateNoWindow = true, // Hide console output but allow GUI windows
                    RedirectStandardOutput = true, // Capture and suppress console output
                    RedirectStandardError = true   // Capture and suppress error output
                };

                _process = Process.Start(startInfo);
                
                if (_process != null)
                {
                    Debug.WriteLine($"Scrcpy process started with PID: {_process.Id}");
                    _status = ProcessStatus.Running;

                    // Start monitoring in background
                    _monitorCancellation = new CancellationTokenSource();
                    _ = Task.Run(() => MonitorProcess(_monitorCancellation.Token));

                    return Task.FromResult(true);
                }
                else
                {
                    _status = ProcessStatus.Error;
                    return Task.FromResult(false);
                }
            }
            catch (Exception ex)
            {
                _status = ProcessStatus.Error;
                Debug.WriteLine($"Error starting scrcpy: {ex.Message}");
                throw;
            }
        }

        public bool Start()
        {
            return StartAsync().GetAwaiter().GetResult();
        }

        public async Task<bool> StopAsync(int timeoutMs = 5000)
        {
            if (_status != ProcessStatus.Running && _status != ProcessStatus.Reconnecting)
                return true;

            _status = ProcessStatus.Stopping;
            _monitorCancellation?.Cancel();

            return await CleanupProcessAsync(timeoutMs);
        }

        public bool Stop(int timeoutMs = 5000)
        {
            return StopAsync(timeoutMs).GetAwaiter().GetResult();
        }

        public bool IsRunning()
        {
            return _process != null && !_process.HasExited && _status == ProcessStatus.Running;
        }

        public int? GetExitCode()
        {
            try
            {
                return _process?.HasExited == true ? _process.ExitCode : null;
            }
            catch
            {
                return null;
            }
        }

        private async Task MonitorProcess(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _process != null)
                {
                    try
                    {
                        if (_process.HasExited)
                        {
                            int exitCode = _process.ExitCode;
                            Debug.WriteLine($"Scrcpy process ended with exit code: {exitCode}");
                            _status = ProcessStatus.Stopped;
                            ProcessExited?.Invoke(this, exitCode);
                            break;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Process may have exited between checks - this is normal
                        Debug.WriteLine("Process monitoring detected process exit");
                        _status = ProcessStatus.Stopped;
                        ProcessExited?.Invoke(this, -1); // Unknown exit code
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error checking process status: {ex.Message}");
                        // Continue monitoring - don't break for minor errors
                    }

                    try
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancellation is requested
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                Debug.WriteLine("Process monitoring cancelled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Critical error in process monitoring: {ex.Message}");
                _status = ProcessStatus.Error;
            }
        }

        private async Task<bool> CleanupProcessAsync(int timeoutMs)
        {
            if (_process == null)
                return true;

            try
            {
                if (!_process.HasExited)
                {
                    try
                    {
                        // Try graceful close first
                        _process.CloseMainWindow();
                        
                        if (!await WaitForExitAsync(_process, timeoutMs / 2))
                        {
                            // Force kill if graceful close failed
                            try
                            {
                                if (!_process.HasExited)
                                {
                                    _process.Kill();
                                    await WaitForExitAsync(_process, timeoutMs / 2);
                                }
                            }
                            catch (InvalidOperationException)
                            {
                                // Process already exited - this is okay
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error force-killing process: {ex.Message}");
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Process already exited - this is okay
                    }
                }

                _process.Dispose();
                _process = null;
                _status = ProcessStatus.Stopped;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during process cleanup: {ex.Message}");
                try
                {
                    _process?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
                _process = null;
                _status = ProcessStatus.Error;
                return false;
            }
        }

        private static async Task<bool> WaitForExitAsync(Process process, int timeoutMs)
        {
            try
            {
                return await Task.Run(() => process.WaitForExit(timeoutMs));
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _monitorCancellation?.Cancel();
            _monitorCancellation?.Dispose();
            _process?.Dispose();
        }
    }

    /// <summary>
    /// Auto-reconnect manager
    /// </summary>
    public class AutoReconnectManager : IDisposable
    {
        private readonly Func<string, bool> _deviceChecker;
        private readonly List<IProcessEventListener> _listeners;
        private CancellationTokenSource? _reconnectCancellation;
        private Task? _reconnectTask;

        public bool IsEnabled { get; set; } = false;
        public int AttemptCount { get; private set; } = 0;
        public int MaxAttempts { get; set; } = 0; // 0 means unlimited
        public double RetryDelay { get; set; } = 3.0;

        public AutoReconnectManager(Func<string, bool> deviceChecker)
        {
            _deviceChecker = deviceChecker;
            _listeners = new List<IProcessEventListener>();
        }

        public void AddListener(IProcessEventListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void RemoveListener(IProcessEventListener listener)
        {
            _listeners.Remove(listener);
        }

        public void StartReconnecting(string targetDevice, Func<Task<bool>> restartCallback)
        {
            if (!IsEnabled || _reconnectTask != null)
                return;

            AttemptCount = 0;
            _reconnectCancellation = new CancellationTokenSource();
            _reconnectTask = Task.Run(() => ReconnectLoop(targetDevice, restartCallback, _reconnectCancellation.Token));
        }

        public void StopReconnecting()
        {
            try
            {
                _reconnectCancellation?.Cancel();
                
                if (_reconnectTask != null)
                {
                    try
                    {
                        _reconnectTask.Wait(2000);
                    }
                    catch (AggregateException)
                    {
                        // Task was cancelled or had errors - this is expected
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error waiting for reconnect task: {ex.Message}");
                    }
                    
                    _reconnectTask = null;
                }
                
                try
                {
                    _reconnectCancellation?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error disposing cancellation token: {ex.Message}");
                }
                
                _reconnectCancellation = null;
                AttemptCount = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping reconnection: {ex.Message}");
                // Ensure cleanup even if there are errors
                _reconnectTask = null;
                _reconnectCancellation = null;
                AttemptCount = 0;
            }
        }

        private async Task ReconnectLoop(string targetDevice, Func<Task<bool>> restartCallback, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (MaxAttempts > 0 && AttemptCount >= MaxAttempts)
                    {
                        Debug.WriteLine($"Maximum reconnection attempts ({MaxAttempts}) reached");
                        break;
                    }

                    AttemptCount++;

                    // Notify listeners of reconnect attempt
                    foreach (var listener in _listeners)
                    {
                        try
                        {
                            listener.OnReconnectAttempt(AttemptCount);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error notifying listener: {ex.Message}");
                        }
                    }

                    Debug.WriteLine($"Auto-reconnect attempt {AttemptCount} for device {targetDevice}");

                    // Check if device is available
                    try
                    {
                        if (_deviceChecker(targetDevice))
                        {
                            Debug.WriteLine($"Device {targetDevice} is available, attempting restart...");

                            // Attempt to restart
                            if (await restartCallback())
                            {
                                // Notify listeners of successful reconnection
                                foreach (var listener in _listeners)
                                {
                                    try
                                    {
                                        listener.OnReconnectSuccess();
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error notifying listener: {ex.Message}");
                                    }
                                }

                                AttemptCount = 0;
                                break;
                            }
                            else
                            {
                                Debug.WriteLine("Failed to restart process");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Device {targetDevice} not available yet");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error checking device availability: {ex.Message}");
                    }

                    // Wait before next attempt
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(RetryDelay), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Critical error in reconnect loop: {ex.Message}");
            }
            finally
            {
                _reconnectTask = null;
            }
        }

        public void Dispose()
        {
            StopReconnecting();
        }
    }

    /// <summary>
    /// High-level process manager with auto-reconnect capabilities
    /// </summary>
    public class ProcessManager : IDisposable, IProcessEventListener
    {
        private ScrcpyProcess? _currentProcess;
        private ScrcpyConfig? _currentConfig;
        private ProcessStatus _status;
        private readonly List<IProcessEventListener> _listeners;
        private readonly AutoReconnectManager _autoReconnect;
        private readonly Func<string, bool> _deviceChecker;

        public ProcessStatus Status => _status;
        public bool IsRunning => _currentProcess?.IsRunning() == true && _status == ProcessStatus.Running;

        public event EventHandler<ScrcpyConfig>? ProcessStarted;
        public event EventHandler<int?>? ProcessStopped;
        public event EventHandler<Exception>? ProcessError;

        public ProcessManager(Func<string, bool> deviceChecker)
        {
            _deviceChecker = deviceChecker;
            _status = ProcessStatus.Stopped;
            _listeners = new List<IProcessEventListener>();
            _autoReconnect = new AutoReconnectManager(deviceChecker);
            _autoReconnect.AddListener(this);
        }

        public void AddListener(IProcessEventListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void RemoveListener(IProcessEventListener listener)
        {
            _listeners.Remove(listener);
        }

        public void EnableAutoReconnect(bool enabled = true, int maxAttempts = 0, double retryDelay = 3.0)
        {
            _autoReconnect.IsEnabled = enabled;
            _autoReconnect.MaxAttempts = maxAttempts;
            _autoReconnect.RetryDelay = retryDelay;
        }

        public async Task<bool> StartProcessAsync(ScrcpyConfig config, bool forceStart = false, bool skipProcessCheck = false)
        {
            if (_status != ProcessStatus.Stopped)
                return false;

            // Check for existing system processes (unless skipped)
            if (!skipProcessCheck && SystemProcessChecker.HasRunningScrcpy())
            {
                if (forceStart)
                {
                    Debug.WriteLine("Force start enabled - killing existing scrcpy processes...");
                    int killedCount = SystemProcessChecker.KillExistingScrcpyProcesses();
                    if (killedCount > 0)
                    {
                        Debug.WriteLine($"Killed {killedCount} existing scrcpy process(es)");
                        await Task.Delay(1000); // Wait for cleanup
                    }
                }
                else
                {
                    var existingProcesses = SystemProcessChecker.GetScrcpyProcesses();
                    int processCount = existingProcesses.Count;
                    
                    Debug.WriteLine($"Process detection found {processCount} scrcpy processes: {string.Join(", ", existingProcesses)}");
                    
                    string errorMsg = processCount > 0 
                        ? $"Another scrcpy session is already running (Found {processCount} process(es)). Please close any existing scrcpy windows and try again."
                        : "Process detection indicates scrcpy is running, but no processes were found. Try restarting the application.";
                    
                    throw new InvalidOperationException(errorMsg);
                }
            }

            try
            {
                _currentConfig = config;
                _currentProcess = new ScrcpyProcess(config);
                
                // Subscribe to process events
                _currentProcess.ProcessExited += OnCurrentProcessExited;
                
                if (await _currentProcess.StartAsync())
                {
                    _status = ProcessStatus.Running;
                    
                    // Notify listeners
                    NotifyListeners(l => l.OnProcessStarted(config));
                    ProcessStarted?.Invoke(this, config);
                    
                    // Always start monitoring for process lifecycle
                    StartProcessMonitoring();
                    
                    return true;
                }
                else
                {
                    _currentProcess.Dispose();
                    _currentProcess = null;
                    _currentConfig = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _status = ProcessStatus.Error;
                _currentProcess?.Dispose();
                _currentProcess = null;
                _currentConfig = null;
                
                // Notify listeners
                NotifyListeners(l => l.OnProcessError(ex));
                ProcessError?.Invoke(this, ex);
                
                throw;
            }
        }

        public bool StartProcess(ScrcpyConfig config, bool forceStart = false, bool skipProcessCheck = false)
        {
            return StartProcessAsync(config, forceStart, skipProcessCheck).GetAwaiter().GetResult();
        }

        public async Task<bool> StopProcessAsync()
        {
            _autoReconnect.StopReconnecting();
            
            bool success = true;
            int? exitCode = null;
            
            if (_currentProcess != null)
            {
                exitCode = _currentProcess.GetExitCode();
                success = await _currentProcess.StopAsync();
                
                _currentProcess.ProcessExited -= OnCurrentProcessExited;
                _currentProcess.Dispose();
                _currentProcess = null;
            }
            
            _currentConfig = null;
            _status = ProcessStatus.Stopped;
            
            // Notify listeners
            NotifyListeners(l => l.OnProcessStopped(exitCode));
            ProcessStopped?.Invoke(this, exitCode);
            
            return success;
        }

        public bool StopProcess()
        {
            return StopProcessAsync().GetAwaiter().GetResult();
        }

        private void OnCurrentProcessExited(object? sender, int exitCode)
        {
            Debug.WriteLine($"Process ended, determining action...");
            
            // Check if auto-reconnect should be attempted
            if (_autoReconnect.IsEnabled && _currentConfig != null && exitCode != 0)
            {
                Debug.WriteLine($"Process ended unexpectedly (exit code: {exitCode}), starting auto-reconnect for device: {_currentConfig.DeviceId}");
                _status = ProcessStatus.Reconnecting;
                
                // Notify listeners that process stopped
                NotifyListeners(l => l.OnProcessStopped(exitCode));
                ProcessStopped?.Invoke(this, exitCode);
                
                _autoReconnect.StartReconnecting(_currentConfig.DeviceId, AttemptRestartAsync);
            }
            else
            {
                // Normal termination or auto-reconnect disabled
                if (exitCode == 0)
                    Debug.WriteLine("Process ended normally (user closed window)");
                else
                    Debug.WriteLine($"Process ended with exit code {exitCode}, auto-reconnect disabled");
                
                _status = ProcessStatus.Stopped;
                
                // Notify listeners that process stopped
                NotifyListeners(l => l.OnProcessStopped(exitCode));
                ProcessStopped?.Invoke(this, exitCode);
            }
        }

        private void StartProcessMonitoring()
        {
            // Monitoring is handled by the ScrcpyProcess itself via the ProcessExited event
        }

        private async Task<bool> AttemptRestartAsync()
        {
            if (_currentConfig == null)
                return false;

            try
            {
                // Clean up current process
                if (_currentProcess != null)
                {
                    try
                    {
                        await _currentProcess.StopAsync();
                        _currentProcess.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error stopping current process: {ex.Message}");
                    }
                }

                // Kill any existing scrcpy processes before restarting
                try
                {
                    if (SystemProcessChecker.HasRunningScrcpy())
                    {
                        int killedCount = SystemProcessChecker.KillExistingScrcpyProcesses();
                        if (killedCount > 0)
                        {
                            Debug.WriteLine($"Auto-reconnect: Killed {killedCount} existing process(es)");
                            await Task.Delay(1000); // Wait for cleanup
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cleaning up processes: {ex.Message}");
                }

                // Start new process
                try
                {
                    _currentProcess = new ScrcpyProcess(_currentConfig);
                    _currentProcess.ProcessExited += OnCurrentProcessExited;
                    
                    if (await _currentProcess.StartAsync())
                    {
                        _status = ProcessStatus.Running;
                        
                        // Notify listeners that process restarted
                        NotifyListeners(l => l.OnProcessStarted(_currentConfig));
                        ProcessStarted?.Invoke(this, _currentConfig);
                        
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("Failed to start new process");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error starting new process: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error restarting process: {ex.Message}");
                return false;
            }
        }

        private void NotifyListeners(Action<IProcessEventListener> action)
        {
            foreach (var listener in _listeners)
            {
                try
                {
                    action(listener);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error notifying listener: {ex.Message}");
                }
            }
        }

        // IProcessEventListener implementation for auto-reconnect forwarding
        public void OnProcessStarted(ScrcpyConfig config) { }
        public void OnProcessStopped(int? exitCode) { }
        public void OnProcessError(Exception error) { }

        public void OnReconnectAttempt(int attempt)
        {
            NotifyListeners(l => l.OnReconnectAttempt(attempt));
        }

        public void OnReconnectSuccess()
        {
            NotifyListeners(l => l.OnReconnectSuccess());
        }

        public void Dispose()
        {
            _autoReconnect.StopReconnecting();
            _currentProcess?.Dispose();
            _autoReconnect.Dispose();
        }
    }
}