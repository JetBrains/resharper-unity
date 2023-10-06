package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.process.OSProcessUtil
import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.project.Project
import com.intellij.util.application
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.ViewableList
import com.jetbrains.rider.plugins.unity.run.UnityLocalProcess
import com.jetbrains.rider.plugins.unity.run.UnityRunUtil
import com.jetbrains.rider.plugins.unity.run.toUnityProcess
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJson
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJsonStatus
import com.jetbrains.rider.projectView.solutionDirectory

class UnityAttachToEditorViewModel(val lifetime: Lifetime, private val project: Project) {

    val editorInstanceJsonStatus: IProperty<EditorInstanceJsonStatus?> = Property(null)
    val editorProcesses: ViewableList<UnityLocalProcess> = ViewableList()
    val pid: IProperty<Int?> = Property(null)
    private val editorInstanceJson = EditorInstanceJson.getInstance(project)

    init {
        refreshProcessList()
    }

    fun refreshProcessList() {
        editorProcesses.clear()

        application.executeOnPooledThread {
            val processList = OSProcessUtil.getProcessList()
            val editors = getEditorProcessInfos(processList)

            application.invokeLater({
                editorProcesses.addAll(editors)
                editorInstanceJsonStatus.set(editorInstanceJson.validateStatus(processList))
                pid.value = if (editorInstanceJsonStatus.value != EditorInstanceJsonStatus.Valid && editors.count() == 1) {
                    editors[0].pid
                } else if (editorInstanceJson.status == EditorInstanceJsonStatus.Valid) {
                    editorInstanceJson.contents?.process_id
                } else {
                    // If we're a class library project in the same folder as a Unity project, we can still guess the name
                    editors.firstOrNull { project.solutionDirectory.name.equals(it.projectName, true) }?.pid
                }
            }, ModalityState.any())
        }
    }

    private fun getEditorProcessInfos(processList: Array<ProcessInfo>): List<UnityLocalProcess> {
        val unityProcesses = processList.filter { UnityRunUtil.isUnityEditorProcess(it) }
        val unityProcessInfoMap = UnityRunUtil.getAllUnityProcessInfo(unityProcesses, project)
        return unityProcesses.map {
            it.toUnityProcess(unityProcessInfoMap[it.pid])
        }
    }
}