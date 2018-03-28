package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.Executor
import com.intellij.execution.configurations.RemoteRunProfile
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rider.debugger.IDotNetDebuggable
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import javax.swing.Icon

class UnityLocalAttachRunProfile(private val configurationName: String, private val configuration: UnityLocalAttachConfiguration) : RemoteRunProfile, IDotNetDebuggable {

    override fun getState(p0: Executor, p1: ExecutionEnvironment): RunProfileState? {
        return UnityLocalAttachProfileState(configuration, p1)
    }

    override fun getName(): String {
        return configurationName
    }

    override fun getIcon(): Icon {
        return UnityIcons.Icons.AttachEditorDebugConfiguration
    }
}