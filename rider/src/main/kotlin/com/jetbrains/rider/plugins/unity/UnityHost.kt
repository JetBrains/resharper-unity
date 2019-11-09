package com.jetbrains.rider.plugins.unity

import com.intellij.execution.ProgramRunnerUtil
import com.intellij.execution.RunManager
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.wm.WindowManager
import com.intellij.util.BitUtil
import com.intellij.xdebugger.XDebuggerManager
import com.jetbrains.rd.framework.impl.RdTask
import com.jetbrains.rd.util.reactive.AddRemove
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.debugger.DebuggerInitializingState
import com.jetbrains.rider.debugger.RiderDebugActiveDotNetSessionsTracker
import com.jetbrains.rider.model.frontendBackendModel
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.run.DefaultRunConfigurationGenerator
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachToEditorRunConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.UnityDebugConfigurationType
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.getComponent
import com.sun.jna.Native
import com.sun.jna.win32.StdCallLibrary
import java.awt.Frame

class UnityHost(project: Project, runManager: RunManager) : LifetimedProjectComponent(project) {
    private val logger = Logger.getInstance(UnityHost::class.java)

    val model = project.solution.frontendBackendModel
    val sessionInitialized = model.sessionInitialized
    val unityState = model.editorState
    val onLogEvent = model.onUnityLogEvent

    init {
        model.activateRider.advise(componentLifetime) {
            ProjectUtil.focusProjectWindow(project, true)
            val frame = WindowManager.getInstance().getFrame(project)
            if (frame != null) {
                frame.extendedState = BitUtil.set(frame.extendedState, Frame.ICONIFIED, false)
            }
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

        model.allowSetForegroundWindow.set { _, _ ->
            val task = RdTask<Boolean>()
            if (SystemInfo.isWindows) {
                val id = model.unityProcessId.valueOrNull
                if (id != null && id > 0)
                    task.set(user32!!.AllowSetForegroundWindow(id))
                else
                    logger.warn("unityProcessId is null or 0")
            }
            else
                task.set(true)

            task
        }

    }

    companion object {
        fun getInstance(project: Project) = project.getComponent<UnityHost>()
    }

    @Suppress("FunctionName")
    private interface User32 : StdCallLibrary {
        fun AllowSetForegroundWindow(id:Int) : Boolean
    }

    private val user32 = if (SystemInfo.isWindows) Native.load("user32", User32::class.java) else null
}

fun Project.isConnectedToEditor() = UnityHost.getInstance(this).sessionInitialized.valueOrDefault(false)