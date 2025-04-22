
# Costumized SCRCPY


## How to Install

1. Download the [Release](https://github.com/ZowieKMK/Screen_Copy/releases)
2. Enable Android USB Debugging
3. [Install USB Driver](https://developer.android.com/studio/run/win-usb) `https://developer.android.com/studio/run/win-usb`
4. Plug Android via USB
5. Run `RUN_SCRCPY.bat`

## Details
`RUN_SCRCPY.bat` Tested with Poco X6 Pro
```bash
  scrcpy --render-driver=OpenGL --video-bit-rate=60M --video-codec=h264 --max-fps=65 --stay-awake -f
```



## Flags :

`--render-driver=OpenGL` = Using OpenGL Render

`--video-codec=h264` = H264 Video Codec

`-f` = fullscreen

`--max-fps=65` = maximum framerate

`--video-bit-rate=60M` = video bit rate 


## Usefull Tips :
- Decrease the Framerate & Video Bitrate (`--max-fps=65` & `--video-bit-rate=60M`) if there any delay/lags,
- You can also enable Wireless Mode using [Wireless ADB](https://help.famoco.com/developers/dev-env/adb-over-wifi)

## Audio

Audio forwarding is supported for devices with _Android 11 or higher_, and it is
enabled by default:

 - For **Android 12 or newer**, it works out-of-the-box.
 - For **Android 11**, you'll need to ensure that the device screen is unlocked
   when starting scrcpy. A fake popup will briefly appear to make the system
   think that the shell app is in the foreground. Without this, audio capture
   will fail.
 - For **Android 10 or earlier**, audio cannot be captured and is automatically
   disabled.

If audio capture fails, then mirroring continues with video only (since audio is
enabled by default, it is not acceptable to make scrcpy fail if it is not
available), unless `--require-audio` is set.

