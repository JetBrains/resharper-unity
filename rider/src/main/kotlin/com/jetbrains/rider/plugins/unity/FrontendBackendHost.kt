package com.jetbrains.rider.plugins.unity

import com.intellij.execution.ProgramRunnerUtil
import com.intellij.execution.RunManager
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.wm.WindowManager
import com.intellij.util.BitUtil
import com.intellij.xdebugger.XDebuggerManager
import com.jetbrains.rd.framework.impl.RdTask
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.reactive.*
import com.jetbrains.rider.debugger.DebuggerInitializingState
import com.jetbrains.rider.debugger.RiderDebugActiveDotNetSessionsTracker
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.model.LogEvent
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.DefaultRunConfigurationGenerator
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachToEditorRunConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.UnityDebugConfigurationType
import com.jetbrains.rider.plugins.unity.util.Utils.Companion.AllowUnitySetForegroundWindow
import com.jetbrains.rider.projectView.solution
import java.awt.Frame
import java.io.File
import java.nio.file.Path
import kotlin.math.max

class FrontendBackendHost(project: Project) : ProtocolSubscribedProjectComponent(project) {
    val model = project.solution.frontendBackendModel

    val logSignal = Signal<LogEvent>()

    init {
        model.activateRider.advise(projectComponentLifetime) {
            ProjectUtil.focusProjectWindow(project, true)
            val frame = WindowManager.getInstance().getFrame(project)
            if (frame != null) {
                if (BitUtil.isSet(frame.extendedState, Frame.ICONIFIED))
                    frame.extendedState = BitUtil.set(frame.extendedState, Frame.ICONIFIED, false)
            }
        }

        model.consoleLogging.onConsoleLogEvent.adviseNotNull(projectComponentLifetime) {
            logSignal.fire(it)
        }

        model.startUnity.advise(projectComponentLifetime) {
            StartUnityAction.startUnity(project)
        }

        model.attachDebuggerToUnityEditor.set { lt, _ ->
            val sessions = XDebuggerManager.getInstance(project).debugSessions
            val task = RdTask<Boolean>()

            val configuration =
                RunManager.getInstance(project).findConfigurationByTypeAndName(UnityDebugConfigurationType.id, DefaultRunConfigurationGenerator.ATTACH_CONFIGURATION_NAME)
            if (configuration == null)
                task.set(false)
            else {
                val unityAttachConfiguration = configuration.configuration as UnityAttachToEditorRunConfiguration
                val isAttached = sessions.any { it.runProfile != null &&
                    it.runProfile is UnityAttachToEditorRunConfiguration &&
                        (it.runProfile as UnityAttachToEditorRunConfiguration).pid == unityAttachConfiguration.pid

                }
                if (!isAttached) {
                    val processTracker: RiderDebugActiveDotNetSessionsTracker = RiderDebugActiveDotNetSessionsTracker.getInstance(project)
                    processTracker.dotNetDebugProcesses.change.advise(projectComponentLifetime) { (event, debugProcess) ->
                        if (event == AddRemove.Add) {
                            debugProcess.initializeDebuggerTask.debuggerInitializingState.advise(lt) {
                                if (it == DebuggerInitializingState.Initialized)
                                    task.set(true)
                                if (it == DebuggerInitializingState.Canceled)
                                    task.set(false)
                            }
                        }
                    }

                    ProgramRunnerUtil.executeConfiguration(configuration, DefaultDebugExecutor.getDebugExecutorInstance())
                } else
                    task.set(true)
            }
            task
        }

        model.allowSetForegroundWindow.set { _, _ ->
            val task = RdTask<Boolean>()

            val id = model.unityApplicationData.valueOrNull?.unityProcessId
            if (id == null)
                task.set(false)
            else
                task.set(AllowUnitySetForegroundWindow(id))

            task
        }

        model.openFileLineCol.set { _, arg ->
            val manager = FileEditorManager.getInstance(project)
            val file = VfsUtil.findFileByIoFile(File(arg.path), false) ?: return@set RdTask.fromResult(false)
            val editors = manager.openEditor(OpenFileDescriptor(project, file, max(0, arg.line - 1), max(0, arg.col - 1)), true)

            RdTask.fromResult(true)
        }
    }

    companion object {
        fun getInstance(project: Project): FrontendBackendHost = project.getComponent(FrontendBackendHost::class.java)
    }
}

fun Project?.isConnectedToEditor() = this != null && this.solution.frontendBackendModel.unityEditorConnected.valueOrDefault(false)