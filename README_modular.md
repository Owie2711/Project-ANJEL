# Scrcpy Controller - Modular Architecture

## Recent Fixes

### CtkComboBox Device Selection Fix
Fixed the device selection dropdown (CtkComboBox) in the modular version:
- Added proper variable binding for device selection tracking
- Implemented device selection change callbacks
- Added device selection persistence to configuration
- Enhanced dropdown click area for better usability
- Fixed configuration loading for entry fields with proper type checking

## Overview

The Scrcpy Controller application has been refactored into a modular architecture that makes it easy to add new features and redesign the UI. The application is now organized into separate, reusable components.

## Architecture

### Core Modules

#### 1. `ui_components.py` - Reusable UI Components
- **UITheme**: Centralized theme configuration (colors, fonts, spacing)
- **BaseCard**: Foundation for card-based UI sections
- **StyledButton**: Consistently styled buttons
- **StyledEntry**: Themed input fields
- **StyledComboBox**: Themed dropdown menus
- **StyledCheckBox**: Themed checkboxes
- **StatusLabel**: Color-coded status messages
- **LabeledInput**: Helper for labeled input fields
- **HeaderSection**: Application header with icon and title

#### 2. `device_manager.py` - Device Management
- **DeviceInfo**: Data class for device information
- **ADBManager**: Low-level ADB operations
- **DeviceManager**: High-level device management with auto-monitoring
- **DeviceValidator**: Device operation validation
- **DeviceConnectionListener**: Interface for device events

#### 3. `process_manager.py` - Process Management
- **ScrcpyConfig**: Configuration for scrcpy processes
- **ScrcpyProcess**: Individual process management
- **AutoReconnectManager**: Auto-reconnection functionality
- **ProcessManager**: High-level process orchestration
- **ProcessEventListener**: Interface for process events
- **SystemProcessChecker**: System-wide process detection

#### 4. `config_manager.py` - Configuration Management
- **AppConfig**: Application configuration data class
- **ConfigValidator**: Validation framework
- **ConfigManager**: Settings persistence and validation
- **AutoSaveConfigManager**: Automatic configuration saving

#### 5. `ui_layouts.py` - UI Layout Components
- **DeviceSelectionSection**: Device selection interface
- **VideoSettingsSection**: Video configuration controls
- **AudioSettingsSection**: Audio configuration controls
- **ControlSection**: Start/Stop controls
- **MainLayout**: Complete application layout

#### 6. `GUI_modular.py` - Main Application
- **ScrcpyGUI**: Main application class implementing event listeners
- Orchestrates all modules and handles application lifecycle

## Key Benefits

### 1. **Easy Feature Addition**
- Add new UI sections by creating components in `ui_layouts.py`
- Extend functionality by implementing new managers
- Add new configuration options in `AppConfig`

### 2. **Simple UI Redesign**
- Modify `UITheme` for global appearance changes
- Create new UI components in `ui_components.py`
- Reorganize layouts in `ui_layouts.py`

### 3. **Better Maintainability**
- Clear separation of concerns
- Well-defined interfaces between modules
- Centralized configuration management

### 4. **Enhanced Testability**
- Each module can be tested independently
- Mock interfaces for unit testing
- Validators ensure data integrity

## Adding New Features

### Example: Adding a New Setting

1. **Update Configuration**:
   ```python
   # In config_manager.py - AppConfig class
   new_setting: bool = False
   ```

2. **Add Validator** (if needed):
   ```python
   # In ConfigManager._setup_default_validators()
   self.validators['new_setting'] = ChoiceValidator([True, False])
   ```

3. **Create UI Component**:
   ```python
   # In ui_layouts.py - VideoSettingsSection
   self.new_setting_check = StyledCheckBox(container, "New Setting")
   ```

4. **Connect Events**:
   ```python
   # In the section's _setup_bindings method
   self.new_setting_check.configure(command=lambda: self._on_setting_changed('new_setting', self.new_setting_check.get()))
   ```

### Example: Adding a New UI Section

1. **Create Section Class**:
   ```python
   # In ui_layouts.py
   class NewSection(BaseCard):
       def __init__(self, parent, config_manager):
           super().__init__(parent, title="New Feature")
           self.config_manager = config_manager
           self._setup_ui()
   ```

2. **Add to Main Layout**:
   ```python
   # In MainLayout._setup_layout()
   self.new_section = NewSection(content_frame, self.config_manager)
   self.new_section.grid(row=4, column=0, sticky="ew", pady=(0, UITheme.PADDING_MEDIUM))
   ```

## UI Redesign Guide

### Theme Customization
Modify `UITheme` in `ui_components.py`:
```python
class UITheme:
    # Colors
    BACKGROUND = "#your_color"
    TEXT_PRIMARY = "#your_color"
    
    # Spacing
    PADDING_LARGE = 20  # Increase for more spacious design
    CORNER_RADIUS = 20  # Change for different aesthetics
```

### Layout Changes
- Modify `MainLayout` in `ui_layouts.py` for overall structure
- Create new section classes for different UI patterns
- Use `BaseCard` as foundation for consistent styling

### Component Styling
- Extend styled components in `ui_components.py`
- Create new component types for specific needs
- Use theme constants for consistency

## Migration from Original GUI

The original `GUI.py` file has been preserved. The new modular version is in `GUI_modular.py`. Key differences:

- **Separation**: Logic is split across specialized modules
- **Reusability**: Components can be reused and extended
- **Maintainability**: Each module has a single responsibility
- **Testability**: Clear interfaces enable better testing

## File Structure

```
Project-ANJEL - dev/
├── GUI.py                 # Original monolithic version
├── GUI_modular.py         # New modular main application
├── ui_components.py       # Reusable UI components
├── ui_layouts.py          # UI layout sections
├── device_manager.py      # Device management
├── process_manager.py     # Process management
├── config_manager.py      # Configuration management
├── config.json           # Configuration file
└── README_modular.md      # This documentation
```

## Future Enhancements

The modular architecture enables easy addition of:
- New device connection methods (WiFi, USB-C, etc.)
- Additional video/audio codecs
- Plugin system for custom features
- Different UI themes and layouts
- Advanced process monitoring
- Logging and debugging tools
- Multi-device support
- Configuration profiles

## Development Guidelines

1. **Follow the Module Pattern**: Each module should have a single responsibility
2. **Use Interfaces**: Implement listener interfaces for loose coupling
3. **Centralize Configuration**: Add new settings to `AppConfig`
4. **Maintain Theme Consistency**: Use `UITheme` constants
5. **Test Independently**: Each module should be testable in isolation
6. **Document Changes**: Update this README when adding new modules