package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.CantRunException
import com.intellij.execution.Executor
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.configurations.RuntimeConfigurationError
import com.intellij.execution.configurations.WithoutOwnBeforeRunSteps
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.process.OSProcessUtil
import com.intellij.execution.process.ProcessInfo
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.RunConfigurationWithSuppressedDefaultRunAction
import com.intellij.openapi.extensions.ExtensionPointName
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.progress.ProgressIndicator
import com.intellij.openapi.progress.Task
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.hasTrueValue
import com.jetbrains.rdclient.util.idea.pumpMessages
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.run.attach.UnityRunUtil
import com.jetbrains.rider.plugins.unity.util.*
import com.jetbrains.rider.run.configurations.remote.DotNetRemoteConfiguration
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import com.jetbrains.rider.run.configurations.unity.UnityAttachConfigurationExtension
import org.jdom.Element
import org.jetbrains.concurrency.AsyncPromise

class UnityAttachToEditorRunConfiguration(project: Project, factory: UnityAttachToEditorFactory, val play: Boolean = false)
    : DotNetRemoteConfiguration(project, factory, "Attach To Unity Editor"),
    RunConfigurationWithSuppressedDefaultRunAction,
    RemoteConfiguration,
    WithoutOwnBeforeRunSteps {

    // TEMP, will be removed in 19.2
    companion object {
        val EP_NAME = ExtensionPointName<UnityAttachConfigurationExtension>("com.intellij.resharper.unity.unityAttachConfiguration")
        private const val WAIT_FOR_PROCESSES_TIMEOUT = 10000L
    }

    // Note that we don't serialise these - they will change between sessions, possibly during a session
    override var port: Int = -1
    override var address: String = "127.0.0.1"
    var pid: Int? = null
    var isUserSelectedPid = false

    override fun clone(): RunConfiguration {
        val configuration = super.clone() as UnityAttachToEditorRunConfiguration
        configuration.pid = pid
        configuration.isUserSelectedPid = isUserSelectedPid
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
        val model = UnityHost.getInstance(project).model
        // We could do this in getState, but if we throw an error there, it just shows a balloon
        // If we throw an error here (at least, RuntimeConfigurationError), it will cause the
        // Edit Run Configurations dialog to be shown
        if (!updatePidAndPort() && (UnityInstallationFinder.getInstance(project).getApplicationPath() == null ||
                model.hasUnityReference.hasTrueValue && !UnityProjectDiscoverer.getInstance(project).isUnityProjectFolder))
            throw RuntimeConfigurationError("Cannot automatically determine Unity Editor instance. Please open the project in Unity and try again.")
    }

    private fun updatePidAndPort() : Boolean {

        val processList = getProcesses()

        // We might have a pid from a previous run, but the editor might have died
        pid = if (isUserSelectedPid) {
            checkValidEditorInstance(pid, processList) ?: findUnityEditorInstance(processList)
        } else {
            findUnityEditorInstance(processList)
        }

        if (pid == null)
            return false

        port = convertPidToDebuggerPort(pid!!)
        return true
    }

    //get processes on background thread with progress
    private fun getProcesses(): Array<ProcessInfo> {
        val promise = AsyncPromise<Array<ProcessInfo>>()

        object : Task.Backgroundable(project, "Getting list of processes...") {
            override fun run(p0: ProgressIndicator) {
                try {
                    val processes = OSProcessUtil.getProcessList()
                    promise.setResult(processes)
                } catch (t: Throwable) {
                    promise.setError(t)
                }
            }

        }.queue()

        if (!pumpMessages(WAIT_FOR_PROCESSES_TIMEOUT) {
                promise.isDone
            }) {
            throw CantRunException("Failed to fetch list of processes.")
        }

        return promise.blockingGet(WAIT_FOR_PROCESSES_TIMEOUT.toInt())
            ?: throw CantRunException("Failed to fetch list of processes.")
    }

    private fun findUnityEditorInstance(processList: Array<ProcessInfo>): Int? {
        isUserSelectedPid = false
        return findUnityEditorInstanceFromEditorInstanceJson(processList)
            ?: findUnityEditorInstanceFromProcesses(processList)
    }

    private fun findUnityEditorInstanceFromEditorInstanceJson(processList: Array<ProcessInfo>): Int? {
        val (status, editorInstanceJson) = EditorInstanceJson.load(project)
        if (status == EditorInstanceJsonStatus.Valid && editorInstanceJson != null) {
            return checkValidEditorInstance(editorInstanceJson.process_id, processList)
        }

        return null
    }

    private fun checkValidEditorInstance(pid: Int?, processList: Array<ProcessInfo>): Int? {
        if (pid != null) {
            // Look for processes, if it exists and has the correct name, return it unchanged,
            // else return invalidValue. Do not throw, as we'll attempt to recover
            if (processList.any { it.pid == pid && UnityRunUtil.isUnityEditorProcess(it) })
                return pid
        }
        return null
    }

    private fun findUnityEditorInstanceFromProcesses(processList: Array<ProcessInfo>): Int? {

        val pids = processList.filter { UnityRunUtil.isUnityEditorProcess(it) }
            .map { it.pid }

        if (pids.isEmpty()) {
            return null
        } else if (pids.size > 1) {
            throw RuntimeConfigurationError("Cannot automatically determine Unity Editor instance. Please select from the list or open the project in Unity.")
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

