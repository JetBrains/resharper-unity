package com.jetbrains.rider.plugins.unity

import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.util.reactive.Property
import com.jetbrains.rider.util.reactive.Signal
import org.codehaus.jettison.json.JSONObject

class ProjectCustomDataHost(project: Project) : LifetimedProjectComponent(project) {

    val logger = Logger.getInstance(ProjectCustomDataHost::class.java)

    val sessionInitialized = Property(false)
    val unityState = Property(DISCONNECTED)
    val logSignal = Signal<RdLogEvent>()
    val play = Property<Boolean?>(null)
    val pause = Property(false)

    init {
        project.solution.customData.data.advise(componentLifetime) { item ->
            val newVal = item.newValueOpt
            if (item.key == "UNITY_ActivateRider" && newVal == "true") {
                logger.info(item.key+" "+ newVal)
                ProjectUtil.focusProjectWindow(project, true)
                project.solution.customData.data["UNITY_ActivateRider"] = "false";
            }else if (item.key == "UNITY_Play" && newVal != null) {
                if (newVal != "undef")
                    play.set(newVal.toBoolean())
                else
                    play.set(null)
            } else if (item.key == "UNITY_EditorState" && newVal != null) {
                unityState.set(newVal.toString())
            } else if (item.key == "UNITY_Pause" && newVal!=null) {
                pause.set(newVal.toBoolean())
            } else if (item.key == "UNITY_SessionInitialized" && newVal!=null) {
                sessionInitialized.set(newVal.toBoolean())
            } else if (item.key == "UNITY_LogEntry" && newVal!=null) {
                logger.info(item.key+" "+ newVal)
                val jsonObj = JSONObject(newVal)
                val type = RdLogEventType.values().get(jsonObj.getInt("Type"))
                val mode = RdLogEventMode.values().get(jsonObj.getInt("Mode"))
                logSignal.fire(RdLogEvent(type, mode, jsonObj.getString("Message"), jsonObj.getString("StackTrace")))
            }
        }
    }
    companion object {
        fun CallBackendRefresh(project: Project, force:Boolean) { CallBackend(project, "UNITY_Refresh", force.toString().toLowerCase()) }
        fun CallBackendPlay(project: Project, value:Boolean) { CallBackend(project, "UNITY_Play", value.toString().toLowerCase()) }
        fun CallBackendPause(project: Project, value:Boolean) { CallBackend(project, "UNITY_Pause", value.toString().toLowerCase()) }
        fun CallBackendStep(project: Project) { CallBackend(project, "UNITY_Step", "true") }

        const val DISCONNECTED = "Disconnected"
        const val CONNECTED_IDLE = "ConnectedIdle"
        const val CONNECTED_PLAY = "ConnectedPlay"
        const val CONNECTED_REFRESH = "ConnectedRefresh"

        private fun CallBackend(project: Project, key : String, value:String) {
            project.solution.customData.data[key] = value
        }
    }
}