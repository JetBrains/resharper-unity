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
fun unitySkillPlacesMessage(installs: List<UnitySkillInstall>): String? {
    if (unitySkillOpenTargets(installs).isEmpty()) return null
    val places = installs
        .flatMap { install ->
            val agents = unitySkillReaders(install).mapNotNull(::unitySkillAgentName)
            agents.ifEmpty { listOf(escapeXmlEntities(install.client)) }.map { agent -> agent to install.local }
        }
        .distinct()
        .joinToString("; ") { (agent, local) ->
            val scope = if (local) {
                UnityBundle.message("unity.agents.skills.scope.project")
            }
            else {
                UnityBundle.message("unity.agents.skills.scope.global")
            }
            UnityBundle.message("unity.agents.skills.place", agent, scope)
        }
    return UnityBundle.message("unity.agents.skills.places", places)
}

private fun unitySkillAgentName(agent: UnitySkillTarget): @Nls String? = when (agent) {
    UnitySkillTarget.CODEX -> UnityBundle.message("unity.agents.skills.client.codex")
    UnitySkillTarget.CLAUDE_CODE -> UnityBundle.message("unity.agents.skills.client.claude.code")
    UnitySkillTarget.COPILOT -> UnityBundle.message("unity.agents.skills.client.copilot")
    UnitySkillTarget.JUNIE -> UnityBundle.message("unity.agents.skills.client.junie")
    UnitySkillTarget.ALL -> null
}

@Nls
internal fun unitySkillFailureMessage(failure: UnitySkillFailure, exitCode: Int?): String = when (failure) {
    UnitySkillFailure.LAUNCH_FAILED -> UnityBundle.message("unity.agents.skills.failed.launch")
    UnitySkillFailure.EXIT_CODE -> UnityBundle.message("unity.agents.skills.failed.exit.code", exitCode ?: -1)
    UnitySkillFailure.NOT_CONFIRMED -> UnityBundle.message("unity.agents.skills.failed.not.confirmed")
}
