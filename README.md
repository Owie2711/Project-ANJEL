# 🎥 Project-ANJEL

**⚡ Konfigurasi custom SCRCPY untuk merekam layar Android dengan performa tinggi**  

---

## ✨ Fitur Utama

- 🖥️ Menggunakan **SCRCPY** dengan konfigurasi optimal:
  - 🎮 Render via **OpenGL & DirectX**
  - 📶 **Support High Bitrate**
  - ⚡ **Support High FPS**
  - 🖼️ Mode layar penuh (**fullscreen**)
- 🔊 **Audio forwarding** (tersedia di Android 11 ke atas):
  - 📱 **Android 12+**: aktif otomatis  
  - 🔓 **Android 11**: memerlukan layar tidak terkunci saat start SCRCPY  
  - ⬇️ **Android 10 ke bawah**: tidak mendukung audio (otomatis fallback ke video saja)  

---

## 🚀 Cara Install & Pakai

1. 📥 **Download** rilis terbaru (lihat tab **[Releases](../../releases)**).
2. ⚙️ Aktifkan **USB Debugging** di perangkat Android.  
   👉 (Pengaturan > Tentang Ponsel > ketuk **Build Number** 7x > aktifkan **Developer Options**)  
3. 💽 **Install driver USB** (jika belum):  
   – Windows: ikuti panduan di [developer.android.com](https://developer.android.com/studio/run/oem-usb)  
4. 🔌 **Hubungkan** perangkat Android ke PC via USB.  
5. ▶️ Jalankan berkas **`ScrcpyController.exe`**.  

---

## 🛠️ Tips Penggunaan

- 🐢 Jika mengalami **lag/delay**, turunkan opsi `Max FPS` atau `Video Bitrate`.  
- 🎧 Jika audio gagal, rekaman tetap berjalan dengan video saja.  
