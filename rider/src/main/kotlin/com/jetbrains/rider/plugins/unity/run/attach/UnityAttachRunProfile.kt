package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.Executor
import com.intellij.execution.configurations.RemoteRunProfile
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rider.debugger.IDotNetDebuggable
import icons.UnityIcons

class UnityAttachRunProfile(private val configurationName: String, private val configuration: UnityAttachConfiguration,
                            private val targetName: String, private val isEditor: Boolean)
    : RemoteRunProfile, IDotNetDebuggable {

    override fun getState(executor: Executor, executionEnvironment: ExecutionEnvironment): RunProfileState? {
        return UnityAttachProfileState(configuration, executionEnvironment, targetName, isEditor)
    }

    override fun getName() = configurationName
    override fun getIcon() = UnityIcons.RunConfigurations.AttachToUnityParentConfiguration
}