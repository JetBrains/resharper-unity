package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.util.text.StringUtil.escapeXmlEntities
import com.jetbrains.rider.plugins.unity.UnityBundle
import org.jetbrains.annotations.ApiStatus
import org.jetbrains.annotations.Nls

@Nls
@ApiStatus.Internal
fun unityPipelineGateMessage(gate: UnityMcpSectionState): String? = when (gate) {
    UnityMcpSectionState.NO_PROJECT -> UnityBundle.message("unity.agents.pipeline.no.project")
    UnityMcpSectionState.NOT_TRUSTED -> UnityBundle.message("unity.agents.pipeline.not.trusted")
    UnityMcpSectionState.NO_CLI -> UnityBundle.message("unity.agents.pipeline.needs.cli")
    UnityMcpSectionState.CHECKING, UnityMcpSectionState.READY -> null
}

@Nls
@ApiStatus.Internal
fun unityPipelineInstallHint(latestVersion: String?): String =
    if (latestVersion == null) UnityBundle.message("unity.agents.pipeline.hint.no.version")
    else UnityBundle.message("unity.agents.pipeline.hint", escapeXmlEntities(latestVersion))

@Nls
@ApiStatus.Internal
fun unityPipelineVersionMessage(version: String): String =
    UnityBundle.message("unity.agents.pipeline.version", escapeXmlEntities(version))

@Nls
@ApiStatus.Internal
fun unityPipelineNewVersionMessage(latestVersion: String): String =
    UnityBundle.message("unity.agents.pipeline.new.version", escapeXmlEntities(latestVersion))

@Nls
@ApiStatus.Internal
fun unityPipelineBusyMessage(operation: UnityPipelineOperation): String = when (operation) {
    UnityPipelineOperation.INSTALLING -> UnityBundle.message("unity.agents.pipeline.installing")
    UnityPipelineOperation.UPGRADING -> UnityBundle.message("unity.agents.pipeline.updating")
}

@Nls
@ApiStatus.Internal
fun unityPipelineFailureMessage(failure: UnityPipelineFailure): String = when (failure) {
    UnityPipelineFailure.REGISTRY_UNREACHABLE -> UnityBundle.message("unity.agents.pipeline.failed.registry")
    UnityPipelineFailure.MANIFEST_WRITE -> UnityBundle.message("unity.agents.pipeline.failed.write")
    UnityPipelineFailure.NOT_CONFIRMED -> UnityBundle.message("unity.agents.pipeline.failed.not.confirmed")
    UnityPipelineFailure.EDITOR_TOO_OLD -> UnityBundle.message("unity.agents.pipeline.failed.old.editor")
}

@Nls
@ApiStatus.Internal
fun unityPipelineOldEditorMessage(minimumEditorVersion: String?, editorVersion: String?): String =
    UnityBundle.message(
        "unity.agents.pipeline.old.editor",
        escapeXmlEntities(minimumEditorVersion.orEmpty()),
        escapeXmlEntities(editorVersion.orEmpty()),
    )

/** The gate text of the skill and MCP sections in a project with an old Editor. */
@Nls
@ApiStatus.Internal
fun unityOldEditorGateMessage(oldEditor: UnityOldEditor): String =
    UnityBundle.message(
        "unity.agents.old.editor.gate",
        escapeXmlEntities(oldEditor.minimumVersion),
        escapeXmlEntities(oldEditor.editorVersion),
    )
