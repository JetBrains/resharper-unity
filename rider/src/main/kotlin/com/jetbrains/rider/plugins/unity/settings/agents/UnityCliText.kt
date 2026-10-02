package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.util.text.StringUtil.escapeXmlEntities
import com.jetbrains.rider.plugins.unity.UnityBundle
import org.jetbrains.annotations.Nls
import java.nio.file.Path

@Nls
fun unityCliVersionMessage(installedVersion: String?): String =
    UnityBundle.message(
        "unity.agents.cli.version",
        if (installedVersion == null) UnityBundle.message("unity.agents.cli.version.none")
        else escapeXmlEntities(installedVersion),
    )

@Nls
fun unityCliNewVersionMessage(latestVersion: String?): String =
    UnityBundle.message("unity.agents.cli.new.version", escapeXmlEntities(latestVersion.orEmpty()))

@Nls
fun unityCliPathMessage(location: Path, installMethod: UnityCliInstallMethod): String {
    val path = escapeXmlEntities(location.toString())
    val method = when (installMethod) {
        UnityCliInstallMethod.CDN -> UnityBundle.message("unity.agents.cli.method.cdn")
        UnityCliInstallMethod.HOMEBREW -> UnityBundle.message("unity.agents.cli.method.homebrew")
        UnityCliInstallMethod.WINGET -> UnityBundle.message("unity.agents.cli.method.winget")
        UnityCliInstallMethod.APT -> UnityBundle.message("unity.agents.cli.method.apt")
        UnityCliInstallMethod.DNF -> UnityBundle.message("unity.agents.cli.method.dnf")
        UnityCliInstallMethod.UNKNOWN -> null
    }
    return if (method == null) UnityBundle.message("unity.agents.cli.path", path)
    else UnityBundle.message("unity.agents.cli.path.with.method", path, method)
}

@Nls
fun unityCliInstallDisclosure(foreignBinaryOnPath: Path?): String =
    if (foreignBinaryOnPath == null) UnityBundle.message("unity.agents.cli.install.explanation")
    else UnityBundle.message("unity.agents.cli.foreign.binary", escapeXmlEntities(foreignBinaryOnPath.toString()))

@Nls
fun unityCliUpdateByHandMessage(command: String?): String =
    if (command == null) UnityBundle.message("unity.agents.cli.update.by.hand.unsafe")
    else UnityBundle.message("unity.agents.cli.update.by.hand", escapeXmlEntities(command))
