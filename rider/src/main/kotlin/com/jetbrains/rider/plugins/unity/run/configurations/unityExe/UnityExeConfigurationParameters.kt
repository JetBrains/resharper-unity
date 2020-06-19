package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.openapi.project.Project
import com.jetbrains.rider.run.configurations.dotNetExe.DotNetExeConfigurationParameters

open class UnityExeConfigurationParameters(
    project: Project,
    exePath: String,
    programParameters: String,
    workingDirectory: String,
    envs: Map<String, String>,
    isPassParentEnvs: Boolean,
    useExternalConsole: Boolean,
    runtimeArguments: String
) : DotNetExeConfigurationParameters(
    project,
    exePath,
    programParameters,
    workingDirectory,
    envs,
    isPassParentEnvs,
    useExternalConsole,
    false,
    false,
    null,
    runtimeArguments
)
