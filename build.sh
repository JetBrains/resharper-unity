#!/usr/bin/env bash

echo "Ensure to call dotnet restore in the src directory first"

SDK_DIR="/usr/local/share/dotnet/sdk/1.0.1"

echo $SDK_DIR

export MSBuildExtensionsPath=$SDK_DIR/
export CscToolExe=$SDK_DIR/Roslyn/RunCsc.sh
export MSBuildSDKsPath=$SDK_DIR/Sdks

msbuild src/resharper-unity/resharper-unity.rider.csproj /t:pack /p:Configuration=Release /p:NuspecFile=resharper-unity.rider.nuspec
