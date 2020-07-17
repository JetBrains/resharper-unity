#!/usr/bin/env bash
set -e
set -o pipefail

pushd rider
# Use --info and/or --stacktrace to get logging
exec "$(cd "$(dirname "$0")"; pwd)/gradlew" buildPlugin $@
