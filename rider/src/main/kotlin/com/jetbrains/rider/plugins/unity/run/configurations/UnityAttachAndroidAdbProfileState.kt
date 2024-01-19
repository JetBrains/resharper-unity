package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.CantRunException
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.model.debuggerWorker.DebuggerStartInfoBase
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityAndroidAdbStartInfo
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import java.nio.file.Path
import kotlin.io.path.isDirectory

/**
 * [RunProfileState] to attach to an Android device via ADB
 */
class UnityAttachAndroidAdbProfileState(private val project: Project,
                                        private val remoteConfiguration: RemoteConfiguration,
                                        executionEnvironment: ExecutionEnvironment,
                                        targetName: String,
                                        private val deviceId: String)
    : UnityAttachProfileState(remoteConfiguration, executionEnvironment, targetName, false) {

    override suspend fun createModelStartInfo(lifetime: Lifetime): DebuggerStartInfoBase {

        val model = project.solution.frontendBackendModel
        var sdkRoot = model.getAndroidSdkRoot.startSuspending(lifetime, Unit)?.let { Path.of(it) }
        if (sdkRoot == null || !sdkRoot.isDirectory()) {
           sdkRoot = UnityInstallationFinder.getInstance(project).getAdditionalPlaybackEnginesRoot()?.resolve("AndroidPlayer")
        }

        if (sdkRoot?.isDirectory() == false) {
            throw CantRunException(UnityBundle.message("dialog.message.unable.to.find.androidSdkRoot.folder", sdkRoot))
        }

        return UnityAndroidAdbStartInfo(
            sdkRoot.toString(),
            deviceId,
            remoteConfiguration.address,
            remoteConfiguration.port,
            false,
            getUnityBundlesList()
        )
    }
}