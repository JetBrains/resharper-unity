package com.jetbrains.rider.plugins.unity.profiler.lineMarkers

import com.intellij.openapi.client.ClientAppSession
import com.intellij.openapi.editor.Document
import com.intellij.openapi.project.Project
import com.jetbrains.rd.ide.model.RdMarkupModel
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rdclient.daemon.IProtocolHighlighterModelHandler
import com.jetbrains.rdclient.daemon.IProtocolHighlighterModelSupport
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendProfilerModel
import com.jetbrains.rider.projectView.solution

class UnityProfilerLineMarkerModelSupport : IProtocolHighlighterModelSupport {
    override fun createHandler(
        lifetime: Lifetime,
        project: Project?,
        session: ClientAppSession,
        markupModel: RdMarkupModel,
        document: Document,
    ): IProtocolHighlighterModelHandler? {
        project ?: return null
        if (!project.isUnityProject.value) return null

        val profilerModel = project.solution.frontendBackendModel.frontendBackendProfilerModel
        return UnityProfilerLineMarkerModelHandler(profilerModel, project, lifetime)
    }
}
