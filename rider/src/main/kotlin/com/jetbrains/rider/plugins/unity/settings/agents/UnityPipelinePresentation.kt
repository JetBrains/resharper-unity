package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.util.NlsSafe
import com.intellij.util.text.SemVer
import org.jetbrains.annotations.ApiStatus

@ApiStatus.Internal
enum class UnityPipelineOperation {
    INSTALLING,
    UPGRADING,
}

@ApiStatus.Internal
enum class UnityPipelineFailure {
    LAUNCH_FAILED,
    EXIT_CODE,
    NOT_CONFIRMED,
}

@ApiStatus.Internal
enum class UnityPipelineOffer {
    GATED,
    CHECKING,
    UNREADABLE,
    INSTALL,
    INSTALLED,
    UPGRADE,
    BUSY,
    FAILED,
}

@ApiStatus.Internal
data class UnityPipelinePresentation(
    val gate: UnityMcpSectionState = UnityMcpSectionState.CHECKING,
    val manifestPath: @NlsSafe String? = null,
    /** Null when `Packages/manifest.json` did not answer. A missing file answers an absent package. */
    val manifest: UnityPipelineManifest? = null,
    val latestVersion: @NlsSafe String? = null,
    val operation: UnityPipelineOperation? = null,
    val failure: UnityPipelineFailure? = null,
    val failureExitCode: Int? = null,
    val failureCliError: @NlsSafe String? = null,
    val failedOperation: UnityPipelineOperation? = null,
) {
    val offer: UnityPipelineOffer
        get() = when {
            operation != null -> UnityPipelineOffer.BUSY
            else -> when (gate) {
                UnityMcpSectionState.NO_PROJECT,
                UnityMcpSectionState.NOT_TRUSTED,
                    -> UnityPipelineOffer.GATED
                UnityMcpSectionState.NO_CLI ->
                    if (manifest?.present == true) UnityPipelineOffer.INSTALLED else UnityPipelineOffer.GATED
                UnityMcpSectionState.CHECKING ->
                    if (failure != null) UnityPipelineOffer.FAILED else UnityPipelineOffer.CHECKING
                UnityMcpSectionState.READY ->
                    if (failure != null) UnityPipelineOffer.FAILED else offerInReadyProject(manifest, latestVersion)
            }
        }

    val needsPipelinePackage: Boolean? get() = manifest?.present?.not()
}

private fun offerInReadyProject(manifest: UnityPipelineManifest?, latestVersion: String?): UnityPipelineOffer {
    if (manifest == null) return UnityPipelineOffer.UNREADABLE
    val installed = manifest.version ?: return UnityPipelineOffer.INSTALL
    if (latestVersion != null && unityPipelineHasNewerVersion(latestVersion, installed)) return UnityPipelineOffer.UPGRADE
    return UnityPipelineOffer.INSTALLED
}

/**
 * Unlike [unityCliHasNewerVersion], text that is not a version is never older.
 *
 * The manifest can name the package by a git URL or a `file:` path, and the Unity CLI reports such a package as the latest.
 */
@ApiStatus.Internal
fun unityPipelineHasNewerVersion(latest: String, installed: String): Boolean {
    val latestSemVer = SemVer.parseFromText(latest) ?: return false
    val installedSemVer = SemVer.parseFromText(installed) ?: return false
    return latestSemVer > installedSemVer
}

@ApiStatus.Internal
sealed interface UnityPipelinePackageOutcome {
    data object Ready : UnityPipelinePackageOutcome
    data object Declined : UnityPipelinePackageOutcome
    data class Failed(val failure: UnityMcpFailure, val exitCode: Int?) : UnityPipelinePackageOutcome
}

