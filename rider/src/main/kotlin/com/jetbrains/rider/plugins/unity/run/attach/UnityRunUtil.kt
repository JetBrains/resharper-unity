package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.ProgramRunnerUtil
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.process.ProcessInfo
import com.intellij.execution.runners.ExecutionEnvironmentBuilder
import com.intellij.openapi.project.Project

object UnityRunUtil {

    fun isUnityEditorProcess(processInfo: ProcessInfo): Boolean {
        val name = processInfo.executableDisplayName
        return (name.startsWith("Unity", true) ||
            name.contains("Unity.app")) &&
            !name.contains("UnityDebug") &&
            !name.contains("UnityShader") &&
            !name.contains("UnityHelper") &&
            !name.contains("Unity Helper") &&
            !name.contains("Unity Hub") &&
            !name.contains("UnityCrashHandler")
    }

    fun runAttach(pid: Int, project: Project) {
        val port = 56000 + pid % 1000
        runAttach("localhost", port, pid.toString(), project)
    }

    fun runAttach(host: String, port: Int, playerId: String, project: Project) {
        val configuration = UnityLocalAttachConfiguration(port, playerId, host)
        val profile = UnityLocalAttachRunProfile(playerId, configuration)
        val environment = ExecutionEnvironmentBuilder
            .create(project, DefaultDebugExecutor.getDebugExecutorInstance(), profile)
            .build()
        ProgramRunnerUtil.executeConfiguration(environment, false, true)
    }
}