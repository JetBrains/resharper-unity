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

class UnityAttachToEditorViewModel(val lifetime: Lifetime, private val project: Project) {

    data class EditorProcessInfo(val name: String, val pid: Int?, val projectName: String?)

    val editorInstanceJsonStatus: EditorInstanceJsonStatus
    val editorProcesses: ViewableList<EditorProcessInfo> = ViewableList()
    val pid: IProperty<Int?> = Property(null)
    val isUserSelectedPid : Property<Boolean> = Property(false)
    private val editorInstanceJson = EditorInstanceJson.getInstance(project)

    init {
        val processList = OSProcessUtil.getProcessList()
        updateProcessList(processList)

        editorInstanceJsonStatus = editorInstanceJson.validateStatus(processList)

        this.pid.value = if (editorInstanceJsonStatus != EditorInstanceJsonStatus.Valid && editorProcesses.count() == 1) {
            editorProcesses[0].pid
        }
        else {
            editorInstanceJson.contents?.process_id
        }
    }

    private fun updateProcessList(processList: Array<ProcessInfo>) {
        val unityProcesses = processList.filter { UnityRunUtil.isUnityEditorProcess(it) }
        val projectNames = UnityRunUtil.getUnityProcessProjectNames(unityProcesses, project)
        unityProcesses.forEach {
            editorProcesses.add(EditorProcessInfo(it.executableName, it.pid, projectNames[it.pid]))
        }
    }

    fun refreshProcessList() {
        editorProcesses.clear()
        val processList = OSProcessUtil.getProcessList()
        updateProcessList(processList)
    }
}