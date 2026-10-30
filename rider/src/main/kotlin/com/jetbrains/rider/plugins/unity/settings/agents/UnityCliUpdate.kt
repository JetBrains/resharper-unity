package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.openapi.diagnostic.fileLogger
import org.jetbrains.annotations.ApiStatus
import java.nio.file.Path

private val logger = fileLogger()

private const val SELF_UPDATE_SUBCOMMAND = "self-update"
/** The name of [SELF_UPDATE_SUBCOMMAND] before 1.0.0-beta.8. Later versions keep it as an alias. */
private const val UPGRADE_SUBCOMMAND = "upgrade"
private const val SKIP_CONFIRMATION_FLAG = "-y"
private const val HOMEBREW_PACKAGE = "unity-cli"
private const val WINGET_PACKAGE = "Unity.CLI"

/** The package name in `packageManagers` of the channel manifest. */
private const val LINUX_PACKAGE = "unity-cli"

/** The commands that the page shows for a copy that only the user can update, because they need `sudo`. Rider never runs them. */
private val CLIPBOARD_COMMANDS: Map<UnityCliInstallMethod, String> = mapOf(
    UnityCliInstallMethod.APT to "sudo apt-get install --only-upgrade $LINUX_PACKAGE",
    UnityCliInstallMethod.DNF to "sudo dnf upgrade $LINUX_PACKAGE",
)

/**
 * The command Rider runs to update a copy from a package manager, or null when Rider runs none.
 *
 * | Install method | What Rider runs |
 * |---|---|
 * | [UnityCliInstallMethod.CDN] | nothing here, because [runUnityCliUpdateChain] updates the copy |
 * | [UnityCliInstallMethod.HOMEBREW] | `brew upgrade unity-cli` |
 * | [UnityCliInstallMethod.WINGET] | `winget upgrade Unity.CLI` |
 * | [UnityCliInstallMethod.APT], [UnityCliInstallMethod.DNF] | nothing, because the command needs `sudo` |
 */
@ApiStatus.Internal
fun unityCliUpdateCommandLine(installMethod: UnityCliInstallMethod): GeneralCommandLine? = when (installMethod) {
    UnityCliInstallMethod.HOMEBREW -> GeneralCommandLine("brew", "upgrade", HOMEBREW_PACKAGE)
    UnityCliInstallMethod.WINGET -> GeneralCommandLine("winget", "upgrade", WINGET_PACKAGE)
    UnityCliInstallMethod.CDN, UnityCliInstallMethod.APT, UnityCliInstallMethod.DNF, UnityCliInstallMethod.UNKNOWN -> null
}

@ApiStatus.Internal
fun unityCliUpdateCommandForClipboard(installMethod: UnityCliInstallMethod): String? = CLIPBOARD_COMMANDS[installMethod]

@ApiStatus.Internal
fun unityCliUpdateOutcome(exitCode: Int?): UnityCliFailure? = when {
    // The process never started, so there is no code.
    // This branch runs first, because a null code must not read as nonzero.
    exitCode == null -> UnityCliFailure.LAUNCH_FAILED
    exitCode != 0 -> UnityCliFailure.UPDATE_EXIT_CODE
    else -> null
}

/**
 * Updates the CDN copy at [cliPath]. Rider never uninstalls the copy, so a failed update leaves the old copy in place.
 *
 * 1. Reads the latest version. A failure stops the chain before it runs anything.
 * 2. Runs `unity self-update -y`. The chain stops when the copy is then current.
 * 3. Runs `unity upgrade -y`, which a copy older than 1.0.0-beta.8 knows instead.
 *
 * @param runCommand runs a command line, and gives its exit code, or null when it did not start
 * @param findCli finds the CLI again, and gives its version, or null when no copy is left
 */
@ApiStatus.Internal
suspend fun runUnityCliUpdateChain(
    cliPath: Path,
    readLatestVersion: suspend () -> String?,
    runCommand: suspend (GeneralCommandLine) -> Int?,
    findCli: suspend () -> String?,
): UnityCliInstallResult {
    val latest = readLatestVersion() ?: return UnityCliInstallResult(UnityCliFailure.MANIFEST_READ, null)

    val selfUpdateExitCode = runCommand(unityCliSelfCommandLine(cliPath, SELF_UPDATE_SUBCOMMAND))
    if (isCurrent(findCli(), latest)) return UnityCliInstallResult(null, selfUpdateExitCode)
    logger.warn(
        "unity $SELF_UPDATE_SUBCOMMAND exited with $selfUpdateExitCode and left an old copy, so Rider runs unity $UPGRADE_SUBCOMMAND",
    )

    val upgradeExitCode = runCommand(unityCliSelfCommandLine(cliPath, UPGRADE_SUBCOMMAND))
    val failure = if (isCurrent(findCli(), latest)) null else UnityCliFailure.UPDATE_FAILED
    return UnityCliInstallResult(failure, upgradeExitCode)
}

private fun isCurrent(installed: String?, latest: String): Boolean =
    installed != null && !unityCliHasNewerVersion(latest, installed)

// The variable replaces the base of every download the CLI makes, so a value in the environment of the user would
// redirect the update.
private fun unityCliSelfCommandLine(cliPath: Path, subcommand: String): GeneralCommandLine =
    GeneralCommandLine(cliPath.toString(), subcommand, SKIP_CONFIRMATION_FLAG).withEnvironment(UNITY_CLI_CDN_BASE_VARIABLE, "")
