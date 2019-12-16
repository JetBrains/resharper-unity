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
import com.intellij.util.xmlb.annotations.Transient
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
    @Transient override var port: Int = -1
    @Transient override var address: String = "127.0.0.1"
    @Transient var pid: Int? = null

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

    override fun checkSettingsBeforeRun() {
        // This method lets us check settings before run. If we throw an instance of RuntimeConfigurationError, the Run
        // Configuration editor is displayed. It's called on the EDT, so theres' not a lot we can do - e.g. we can't get
        // a process list.

        // If we already have a pid, that means this run configuration has been launched before, and we've successfully
        // attached to a process. Use it again.
        if (pid != null) {
            return
        }

        // Verify that we have an EditorInstance.json. If we don't, we can't easily tell that any running instances are
        // for our project.
        val editorInstanceJson = EditorInstanceJson.getInstance(project)
        if (editorInstanceJson.status != EditorInstanceJsonStatus.Valid) {
            throw RuntimeConfigurationError("Unable to automatically discover correct Unity Editor to debug")
        }
    }

    fun updatePidAndPort() : Boolean {

        val processList = OSProcessUtil.getProcessList()

        // Try to reuse the previous process ID, if it's still valid, then fall back to finding the process
        // automatically. Theoretically, there is a tiny chance the previous process has died, and the process ID has
        // been recycled for a new process that just happens to be a Unity process. Practically, this is not likely
        port = -1
        pid = checkValidEditorInstance(pid, processList) ?: findUnityEditorInstanceFromEditorInstanceJson(processList)
        if (pid == null) {
            return false
        }
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

    override fun readExternal(element: Element) {
        super.readExternal(element)
        // Reset pid, address + port to defaults. It makes no sense to persist the pid across sessions. Unfortunately,
        // the base class has been serialising them for years...
        pid = null
        port = -1
        address = "127.0.0.1"
    }

    override fun writeExternal(element: Element) {
        super.writeExternal(element)
        // Write it, but don't read it. We need to write it so that the modified check works, but we're not interested
        // in reading it as we will recalculate it.
        // TODO: Explain the comment above - what modified check?
        if (pid != null) {
            element.setAttribute("ignored-value-for-modified-check", pid.toString())
        }
    }
}

