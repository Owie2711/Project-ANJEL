import tkinter as tk
from tkinter import ttk, messagebox
import customtkinter as ctk
import subprocess
import threading
import re
import json
import os

# Optional import for process checking
try:
    import psutil  # type: ignore
    PSUTIL_AVAILABLE = True
except ImportError:
    psutil = None  # type: ignore
    PSUTIL_AVAILABLE = False

class ScrcpyGUI:
    def __init__(self, root):
        self.root = root
        self.root.title("Scrcpy Controller")
        self.root.geometry("400x600")
        self.root.resizable(True, True)
        self.root.minsize(400, 600)
        
        # Configure CustomTkinter theme
        ctk.set_appearance_mode("light")  # "light" or "dark"
        ctk.set_default_color_theme("blue")  # "blue", "green", "dark-blue"
        
        # Set window background
        self.root.configure(bg='#f8fafc')
        
        # Variables
        self.selected_device = tk.StringVar()
        self.bitrate = tk.StringVar(value="20")
        self.framerate = tk.StringVar(value="60")
        self.fullscreen_enabled = tk.BooleanVar(value=False)
        self.auto_reconnect_enabled = tk.BooleanVar(value=False)
        self.audio_source = tk.StringVar(value="playback")
        self.is_running = False
        self.scrcpy_process = None
        self.last_connected_device = None
        self.reconnect_attempts = 0
        # Removed max_reconnect_attempts - will try indefinitely
        
        self.setup_ui()
        self.refresh_devices()
        
        # Start automatic device refresh
        self.start_auto_refresh()
        
        # Load configuration after UI is fully initialized
        self.root.after(100, self.load_config)
    
    def setup_ui(self):
        # Configure root grid
        self.root.grid_rowconfigure(0, weight=1)
        self.root.grid_columnconfigure(0, weight=1)
        
        # Main container with clean styling
        main_container = ctk.CTkFrame(self.root, corner_radius=15, fg_color="#f8f9fa", border_width=2, border_color="#000000")
        main_container.grid(row=0, column=0, sticky="nsew", padx=5, pady=5)
        main_container.grid_rowconfigure(1, weight=1)
        main_container.grid_columnconfigure(0, weight=1)
        
        # Title header with game controller icon
        header_frame = ctk.CTkFrame(main_container, height=45, corner_radius=0, fg_color="transparent")
        header_frame.grid(row=0, column=0, sticky="ew", pady=(8, 0), padx=10)
        header_frame.grid_propagate(False)
        header_frame.grid_columnconfigure(1, weight=1)
        
        # Game controller icon
        controller_icon = ctk.CTkLabel(header_frame, text="🎮", 
                                      font=ctk.CTkFont(family="Segoe UI", size=20))
        controller_icon.grid(row=0, column=0, sticky="w", padx=(0, 10))
        
        # Title
        title_label = ctk.CTkLabel(header_frame, text="Scrcpy Controller", 
                                  font=ctk.CTkFont(family="Segoe UI", size=16, weight="bold"),
                                  text_color="#000000")
        title_label.grid(row=0, column=1, sticky="w")
        
        # Main content frame
        content_frame = ctk.CTkFrame(main_container, corner_radius=0, fg_color="transparent")
        content_frame.grid(row=1, column=0, sticky="nsew", padx=10, pady=(8, 8))
        content_frame.grid_columnconfigure(0, weight=1)
        
        # Device selection card
        device_frame = ctk.CTkFrame(content_frame, corner_radius=15, border_width=2, border_color="#000000", fg_color="#f8f9fa")
        device_frame.grid(row=0, column=0, sticky="ew", pady=(0, 8))
        device_frame.grid_columnconfigure(0, weight=1)
        
        device_title = ctk.CTkLabel(device_frame, text="Device Selection", 
                                   font=ctk.CTkFont(family="Segoe UI", size=14, weight="bold"),
                                   text_color="#000000")
        device_title.grid(row=0, column=0, sticky="w", pady=(8, 3), padx=12)
        
        # Device selection container
        device_container = ctk.CTkFrame(device_frame, fg_color="transparent")
        device_container.grid(row=1, column=0, sticky="ew", pady=(0, 6), padx=12)
        device_container.grid_columnconfigure(0, weight=1)
        
        # Connected devices label and dropdown in same row
        device_row = ctk.CTkFrame(device_container, fg_color="transparent")
        device_row.grid(row=0, column=0, sticky="ew")
        device_row.grid_columnconfigure(0, weight=1)
        
        self.device_combo = ctk.CTkComboBox(device_row, variable=self.selected_device,
                                           state="readonly", width=400, height=30,
                                           font=ctk.CTkFont(family="Segoe UI", size=11),
                                           border_width=2, border_color="#000000",
                                           fg_color="#ffffff", text_color="#000000",
                                           dropdown_fg_color="#ffffff",
                                           justify="left")
        self.device_combo.grid(row=0, column=0, sticky="ew", padx=(0, 8))
        self.device_combo.set("Connected Devices:")
        
        # Make the entire dropdown area clickable
        def on_dropdown_click(event):
            self.device_combo._open_dropdown_menu()
        
        # Bind click events to make entire area clickable
        self.device_combo.bind("<Button-1>", on_dropdown_click)
        self.device_combo._entry.bind("<Button-1>", on_dropdown_click)
        
        refresh_btn = ctk.CTkButton(device_row, text="Refresh", 
                                   command=self.refresh_devices, width=70, height=30,
                                   font=ctk.CTkFont(family="Segoe UI", size=11, weight="bold"),
                                   fg_color="#ffffff", text_color="#000000",
                                   border_width=2, border_color="#000000",
                                   hover_color="#f0f0f0")
        refresh_btn.grid(row=0, column=1)
        
        # Device status display
        self.device_status_label = ctk.CTkLabel(device_container, text="No devices scanned yet", 
                                               font=ctk.CTkFont(family="Segoe UI", size=10),
                                               text_color="#666666")
        self.device_status_label.grid(row=1, column=0, sticky="w", pady=(2, 0))
        
        # Reconnection status display
        self.reconnect_status_label = ctk.CTkLabel(device_container, text="", 
                                                  font=ctk.CTkFont(family="Segoe UI", size=9),
                                                  text_color="#d97706")
        self.reconnect_status_label.grid(row=2, column=0, sticky="w", pady=(0, 0))
        
        # Video settings card
        video_frame = ctk.CTkFrame(content_frame, corner_radius=15, border_width=2, border_color="#000000", fg_color="#f8f9fa")
        video_frame.grid(row=1, column=0, sticky="ew", pady=(0, 8))
        video_frame.grid_columnconfigure(0, weight=1)
        video_frame.grid_columnconfigure(1, weight=1)
        
        video_title = ctk.CTkLabel(video_frame, text="Video Settings", 
                                  font=ctk.CTkFont(family="Segoe UI", size=14, weight="bold"),
                                  text_color="#000000")
        video_title.grid(row=0, column=0, columnspan=3, sticky="w", pady=(8, 5), padx=12)
        
        # Bitrate row
        bitrate_label = ctk.CTkLabel(video_frame, text="Bitrate Mbps)",
                                    font=ctk.CTkFont(family="Segoe UI", size=11),
                                    text_color="#000000")
        bitrate_label.grid(row=1, column=0, sticky="", pady=(0, 5), padx=12)
        
        self.bitrate_entry = ctk.CTkEntry(video_frame, textvariable=self.bitrate, 
                                         width=130, height=30,
                                         font=ctk.CTkFont(family="Segoe UI", size=11),
                                         border_width=2, border_color="#000000",
                                         fg_color="#ffffff", text_color="#000000")
        self.bitrate_entry.grid(row=2, column=0, sticky="", pady=(0, 8), padx=12)
        
        # Max FPS row
        fps_label = ctk.CTkLabel(video_frame, text="Max Framerate (FPS)",
                                font=ctk.CTkFont(family="Segoe UI", size=11),
                                text_color="#000000")
        fps_label.grid(row=1, column=1, sticky="", pady=(0, 5), padx=12)
        
        self.framerate_entry = ctk.CTkEntry(video_frame, textvariable=self.framerate, 
                                           width=130, height=30,
                                           font=ctk.CTkFont(family="Segoe UI", size=11),
                                           border_width=2, border_color="#000000",
                                           fg_color="#ffffff", text_color="#000000")
        self.framerate_entry.grid(row=2, column=1, sticky="", pady=(0, 8), padx=12)
        
        # Fullscreen and Auto-reconnect checkboxes row
        checkbox_container = ctk.CTkFrame(video_frame, fg_color="transparent")
        checkbox_container.grid(row=3, column=0, columnspan=2, sticky="ew", pady=(0, 8), padx=12)
        checkbox_container.grid_columnconfigure(0, weight=1)
        checkbox_container.grid_columnconfigure(1, weight=1)
        
        # Fullscreen checkbox
        self.fullscreen_check = ctk.CTkCheckBox(checkbox_container, text="Enable Fullscreen Mode", 
                                               variable=self.fullscreen_enabled,
                                               font=ctk.CTkFont(family="Segoe UI", size=11),
                                               text_color="#000000",
                                               border_width=2, border_color="#000000",
                                               fg_color="#ffffff", hover_color="#f0f0f0",
                                               checkmark_color="#000000")
        self.fullscreen_check.grid(row=0, column=0, sticky="")
        
        # Auto-reconnect checkbox
        self.auto_reconnect_check = ctk.CTkCheckBox(checkbox_container, text="Auto Reconnect", 
                                                   variable=self.auto_reconnect_enabled,
                                                   font=ctk.CTkFont(family="Segoe UI", size=11),
                                                   text_color="#000000",
                                                   border_width=2, border_color="#000000",
                                                   fg_color="#ffffff", hover_color="#f0f0f0",
                                                   checkmark_color="#000000")
        self.auto_reconnect_check.grid(row=0, column=1, sticky="", padx=(8, 0))
        
        # Audio settings card
        audio_frame = ctk.CTkFrame(content_frame, corner_radius=15, border_width=2, border_color="#000000", fg_color="#f8f9fa")
        audio_frame.grid(row=2, column=0, sticky="ew", pady=(0, 8))
        audio_frame.grid_columnconfigure(1, weight=1)
        
        audio_title = ctk.CTkLabel(audio_frame, text="Audio Settings", 
                                  font=ctk.CTkFont(family="Segoe UI", size=14, weight="bold"),
                                  text_color="#000000")
        audio_title.grid(row=0, column=0, columnspan=2, sticky="w", pady=(8, 5), padx=12)
        
        # Audio source selection
        audio_label = ctk.CTkLabel(audio_frame, text="Audio Source:",
                                  font=ctk.CTkFont(family="Segoe UI", size=11),
                                  text_color="#000000")
        audio_label.grid(row=1, column=0, sticky=tk.W, pady=(0, 5), padx=12)
        
        audio_options = [
            "No audio",
            "Microphone", 
            "Audio Playback"
        ]
        
        self.audio_combo = ctk.CTkComboBox(audio_frame, 
                                          values=audio_options, 
                                          state="readonly", width=400, height=30,
                                          font=ctk.CTkFont(family="Segoe UI", size=11),
                                          border_width=2, border_color="#000000",
                                          fg_color="#ffffff", text_color="#000000",
                                          dropdown_fg_color="#ffffff",
                                          justify="left")
        self.audio_combo.grid(row=2, column=0, columnspan=2, sticky="ew", pady=(0, 6), padx=12)
        self.audio_combo.set("Audio Playback")
        
        # Make the entire audio dropdown area clickable
        def on_audio_dropdown_click(event):
            self.audio_combo._open_dropdown_menu()
        
        # Bind click events to make entire area clickable
        self.audio_combo.bind("<Button-1>", on_audio_dropdown_click)
        self.audio_combo._entry.bind("<Button-1>", on_audio_dropdown_click)
        
        audio_hint = ctk.CTkLabel(audio_frame, text="Audio requires Android 11+", 
                                 font=ctk.CTkFont(family="Segoe UI", size=9),
                                 text_color="#666666")
        audio_hint.grid(row=3, column=0, columnspan=2, sticky=tk.W, pady=(0, 8), padx=12)
        
        # Start/Stop button - centered at bottom
        self.toggle_btn = ctk.CTkButton(content_frame, text="▶ Start Mirroring", 
                                       command=self.toggle_mirroring, width=220, height=50,
                                       font=ctk.CTkFont(family="Segoe UI", size=14, weight="bold"),
                                       fg_color="#ffffff", text_color="#000000",
                                       border_width=2, border_color="#000000",
                                       hover_color="#f0f0f0")
        self.toggle_btn.grid(row=3, column=0, pady=(12, 0))
        
        # Setup auto-save bindings after UI is fully initialized
        self.setup_auto_save_bindings()
    
    def check_existing_scrcpy_processes(self):
        """Check if there are any existing scrcpy processes running"""
        if PSUTIL_AVAILABLE and psutil is not None:
            try:
                for proc in psutil.process_iter(['pid', 'name']):
                    if proc.info['name'] and 'scrcpy' in proc.info['name'].lower():
                        print(f"Found existing scrcpy process: PID {proc.info['pid']}")
                        return True
                return False
            except Exception as e:
                print(f"Error checking for existing scrcpy processes with psutil: {e}")
                # Fall through to platform-specific commands
        
        # psutil not available or failed, use platform-specific commands as fallback
        try:
            if os.name == 'nt':  # Windows
                result = subprocess.run(['tasklist', '/FI', 'IMAGENAME eq scrcpy.exe'], 
                                      capture_output=True, text=True, timeout=5)
                return 'scrcpy.exe' in result.stdout
            else:  # Unix-like systems
                result = subprocess.run(['pgrep', 'scrcpy'], 
                                      capture_output=True, text=True, timeout=5)
                return result.returncode == 0
        except Exception as e:
            print(f"Could not check for existing scrcpy processes: {e}")
            return False
    
    def start_auto_refresh(self):
        """Start automatic device refresh every 3 seconds"""
        self.auto_refresh_devices()
    
    def auto_refresh_devices(self):
        """Automatically refresh devices and schedule next refresh"""
        if not self.is_running:  # Only refresh when not mirroring
            self.refresh_devices()
        else:
            # Check if device is still connected during mirroring
            self.check_device_connection()
        # Schedule next refresh in 3 seconds
        self.root.after(3000, self.auto_refresh_devices)
    
    def update_status(self, message, color="#000000", icon="✓"):
        """Update status - simplified since status section is removed"""
        pass
    
    def setup_auto_save_bindings(self):
        """Setup auto-save bindings for all configuration controls"""
        # Bind bitrate entry changes using direct reference
        self.bitrate_entry.bind('<KeyRelease>', lambda e: self.auto_save_config())
        self.bitrate_entry.bind('<FocusOut>', lambda e: self.auto_save_config())
        
        # Bind framerate entry changes using direct reference
        self.framerate_entry.bind('<KeyRelease>', lambda e: self.auto_save_config())
        self.framerate_entry.bind('<FocusOut>', lambda e: self.auto_save_config())
        
        # Bind fullscreen checkbox
        self.fullscreen_check.configure(command=self.auto_save_config)
        
        # Bind auto-reconnect checkbox
        self.auto_reconnect_check.configure(command=self.auto_save_config)
        
        # Bind audio combo changes - CustomTkinter uses command instead of bind
        self.audio_combo.configure(command=self.on_audio_combo_change)
    
    def refresh_devices(self):
        """Refresh the list of connected ADB devices"""
        try:
            self.device_status_label.configure(text="Scanning for devices...", text_color="#666666")
            self.root.update()
            
            # Run adb devices command
            result = subprocess.run(['adb', 'devices'], 
                                  capture_output=True, text=True, timeout=10)
            
            if result.returncode == 0:
                devices = []
                lines = result.stdout.strip().split('\n')
                
                for line in lines[1:]:  # Skip the first line "List of devices attached"
                    if line.strip() and '\tdevice' in line:
                        device_id = line.split('\t')[0]
                        devices.append(device_id)
                
                if devices:
                    self.device_combo.configure(values=devices)
                    if not self.selected_device.get() or self.selected_device.get() not in devices:
                        self.device_combo.set(devices[0])
                        self.selected_device.set(devices[0])
                    self.device_status_label.configure(text=f"Found {len(devices)} device(s)", text_color="#059669")
                else:
                    self.device_combo.configure(values=[])
                    self.device_combo.set("Connected Devices:")
                    self.selected_device.set("")
                    self.device_status_label.configure(text="No devices found", text_color="#dc2626")
            else:
                self.device_status_label.configure(text="ADB not found or error occurred", text_color="#dc2626")
                messagebox.showerror("Error", "ADB not found. Please make sure ADB is installed and in PATH.")
                
        except subprocess.TimeoutExpired:
            self.device_status_label.configure(text="Device scan timeout", text_color="#dc2626")
        except FileNotFoundError:
            self.device_status_label.configure(text="ADB not found", text_color="#dc2626")
            messagebox.showerror("Error", "ADB not found. Please install Android SDK platform-tools.")
        except Exception as e:
            self.device_status_label.configure(text=f"Error: {str(e)}", text_color="#dc2626")
    
    def check_device_connection(self):
        """Check if the current device is still connected during mirroring"""
        if not self.auto_reconnect_enabled.get() or not self.last_connected_device or not self.is_running:
            return
        
        # Only check if process is not running (which might indicate disconnection)
        if self.scrcpy_process and self.scrcpy_process.poll() is None:
            # Process is still running, device likely connected
            return
            
        try:
            # Run adb devices command to check current devices
            result = subprocess.run(['adb', 'devices'], 
                                  capture_output=True, text=True, timeout=5)
            
            if result.returncode == 0:
                devices = []
                lines = result.stdout.strip().split('\n')
                
                for line in lines[1:]:
                    if line.strip() and '\tdevice' in line:
                        device_id = line.split('\t')[0]
                        devices.append(device_id)
                
                # Check if last connected device is still available
                if self.last_connected_device not in devices:
                    print(f"Device {self.last_connected_device} disconnected. Auto-reconnect will continue indefinitely...")
                    # Only attempt reconnect if we're still supposed to be running
                    if self.is_running:
                        self.attempt_reconnect()
                else:
                    # Reset reconnect attempts if device is back
                    self.reconnect_attempts = 0
        except Exception as e:
            print(f"Error checking device connection: {str(e)}")
    
    def attempt_reconnect(self):
        """Attempt to reconnect to the last connected device indefinitely"""
        if not self.auto_reconnect_enabled.get():
            return
        
        self.reconnect_attempts += 1
        print(f"Reconnect attempt {self.reconnect_attempts} (unlimited attempts)")
        
        # Update status display
        self.reconnect_status_label.configure(
            text=f"Auto-reconnecting... (attempt {self.reconnect_attempts})",
            text_color="#d97706"
        )
        
        # Stop current mirroring process if it exists
        if self.scrcpy_process:
            try:
                self.scrcpy_process.terminate()
                self.scrcpy_process.wait(timeout=2)  # Wait for process to terminate
            except:
                try:
                    self.scrcpy_process.kill()  # Force kill if terminate doesn't work
                except:
                    pass
            self.scrcpy_process = None
        
        # Keep UI in "running" state during reconnection
        print(f"Waiting 3 seconds before reconnection attempt {self.reconnect_attempts}...")
        print("Auto-reconnect will continue indefinitely until manually stopped.")
        
        # Wait a moment and try to reconnect
        self.root.after(3000, self.try_restart_mirroring)
    
    def try_restart_mirroring(self):
        """Try to restart mirroring with the same settings"""
        # Refresh devices first
        self.refresh_devices()
        
        # Wait a moment for refresh to complete, then try to start
        self.root.after(1000, self.restart_mirroring_if_device_available)
    
    def restart_mirroring_if_device_available(self):
        """Restart mirroring if the device is available again"""
        if not self.auto_reconnect_enabled.get():
            return
            
        current_devices = []
        try:
            result = subprocess.run(['adb', 'devices'], 
                                  capture_output=True, text=True, timeout=5)
            if result.returncode == 0:
                lines = result.stdout.strip().split('\n')
                for line in lines[1:]:
                    if line.strip() and '\tdevice' in line:
                        device_id = line.split('\t')[0]
                        current_devices.append(device_id)
        except Exception as e:
            print(f"Error checking devices during reconnect: {str(e)}")
            # Schedule another attempt
            self.root.after(3000, self.try_restart_mirroring)
            return
        
        if self.last_connected_device and self.last_connected_device in current_devices:
            print(f"Device {self.last_connected_device} reconnected. Restarting mirroring...")
            
            # Update device selection to ensure it's properly set
            self.selected_device.set(self.last_connected_device)
            self.device_combo.set(self.last_connected_device)
            
            # Update device status
            self.device_status_label.configure(
                text=f"Device {self.last_connected_device} reconnected", 
                text_color="#059669"
            )
            
            self.reconnect_attempts = 0
            
            # Clear reconnection status
            self.reconnect_status_label.configure(text="Device reconnected successfully!", text_color="#059669")
            
            # Clear status after 3 seconds
            self.root.after(3000, lambda: self.reconnect_status_label.configure(text=""))
            
            # Start mirroring again with the same settings
            was_running = self.is_running
            try:
                # Ensure no existing process is running before starting new one
                if self.scrcpy_process:
                    try:
                        self.scrcpy_process.terminate()
                        self.scrcpy_process.wait(timeout=2)
                    except:
                        try:
                            self.scrcpy_process.kill()
                        except:
                            pass
                    self.scrcpy_process = None
                
                # Force is_running to False temporarily to allow start_scrcpy to proceed
                self.is_running = False
                
                # Start mirroring with stored device
                self.start_scrcpy()
                
                print(f"Successfully restarted mirroring for device: {self.last_connected_device}")
            except Exception as e:
                print(f"Error restarting mirroring: {str(e)}")
                # Restore running state and try again after a delay
                self.is_running = was_running
                self.root.after(3000, self.try_restart_mirroring)
        else:
            print(f"Device {self.last_connected_device} not yet available. Will retry indefinitely...")
            # Continue trying indefinitely since we removed max attempts limit
            self.root.after(3000, self.try_restart_mirroring)
    
    def toggle_mirroring(self):
        """Toggle between start and stop mirroring"""
        if self.is_running:
            self.stop_scrcpy()
        else:
            self.start_scrcpy()
    
    def validate_bitrate(self, bitrate):
        """Validate bitrate format - should be a positive number"""
        try:
            value = float(bitrate)
            return value > 0
        except ValueError:
            return False
    
    def validate_framerate(self, framerate):
        """Validate framerate format - should be a positive integer"""
        try:
            value = int(framerate)
            return value > 0
        except ValueError:
            return False
    
    def start_scrcpy(self):
        """Start scrcpy with selected device and settings"""
        if self.is_running:
            print("Scrcpy is already running. Skipping start request.")
            return
        
        # Ensure no existing process is running
        if self.scrcpy_process:
            print("Existing scrcpy process found. Terminating before starting new one.")
            try:
                self.scrcpy_process.terminate()
                self.scrcpy_process.wait(timeout=2)
            except:
                try:
                    self.scrcpy_process.kill()
                except:
                    pass
            self.scrcpy_process = None
        
        # Check for any existing scrcpy processes on the system
        if self.check_existing_scrcpy_processes():
            print("Warning: Other scrcpy processes detected on system. Proceeding with caution.")
            
        device = self.device_combo.get()
        bitrate = self.bitrate.get().strip()
        framerate = self.framerate.get().strip()
        
        if not device:
            messagebox.showerror("Error", "Please select a device first.")
            return
        
        if not bitrate:
            messagebox.showerror("Error", "Please enter a bitrate value.")
            return
        
        if not framerate:
            messagebox.showerror("Error", "Please enter a framerate value.")
            return
        
        # Validate bitrate input
        if not self.validate_bitrate(bitrate):
            messagebox.showerror("Error", "Please enter a valid positive number for bitrate.")
            return
        
        # Validate framerate input
        if not self.validate_framerate(framerate):
            messagebox.showerror("Error", "Please enter a valid positive integer for framerate.")
            return
        
        try:
            # Build scrcpy command - add 'M' suffix to bitrate
            bitrate_with_suffix = f"{bitrate}M"
            cmd = ['scrcpy', '-s', device, '-b', bitrate_with_suffix]
            
            # Add framerate setting
            cmd.extend(['--max-fps', framerate])
            
            # Add fullscreen if enabled
            if self.fullscreen_enabled.get():
                cmd.extend(['-f'])
            
            # Add audio settings
            audio_source = self.audio_combo.get()
            if audio_source == "Audio Playback":
                cmd.extend(['--audio-source=playback'])
            elif audio_source == "Microphone":
                cmd.extend(['--audio-source=mic-voice-communication'])
            elif audio_source == "No audio":
                cmd.extend(['--no-audio'])
            
            print("Starting scrcpy...")
            self.toggle_btn.configure(text="⏹ Stop Mirroring", 
                                     fg_color="#ff4444", 
                                     hover_color="#ff6666",
                                     text_color="#ffffff")
            
            # Store the connected device for auto-reconnect
            self.last_connected_device = device
            self.reconnect_attempts = 0
            
            # Show auto-reconnect status if enabled
            if self.auto_reconnect_enabled.get():
                print(f"Auto-reconnect enabled for device: {device} (unlimited attempts)")
            
            # Start scrcpy in a separate thread
            def run_scrcpy():
                try:
                    # Double-check that no process is already running
                    if self.scrcpy_process:
                        print("Warning: Existing process detected in thread. Terminating...")
                        try:
                            self.scrcpy_process.terminate()
                            self.scrcpy_process.wait(timeout=2)
                        except:
                            try:
                                self.scrcpy_process.kill()
                            except:
                                pass
                    
                    self.scrcpy_process = subprocess.Popen(cmd)
                    self.is_running = True
                    self.root.after(0, lambda: print(f"Mirroring Active • Device: {device} • {bitrate}M • {framerate}FPS"))
                    
                    # Wait for process to complete
                    self.scrcpy_process.wait()
                    
                except Exception as e:
                    if self.auto_reconnect_enabled.get() and self.last_connected_device:
                        print(f"Scrcpy process error: {str(e)}. Auto-reconnect enabled, attempting reconnection...")
                        # Don't reset UI state, keep is_running = True for auto-reconnect
                        self.scrcpy_process = None
                        self.root.after(0, self.attempt_reconnect)
                    else:
                        self.root.after(0, lambda: messagebox.showerror("Error", f"Failed to start scrcpy: {str(e)}"))
                        self.is_running = False
                        self.scrcpy_process = None
                        self.root.after(0, self.reset_ui)
                finally:
                    # Only reset if auto-reconnect is disabled or no device to reconnect to
                    if not self.auto_reconnect_enabled.get() or not self.last_connected_device:
                        self.is_running = False
                        self.scrcpy_process = None
                        self.root.after(0, self.reset_ui)
                    else:
                        # Process ended but auto-reconnect is enabled - try to reconnect
                        self.scrcpy_process = None
                        if self.is_running:  # Only attempt if we're still supposed to be running
                            print("Scrcpy process ended. Auto-reconnect enabled, attempting reconnection...")
                            self.root.after(0, self.attempt_reconnect)
            
            thread = threading.Thread(target=run_scrcpy, daemon=True)
            thread.start()
            
        except FileNotFoundError:
            messagebox.showerror("Error", "Scrcpy not found. Please install scrcpy and add it to PATH.")
            self.reset_ui()
        except Exception as e:
            messagebox.showerror("Error", f"Failed to start scrcpy: {str(e)}")
            self.reset_ui()
    
    def stop_scrcpy(self):
        """Stop the running scrcpy process and disable auto-reconnect"""
        print("User manually stopped screen mirroring. Disabling auto-reconnect.")
        
        # Stop auto-reconnect by clearing the device reference
        self.last_connected_device = None
        self.reconnect_attempts = 0
        
        if self.scrcpy_process and self.is_running:
            try:
                self.scrcpy_process.terminate()
                print("Stopping scrcpy...")
                # Wait for process to terminate
                try:
                    self.scrcpy_process.wait(timeout=3)
                except subprocess.TimeoutExpired:
                    self.scrcpy_process.kill()
            except Exception as e:
                print(f"Error stopping scrcpy: {str(e)}")
                try:
                    self.scrcpy_process.kill()
                except:
                    pass
        
        # Clear all status displays
        self.reconnect_status_label.configure(text="")
        self.reset_ui()
    
    def reset_ui(self):
        """Reset UI to initial state"""
        self.is_running = False
        self.scrcpy_process = None
        self.toggle_btn.configure(text="▶ Start Mirroring",
                                 fg_color="#ffffff",
                                 hover_color="#f0f0f0",
                                 text_color="#000000")
        print("Ready to start mirroring")

    def get_config_path(self):
        """Get the full path for config.json in the application directory"""
        app_dir = os.path.dirname(os.path.abspath(__file__))
        return os.path.join(app_dir, "config.json")

    def save_config(self):
        """Save current settings to config.json silently"""
        try:
            # Map display values back to technical values for saving
            audio_reverse_mapping = {
                "Audio Playback": "Audio Playback",
                "Microphone": "Microphone", 
                "No audio": "No audio"
            }
            
            audio_value = self.audio_combo.get()
            saved_audio_value = audio_reverse_mapping.get(audio_value, audio_value)
            
            config = {
                "bitrate": self.bitrate.get(),
                "framerate": self.framerate.get(),
                "fullscreen_enabled": self.fullscreen_enabled.get(),
                "auto_reconnect_enabled": self.auto_reconnect_enabled.get(),
                "audio_source": saved_audio_value
            }
            
            config_path = self.get_config_path()
            with open(config_path, "w") as f:
                json.dump(config, f, indent=4)
            
        except Exception as e:
            pass  # Silent operation for auto-save

    def auto_save_config(self, *args):
        """Automatically save config when settings change"""
        self.save_config()

    def on_audio_combo_change(self, choice):
        """Handle audio combo selection change"""
        self.save_config()

    def load_config(self):
        """Load settings from config.json"""
        try:
            config_path = self.get_config_path()
            
            if os.path.exists(config_path):
                with open(config_path, "r") as f:
                    config = json.load(f)
                
                # Apply loaded settings with error checking
                if "bitrate" in config:
                    self.bitrate.set(config["bitrate"])
                
                if "framerate" in config:
                    self.framerate.set(config["framerate"])
                
                if "fullscreen_enabled" in config:
                    self.fullscreen_enabled.set(config["fullscreen_enabled"])
                
                if "auto_reconnect_enabled" in config:
                    self.auto_reconnect_enabled.set(config["auto_reconnect_enabled"])
                
                if "audio_source" in config:
                    try:
                        # Map old config values to new display values
                        audio_mapping = {
                            "Audio Playback": "Audio Playback",
                            "Microphone": "Microphone",
                            "No audio": "No audio"
                        }
                        old_value = config["audio_source"]
                        new_value = audio_mapping.get(old_value, old_value) or "Audio Playback"
                        self.audio_combo.set(new_value)
                    except Exception:
                        self.audio_combo.set("Audio Playback")
                
                if hasattr(self, 'toggle_btn'):
                    print("Configuration loaded successfully")
            else:
                if hasattr(self, 'toggle_btn'):
                    print("No saved configuration found")
                
        except Exception as e:
            if hasattr(self, 'toggle_btn'):
                print("Error loading configuration")

def main():
    root = ctk.CTk()
    app = ScrcpyGUI(root)
    
    # Handle window close
    def on_closing():
        if app.is_running:
            app.stop_scrcpy()
        # Auto-save config on exit (silent)
        app.save_config()
        root.destroy()
    
    root.protocol("WM_DELETE_WINDOW", on_closing)
    root.mainloop()

if __name__ == "__main__":
    main()
