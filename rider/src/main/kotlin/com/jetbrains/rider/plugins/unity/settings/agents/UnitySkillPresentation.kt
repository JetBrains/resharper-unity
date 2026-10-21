package com.jetbrains.rider.plugins.unity.settings.agents

import org.jetbrains.annotations.ApiStatus

@ApiStatus.Internal
enum class UnitySkillFailure {
    LAUNCH_FAILED,
    EXIT_CODE,
    NOT_CONFIRMED,
}

@ApiStatus.Internal
enum class UnitySkillOffer {
    GATED,
    CHECKING,
    UNREADABLE,
    INSTALL,
    INSTALLED,
    BUSY,
    FAILED,
}

@ApiStatus.Internal
data class UnitySkillPresentation(
    val gate: UnityMcpSectionState = UnityMcpSectionState.CHECKING,
    val packageInstalled: Boolean = false,
    val installs: List<UnitySkillInstall>? = null,
    val installing: Boolean = false,
    val failure: UnitySkillFailure? = null,
    val failureExitCode: Int? = null,
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
                    installs == null -> UnitySkillOffer.UNREADABLE
                    !packageInstalled -> UnitySkillOffer.GATED
                    else -> UnitySkillOffer.INSTALL
                }
            }
        }
}
