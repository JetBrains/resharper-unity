package com.jetbrains.rider.plugins.unity

import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.project.Project
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEvent
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventMode
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventType
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.getComponent
import com.jetbrains.rd.util.reactive.Signal
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rd.util.reactive.valueOrDefault

class UnityHost(project: Project) : LifetimedProjectComponent(project) {
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
    }

    companion object {
        fun getInstance(project: Project) = project.getComponent<UnityHost>()
    }
}

fun Project.isConnectedToEditor() = UnityHost.getInstance(this).sessionInitialized.valueOrDefault(false)