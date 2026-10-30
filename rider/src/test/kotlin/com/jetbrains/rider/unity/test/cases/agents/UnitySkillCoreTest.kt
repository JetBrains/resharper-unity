package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsResult
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpSectionState
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillClient
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillCore
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillInstall
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillOffer
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillTarget
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.async
import kotlinx.coroutines.runBlocking
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.io.TempDir
import java.io.ByteArrayOutputStream
import java.nio.file.Files
import java.nio.file.Path
import java.util.zip.ZipEntry
import java.util.zip.ZipOutputStream

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnitySkillCoreTest {
    @TempDir
    lateinit var solution: Path

    @TempDir
    lateinit var home: Path

    private var archive: ByteArray? = archiveOf(
        "skills-main/" to "",
        "skills-main/skills/unity-cli/" to "",
        "skills-main/skills/unity-cli/SKILL.md" to SKILL_TEXT,
        "skills-main/skills/unity-cli/references/build.md" to BUILD_TEXT,
    )

    private val core = coreWith()

    private fun coreWith(
        exists: (Path) -> Boolean = { Files.exists(it) },
        delete: (Path) -> Unit = {},
        download: suspend () -> ByteArray? = { archive },
    ): UnitySkillCore = UnitySkillCore(
        userHome = { home },
        isFile = { Files.isRegularFile(it) },
        exists = exists,
        delete = delete,
        downloadArchive = download,
    )

    @Test
    fun `install ALL writes the skill for codex and claude-code and records SUCCESS once`() = runBlocking {
        val results = mutableListOf<UnityAgentsResult>()
        core.install(solution, UnitySkillTarget.ALL) { results.add(it) }

        for (folder in listOf(".agents/skills/unity-cli", ".claude/skills/unity-cli")) {
            assertEquals(SKILL_TEXT, Files.readString(solution.resolve("$folder/SKILL.md")), "the skill text of $folder")
            assertEquals(BUILD_TEXT, Files.readString(solution.resolve("$folder/references/build.md")), "the reference of $folder")
        }
        assertNull(core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution).failure)
        assertEquals(listOf(UnityAgentsResult.SUCCESS), results)
    }

    @Test
    fun `an install replaces the old skill folder and leaves no copy beside it`() = runBlocking {
        val old = solution.resolve(".agents/skills/unity-cli/old.md")
        Files.createDirectories(old.parent)
        Files.writeString(old, "old")

        core.install(solution, UnitySkillTarget.ALL)

        assertFalse(Files.exists(old))
        assertEquals(listOf("unity-cli"), names(solution.resolve(".agents/skills")))
        assertFalse(Files.exists(solution.resolve(".agents/skills.backup")))
    }

    @Test
    fun `a failed download holds DOWNLOAD, writes nothing, and reports FAILED once`() = runBlocking {
        archive = null
        val results = mutableListOf<UnityAgentsResult>()
        core.install(solution, UnitySkillTarget.ALL) { results.add(it) }

        assertEquals(
            UnitySkillFailure.DOWNLOAD,
            core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution).failure,
        )
        assertEquals(emptyList<String>(), names(solution))
        assertEquals(listOf(UnityAgentsResult.FAILED), results)
    }

    @Test
    fun `a failed refresh holds no failure, keeps the installed skill, and reports FAILED once`() = runBlocking {
        core.install(solution, UnitySkillTarget.CODEX)
        archive = null
        val results = mutableListOf<UnityAgentsResult>()

        core.refresh(solution, UnitySkillTarget.CODEX.clients) { results.add(it) }

        val presentation = core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution)
        assertNull(presentation.failure)
        assertEquals(UnitySkillOffer.INSTALLED, presentation.offer)
        assertEquals(listOf(UnityAgentsResult.FAILED), results)
    }

    @Test
    fun `an archive without the unity-cli skill holds NOT_CONFIRMED and writes nothing`() = runBlocking {
        archive = archiveOf("skills-main/" to "", "skills-main/skills/other/SKILL.md" to "other")

        core.install(solution, UnitySkillTarget.ALL)

        assertEquals(
            UnitySkillFailure.NOT_CONFIRMED,
            core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution).failure,
        )
        assertEquals(emptyList<String>(), names(solution))
    }

    @Test
    fun `bytes that are not a zip archive hold NOT_CONFIRMED`() = runBlocking {
        archive = "not a zip archive".toByteArray()

        core.install(solution, UnitySkillTarget.CODEX)

        assertEquals(
            UnitySkillFailure.NOT_CONFIRMED,
            core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution).failure,
        )
    }

    @Test
    fun `install deletes the disabled copy of the installed skill`() = runBlocking {
        val disabled = solution.resolve(".agents/skills.disabled/unity-cli")
        val deleted = mutableListOf<Path>()
        val results = mutableListOf<UnityAgentsResult>()

        coreWith(exists = { it == disabled }, delete = { deleted.add(it) })
            .install(solution, UnitySkillTarget.CODEX) { results.add(it) }

        assertEquals(listOf(disabled), deleted)
        assertEquals(listOf(UnityAgentsResult.SUCCESS), results)
    }

    @Test
    fun `a failed install keeps the disabled copy of the skill`() = runBlocking {
        archive = archiveOf("skills-main/" to "")
        val deleted = mutableListOf<Path>()

        coreWith(exists = { true }, delete = { deleted.add(it) }).install(solution, UnitySkillTarget.CODEX)

        assertEquals(emptyList<Path>(), deleted)
    }

    @Test
    fun `a second install call while one runs reports nothing`() = runBlocking {
        val started = CompletableDeferred<Unit>()
        val release = CompletableDeferred<Unit>()
        val blocked = coreWith(download = {
            started.complete(Unit)
            release.await()
            archive
        })

        val firstResults = mutableListOf<UnityAgentsResult>()
        val secondResults = mutableListOf<UnityAgentsResult>()
        val first = async { blocked.install(solution, UnitySkillTarget.CODEX) { firstResults.add(it) } }
        started.await()
        blocked.install(solution, UnitySkillTarget.CODEX) { secondResults.add(it) }
        release.complete(Unit)
        first.await()

        assertEquals(1, firstResults.size)
        assertEquals(emptyList<UnityAgentsResult>(), secondResults)
    }

    @Test
    fun `read with gate READY lists the project skill after an install`() = runBlocking {
        core.install(solution, UnitySkillTarget.CODEX)

        val presentation = core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution)

        assertEquals(
            listOf(UnitySkillInstall("codex", local = true, solution.resolve(".agents/skills/unity-cli"))),
            presentation.installs,
        )
        assertEquals(UnitySkillOffer.INSTALLED, presentation.offer)
    }

    @Test
    fun `read with gate NO_CLI gives null installs`() = runBlocking {
        core.install(solution, UnitySkillTarget.CODEX)

        val presentation = core.read(UnityMcpSectionState.NO_CLI, packageInstalled = true, solution = solution)

        assertNull(presentation.installs)
        assertEquals(UnitySkillOffer.GATED, presentation.offer)
    }

    @Test
    fun `clearFailure clears a held failure`() = runBlocking {
        archive = null
        core.install(solution, UnitySkillTarget.CODEX)
        assertEquals(
            UnitySkillFailure.DOWNLOAD,
            core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution).failure,
        )

        core.clearFailure()

        assertNull(core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution).failure)
    }

    @Test
    fun `a failed install for CLAUDE_CODE holds the target for Try Again`() = runBlocking {
        archive = null

        core.install(solution, UnitySkillTarget.CLAUDE_CODE)

        val presentation = core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution)
        assertEquals(UnitySkillFailure.DOWNLOAD, presentation.failure)
        assertEquals(UnitySkillTarget.CLAUDE_CODE, presentation.failedTarget)
    }

    @Test
    fun `a failed install for ALL holds ALL as the target for Try Again`() = runBlocking {
        archive = null

        core.install(solution, UnitySkillTarget.ALL)

        val presentation = core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution)
        assertEquals(UnitySkillTarget.ALL, presentation.failedTarget)
    }

    @Test
    fun `a failed refresh holds no failure and no failed target`() = runBlocking {
        core.install(solution, UnitySkillTarget.CODEX)
        archive = null

        core.refresh(solution, UnitySkillTarget.CODEX.clients)

        val presentation = core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution)
        assertNull(presentation.failure)
        assertNull(presentation.failedTarget)
    }

    @Test
    fun `clearFailure clears a held failure and its target`() = runBlocking {
        archive = null
        core.install(solution, UnitySkillTarget.CLAUDE_CODE)
        val held = core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution)
        assertEquals(UnitySkillTarget.CLAUDE_CODE, held.failedTarget)

        core.clearFailure()

        val presentation = core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution)
        assertNull(presentation.failure)
        assertNull(presentation.failedTarget)
    }

    @Test
    fun `a later install that works clears the failure and its target`() = runBlocking {
        val good = archive
        archive = null
        core.install(solution, UnitySkillTarget.CLAUDE_CODE)
        archive = good

        core.install(solution, UnitySkillTarget.CLAUDE_CODE)

        assertEquals(SKILL_TEXT, Files.readString(solution.resolve(".claude/skills/unity-cli/SKILL.md")))
        val presentation = core.read(UnityMcpSectionState.READY, packageInstalled = true, solution = solution)
        assertNull(presentation.failure)
        assertNull(presentation.failedTarget)
    }

    @Test
    fun `installedClients is empty when no client has the skill`() = runBlocking {
        assertEquals(emptyList<UnitySkillClient>(), core.installedClients(solution))
    }

    @Test
    fun `installedClients lists the client that has the skill`() = runBlocking {
        writeSkill(".claude/skills/unity-cli")

        assertEquals(listOf(UnitySkillClient.CLAUDE_CODE), core.installedClients(solution))
    }

    @Test
    fun `installedClients lists both clients when both have the skill`() = runBlocking {
        writeSkill(".agents/skills/unity-cli")
        writeSkill(".claude/skills/unity-cli")

        assertEquals(listOf(UnitySkillClient.CODEX, UnitySkillClient.CLAUDE_CODE), core.installedClients(solution))
    }

    @Test
    fun `installedClients ignores the skill in the VS Code folder`() = runBlocking {
        writeSkill(".github/skills/unity-cli")

        assertEquals(emptyList<UnitySkillClient>(), core.installedClients(solution))
    }

    @Test
    fun `a refresh for the installed clients writes only the Claude Code folder and replaces its old content`() = runBlocking {
        writeSkill(".claude/skills/unity-cli")
        val old = solution.resolve(".claude/skills/unity-cli/old.md")
        Files.writeString(old, "old")

        core.refresh(solution, core.installedClients(solution))

        assertFalse(Files.exists(old))
        assertEquals(SKILL_TEXT, Files.readString(solution.resolve(".claude/skills/unity-cli/SKILL.md")))
        assertEquals(listOf(".claude"), names(solution))
    }

    private fun writeSkill(folder: String) {
        val file = solution.resolve(folder).resolve("SKILL.md")
        Files.createDirectories(file.parent)
        Files.writeString(file, SKILL_TEXT)
    }

    private fun archiveOf(vararg entries: Pair<String, String>): ByteArray {
        val bytes = ByteArrayOutputStream()
        ZipOutputStream(bytes).use { zip ->
            for ([name, text] in entries) {
                zip.putNextEntry(ZipEntry(name))
                if (!name.endsWith("/")) zip.write(text.toByteArray())
                zip.closeEntry()
            }
        }
        return bytes.toByteArray()
    }

    private fun names(folder: Path): List<String> = Files.list(folder).use { entries ->
        entries.map { it.fileName.toString() }.sorted().toList()
    }

    private companion object {
        const val SKILL_TEXT = "the unity-cli skill"
        const val BUILD_TEXT = "the build reference"
    }
}
