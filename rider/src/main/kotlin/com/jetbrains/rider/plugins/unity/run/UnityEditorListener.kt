package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.process.OSProcessUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.diagnostic.trace
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService

class UnityEditorListener {

    companion object {
        private val logger = Logger.getInstance(UnityEditorListener::class.java)
    }

    // Refresh once a second
    private val refreshPeriod: Long = 1000
    private val editorProcesses = mutableMapOf<Int, UnityDebugTarget>()

    // Invoked on UI thread
    fun startListening(project: Project,
                       lifetime: Lifetime,
                       onEditorAdded: (UnityDebugTarget) -> Unit,
                       onEditorRemoved: (UnityDebugTarget) -> Unit) {
        val refreshTimer = kotlin.concurrent.timer("Listen for Unity Editor processes", true, 0L, refreshPeriod) {
            refreshUnityEditorProcesses(project, onEditorAdded, onEditorRemoved)
        }

        lifetime.onTermination { refreshTimer.cancel() }
    }

    // Invoked on background thread
    fun getEditorProcesses(project: Project): List<UnityDebugTarget> {
        val lifetime = UnityProjectLifetimeService.getLifetime(project).createNested()
        try {
            val editors = mutableListOf<UnityDebugTarget>()
            refreshUnityEditorProcesses(project, { editors.add(it) }, { editors.remove(it) })
            return editors
        }
        finally {
            lifetime.terminate()
        }
    }

    private fun refreshUnityEditorProcesses(
        project: Project,
        onEditorAdded: (UnityDebugTarget) -> Unit,
        onEditorRemoved: (UnityDebugTarget) -> Unit
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
                logger.trace {
                    val pid = (it as? UnityLocalProcess)?.processId ?: 0
                    "Removing old Unity editor ${it.name}:$pid"
                }
                onEditorRemoved(it)
            }
        }

        val newProcesses = processes.filter { !editorProcesses.containsKey(it.pid) }
        val unityProcessInfoMap = UnityRunUtil.getAllUnityProcessInfo(newProcesses, project)
        newProcesses.forEach { processInfo ->
            val unityProcessInfo = unityProcessInfoMap[processInfo.pid]
            val editorProcess = processInfo.toUnityDebugTarget(unityProcessInfo)

            logger.trace {
                val pid = (editorProcess as? UnityLocalProcess)?.processId ?: 0
                "Adding Unity editor process ${editorProcess.name}:$pid"
            }

            editorProcesses[processInfo.pid] = editorProcess
            onEditorAdded(editorProcess)
        }

        if (logger.isTraceEnabled) {
            val duration = System.currentTimeMillis() - start
            logger.trace("Finished refreshing local editor processes. Took ${duration}ms")
        }
    }
}
