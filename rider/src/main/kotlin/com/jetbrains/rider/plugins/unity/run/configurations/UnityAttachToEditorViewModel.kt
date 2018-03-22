package com.jetbrains.rider.plugins.unity.run.configurations

import com.google.gson.JsonParser
import com.intellij.execution.process.OSProcessUtil
import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.util.attach.UnityProcessUtil
import com.jetbrains.rider.use2
import com.jetbrains.rider.util.lifetime.Lifetime
import com.jetbrains.rider.util.reactive.IProperty
import com.jetbrains.rider.util.reactive.Property
import com.jetbrains.rider.util.reactive.ViewableList

class UnityAttachToEditorViewModel(val lifetime: Lifetime, project: Project) {
    val editorInstanceJsonStatus: EditorInstanceJsonStatus
    val editorProcesses: ViewableList<EditorProcessInfo> = ViewableList()
    val pid: IProperty<Int?> = Property(null)
    val logger = Logger.getInstance(UnityAttachToEditorViewModel::class.java)

    data class EditorProcessInfo(val name: String, val pid: Int?)
    data class EditorInstanceJsonResult(val status: EditorInstanceJsonStatus, val pid: Int?)

    init {
        val processList = OSProcessUtil.getProcessList()
        updateProcessList(processList)

        var (status, pid) = readEditorInstanceJson(project, processList)
        if (status != EditorInstanceJsonStatus.Valid && editorProcesses.count() == 1) {
            pid = editorProcesses[0].pid
        }

        editorInstanceJsonStatus = status
        this.pid.value = pid
    }

    private fun updateProcessList(processList: Array<out ProcessInfo>) {
        processList.forEach {
            if (UnityProcessUtil.isUnityEditorProcess(it))
                editorProcesses.add(EditorProcessInfo(it.executableName, it.pid))
        }
    }

    private fun readEditorInstanceJson(project: Project, processList: Array<ProcessInfo>): EditorInstanceJsonResult {
        val editorInstanceJsonFile = project.baseDir.findFileByRelativePath("Library/EditorInstance.json")
        if (editorInstanceJsonFile == null || !editorInstanceJsonFile.exists()) {
            return EditorInstanceJsonResult(EditorInstanceJsonStatus.Missing, null)
        }

        try {
            val processId = editorInstanceJsonFile.inputStream.reader().use2 { reader ->
                val jsonObject = JsonParser().parse(reader).asJsonObject
                return@use2 jsonObject["process_id"].asInt
            }

            return if (checkValidEditorInstance(processId, processList))
                EditorInstanceJsonResult(EditorInstanceJsonStatus.Valid, processId)
            else
                EditorInstanceJsonResult(EditorInstanceJsonStatus.Outdated, null)
        }
        catch(ex: Exception) {
            return EditorInstanceJsonResult(EditorInstanceJsonStatus.Error, null)
        }
    }

    private fun checkValidEditorInstance(pid: Int, processList: Array<ProcessInfo>): Boolean {
        // Look for processes, if it exists and has the correct name, return it unchanged,
        // else return invalidValue. Do not throw, as we'll attempt to recover
        return processList.any { it.pid == pid && UnityProcessUtil.isUnityEditorProcess(it) }
    }

    fun updateProcessList() {
        editorProcesses.clear()
        val processList = OSProcessUtil.getProcessList()
        updateProcessList(processList)
    }
}