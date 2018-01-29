package com.jetbrains.rider.plugins.unity

import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.util.reactive.Property
import com.jetbrains.rider.util.reactive.Signal
import com.jetbrains.rider.util.reactive.set
import org.codehaus.jettison.json.JSONObject

class ProjectCustomDataHost(project: Project) : LifetimedProjectComponent(project) {
    val logger = Logger.getInstance(ProjectCustomDataHost::class.java)

    val unitySession = Property<Boolean>(false)
    val logSignal = Signal<RdLogEvent>()
    val play = Property<Boolean>(false)
    val pause = Property<Boolean>(false)

    init {
        project.solution.customData.data.advise(componentLifetime) { item ->
            if (item.key == "UNITY_ActivateRider" && item.newValueOpt == "true") {
                logger.info(item.key+" "+ item.newValueOpt)
                ProjectUtil.focusProjectWindow(project, true)
                project.solution.customData.data["UNITY_ActivateRider"] = "false";
            }
        }

        project.solution.customData.data.advise(componentLifetime) { item ->
            if (item.key == "UNITY_SessionInitialized" && item.newValueOpt == "true") {
                logger.info(item.key + " " + item.newValueOpt)
                unitySession.set(true)
            }
        }

        project.solution.customData.data.advise(componentLifetime) { item ->
            if (item.key == "UNITY_Play" && item.newValueOpt!=null) {
                play.set(item.newValueOpt!!.toBoolean())
            }
        }

        project.solution.customData.data.advise(componentLifetime) { item ->
            if (item.key == "UNITY_Pause" && item.newValueOpt!=null) {
                pause.set(item.newValueOpt!!.toBoolean())
            }
        }

        project.solution.customData.data.advise(componentLifetime) { item ->
            if (item.key == "UNITY_LogEntry" && item.newValueOpt!=null) {
                logger.info(item.key+" "+ item.newValueOpt)
                
                val jsonObj = JSONObject(item.newValueOpt)
                val type = RdLogEventType.values().get(jsonObj.getInt("Type"))
                val mode = RdLogEventMode.values().get(jsonObj.getInt("Mode"))
                logSignal.fire(RdLogEvent(type, mode, jsonObj.getString("Message"), jsonObj.getString("StackTrace")))
            }
        }
    }
    companion object {
        fun CallBackendRefresh(project: Project) { CallBackend(project, "UNITY_Refresh", "true") }
        fun CallBackendPlay(project: Project, value:Boolean) { CallBackend(project, "UNITY_Play", value.toString().toLowerCase()) }
        fun CallBackendPause(project: Project, value:Boolean) { CallBackend(project, "UNITY_Pause", value.toString().toLowerCase()) }
        fun CallBackendStep(project: Project) { CallBackend(project, "UNITY_Step", "true") }

        private fun CallBackend(project: Project, key : String, value:String) {
            project.solution.customData.data.remove(key)
            project.solution.customData.data[key] = value
        }
    }
}