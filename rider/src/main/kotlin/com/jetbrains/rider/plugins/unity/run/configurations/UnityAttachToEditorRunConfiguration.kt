package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.Executor
import com.intellij.execution.configurations.*
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.process.OSProcessUtil
import com.intellij.execution.process.ProcessInfo
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.RunConfigurationWithSuppressedDefaultRunAction
import com.intellij.openapi.extensions.ExtensionPointName
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.run.UnityRunUtil
import com.jetbrains.rider.plugins.unity.util.*
import com.jetbrains.rider.run.configurations.remote.DotNetRemoteConfiguration
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import com.jetbrains.rider.run.configurations.unity.UnityAttachConfigurationExtension
import org.jdom.Element

class UnityAttachToEditorRunConfiguration(project: Project, factory: ConfigurationFactory, val play: Boolean = false)
    : DotNetRemoteConfiguration(project, factory, "Attach To Unity Editor"),
    RunConfigurationWithSuppressedDefaultRunAction,
    RemoteConfiguration,
    WithoutOwnBeforeRunSteps {

    // TEMP, will be removed in 19.2
    companion object {
        val EP_NAME = ExtensionPointName<UnityAttachConfigurationExtension>("com.intellij.resharper.unity.unityAttachConfiguration")
    }

    // Note that we don't serialise these - they will change between sessions, possibly during a session
    override var port: Int = -1
    override var address: String = "127.0.0.1"
    var pid: Int? = null

    override fun clone(): RunConfiguration {
        val configuration = super.clone() as UnityAttachToEditorRunConfiguration
        configuration.pid = pid
        return configuration
    }

    override fun hideDisabledExecutorButtons(): Boolean {
        return true
    }

    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration> = UnityAttachToEditorSettingsEditor(project)

    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState? {
        val executorId = executor.id

        for (ext in EP_NAME.getExtensions(project)) {
            if (ext.canExecute(executorId)) {
                val finder = UnityInstallationFinder.getInstance(project)
                val args = getUnityWithProjectArgs(project)
                if (play) {
                    addPlayModeArguments(args)
                }

                return ext.executor(UnityAttachConfigurationParametersImpl(pid, finder.getApplicationPath(), args, finder.getApplicationVersion()), environment)
            }
        }

        if (executorId == DefaultDebugExecutor.EXECUTOR_ID)
            return UnityAttachToEditorProfileState(this, environment)

        return null
    }

    override var listenPortForConnections: Boolean = false

    fun updatePidAndPort() : Boolean {

        val processList = OSProcessUtil.getProcessList()

        // Try to reuse the previous process ID, if it's still valid, then fall back to finding the process
        // automatically. Theoretically, there is a tiny chance the previous process has died, and the process ID has
        // been recycled for a new process that just happens to be a Unity process. Practically, this is not likely
        pid = checkValidEditorInstance(pid, processList) ?: findUnityEditorInstanceFromEditorInstanceJson(processList) ?: return false
        port = convertPidToDebuggerPort(pid!!)
        return true
    }

    private fun findUnityEditorInstanceFromEditorInstanceJson(processList: Array<ProcessInfo>): Int? {
        val editorInstanceJson = EditorInstanceJson.getInstance(project)
        if (editorInstanceJson.validateStatus(processList) == EditorInstanceJsonStatus.Valid) {
            return editorInstanceJson.contents!!.process_id
        }

        return null
    }

    private fun checkValidEditorInstance(pid: Int?, processList: Array<ProcessInfo>): Int? {
        if (pid != null && UnityRunUtil.isValidUnityEditorProcess(pid, processList)) {
            return pid
        }
        return null
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

