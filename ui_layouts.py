"""
UI Layouts Module
Defines different UI sections and layouts for the Scrcpy Controller application.
"""

import tkinter as tk
import customtkinter as ctk
from typing import Callable, Optional, List, Dict, Any
from ui_components import (
    BaseCard, StyledButton, StyledEntry, StyledComboBox, 
    StyledCheckBox, StatusLabel, LabeledInput, HeaderSection, UITheme
)
from device_manager import DeviceManager, DeviceConnectionListener
from config_manager import ConfigManager


class DeviceSelectionSection(BaseCard, DeviceConnectionListener):
    """Device selection UI section"""
    
    def __init__(self, parent, device_manager: DeviceManager, config_manager: ConfigManager):
        super().__init__(parent, title="Device Selection")
        
        self.device_manager = device_manager
        self.config_manager = config_manager
        self.refresh_callback: Optional[Callable] = None
        self.device_selection_callback: Optional[Callable] = None
        
        # Create variable for device selection - start empty
        self.selected_device_var = tk.StringVar(value="")
        
        self._setup_ui()
        self._setup_bindings()
        
        # Register as device listener
        self.device_manager.add_listener(self)
    
    def _setup_ui(self):
        """Setup the device selection UI"""
        # Device selection container
        device_container = self.add_content_frame()
        device_container.grid_columnconfigure(0, weight=1)
        
        # Device selection row
        device_row = ctk.CTkFrame(device_container, fg_color="transparent")
        device_row.grid(row=0, column=0, sticky="ew")
        device_row.grid_columnconfigure(0, weight=1)
        
        # Device dropdown
        self.device_combo = StyledComboBox(
            device_row,
            variable=self.selected_device_var,
            state="readonly", 
            width=400, 
            height=30,
            values=[]  # Start with empty values
        )
        self.device_combo.grid(row=0, column=0, sticky="ew", padx=(0, 8))
        # Initially empty - will be populated when devices are found
        
        # Refresh button
        self.refresh_btn = StyledButton(
            device_row, 
            text="Refresh", 
            command=self._on_refresh_clicked,
            width=70, 
            height=30
        )
        self.refresh_btn.grid(row=0, column=1)
        
        # Device status
        self.device_status = StatusLabel(device_container, text="No devices connected")
        self.device_status.grid(row=1, column=0, sticky="w", pady=(2, 0))
        # Set initial status to error (red)
        self.device_status.update_status("No devices connected", "error")
        
        # Reconnection status
        self.reconnect_status = StatusLabel(
            device_container, 
            text="",
            font=ctk.CTkFont(family=UITheme.FONT_FAMILY, size=UITheme.FONT_SIZE_SMALL)
        )
        self.reconnect_status.grid(row=2, column=0, sticky="w", pady=(0, 0))
    
    def _setup_bindings(self):
        """Setup event bindings"""
        # Device selection change callback
        def on_device_selection_change(choice):
            """Handle device selection change"""
            self.selected_device_var.set(choice)
            # Notify that device selection changed
            if hasattr(self, 'device_selection_callback') and self.device_selection_callback:
                self.device_selection_callback(choice)
        
        # Configure the combobox command
        self.device_combo.configure(command=on_device_selection_change)
        
        # Make dropdown clickable
        def on_dropdown_click(event):
            try:
                if hasattr(self.device_combo, '_open_dropdown_menu'):
                    self.device_combo._open_dropdown_menu()
            except AttributeError:
                pass
        
        try:
            self.device_combo.bind("<Button-1>", on_dropdown_click)
            if hasattr(self.device_combo, '_entry'):
                self.device_combo._entry.bind("<Button-1>", on_dropdown_click)
        except Exception as e:
            print(f"Warning: Could not bind dropdown click events: {e}")
    
    def _on_refresh_clicked(self):
        """Handle refresh button click"""
        if self.refresh_callback:
            self.refresh_callback()
    
    def set_refresh_callback(self, callback: Callable):
        """Set the refresh callback function"""
        self.refresh_callback = callback
    
    def set_device_selection_callback(self, callback: Callable):
        """Set the device selection change callback"""
        self.device_selection_callback = callback
    
    def get_selected_device(self) -> str:
        """Get the currently selected device"""
        selected = self.selected_device_var.get()
        # Return empty string if no device is selected
        return selected.strip() if selected else ""
    
    def set_selected_device(self, device_id: str):
        """Set the selected device"""
        self.selected_device_var.set(device_id)
        self.device_combo.set(device_id)
    
    def update_device_status(self, message: str, status_type: str = "info"):
        """Update device status message"""
        self.device_status.update_status(message, status_type)
    
    def update_reconnect_status(self, message: str, status_type: str = "warning"):
        """Update reconnection status message"""
        self.reconnect_status.update_status(message, status_type)
    
    def clear_reconnect_status(self):
        """Clear reconnection status"""
        self.reconnect_status.configure(text="")
    
    # DeviceConnectionListener implementation
    def on_devices_changed(self, devices: List[str]):
        """Called when device list changes"""
        current_selection = self.selected_device_var.get()
        
        if devices:
            # Only show actual devices in the dropdown, no placeholder
            self.device_combo.configure(values=devices)
            
            # If current selection is not in the new list, select first device
            if current_selection not in devices:
                self.selected_device_var.set(devices[0])
                self.device_combo.set(devices[0])
            self.update_device_status(f"Found {len(devices)} device(s)", "success")
        else:
            # When no devices, clear dropdown and show red error message
            self.device_combo.configure(values=[])
            self.selected_device_var.set("")
            self.device_combo.set("")  # Empty dropdown
            self.update_device_status("No devices connected", "error")
    
    def on_device_connected(self, device_id: str):
        """Called when a device connects"""
        pass  # Handled by on_devices_changed
    
    def on_device_disconnected(self, device_id: str):
        """Called when a device disconnects"""
        pass  # Handled by on_devices_changed


class VideoSettingsSection(BaseCard):
    """Video settings UI section"""
    
    def __init__(self, parent, config_manager: ConfigManager):
        super().__init__(parent, title="Video Settings")
        
        self.config_manager = config_manager
        self.change_callbacks: List[Callable[[str, Any], None]] = []
        
        self._setup_ui()
        self._setup_bindings()
    
    def _setup_ui(self):
        """Setup the video settings UI"""
        content_frame = self.add_content_frame()
        content_frame.grid_columnconfigure(0, weight=1)
        content_frame.grid_columnconfigure(1, weight=1)
        
        # Bitrate input
        self.bitrate_input = LabeledInput(
            content_frame, 
            "Bitrate (Mbps)", 
            "entry",
            width=130, 
            height=30
        )
        self.bitrate_input.grid_vertical(0, 0, pady=(0, 8), padx=UITheme.PADDING_LARGE)
        
        # Framerate input
        self.framerate_input = LabeledInput(
            content_frame, 
            "Max Framerate (FPS)", 
            "entry",
            width=130, 
            height=30
        )
        self.framerate_input.grid_vertical(0, 1, pady=(0, 8), padx=UITheme.PADDING_LARGE)
        
        # Checkboxes container
        checkbox_container = ctk.CTkFrame(content_frame, fg_color="transparent")
        checkbox_container.grid(row=2, column=0, columnspan=2, sticky="ew", pady=(0, 8), padx=UITheme.PADDING_LARGE)
        checkbox_container.grid_columnconfigure(0, weight=1)
        checkbox_container.grid_columnconfigure(1, weight=1)
        
        # Fullscreen checkbox
        self.fullscreen_check = StyledCheckBox(checkbox_container, "Enable Fullscreen Mode")
        self.fullscreen_check.grid(row=0, column=0, sticky="")
        
        # Auto-reconnect checkbox
        self.auto_reconnect_check = StyledCheckBox(checkbox_container, "Auto Reconnect")
        self.auto_reconnect_check.grid(row=0, column=1, sticky="", padx=(8, 0))
    
    def _setup_bindings(self):
        """Setup event bindings for auto-save"""
        # Bitrate entry bindings
        self.bitrate_input.widget.bind('<KeyRelease>', lambda e: self._on_setting_changed('bitrate', self.bitrate_input.widget.get()))
        self.bitrate_input.widget.bind('<FocusOut>', lambda e: self._on_setting_changed('bitrate', self.bitrate_input.widget.get()))
        
        # Framerate entry bindings
        self.framerate_input.widget.bind('<KeyRelease>', lambda e: self._on_setting_changed('framerate', self.framerate_input.widget.get()))
        self.framerate_input.widget.bind('<FocusOut>', lambda e: self._on_setting_changed('framerate', self.framerate_input.widget.get()))
        
        # Checkbox bindings
        self.fullscreen_check.configure(command=lambda: self._on_setting_changed('fullscreen_enabled', self.fullscreen_check.get()))
        self.auto_reconnect_check.configure(command=lambda: self._on_setting_changed('auto_reconnect_enabled', self.auto_reconnect_check.get()))
    
    def _on_setting_changed(self, setting_name: str, value: Any):
        """Handle setting change"""
        # Convert framerate to int if needed
        if setting_name == 'framerate':
            try:
                value = int(value) if value else 60
            except ValueError:
                return  # Invalid input, don't save
        
        # Update config
        self.config_manager.set(setting_name, value)
        
        # Notify callbacks
        for callback in self.change_callbacks:
            try:
                callback(setting_name, value)
            except Exception as e:
                print(f"Error in setting change callback: {e}")
    
    def add_change_callback(self, callback: Callable[[str, Any], None]):
        """Add a callback for setting changes"""
        if callback not in self.change_callbacks:
            self.change_callbacks.append(callback)
    
    def load_from_config(self):
        """Load settings from config manager"""
        # Load bitrate
        if hasattr(self.bitrate_input.widget, 'delete') and hasattr(self.bitrate_input.widget, 'insert'):
            self.bitrate_input.widget.delete(0, tk.END)
            self.bitrate_input.widget.insert(0, self.config_manager.get('bitrate', '20'))
        
        # Load framerate  
        if hasattr(self.framerate_input.widget, 'delete') and hasattr(self.framerate_input.widget, 'insert'):
            self.framerate_input.widget.delete(0, tk.END)
            self.framerate_input.widget.insert(0, str(self.config_manager.get('framerate', 60)))
        
        # Load checkboxes
        if self.config_manager.get('fullscreen_enabled', False):
            self.fullscreen_check.select()
        else:
            self.fullscreen_check.deselect()
            
        if self.config_manager.get('auto_reconnect_enabled', False):
            self.auto_reconnect_check.select()
        else:
            self.auto_reconnect_check.deselect()
    
    def get_settings(self) -> Dict[str, Any]:
        """Get current settings"""
        try:
            framerate = int(self.framerate_input.widget.get())
        except ValueError:
            framerate = 60
        
        return {
            'bitrate': self.bitrate_input.widget.get(),
            'framerate': framerate,
            'fullscreen_enabled': self.fullscreen_check.get(),
            'auto_reconnect_enabled': self.auto_reconnect_check.get()
        }


class AudioSettingsSection(BaseCard):
    """Audio settings UI section"""
    
    def __init__(self, parent, config_manager: ConfigManager):
        super().__init__(parent, title="Audio Settings")
        
        self.config_manager = config_manager
        self.change_callbacks: List[Callable[[str, Any], None]] = []
        
        self._setup_ui()
        self._setup_bindings()
    
    def _setup_ui(self):
        """Setup the audio settings UI"""
        content_frame = self.add_content_frame()
        content_frame.grid_columnconfigure(1, weight=1)
        
        # Audio source label
        audio_label = ctk.CTkLabel(
            content_frame,
            text="Audio Source:",
            font=ctk.CTkFont(family=UITheme.FONT_FAMILY, size=UITheme.FONT_SIZE_NORMAL),
            text_color=UITheme.TEXT_PRIMARY
        )
        audio_label.grid(row=0, column=0, sticky=tk.W, pady=(0, 5), padx=UITheme.PADDING_LARGE)
        
        # Audio source dropdown
        audio_options = ["No audio", "Microphone", "Audio Playback"]
        self.audio_combo = StyledComboBox(
            content_frame,
            values=audio_options,
            state="readonly",
            width=400,
            height=30
        )
        self.audio_combo.grid(row=1, column=0, columnspan=2, sticky="ew", pady=(0, 6), padx=UITheme.PADDING_LARGE)
        self.audio_combo.set("Audio Playback")
        
        # Audio hint
        audio_hint = ctk.CTkLabel(
            content_frame,
            text="Audio requires Android 11+",
            font=ctk.CTkFont(family=UITheme.FONT_FAMILY, size=UITheme.FONT_SIZE_SMALL),
            text_color=UITheme.TEXT_SECONDARY
        )
        audio_hint.grid(row=2, column=0, columnspan=2, sticky=tk.W, pady=(0, 8), padx=UITheme.PADDING_LARGE)
    
    def _setup_bindings(self):
        """Setup event bindings"""
        # Audio combo binding
        self.audio_combo.configure(command=self._on_audio_changed)
        
        # Make dropdown clickable
        def on_dropdown_click(event):
            try:
                if hasattr(self.audio_combo, '_open_dropdown_menu'):
                    self.audio_combo._open_dropdown_menu()
            except AttributeError:
                pass
        
        try:
            self.audio_combo.bind("<Button-1>", on_dropdown_click)
            if hasattr(self.audio_combo, '_entry'):
                self.audio_combo._entry.bind("<Button-1>", on_dropdown_click)
        except Exception as e:
            print(f"Warning: Could not bind audio dropdown click events: {e}")
    
    def _on_audio_changed(self, choice):
        """Handle audio source change"""
        self.config_manager.set('audio_source', choice)
        
        # Notify callbacks
        for callback in self.change_callbacks:
            try:
                callback('audio_source', choice)
            except Exception as e:
                print(f"Error in audio change callback: {e}")
    
    def add_change_callback(self, callback: Callable[[str, Any], None]):
        """Add a callback for setting changes"""
        if callback not in self.change_callbacks:
            self.change_callbacks.append(callback)
    
    def load_from_config(self):
        """Load settings from config manager"""
        audio_source = self.config_manager.get('audio_source', 'Audio Playback')
        self.audio_combo.set(audio_source)
    
    def get_audio_source(self) -> str:
        """Get current audio source setting"""
        return self.audio_combo.get()


class ControlSection(ctk.CTkFrame):
    """Control buttons section"""
    
    def __init__(self, parent):
        super().__init__(parent, fg_color="transparent")
        
        self.start_callback: Optional[Callable] = None
        self.stop_callback: Optional[Callable] = None
        self.is_running = False
        
        self._setup_ui()
    
    def _setup_ui(self):
        """Setup the control UI"""
        # Main toggle button
        self.toggle_btn = StyledButton(
            self,
            text="▶ Start Mirroring",
            command=self._on_toggle_clicked,
            width=220,
            height=50,
            font=ctk.CTkFont(family=UITheme.FONT_FAMILY, size=UITheme.FONT_SIZE_LARGE, weight="bold")
        )
        self.toggle_btn.grid(row=0, column=0, pady=(UITheme.PADDING_LARGE, 0))
    
    def _on_toggle_clicked(self):
        """Handle toggle button click"""
        if self.is_running:
            if self.stop_callback:
                self.stop_callback()
        else:
            if self.start_callback:
                self.start_callback()
    
    def set_start_callback(self, callback: Callable):
        """Set the start callback"""
        self.start_callback = callback
    
    def set_stop_callback(self, callback: Callable):
        """Set the stop callback"""
        self.stop_callback = callback
    
    def set_running_state(self, is_running: bool):
        """Update the button state"""
        self.is_running = is_running
        
        if is_running:
            self.toggle_btn.configure(
                text="⏹ Stop Mirroring",
                fg_color=UITheme.ERROR_COLOR,
                hover_color="#ff6666",
                text_color="#ffffff"
            )
        else:
            self.toggle_btn.configure(
                text="▶ Start Mirroring",
                fg_color=UITheme.BUTTON_PRIMARY,
                hover_color=UITheme.BUTTON_HOVER,
                text_color=UITheme.TEXT_PRIMARY
            )


class MainLayout(ctk.CTkFrame):
    """Main application layout container"""
    
    def __init__(self, parent, device_manager: DeviceManager, config_manager: ConfigManager):
        super().__init__(
            parent,
            corner_radius=UITheme.CORNER_RADIUS,
            fg_color=UITheme.CARD_BACKGROUND,
            border_width=UITheme.BORDER_WIDTH,
            border_color=UITheme.BORDER_COLOR
        )
        
        self.device_manager = device_manager
        self.config_manager = config_manager
        
        # Configure grid
        self.grid_rowconfigure(1, weight=1)
        self.grid_columnconfigure(0, weight=1)
        
        self._setup_layout()
    
    def _setup_layout(self):
        """Setup the main layout"""
        # Header
        self.header = HeaderSection(
            self,
            title="Scrcpy Controller",
            icon="🎮"
        )
        self.header.grid(row=0, column=0, sticky="ew", pady=(UITheme.PADDING_MEDIUM, 0), padx=10)
        
        # Main content container
        content_frame = ctk.CTkFrame(self, corner_radius=0, fg_color="transparent")
        content_frame.grid(row=1, column=0, sticky="nsew", padx=10, pady=(UITheme.PADDING_MEDIUM, UITheme.PADDING_MEDIUM))
        content_frame.grid_columnconfigure(0, weight=1)
        
        # Device selection section
        self.device_section = DeviceSelectionSection(content_frame, self.device_manager, self.config_manager)
        self.device_section.grid(row=0, column=0, sticky="ew", pady=(0, UITheme.PADDING_MEDIUM))
        
        # Video settings section
        self.video_section = VideoSettingsSection(content_frame, self.config_manager)
        self.video_section.grid(row=1, column=0, sticky="ew", pady=(0, UITheme.PADDING_MEDIUM))
        
        # Audio settings section
        self.audio_section = AudioSettingsSection(content_frame, self.config_manager)
        self.audio_section.grid(row=2, column=0, sticky="ew", pady=(0, UITheme.PADDING_MEDIUM))
        
        # Control section
        self.control_section = ControlSection(content_frame)
        self.control_section.grid(row=3, column=0, pady=(UITheme.PADDING_LARGE, 0))
    
    def get_device_section(self) -> DeviceSelectionSection:
        """Get the device selection section"""
        return self.device_section
    
    def get_video_section(self) -> VideoSettingsSection:
        """Get the video settings section"""
        return self.video_section
    
    def get_audio_section(self) -> AudioSettingsSection:
        """Get the audio settings section"""
        return self.audio_section
    
    def get_control_section(self) -> ControlSection:
        """Get the control section"""
        return self.control_section