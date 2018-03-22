package com.jetbrains.rider.plugins.unity.util.attach

import com.intellij.execution.ProgramRunnerUtil
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.process.ProcessInfo
import com.intellij.execution.runners.ExecutionEnvironmentBuilder
import com.intellij.openapi.project.Project
import com.intellij.xdebugger.attach.XLocalAttachDebugger

class UnityAttachDebugger : XLocalAttachDebugger {

    override fun getDebuggerDisplayName(): String {
        return "Mono Debugger"
    }

    override fun attachDebugSession(project: Project, processInfo: ProcessInfo) {
        val configuration = UnityLocalAttachConfiguration(processInfo.pid)
        val environment = ExecutionEnvironmentBuilder
            .create(project, DefaultDebugExecutor.getDebugExecutorInstance(), configuration)
            .build()
        ProgramRunnerUtil.executeConfiguration(environment, false, true)
    }

}