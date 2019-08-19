package com.jetbrains.rider.plugins.unity

import com.intellij.execution.ProgramRunnerUtil
import com.intellij.execution.RunManager
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.WindowManager
import com.intellij.util.BitUtil
import com.intellij.xdebugger.XDebuggerManager
import com.jetbrains.rd.framework.impl.RdTask
import com.jetbrains.rd.util.reactive.AddRemove
import com.jetbrains.rd.util.reactive.Signal
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.debugger.DebuggerInitializingState
import com.jetbrains.rider.debugger.RiderDebugActiveDotNetSessionsTracker
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEvent
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventMode
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventType
import com.jetbrains.rider.plugins.unity.run.DefaultRunConfigurationGenerator
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachToEditorRunConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.UnityDebugConfigurationType
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.getComponent
import java.awt.Frame

class UnityHost(project: Project, runManager: RunManager) : LifetimedProjectComponent(project) {
    val model = project.solution.rdUnityModel

    val sessionInitialized = model.sessionInitialized
    val unityState = model.editorState

    val logSignal = Signal<RdLogEvent>()

    init {
        model.activateRider.advise(componentLifetime) {
            ProjectUtil.focusProjectWindow(project, true)
            val frame = WindowManager.getInstance().getFrame(project)
            if (frame != null) {
                frame.setExtendedState(BitUtil.set(frame.extendedState, Frame.ICONIFIED, false))
            }
        }

        model.onUnityLogEvent.adviseNotNull(componentLifetime) {
            val type = RdLogEventType.values()[it.type]
            val mode = RdLogEventMode.values()[it.mode]
            logSignal.fire(RdLogEvent(it.ticks, type, mode, it.message, it.stackTrace))
        }

        model.startUnity.advise(componentLifetime) {
            StartUnityAction.startUnity(project)
        }

        model.attachDebuggerToUnityEditor.set { lt, _ ->
            val sessions = XDebuggerManager.getInstance(project).debugSessions
            val task = RdTask<Boolean>()

            val configuration =
                runManager.findConfigurationByTypeAndName(UnityDebugConfigurationType.id, DefaultRunConfigurationGenerator.ATTACH_CONFIGURATION_NAME)
            if (configuration == null)
                task.set(false)
            else {
                val unityAttachConfiguration = configuration.configuration as UnityAttachToEditorRunConfiguration
                val isAttached = sessions.any { it.runProfile != null &&
                    it.runProfile is UnityAttachToEditorRunConfiguration &&
                        (it.runProfile as UnityAttachToEditorRunConfiguration).pid == unityAttachConfiguration.pid

                }
                if (!isAttached) {
                    val processTracker: RiderDebugActiveDotNetSessionsTracker = project.getComponent()
                    processTracker.dotNetDebugProcesses.change.advise(componentLifetime) { (event, debugProcess) ->
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
    }

    companion object {
        fun getInstance(project: Project) = project.getComponent<UnityHost>()
    }
}

fun Project.isConnectedToEditor() = UnityHost.getInstance(this).sessionInitialized.valueOrDefault(false)