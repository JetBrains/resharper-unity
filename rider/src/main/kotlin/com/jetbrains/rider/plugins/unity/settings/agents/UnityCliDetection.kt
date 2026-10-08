package com.jetbrains.rider.plugins.unity.settings.agents

import com.google.gson.Gson
import com.google.gson.JsonSyntaxException
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.util.system.LowLevelLocalMachineAccess
import com.intellij.util.system.OS
import com.intellij.util.text.SemVer
import java.net.URI
import java.net.URISyntaxException
import java.nio.file.Path

internal const val UNITY_CLI_COMMAND: String = "unity"

private const val UNITY_CLI_HOME_VARIABLE = "UNITY_CLI_HOME"

private const val LOCAL_APP_DATA_VARIABLE = "LOCALAPPDATA"

private val UNITY_CLI_SUBCOMMANDS = listOf("editors", "self-update", "project", "mcp")

private const val UNITY_HUB_APP_BUNDLE = "Unity Hub.app"

private val gson = Gson()

internal data class UnityCliUpdateInfo(
    val installMethod: UnityCliInstallMethod,
    val manifestUrl: String?,
    val updateCommand: String?,
)

@OptIn(LowLevelLocalMachineAccess::class)
fun unityCliExecutableName(): String =
    if (OS.CURRENT == OS.Windows) "$UNITY_CLI_COMMAND.exe" else UNITY_CLI_COMMAND

internal fun unityCliWellKnownCandidates(
    unityCliHome: String?,
    localAppData: String?,
    userHome: Path,
    executableName: String,
): List<Path> = buildList {
    if (!unityCliHome.isNullOrBlank()) add(Path.of(unityCliHome, "bin", executableName))
    add(userHome.resolve(".unity").resolve("bin").resolve(executableName))
    add(userHome.resolve(".local").resolve("bin").resolve(executableName))
    if (!localAppData.isNullOrBlank()) add(Path.of(localAppData, "Unity", "bin", executableName))
}

internal fun isUnityHubPrivateCopy(path: Path): Boolean =
    path.any { it.toString() == UNITY_HUB_APP_BUNDLE }

internal fun isUnityCliVersionOutput(stdout: String): Boolean = SemVer.parseFromText(stdout.trim()) != null

fun isUnityCliHelpOutput(stdout: String): Boolean =
    (stdout.contains("Usage: unity", ignoreCase = true) || stdout.contains("unity [options]", ignoreCase = true)) &&
    UNITY_CLI_SUBCOMMANDS.any { stdout.contains(it, ignoreCase = true) }

internal fun unityCliInstallMethodFromPath(resolvedPath: Path): UnityCliInstallMethod {
    val directories = resolvedPath.map { it.toString() }
    val caskroom = directories.indexOf("Caskroom")
    return if (caskroom >= 0 && directories.getOrNull(caskroom + 1) == "unity-cli") UnityCliInstallMethod.HOMEBREW
    else UnityCliInstallMethod.UNKNOWN
}

internal fun unityCliVersionCommandLine(executable: Path): GeneralCommandLine =
    GeneralCommandLine(executable.toString(), "--version")

internal fun unityCliHelpCommandLine(executable: Path): GeneralCommandLine =
    GeneralCommandLine(executable.toString(), "--help")

internal fun unityCliDiagnoseUpdateCommandLine(executable: Path): GeneralCommandLine =
    GeneralCommandLine(executable.toString(), "diagnose", "update", "--format", "json")

internal fun parseUnityCliUpdateInfo(stdout: String): UnityCliUpdateInfo? {
    val data = parse<DiagnoseUpdateEnvelope>(stdout)?.data ?: return null
    return UnityCliUpdateInfo(
        installMethod = toInstallMethod(data.installMethod),
        manifestUrl = data.manifestUrl,
        updateCommand = data.updateCommand,
    )
}

private val UNITY_CLI_CDN_HOST: String = URI(UNITY_CLI_CDN_BASE_URL).host

fun isReadableManifestUrl(manifestUrl: String): Boolean {
    val uri = try {
        URI(manifestUrl)
    }
    catch (_: URISyntaxException) {
        return false
    }
    return uri.isAbsolute &&
           uri.scheme.equals("https", ignoreCase = true) &&
           uri.host?.equals(UNITY_CLI_CDN_HOST, ignoreCase = true) == true
}

fun parseUnityCliManifestVersion(body: String): String? =
    parse<ChannelManifest>(body)?.version?.takeIf { it.isNotBlank() }

private inline fun <reified T> parse(text: String): T? =
    try {
        gson.fromJson(text, T::class.java)
    }
    catch (_: JsonSyntaxException) {
        null
    }

private fun toInstallMethod(reported: String?): UnityCliInstallMethod = when (reported?.lowercase()) {
    "cdn" -> UnityCliInstallMethod.CDN
    "homebrew" -> UnityCliInstallMethod.HOMEBREW
    "winget" -> UnityCliInstallMethod.WINGET
    "apt" -> UnityCliInstallMethod.APT
    "dnf" -> UnityCliInstallMethod.DNF
    else -> UnityCliInstallMethod.UNKNOWN
}

internal fun UnityCliEnvironment.unityCliHome(): String? = environmentVariable(UNITY_CLI_HOME_VARIABLE)

internal fun UnityCliEnvironment.localAppData(): String? = environmentVariable(LOCAL_APP_DATA_VARIABLE)

fun isUnityCliDiagnoseUpdateOutput(stdout: String): Boolean {
    val envelope = parse<DiagnoseUpdateEnvelope>(stdout) ?: return false
    val data = envelope.data ?: return false
    return envelope.command == "diagnose-update" &&
           (!data.installMethod.isNullOrBlank() || !data.updateCommand.isNullOrBlank() || !data.manifestUrl.isNullOrBlank())
}

private data class DiagnoseUpdateEnvelope(val data: DiagnoseUpdateData?, val command: String? = null)

private data class DiagnoseUpdateData(
    val installMethod: String?,
    val manifestUrl: String?,
    val updateCommand: String?,
)

private data class ChannelManifest(val version: String?)
