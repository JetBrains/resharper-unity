package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.util.NlsSafe
import com.intellij.platform.eel.fs.EelFileUtils
import org.jetbrains.annotations.ApiStatus
import java.io.IOException
import java.nio.file.Files
import java.nio.file.Path

@ApiStatus.Internal
const val UNITY_CLI_SKILL_NAME: String = "unity-cli"
internal const val UNITY_SKILL_FILE: String = "SKILL.md"
private const val VSCODE_CLIENT: String = "vscode"

/** A client that Rider installs the skill for, with the project folder that holds its skills. */
@ApiStatus.Internal
enum class UnitySkillClient(val key: String, val skillsDirectory: String) {
    CODEX("codex", ".agents/skills"),
    CLAUDE_CODE("claude-code", ".claude/skills"),
}

@ApiStatus.Internal
enum class UnitySkillTarget(val clients: List<UnitySkillClient>) {
    ALL(listOf(UnitySkillClient.CODEX, UnitySkillClient.CLAUDE_CODE)),
    CODEX(listOf(UnitySkillClient.CODEX)),
    CLAUDE_CODE(listOf(UnitySkillClient.CLAUDE_CODE)),
    COPILOT(listOf(UnitySkillClient.CODEX)),
    JUNIE(listOf(UnitySkillClient.CODEX)),
}

@ApiStatus.Internal
data class UnitySkillInstall(
    val client: @NlsSafe String,
    val local: Boolean,
    val path: Path,
)

/** A folder that holds the skills of a client, as `unity skill install --list` reports it. */
private class UnitySkillPlace(val client: String, val local: Boolean, val directory: String)

private val UNITY_SKILL_PLACES: List<UnitySkillPlace> = listOf(
    UnitySkillPlace(client = UnitySkillClient.CODEX.key, local = true, directory = UnitySkillClient.CODEX.skillsDirectory),
    UnitySkillPlace(client = UnitySkillClient.CLAUDE_CODE.key, local = true, directory = UnitySkillClient.CLAUDE_CODE.skillsDirectory),
    UnitySkillPlace(client = VSCODE_CLIENT, local = true, directory = ".github/skills"),
    UnitySkillPlace(client = UnitySkillClient.CODEX.key, local = false, directory = UnitySkillClient.CODEX.skillsDirectory),
    UnitySkillPlace(client = UnitySkillClient.CLAUDE_CODE.key, local = false, directory = UnitySkillClient.CLAUDE_CODE.skillsDirectory),
    UnitySkillPlace(client = VSCODE_CLIENT, local = false, directory = ".copilot/skills"),
)

/**
 * The unity-cli skill folders in [solution] and in [userHome].
 *
 * A folder counts when its `SKILL.md` is a regular file.
 */
@ApiStatus.Internal
fun unitySkillInstalls(solution: Path, userHome: Path, isFile: (Path) -> Boolean): List<UnitySkillInstall> =
    UNITY_SKILL_PLACES
        .map { place ->
            val root = if (place.local) solution else userHome
            UnitySkillInstall(place.client, place.local, root.resolve(place.directory).resolve(UNITY_CLI_SKILL_NAME))
        }
        .filter { isFile(it.path.resolve(UNITY_SKILL_FILE)) }

/** The project folder of the unity-cli skill for [client]. */
@ApiStatus.Internal
fun unitySkillDirectory(client: UnitySkillClient, solution: Path): Path =
    solution.resolve(client.skillsDirectory).resolve(UNITY_CLI_SKILL_NAME)

internal fun unitySkillReaders(install: UnitySkillInstall): List<UnitySkillTarget> = when (install.client) {
    UnitySkillClient.CODEX.key -> if (install.local) {
        listOf(UnitySkillTarget.CODEX, UnitySkillTarget.COPILOT, UnitySkillTarget.JUNIE)
    }
    else {
        listOf(UnitySkillTarget.CODEX)
    }
    UnitySkillClient.CLAUDE_CODE.key -> if (install.local) {
        listOf(UnitySkillTarget.CLAUDE_CODE, UnitySkillTarget.COPILOT)
    }
    else {
        listOf(UnitySkillTarget.CLAUDE_CODE)
    }
    VSCODE_CLIENT -> listOf(UnitySkillTarget.COPILOT)
    else -> emptyList()
}

@ApiStatus.Internal
fun unitySkillOpenTargets(installs: List<UnitySkillInstall>): List<UnitySkillTarget> {
    val projectReaders = installs.filter { it.local }.flatMapTo(HashSet(), ::unitySkillReaders)
    return listOf(UnitySkillTarget.CODEX, UnitySkillTarget.CLAUDE_CODE, UnitySkillTarget.COPILOT, UnitySkillTarget.JUNIE)
        .filter { it !in projectReaders }
}

@ApiStatus.Internal
fun deleteWithEmptyParent(path: Path) {
    EelFileUtils.deleteRecursively(path)
    try {
        Files.deleteIfExists(path.parent)
    }
    catch (_: IOException) {
        // The parent still holds other files, or another process holds it open.
    }
}
