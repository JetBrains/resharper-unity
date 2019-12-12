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
import com.jetbrains.rider.util.idea.application

class UnityAttachToEditorViewModel(val lifetime: Lifetime, private val project: Project) {

    data class EditorProcessInfo(val name: String, val pid: Int?, val projectName: String?)

    val editorInstanceJsonStatus: IProperty<EditorInstanceJsonStatus?> = Property(null)
    val editorProcesses: ViewableList<EditorProcessInfo> = ViewableList()
    val pid: IProperty<Int?> = Property(null)
    private val editorInstanceJson = EditorInstanceJson.getInstance(project)

    init {
        refreshProcessList()
    }

    fun refreshProcessList() {
        editorProcesses.clear()
        val currentModalityState = application.currentModalityState

        application.executeOnPooledThread {
            val processList = OSProcessUtil.getProcessList()
            val editors = getEditorProcessInfos(processList)

            application.invokeLater({ editors.forEach { editorProcesses.add(it) } }, currentModalityState)

            editorInstanceJsonStatus.set(editorInstanceJson.validateStatus(processList))

            this.pid.value = if (editorInstanceJsonStatus.value != EditorInstanceJsonStatus.Valid && editorProcesses.count() == 1) {
                editorProcesses[0].pid
            } else {
                editorInstanceJson.contents?.process_id
            }
        }
    }

    private fun getEditorProcessInfos(processList: Array<ProcessInfo>): List<EditorProcessInfo> {
        val unityProcesses = processList.filter { UnityRunUtil.isUnityEditorProcess(it) }
        val unityProcessInfoMap = UnityRunUtil.getAllUnityProcessInfo(unityProcesses, project)
        return unityProcesses.map {
            EditorProcessInfo(it.executableName, it.pid, unityProcessInfoMap[it.pid]?.projectName)
        }
    }
}