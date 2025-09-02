"""
Device Management Module
Handles ADB device scanning, connection checking, and device-related operations.
"""

import subprocess
import threading
import time
from typing import List, Optional, Callable, Dict, Any
from abc import ABC, abstractmethod


class DeviceConnectionListener(ABC):
    """Abstract base class for device connection event listeners"""
    
    @abstractmethod
    def on_devices_changed(self, devices: List[str]):
        """Called when the device list changes"""
        pass
    
    @abstractmethod
    def on_device_connected(self, device_id: str):
        """Called when a device connects"""
        pass
    
    @abstractmethod
    def on_device_disconnected(self, device_id: str):
        """Called when a device disconnects"""
        pass


class DeviceInfo:
    """Data class for device information"""
    
    def __init__(self, device_id: str, status: str = "device", name: Optional[str] = None):
        self.device_id = device_id
        self.status = status
        self.name = name or device_id
        self.is_connected = status == "device"
    
    def __str__(self):
        return f"{self.name} ({self.device_id})" if self.name != self.device_id else self.device_id
    
    def __eq__(self, other):
        if isinstance(other, DeviceInfo):
            return self.device_id == other.device_id
        return False
    
    def __hash__(self):
        return hash(self.device_id)


class ADBManager:
    """Manages ADB operations and device communication"""
    
    @staticmethod
    def is_adb_available() -> bool:
        """Check if ADB is available in the system PATH"""
        try:
            result = subprocess.run(['adb', 'version'], capture_output=True, timeout=5)
            return result.returncode == 0
        except (FileNotFoundError, subprocess.TimeoutExpired):
            return False
    
    @staticmethod
    def get_connected_devices(timeout: int = 10) -> List[DeviceInfo]:
        """Get list of connected ADB devices"""
        try:
            result = subprocess.run(['adb', 'devices'], 
                                  capture_output=True, text=True, timeout=timeout)
            
            if result.returncode != 0:
                return []
            
            devices = []
            lines = result.stdout.strip().split('\n')
            
            for line in lines[1:]:  # Skip "List of devices attached"
                if line.strip() and '\t' in line:
                    parts = line.split('\t')
                    if len(parts) >= 2:
                        device_id = parts[0].strip()
                        status = parts[1].strip()
                        devices.append(DeviceInfo(device_id, status))
            
            return devices
            
        except (FileNotFoundError, subprocess.TimeoutExpired, Exception):
            return []
    
    @staticmethod
    def get_device_name(device_id: str) -> Optional[str]:
        """Get the human-readable name of a device"""
        try:
            # Try to get device model
            result = subprocess.run(['adb', '-s', device_id, 'shell', 'getprop', 'ro.product.model'], 
                                  capture_output=True, text=True, timeout=5)
            if result.returncode == 0 and result.stdout.strip():
                return result.stdout.strip()
        except:
            pass
        
        return None
    
    @staticmethod
    def is_device_connected(device_id: str) -> bool:
        """Check if a specific device is currently connected"""
        devices = ADBManager.get_connected_devices()
        return any(device.device_id == device_id and device.is_connected for device in devices)


class DeviceManager:
    """High-level device management with automatic monitoring"""
    
    def __init__(self, refresh_interval: float = 3.0):
        self.refresh_interval = refresh_interval
        self.listeners: List[DeviceConnectionListener] = []
        self.current_devices: List[DeviceInfo] = []
        self.selected_device: Optional[str] = None
        self.is_monitoring = False
        self._monitor_thread: Optional[threading.Thread] = None
        self._stop_monitoring = threading.Event()
        
        # Device name cache
        self._device_name_cache: Dict[str, str] = {}
    
    def add_listener(self, listener: DeviceConnectionListener):
        """Add a device connection listener"""
        if listener not in self.listeners:
            self.listeners.append(listener)
    
    def remove_listener(self, listener: DeviceConnectionListener):
        """Remove a device connection listener"""
        if listener in self.listeners:
            self.listeners.remove(listener)
    
    def start_monitoring(self):
        """Start automatic device monitoring"""
        if self.is_monitoring:
            return
        
        self.is_monitoring = True
        self._stop_monitoring.clear()
        self._monitor_thread = threading.Thread(target=self._monitor_devices, daemon=True)
        self._monitor_thread.start()
    
    def stop_monitoring(self):
        """Stop automatic device monitoring"""
        if not self.is_monitoring:
            return
        
        self.is_monitoring = False
        self._stop_monitoring.set()
        if self._monitor_thread:
            self._monitor_thread.join(timeout=2)
    
    def refresh_devices(self, notify_listeners: bool = True) -> List[DeviceInfo]:
        """Manually refresh the device list"""
        old_devices = set(self.current_devices)
        new_devices_raw = ADBManager.get_connected_devices()
        
        # Enhance device info with names
        new_devices = []
        for device in new_devices_raw:
            if device.device_id not in self._device_name_cache:
                name = ADBManager.get_device_name(device.device_id)
                if name:
                    self._device_name_cache[device.device_id] = name
            
            if device.device_id in self._device_name_cache:
                device.name = self._device_name_cache[device.device_id]
            
            new_devices.append(device)
        
        self.current_devices = new_devices
        
        if notify_listeners:
            self._notify_device_changes(old_devices, set(new_devices))
        
        return self.current_devices
    
    def get_devices(self) -> List[DeviceInfo]:
        """Get the current list of devices"""
        return self.current_devices.copy()
    
    def get_device_ids(self) -> List[str]:
        """Get list of connected device IDs"""
        return [device.device_id for device in self.current_devices if device.is_connected]
    
    def select_device(self, device_id: str) -> bool:
        """Select a device by ID"""
        if any(device.device_id == device_id and device.is_connected for device in self.current_devices):
            self.selected_device = device_id
            return True
        return False
    
    def get_selected_device(self) -> Optional[str]:
        """Get the currently selected device ID"""
        return self.selected_device
    
    def is_device_connected(self, device_id: str) -> bool:
        """Check if a specific device is currently connected"""
        return any(device.device_id == device_id and device.is_connected for device in self.current_devices)
    
    def _monitor_devices(self):
        """Background thread for monitoring device changes"""
        while not self._stop_monitoring.is_set():
            try:
                self.refresh_devices()
                self._stop_monitoring.wait(self.refresh_interval)
            except Exception as e:
                print(f"Error in device monitoring: {e}")
                self._stop_monitoring.wait(1)
    
    def _notify_device_changes(self, old_devices: set, new_devices: set):
        """Notify listeners of device changes"""
        # Determine connected and disconnected devices
        connected = new_devices - old_devices
        disconnected = old_devices - new_devices
        
        # Notify about changes
        if connected or disconnected:
            device_ids = [device.device_id for device in new_devices if device.is_connected]
            for listener in self.listeners:
                try:
                    listener.on_devices_changed(device_ids)
                except Exception as e:
                    print(f"Error notifying listener: {e}")
        
        # Notify about individual connections
        for device in connected:
            if device.is_connected:
                for listener in self.listeners:
                    try:
                        listener.on_device_connected(device.device_id)
                    except Exception as e:
                        print(f"Error notifying listener: {e}")
        
        # Notify about individual disconnections
        for device in disconnected:
            for listener in self.listeners:
                try:
                    listener.on_device_disconnected(device.device_id)
                except Exception as e:
                    print(f"Error notifying listener: {e}")


class DeviceValidator:
    """Validates device operations and provides helpful feedback"""
    
    @staticmethod
    def validate_device_selection(device_id: str, available_devices: List[str]) -> tuple[bool, str]:
        """Validate device selection"""
        if not device_id or device_id.strip() == "":
            return False, "No device selected"
        
        if device_id == "Connected Devices:":
            return False, "Please select a device from the dropdown"
        
        if device_id not in available_devices:
            return False, f"Device '{device_id}' is not available or disconnected"
        
        return True, "Device selection is valid"
    
    @staticmethod
    def check_adb_requirements() -> tuple[bool, str]:
        """Check if ADB requirements are met"""
        if not ADBManager.is_adb_available():
            return False, "ADB not found. Please install Android SDK platform-tools and add to PATH"
        
        return True, "ADB is available"