@echo off
:start
scrcpy --render-driver=opengl --video-bit-rate=50M --audio-source=mic-voice-communication --max-fps=60 --print-fps -f
goto start
