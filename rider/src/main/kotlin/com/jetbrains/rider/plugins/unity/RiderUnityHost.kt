package com.jetbrains.rider.plugins.unity

import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rider.framework.FrameworkMarshallers.DateTime
import com.jetbrains.rider.model.EditorState
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.editorPlugin.model.*
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.util.reactive.Property
import com.jetbrains.rider.util.reactive.Signal
import com.jetbrains.rider.util.reactive.flowInto
import org.codehaus.jettison.json.JSONObject
import java.time.LocalDateTime
import java.util.*

class UnityHost(project: Project) : LifetimedProjectComponent(project) {

    private val logger = Logger.getInstance(UnityHost::class.java)

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
        model.editorState.flowInto(componentLifetime, unityState)

        model.data.advise(componentLifetime) { item ->
            val newVal = item.newValueOpt
            if (item.key == "UNITY_Pause" && newVal!=null) {
                pause.set(newVal.toBoolean())
            } else if (item.key == "UNITY_SessionInitialized" && newVal!=null) {
                sessionInitialized.set(newVal.toBoolean())
            } else if (item.key == "UNITY_LogEntry" && newVal!=null) {
                logger.info(item.key+" "+ newVal)
                val jsonObj = JSONObject(newVal)
                val type = RdLogEventType.values().get(jsonObj.getInt("Type"))
                val mode = RdLogEventMode.values().get(jsonObj.getInt("Mode"))
                val ticks = jsonObj.getLong("Time")
                logSignal.fire(RdLogEvent(ticks, type, mode, jsonObj.getString("Message"), jsonObj.getString("StackTrace")))
            }
        }
    }
    companion object {
        fun CallBackendRefresh(project: Project, force:Boolean) { CallBackend(project, "UNITY_Refresh", force.toString().toLowerCase()) }
        fun CallBackendPlay(project: Project, value:Boolean) { project.solution.rdUnityModel.play.set(value) }
        fun CallBackendPause(project: Project, value:Boolean) { CallBackend(project, "UNITY_Pause", value.toString().toLowerCase()) }
        fun CallBackendStep(project: Project) { CallBackend(project, "UNITY_Step", "true") }

        private fun CallBackend(project: Project, key : String, value:String) {
            project.solution.rdUnityModel.data.remove(key) // Step Action requires this
            project.solution.rdUnityModel.data[key] = value
        }
    }
}