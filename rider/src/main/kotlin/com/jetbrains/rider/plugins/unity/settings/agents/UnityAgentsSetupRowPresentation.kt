package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.util.text.StringUtil.escapeXmlEntities
import com.jetbrains.rider.plugins.unity.UnityBundle
import org.jetbrains.annotations.ApiStatus
import org.jetbrains.annotations.Nls

/** A link of the Unity row in the AI Agent Setup widget. */
@ApiStatus.Internal
enum class UnityAgentsSetupRowLink {
    INSTALL,
    INSTALL_ALL,
    DISMISS;

    val text: @Nls String
        get() = when (this) {
            INSTALL -> UnityBundle.message("unity.agents.setup.row.install")
            INSTALL_ALL -> UnityBundle.message("unity.agents.setup.row.install.all")
            DISMISS -> UnityBundle.message("unity.agents.setup.row.dismiss")
        }
}

/**
 * What the Unity row of the AI Agent Setup widget shows.
 *
 * @param progress replaces [links] while the setup chain runs.
 */
@ApiStatus.Internal
data class UnityAgentsSetupRowPresentation(
    val summary: @Nls String,
    val links: List<UnityAgentsSetupRowLink>,
    val progress: @Nls String?,
)

/**
 * Follows the design: Install for one missing step, Install All for two or more, and Dismiss beside it.
 * A dismissed row keeps the "Missing" text without links. "Installing…" replaces the links while the setup chain runs.
 * A complete setup shows "Enabled" and the Unity skill count.
 * A setup that an old Editor stops shows the minimum Editor version instead.
 *
 * @param missing the steps that are not done, in the order of [UnityAgentsSetupStep].
 * @param enabledSkillCount the number of enabled Unity skills. The row shows it only when [missing] is empty.
 * @param blockingEditor the old Editor that keeps the setup from its end. The row then names it instead of "Enabled".
 */
@ApiStatus.Internal
fun unityAgentsSetupRowPresentation(
    missing: List<UnityAgentsSetupStep>,
    dismissed: Boolean,
    installing: Boolean,
    enabledSkillCount: Int,
    blockingEditor: UnityOldEditor? = null,
): UnityAgentsSetupRowPresentation {
    if (missing.isEmpty()) {
        val summary =
            if (blockingEditor != null) {
                UnityBundle.message("unity.agents.setup.row.old.editor", escapeXmlEntities(blockingEditor.minimumVersion))
            }
            else {
                UnityBundle.message("unity.agents.setup.row.enabled", enabledSkillCount)
            }
        return UnityAgentsSetupRowPresentation(summary = summary, links = emptyList(), progress = null)
    }
    val summary = UnityBundle.message("unity.agents.setup.row.missing", missing.joinToString(", ") { it.rowText() })
    return when {
        installing -> UnityAgentsSetupRowPresentation(summary, emptyList(), unityAgentsSetupRowInstallingText())
        dismissed -> UnityAgentsSetupRowPresentation(summary, emptyList(), progress = null)
        else -> {
            val install = if (missing.size == 1) UnityAgentsSetupRowLink.INSTALL else UnityAgentsSetupRowLink.INSTALL_ALL
            UnityAgentsSetupRowPresentation(summary, listOf(install, UnityAgentsSetupRowLink.DISMISS), progress = null)
        }
    }
}

/** The text that replaces the links of the Unity row while the setup chain runs. */
@ApiStatus.Internal
fun unityAgentsSetupRowInstallingText(): @Nls String = UnityBundle.message("unity.agents.setup.row.installing")

private fun UnityAgentsSetupStep.rowText(): @Nls String = when (this) {
    UnityAgentsSetupStep.CLI -> UnityBundle.message("unity.agents.setup.row.step.cli")
    UnityAgentsSetupStep.PIPELINE_PACKAGE -> UnityBundle.message("unity.agents.setup.row.step.pipeline")
    UnityAgentsSetupStep.CLI_SKILL -> UnityBundle.message("unity.agents.setup.row.step.skill")
}
