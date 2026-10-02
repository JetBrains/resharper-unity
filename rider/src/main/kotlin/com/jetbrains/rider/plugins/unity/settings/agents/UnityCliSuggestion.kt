package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.notification.Notification
import com.intellij.openapi.project.Project

enum class UnityCliSuggestion {
    NONE,
    INSTALL,
    UPDATE,
}

fun unityCliSuggestion(
    presentation: UnityCliPresentation,
    installDeclined: Boolean,
    updateDeclined: Boolean,
): UnityCliSuggestion =
    when (presentation.copy(projectTrusted = true).offer) {
        UnityCliOffer.INSTALL -> if (installDeclined) UnityCliSuggestion.NONE else UnityCliSuggestion.INSTALL
        UnityCliOffer.UPDATE, UnityCliOffer.SHOW_UPDATE_COMMAND ->
            if (updateDeclined) UnityCliSuggestion.NONE else UnityCliSuggestion.UPDATE

        UnityCliOffer.CHECKING,
        UnityCliOffer.NOT_TRUSTED,
        UnityCliOffer.CURRENT,
        UnityCliOffer.LATEST_VERSION_UNKNOWN,
        UnityCliOffer.BUSY,
        UnityCliOffer.FAILED,
            -> UnityCliSuggestion.NONE
    }

fun unityCliSuggestionActs(suggestion: UnityCliSuggestion, presentation: UnityCliPresentation): Boolean =
    when (suggestion) {
        UnityCliSuggestion.INSTALL -> true
        UnityCliSuggestion.UPDATE -> presentation.installMethod.updatableFromRider
        UnityCliSuggestion.NONE -> false
    }

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
