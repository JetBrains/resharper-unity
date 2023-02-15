package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.process.OSProcessUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime

class UnityEditorListener(project: Project,
                          lifetime: Lifetime,
                          onEditorAdded: (UnityProcess) -> Unit,
                          onEditorRemoved: (UnityProcess) -> Unit) {

    companion object {
        private val logger = Logger.getInstance(UnityEditorListener::class.java)
    }

    // Refresh once a second
    private val refreshPeriod: Long = 1000
    private val editorProcesses = mutableMapOf<Int, UnityLocalProcess>()

    init {
        val refreshTimer = kotlin.concurrent.timer("Listen for Unity Editor processes", true, 0L, refreshPeriod) {
            refreshUnityEditorProcesses(project, onEditorAdded, onEditorRemoved)
        }

        lifetime.onTermination { refreshTimer.cancel() }
    }

    private fun refreshUnityEditorProcesses(
        project: Project,
        onEditorAdded: (UnityProcess) -> Unit,
        onEditorRemoved: (UnityProcess) -> Unit
    ) {
        val start = System.currentTimeMillis()
        logger.trace("Refreshing local editor processes...")

        val processes = OSProcessUtil.getProcessList().filter {
            UnityRunUtil.isUnityEditorProcess(it)
        }

        editorProcesses.keys.filterNot { existingEditorPid ->
            processes.any { p -> p.pid == existingEditorPid }
        }.forEach { p ->
            editorProcesses.remove(p)?.let {
                logger.trace("Removing old Unity editor ${it.displayName}:${it.pid}")
                onEditorRemoved(it)
            }
        }

        val newProcesses = processes.filter { !editorProcesses.containsKey(it.pid) }
        val unityProcessInfoMap = UnityRunUtil.getAllUnityProcessInfo(newProcesses, project)
        newProcesses.forEach { processInfo ->
            val unityProcessInfo = unityProcessInfoMap[processInfo.pid]
            val editorProcess = if (!unityProcessInfo?.roleName.isNullOrEmpty()) {
                UnityEditorHelper(processInfo.executableName, unityProcessInfo?.roleName!!, processInfo.pid, unityProcessInfo.projectName)
            }
            else {
                UnityEditor(processInfo.executableName, processInfo.pid, unityProcessInfo?.projectName)
            }

            logger.trace("Adding Unity editor process ${editorProcess.displayName}:${editorProcess.pid}")

            editorProcesses[processInfo.pid] = editorProcess
            onEditorAdded(editorProcess)
        }

        if (logger.isTraceEnabled) {
            val duration = System.currentTimeMillis() - start
            logger.trace("Finished refreshing local editor processes. Took ${duration}ms")
        }
    }
}