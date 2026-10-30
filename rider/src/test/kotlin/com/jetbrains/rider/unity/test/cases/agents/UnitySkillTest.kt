package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsSetupStep
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpSectionState
import com.jetbrains.rider.plugins.unity.settings.agents.UnityOldEditor
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillClient
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillInstall
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillOffer
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillPresentation
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillTarget
import com.jetbrains.rider.plugins.unity.settings.agents.deleteWithEmptyParent
import com.jetbrains.rider.plugins.unity.settings.agents.unityAgentsMissingSteps
import com.jetbrains.rider.plugins.unity.settings.agents.unityAgentsSetupPrompt
import com.jetbrains.rider.plugins.unity.settings.agents.unitySkillDirectory
import com.jetbrains.rider.plugins.unity.settings.agents.unitySkillGateMessage
import com.jetbrains.rider.plugins.unity.settings.agents.unitySkillInstalls
import com.jetbrains.rider.plugins.unity.settings.agents.unitySkillOpenTargets
import com.jetbrains.rider.plugins.unity.settings.agents.unitySkillPlacesMessage
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.io.TempDir
import java.nio.file.Files
import java.nio.file.Path

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnitySkillTest {
    private val solution: Path = Path.of("/work/game")
    private val userHome: Path = Path.of("/home/dev")

    @Test
    fun `deleteUnitySkill deletes the disabled directory when it becomes empty`(@TempDir tempDir: Path) {
        val disabled = Files.createDirectories(tempDir.resolve(".agents/skills.disabled/unity-cli"))
        Files.writeString(disabled.resolve("SKILL.md"), "skill")

        deleteWithEmptyParent(disabled)

        assertFalse(Files.exists(disabled.parent))
    }

    @Test
    fun `deleteUnitySkill keeps the disabled directory that holds other skills`(@TempDir tempDir: Path) {
        val disabled = Files.createDirectories(tempDir.resolve(".agents/skills.disabled/unity-cli"))
        Files.writeString(disabled.resolve("SKILL.md"), "skill")
        val other = Files.createDirectories(tempDir.resolve(".agents/skills.disabled/other-skill"))

        deleteWithEmptyParent(disabled)

        assertFalse(Files.exists(disabled))
        assertTrue(Files.isDirectory(other))
    }

    @Test
    fun `unitySkillInstalls finds the six skill folders with their client and scope`() {
        val files = setOf(
            solution.resolve(".agents/skills/unity-cli/SKILL.md"),
            solution.resolve(".claude/skills/unity-cli/SKILL.md"),
            solution.resolve(".github/skills/unity-cli/SKILL.md"),
            userHome.resolve(".agents/skills/unity-cli/SKILL.md"),
            userHome.resolve(".claude/skills/unity-cli/SKILL.md"),
            userHome.resolve(".copilot/skills/unity-cli/SKILL.md"),
        )

        assertEquals(
            listOf(
                UnitySkillInstall("codex", local = true, solution.resolve(".agents/skills/unity-cli")),
                UnitySkillInstall("claude-code", local = true, solution.resolve(".claude/skills/unity-cli")),
                UnitySkillInstall("vscode", local = true, solution.resolve(".github/skills/unity-cli")),
                UnitySkillInstall("codex", local = false, userHome.resolve(".agents/skills/unity-cli")),
                UnitySkillInstall("claude-code", local = false, userHome.resolve(".claude/skills/unity-cli")),
                UnitySkillInstall("vscode", local = false, userHome.resolve(".copilot/skills/unity-cli")),
            ),
            unitySkillInstalls(solution, userHome, isFile = { it in files }),
        )
    }

    @Test
    fun `unitySkillInstalls skips a folder without SKILL md and another skill name`() {
        val files = setOf(
            solution.resolve(".claude/skills/unity-cli/references/a.md"),
            solution.resolve(".agents/skills/other/SKILL.md"),
        )

        assertEquals(emptyList<UnitySkillInstall>(), unitySkillInstalls(solution, userHome, isFile = { it in files }))
    }

    @Test
    fun `unitySkillInstalls gives an empty list for an empty tree`() {
        assertEquals(emptyList<UnitySkillInstall>(), unitySkillInstalls(solution, userHome, isFile = { false }))
    }

    @Test
    fun `unitySkillDirectory gives the unity-cli folder of each client in the solution`() {
        assertEquals(solution.resolve(".agents/skills/unity-cli"), unitySkillDirectory(UnitySkillClient.CODEX, solution))
        assertEquals(solution.resolve(".claude/skills/unity-cli"), unitySkillDirectory(UnitySkillClient.CLAUDE_CODE, solution))
    }

    @Test
    fun `offer rule 1 - installing produces BUSY`() {
        val presentation = UnitySkillPresentation(installing = true)
        assertEquals(UnitySkillOffer.BUSY, presentation.offer)
    }

    @Test
    fun `offer rule 2 - gate NO_PROJECT or NOT_TRUSTED produces GATED`() {
        val noProject = UnitySkillPresentation(gate = UnityMcpSectionState.NO_PROJECT)
        assertEquals(UnitySkillOffer.GATED, noProject.offer)

        val notTrusted = UnitySkillPresentation(gate = UnityMcpSectionState.NOT_TRUSTED)
        assertEquals(UnitySkillOffer.GATED, notTrusted.offer)
    }

    @Test
    fun `offer rule 3 - installs not null and not empty produces INSTALLED`() {
        val presentation = UnitySkillPresentation(
            gate = UnityMcpSectionState.READY,
            installs = listOf(UnitySkillInstall("codex", local = true, Path.of("/path"))),
        )
        assertEquals(UnitySkillOffer.INSTALLED, presentation.offer)
    }

    @Test
    fun `failure with installs produces FAILED`() {
        val presentation = UnitySkillPresentation(
            gate = UnityMcpSectionState.READY,
            installs = listOf(UnitySkillInstall("codex", local = true, Path.of("/path"))),
            failure = UnitySkillFailure.WRITE,
        )
        assertEquals(UnitySkillOffer.FAILED, presentation.offer)
    }

    @Test
    fun `offer rule 4 - gate NO_CLI produces GATED even with failure`() {
        val presentation = UnitySkillPresentation(gate = UnityMcpSectionState.NO_CLI, installs = emptyList())
        assertEquals(UnitySkillOffer.GATED, presentation.offer)

        val failed = UnitySkillPresentation(
            gate = UnityMcpSectionState.NO_CLI,
            failure = UnitySkillFailure.WRITE,
        )
        assertEquals(UnitySkillOffer.GATED, failed.offer)
    }

    @Test
    fun `offer rule 5 - gate CHECKING produces FAILED if failure != null else CHECKING`() {
        val checking = UnitySkillPresentation(gate = UnityMcpSectionState.CHECKING, installs = emptyList())
        assertEquals(UnitySkillOffer.CHECKING, checking.offer)

        val failed = UnitySkillPresentation(
            gate = UnityMcpSectionState.CHECKING,
            installs = emptyList(),
            failure = UnitySkillFailure.WRITE,
        )
        assertEquals(UnitySkillOffer.FAILED, failed.offer)
    }

    @Test
    fun `offer rule 6 - gate READY evaluates failure, installs, and packageInstalled`() {
        val failed = UnitySkillPresentation(
            gate = UnityMcpSectionState.READY,
            failure = UnitySkillFailure.DOWNLOAD,
        )
        assertEquals(UnitySkillOffer.FAILED, failed.offer)

        val installsNull = UnitySkillPresentation(
            gate = UnityMcpSectionState.READY,
            installs = null,
        )
        assertEquals(UnitySkillOffer.GATED, installsNull.offer)

        val packageNotInstalled = UnitySkillPresentation(
            gate = UnityMcpSectionState.READY,
            installs = emptyList(),
            packageInstalled = false,
        )
        assertEquals(UnitySkillOffer.GATED, packageNotInstalled.offer)

        val install = UnitySkillPresentation(
            gate = UnityMcpSectionState.READY,
            installs = emptyList(),
            packageInstalled = true,
        )
        assertEquals(UnitySkillOffer.INSTALL, install.offer)
    }

    @Test
    fun `unitySkillPlacesMessage formats distinct locations and escapes markup`() {
        val path1 = Path.of("/projects/game/.agents/skills/unity-cli")
        val path2 = Path.of("/Users/dev/.agents/skills/unity-cli")
        val twoEntries = listOf(
            UnitySkillInstall("codex", local = true, path1),
            UnitySkillInstall("claude-code", local = false, path2),
        )
        assertEquals(
            "(Codex, project; GitHub Copilot, project; Junie, project; Claude Code, global)",
            unitySkillPlacesMessage(twoEntries),
        )

        val duplicates = listOf(
            UnitySkillInstall("codex", local = false, path1),
            UnitySkillInstall("codex", local = false, path2),
        )
        assertEquals("(Codex, global)", unitySkillPlacesMessage(duplicates))

        val markup = listOf(
            UnitySkillInstall("<custom-client>", local = true, path1),
        )
        assertEquals("(&lt;custom-client&gt;, project)", unitySkillPlacesMessage(markup))
    }

    @Test
    fun `unitySkillPlacesMessage names every agent that reads the project skill folder`() {
        val agentsPath = Path.of("/projects/game/.agents/skills/unity-cli")
        val claudePath = Path.of("/projects/game/.claude/skills/unity-cli")
        assertEquals(
            "(Codex, project; GitHub Copilot, project; Junie, project)",
            unitySkillPlacesMessage(listOf(UnitySkillInstall("codex", local = true, agentsPath))),
        )
        assertEquals(
            "(Claude Code, project; GitHub Copilot, project)",
            unitySkillPlacesMessage(listOf(UnitySkillInstall("claude-code", local = true, claudePath))),
        )
    }

    @Test
    fun `unitySkillPlacesMessage is null when every agent reads the project skill`() {
        val installs = listOf(
            UnitySkillInstall("codex", local = true, Path.of("/projects/game/.agents/skills/unity-cli")),
            UnitySkillInstall("claude-code", local = true, Path.of("/projects/game/.claude/skills/unity-cli")),
        )
        assertNull(unitySkillPlacesMessage(installs))
    }

    @Test
    fun `unityAgentsSetupPrompt names only the missing steps`() {
        assertEquals(
            UnityBundle.message("unity.agents.setup.suggest.title") to
                UnityBundle.message("unity.agents.setup.suggest.content.cli"),
            unityAgentsSetupPrompt(cliMissing = true, packageMissing = true, skillMissing = true),
        )
        assertEquals(
            UnityBundle.message("unity.agents.setup.suggest.title") to
                UnityBundle.message("unity.agents.setup.suggest.content.cli"),
            unityAgentsSetupPrompt(cliMissing = true, packageMissing = true, skillMissing = false),
        )
        assertEquals(
            UnityBundle.message("unity.agents.setup.suggest.title") to
                UnityBundle.message("unity.agents.setup.suggest.content.cli.skill"),
            unityAgentsSetupPrompt(cliMissing = true, packageMissing = false, skillMissing = true),
        )
        assertEquals(
            UnityBundle.message("unity.agents.setup.suggest.title") to
                UnityBundle.message("unity.agents.setup.suggest.content.cli.skill"),
            unityAgentsSetupPrompt(cliMissing = true, packageMissing = false, skillMissing = false),
        )
        assertEquals(
            UnityBundle.message("unity.agents.setup.suggest.title") to
                UnityBundle.message("unity.agents.setup.suggest.content"),
            unityAgentsSetupPrompt(cliMissing = false, packageMissing = true, skillMissing = true),
        )
        assertEquals(
            UnityBundle.message("unity.agents.setup.finish.title") to
                UnityBundle.message("unity.agents.setup.finish.content.package"),
            unityAgentsSetupPrompt(cliMissing = false, packageMissing = true, skillMissing = false),
        )
        assertEquals(
            UnityBundle.message("unity.agents.setup.finish.title") to
                UnityBundle.message("unity.agents.setup.finish.content.skill"),
            unityAgentsSetupPrompt(cliMissing = false, packageMissing = false, skillMissing = true),
        )
    }

    @Test
    fun `unityAgentsMissingSteps lists the steps that the setup balloon offers`() {
        assertEquals(
            listOf(UnityAgentsSetupStep.CLI, UnityAgentsSetupStep.CLI_SKILL),
            unityAgentsMissingSteps(cliMissing = true, packageMissing = false, skillMissing = false),
        )
        assertEquals(
            UnityAgentsSetupStep.entries,
            unityAgentsMissingSteps(cliMissing = true, packageMissing = true, skillMissing = false),
        )
        assertEquals(
            UnityAgentsSetupStep.entries,
            unityAgentsMissingSteps(cliMissing = true, packageMissing = true, skillMissing = true),
        )
        assertEquals(
            listOf(UnityAgentsSetupStep.PIPELINE_PACKAGE, UnityAgentsSetupStep.CLI_SKILL),
            unityAgentsMissingSteps(cliMissing = false, packageMissing = true, skillMissing = true),
        )
        assertEquals(
            listOf(UnityAgentsSetupStep.PIPELINE_PACKAGE),
            unityAgentsMissingSteps(cliMissing = false, packageMissing = true, skillMissing = false),
        )
        assertEquals(
            listOf(UnityAgentsSetupStep.CLI_SKILL),
            unityAgentsMissingSteps(cliMissing = false, packageMissing = false, skillMissing = true),
        )
    }

    @Test
    fun `unitySkillGateMessage asks only for the steps that are missing`() {
        assertEquals(
            UnityBundle.message("unity.agents.skills.needs.setup"),
            unitySkillGateMessage(UnityMcpSectionState.NO_CLI, packageInstalled = false),
        )
        assertEquals(
            UnityBundle.message("unity.agents.skills.needs.cli"),
            unitySkillGateMessage(UnityMcpSectionState.NO_CLI, packageInstalled = true),
        )
        assertEquals(
            UnityBundle.message("unity.agents.skills.needs.package"),
            unitySkillGateMessage(UnityMcpSectionState.READY, packageInstalled = false),
        )
        assertNull(unitySkillGateMessage(UnityMcpSectionState.READY, packageInstalled = true))
        assertNull(unitySkillGateMessage(UnityMcpSectionState.CHECKING, packageInstalled = false))
    }

    @Test
    fun `unitySkillGateMessage names the old Editor when it blocks the package`() {
        val oldEditor = UnityOldEditor("6000.0", "2022.3.62f1")
        val oldEditorText = UnityBundle.message("unity.agents.old.editor.gate", "6000.0", "2022.3.62f1")
        assertEquals(
            oldEditorText,
            unitySkillGateMessage(UnityMcpSectionState.NO_CLI, packageInstalled = false, blockingEditor = oldEditor),
        )
        assertEquals(
            oldEditorText,
            unitySkillGateMessage(UnityMcpSectionState.READY, packageInstalled = false, blockingEditor = oldEditor),
        )
        assertEquals(
            UnityBundle.message("unity.agents.skills.needs.cli"),
            unitySkillGateMessage(UnityMcpSectionState.NO_CLI, packageInstalled = true, blockingEditor = oldEditor),
        )
        assertNull(unitySkillGateMessage(UnityMcpSectionState.READY, packageInstalled = true, blockingEditor = oldEditor))
    }

    @Test
    fun `unitySkillOpenTargets returns all targets when there are no installs`() {
        assertEquals(
            listOf(UnitySkillTarget.CODEX, UnitySkillTarget.CLAUDE_CODE, UnitySkillTarget.COPILOT, UnitySkillTarget.JUNIE),
            unitySkillOpenTargets(emptyList()),
        )
    }

    @Test
    fun `unitySkillOpenTargets returns empty list when local codex and local claude-code are installed`() {
        val path = Path.of("/projects/game/.agents/skills/unity-cli")
        val installs = listOf(
            UnitySkillInstall("codex", local = true, path),
            UnitySkillInstall("claude-code", local = true, path),
        )
        assertEquals(emptyList<UnitySkillTarget>(), unitySkillOpenTargets(installs))
    }

    @Test
    fun `unitySkillOpenTargets returns CODEX and JUNIE when local claude-code alone is installed`() {
        val path = Path.of("/projects/game/.agents/skills/unity-cli")
        val installs = listOf(
            UnitySkillInstall("claude-code", local = true, path),
        )
        assertEquals(listOf(UnitySkillTarget.CODEX, UnitySkillTarget.JUNIE), unitySkillOpenTargets(installs))
    }

    @Test
    fun `unitySkillOpenTargets returns all four when global codex alone is installed`() {
        val path = Path.of("/Users/dev/.agents/skills/unity-cli")
        val installs = listOf(
            UnitySkillInstall("codex", local = false, path),
        )
        assertEquals(
            listOf(UnitySkillTarget.CODEX, UnitySkillTarget.CLAUDE_CODE, UnitySkillTarget.COPILOT, UnitySkillTarget.JUNIE),
            unitySkillOpenTargets(installs),
        )
    }

    @Test
    fun `unitySkillOpenTargets returns CLAUDE_CODE when local codex alone is installed`() {
        val path = Path.of("/projects/game/.agents/skills/unity-cli")
        val installs = listOf(
            UnitySkillInstall("codex", local = true, path),
        )
        assertEquals(listOf(UnitySkillTarget.CLAUDE_CODE), unitySkillOpenTargets(installs))
    }

    @Test
    fun `unitySkillOpenTargets returns CODEX CLAUDE_CODE and JUNIE when local vscode alone is installed`() {
        val path = Path.of("/projects/game/.github/skills/unity-cli")
        val installs = listOf(
            UnitySkillInstall("vscode", local = true, path),
        )
        assertEquals(
            listOf(UnitySkillTarget.CODEX, UnitySkillTarget.CLAUDE_CODE, UnitySkillTarget.JUNIE),
            unitySkillOpenTargets(installs),
        )
    }
}
