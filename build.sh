#!/usr/bin/env bash
set -e
set -o pipefail

pushd rider
# Use --info and/or --stacktrace to get logging
./gradlew buildPlugin $@
