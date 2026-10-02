package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.util.text.StringUtil.escapeXmlEntities
import com.jetbrains.rider.plugins.unity.UnityBundle
import org.jetbrains.annotations.Nls

@Nls
fun unityCliConfirmationTitle(operation: UnityCliOperation): String = when (operation) {
    UnityCliOperation.INSTALLING -> UnityBundle.message("unity.agents.cli.installed.title")
    UnityCliOperation.UPDATING -> UnityBundle.message("unity.agents.cli.updated.title")
}

@Nls
fun unityCliConfirmationMessage(operation: UnityCliOperation, version: String?): String = when (operation) {
    UnityCliOperation.INSTALLING -> UnityBundle.message("unity.agents.cli.installed.content")
    UnityCliOperation.UPDATING ->
        if (version == null) UnityBundle.message("unity.agents.cli.updated.content.no.version")
        else UnityBundle.message("unity.agents.cli.updated.content", escapeXmlEntities(version))
}

@Nls
fun unityCliUpdateSuggestionMessage(latestVersion: String?): String =
    if (latestVersion == null) UnityBundle.message("unity.agents.cli.suggest.update.content.no.version")
    else UnityBundle.message("unity.agents.cli.suggest.update.content", escapeXmlEntities(latestVersion))
