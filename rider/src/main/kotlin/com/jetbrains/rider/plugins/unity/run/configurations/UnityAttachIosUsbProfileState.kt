package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.debugger.DebuggerHelperHost
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.run.WorkerRunInfo
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration

/**
 * [RunProfileState] to attach to an iOS device via USB
 */
class UnityAttachIosUsbProfileState(private val project: Project, private val remoteConfiguration: RemoteConfiguration,
                                    executionEnvironment: ExecutionEnvironment, targetName: String,
                                    private val deviceId: String)
    : UnityAttachProfileState(remoteConfiguration, executionEnvironment, targetName, false) {

    override suspend fun createWorkerRunInfo(lifetime: Lifetime, helper: DebuggerHelperHost, port: Int): WorkerRunInfo {
        val runCmd = super.createWorkerRunInfo(lifetime, helper, port)
        // TODO: Can we do this over protocol?
        // We could avoid hard coding port 12000 (Unity use this port in their debugger plugins)
        UnityInstallationFinder.getInstance(project)
            .getAdditionalPlaybackEnginesRoot()?.resolve("iOSSupport")?.let { proxyFolderPath ->
            runCmd.commandLine.withEnvironment("_RIDER_UNITY_IOS_USB_PROXY_PATH", proxyFolderPath.toString())
            runCmd.commandLine.withEnvironment("_RIDER_UNITY_IOS_USB_DEVICE_ID", deviceId)
            runCmd.commandLine.withEnvironment("_RIDER_UNITY_IOS_USB_LOCAL_PORT", "${remoteConfiguration.port}")
        }
        return runCmd
    }
}