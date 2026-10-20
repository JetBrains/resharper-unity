package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.util.NlsSafe
import com.intellij.openapi.util.text.StringUtil.escapeXmlEntities
import com.jetbrains.rider.plugins.unity.UnityBundle
import org.jetbrains.annotations.ApiStatus
import org.jetbrains.annotations.Nls
import java.nio.file.Path

@ApiStatus.Internal
@Nls
fun unityCliVersionMessage(installedVersion: String?): String =
    UnityBundle.message(
        "unity.agents.cli.version",
        if (installedVersion == null) UnityBundle.message("unity.agents.cli.version.none")
        else escapeXmlEntities(installedVersion),
    )

@ApiStatus.Internal
@Nls
fun unityCliNewVersionMessage(latestVersion: String?): String =
    UnityBundle.message("unity.agents.cli.new.version", escapeXmlEntities(latestVersion.orEmpty()))

@ApiStatus.Internal
@NlsSafe
fun unityCliLocationText(location: Path): String = escapeXmlEntities(location.toString())

@ApiStatus.Internal
@Nls
fun unityCliInstallDisclosure(foreignBinaryOnPath: Path?): String =
    if (foreignBinaryOnPath == null) UnityBundle.message("unity.agents.cli.install.explanation")
    else UnityBundle.message("unity.agents.cli.foreign.binary", escapeXmlEntities(foreignBinaryOnPath.toString()))

@ApiStatus.Internal
@Nls
fun unityCliUpdateByHandMessage(command: String?): String =
    if (command == null) UnityBundle.message("unity.agents.cli.update.by.hand.unsafe")
    else UnityBundle.message("unity.agents.cli.update.by.hand", escapeXmlEntities(command))

@ApiStatus.Internal
@Nls
fun unityAgentsUpdateSuggestionMessage(cliVersion: String?, packageVersion: String?, cliByHand: Boolean = false): String {
    val available = unityAgentsUpdateAvailableMessage(cliVersion, packageVersion)
    return if (cliVersion != null && cliByHand) available + " " + UnityBundle.message("unity.agents.update.suggest.by.hand")
    else available
}

@Nls
private fun unityAgentsUpdateAvailableMessage(cliVersion: String?, packageVersion: String?): String = when {
    cliVersion != null && packageVersion != null ->
        UnityBundle.message("unity.agents.update.suggest.both", escapeXmlEntities(cliVersion), escapeXmlEntities(packageVersion))
    cliVersion != null ->
        UnityBundle.message("unity.agents.update.suggest.cli", escapeXmlEntities(cliVersion))
    packageVersion != null ->
        UnityBundle.message("unity.agents.update.suggest.package", escapeXmlEntities(packageVersion))
    else ->
        UnityBundle.message("unity.agents.update.suggest.title")
}
