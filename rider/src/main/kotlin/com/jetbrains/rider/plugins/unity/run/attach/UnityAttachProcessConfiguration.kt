package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.Executor
import com.intellij.execution.configurations.RunProfile
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.icons.AllIcons
import com.jetbrains.rider.debugger.IRiderDebuggable
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import javax.swing.Icon

class UnityAttachProcessConfiguration(override var address: String, override var port: Int,
                                      private val playerId: String, private val isEditor: Boolean)
    : RemoteConfiguration, RunProfile, IRiderDebuggable {

    override fun getName(): String = playerId
    override fun getIcon(): Icon = AllIcons.Actions.StartDebugger

    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState? {
        if (executor.id != DefaultDebugExecutor.EXECUTOR_ID)
            return null
        return UnityAttachProfileState(this, environment, playerId, isEditor)
    }

    override var listenPortForConnections: Boolean = false
}