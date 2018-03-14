package com.jetbrains.rider.plugins.unity

import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.util.reactive.Property
import com.jetbrains.rider.util.reactive.Signal
import org.codehaus.jettison.json.JSONObject

class UnityHost(project: Project) : LifetimedProjectComponent(project) {

    val logger = Logger.getInstance(UnityHost::class.java)

    val sessionInitialized = Property(false)
    val unityState = Property(DISCONNECTED)
    val logSignal = Signal<RdLogEvent>()
    val play = Property(false)
    val pause = Property(false)

    val model = project.solution.rdUnityModel

    init {
        model.data.advise(componentLifetime) { item ->
            if (item.key == "UNITY_ActivateRider" && item.newValueOpt == "true") {
                logger.info(item.key+" "+ item.newValueOpt)
                ProjectUtil.focusProjectWindow(project, true)
                model.data["UNITY_ActivateRider"] = "false";
            }else if (item.key == "UNITY_Play" && item.newValueOpt!=null) {
                play.set(item.newValueOpt!!.toBoolean())
            } else if (item.key == "UNITY_EditorState" && item.newValueOpt!=null) {
                unityState.set(item.newValueOpt.toString())
            } else if (item.key == "UNITY_Pause" && item.newValueOpt!=null) {
                pause.set(item.newValueOpt!!.toBoolean())
            } else if (item.key == "UNITY_SessionInitialized" && item.newValueOpt!=null) {
                sessionInitialized.set(item.newValueOpt!!.toBoolean())
            } else if (item.key == "UNITY_LogEntry" && item.newValueOpt!=null) {
                logger.info(item.key+" "+ item.newValueOpt)
                val jsonObj = JSONObject(item.newValueOpt)
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
            project.solution.rdUnityModel.data.remove(key)
            project.solution.rdUnityModel.data[key] = value
        }
    }
}