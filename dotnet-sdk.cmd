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
export DOTNET_MULTILEVEL_LOOKUP=false

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
    DOTNET_HASH_URL=91c41b31-cf90-4771-934b-6928bbb48aaf/76e95bac2a4cb3fd50c920fd1601527c
    DOTNET_FILE_NAME=dotnet-sdk-7.0.102-osx-x64
    ;;
  arm64)
    DOTNET_HASH_URL=d0c47b58-a384-46b3-8fce-bd9188541858/dbfe7b537396b747255e65c0fbc9641e
    DOTNET_FILE_NAME=dotnet-sdk-7.0.102-osx-arm64
    ;;
  *) echo "Unknown architecture $DOTNET_ARCH" >&2; exit 1;;
  esac;;
Linux)
  DOTNET_ARCH=$(linux$(getconf LONG_BIT) uname -m)
  case $DOTNET_ARCH in
  x86_64)
    if is_linux_musl; then
      DOTNET_HASH_URL=43fbb34e-6e21-4abd-aa90-0e65de1fa3c2/0f29b8b0f6e7d5dfd6e790e49a4700db
      DOTNET_FILE_NAME=dotnet-sdk-7.0.102-linux-musl-x64
    else
      DOTNET_HASH_URL=c646b288-5d5b-4c9c-a95b-e1fad1c0d95d/e13d71d48b629fe3a85f5676deb09e2d
      DOTNET_FILE_NAME=dotnet-sdk-7.0.102-linux-x64
    fi
    ;;
  aarch64)
    if is_linux_musl; then
      DOTNET_HASH_URL=b98e56bf-6021-4236-aa19-58b216a4674e/f367378def5dea5bc439dbb94b7699af
      DOTNET_FILE_NAME=dotnet-sdk-7.0.102-linux-musl-arm64
    else
      DOTNET_HASH_URL=72ec0dc2-f425-48c3-97f1-dc83740ba400/78e8fa01fa9987834fa01c19a23dd2e7
      DOTNET_FILE_NAME=dotnet-sdk-7.0.102-linux-arm64
    fi
    ;;
  armv7l | armv8l)
    if is_linux_musl; then
      DOTNET_HASH_URL=de77d6a5-d77e-4421-8198-135b3cce9caf/ad2194c26f424c81fb2b79decb6e9f2b
      DOTNET_FILE_NAME=dotnet-sdk-7.0.102-linux-musl-arm
    else
      DOTNET_HASH_URL=54b057ec-36ef-4808-a436-50ee3fa39a44/87d696a761176b721daaf8ab9761c9c8
      DOTNET_FILE_NAME=dotnet-sdk-7.0.102-linux-arm
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
    set DOTNET_HASH_URL=a93af409-9846-4340-b810-1c112db0969b/52557cefadc77036a5febb00a31a439b
    set DOTNET_FILE_NAME=dotnet-sdk-7.0.102-win-arm64
) else (

if "%ARCH%"=="AMD64" (
    set DOTNET_HASH_URL=7c869d6e-b49e-4c52-b197-77fca05f0c69/f3b6fb63231c8ed6afc585da090d4595
    set DOTNET_FILE_NAME=dotnet-sdk-7.0.102-win-x64
) else (

if "%ARCH%"=="x86" (
    set DOTNET_HASH_URL=30d69e4b-74d9-4043-9b50-2f91b27d9e80/c6ecb4b16858afb2fc1f056bd3ecdbb4
    set DOTNET_FILE_NAME=dotnet-sdk-7.0.102-win-x86
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
set export DOTNET_MULTILEVEL_LOOKUP=false

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