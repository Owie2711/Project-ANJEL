# Scrcpy Controller - Executable Distribution

## Overview
This is the compiled executable version of the Scrcpy Controller GUI application with support for portable local dependencies.

## Files Included
- `Scrcpy_Controller.exe` - Main application executable
- `Run_Scrcpy_Controller.bat` - Launcher batch file (optional)

## Portable Setup (Recommended)

For a fully portable installation, download and place scrcpy files in the same directory:

### Folder Structure
```
📁 Your_Folder/
├── 📄 Scrcpy_Controller.exe
├── 📁 scrcpy/
│   ├── 📄 scrcpy.exe
│   ├── 📄 scrcpy-server
│   ├── 📄 adb.exe
│   └── 📄 ... (other scrcpy files)
└── 📄 config.json (auto-generated)
```

### Download Required Files
1. **Scrcpy**: Download from https://github.com/Genymobile/scrcpy/releases
2. Extract scrcpy files to a `scrcpy/` subfolder
3. The application will automatically detect and use local files

### Search Priority
The application searches for executables in this order:
- **Scrcpy**: `./scrcpy/scrcpy.exe` → `./scrcpy.exe` → `./bin/scrcpy.exe` → System PATH
- **ADB**: `./platform-tools/adb.exe` → `./adb.exe` → `./bin/adb.exe` → `./scrcpy/adb.exe` → System PATH

## Quick Setup Guide

### Step 1: Download Scrcpy
1. Go to https://github.com/Genymobile/scrcpy/releases
2. Download the latest `scrcpy-win64-vX.X.X.zip`
3. Extract the contents

### Step 2: Organize Files
Create this folder structure:
```
📁 Your_Scrcpy_Folder/
├── 📄 Scrcpy_Controller.exe          ← This file
├── 📄 Setup_Dependencies.bat         ← Helper script
├── 📄 Run_Scrcpy_Controller.bat      ← Launcher
├── 📁 scrcpy/                        ← Create this folder
│   ├── 📄 scrcpy.exe                 ← From download
│   ├── 📄 scrcpy-server              ← From download
│   ├── 📄 adb.exe                    ← From download
│   └── 📄 ... (other scrcpy files)
└── 📄 config.json                    ← Auto-generated
```

### Step 3: Run
- Double-click `Setup_Dependencies.bat` to check your setup
- Double-click `Scrcpy_Controller.exe` to start the application

## Features
- ✅ Device detection and management
- ✅ Video quality settings (bitrate, framerate)
- ✅ Audio forwarding (Android 11+)
- ✅ Auto-reconnect functionality
- ✅ Fullscreen mode support
- ✅ Configuration auto-save
- ✅ Modern GUI interface

## Configuration
Settings are automatically saved to `config.json` in the same directory as the executable.

## Troubleshooting
- **"Another scrcpy session is already running"**: The application will ask if you want to close existing scrcpy processes. Click "Yes" to force start a new session.
- **No devices detected**: Check USB debugging is enabled and device is connected
- **ADB not found**: Install Android SDK platform-tools and add to PATH
- **Scrcpy not found**: Install Scrcpy and add to PATH
- **Audio issues**: Ensure Android 11+ and screen is unlocked during startup

## Version Information
- Built with PyInstaller
- Includes all necessary Python dependencies
- No additional Python installation required