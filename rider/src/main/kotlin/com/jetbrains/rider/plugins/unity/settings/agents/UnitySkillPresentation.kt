package com.jetbrains.rider.plugins.unity.settings.agents

import org.jetbrains.annotations.ApiStatus

@ApiStatus.Internal
enum class UnitySkillFailure {
    /** Rider could not download or read the skills archive. */
    DOWNLOAD,

    /** Rider could not write the skill folder into the project. */
    WRITE,

    /** The skills archive has no `skills/unity-cli` folder. */
    NOT_CONFIRMED,
}

@ApiStatus.Internal
enum class UnitySkillOffer {
    GATED,
    CHECKING,
    INSTALL,
    INSTALLED,
    BUSY,
    FAILED,
}

@ApiStatus.Internal
data class UnitySkillPresentation(
    val gate: UnityMcpSectionState = UnityMcpSectionState.CHECKING,
    val packageInstalled: Boolean = false,
    /** The old Editor that keeps the Pipeline package, and so the skill, out of the project. */
    val blockingEditor: UnityOldEditor? = null,
    val installs: List<UnitySkillInstall>? = null,
    val installing: Boolean = false,
    val failure: UnitySkillFailure? = null,
    /** The target of the install that failed. Try Again installs for it again. */
    val failedTarget: UnitySkillTarget? = null,
) {
    val offer: UnitySkillOffer
        get() = when {
            installing -> UnitySkillOffer.BUSY
            else -> when (gate) {
                UnityMcpSectionState.NO_PROJECT -> UnitySkillOffer.GATED
                UnityMcpSectionState.NOT_TRUSTED -> UnitySkillOffer.GATED
                UnityMcpSectionState.NO_CLI -> UnitySkillOffer.GATED
                UnityMcpSectionState.CHECKING -> when {
                    failure != null -> UnitySkillOffer.FAILED
                    !installs.isNullOrEmpty() -> UnitySkillOffer.INSTALLED
                    else -> UnitySkillOffer.CHECKING
                }
                UnityMcpSectionState.READY -> when {
                    failure != null -> UnitySkillOffer.FAILED
                    !installs.isNullOrEmpty() -> UnitySkillOffer.INSTALLED
                    !packageInstalled -> UnitySkillOffer.GATED
                    else -> UnitySkillOffer.INSTALL
                }
            }
        }
}
