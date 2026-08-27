package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.Executor
import com.intellij.execution.configurations.ConfigurationFactory
import com.intellij.execution.configurations.ConfigurationPerRunnerSettings
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.configurations.RunnerSettings
import com.intellij.execution.configurations.RuntimeConfigurationError
import com.intellij.execution.configurations.WithoutOwnBeforeRunSteps
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.execution.runners.RunConfigurationWithSuppressedDefaultRunAction
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.extensions.ExtensionPointName
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.intellij.util.application
import com.intellij.util.xmlb.annotations.Transient
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.debugger.RiderDebugRunner
import com.jetbrains.rider.debugger.attach.processes.MsClrAttachableProcessesHost
import com.jetbrains.rider.model.GenericCoreClrRuntime
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.isUnityProjectFolder
import com.jetbrains.rider.plugins.unity.model.UnityEditorState
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfigurationType
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeDebugProfileState
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJson
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJsonStatus
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.util.addPlayModeArguments
import com.jetbrains.rider.plugins.unity.util.convertPidToDebuggerPort
import com.jetbrains.rider.plugins.unity.util.getUnityArgs
import com.jetbrains.rider.plugins.unity.util.toProgramParameters
import com.jetbrains.rider.plugins.unity.util.withDebugCodeOptimization
import com.jetbrains.rider.plugins.unity.util.withProjectPath
import com.jetbrains.rider.plugins.unity.util.withRiderPath
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.run.configurations.exe.ExeConfigurationParameters
import com.jetbrains.rider.run.configurations.remote.DotNetRemoteConfiguration
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import com.jetbrains.rider.run.configurations.unity.UnityAttachConfigurationExtension
import com.jetbrains.rider.run.configurations.unity.UnityAttachRunConfiguration
import org.jdom.Element

class UnityAttachToEditorRunConfiguration(project: Project, factory: ConfigurationFactory, val play: Boolean = false)
    : DotNetRemoteConfiguration(project, factory, "Attach To Unity Editor"),
      RunConfigurationWithSuppressedDefaultRunAction,
      RemoteConfiguration,
      WithoutOwnBeforeRunSteps,
      UnityAttachRunConfiguration {

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

    @Transient
    var isCoreClr = false

    @Transient
    override var listenPortForConnections: Boolean = false

    override fun clone(): RunConfiguration {
        val configuration = super.clone() as UnityAttachToEditorRunConfiguration
        configuration.pid = pid
        configuration.useMixedMode = useMixedMode
        return configuration
    }

    override fun hideDisabledExecutorButtons(): Boolean = true

    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration> = UnityAttachToEditorSettingsEditor(project)

    override fun getUnityEditorPid(): Int? = pid

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
                programParameters = mutableListOf<String>()
                    .withProjectPath(project)
                    .withDebugCodeOptimization()
                    .withRiderPath()
                    .toProgramParameters(),
                workingDirectory = project.solutionDirectory.canonicalPath,
                envs = hashMapOf(),
                isPassParentEnvs = true,
                mixedModeDebugging = false // false by default
            )
            val exeConfigurationFactory = ConfigurationTypeUtil.findConfigurationType(UnityExeConfigurationType::class.java).factory
            val exeConfiguration = UnityExeConfiguration(name, project, exeConfigurationFactory, params, isEditor = true)
            val exeDebugProfileState = UnityExeDebugProfileState(exeConfiguration, this, environment)
            return UnityAttachToEditorProfileState(exeDebugProfileState, this, environment)
        }
        return null
    }

    override fun checkRunnerSettings(
        runner: ProgramRunner<*>,
        runnerSettings: RunnerSettings?,
        configurationPerRunnerSettings: ConfigurationPerRunnerSettings?
    ) {
        if (runner is RiderDebugRunner) {
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
        port = -1

        try {
            val pid = findUnityEditorProcessFromEditorInstanceJson()
            this.pid = pid
            if (pid == null) {
                return false
            }
            LOG.info("Found Unity Editor process: $pid")
            isCoreClr = getIsCoreClrProcess(pid, project)
            port = convertPidToDebuggerPort(pid)
            return true
        }
        catch (t: Throwable) {
            pid = null
            throw t
        }
    }

    private fun findUnityEditorProcessFromEditorInstanceJson(): Int? {
        val editorInstanceJson = EditorInstanceJson.getInstance(project)
        if (editorInstanceJson.validateStatus() == EditorInstanceJsonStatus.Valid) {
            return editorInstanceJson.contents!!.process_id
        }

        return null
    }

    suspend fun getIsCoreClrProcess(pid: Int, project: Project): Boolean {
        val runtimes = application.service<MsClrAttachableProcessesHost>().localHostHolder.calculateRuntimesForProcess(pid, project)
        return runtimes.any { it is GenericCoreClrRuntime }
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
}

private val LOG = logger<UnityAttachToEditorRunConfiguration>()
