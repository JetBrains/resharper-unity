package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.util.text.SemVer
import java.nio.file.Path

enum class UnityCliInstallMethod {
    /** The install script of Unity, or a direct download. The CLI updates the copy itself. */
    CDN,
    HOMEBREW,
    WINGET,
    APT,
    DNF,
    UNKNOWN;

    val updatableFromRider: Boolean
        get() = this == CDN || this == HOMEBREW || this == WINGET
}

enum class UnityCliOperation { INSTALLING, UPDATING }

enum class UnityCliFailure {
    DOWNLOAD,
    LAUNCH_FAILED,
    INSTALLER_EXIT_CODE,
    UPDATE_EXIT_CODE,
    VERIFICATION,
}

enum class UnityCliOffer {
    CHECKING,
    NOT_TRUSTED,
    INSTALL,
    CURRENT,
    LATEST_VERSION_UNKNOWN,
    UPDATE,
    SHOW_UPDATE_COMMAND,
    BUSY,
    FAILED,
}

internal fun unityCliHasNewerVersion(latest: String, installed: String): Boolean {
    val latestSemVer = SemVer.parseFromText(latest)
    val installedSemVer = SemVer.parseFromText(installed)
    if (latestSemVer == null || installedSemVer == null) return latest != installed
    return latestSemVer > installedSemVer
}

data class UnityCliPresentation(
    val detected: Boolean = false,
    val installedVersion: String? = null,
    val location: Path? = null,
    val installMethod: UnityCliInstallMethod = UnityCliInstallMethod.UNKNOWN,
    val latestVersion: String? = null,
    val updateCommand: String? = null,
    val foreignBinaryOnPath: Path? = null,
    val projectTrusted: Boolean = true,
    val operation: UnityCliOperation? = null,
    val operationFraction: Double? = null,
    val failure: UnityCliFailure? = null,
    val failureExitCode: Int? = null,
    val failedOperation: UnityCliOperation? = null,
    /** Names the failure that this presentation shows. [UnityCliStatusProvider.clearFailure] takes it. */
    val failureId: Long? = null,
) {
    val offer: UnityCliOffer
        get() = when {
            operation != null -> UnityCliOffer.BUSY
            failure != null -> UnityCliOffer.FAILED
            !projectTrusted && offerInTrustedProject != UnityCliOffer.SHOW_UPDATE_COMMAND -> UnityCliOffer.NOT_TRUSTED
            !detected -> UnityCliOffer.CHECKING
            else -> offerInTrustedProject
        }

    private val offerInTrustedProject: UnityCliOffer
        get() = when {
            installedVersion == null -> UnityCliOffer.INSTALL
            latestVersion == null -> UnityCliOffer.LATEST_VERSION_UNKNOWN
            !unityCliHasNewerVersion(latestVersion, installedVersion) -> UnityCliOffer.CURRENT
            installMethod.updatableFromRider -> UnityCliOffer.UPDATE
            else -> UnityCliOffer.SHOW_UPDATE_COMMAND
        }
}
