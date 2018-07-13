package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.Executor
import com.intellij.execution.configurations.RemoteRunProfile
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rider.debugger.IDotNetDebuggable
import com.jetbrains.rider.plugins.unity.util.UnityIcons

class UnityLocalAttachRunProfile(private val configurationName: String, private val configuration: UnityLocalAttachConfiguration) : RemoteRunProfile, IDotNetDebuggable {

    override fun getState(executor: Executor, executionEnvironment: ExecutionEnvironment): RunProfileState? {
        return UnityLocalAttachProfileState(configuration, executionEnvironment)
    }

    override fun getName() = configurationName
    override fun getIcon() = UnityIcons.RunConfigurations.AttachToUnityParentConfiguration
}