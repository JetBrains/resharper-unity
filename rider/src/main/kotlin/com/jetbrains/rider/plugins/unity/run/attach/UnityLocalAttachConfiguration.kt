package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.Executor
import com.intellij.execution.configurations.RunProfile
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.icons.AllIcons
import com.jetbrains.rider.debugger.IDotNetDebuggable
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import javax.swing.Icon

class UnityLocalAttachConfiguration(override var port: Int, private val playerId: String, host: String = "127.0.0.1") : RemoteConfiguration, RunProfile, IDotNetDebuggable {

    override fun getName(): String = playerId
    override fun getIcon(): Icon = AllIcons.Actions.StartDebugger

    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState? {
        if (executor.id != DefaultDebugExecutor.EXECUTOR_ID)
            return null
        return UnityLocalAttachProfileState(this, environment)
    }

    override var address: String = host
    override var listenPortForConnections: Boolean = false
}