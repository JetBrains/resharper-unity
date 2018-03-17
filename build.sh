#!/usr/bin/env bash
set -e
set -o pipefail

pushd rider
./gradlew buildPlugin
