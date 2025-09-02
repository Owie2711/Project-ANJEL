"""
Process Management Module
Handles scrcpy process lifecycle, auto-reconnect functionality, and process monitoring.
"""

import subprocess
import threading
import time
import os
from typing import Optional, Callable, Dict, Any, List
from abc import ABC, abstractmethod
from dataclasses import dataclass, field

# Optional import for process checking
try:
    import psutil  # type: ignore
    PSUTIL_AVAILABLE = True
except ImportError:
    psutil = None  # type: ignore
    PSUTIL_AVAILABLE = False


@dataclass
class ScrcpyConfig:
    """Configuration for scrcpy process"""
    device_id: str
    bitrate: str = "20M"
    framerate: int = 60
    fullscreen: bool = False
    audio_source: str = "playback"  # playback, mic-voice-communication, or none
    additional_args: List[str] = field(default_factory=list)
    
    def to_command_args(self) -> List[str]:
        """Convert configuration to scrcpy command arguments"""
        cmd = ['scrcpy', '-s', self.device_id, '-b', self.bitrate]
        
        # Add framerate
        cmd.extend(['--max-fps', str(self.framerate)])
        
        # Add fullscreen if enabled
        if self.fullscreen:
            cmd.extend(['-f'])
        
        # Add audio settings
        if self.audio_source == "playback":
            cmd.extend(['--audio-source=playback'])
        elif self.audio_source == "mic-voice-communication":
            cmd.extend(['--audio-source=mic-voice-communication'])
        elif self.audio_source == "none":
            cmd.extend(['--no-audio'])
        
        # Add any additional arguments
        cmd.extend(self.additional_args)
        
        return cmd


class ProcessEventListener(ABC):
    """Abstract base class for process event listeners"""
    
    @abstractmethod
    def on_process_started(self, config: ScrcpyConfig):
        """Called when the process starts successfully"""
        pass
    
    @abstractmethod
    def on_process_stopped(self, exit_code: Optional[int]):
        """Called when the process stops"""
        pass
    
    @abstractmethod
    def on_process_error(self, error: Exception):
        """Called when a process error occurs"""
        pass
    
    @abstractmethod
    def on_reconnect_attempt(self, attempt: int):
        """Called when auto-reconnect attempts to reconnect"""
        pass
    
    @abstractmethod
    def on_reconnect_success(self):
        """Called when auto-reconnect successfully reconnects"""
        pass


class ProcessStatus:
    """Process status enumeration"""
    STOPPED = "stopped"
    STARTING = "starting"
    RUNNING = "running"
    STOPPING = "stopping"
    RECONNECTING = "reconnecting"
    ERROR = "error"


class SystemProcessChecker:
    """Utility class for checking system-wide scrcpy processes"""
    
    @staticmethod
    def get_scrcpy_processes() -> List[int]:
        """Get list of running scrcpy process PIDs"""
        processes = []
        
        if PSUTIL_AVAILABLE and psutil is not None:
            try:
                for proc in psutil.process_iter(['pid', 'name', 'cmdline']):
                    if proc.info['name'] and 'scrcpy' in proc.info['name'].lower():
                        processes.append(proc.info['pid'])
                return processes
            except Exception as e:
                print(f"Error checking processes with psutil: {e}")
        
        # Fallback to platform-specific commands
        try:
            if os.name == 'nt':  # Windows
                result = subprocess.run(['tasklist', '/FI', 'IMAGENAME eq scrcpy.exe'], 
                                      capture_output=True, text=True, timeout=5)
                scrcpy_lines = [line for line in result.stdout.split('\n') if 'scrcpy.exe' in line]
                return list(range(len(scrcpy_lines)))  # Return dummy PIDs
            else:  # Unix-like systems
                result = subprocess.run(['pgrep', 'scrcpy'], 
                                      capture_output=True, text=True, timeout=5)
                if result.returncode == 0 and result.stdout.strip():
                    return [int(pid) for pid in result.stdout.strip().split('\n')]
        except Exception as e:
            print(f"Error checking processes: {e}")
        
        return []
    
    @staticmethod
    def has_running_scrcpy() -> bool:
        """Check if any scrcpy processes are running"""
        return len(SystemProcessChecker.get_scrcpy_processes()) > 0


class ScrcpyProcess:
    """Manages a single scrcpy process instance"""
    
    def __init__(self, config: ScrcpyConfig):
        self.config = config
        self.process: Optional[subprocess.Popen] = None
        self.status = ProcessStatus.STOPPED
        self._monitor_thread: Optional[threading.Thread] = None
        self._stop_monitoring = threading.Event()
    
    def start(self) -> bool:
        """Start the scrcpy process"""
        if self.status != ProcessStatus.STOPPED:
            return False
        
        try:
            self.status = ProcessStatus.STARTING
            cmd = self.config.to_command_args()
            print(f"Starting scrcpy with command: {' '.join(cmd)}")
            
            self.process = subprocess.Popen(cmd)
            self.status = ProcessStatus.RUNNING
            
            # Start monitoring thread
            self._stop_monitoring.clear()
            self._monitor_thread = threading.Thread(target=self._monitor_process, daemon=True)
            self._monitor_thread.start()
            
            return True
            
        except Exception as e:
            self.status = ProcessStatus.ERROR
            self.process = None
            raise e
    
    def stop(self, timeout: int = 5) -> bool:
        """Stop the scrcpy process"""
        if self.status not in [ProcessStatus.RUNNING, ProcessStatus.RECONNECTING]:
            return True
        
        self.status = ProcessStatus.STOPPING
        self._stop_monitoring.set()
        
        return self._cleanup_process(timeout)
    
    def is_running(self) -> bool:
        """Check if the process is currently running"""
        return (self.process is not None and 
                self.process.poll() is None and 
                self.status == ProcessStatus.RUNNING)
    
    def get_exit_code(self) -> Optional[int]:
        """Get the exit code of the process if it has finished"""
        if self.process:
            return self.process.poll()
        return None
    
    def _monitor_process(self):
        """Monitor the process in a background thread"""
        while not self._stop_monitoring.is_set() and self.process:
            if self.process.poll() is not None:
                # Process has ended
                self.status = ProcessStatus.STOPPED
                break
            
            self._stop_monitoring.wait(1)
    
    def _cleanup_process(self, timeout: int) -> bool:
        """Clean up the process"""
        if not self.process:
            return True
        
        try:
            # First try graceful termination
            if self.process.poll() is None:
                self.process.terminate()
                try:
                    self.process.wait(timeout=timeout)
                except subprocess.TimeoutExpired:
                    # Force kill if graceful termination fails
                    self.process.kill()
                    try:
                        self.process.wait(timeout=2)
                    except subprocess.TimeoutExpired:
                        return False
            
            self.process = None
            self.status = ProcessStatus.STOPPED
            return True
            
        except Exception as e:
            print(f"Error during process cleanup: {e}")
            self.process = None
            self.status = ProcessStatus.ERROR
            return False


class AutoReconnectManager:
    """Manages auto-reconnect functionality"""
    
    def __init__(self, device_checker: Callable[[str], bool]):
        self.device_checker = device_checker
        self.is_enabled = False
        self.attempt_count = 0
        self.max_attempts = 0  # 0 means unlimited
        self.retry_delay = 3.0
        self.listeners: List[ProcessEventListener] = []
        self._reconnect_thread: Optional[threading.Thread] = None
        self._stop_reconnecting = threading.Event()
    
    def add_listener(self, listener: ProcessEventListener):
        """Add a reconnect event listener"""
        if listener not in self.listeners:
            self.listeners.append(listener)
    
    def remove_listener(self, listener: ProcessEventListener):
        """Remove a reconnect event listener"""
        if listener in self.listeners:
            self.listeners.remove(listener)
    
    def start_reconnecting(self, target_device: str, restart_callback: Callable[[], bool]):
        """Start the auto-reconnect process"""
        if not self.is_enabled or self._reconnect_thread:
            return
        
        self.attempt_count = 0
        self._stop_reconnecting.clear()
        self._reconnect_thread = threading.Thread(
            target=self._reconnect_loop,
            args=(target_device, restart_callback),
            daemon=True
        )
        self._reconnect_thread.start()
    
    def stop_reconnecting(self):
        """Stop the auto-reconnect process"""
        self._stop_reconnecting.set()
        if self._reconnect_thread:
            self._reconnect_thread.join(timeout=2)
            self._reconnect_thread = None
        self.attempt_count = 0
    
    def _reconnect_loop(self, target_device: str, restart_callback: Callable[[], bool]):
        """Main reconnect loop running in background thread"""
        while not self._stop_reconnecting.is_set():
            if self.max_attempts > 0 and self.attempt_count >= self.max_attempts:
                print(f"Maximum reconnection attempts ({self.max_attempts}) reached")
                break
            
            self.attempt_count += 1
            
            # Notify listeners of reconnect attempt
            for listener in self.listeners:
                try:
                    listener.on_reconnect_attempt(self.attempt_count)
                except Exception as e:
                    print(f"Error notifying listener: {e}")
            
            print(f"Auto-reconnect attempt {self.attempt_count} for device {target_device}")
            
            # Check if device is available
            if self.device_checker(target_device):
                print(f"Device {target_device} is available, attempting restart...")
                
                # Attempt to restart
                if restart_callback():
                    # Notify listeners of successful reconnection
                    for listener in self.listeners:
                        try:
                            listener.on_reconnect_success()
                        except Exception as e:
                            print(f"Error notifying listener: {e}")
                    
                    self.attempt_count = 0
                    break
                else:
                    print("Failed to restart process")
            else:
                print(f"Device {target_device} not available yet")
            
            # Wait before next attempt
            self._stop_reconnecting.wait(self.retry_delay)
        
        self._reconnect_thread = None


class ProcessManager:
    """High-level process manager with auto-reconnect capabilities"""
    
    def __init__(self, device_checker: Callable[[str], bool]):
        self.current_process: Optional[ScrcpyProcess] = None
        self.current_config: Optional[ScrcpyConfig] = None
        self.status = ProcessStatus.STOPPED
        self.listeners: List[ProcessEventListener] = []
        self.auto_reconnect = AutoReconnectManager(device_checker)
        self.device_checker = device_checker
        
        # Setup auto-reconnect listeners
        self.auto_reconnect.add_listener(self)
    
    def add_listener(self, listener: ProcessEventListener):
        """Add a process event listener"""
        if listener not in self.listeners:
            self.listeners.append(listener)
    
    def remove_listener(self, listener: ProcessEventListener):
        """Remove a process event listener"""
        if listener in self.listeners:
            self.listeners.remove(listener)
    
    def start_process(self, config: ScrcpyConfig) -> bool:
        """Start a new scrcpy process"""
        if self.status != ProcessStatus.STOPPED:
            return False
        
        # Check for existing system processes
        if SystemProcessChecker.has_running_scrcpy():
            raise RuntimeError("Another scrcpy session is already running")
        
        try:
            self.current_config = config
            self.current_process = ScrcpyProcess(config)
            
            if self.current_process.start():
                self.status = ProcessStatus.RUNNING
                
                # Notify listeners
                for listener in self.listeners:
                    try:
                        listener.on_process_started(config)
                    except Exception as e:
                        print(f"Error notifying listener: {e}")
                
                # Start monitoring for disconnection
                self._start_disconnect_monitoring()
                
                return True
            else:
                self.current_process = None
                self.current_config = None
                return False
                
        except Exception as e:
            self.status = ProcessStatus.ERROR
            self.current_process = None
            self.current_config = None
            
            # Notify listeners
            for listener in self.listeners:
                try:
                    listener.on_process_error(e)
                except Exception as e2:
                    print(f"Error notifying listener: {e2}")
            
            raise e
    
    def stop_process(self) -> bool:
        """Stop the current process and disable auto-reconnect"""
        self.auto_reconnect.stop_reconnecting()
        
        if self.current_process:
            success = self.current_process.stop()
            exit_code = self.current_process.get_exit_code()
            
            # Notify listeners
            for listener in self.listeners:
                try:
                    listener.on_process_stopped(exit_code)
                except Exception as e:
                    print(f"Error notifying listener: {e}")
            
            self.current_process = None
        else:
            success = True
        
        self.current_config = None
        self.status = ProcessStatus.STOPPED
        return success
    
    def is_running(self) -> bool:
        """Check if a process is currently running"""
        return (self.current_process is not None and 
                self.current_process.is_running() and 
                self.status == ProcessStatus.RUNNING)
    
    def get_status(self) -> str:
        """Get the current process status"""
        return self.status
    
    def enable_auto_reconnect(self, enabled: bool = True, max_attempts: int = 0, retry_delay: float = 3.0):
        """Configure auto-reconnect settings"""
        self.auto_reconnect.is_enabled = enabled
        self.auto_reconnect.max_attempts = max_attempts
        self.auto_reconnect.retry_delay = retry_delay
    
    def _start_disconnect_monitoring(self):
        """Start monitoring for process disconnection"""
        if not self.auto_reconnect.is_enabled or not self.current_config:
            return
        
        def monitor():
            while (self.current_process and 
                   self.status == ProcessStatus.RUNNING and 
                   self.auto_reconnect.is_enabled):
                
                if not self.current_process.is_running():
                    print("Process ended, checking for auto-reconnect...")
                    
                    if self.auto_reconnect.is_enabled and self.current_config:
                        self.status = ProcessStatus.RECONNECTING
                        self.auto_reconnect.start_reconnecting(
                            self.current_config.device_id,
                            self._attempt_restart
                        )
                    else:
                        self.status = ProcessStatus.STOPPED
                    break
                
                time.sleep(1)
        
        thread = threading.Thread(target=monitor, daemon=True)
        thread.start()
    
    def _attempt_restart(self) -> bool:
        """Attempt to restart the process with the same configuration"""
        if not self.current_config:
            return False
        
        try:
            # Clean up current process
            if self.current_process:
                self.current_process.stop()
            
            # Start new process
            self.current_process = ScrcpyProcess(self.current_config)
            if self.current_process.start():
                self.status = ProcessStatus.RUNNING
                
                # Notify listeners that process restarted
                for listener in self.listeners:
                    try:
                        listener.on_process_started(self.current_config)
                    except Exception as e:
                        print(f"Error notifying listener of restart: {e}")
                
                # Start monitoring again for disconnection
                self._start_disconnect_monitoring()
                
                return True
            else:
                return False
                
        except Exception as e:
            print(f"Error restarting process: {e}")
            return False
    
    # ProcessEventListener implementation for auto-reconnect
    def on_process_started(self, config: ScrcpyConfig):
        pass  # Handled in start_process
    
    def on_process_stopped(self, exit_code: Optional[int]):
        pass  # Handled in stop_process
    
    def on_process_error(self, error: Exception):
        pass  # Handled in start_process
    
    def on_reconnect_attempt(self, attempt: int):
        # Forward to other listeners
        for listener in self.listeners:
            if listener != self:
                try:
                    listener.on_reconnect_attempt(attempt)
                except Exception as e:
                    print(f"Error notifying listener: {e}")
    
    def on_reconnect_success(self):
        # Forward to other listeners
        for listener in self.listeners:
            if listener != self:
                try:
                    listener.on_reconnect_success()
                except Exception as e:
                    print(f"Error notifying listener: {e}")