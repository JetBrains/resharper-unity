package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.extractUnitySkill
import com.jetbrains.rider.plugins.unity.settings.agents.replaceUnitySkillFolder
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertThrows
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.io.TempDir
import java.io.ByteArrayInputStream
import java.io.ByteArrayOutputStream
import java.io.IOException
import java.nio.file.Files
import java.nio.file.Path
import java.util.zip.ZipEntry
import java.util.zip.ZipOutputStream

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnitySkillArchiveTest {
    @Test
    fun `extraction keeps only the unity-cli skill folder`(@TempDir tempDir: Path) {
        val staging = Files.createDirectories(tempDir.resolve("staging"))
        val archive = archiveOf(
            "skills-main/" to "",
            "skills-main/README.md" to "readme",
            "skills-main/skills/other/SKILL.md" to "other",
            "skills-main/skills/unity-cli/" to "",
            "skills-main/skills/unity-cli/SKILL.md" to "skill",
            "skills-main/skills/unity-cli/references/a.md" to "reference",
        )

        assertTrue(extractUnitySkill(ByteArrayInputStream(archive), staging))
        assertEquals(setOf("SKILL.md", "references/a.md"), filesIn(staging))
    }

    @Test
    fun `extraction gives false when the archive has no unity-cli SKILL md`(@TempDir tempDir: Path) {
        val staging = Files.createDirectories(tempDir.resolve("staging"))
        val archive = archiveOf(
            "skills-main/skills/unity-cli/references/a.md" to "reference",
            "skills-main/skills/other/SKILL.md" to "other",
        )

        assertFalse(extractUnitySkill(ByteArrayInputStream(archive), staging))
    }

    @Test
    fun `an entry that leaves the skill folder writes no file outside staging`(@TempDir tempDir: Path) {
        val staging = Files.createDirectories(tempDir.resolve("staging"))
        val archive = archiveOf(
            "skills-main/skills/unity-cli/SKILL.md" to "skill",
            "skills-main/skills/unity-cli/../../evil.md" to "evil",
            "skills-main/skills/unity-cli/references/a.md" to "reference",
        )

        assertTrue(extractUnitySkill(ByteArrayInputStream(archive), staging))
        assertFalse(Files.exists(staging.parent.resolve("evil.md")))
        assertFalse(Files.exists(staging.resolve("evil.md")))
        assertEquals(setOf("SKILL.md", "references/a.md"), filesIn(staging))
    }

    @Test
    fun `an entry whose name is not a valid path is skipped, and the other entries land`(@TempDir tempDir: Path) {
        val staging = Files.createDirectories(tempDir.resolve("staging"))
        val archive = archiveOf(
            "skills-main/skills/unity-cli/SKILL.md" to "skill",
            "skills-main/skills/unity-cli/bad\u0000name.md" to "bad",
        )

        assertTrue(extractUnitySkill(ByteArrayInputStream(archive), staging))
        assertEquals(setOf("SKILL.md"), filesIn(staging))
    }

    @Test
    fun `the archive root name does not matter`(@TempDir tempDir: Path) {
        val staging = Files.createDirectories(tempDir.resolve("staging"))
        val archive = archiveOf("skills-abc123/skills/unity-cli/SKILL.md" to "skill")

        assertTrue(extractUnitySkill(ByteArrayInputStream(archive), staging))
        assertEquals("skill", Files.readString(staging.resolve("SKILL.md")))
    }

    @Test
    fun `replaceUnitySkillFolder creates parents, replaces the old content, and leaves no copy`(@TempDir tempDir: Path) {
        val target = tempDir.resolve("project/.agents/skills/unity-cli")
        val first = Files.createDirectories(tempDir.resolve("first"))
        Files.writeString(first.resolve("SKILL.md"), "first")

        replaceUnitySkillFolder(first, target)

        assertEquals("first", Files.readString(target.resolve("SKILL.md")))

        Files.writeString(target.resolve("old.md"), "old")
        val second = Files.createDirectories(tempDir.resolve("second"))
        Files.writeString(second.resolve("SKILL.md"), "second")

        replaceUnitySkillFolder(second, target)

        assertEquals("second", Files.readString(target.resolve("SKILL.md")))
        assertFalse(Files.exists(target.resolve("old.md")))
        assertEquals(listOf("unity-cli"), names(target.parent))
        assertFalse(Files.exists(backupOf(target.parent)))
    }

    @Test
    fun `a working replace leaves no backup folder beside the skills folder`(@TempDir tempDir: Path) {
        val target = tempDir.resolve("project/.claude/skills/unity-cli")
        Files.createDirectories(target)
        Files.writeString(target.resolve("old.md"), "old")
        val staging = Files.createDirectories(tempDir.resolve("staging"))
        Files.writeString(staging.resolve("SKILL.md"), "new")

        replaceUnitySkillFolder(staging, target)

        assertEquals("new", Files.readString(target.resolve("SKILL.md")))
        assertEquals(listOf("unity-cli"), names(target.parent))
        assertFalse(Files.exists(backupOf(target.parent)))
    }

    @Test
    fun `a failed move of the new folder restores the old folder and leaves no copy`(@TempDir tempDir: Path) {
        val target = Files.createDirectories(tempDir.resolve("project/unity-cli"))
        Files.writeString(target.resolve("SKILL.md"), "old")
        val staging = Files.createDirectories(tempDir.resolve("staging"))
        Files.writeString(staging.resolve("SKILL.md"), "new")
        // The copy of the new folder ends with .tmp, so only the move of that copy fails.
        val move: (Path, Path) -> Unit = { source, destination ->
            if (source.fileName.toString().endsWith(".tmp")) throw IOException("The move of the new folder failed")
            Files.move(source, destination)
        }

        assertThrows(IOException::class.java) { replaceUnitySkillFolder(staging, target, move) }

        assertEquals("old", Files.readString(target.resolve("SKILL.md")))
        assertEquals(listOf("unity-cli"), names(target.parent))
        assertFalse(Files.exists(backupOf(target.parent)))
    }

    @Test
    fun `a failed restore keeps the old folder beside the target`(@TempDir tempDir: Path) {
        val target = Files.createDirectories(tempDir.resolve("project/unity-cli"))
        Files.writeString(target.resolve("SKILL.md"), "old")
        val staging = Files.createDirectories(tempDir.resolve("staging"))
        Files.writeString(staging.resolve("SKILL.md"), "new")
        // Every move into the target fails. The first move takes the target to the old name, so it works.
        val move: (Path, Path) -> Unit = { source, destination ->
            if (destination == target && source != target) throw IOException("The move into the target failed")
            Files.move(source, destination)
        }

        assertThrows(IOException::class.java) { replaceUnitySkillFolder(staging, target, move) }

        val backup = backupOf(target.parent)
        val oldFolders = names(backup).filter { it.startsWith("unity-cli.") && it.endsWith(".old") }
        assertEquals(1, oldFolders.size, "the old folder was not kept: ${names(backup)}")
        assertEquals("old", Files.readString(backup.resolve(oldFolders.single()).resolve("SKILL.md")))
        assertEquals(emptyList<String>(), names(target.parent).filter { it != "unity-cli" }, "the skills folder holds another folder")
    }

    private fun archiveOf(vararg entries: Pair<String, String>): ByteArray {
        val bytes = ByteArrayOutputStream()
        ZipOutputStream(bytes).use { zip ->
            for ((name, text) in entries) {
                zip.putNextEntry(ZipEntry(name))
                if (!name.endsWith("/")) zip.write(text.toByteArray())
                zip.closeEntry()
            }
        }
        return bytes.toByteArray()
    }

    private fun filesIn(root: Path): Set<String> = Files.walk(root).use { paths ->
        paths.filter { Files.isRegularFile(it) }.map { root.relativize(it).joinToString("/") }.toList().toSet()
    }

    private fun backupOf(skills: Path): Path = skills.resolveSibling("${skills.fileName}.backup")

    private fun names(folder: Path): List<String> = Files.list(folder).use { entries ->
        entries.map { it.fileName.toString() }.sorted().toList()
    }
}
