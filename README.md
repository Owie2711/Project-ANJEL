# 🎥 Project-ANJEL

**⚡ Custom SCRCPY configuration for high-performance Android screen recording**  

---

## ✨ Main Features

- 🖥️ Runs **SCRCPY** with optimal configuration:
  - 🎮 Render via **OpenGL & DirectX**
  - 📶 **High Bitrate Support**
  - ⚡ **High FPS Support**
  - 🖼️ Fullscreen mode
- 🔊 **Audio forwarding** (available on Android 11+):
  - 📱 **Android 12+**: enabled automatically  
  - 🔓 **Android 11**: requires device screen unlocked when starting SCRCPY  
  - ⬇️ **Android 10 and below**: not supported (falls back to video only)  

---

## 🚀 How to Install & Use

1. 📥 **Download** the latest release (see the **[Releases](../../releases)** tab).  
2. ⚙️ Enable **USB Debugging** on your Android device.  
   👉 (Settings > About Phone > tap **Build Number** 7x > enable **Developer Options**)  
3. 💽 **Install USB drivers** (if not already):  
   – Windows: follow the guide at [developer.android.com](https://developer.android.com/studio/run/oem-usb)  
4. 🔌 **Connect** your Android device to your PC via USB.  
5. ▶️ Run **`ScrcpyController.exe`**.  

---

## 🛠️ Usage Tips

- 🐢 If you experience **lag or delay**, try lowering `Max FPS` or `Video Bitrate`.  
- 🎧 If audio fails, video recording will still work. 
