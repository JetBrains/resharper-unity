package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.util.text.StringUtil.escapeXmlEntities
import com.jetbrains.rider.plugins.unity.UnityBundle
import org.jetbrains.annotations.ApiStatus
import org.jetbrains.annotations.Nls

internal fun unitySkillGateMessage(gate: UnityMcpSectionState): @Nls String? = when (gate) {
    UnityMcpSectionState.NO_PROJECT -> UnityBundle.message("unity.agents.skills.no.project")
    UnityMcpSectionState.NOT_TRUSTED -> UnityBundle.message("unity.agents.skills.not.trusted")
    UnityMcpSectionState.NO_CLI,
    UnityMcpSectionState.READY,
        -> UnityBundle.message("unity.agents.skills.needs.setup")
    UnityMcpSectionState.CHECKING -> null
}

@Nls
@ApiStatus.Internal
fun unitySkillPlacesMessage(installs: List<UnitySkillInstall>): String {
    val places = installs
        .distinctBy { it.client to it.local }
        .joinToString("; ") { install ->
            val agent = when (install.client) {
                UnitySkillClient.CODEX.key -> UnityBundle.message("unity.agents.skills.client.codex")
                UnitySkillClient.CLAUDE_CODE.key -> UnityBundle.message("unity.agents.skills.client.claude.code")
                "vscode" -> UnityBundle.message("unity.agents.skills.client.copilot")
                else -> escapeXmlEntities(install.client)
            }
            val scope = if (install.local) {
                UnityBundle.message("unity.agents.skills.scope.project")
            }
            else {
                UnityBundle.message("unity.agents.skills.scope.global")
            }
            UnityBundle.message("unity.agents.skills.place", agent, scope)
        }
    return UnityBundle.message("unity.agents.skills.places", places)
}

@Nls
internal fun unitySkillFailureMessage(failure: UnitySkillFailure, exitCode: Int?): String = when (failure) {
    UnitySkillFailure.LAUNCH_FAILED -> UnityBundle.message("unity.agents.skills.failed.launch")
    UnitySkillFailure.EXIT_CODE -> UnityBundle.message("unity.agents.skills.failed.exit.code", exitCode ?: -1)
    UnitySkillFailure.NOT_CONFIRMED -> UnityBundle.message("unity.agents.skills.failed.not.confirmed")
}
