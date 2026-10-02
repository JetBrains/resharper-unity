package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.execution.configurations.GeneralCommandLine
import java.nio.file.InvalidPathException
import java.nio.file.Path

private const val SELF_UPDATE_SUBCOMMAND = "self-update"
private const val UPGRADE_SUBCOMMAND = "upgrade"
private const val SKIP_CONFIRMATION_FLAG = "-y"
private const val HOMEBREW_PACKAGE = "unity-cli"
private const val WINGET_PACKAGE = "Unity.CLI"

private val SELF_UPDATE_SUBCOMMANDS: Set<String> = setOf(SELF_UPDATE_SUBCOMMAND, UPGRADE_SUBCOMMAND)

private val CLIPBOARD_PROGRAMS: Map<UnityCliInstallMethod, Set<String>> = mapOf(
    UnityCliInstallMethod.APT to setOf("apt", "apt-get"),
    UnityCliInstallMethod.DNF to setOf("dnf"),
)
private val SHELL_CHARACTERS: Set<Char> = setOf('|', '&', ';', '<', '>', '(', ')', '$', '`', '"', '\'', '\n', '\r')

fun splitUnityCliUpdateCommand(updateCommand: String): List<String>? {
    if (updateCommand.any { it in SHELL_CHARACTERS }) return null
    val argv = updateCommand.trim().split(Regex("\\s+")).filter { it.isNotEmpty() }
    return argv.ifEmpty { null }
}

internal fun programName(token: String): String? {
    val path = try {
        Path.of(token)
    } catch (_: InvalidPathException) {
        return null
    }
    if (path.nameCount != 1 || path.isAbsolute) return null
    return path.toString().lowercase().removeSuffix(".exe")
}

/**
 * The subcommand that makes the CLI update itself.
 *
 * The CLI named this `upgrade` until 1.0.0-beta.8, which renamed it to `self-update` and kept
 * `upgrade` as an alias. An older copy therefore answers `unknown command 'self-update'` and exits
 * 2. `diagnose update` reports the name the copy answers to, so Rider takes it from there.
 *
 * The allowlist keeps Rider from running a word the CLI invents later, and `upgrade` is the
 * fallback, because every measured version runs it.
 */
fun unityCliSelfUpdateSubcommand(updateCommand: String?): String {
    val argv = updateCommand?.let { splitUnityCliUpdateCommand(it) } ?: return UPGRADE_SUBCOMMAND
    val reported = argv.getOrNull(1) ?: return UPGRADE_SUBCOMMAND
    return if (reported in SELF_UPDATE_SUBCOMMANDS) reported else UPGRADE_SUBCOMMAND
}

/**
 * The command Rider runs to update the CLI, or null when Rider must not act.
 *
 * Rider branches on the install method, because no single mechanism updates every one, and running
 * the installer again would install a second copy beside an existing package-manager one.
 *
 * | Install method | What Rider runs |
 * |---|---|
 * | [UnityCliInstallMethod.CDN] | `unity <subcommand> -y`, from [unityCliSelfUpdateSubcommand] |
 * | [UnityCliInstallMethod.HOMEBREW] | `brew upgrade unity-cli` |
 * | [UnityCliInstallMethod.WINGET] | `winget upgrade Unity.CLI` |
 * | [UnityCliInstallMethod.APT], [UnityCliInstallMethod.DNF] | nothing, because the command needs `sudo` |
 *
 * @param updateCommand the command the CLI reported, which names the CDN subcommand and nothing else
 */
fun unityCliUpdateCommandLine(
    installMethod: UnityCliInstallMethod,
    cliPath: Path,
    updateCommand: String?,
): GeneralCommandLine? = when (installMethod) {
    UnityCliInstallMethod.CDN ->
        GeneralCommandLine(cliPath.toString(), unityCliSelfUpdateSubcommand(updateCommand), SKIP_CONFIRMATION_FLAG)
            .withEnvironment(UNITY_CLI_CDN_BASE_VARIABLE, "")

    UnityCliInstallMethod.HOMEBREW -> GeneralCommandLine("brew", "upgrade", HOMEBREW_PACKAGE)
    UnityCliInstallMethod.WINGET -> GeneralCommandLine("winget", "upgrade", WINGET_PACKAGE)

    UnityCliInstallMethod.APT, UnityCliInstallMethod.DNF, UnityCliInstallMethod.UNKNOWN -> null
}

fun unityCliUpdateCommandForClipboard(installMethod: UnityCliInstallMethod, updateCommand: String?): String? {
    val argv = updateCommand?.let { splitUnityCliUpdateCommand(it) } ?: return null
    val allowed = CLIPBOARD_PROGRAMS[installMethod] ?: return null
    val program = (if (argv.first() == "sudo") argv.getOrNull(1) else argv.first()) ?: return null
    if (programName(program) !in allowed) return null
    return updateCommand
}

fun unityCliUpdateOutcome(exitCode: Int?): UnityCliFailure? = when {
    // The process never started, so there is no code.
    // This branch runs first, because a null code must not read as nonzero.
    exitCode == null -> UnityCliFailure.LAUNCH_FAILED
    exitCode != 0 -> UnityCliFailure.UPDATE_EXIT_CODE
    else -> null
}
