package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.util.text.StringUtil.escapeXmlEntities
import com.jetbrains.rider.plugins.unity.UnityBundle
import org.jetbrains.annotations.Nls

@Nls
fun unityPipelineGateMessage(gate: UnityMcpSectionState): String? = when (gate) {
    UnityMcpSectionState.NO_PROJECT -> UnityBundle.message("unity.agents.pipeline.no.project")
    UnityMcpSectionState.NOT_TRUSTED -> UnityBundle.message("unity.agents.pipeline.not.trusted")
    UnityMcpSectionState.NO_CLI -> UnityBundle.message("unity.agents.pipeline.needs.cli")
    UnityMcpSectionState.CHECKING, UnityMcpSectionState.READY -> null
}

@Nls
fun unityPipelineInstallHint(latestVersion: String?): String =
    if (latestVersion == null) UnityBundle.message("unity.agents.pipeline.hint.no.version")
    else UnityBundle.message("unity.agents.pipeline.hint", escapeXmlEntities(latestVersion))

@Nls
fun unityPipelineVersionMessage(version: String): String =
    UnityBundle.message("unity.agents.pipeline.version", escapeXmlEntities(version))

@Nls
fun unityPipelineNewVersionMessage(latestVersion: String): String =
    UnityBundle.message("unity.agents.pipeline.new.version", escapeXmlEntities(latestVersion))

@Nls
fun unityPipelineBusyMessage(operation: UnityPipelineOperation): String = when (operation) {
    UnityPipelineOperation.INSTALLING -> UnityBundle.message("unity.agents.pipeline.installing")
    UnityPipelineOperation.UPGRADING -> UnityBundle.message("unity.agents.pipeline.updating")
}

@Nls
fun unityPipelineFailureMessage(failure: UnityPipelineFailure, exitCode: Int?): String = when (failure) {
    UnityPipelineFailure.LAUNCH_FAILED -> UnityBundle.message("unity.agents.pipeline.failed.launch")
    UnityPipelineFailure.EXIT_CODE -> UnityBundle.message("unity.agents.pipeline.failed.exit.code", exitCode ?: -1)
    UnityPipelineFailure.NOT_CONFIRMED -> UnityBundle.message("unity.agents.pipeline.failed.not.confirmed")
}
