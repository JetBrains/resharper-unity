package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.process.OSProcessUtil
import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.ViewableList
import com.jetbrains.rider.plugins.unity.run.UnityRunUtil
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJson
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJsonStatus

class UnityAttachToEditorViewModel(val lifetime: Lifetime, project: Project) {

    val editorInstanceJsonStatus: EditorInstanceJsonStatus
    val editorProcesses: ViewableList<EditorProcessInfo> = ViewableList()
    val pid: IProperty<Int?> = Property(null)
    val isUserSelectedPid : Property<Boolean> = Property(false)
    data class EditorProcessInfo(val name: String, val pid: Int?)

    init {
        val processList = OSProcessUtil.getProcessList()
        updateProcessList(processList)

        val (status, editorInstanceJson) = readEditorInstanceJson(project, processList)
        editorInstanceJsonStatus = status
        this.pid.value = if (status != EditorInstanceJsonStatus.Valid && editorProcesses.count() == 1) {
            editorProcesses[0].pid
        }
        else {
            editorInstanceJson?.process_id
        }
    }

    private fun updateProcessList(processList: Array<out ProcessInfo>) {
        processList.forEach {
            if (UnityRunUtil.isUnityEditorProcess(it))
                editorProcesses.add(EditorProcessInfo(it.executableName, it.pid))
        }
    }

    private fun readEditorInstanceJson(project: Project, processList: Array<ProcessInfo>): Pair<EditorInstanceJsonStatus, EditorInstanceJson?> {
        val editorInstanceJson = EditorInstanceJson.load(project)
        if (editorInstanceJson.first == EditorInstanceJsonStatus.Valid && !isValidProcessId(editorInstanceJson.second!!, processList)) {
            return Pair(EditorInstanceJsonStatus.Outdated, editorInstanceJson.second)
        }
        return editorInstanceJson
    }

    private fun isValidProcessId(editorInstanceJson: EditorInstanceJson, processList: Array<ProcessInfo>): Boolean {
        // Look for processes, if it exists and has the correct name, return it unchanged,
        // else return invalidValue. Do not throw, as we'll attempt to recover
        return processList.any { it.pid == editorInstanceJson.process_id && UnityRunUtil.isUnityEditorProcess(it) }
    }

    fun updateProcessList() {
        editorProcesses.clear()
        val processList = OSProcessUtil.getProcessList()
        updateProcessList(processList)
    }
}