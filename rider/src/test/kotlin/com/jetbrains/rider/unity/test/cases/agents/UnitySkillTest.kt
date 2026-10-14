package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpSectionState
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillClient
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillInstall
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillOffer
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillPresentation
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillTarget
import com.jetbrains.rider.plugins.unity.settings.agents.parseUnitySkillList
import com.jetbrains.rider.plugins.unity.settings.agents.unityAgentsSetupPrompt
import com.jetbrains.rider.plugins.unity.settings.agents.unitySkillInstallCommandLine
import com.jetbrains.rider.plugins.unity.settings.agents.unitySkillInstallsOf
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
    @Test
    fun `parseUnitySkillList parses valid json and skips other skills`() {
        val sample = """
            {"success": true, "command": "skill-list", "data": [
              {"skill": "unity-cli", "client": "claude-code", "local": true,
               "path": "/private/var/folders/.../proj/.claude/skills/unity-cli", "kind": "tree", "enabled": true},
              {"skill": "unity-cli", "client": "codex", "local": false,
               "path": "/Users/dev/.agents/skills/unity-cli", "kind": "tree", "enabled": true},
              {"skill": "other-skill", "client": "codex", "local": false,
               "path": "/Users/dev/.agents/skills/other-skill", "kind": "tree", "enabled": true}
            ], "errors": [], "warnings": []}
        """.trimIndent()

        val installs = parseUnitySkillList(sample)
        assertEquals(2, installs?.size)
        assertEquals("claude-code", installs?.get(0)?.client)
        assertTrue(installs?.get(0)?.local == true)
        assertEquals(Path.of("/private/var/folders/.../proj/.claude/skills/unity-cli"), installs?.get(0)?.path)
        assertEquals("codex", installs?.get(1)?.client)
        assertFalse(installs?.get(1)?.local == true)
        assertEquals(Path.of("/Users/dev/.agents/skills/unity-cli"), installs?.get(1)?.path)

        assertNull(parseUnitySkillList("not json"))
        assertNull(parseUnitySkillList("""{"success": false, "data": null}"""))
    }

    @Test
    fun `parseUnitySkillList skips null elements in data array`() {
        val sample = """
            {"success": true, "command": "skill-list", "data": [
              null,
              {"skill": "unity-cli", "client": "codex", "local": true,
               "path": "/Users/dev/.agents/skills/unity-cli", "kind": "tree", "enabled": true}
            ], "errors": [], "warnings": []}
        """.trimIndent()

        val installs = parseUnitySkillList(sample)
        assertEquals(
            listOf(UnitySkillInstall("codex", local = true, Path.of("/Users/dev/.agents/skills/unity-cli"))),
            installs,
        )
    }

    @Test
    fun `unitySkillInstallsOf filters by existence and solution directory`(@TempDir tempDir: Path) {
        val solution = tempDir.resolve("my-project")
        Files.createDirectories(solution)
        val realSolution = solution.toRealPath()

        val validLocal = UnitySkillInstall("codex", local = true, realSolution.resolve(".agents/skills/unity-cli"))
        val missingLocal = UnitySkillInstall("claude-code", local = true, realSolution.resolve(".claude/skills/unity-cli"))
        val otherProjectLocal = UnitySkillInstall("codex", local = true, tempDir.resolve("other-project/.agents/skills/unity-cli"))
        val validGlobal = UnitySkillInstall("codex", local = false, Path.of("/Users/dev/.agents/skills/unity-cli"))
        val missingGlobal = UnitySkillInstall("claude-code", local = false, Path.of("/Users/dev/.claude/skills/unity-cli"))

        val existingPaths = setOf(validLocal.path, otherProjectLocal.path, validGlobal.path)
        val result = unitySkillInstallsOf(
            listOf(validLocal, missingLocal, otherProjectLocal, validGlobal, missingGlobal),
            solution,
            exists = { it in existingPaths },
        )

        assertEquals(listOf(validLocal, validGlobal), result)
    }

    @Test
    fun `unitySkillInstallCommandLine builds correct arguments and never passes force`() {
        val unity = Path.of("/opt/unity/bin/unity")
        val solution = Path.of("/projects/game")
        val command = unitySkillInstallCommandLine(unity, UnitySkillClient.CODEX, solution)

        assertEquals(
            listOf("skill", "install", "codex", "--local", "--yes", "--non-interactive", "--json"),
            command.parametersList.parameters,
        )
        assertEquals(solution, command.workingDirectory)
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
            failure = UnitySkillFailure.EXIT_CODE,
        )
        assertEquals(UnitySkillOffer.FAILED, presentation.offer)
    }

    @Test
    fun `offer rule 4 - gate NO_CLI produces GATED even with failure`() {
        val presentation = UnitySkillPresentation(gate = UnityMcpSectionState.NO_CLI, installs = emptyList())
        assertEquals(UnitySkillOffer.GATED, presentation.offer)

        val failed = UnitySkillPresentation(
            gate = UnityMcpSectionState.NO_CLI,
            failure = UnitySkillFailure.EXIT_CODE,
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
            failure = UnitySkillFailure.EXIT_CODE,
        )
        assertEquals(UnitySkillOffer.FAILED, failed.offer)
    }

    @Test
    fun `offer rule 6 - gate READY evaluates failure, installs, and packageInstalled`() {
        val failed = UnitySkillPresentation(
            gate = UnityMcpSectionState.READY,
            failure = UnitySkillFailure.LAUNCH_FAILED,
        )
        assertEquals(UnitySkillOffer.FAILED, failed.offer)

        val unreadable = UnitySkillPresentation(
            gate = UnityMcpSectionState.READY,
            installs = null,
        )
        assertEquals(UnitySkillOffer.UNREADABLE, unreadable.offer)

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
        assertEquals("(Codex, project; Claude Code, global)", unitySkillPlacesMessage(twoEntries))

        val duplicates = listOf(
            UnitySkillInstall("codex", local = true, path1),
            UnitySkillInstall("codex", local = true, path2),
        )
        assertEquals("(Codex, project)", unitySkillPlacesMessage(duplicates))

        val markup = listOf(
            UnitySkillInstall("<custom-client>", local = true, path1),
        )
        assertEquals("(&lt;custom-client&gt;, project)", unitySkillPlacesMessage(markup))
    }

    @Test
    fun `unityAgentsSetupPrompt handles the four cases`() {
        assertEquals(
            UnityBundle.message("unity.agents.setup.suggest.title") to
                UnityBundle.message("unity.agents.setup.suggest.content.cli"),
            unityAgentsSetupPrompt(cliMissing = true, packageMissing = false, skillMissing = false),
        )
        assertEquals(
            UnityBundle.message("unity.agents.setup.suggest.title") to
                UnityBundle.message("unity.agents.setup.suggest.content.cli"),
            unityAgentsSetupPrompt(cliMissing = true, packageMissing = true, skillMissing = true),
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
}
