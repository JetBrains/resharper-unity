#!/usr/bin/env bash
set -e
set -o pipefail

pushd rider
./gradlew --info --stacktrace buildBackend
./gradlew --info --stacktrace buildPlugin