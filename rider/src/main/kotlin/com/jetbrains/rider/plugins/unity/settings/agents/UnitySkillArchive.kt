package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.diagnostic.fileLogger
import com.intellij.openapi.util.io.NioFiles
import com.intellij.platform.eel.fs.EelFileUtils
import com.intellij.util.io.HttpRequests
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.jetbrains.annotations.ApiStatus
import java.io.IOException
import java.io.InputStream
import java.nio.file.AtomicMoveNotSupportedException
import java.nio.file.Files
import java.nio.file.InvalidPathException
import java.nio.file.LinkOption
import java.nio.file.Path
import java.nio.file.StandardCopyOption
import java.util.UUID
import java.util.zip.ZipInputStream

private val logger = fileLogger()

/** The GitHub repository of the Unity skills. The Unity CLI downloads its skills from it too. */
@ApiStatus.Internal
const val UNITY_SKILL_REGISTRY_SOURCE: String = "Unity-Technologies/skills"

private const val UNITY_SKILL_ARCHIVE_URL: String = "https://github.com/$UNITY_SKILL_REGISTRY_SOURCE/archive/refs/heads/main.zip"

/** The folder of the skills in the repository. */
private const val SKILLS_DIRECTORY: String = "skills"

private const val CONNECT_TIMEOUT_MS: Int = 10_000

private const val READ_TIMEOUT_MS: Int = 30_000

private const val ARCHIVE_CAP_BYTES: Int = 10 * 1024 * 1024

/** The cap on the extracted unity-cli folder, so that a hostile archive cannot fill the disk. */
private const val EXTRACTED_CAP_BYTES: Int = 10 * 1024 * 1024

/** Downloads the archive of the `main` branch of [UNITY_SKILL_REGISTRY_SOURCE]. Gives null when the download fails. */
@ApiStatus.Internal
suspend fun downloadUnitySkillArchive(): ByteArray? = withContext(Dispatchers.IO) {
    try {
        val bytes = HttpRequests.request(UNITY_SKILL_ARCHIVE_URL)
            .productNameAsUserAgent()
            .connectTimeout(CONNECT_TIMEOUT_MS)
            .readTimeout(READ_TIMEOUT_MS)
            .connect { request -> request.inputStream.readNBytes(ARCHIVE_CAP_BYTES + 1) }
        if (bytes.size > ARCHIVE_CAP_BYTES) {
            logger.warn("The archive at $UNITY_SKILL_ARCHIVE_URL is larger than $ARCHIVE_CAP_BYTES bytes")
            null
        }
        else {
            bytes
        }
    }
    catch (e: IOException) {
        logger.warn("Rider could not download $UNITY_SKILL_ARCHIVE_URL", e)
        null
    }
}

/**
 * Extracts the `skills/unity-cli` folder of a GitHub archive into [staging].
 *
 * The archive puts every entry under one root folder, such as `skills-main/`. Rider skips an entry with `..`, an entry
 * that resolves outside [staging], and an entry whose name is not a valid path on this machine.
 *
 * Returns false when the archive has no `skills/unity-cli/SKILL.md`. Throws [IOException] for an archive that does not
 * read, and for a skill folder larger than [EXTRACTED_CAP_BYTES].
 */
@ApiStatus.Internal
fun extractUnitySkill(archive: InputStream, staging: Path): Boolean {
    val root = staging.toAbsolutePath().normalize()
    var hasSkillFile = false
    var extracted = 0
    ZipInputStream(archive).use { zip ->
        for (entry in generateSequence { zip.nextEntry }) {
            val parts = entry.name.split('/')
            if (parts.size < 4 || parts[1] != SKILLS_DIRECTORY || parts[2] != UNITY_CLI_SKILL_NAME) continue
            val relative = parts.drop(3).filter { it.isNotEmpty() }
            if (relative.isEmpty()) continue
            val target = resolveEntry(root, relative)
            if (target == null || ".." in relative || !target.startsWith(root) || target == root) {
                logger.warn(
                    "Rider skipped the entry ${entry.name} of the skills archive, " +
                        "because it is not a safe path in the skill folder",
                )
                continue
            }
            if (entry.isDirectory) {
                Files.createDirectories(target)
                continue
            }
            val bytes = zip.readNBytes(EXTRACTED_CAP_BYTES - extracted + 1)
            extracted += bytes.size
            if (extracted > EXTRACTED_CAP_BYTES) throw IOException("The unity-cli skill is larger than $EXTRACTED_CAP_BYTES bytes")
            Files.createDirectories(target.parent)
            Files.write(target, bytes)
            if (relative == listOf(UNITY_SKILL_FILE)) hasSkillFile = true
        }
    }
    return hasSkillFile
}

/** The path of an archive entry inside [root], or null when the entry name is not a valid path on this machine. */
private fun resolveEntry(root: Path, relative: List<String>): Path? =
    try {
        root.resolve(relative.joinToString("/")).normalize()
    }
    catch (_: InvalidPathException) {
        null
    }

/**
 * Replaces the folder [target] with a copy of [staging].
 *
 * The copy goes to a work folder in `<skills>.backup` beside the skills folder first. So each move stays on one file
 * system, and the old folder goes away only after the copy is complete. The agents read only the skills folder, so they
 * never see a work folder. When the second move fails, the old folder comes back. When that move fails too, the old
 * folder stays in `<skills>.backup`.
 *
 * @param move moves a folder. Tests replace it to make a move fail.
 */
@ApiStatus.Internal
fun replaceUnitySkillFolder(staging: Path, target: Path, move: (source: Path, target: Path) -> Unit = ::moveFolder) {
    val skills = target.toAbsolutePath().parent
    Files.createDirectories(skills)
    // The agents load every folder in the skills folder, so the work folders go to a sibling folder.
    val work = skills.resolveSibling("${skills.fileName}.backup")
    Files.createDirectories(work)
    val id = UUID.randomUUID()
    val fresh = work.resolve("${target.fileName}.$id.tmp")
    val old = work.resolve("${target.fileName}.$id.old")
    var keepOld = false
    try {
        NioFiles.copyRecursively(staging, fresh)
        val hadOld = Files.exists(target, LinkOption.NOFOLLOW_LINKS)
        if (hadOld) move(target, old)
        try {
            move(fresh, target)
        }
        catch (e: IOException) {
            if (hadOld) {
                try {
                    move(old, target)
                }
                catch (restore: IOException) {
                    // The old folder is the only copy of the skill now, so it stays where it is.
                    keepOld = true
                    logger.warn("Rider could not put the old skill folder back. It stays at $old", restore)
                    e.addSuppressed(restore)
                }
            }
            throw e
        }
    }
    finally {
        deleteQuietly(fresh)
        if (!keepOld) deleteQuietly(old)
        deleteIfEmpty(work)
    }
}

private fun moveFolder(source: Path, target: Path) {
    try {
        Files.move(source, target, StandardCopyOption.ATOMIC_MOVE)
    }
    catch (_: AtomicMoveNotSupportedException) {
        Files.move(source, target)
    }
}

private fun deleteQuietly(path: Path) {
    try {
        EelFileUtils.deleteRecursively(path)
    }
    catch (e: IOException) {
        logger.warn("Rider could not delete $path", e)
    }
}

/** Deletes [folder] when it holds nothing. A folder that still holds a kept old skill stays. */
private fun deleteIfEmpty(folder: Path) {
    try {
        Files.deleteIfExists(folder)
    }
    catch (_: IOException) {
        // The folder holds a kept old skill, or another process holds it open.
    }
}
