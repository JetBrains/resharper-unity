:<<"::CMDLITERAL"
@ECHO OFF
GOTO :CMDSCRIPT
::CMDLITERAL

set -eu

DOTNET_VERSION=8.0.101
DOTNET_SHORT_VERSION=$DOTNET_VERSION
COMPANY_DIR="JetBrains"
TARGET_DIR="${TEMPDIR:-$HOME/.local/share}/$COMPANY_DIR/dotnet-cmd"
KEEP_ROSETTA2=false
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
export DOTNET_CLI_TELEMETRY_OPTOUT=true
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true
export DOTNET_MULTILEVEL_LOOKUP=false
unset DOTNET_ROOT
unset MSBUILD_TASK_PARENT_PROCESS_PID
unset MSBuildSDKsPath

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
  DOTNET_OS=osx
  UNAME_ARCH=$(uname -m)
  if ! $KEEP_ROSETTA2 && [ "$(sysctl -n sysctl.proc_translated 2>/dev/null || true)" = "1" ]; then
    DOTNET_ARCH=arm64
  fi
  case $UNAME_ARCH in
  arm64)  DOTNET_ARCH=arm64;;
  x86_64) DOTNET_ARCH=x64;;
  *) echo "Unknown architecture $UNAME_ARCH" >&2; exit 1;;
  esac;;
Linux)
  DOTNET_OS=linux
  UNAME_ARCH=$(linux$(getconf LONG_BIT) uname -m)
  case $UNAME_ARCH in
  armv7l | armv8l) is_linux_musl && DOTNET_ARCH=musl-arm   || DOTNET_ARCH=arm;;
  aarch64)         is_linux_musl && DOTNET_ARCH=musl-arm64 || DOTNET_ARCH=arm64;;
  x86_64)          is_linux_musl && DOTNET_ARCH=musl-x64   || DOTNET_ARCH=x64;;
  *) echo "Unknown architecture $UNAME_ARCH" >&2; exit 1;;
  esac;;
*) echo "Unknown platform: $(uname)" >&2; exit 1;;
esac

DOTNET_URL=https://cache-redirector.jetbrains.com/builds.dotnet.microsoft.com/dotnet/Sdk/$DOTNET_VERSION/dotnet-sdk-$DOTNET_VERSION-$DOTNET_OS-$DOTNET_ARCH.tar.gz
DOTNET_TARGET_DIR=$TARGET_DIR/s$DOTNET_SHORT_VERSION-$DOTNET_ARCH
DOTNET_TEMP_FILE=$TARGET_DIR/temp.tar.gz

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

# Ensure usage of the same dotnet runtime for any child dotnet processes
DOTNET_HOST_PATH=$DOTNET_TARGET_DIR/dotnet

exec "$DOTNET_HOST_PATH" "$@"

:CMDSCRIPT

setlocal
set DOTNET_VERSION=8.0.101
set DOTNET_SHORT_VERSION=%DOTNET_VERSION%
set COMPANY_NAME=JetBrains
set TARGET_DIR=%LOCALAPPDATA%\%COMPANY_NAME%\dotnet-cmd\

for /f "tokens=3 delims= " %%a in ('reg query "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v "PROCESSOR_ARCHITECTURE"') do set ARCH=%%a

if "%ARCH%"=="ARM"   (set DOTNET_ARCH=arm)   else (
if "%ARCH%"=="ARM64" (set DOTNET_ARCH=arm64) else (
if "%ARCH%"=="AMD64" (set DOTNET_ARCH=x64)   else (
if "%ARCH%"=="x86"   (set DOTNET_ARCH=x86)   else (

echo Unknown Windows architecture
goto fail

))))

set DOTNET_URL=https://cache-redirector.jetbrains.com/builds.dotnet.microsoft.com/dotnet/Sdk/%DOTNET_VERSION%/dotnet-sdk-%DOTNET_VERSION%-win-%DOTNET_ARCH%.zip
set DOTNET_TARGET_DIR=%TARGET_DIR%s%DOTNET_SHORT_VERSION%-%DOTNET_ARCH%\
set DOTNET_TEMP_FILE=%TARGET_DIR%temp.zip
set DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
set DOTNET_CLI_TELEMETRY_OPTOUT=true
set DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true
set DOTNET_MULTILEVEL_LOOKUP=false
set DOTNET_ROOT=
set MSBUILD_TASK_PARENT_PROCESS_PID=
set MSBuildSDKsPath=

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

if not exist "%DOTNET_TARGET_DIR%dotnet.exe" (
  echo Unable to find dotnet.exe under %DOTNET_TARGET_DIR%
  goto fail
)

REM Prevent globally installed .NET Core from leaking into this runtime's lookup
SET DOTNET_MULTILEVEL_LOOKUP=0
REM Ensure usage of the same dotnet runtime for any child dotnet processes
SET DOTNET_HOST_PATH=%DOTNET_TARGET_DIR%dotnet.exe

call "%DOTNET_HOST_PATH%" %*
exit /B %ERRORLEVEL%
endlocal

:fail
echo "FAIL"
exit /b 1