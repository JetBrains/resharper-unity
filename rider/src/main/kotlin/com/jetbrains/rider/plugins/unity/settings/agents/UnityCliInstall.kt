package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.OSProcessHandler
import com.intellij.openapi.diagnostic.Logger
import com.intellij.util.io.HttpRequests
import com.intellij.util.system.LowLevelLocalMachineAccess
import com.intellij.util.system.OS
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.IOException
import java.net.HttpURLConnection
import java.nio.file.Path

private val logger = Logger.getInstance("#com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstall")

const val UNITY_CLI_CDN_BASE_URL: String = "https://public-cdn.cloud.unity3d.com/hub/prod/cli/"

const val UNITY_CLI_STABLE_MANIFEST: String = "latest.json"

const val UNITY_CLI_BETA_CHANNEL: String = "beta"

const val UNITY_CLI_STABLE_CHANNEL: String = "stable"

const val UNITY_CLI_CHANNEL_VARIABLE: String = "UNITY_CLI_CHANNEL"

const val UNITY_CLI_CDN_BASE_VARIABLE: String = "UNITY_CLI_CDN_BASE"

const val BASH_ENV_VARIABLE: String = "BASH_ENV"

enum class UnityCliInstallerScript(val fileName: String) {
    SHELL("install.sh"),
    POWERSHELL("install.ps1");

    companion object {
        @OptIn(LowLevelLocalMachineAccess::class)
        fun forCurrentOs(): UnityCliInstallerScript? = when (OS.CURRENT) {
            OS.Windows -> POWERSHELL
            OS.macOS, OS.Linux -> SHELL
            else -> null
        }
    }
}

fun unityCliCdnUrl(cdnBaseUrl: String, fileName: String): String =
    "${cdnBaseUrl.trimEnd('/')}/$fileName"

fun unityCliInstallerCommandLine(
    script: UnityCliInstallerScript,
    installer: Path,
    channel: String?,
): GeneralCommandLine {
    val commandLine = when (script) {
        // Never `bash -i`. An interactive shell reads the profile of the user. It could change what runs.
        UnityCliInstallerScript.SHELL -> GeneralCommandLine("bash", installer.toString())
        UnityCliInstallerScript.POWERSHELL -> GeneralCommandLine(
            "powershell.exe",
            "-NoProfile",
            "-NonInteractive",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            installer.toString(),
        )
    }
    // An empty value reads as unset because install.sh expands the variable with `:-`. PowerShell
    // reads no such variable, but scrubbing both keeps one rule.
    commandLine.withEnvironment(UNITY_CLI_CDN_BASE_VARIABLE, "")
    commandLine.withEnvironment(BASH_ENV_VARIABLE, "")
    if (channel != null) commandLine.withEnvironment(UNITY_CLI_CHANNEL_VARIABLE, channel)
    OSProcessHandler.deleteFileOnTermination(commandLine, installer.toFile())
    return commandLine
}

data class UnityCliInstallResult(val failure: UnityCliFailure?, val exitCode: Int?)

suspend fun runUnityCliInstall(
    script: UnityCliInstallerScript,
    cdnBaseUrl: String,
    forcedChannel: String,
    download: suspend (url: String) -> Path?,
    runInstaller: suspend (GeneralCommandLine) -> Int?,
    findCli: suspend () -> Boolean,
): UnityCliInstallResult {
    val installer = download(unityCliCdnUrl(cdnBaseUrl, script.fileName))
        ?: return UnityCliInstallResult(UnityCliFailure.DOWNLOAD, null)

    val channel = probeUnityCliChannel(cdnBaseUrl, forcedChannel)
    val exitCode = runInstaller(unityCliInstallerCommandLine(script, installer, channel))
    return UnityCliInstallResult(unityCliInstallOutcome(exitCode, findCli()), exitCode)
}

fun unityCliInstallOutcome(exitCode: Int?, cliFound: Boolean): UnityCliFailure? = when {
    // The process never started, so there is no code.
    // This branch runs first because a null code must not read as nonzero.
    exitCode == null -> UnityCliFailure.LAUNCH_FAILED
    exitCode != 0 -> UnityCliFailure.INSTALLER_EXIT_CODE
    // The script can exit 0 and leave nothing Rider can find. The check for the CLI catches this case.
    !cliFound -> UnityCliFailure.VERIFICATION
    else -> null
}

suspend fun probeUnityCliChannel(cdnBaseUrl: String, forcedChannel: String): String? {
    if (forcedChannel.isNotBlank()) {
        return if (forcedChannel == UNITY_CLI_STABLE_CHANNEL) null else forcedChannel
    }

    val url = unityCliCdnUrl(cdnBaseUrl, UNITY_CLI_STABLE_MANIFEST)
    return withContext(Dispatchers.IO) {
        try {
            HttpRequests.request(url).throwStatusCodeException(true).readString(null)
            null
        }
        catch (e: HttpRequests.HttpStatusException) {
            if (e.statusCode != HttpURLConnection.HTTP_NOT_FOUND) {
                logger.warn("The stable channel of the Unity CLI answered ${e.statusCode}. Rider asks for beta")
            }
            UNITY_CLI_BETA_CHANNEL
        }
        catch (e: IOException) {
            logger.warn("Rider could not read the stable channel of the Unity CLI. It asks for beta", e)
            UNITY_CLI_BETA_CHANNEL
        }
    }
}
