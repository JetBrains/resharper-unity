package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.CantRunException
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.model.debuggerWorker.DebuggerStartInfoBase
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityIosUsbStartInfo
import com.jetbrains.rider.plugins.unity.run.UnityDebugEngine
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import kotlin.io.path.isDirectory

/**
 * [RunProfileState] to attach to an iOS device via USB
 */
class UnityAttachIosUsbProfileState(private val project: Project,
                                    debugEngine: UnityDebugEngine,
                                    executionEnvironment: ExecutionEnvironment, targetName: String,
                                    private val deviceId: String)
    : UnityAttachProfileState(debugEngine, executionEnvironment, targetName, false) {

    override suspend fun createMonoModelStartInfo(lifetime: Lifetime, monoDebugEngine: UnityDebugEngine.Mono): DebuggerStartInfoBase {
        val iosSupportPath =
            UnityInstallationFinder.getInstance(project).getAdditionalPlaybackEnginesRoot()?.resolve("iOSSupport")

        // This shouldn't be false - if we're starting debug, that means we'll have listed the USB device, and that
        // should mean we've already used the usbmuxd DLL from the support folder
        if (iosSupportPath?.isDirectory() == false) {
            throw CantRunException(UnityBundle.message("dialog.message.unable.to.find.iossupport.folder", iosSupportPath))
        }

        return UnityIosUsbStartInfo(iosSupportPath.toString(),
            deviceId,
            monoDebugEngine.host,
            monoDebugEngine.port,
            false,
            getUnityBundlesList(),
            getUnityPackagesList(project))
    }
}
