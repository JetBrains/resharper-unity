package com.jetbrains.rider.plugins.unity

import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.LocalFileSystem
import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.ILifetimedComponent
import com.jetbrains.rider.util.idea.LifetimedComponent
import com.jetbrains.rider.util.reactive.Property
import com.jetbrains.rider.util.reactive.Signal
import com.jetbrains.rider.util.reactive.set
import org.codehaus.jettison.json.JSONObject

class ProjectCustomDataHost(val project: Project) : ILifetimedComponent by LifetimedComponent(project) {
    val logger = Logger.getInstance(ProjectCustomDataHost::class.java)

    val unitySession = Property<Boolean>()
    val logSignal = Signal<RdLogEvent>()

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
        fun CallBackendRefresh(project: Project) { CallBackend(project, "UNITY_Refresh") }
        fun CallBackendPlay(project: Project) { CallBackend(project, "UNITY_Play") }
        fun CallBackendPause(project: Project) { CallBackend(project, "UNITY_Pause") }
        fun CallBackendResume(project: Project) { CallBackend(project, "UNITY_Resume") }
        fun CallBackendStop(project: Project) { CallBackend(project, "UNITY_Stop") }

        private fun CallBackend(project: Project, key : String) {
            project.solution.customData.data.remove(key)
            project.solution.customData.data[key] = "true"
        }
    }
}