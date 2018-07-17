package com.jetbrains.rider.plugins.unity

import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.project.Project
import com.jetbrains.rider.model.EditorState
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEvent
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventMode
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventType
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.util.reactive.*

class UnityHost(project: Project) : LifetimedProjectComponent(project) {
    val sessionInitialized = Property(false)
    val unityState = Property(EditorState.Disconnected)
    val logSignal = Signal<RdLogEvent>()
    val play = Property<Boolean?>(null)
    val pause = Property(false)

    val model = project.solution.rdUnityModel

    init {
        model.activateRider.advise(componentLifetime){
            ProjectUtil.focusProjectWindow(project, true)
        }

        model.play.flowInto(componentLifetime, play)
        model.pause.flowInto(componentLifetime, pause)
        model.editorState.flowInto(componentLifetime, unityState)
        model.sessionInitialized.flowInto(componentLifetime, sessionInitialized)
        model.onUnityLogEvent.adviseNotNull(componentLifetime){
            val type = RdLogEventType.values()[it.type]
            val mode = RdLogEventMode.values()[it.mode]
            logSignal.fire(RdLogEvent(it.ticks, type, mode, it.message, it.stackTrace))
        }
    }
    companion object {
        fun CallBackendRefresh(project: Project, force:Boolean) { CallBackend(project, "UNITY_Refresh", force.toString().toLowerCase()) }
        fun CallBackendPlay(project: Project, value:Boolean) { project.solution.rdUnityModel.play.set(value) }
        fun CallBackendStep(project: Project) { CallBackend(project, "UNITY_Step", "true") }

        private fun CallBackend(project: Project, key : String, value:String) {
            project.solution.rdUnityModel.data.remove(key) // Step Action requires this
            project.solution.rdUnityModel.data[key] = value
        }
    }
}