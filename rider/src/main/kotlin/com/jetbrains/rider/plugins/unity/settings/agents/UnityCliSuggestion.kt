package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.notification.Notification
import com.intellij.openapi.project.Project
import org.jetbrains.annotations.ApiStatus

@ApiStatus.Internal
enum class UnityCliSuggestion {
    NONE,
    INSTALL,
    UPDATE,
}

@ApiStatus.Internal
data class UnityAgentsSweepFacts(
    val packageMissing: Boolean,
    val packageUpgradable: Boolean,
    val installDeclined: Boolean,
    val updateDeclined: Boolean,
)

@ApiStatus.Internal
fun unityCliSuggestion(presentation: UnityCliPresentation, facts: UnityAgentsSweepFacts): UnityCliSuggestion {
    val cli = presentation.copy(projectTrusted = true).offer
    val wantsInstall = when (cli) {
        UnityCliOffer.INSTALL -> true
        UnityCliOffer.CURRENT,
        UnityCliOffer.LATEST_VERSION_UNKNOWN,
        UnityCliOffer.UPDATE,
        UnityCliOffer.SHOW_UPDATE_COMMAND,
            -> facts.packageMissing
        UnityCliOffer.CHECKING, UnityCliOffer.NOT_TRUSTED, UnityCliOffer.BUSY, UnityCliOffer.FAILED -> false
    }
    if (wantsInstall) return if (facts.installDeclined) UnityCliSuggestion.NONE else UnityCliSuggestion.INSTALL
    val wantsUpdate = when (cli) {
        UnityCliOffer.UPDATE, UnityCliOffer.SHOW_UPDATE_COMMAND -> true
        UnityCliOffer.CURRENT, UnityCliOffer.LATEST_VERSION_UNKNOWN -> facts.packageUpgradable
        UnityCliOffer.INSTALL,
        UnityCliOffer.CHECKING,
        UnityCliOffer.NOT_TRUSTED,
        UnityCliOffer.BUSY,
        UnityCliOffer.FAILED,
            -> false
    }
    return if (wantsUpdate && !facts.updateDeclined) UnityCliSuggestion.UPDATE else UnityCliSuggestion.NONE
}

internal fun unityCliUpdatable(presentation: UnityCliPresentation): Boolean =
    presentation.copy(projectTrusted = true).offer == UnityCliOffer.UPDATE

@ApiStatus.Internal
fun unityCliSuggestionActs(
    suggestion: UnityCliSuggestion,
    presentation: UnityCliPresentation,
    packageUpgradable: Boolean,
): Boolean =
    when (suggestion) {
        UnityCliSuggestion.INSTALL -> true
        UnityCliSuggestion.UPDATE -> unityCliUpdatable(presentation) || packageUpgradable
        UnityCliSuggestion.NONE -> false
    }

@ApiStatus.Internal
enum class UnityAgentsUpdateAction {
    UPDATE,
    SHOW_UPDATE_COMMAND,
    LEARN_MORE,
}

/**
 * The actions of the update balloon, in order. The balloon draws [UnityAgentsUpdateAction.UPDATE] as its
 * button and every other action as a link.
 */
@ApiStatus.Internal
fun unityAgentsUpdateActions(acts: Boolean, cliOffer: UnityCliOffer): List<UnityAgentsUpdateAction> = buildList {
    if (acts) add(UnityAgentsUpdateAction.UPDATE)
    if (cliOffer == UnityCliOffer.SHOW_UPDATE_COMMAND) add(UnityAgentsUpdateAction.SHOW_UPDATE_COMMAND)
    add(UnityAgentsUpdateAction.LEARN_MORE)
}

@ApiStatus.Internal
object UnityCliDeclines {
    const val INSTALL_DISPLAY_ID: String = "unity.for.agents.cli.install"
    const val UPDATE_DISPLAY_ID: String = "unity.for.agents.cli.update"

    fun displayIdOf(suggestion: UnityCliSuggestion): String? = when (suggestion) {
        UnityCliSuggestion.INSTALL -> INSTALL_DISPLAY_ID
        UnityCliSuggestion.UPDATE -> UPDATE_DISPLAY_ID
        UnityCliSuggestion.NONE -> null
    }

    fun isDeclined(project: Project?, displayId: String): Boolean =
        Notification.isDoNotAskFor(project, displayId)
}
