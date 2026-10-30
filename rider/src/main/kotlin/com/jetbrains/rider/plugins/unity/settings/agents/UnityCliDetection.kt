package com.jetbrains.rider.plugins.unity.settings.agents

import com.google.gson.Gson
import com.google.gson.JsonSyntaxException
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.util.system.LowLevelLocalMachineAccess
import com.intellij.util.system.OS
import com.intellij.util.text.SemVer
import org.jetbrains.annotations.ApiStatus
import java.io.IOException
import java.nio.file.Files
import java.nio.file.Path

internal const val UNITY_CLI_COMMAND: String = "unity"

private const val UNITY_CLI_HOME_VARIABLE = "UNITY_CLI_HOME"

private const val LOCAL_APP_DATA_VARIABLE = "LOCALAPPDATA"

private const val UNITY_HUB_APP_BUNDLE = "Unity Hub.app"

/** The usage line of the help of the CLI. */
private val UNITY_CLI_USAGE_MARKERS = listOf("Usage: unity", "unity [options]")

/** The command that updates the CLI, under its name since 1.0.0-beta.8 and under its older name. */
private val UNITY_CLI_UPDATE_MARKERS = listOf("self-update", "upgrade")

/** The command that runs the MCP server, which the agents need. */
private const val UNITY_CLI_MCP_MARKER = "mcp"

private const val HOMEBREW_PACKAGE_DIRECTORY = "unity-cli"

/** A package manager of Linux installs under this folder. */
private val LINUX_PACKAGE_ROOT: Path = Path.of("/usr")

/** No package manager owns a copy under this folder. */
private val LINUX_LOCAL_ROOT: Path = Path.of("/usr/local")

private const val DPKG_PACKAGE_LIST = "unity-cli.list"

/** The list file of a Multi-Arch package carries the architecture, as in `unity-cli:amd64.list`. */
private const val DPKG_MULTI_ARCH_PACKAGE_LIST = "unity-cli:*.list"

private val gson = Gson()

@OptIn(LowLevelLocalMachineAccess::class)
@ApiStatus.Internal
fun unityCliExecutableName(): String =
    if (OS.CURRENT == OS.Windows) "$UNITY_CLI_COMMAND.exe" else UNITY_CLI_COMMAND

/** The folders where the installer scripts of Unity put the CLI. */
internal fun unityCliCdnDirectories(
    unityCliHome: String?,
    localAppData: String?,
    userHome: Path,
): List<Path> = buildList {
    if (!unityCliHome.isNullOrBlank()) add(Path.of(unityCliHome, "bin"))
    add(userHome.resolve(".unity").resolve("bin"))
    add(userHome.resolve(".local").resolve("bin"))
    if (!localAppData.isNullOrBlank()) add(Path.of(localAppData, "Unity", "bin"))
}

internal fun unityCliWellKnownCandidates(
    unityCliHome: String?,
    localAppData: String?,
    userHome: Path,
    executableName: String,
): List<Path> = buildList {
    unityCliCdnDirectories(unityCliHome, localAppData, userHome).mapTo(this) { it.resolve(executableName) }
    if (!localAppData.isNullOrBlank()) {
        // The PATH lookup skips an MSIX app execution alias, because Java cannot read its attributes.
        add(Path.of(localAppData, "Microsoft", "WindowsApps", executableName))
    }
}

internal fun isUnityHubPrivateCopy(path: Path): Boolean =
    path.any { it.toString() == UNITY_HUB_APP_BUNDLE }

/**
 * The version that `unity --version` prints, or null when the output names none.
 *
 * Rider takes the first word that parses as a semantic version. So a prefix such as `unity`, or a banner line above the
 * version, does not matter.
 */
@ApiStatus.Internal
fun parseUnityCliVersion(stdout: String): String? =
    stdout.split(' ', '\t', '\r', '\n').firstOrNull { SemVer.parseFromText(it) != null }

/**
 * Tells whether `unity --help` printed the help of the Unity CLI.
 *
 * The help must name its usage line, the command that updates the CLI, and the MCP server. Each check is a substring
 * check that ignores the case, so a new layout of the help does not matter.
 */
@ApiStatus.Internal
fun isUnityCliHelpOutput(stdout: String): Boolean =
    UNITY_CLI_USAGE_MARKERS.any { stdout.contains(it, ignoreCase = true) } &&
    UNITY_CLI_UPDATE_MARKERS.any { stdout.contains(it, ignoreCase = true) } &&
    stdout.contains(UNITY_CLI_MCP_MARKER, ignoreCase = true)

/**
 * The install method of the copy at [path], from the path alone.
 *
 * Rider checks [path] and [realPath], because a Homebrew link resolves into the Caskroom, and a WinGet alias does not
 * resolve at all. The CDN rule reads only [realPath]. The installer copies the binary into its folder, so a link there
 * points to the copy of another install.
 *
 * @param cdnDirectories the folders from [unityCliCdnDirectories], as they are and with their links resolved
 * @param dpkgListsUnityCli tells whether dpkg owns the `unity-cli` package, which separates apt from dnf
 * @param dnfIsInstalled tells whether dnf is on the machine. Without dnf and dpkg, Rider knows no package manager.
 */
@ApiStatus.Internal
fun unityCliInstallMethodFromPath(
    path: Path,
    realPath: Path,
    cdnDirectories: List<Path>,
    dpkgListsUnityCli: () -> Boolean,
    dnfIsInstalled: () -> Boolean,
): UnityCliInstallMethod {
    val paths = listOf(path, realPath)
    return when {
        paths.any(::isHomebrewPath) -> UnityCliInstallMethod.HOMEBREW
        paths.any(::isWingetPath) -> UnityCliInstallMethod.WINGET
        realPath.parent in cdnDirectories -> UnityCliInstallMethod.CDN
        paths.any(::isLinuxPackagePath) -> when {
            dpkgListsUnityCli() -> UnityCliInstallMethod.APT
            dnfIsInstalled() -> UnityCliInstallMethod.DNF
            else -> UnityCliInstallMethod.UNKNOWN
        }
        else -> UnityCliInstallMethod.UNKNOWN
    }
}

private fun isHomebrewPath(path: Path): Boolean {
    val names = path.map { it.toString() }
    val caskroom = names.indexOf("Caskroom")
    return caskroom >= 0 && names.getOrNull(caskroom + 1) == HOMEBREW_PACKAGE_DIRECTORY
}

// A user-scope copy lives under `Microsoft\WinGet` or `WindowsApps`, and a machine-scope copy under `Program Files\WinGet`.
private fun isWingetPath(path: Path): Boolean =
    path.any { it.toString().equals("WindowsApps", ignoreCase = true) || it.toString().equals("WinGet", ignoreCase = true) }

private fun isLinuxPackagePath(path: Path): Boolean =
    path.startsWith(LINUX_PACKAGE_ROOT) && !path.startsWith(LINUX_LOCAL_ROOT)

/** Tells whether the dpkg folder [dpkgInfo] lists the `unity-cli` package. */
@ApiStatus.Internal
fun dpkgListsUnityCli(dpkgInfo: Path): Boolean {
    return Files.isRegularFile(dpkgInfo.resolve(DPKG_PACKAGE_LIST)) || try {
        Files.newDirectoryStream(dpkgInfo, DPKG_MULTI_ARCH_PACKAGE_LIST).use { it.any() }
    }
    catch (_: IOException) {
        false
    }
}

internal fun unityCliVersionCommandLine(executable: Path): GeneralCommandLine =
    GeneralCommandLine(executable.toString(), "--version")

internal fun unityCliHelpCommandLine(executable: Path): GeneralCommandLine =
    GeneralCommandLine(executable.toString(), "--help")

@ApiStatus.Internal
fun parseUnityCliManifestVersion(body: String): String? =
    parse<ChannelManifest>(body)?.version?.takeIf { it.isNotBlank() }

private inline fun <reified T> parse(text: String): T? =
    try {
        gson.fromJson(text, T::class.java)
    }
    catch (_: JsonSyntaxException) {
        null
    }

internal fun UnityCliEnvironment.unityCliHome(): String? = environmentVariable(UNITY_CLI_HOME_VARIABLE)

internal fun UnityCliEnvironment.localAppData(): String? = environmentVariable(LOCAL_APP_DATA_VARIABLE)

@ApiStatus.Internal
fun UnityCliEnvironment.unityCliDirectoryOffPath(): Path? {
    if (findOnPath(UNITY_CLI_COMMAND) != null) return null
    return unityCliWellKnownCandidates(unityCliHome(), localAppData(), userHome(), unityCliExecutableName())
        .firstOrNull { !isUnityHubPrivateCopy(it) && isExecutable(it) }
        ?.parent
}

private data class ChannelManifest(val version: String?)
