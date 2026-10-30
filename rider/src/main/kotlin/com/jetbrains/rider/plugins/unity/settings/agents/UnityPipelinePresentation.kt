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
    /** Rider could not read the latest version from the package registry. */
    REGISTRY_UNREACHABLE,

    /** Rider could not write `Packages/manifest.json`. */
    MANIFEST_WRITE,

    /** The manifest does not show the version that Rider wrote. */
    NOT_CONFIRMED,

    /** The project uses a Unity Editor older than the one that the package needs. */
    EDITOR_TOO_OLD,
}

@ApiStatus.Internal
enum class UnityPipelineOffer {
    GATED,
    CHECKING,
    UNREADABLE,
    INSTALL,
    INSTALLED,
    UPGRADE,
    EDITOR_TOO_OLD,
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
    val failedOperation: UnityPipelineOperation? = null,
    /** The Unity Editor version of the project, from `ProjectSettings/ProjectVersion.txt`. */
    val editorVersion: @NlsSafe String? = null,
    /** The oldest Unity Editor that the package supports. */
    val minimumEditorVersion: @NlsSafe String? = null,
) {
    /** The project uses an Editor that the package does not support, so Rider does not add the package. */
    val editorTooOld: Boolean get() = unityEditorIsOlder(editorVersion, minimumEditorVersion)

    val offer: UnityPipelineOffer
        get() = when {
            operation != null -> UnityPipelineOffer.BUSY
            else -> when (gate) {
                UnityMcpSectionState.NO_PROJECT,
                UnityMcpSectionState.NOT_TRUSTED,
                    -> UnityPipelineOffer.GATED
                UnityMcpSectionState.NO_CLI -> when {
                    manifest?.present == true -> UnityPipelineOffer.INSTALLED
                    editorTooOld -> UnityPipelineOffer.EDITOR_TOO_OLD
                    else -> UnityPipelineOffer.GATED
                }
                UnityMcpSectionState.CHECKING ->
                    if (failure != null) UnityPipelineOffer.FAILED else UnityPipelineOffer.CHECKING
                UnityMcpSectionState.READY -> when {
                    manifest?.present == false && editorTooOld -> UnityPipelineOffer.EDITOR_TOO_OLD
                    failure != null -> UnityPipelineOffer.FAILED
                    else -> offerInReadyProject(manifest, latestVersion, editorTooOld)
                }
            }
        }

    val needsPipelinePackage: Boolean? get() = manifest?.present?.not()

    /** The setup adds the package: the project lacks it, and its Editor supports it. */
    val setupAddsPackage: Boolean get() = needsPipelinePackage == true && !editorTooOld

    /** The old Editor that keeps the package out of the project, or null when the package can be added or is there. */
    val blockingEditor: UnityOldEditor?
        get() {
            if (!editorTooOld || manifest?.present != false) return null
            val minimum = minimumEditorVersion ?: return null
            val editor = editorVersion ?: return null
            return UnityOldEditor(minimum, editor)
        }
}

/** The project uses a Unity Editor older than the one that the Pipeline package needs. */
@ApiStatus.Internal
data class UnityOldEditor(val minimumVersion: @NlsSafe String, val editorVersion: @NlsSafe String)

private fun offerInReadyProject(manifest: UnityPipelineManifest?, latestVersion: String?, editorTooOld: Boolean): UnityPipelineOffer {
    if (manifest == null) return UnityPipelineOffer.UNREADABLE
    val installed = manifest.version ?: return UnityPipelineOffer.INSTALL
    if (!editorTooOld && latestVersion != null && unityPipelineHasNewerVersion(latestVersion, installed)) return UnityPipelineOffer.UPGRADE
    return UnityPipelineOffer.INSTALLED
}

/**
 * Unlike [unityCliHasNewerVersion], text that is not a version is never older.
 *
 * The manifest can name the package by a git URL or a `file:` path. Such a package is never older than the version of the registry.
 */
@ApiStatus.Internal
fun unityPipelineHasNewerVersion(latest: String, installed: String): Boolean {
    val latestSemVer = SemVer.parseFromText(latest) ?: return false
    val installedSemVer = SemVer.parseFromText(installed) ?: return false
    return latestSemVer > installedSemVer
}

/**
 * Tells whether the Unity Editor [editorVersion] is older than [minimumVersion].
 *
 * Both are Unity versions such as `2022.3.62f1` and `6000.0`. Only the major and the minor numbers count. Text that is
 * not a Unity version is never older.
 */
@ApiStatus.Internal
fun unityEditorIsOlder(editorVersion: String?, minimumVersion: String?): Boolean {
    val editor = editorVersion?.let(::unityMajorMinor) ?: return false
    val minimum = minimumVersion?.let(::unityMajorMinor) ?: return false
    return compareValuesBy(editor, minimum, { it.first }, { it.second }) < 0
}

/**
 * The newest release that [editorVersion] supports, or null when no release does.
 *
 * An unknown Editor version supports every release, so the result is then the release of [UnityPipelineRegistry.latest].
 */
@ApiStatus.Internal
fun unityPipelineReleaseFor(registry: UnityPipelineRegistry, editorVersion: String?): UnityPipelineRelease? =
    registry.releases
        .filter { !unityEditorIsOlder(editorVersion, it.minimumEditorVersion) }
        .maxByOrNull { SemVer.parseFromText(it.version)!! }

/**
 * The oldest Editor that any release supports, or null when a release names no minimum.
 *
 * The page names it when no release supports the project Editor.
 */
@ApiStatus.Internal
fun unityPipelineLowestMinimumEditor(registry: UnityPipelineRegistry): String? {
    val minimums = registry.releases.map { it.minimumEditorVersion ?: return null }
    return minimums.minWithOrNull(compareBy({ unityMajorMinor(it)?.first }, { unityMajorMinor(it)?.second }))
}

private fun unityMajorMinor(version: String): Pair<Int, Int>? {
    val parts = version.trim().split('.')
    val major = parts.getOrNull(0)?.toIntOrNull() ?: return null
    val minor = parts.getOrNull(1)?.takeWhile { it.isDigit() }?.toIntOrNull() ?: return null
    return major to minor
}

@ApiStatus.Internal
sealed interface UnityPipelinePackageOutcome {
    data object Ready : UnityPipelinePackageOutcome
    data object Declined : UnityPipelinePackageOutcome
    data class Failed(val failure: UnityMcpFailure) : UnityPipelinePackageOutcome
}

