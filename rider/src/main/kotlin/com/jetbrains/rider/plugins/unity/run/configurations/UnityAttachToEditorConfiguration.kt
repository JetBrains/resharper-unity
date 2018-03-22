package com.jetbrains.rider.plugins.unity.run.configurations

import com.google.gson.JsonParser
import com.intellij.execution.Executor
import com.intellij.execution.configurations.*
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.process.OSProcessUtil
import com.intellij.execution.process.ProcessInfo
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.RunConfigurationWithSuppressedDefaultRunAction
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.util.convertPortToDebuggerPort
import com.jetbrains.rider.run.configurations.remote.DotNetRemoteConfiguration
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import com.jetbrains.rider.plugins.unity.util.attach.UnityProcessUtil
import com.jetbrains.rider.use2
import org.apache.commons.logging.LogFactory
import org.jdom.Element

class UnityAttachToEditorConfiguration(project: Project, factory: UnityAttachToEditorFactory, val play: Boolean = false)
    : DotNetRemoteConfiguration(project, factory, "Attach To Unity Editor"),
        RunConfigurationWithSuppressedDefaultRunAction,
        RemoteConfiguration,
        WithoutOwnBeforeRunSteps {

    // Note that we don't serialise these - they will change between sessions, possibly during a session
    override var port: Int = -1
    override var address: String = "127.0.0.1"
    var pid: Int? = null

    override fun clone(): RunConfiguration {
        val configuration = super.clone() as UnityAttachToEditorConfiguration
        configuration.pid = pid
        return configuration
    }

    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration> = UnityAttachToEditorSettingsEditor(project)

    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState? {
        if (executor.id != DefaultDebugExecutor.EXECUTOR_ID)
            return null
        return UnityAttachToPlayerProfileState(this, environment)
    }

    override var listenPortForConnections: Boolean = false

    override fun checkSettingsBeforeRun() {

        // We could do this in getState, but if we throw an error there, it just shows a balloon
        // If we throw an error here (at least, RuntimeConfigurationError), it will cause the
        // Edit Run Configurations dialog to be shown

        val processList = OSProcessUtil.getProcessList()

        // We might have a pid from a previous run, but the editor might have died
        pid = checkValidEditorInstance(pid, processList)
                ?: findUnityEditorInstance(processList)
                ?: throw RuntimeConfigurationError("Cannot find Unity Editor instance")

        port = convertPortToDebuggerPort(pid!!)
    }

    private fun checkValidEditorInstance(pid: Int?, processList: Array<ProcessInfo>): Int? {
        if (pid != null) {
            // Look for processes, if it exists and has the correct name, return it unchanged,
            // else return invalidValue. Do not throw, as we'll attempt to recover
            if (processList.any { it.pid == pid && UnityProcessUtil.isUnityEditorProcess(it) })
                return pid
        }
        return null
    }

    private fun findUnityEditorInstance(processList: Array<ProcessInfo>): Int? {
        return findUnityEditorInstanceFromEditorInstanceJson(processList)
                ?: findUnityEditorInstanceFromProcesses(processList)
    }

    private fun findUnityEditorInstanceFromEditorInstanceJson(processList: Array<ProcessInfo>): Int? {
        project.baseDir.findFileByRelativePath("Library/EditorInstance.json")?.let { file ->
            try {
                // Not a RuntimeConfigurationError, mainly because we can recover
                return file.inputStream.reader().use2 { reader ->
                    val jsonObject = JsonParser().parse(reader).asJsonObject
                    val processId = jsonObject["process_id"].asInt

                    return@use2 checkValidEditorInstance(processId, processList)
                }
            } catch (e: Throwable) {
                LogFactory.getLog("catch").warn("Error reading EditorInstance.json", e)
            }
        }

        return null
    }

    private fun findUnityEditorInstanceFromProcesses(processList: Array<ProcessInfo>): Int {

        val pids = processList.filter { UnityProcessUtil.isUnityEditorProcess(it) }
                .map { it.pid }

        if (pids.isEmpty()) {
            throw RuntimeConfigurationError("No Unity Editor instances found")
        } else if (pids.size > 1) {
            throw RuntimeConfigurationError("Multiple Unity Editor instances found")
        }

        return pids[0]
    }

    override fun checkConfiguration() {
        // Too expensive to check here?
    }

    override fun writeExternal(element: Element) {
        super.writeExternal(element)
        // Write it, but don't read it. We need to write it so that the modified check
        // works, but we're not interested in reading it as we will recalculate it
        if (pid != null) {
            element.setAttribute("pid", pid.toString())
        }
    }
}

