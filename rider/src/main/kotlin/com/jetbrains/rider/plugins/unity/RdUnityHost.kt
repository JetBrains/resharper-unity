package com.jetbrains.rider.plugins.unity

import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rider.model.RdUnityModel
import com.jetbrains.rider.protocol.IProtocolHost
import com.jetbrains.rider.protocol.ProtocolComponent
import com.jetbrains.rider.protocol.ProtocolComponentFactory
import com.jetbrains.rider.util.reactive.Property
import com.jetbrains.rider.util.reactive.Signal
import org.codehaus.jettison.json.JSONObject

class RdUnityFactory : ProtocolComponentFactory {
    override fun create(protocolHost: IProtocolHost): ProtocolComponent? {
        return RdUnityHost(protocolHost)
    }

}

class RdUnityHost(protocolHost: IProtocolHost) : ProtocolComponent(protocolHost) {

    val logger = Logger.getInstance(RdUnityHost::class.java)

    val isConnected = Property<Boolean>(false)
    val logSignal = Signal<RdLogEvent>()
    val play = Property<Boolean>(false)
    val pause = Property<Boolean>(false)

    val model = RdUnityModel.create(lifetime, protocol)

    init {
        model.data.advise(lifetime) { item ->
            if (item.key == "UNITY_ActivateRider" && item.newValueOpt == "true") {
                logger.info(item.key+" "+ item.newValueOpt)
                ProjectUtil.focusProjectWindow(project, true)
                model.data["UNITY_ActivateRider"] = "false";
            }
        }

        model.data.advise(lifetime) { item ->
            if (item.key == "UNITY_Play" && item.newValueOpt!=null) {
                play.set(item.newValueOpt!!.toBoolean())
            }
        }

        model.data.advise(lifetime) { item ->
            if (item.key == "UNITY_Pause" && item.newValueOpt!=null) {
                pause.set(item.newValueOpt!!.toBoolean())
            }
        }

        model.data.advise(lifetime) { item ->
            if (item.key == "UNITY_SessionInitialized" && item.newValueOpt!=null) {
                isConnected.set(item.newValueOpt!!.toBoolean())
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

    fun CallBackendRefresh(project: Project) { CallBackend("UNITY_Refresh", "true") }
    fun CallBackendPlay(project: Project, value:Boolean) { CallBackend("UNITY_Play", value.toString().toLowerCase()) }
    fun CallBackendPause(project: Project, value:Boolean) { CallBackend("UNITY_Pause", value.toString().toLowerCase()) }
    fun CallBackendStep(project: Project) { CallBackend("UNITY_Step", "true") }

    private fun CallBackend(key: String, value: String) {
        model.data.remove(key)
        model.data[key] = value
    }
}