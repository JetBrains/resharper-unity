package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.jetbrains.rider.run.configurations.exe.ExeConfigurationParameters

open class UnityExeConfigurationParameters(
    exePath: String,
    programParameters: String,
    workingDirectory: String,
    envs: Map<String, String>,
    isPassParentEnvs: Boolean,
    useExternalConsole: Boolean
) : ExeConfigurationParameters(
    exePath,
    programParameters,
    workingDirectory,
    envs,
    isPassParentEnvs,
    useExternalConsole
)
