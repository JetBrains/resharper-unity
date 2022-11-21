:<<"::CMDLITERAL"
@ECHO OFF
GOTO :CMDSCRIPT
::CMDLITERAL

set -eu

SCRIPT_VERSION=dotnet-cmd-v2
COMPANY_DIR="JetBrains"
TARGET_DIR="${TEMPDIR:-$HOME/.local/share}/$COMPANY_DIR/dotnet-cmd"
KEEP_ROSETTA2=false
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
export DOTNET_CLI_TELEMETRY_OPTOUT=true

warn () {
    echo "$*"
}

die () {
    echo
    echo "$*"
    echo
    exit 1
}

retry_on_error () {
  local n="$1"
  shift

  for i in $(seq 2 "$n"); do
    "$@" 2>&1 && return || echo "WARNING: Command '$1' returned non-zero exit status $?, try again"
  done
  "$@"
}

is_linux_musl () {
  (ldd --version 2>&1 || true) | grep -q musl
}

case $(uname) in
Darwin)
  DOTNET_ARCH=$(uname -m)
  if ! $KEEP_ROSETTA2 && [ "$(sysctl -n sysctl.proc_translated 2>/dev/null || true)" = "1" ]; then
    DOTNET_ARCH=arm64
  fi
  case $DOTNET_ARCH in
  x86_64)
    DOTNET_HASH_URL=09e4b839-c809-49b5-b424-86d8ca67b42e/54be2d3868ae49fa00b1cc59065d5e2e
    DOTNET_FILE_NAME=dotnet-sdk-7.0.100-osx-x64
    ;;
  arm64)
    DOTNET_HASH_URL=1a0e0146-3401-4c2b-9369-4cb5e72785b7/8548e8f2c619330ea7282e15d1116155
    DOTNET_FILE_NAME=dotnet-sdk-7.0.100-osx-arm64
    ;;
  *) echo "Unknown architecture $DOTNET_ARCH" >&2; exit 1;;
  esac;;
Linux)
  DOTNET_ARCH=$(linux$(getconf LONG_BIT) uname -m)
  case $DOTNET_ARCH in
  x86_64)
    if is_linux_musl; then
      DOTNET_HASH_URL=302a8de6-66a1-418f-b349-638d4b88dcd4/248ad4dfcbbf931009fa736ced0518e0
      DOTNET_FILE_NAME=dotnet-sdk-7.0.100-linux-musl-x64
    else
      DOTNET_HASH_URL=253e5af8-41aa-48c6-86f1-39a51b44afdc/5bb2cb9380c5b1a7f0153e0a2775727b
      DOTNET_FILE_NAME=dotnet-sdk-7.0.100-linux-x64
    fi
    ;;
  aarch64)
    if is_linux_musl; then
      DOTNET_HASH_URL=a620c089-f7f2-4b54-9c29-1e123e346c40/52bf752a693f9139133a041009d27ae4
      DOTNET_FILE_NAME=dotnet-sdk-7.0.100-linux-musl-arm64
    else
      DOTNET_HASH_URL=47337472-c910-4815-9d9b-80e1a30fcf16/14847f6a51a6a7e53a859d4a17edc311
      DOTNET_FILE_NAME=dotnet-sdk-7.0.100-linux-arm64
    fi
    ;;
  armv7l | armv8l)
    if is_linux_musl; then
      DOTNET_HASH_URL=14c9ff00-b457-4bf3-ba1e-b54634827e8e/1292a52a53115da28ce17b19a2a0cd45
      DOTNET_FILE_NAME=dotnet-sdk-7.0.100-linux-musl-arm
    else
      DOTNET_HASH_URL=fd193b5b-f171-436e-8783-5eb77a7ad5ee/2a0af2a874f3c4f7da0199dc64996829
      DOTNET_FILE_NAME=dotnet-sdk-7.0.100-linux-arm
    fi
    ;;
  *) echo "Unknown architecture $DOTNET_ARCH" >&2; exit 1;;
  esac;;
*) echo "Unknown platform: $(uname)" >&2; exit 1;;
esac

DOTNET_URL=https://cache-redirector.jetbrains.com/download.visualstudio.microsoft.com/download/pr/$DOTNET_HASH_URL/$DOTNET_FILE_NAME.tar.gz
DOTNET_TARGET_DIR=$TARGET_DIR/$DOTNET_FILE_NAME-$SCRIPT_VERSION
DOTNET_TEMP_FILE=$TARGET_DIR/dotnet-sdk-temp.tar.gz

if grep -q -x "$DOTNET_URL" "$DOTNET_TARGET_DIR/.flag" 2>/dev/null; then
  # Everything is up-to-date in $DOTNET_TARGET_DIR, do nothing
  true
else
while true; do  # Note(k15tfu): for goto
  mkdir -p "$TARGET_DIR"

  LOCK_FILE="$TARGET_DIR/.dotnet-cmd-lock.pid"
  TMP_LOCK_FILE="$TARGET_DIR/.tmp.$$.pid"
  echo $$ >"$TMP_LOCK_FILE"

  while ! ln "$TMP_LOCK_FILE" "$LOCK_FILE" 2>/dev/null; do
    LOCK_OWNER=$(cat "$LOCK_FILE" 2>/dev/null || true)
    while [ -n "$LOCK_OWNER" ] && ps -p $LOCK_OWNER >/dev/null; do
      warn "Waiting for the process $LOCK_OWNER to finish bootstrap dotnet.cmd"
      sleep 1
      LOCK_OWNER=$(cat "$LOCK_FILE" 2>/dev/null || true)

      # Hurry up, bootstrap is ready..
      if grep -q -x "$DOTNET_URL" "$DOTNET_TARGET_DIR/.flag" 2>/dev/null; then
        break 3  # Note(k15tfu): goto out of the outer if-else block.
      fi
    done

    if [ -n "$LOCK_OWNER" ] && grep -q -x $LOCK_OWNER "$LOCK_FILE" 2>/dev/null; then
      die "ERROR: The lock file $LOCK_FILE still exists on disk after the owner process $LOCK_OWNER exited"
    fi
  done

  trap "rm -f \"$LOCK_FILE\"" EXIT
  rm "$TMP_LOCK_FILE"

  if ! grep -q -x "$DOTNET_URL" "$DOTNET_TARGET_DIR/.flag" 2>/dev/null; then
    warn "Downloading $DOTNET_URL to $DOTNET_TEMP_FILE"

    rm -f "$DOTNET_TEMP_FILE"
    if command -v curl >/dev/null 2>&1; then
      if [ -t 1 ]; then CURL_PROGRESS="--progress-bar"; else CURL_PROGRESS="--silent --show-error"; fi
      retry_on_error 5 curl -L $CURL_PROGRESS --output "${DOTNET_TEMP_FILE}" "$DOTNET_URL"
    elif command -v wget >/dev/null 2>&1; then
      if [ -t 1 ]; then WGET_PROGRESS=""; else WGET_PROGRESS="-nv"; fi
      retry_on_error 5 wget $WGET_PROGRESS -O "${DOTNET_TEMP_FILE}" "$DOTNET_URL"
    else
      die "ERROR: Please install wget or curl"
    fi

    warn "Extracting $DOTNET_TEMP_FILE to $DOTNET_TARGET_DIR"
    rm -rf "$DOTNET_TARGET_DIR"
    mkdir -p "$DOTNET_TARGET_DIR"

    tar -x -f "$DOTNET_TEMP_FILE" -C "$DOTNET_TARGET_DIR"
    rm -f "$DOTNET_TEMP_FILE"

    echo "$DOTNET_URL" >"$DOTNET_TARGET_DIR/.flag"
  fi

  rm "$LOCK_FILE"
  break
done
fi

if [ ! -x "$DOTNET_TARGET_DIR/dotnet" ]; then
  die "Unable to find dotnet under $DOTNET_TARGET_DIR"
fi

exec "$DOTNET_TARGET_DIR/dotnet" "$@"

:CMDSCRIPT

setlocal
set SCRIPT_VERSION=v2
set COMPANY_NAME=JetBrains
set TARGET_DIR=%LOCALAPPDATA%\%COMPANY_NAME%\dotnet-cmd\

for /f "tokens=3 delims= " %%a in ('reg query "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v "PROCESSOR_ARCHITECTURE"') do set ARCH=%%a

if "%ARCH%"=="ARM64" (
    set DOTNET_HASH_URL=2586d573-4d02-43b9-bf4a-452d4170daaa/0f25ae6ea4381187688ddde932655343
    set DOTNET_FILE_NAME=dotnet-sdk-7.0.100-win-arm64
) else (

if "%ARCH%"=="AMD64" (
    set DOTNET_HASH_URL=1fb808dc-d017-4460-94f8-bf1ac83e6cd8/756b301e714755e411b84684b885a516
    set DOTNET_FILE_NAME=dotnet-sdk-7.0.100-win-x64
) else (

if "%ARCH%"=="x86" (
    set DOTNET_HASH_URL=c45d50f1-c3f6-47cd-b9c9-861d095f8cb7/5a5ed9fb5e4b40fa4d4131cbe79d6a36
    set DOTNET_FILE_NAME=dotnet-sdk-7.0.100-win-x86
) else (

echo Unknown Windows architecture
goto fail

)))

set DOTNET_URL=https://cache-redirector.jetbrains.com/download.visualstudio.microsoft.com/download/pr/%DOTNET_HASH_URL%/%DOTNET_FILE_NAME%.zip
set DOTNET_TARGET_DIR=%TARGET_DIR%%DOTNET_FILE_NAME%-%SCRIPT_VERSION%\
set DOTNET_TEMP_FILE=%TARGET_DIR%dotnet-sdk-temp.zip
set DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
set DOTNET_CLI_TELEMETRY_OPTOUT=true

set POWERSHELL=%SystemRoot%\system32\WindowsPowerShell\v1.0\powershell.exe

if not exist "%DOTNET_TARGET_DIR%.flag" goto downloadAndExtractDotNet

set /p CURRENT_FLAG=<"%DOTNET_TARGET_DIR%.flag"
if "%CURRENT_FLAG%" == "%DOTNET_URL%" goto continueWithDotNet

:downloadAndExtractDotNet

set DOWNLOAD_AND_EXTRACT_DOTNET_PS1= ^
Set-StrictMode -Version 3.0; ^
$ErrorActionPreference = 'Stop'; ^
 ^
$createdNew = $false; ^
$lock = New-Object System.Threading.Mutex($true, 'Global\dotnet-cmd-lock', [ref]$createdNew); ^
if (-not $createdNew) { ^
    Write-Host 'Waiting for the other process to finish bootstrap dotnet.cmd'; ^
    [void]$lock.WaitOne(); ^
} ^
 ^
try { ^
    if ((Get-Content '%DOTNET_TARGET_DIR%.flag' -ErrorAction Ignore) -ne '%DOTNET_URL%') { ^
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; ^
        Write-Host 'Downloading %DOTNET_URL% to %DOTNET_TEMP_FILE%'; ^
        [void](New-Item '%TARGET_DIR%' -ItemType Directory -Force); ^
        (New-Object Net.WebClient).DownloadFile('%DOTNET_URL%', '%DOTNET_TEMP_FILE%'); ^
 ^
        Write-Host 'Extracting %DOTNET_TEMP_FILE% to %DOTNET_TARGET_DIR%'; ^
        if (Test-Path '%DOTNET_TARGET_DIR%') { ^
            Remove-Item '%DOTNET_TARGET_DIR%' -Recurse; ^
        } ^
        Add-Type -A 'System.IO.Compression.FileSystem'; ^
        [IO.Compression.ZipFile]::ExtractToDirectory('%DOTNET_TEMP_FILE%', '%DOTNET_TARGET_DIR%'); ^
        Remove-Item '%DOTNET_TEMP_FILE%'; ^
 ^
        Set-Content '%DOTNET_TARGET_DIR%.flag' -Value '%DOTNET_URL%'; ^
    } ^
} ^
finally { ^
    $lock.ReleaseMutex(); ^
}

"%POWERSHELL%" -nologo -noprofile -Command %DOWNLOAD_AND_EXTRACT_DOTNET_PS1%
if errorlevel 1 goto fail

:continueWithDotNet

if not exist "%DOTNET_TARGET_DIR%\dotnet.exe" (
  echo Unable to find dotnet.exe under %DOTNET_TARGET_DIR%
  goto fail
)

REM Prevent globally installed .NET Core from leaking into this runtime's lookup
SET DOTNET_MULTILEVEL_LOOKUP=0

for /f "tokens=2 delims=:." %%c in ('chcp') do set /a PREV_CODE_PAGE=%%c
chcp 65001 >nul && call "%DOTNET_TARGET_DIR%\dotnet.exe" %*
set /a DOTNET_EXIT_CODE=%ERRORLEVEL%
chcp %PREV_CODE_PAGE% >nul

exit /B %DOTNET_EXIT_CODE%
endlocal

:fail
echo "FAIL"
exit /b 1