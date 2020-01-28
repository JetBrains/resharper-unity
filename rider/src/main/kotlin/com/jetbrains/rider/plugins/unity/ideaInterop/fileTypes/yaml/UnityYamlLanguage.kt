package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.yaml

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageBase

// The frontend already has YAML support. This is mapping extra file types to the backend implementation, which is just
// a YAML implementation. The backend adds extra references for Unity specific YAML files
object UnityYamlLanguage : RiderLanguageBase("UnityYaml", "UnityYaml")