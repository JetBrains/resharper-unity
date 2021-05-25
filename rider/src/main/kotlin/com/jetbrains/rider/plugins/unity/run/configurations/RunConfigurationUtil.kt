package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.Executor
import com.intellij.execution.ProgramRunnerUtil
import com.intellij.execution.configurations.RunProfile
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ExecutionEnvironmentBuilder
import com.intellij.icons.AllIcons
import com.intellij.openapi.project.Project
import com.jetbrains.rider.debugger.IRiderDebuggable
import com.jetbrains.rider.plugins.unity.run.*
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import javax.swing.Icon

fun attachToUnityProcess(project: Project, process: UnityProcess) {
    val runProfile = UnityProcessRunProfile(project, process)
    val environment = ExecutionEnvironmentBuilder
        .create(project, DefaultDebugExecutor.getDebugExecutorInstance(), runProfile)
        .build()
    ProgramRunnerUtil.executeConfiguration(environment, false, true)
}

/**
 * Simple [RunProfile] implementation to connect to a [UnityProcess] via the Attach To menu
 */
class UnityProcessRunProfile(private val project: Project, private val process: UnityProcess)
    : RunProfile, IRiderDebuggable {

    override fun getName(): String = process.displayName
    override fun getIcon(): Icon = AllIcons.Actions.StartDebugger

    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState? {
        return when (process) {
            is UnityIosUsbProcess -> {
                // We need to tell the debugger which port to connect to. The proxy will open this port and forward
                // traffic to the on-device port of 56000. These port numbers are hardcoded, and follow what Unity does
                // in their debugger plugins. There is a chance that the local 12000 port is in use, but there's not
                // much we can do about that - we tell the proxy both port numbers and it tries to set things up.
                UnityAttachIosUsbProfileState(project, MyRemoteConfiguration("127.0.0.1", 12000),
                    environment, process.displayName, process.deviceId)
            }
            is UnityLocalUwpPlayer -> {
                UnityAttachLocalUwpProfileState(MyRemoteConfiguration(process.host, process.port), environment,
                    process.displayName, process.packageName)
            }
            is UnityRemoteConnectionDetails -> {
                UnityAttachProfileState(MyRemoteConfiguration(process.host, process.port), environment, process.displayName,
                    process is UnityEditor || process is UnityEditorHelper
                )
            }
            else -> null
        }
    }

    private class MyRemoteConfiguration(override var address: String, override var port: Int) : RemoteConfiguration {
        override var listenPortForConnections: Boolean = false
    }
}
