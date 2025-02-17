package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.Executor
import com.intellij.execution.configurations.*
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.process.OSProcessUtil
import com.intellij.execution.process.ProcessInfo
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.execution.runners.RunConfigurationWithSuppressedDefaultRunAction
import com.intellij.openapi.extensions.ExtensionPointName
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.intellij.util.xmlb.annotations.Transient
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.debugger.DotNetDebugRunner
import com.jetbrains.rider.debugger.IMixedModeDebugAwareRunConfiguration
import com.jetbrains.rider.debugger.attach.util.getAvailableRuntimes
import com.jetbrains.rider.debugger.mixed.mode.isMixedModeDebugFeatureEnabled
import com.jetbrains.rider.model.ProcessRuntimeInformation
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.isUnityProjectFolder
import com.jetbrains.rider.plugins.unity.model.UnityEditorState
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.UnityRunUtil
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfigurationType
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeDebugProfileState
import com.jetbrains.rider.plugins.unity.util.*
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.run.configurations.exe.ExeConfigurationParameters
import com.jetbrains.rider.run.configurations.remote.DotNetRemoteConfiguration
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import com.jetbrains.rider.run.configurations.unity.UnityAttachConfigurationExtension
import com.jetbrains.rider.run.configurations.unity.UnityAttachRunConfiguration
import kotlinx.coroutines.ExperimentalCoroutinesApi
import org.jdom.Element

class UnityAttachToEditorRunConfiguration(project: Project, factory: ConfigurationFactory, val play: Boolean = false)
    : DotNetRemoteConfiguration(project, factory, "Attach To Unity Editor"),
      RunConfigurationWithSuppressedDefaultRunAction,
      RemoteConfiguration,
      WithoutOwnBeforeRunSteps,
      UnityAttachRunConfiguration,
      IMixedModeDebugAwareRunConfiguration {

    // TEMP, will be removed in 19.2
    companion object {
        val EP_NAME = ExtensionPointName<UnityAttachConfigurationExtension>("com.intellij.resharper.unity.unityAttachConfiguration")
    }

    // Note that we don't serialise these - they will change between sessions, possibly during a session
    // TODO: We don't serialise these properties, but the base classes does serialise its own "address" and "port"
    @Transient
    override var port: Int = -1

    @Transient
    override var address: String = "127.0.0.1"

    @Transient
    var pid: Int? = null

    var useMixedMode : Boolean = false

    var runtimes:List<ProcessRuntimeInformation> = emptyList()

    @Transient
    override var listenPortForConnections: Boolean = false

    override fun clone(): RunConfiguration {
        val configuration = super.clone() as UnityAttachToEditorRunConfiguration
        configuration.pid = pid
        configuration.useMixedMode = useMixedMode
        return configuration
    }

    override fun hideDisabledExecutorButtons() = true

    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration> = UnityAttachToEditorSettingsEditor(project)

    override fun getUnityEditorPid(): Int? = pid

    @OptIn(ExperimentalCoroutinesApi::class)
    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState? {
        val executorId = executor.id
        for (ext in EP_NAME.getExtensions(project)) {
            if (ext.canExecute(executorId)) {
                val finder = UnityInstallationFinder.getInstance(project)
                val args = getUnityArgs(project).withProjectPath(project).withRiderPath()
                if (play) {
                    addPlayModeArguments(args)
                }

                // when the process is disconnected, we would not be able to call startProfiling anyway
                val processId = if (project.solution.frontendBackendModel.unityEditorState.valueOrDefault(
                        UnityEditorState.Disconnected) != UnityEditorState.Disconnected)
                    project.solution.frontendBackendModel.unityApplicationData.valueOrNull?.unityProcessId ?: pid
                else null

                val res = ext.executor(UnityAttachConfigurationParametersImpl(processId,
                                                                              finder.getApplicationExecutablePath(), args,
                                                                              finder.getApplicationVersion()), environment) { _, _, _ ->
                    run {
                        if (executorId == "dotTrace Profiler") {
                            project.solution.frontendBackendModel.startProfiling.start(UnityProjectLifetimeService.getLifetime(project),
                                                                                       play)
                        }
                    }
                }

                return res
            }
        }

        if (executorId == DefaultDebugExecutor.EXECUTOR_ID) {
            val params = ExeConfigurationParameters(
                exePath = UnityInstallationFinder.getInstance(project).getApplicationExecutablePath().toString(),
                programParameters = mutableListOf<String>().withProjectPath(
                    project).withDebugCodeOptimization().withRiderPath().toProgramParameters(),
                workingDirectory = project.solutionDirectory.canonicalPath,
                envs = hashMapOf(),
                isPassParentEnvs = true,
                useExternalConsole = false
            )
            val exeConfiguration = UnityExeConfiguration(name, project,
                                                         ConfigurationTypeUtil.findConfigurationType(
                                                             UnityExeConfigurationType::class.java).factory, params)
            return UnityAttachToEditorProfileState(
                UnityExeDebugProfileState(exeConfiguration, this, environment, true), this, environment)
        }
        return null
    }

    override fun checkRunnerSettings(
        runner: ProgramRunner<*>,
        runnerSettings: RunnerSettings?,
        configurationPerRunnerSettings: ConfigurationPerRunnerSettings?
    ) {
        if (runner is DotNetDebugRunner) {
            // This method lets us check settings before run. If we throw an instance of RuntimeConfigurationError, the Run
            // Configuration editor is displayed. It's called on the EDT, so there's not a lot we can do - e.g. we can't get
            // a process list.

            // If we already have a pid, that means this run configuration has been launched before, and we've successfully
            // attached to a process. Use it again. If the pid is out of date (highly unlikely), we'll do our best to find
            // the process again
            if (pid != null) {
                return
            }

            // If we're a class library project that isn't in a Unity project folder, we can't guess at the correct project
            // to attach to, so throw an error and show the dialog. This value will be null until the backend has finished
            // loading. However, because we're a Unity run configuration, we can safely assume we're a Unity project, and if
            // we're not inside a Unity project folder, then we can't automatically attach, so throw an error and show the
            // dialog

            if (!project.isUnityProjectFolder.value) {
                throw RuntimeConfigurationError(
                    UnityBundle.message("dialog.message.unable.to.automatically.discover.correct.unity.editor.to.debug"))
            }
        }
        super.checkRunnerSettings(runner, runnerSettings, configurationPerRunnerSettings)
    }

    suspend fun updatePidAndPort(): Boolean {

        val processList = OSProcessUtil.getProcessList()

        port = -1

        try {
            // Try to reuse the previously attached process ID, if it's still valid. If we don't have a previous pid, or
            // the process is no longer valid, try to find the best match, via EditorInstance.json or project name.
            pid = checkValidEditorProcess(pid, processList)
                  ?: findUnityEditorProcessFromEditorInstanceJson(processList)
                  ?: findUnityEditorProcessFromProjectName(processList)
            if (pid == null) {
                return false
            }
            val processInfo = processList.firstOrNull { it.pid == pid } ?: return false
            runtimes = getAvailableRuntimes(processInfo, project)

            port = convertPidToDebuggerPort(pid!!)
            return true
        }
        catch (t: Throwable) {
            pid = null
            throw t
        }
    }

    private fun checkValidEditorProcess(pid: Int?, processList: Array<ProcessInfo>): Int? {
        if (pid != null && UnityRunUtil.isValidUnityEditorProcess(pid, processList)) {
            return pid
        }
        return null
    }

    private fun findUnityEditorProcessFromEditorInstanceJson(processList: Array<ProcessInfo>): Int? {
        val editorInstanceJson = EditorInstanceJson.getInstance(project)
        if (editorInstanceJson.validateStatus(processList) == EditorInstanceJsonStatus.Valid) {
            return editorInstanceJson.contents!!.process_id
        }

        return null
    }

    private fun findUnityEditorProcessFromProjectName(processList: Array<ProcessInfo>): Int? {
        // This only works if we can figure out the project name for a running process. This might not succeed on
        // Windows, if the process is started without appropriate command line args.
        val unityProcesses = processList.filter { UnityRunUtil.isUnityEditorProcess(it) }
        val map = UnityRunUtil.getAllUnityProcessInfo(unityProcesses, project)

        // If we're a generated project, or a class library project that lives in the root of a Unity project alongside
        // a generated project, we can use the project dir as the expected project name.
        if (project.isUnityProject.value) {
            val expectedProjectName = project.solutionDirectory.name
            val entry = map.entries.firstOrNull { expectedProjectName.equals(it.value.projectName, true) }
            if (entry != null) {
                return entry.key
            }

            // We don't have a cached pid from a previous debug session, we don't have EditorInstance.json, we can't
            // find a process with a matching project name. Best guess fallback is to attach to an unnamed project
            val noNameProjects = map.entries.filter { it.value.projectName == null }
            if (noNameProjects.count() == 1) {
                return noNameProjects[0].key
            }

            return null
        }
        else {
            // We're a class library project in a standalone directory. We can't guess the project name, and it's best
            // not to attach to a random editor
            throw RuntimeConfigurationError(
                UnityBundle.message("dialog.message.unable.to.automatically.discover.correct.unity.editor.to.debug"))
        }
    }

    override fun readExternal(element: Element) {
        super.readExternal(element)
        // Reset pid, address + port to defaults. It makes no sense to persist the pid across sessions. Unfortunately,
        // the base class has been serialising them for years...
        pid = null
        port = -1
        address = "127.0.0.1"
        listenPortForConnections = false
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

    override fun useMixedDebugMode(): Boolean = isMixedModeDebugFeatureEnabled() && useMixedMode
}

