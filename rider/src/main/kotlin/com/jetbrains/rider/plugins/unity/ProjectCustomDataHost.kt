package com.jetbrains.rider.plugins.unity

import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
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
                logSignal.fire(RdLogEvent(RdLogEventType.Error, jsonObj.getString("Message"), jsonObj.getString("Stacktrace")))
            }
        }
    }

    fun CallBackendRefresh(project: Project) {
        project.solution.customData.data["UNITY_Refresh"] = "true";
    }
}