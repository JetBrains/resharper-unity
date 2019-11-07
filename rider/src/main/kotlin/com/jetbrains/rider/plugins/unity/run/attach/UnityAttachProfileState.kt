package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rider.plugins.unity.run.UnityDebuggerOutputListener
import com.jetbrains.rider.run.IDebuggerOutputListener
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration

class UnityAttachProfileState(private val remoteConfiguration: RemoteConfiguration,
                              executionEnvironment: ExecutionEnvironment,
                              private val targetName: String,
                              private val isEditor: Boolean)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {

    override fun getDebuggerOutputEventsListener(): IDebuggerOutputListener {
        return UnityDebuggerOutputListener(executionEnvironment.project, remoteConfiguration.address, targetName, isEditor)
    }
}