package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration

class UnityAttachToPlayerProfileState(remoteConfiguration: RemoteConfiguration, executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment)