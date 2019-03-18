package com.jetbrains.rider.plugins.unity

import com.intellij.execution.ProgramRunnerUtil
import com.intellij.execution.RunManager
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.project.Project
import com.jetbrains.rd.framework.impl.RdTask
import com.jetbrains.rd.util.reactive.Signal
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
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

class UnityHost(project: Project, runManager: RunManager) : LifetimedProjectComponent(project) {
    val model = project.solution.rdUnityModel

    val sessionInitialized = model.sessionInitialized
    val unityState = model.editorState
    val play = model.play
    val pause = model.pause

    val logSignal = Signal<RdLogEvent>()

    init {
        model.activateRider.advise(componentLifetime) {
            ProjectUtil.focusProjectWindow(project, true)
        }

        model.onUnityLogEvent.adviseNotNull(componentLifetime) {
            val type = RdLogEventType.values()[it.type]
            val mode = RdLogEventMode.values()[it.mode]
            logSignal.fire(RdLogEvent(it.ticks, type, mode, it.message, it.stackTrace))
        }

        model.startUnity.advise(componentLifetime) {
            StartUnityAction.startUnity(project)
        }

        model.attachDebuggerToUnityEditor.set { _, _ ->
            val task = RdTask<Boolean>()
            UnityAttachToEditorRunConfiguration
            val configuration = runManager.findConfigurationByTypeAndName(UnityDebugConfigurationType.id, DefaultRunConfigurationGenerator.ATTACH_CONFIGURATION_NAME)
            if (configuration != null) {
                ProgramRunnerUtil.executeConfiguration(configuration, DefaultDebugExecutor.getDebugExecutorInstance())
                task.set(true)
            }
            else
                task.set(false)

            task
        }
    }

    companion object {
        fun getInstance(project: Project) = project.getComponent<UnityHost>()
    }
}

fun Project.isConnectedToEditor() = UnityHost.getInstance(this).sessionInitialized.valueOrDefault(false)