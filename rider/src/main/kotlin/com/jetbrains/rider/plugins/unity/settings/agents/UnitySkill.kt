package com.jetbrains.rider.plugins.unity.settings.agents

import com.google.gson.Gson
import com.google.gson.JsonSyntaxException
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessOutput
import com.intellij.openapi.util.NlsSafe
import com.intellij.platform.eel.fs.EelFileUtils
import org.jetbrains.annotations.ApiStatus
import java.io.IOException
import java.nio.file.Files
import java.nio.file.InvalidPathException
import java.nio.file.Path

@ApiStatus.Internal
const val UNITY_CLI_SKILL_NAME: String = "unity-cli"
private const val VSCODE_CLIENT: String = "vscode"

@ApiStatus.Internal
enum class UnitySkillClient(val key: String) {
    CODEX("codex"),
    CLAUDE_CODE("claude-code"),
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

private val gson = Gson()

internal fun unitySkillListCommandLine(unity: Path): GeneralCommandLine =
    GeneralCommandLine(unity.toString(), "skill", "list", "--non-interactive", "--json")

@ApiStatus.Internal
fun unitySkillInstallCommandLine(unity: Path, client: UnitySkillClient, solution: Path): GeneralCommandLine =
    GeneralCommandLine(unity.toString(), "skill", "install", client.key, "--local", "--yes", "--non-interactive", "--json")
        .withWorkingDirectory(solution)

internal fun unitySkillRefreshCommandLine(unity: Path): GeneralCommandLine =
    GeneralCommandLine(unity.toString(), "skill", "refresh", "--yes", "--non-interactive", "--json")

@ApiStatus.Internal
fun parseUnitySkillList(json: String): List<UnitySkillInstall>? {
    val answer = try {
        gson.fromJson(json, SkillListAnswer::class.java)
    }
    catch (_: JsonSyntaxException) {
        null
    } ?: return null

    if (answer.success != true || answer.data == null) return null

    return answer.data.filterNotNull().mapNotNull { entry ->
        if (entry.skill != UNITY_CLI_SKILL_NAME) return@mapNotNull null
        val client = entry.client ?: return@mapNotNull null
        val pathString = entry.path ?: return@mapNotNull null
        val path = try {
            Path.of(pathString)
        }
        catch (_: InvalidPathException) {
            return@mapNotNull null
        }
        UnitySkillInstall(
            client = client,
            local = entry.local == true,
            path = path,
        )
    }
}

@ApiStatus.Internal
fun unitySkillInstallsOf(
    installs: List<UnitySkillInstall>,
    solution: Path,
    exists: (Path) -> Boolean,
): List<UnitySkillInstall> {
    val realSolution = try {
        solution.toRealPath()
    }
    catch (_: IOException) {
        solution.toAbsolutePath().normalize()
    }
    return installs.filter { exists(it.path) && (!it.local || it.path.startsWith(realSolution)) }
}

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

internal fun unitySkillInstallOutcome(output: ProcessOutput?, landed: Boolean): UnitySkillFailure? = when {
    output == null || output.isTimeout -> UnitySkillFailure.LAUNCH_FAILED
    landed -> null
    output.exitCode != 0 -> UnitySkillFailure.EXIT_CODE
    else -> UnitySkillFailure.NOT_CONFIRMED
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

private data class SkillListAnswer(
    val success: Boolean? = null,
    val data: List<SkillEntry?>? = null,
)

private data class SkillEntry(
    val skill: String? = null,
    val client: String? = null,
    val local: Boolean? = null,
    val path: String? = null,
)
