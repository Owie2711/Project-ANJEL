"""
Configuration Management Module
Handles application settings persistence, validation, and management.
"""

import json
import os
from typing import Any, Dict, Optional, Callable, List
from dataclasses import dataclass, asdict, field
from abc import ABC, abstractmethod


class ConfigValidator(ABC):
    """Abstract base class for configuration validators"""
    
    @abstractmethod
    def validate(self, value: Any) -> tuple[bool, str]:
        """Validate a configuration value. Returns (is_valid, error_message)"""
        pass


class RangeValidator(ConfigValidator):
    """Validates numeric values within a range"""
    
    def __init__(self, min_value: float, max_value: float, value_type: type = float):
        self.min_value = min_value
        self.max_value = max_value
        self.value_type = value_type
    
    def validate(self, value: Any) -> tuple[bool, str]:
        try:
            if isinstance(value, str):
                if not value.strip():
                    return False, f"Value cannot be empty"
                converted_value = self.value_type(value.strip())
            else:
                converted_value = self.value_type(value)
            
            if converted_value < self.min_value:
                return False, f"Value must be at least {self.min_value}"
            if converted_value > self.max_value:
                return False, f"Value must be at most {self.max_value}"
            
            return True, "Valid"
        except (ValueError, TypeError):
            return False, f"Value must be a valid {self.value_type.__name__}"


class ChoiceValidator(ConfigValidator):
    """Validates values against a list of allowed choices"""
    
    def __init__(self, choices: List[Any]):
        self.choices = choices
    
    def validate(self, value: Any) -> tuple[bool, str]:
        if value in self.choices:
            return True, "Valid"
        return False, f"Value must be one of: {', '.join(map(str, self.choices))}"


class BitrateValidator(ConfigValidator):
    """Special validator for bitrate values"""
    
    def validate(self, value: Any) -> tuple[bool, str]:
        try:
            if isinstance(value, str):
                if not value.strip():
                    return False, "Bitrate cannot be empty"
                # Remove 'M' suffix if present
                clean_value = value.strip().rstrip('Mm')
                bitrate_value = float(clean_value)
            else:
                bitrate_value = float(value)
            
            if bitrate_value <= 0:
                return False, "Bitrate must be greater than 0"
            if bitrate_value > 1000:
                return False, "Bitrate too high (max 1000 Mbps)"
            
            return True, "Valid"
        except (ValueError, TypeError):
            return False, "Bitrate must be a valid number"


@dataclass
class AppConfig:
    """Application configuration data class"""
    
    # Video settings
    bitrate: str = "20"
    framerate: int = 60
    fullscreen_enabled: bool = False
    
    # Audio settings
    audio_source: str = "Audio Playback"
    
    # Connection settings
    auto_reconnect_enabled: bool = False
    reconnect_max_attempts: int = 0  # 0 = unlimited
    reconnect_delay: float = 3.0
    
    # UI settings
    window_width: int = 400
    window_height: int = 600
    theme: str = "light"
    
    # Device settings
    last_selected_device: str = ""
    device_refresh_interval: float = 3.0
    
    # Advanced settings
    additional_scrcpy_args: List[str] = field(default_factory=list)
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for JSON serialization"""
        return asdict(self)
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'AppConfig':
        """Create instance from dictionary"""
        # Filter out unknown fields
        known_fields = {f.name for f in cls.__dataclass_fields__.values()}
        filtered_data = {k: v for k, v in data.items() if k in known_fields}
        return cls(**filtered_data)


class ConfigManager:
    """Manages application configuration with validation and persistence"""
    
    def __init__(self, config_filename: str = "config.json"):
        self.config_filename = config_filename
        self.config = AppConfig()
        self.validators: Dict[str, ConfigValidator] = {}
        self.change_listeners: List[Callable[[str, Any], None]] = []
        self._setup_default_validators()
    
    def _setup_default_validators(self):
        """Setup default validators for configuration fields"""
        self.validators = {
            'bitrate': BitrateValidator(),
            'framerate': RangeValidator(1, 240, int),
            'fullscreen_enabled': ChoiceValidator([True, False]),
            'audio_source': ChoiceValidator(["Audio Playback", "Microphone", "No audio"]),
            'auto_reconnect_enabled': ChoiceValidator([True, False]),
            'reconnect_max_attempts': RangeValidator(0, 100, int),
            'reconnect_delay': RangeValidator(0.1, 60.0, float),
            'window_width': RangeValidator(300, 2000, int),
            'window_height': RangeValidator(400, 1500, int),
            'theme': ChoiceValidator(["light", "dark"]),
            'device_refresh_interval': RangeValidator(0.5, 30.0, float)
        }
    
    def add_validator(self, field_name: str, validator: ConfigValidator):
        """Add a custom validator for a configuration field"""
        self.validators[field_name] = validator
    
    def add_change_listener(self, listener: Callable[[str, Any], None]):
        """Add a listener for configuration changes"""
        if listener not in self.change_listeners:
            self.change_listeners.append(listener)
    
    def remove_change_listener(self, listener: Callable[[str, Any], None]):
        """Remove a configuration change listener"""
        if listener in self.change_listeners:
            self.change_listeners.remove(listener)
    
    def get_config_path(self) -> str:
        """Get the full path to the configuration file"""
        app_dir = os.path.dirname(os.path.abspath(__file__))
        return os.path.join(app_dir, self.config_filename)
    
    def load_config(self) -> bool:
        """Load configuration from file"""
        config_path = self.get_config_path()
        
        try:
            if os.path.exists(config_path):
                with open(config_path, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                
                # Validate loaded data
                validated_data = {}
                for key, value in data.items():
                    if hasattr(self.config, key):
                        is_valid, error_msg = self.validate_field(key, value)
                        if is_valid:
                            validated_data[key] = value
                        else:
                            print(f"Invalid config value for {key}: {error_msg}, using default")
                
                # Create new config with validated data
                self.config = AppConfig.from_dict(validated_data)
                print("Configuration loaded successfully")
                return True
            else:
                print("No configuration file found, using defaults")
                return False
                
        except json.JSONDecodeError as e:
            print(f"Invalid JSON in config file: {e}, using defaults")
            return False
        except Exception as e:
            print(f"Error loading configuration: {e}, using defaults")
            return False
    
    def save_config(self, silent: bool = True) -> bool:
        """Save configuration to file"""
        config_path = self.get_config_path()
        
        try:
            with open(config_path, 'w', encoding='utf-8') as f:
                json.dump(self.config.to_dict(), f, indent=4)
            
            if not silent:
                print("Configuration saved successfully")
            return True
            
        except Exception as e:
            if not silent:
                print(f"Error saving configuration: {e}")
            return False
    
    def get(self, field_name: str, default: Any = None) -> Any:
        """Get a configuration value"""
        return getattr(self.config, field_name, default)
    
    def set(self, field_name: str, value: Any, validate: bool = True, notify: bool = True) -> bool:
        """Set a configuration value with optional validation"""
        if validate:
            is_valid, error_msg = self.validate_field(field_name, value)
            if not is_valid:
                print(f"Invalid value for {field_name}: {error_msg}")
                return False
        
        # Set the value
        if hasattr(self.config, field_name):
            old_value = getattr(self.config, field_name)
            setattr(self.config, field_name, value)
            
            # Notify listeners of change
            if notify and old_value != value:
                for listener in self.change_listeners:
                    try:
                        listener(field_name, value)
                    except Exception as e:
                        print(f"Error notifying config change listener: {e}")
            
            return True
        else:
            print(f"Unknown configuration field: {field_name}")
            return False
    
    def validate_field(self, field_name: str, value: Any) -> tuple[bool, str]:
        """Validate a single field value"""
        if field_name in self.validators:
            return self.validators[field_name].validate(value)
        return True, "No validator defined"
    
    def validate_all(self) -> Dict[str, str]:
        """Validate all configuration fields, return dict of field: error_message for invalid fields"""
        errors = {}
        
        for field_name in self.config.__dataclass_fields__.keys():
            value = getattr(self.config, field_name)
            is_valid, error_msg = self.validate_field(field_name, value)
            if not is_valid:
                errors[field_name] = error_msg
        
        return errors
    
    def reset_to_defaults(self):
        """Reset configuration to default values"""
        self.config = AppConfig()
        
        # Notify listeners of reset
        for listener in self.change_listeners:
            try:
                listener("__reset__", None)
            except Exception as e:
                print(f"Error notifying config reset listener: {e}")
    
    def export_config(self, filepath: str) -> bool:
        """Export configuration to a specific file"""
        try:
            with open(filepath, 'w', encoding='utf-8') as f:
                json.dump(self.config.to_dict(), f, indent=4)
            return True
        except Exception as e:
            print(f"Error exporting configuration: {e}")
            return False
    
    def import_config(self, filepath: str, validate: bool = True) -> bool:
        """Import configuration from a specific file"""
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                data = json.load(f)
            
            if validate:
                # Validate imported data
                validated_data = {}
                for key, value in data.items():
                    if hasattr(self.config, key):
                        is_valid, error_msg = self.validate_field(key, value)
                        if is_valid:
                            validated_data[key] = value
                        else:
                            print(f"Invalid imported value for {key}: {error_msg}")
                
                self.config = AppConfig.from_dict(validated_data)
            else:
                self.config = AppConfig.from_dict(data)
            
            return True
            
        except Exception as e:
            print(f"Error importing configuration: {e}")
            return False
    
    def get_audio_source_mapping(self) -> Dict[str, str]:
        """Get mapping between display names and internal values for audio sources"""
        return {
            "Audio Playback": "playback",
            "Microphone": "mic-voice-communication",
            "No audio": "none"
        }
    
    def get_audio_source_internal(self) -> str:
        """Get the internal audio source value for scrcpy"""
        mapping = self.get_audio_source_mapping()
        return mapping.get(self.config.audio_source, "playback")


class AutoSaveConfigManager(ConfigManager):
    """Configuration manager with automatic saving capabilities"""
    
    def __init__(self, config_filename: str = "config.json", auto_save_delay: float = 1.0):
        super().__init__(config_filename)
        self.auto_save_delay = auto_save_delay
        self._auto_save_timer: Optional[Any] = None
        self._pending_save = False
        
        # Add self as change listener for auto-save
        self.add_change_listener(self._on_config_change)
    
    def set(self, field_name: str, value: Any, validate: bool = True, notify: bool = True, auto_save: bool = True) -> bool:
        """Set configuration value with optional auto-save"""
        success = super().set(field_name, value, validate, notify)
        
        if success and auto_save:
            self._schedule_auto_save()
        
        return success
    
    def _on_config_change(self, field_name: str, value: Any):
        """Handle configuration changes for auto-save"""
        if field_name != "__reset__":
            self._schedule_auto_save()
    
    def _schedule_auto_save(self):
        """Schedule an auto-save operation"""
        self._pending_save = True
        
        # Cancel existing timer
        if self._auto_save_timer:
            try:
                self._auto_save_timer.cancel()
            except:
                pass
        
        # Schedule new save
        import threading
        self._auto_save_timer = threading.Timer(self.auto_save_delay, self._perform_auto_save)
        self._auto_save_timer.start()
    
    def _perform_auto_save(self):
        """Perform the actual auto-save operation"""
        if self._pending_save:
            self.save_config(silent=True)
            self._pending_save = False
            self._auto_save_timer = None