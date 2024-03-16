@echo off
setlocal

REM download urls
set "ffmpeg_url=https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-lgpl-shared.zip"
set "ytdlp_url=https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"

REM get needed dirs
set "script_dir=%~dp0"
set "download_dir=%script_dir%download" 
if not exist "%download_dir%" mkdir "%download_dir%"
set "output_dir=%script_dir%bin"
if not exist "%output_dir%" mkdir "%output_dir%"

REM ffmpeg
echo Downloading ffmpeg...
powershell -command "(New-Object System.Net.WebClient).DownloadFile('%ffmpeg_url%', '%download_dir%\ffmpeg.zip')"

if not exist "%download_dir%\ffmpeg.zip" (
    echo Failed to download ffmpeg.
    pause
    exit /b 1
)

echo Extracting ffmpeg...
powershell -command "try { Expand-Archive -Path '%download_dir%\ffmpeg.zip' -DestinationPath '%download_dir%' } catch { echo Failed to extract ffmpeg: $_; exit 1 }"

if errorlevel 1 (
    echo Failed to extract ffmpeg.
    pause
    exit /b 1
)

echo Moving ffmpeg binaries...
move "%download_dir%\ffmpeg-master-latest-win64-lgpl-shared\bin\*" "%output_dir%"

echo ffmpeg binaries downloaded

REM yt-dlp
echo Downloading yt-dlp...
powershell -command "(New-Object System.Net.WebClient).DownloadFile('%ytdlp_url%', '%download_dir%\yt-dlp.exe')"

if not exist "%download_dir%\yt-dlp.exe" (
    echo Failed to download yt-dlp.
    pause
    exit /b 1
)

move "%download_dir%\yt-dlp.exe" "%output_dir%"
echo yt-dlp downloaded

REM everything complete
echo everything downloaded
rmdir /s /q "%download_dir%"