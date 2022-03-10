package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.application.ex.ApplicationInfoEx
import com.intellij.openapi.project.Project
import com.jetbrains.rider.actions.RiderTechnicalSupportEntry
import com.jetbrains.rider.actions.RiderTechnicalSupportInfoProvider
import com.jetbrains.rider.plugins.unity.FrontendBackendHost

class UnityVersionRiderTechnicalSupportInfoProvider : RiderTechnicalSupportInfoProvider {

    override fun getEntry(e: AnActionEvent, appInfo: ApplicationInfoEx): RiderTechnicalSupportEntry {
       return RiderTechnicalSupportEntry("\$UNITY_VERSION", getUnityVersion(e.project))
    }


    fun getUnityVersion(project: Project?): String {
        if (project == null)
            return ""

        if (project.isDisposed)
            return ""

        val unityHost = FrontendBackendHost.getInstance(project)

        return unityHost.model.unityApplicationData.valueOrNull?.applicationVersion ?: ""
    }
}