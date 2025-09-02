"""
Modular Scrcpy GUI Application
A refactored version of the Scrcpy Controller with modular components.
"""

import tkinter as tk
from tkinter import messagebox
import customtkinter as ctk
import threading
import os
from typing import Optional

# Import modular components
from ui_components import setup_theme, UITheme
from ui_layouts import MainLayout
from device_manager import DeviceManager, DeviceConnectionListener, DeviceValidator
from process_manager import ProcessManager, ProcessEventListener, ScrcpyConfig
from config_manager import AutoSaveConfigManager


class ScrcpyGUI(ProcessEventListener, DeviceConnectionListener):
    """Main GUI class using modular components"""
    
    def __init__(self, root):
        self.root = root
        self.root.title("Scrcpy Controller")
        self.root.geometry("350x560")
        self.root.resizable(True, True)
        self.root.minsize(350, 560)
        
        # Setup theme
        setup_theme()
        self.root.configure(bg=UITheme.BACKGROUND)
        
        # Initialize managers
        self.config_manager = AutoSaveConfigManager()
        self.device_manager = DeviceManager()
        self.process_manager = ProcessManager(lambda device_id: self.device_manager.is_device_connected(device_id))
        
        # Add listeners
        self.device_manager.add_listener(self)
        self.process_manager.add_listener(self)
        
        # State variables
        self.is_running = False
        self.last_connected_device: Optional[str] = None
        
        # Setup UI and load configuration
        self.setup_ui()
        
        # Load configuration after setup
        self.root.after(100, self.load_config)
        
        # Start device monitoring
        self.device_manager.start_monitoring()
    
    def setup_ui(self):
        """Setup the main UI using modular components"""
        # Configure root grid
        self.root.grid_rowconfigure(0, weight=1)
        self.root.grid_columnconfigure(0, weight=1)
        
        # Create main layout
        self.main_layout = MainLayout(self.root, self.device_manager, self.config_manager)
        self.main_layout.grid(row=0, column=0, sticky="nsew", padx=4, pady=4)
        
        # Setup section callbacks
        self._setup_section_callbacks()
    
    def _setup_section_callbacks(self):
        """Setup callbacks for UI sections"""
        # Device section callbacks
        device_section = self.main_layout.get_device_section()
        device_section.set_refresh_callback(self.refresh_devices)
        device_section.set_device_selection_callback(self._on_device_selected)
        
        # Video section callbacks
        video_section = self.main_layout.get_video_section()
        video_section.add_change_callback(self._on_setting_changed)
        
        # Audio section callbacks
        audio_section = self.main_layout.get_audio_section()
        audio_section.add_change_callback(self._on_setting_changed)
        
        # Control section callbacks
        control_section = self.main_layout.get_control_section()
        control_section.set_start_callback(self.start_scrcpy)
        control_section.set_stop_callback(self.stop_scrcpy)
    
    def _on_setting_changed(self, setting_name: str, value):
        """Handle setting changes from UI sections"""
        # Update process manager auto-reconnect settings
        if setting_name == 'auto_reconnect_enabled':
            self.process_manager.enable_auto_reconnect(value, max_attempts=0)  # Unlimited attempts
            print(f"Auto-reconnect {'enabled' if value else 'disabled'}")
        
        # Auto-save is handled by AutoSaveConfigManager
    
    def _on_device_selected(self, device_id: str):
        """Handle device selection change"""
        # Save last selected device to config
        if device_id and device_id != "Connected Devices:":
            self.config_manager.set('last_selected_device', device_id)
    
    def refresh_devices(self):
        """Refresh the list of connected ADB devices"""
        self.device_manager.refresh_devices()
    
    def start_scrcpy(self):
        """Start scrcpy with selected device and settings"""
        if self.is_running:
            return
        
        try:
            # Get current settings from UI
            device_section = self.main_layout.get_device_section()
            video_section = self.main_layout.get_video_section()
            audio_section = self.main_layout.get_audio_section()
            
            device_id = device_section.get_selected_device()
            video_settings = video_section.get_settings()
            audio_source = audio_section.get_audio_source()
            
            # Validate device selection
            is_valid, error_msg = DeviceValidator.validate_device_selection(
                device_id, self.device_manager.get_device_ids()
            )
            if not is_valid:
                messagebox.showerror("Error", error_msg)
                return
            
            # Validate video settings
            if not self._validate_video_settings(video_settings):
                return
            
            # Store the last connected device for auto-reconnect
            self.last_connected_device = device_id
            self.config_manager.set('last_selected_device', device_id)
            
            # Get audio source safely
            try:
                audio_source_internal = self.config_manager.get_audio_source_internal()
            except Exception as e:
                print(f"Error getting audio source, using default: {e}")
                audio_source_internal = "playback"
            
            # Create scrcpy configuration
            config = ScrcpyConfig(
                device_id=device_id,
                bitrate=f"{video_settings['bitrate']}M",
                framerate=video_settings['framerate'],
                fullscreen=video_settings['fullscreen_enabled'],
                audio_source=audio_source_internal
            )
            
            self.process_manager.start_process(config)
        except Exception as e:
            messagebox.showerror("Error", f"Failed to start scrcpy: {str(e)}")
    
    def _validate_video_settings(self, video_settings: dict) -> bool:
        """Validate video settings before starting scrcpy"""
        try:
            # Validate bitrate
            bitrate = float(video_settings.get('bitrate', 0))
            if bitrate <= 0 or bitrate > 1000:
                messagebox.showerror("Error", "Bitrate must be between 1 and 1000 Mbps")
                return False
            
            # Validate framerate
            framerate = int(video_settings.get('framerate', 0))
            if framerate <= 0 or framerate > 240:
                messagebox.showerror("Error", "Framerate must be between 1 and 240 FPS")
                return False
            
            return True
        except (ValueError, TypeError) as e:
            messagebox.showerror("Error", f"Invalid video settings: {str(e)}")
            return False
    
    def stop_scrcpy(self):
        """Stop the running scrcpy process"""
        if self.is_running:
            print("User manually stopped screen mirroring. Disabling auto-reconnect.")
            # Clear last connected device to prevent auto-reconnect
            self.last_connected_device = None
            self.process_manager.stop_process()
    
    def load_config(self):
        """Load configuration from config manager"""
        self.config_manager.load_config()
        
        # Load settings into UI sections
        self.main_layout.get_video_section().load_from_config()
        self.main_layout.get_audio_section().load_from_config()
        
        # Configure auto-reconnect
        auto_reconnect = self.config_manager.get('auto_reconnect_enabled', False)
        self.process_manager.enable_auto_reconnect(auto_reconnect, max_attempts=0)  # Unlimited attempts
        
        # Try to restore last selected device if available
        last_device = self.config_manager.get('last_selected_device', '')
        if last_device:
            self.last_connected_device = last_device
            device_section = self.main_layout.get_device_section()
            # Check if device is currently available
            if last_device in self.device_manager.get_device_ids():
                device_section.set_selected_device(last_device)
        
        print("Configuration loaded successfully")
    
    # ProcessEventListener implementation
    def on_process_started(self, config):
        """Called when process starts"""
        self.is_running = True
        control_section = self.main_layout.get_control_section()
        control_section.set_running_state(True)
        print(f"Mirroring Active • Device: {config.device_id} • {config.bitrate} • {config.framerate}FPS")
    
    def on_process_stopped(self, exit_code):
        """Called when process stops"""
        try:
            # Only reset state if auto-reconnect is disabled or manually stopped
            if not self.config_manager.get('auto_reconnect_enabled', False) or not self.last_connected_device:
                self.is_running = False
                control_section = self.main_layout.get_control_section()
                control_section.set_running_state(False)
                
                device_section = self.main_layout.get_device_section()
                device_section.clear_reconnect_status()
                
                print("Ready to start mirroring")
            else:
                # Keep running state during auto-reconnect
                print("Process stopped - auto-reconnect will attempt to restart")
        except tk.TclError:
            # UI might be destroyed during shutdown
            pass
        except Exception as e:
            print(f"Error handling process stop: {e}")
    
    def on_process_error(self, error):
        """Called when process error occurs"""
        try:
            messagebox.showerror("Error", f"Process error: {str(error)}")
        except tk.TclError:
            # UI might be destroyed, log instead
            print(f"Process error (UI unavailable): {str(error)}")
        except Exception as e:
            print(f"Error handling process error: {e}")
    
    def on_reconnect_attempt(self, attempt):
        """Called during reconnect attempts"""
        try:
            device_section = self.main_layout.get_device_section()
            device_section.update_reconnect_status(
                f"Auto-reconnecting... (attempt {attempt})", 
                "warning"
            )
        except tk.TclError:
            # UI might be destroyed during shutdown
            pass
        except Exception as e:
            print(f"Error updating reconnect status: {e}")
    
    def on_reconnect_success(self):
        """Called on successful reconnection"""
        try:
            device_section = self.main_layout.get_device_section()
            device_section.update_reconnect_status("Device reconnected successfully!", "success")
            
            # Ensure UI is in running state
            self.is_running = True
            control_section = self.main_layout.get_control_section()
            control_section.set_running_state(True)
            
            # Clear status after 3 seconds with error handling
            def clear_status():
                try:
                    device_section.clear_reconnect_status()
                except tk.TclError:
                    pass  # UI might be destroyed
                except Exception as e:
                    print(f"Error clearing reconnect status: {e}")
            
            self.root.after(3000, clear_status)
            
            print(f"Auto-reconnect successful for device: {self.last_connected_device}")
        except tk.TclError:
            # UI might be destroyed during shutdown
            pass
        except Exception as e:
            print(f"Error in reconnect success handler: {e}")
    
    # DeviceConnectionListener implementation
    def on_devices_changed(self, devices):
        """Called when device list changes"""
        # Check if last connected device was disconnected during mirroring
        if (self.is_running and self.last_connected_device and 
            self.last_connected_device not in devices):
            print(f"Device {self.last_connected_device} disconnected during mirroring")
            # Auto-reconnect will be handled by ProcessManager
    
    def on_device_connected(self, device_id):
        """Called when device connects"""
        # If this is the device we were waiting for, auto-reconnect should handle it
        if (self.last_connected_device == device_id and 
            not self.is_running and 
            self.config_manager.get('auto_reconnect_enabled', False)):
            print(f"Previously connected device {device_id} reconnected")
    
    def on_device_disconnected(self, device_id):
        """Called when device disconnects"""
        if device_id == self.last_connected_device and self.is_running:
            print(f"Currently used device {device_id} disconnected")


def main():
    """Main application entry point"""
    root = ctk.CTk()
    app = ScrcpyGUI(root)
    
    # Handle window close
    def on_closing():
        if app.is_running:
            app.stop_scrcpy()
        # Stop device monitoring
        app.device_manager.stop_monitoring()
        # Auto-save config on exit
        app.config_manager.save_config(silent=True)
        root.destroy()
    
    root.protocol("WM_DELETE_WINDOW", on_closing)
    root.mainloop()


if __name__ == "__main__":
    main()