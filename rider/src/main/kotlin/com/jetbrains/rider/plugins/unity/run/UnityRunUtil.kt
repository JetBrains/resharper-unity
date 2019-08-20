package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.ProgramRunnerUtil
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.process.ProcessInfo
import com.intellij.execution.runners.ExecutionEnvironmentBuilder
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.run.attach.UnityAttachProcessConfiguration
import com.jetbrains.rider.plugins.unity.run.attach.UnityAttachRunProfile
import com.jetbrains.rider.plugins.unity.util.convertPidToDebuggerPort
import java.nio.file.Paths

object UnityRunUtil {

    fun isUnityEditorProcess(processInfo: ProcessInfo): Boolean {
        val name = processInfo.executableDisplayName
        var execPathName = "";
        if (processInfo.executableCannonicalPath.isPresent)
            execPathName = Paths.get(processInfo.executableCannonicalPath.get()).fileName.toString() // for the case of symlink
        return (name.startsWith("Unity", true)
                || name.contains("Unity.app")
                || execPathName.equals("Unity", true))
            && !name.contains("UnityDebug")
            && !name.contains("UnityShader")
            && !name.contains("UnityHelper")
            && !name.contains("Unity Helper")
            && !name.contains("Unity Hub")
            && !name.contains("UnityCrashHandler")
            && !name.contains("UnityPackageManager")
            && !name.contains("Unity.Licensing.Client")
            && !name.contains("unityhub", true)
    }

    fun attachToLocalUnityProcess(pid: Int, project: Project) {
        val port = convertPidToDebuggerPort(pid)
        attachToUnityProcess("localhost", port, "Unity Editor", project, true)
    }

    fun attachToUnityProcess(host: String, port: Int, playerId: String, project: Project, isEditor: Boolean) {
        val configuration = UnityAttachProcessConfiguration(host, port, playerId, isEditor)
        val profile = UnityAttachRunProfile(playerId, configuration, playerId, isEditor)
        val environment = ExecutionEnvironmentBuilder
            .create(project, DefaultDebugExecutor.getDebugExecutorInstance(), profile)
            .build()
        ProgramRunnerUtil.executeConfiguration(environment, false, true)
    }
}