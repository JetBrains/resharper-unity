package com.jetbrains.rider.plugins.unity.settings.agents

import com.google.gson.Gson
import com.google.gson.JsonSyntaxException
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessOutput
import com.intellij.openapi.util.NlsSafe
import org.jetbrains.annotations.ApiStatus
import java.nio.file.Path

@ApiStatus.Internal
const val UNITY_PIPELINE_PACKAGE_ID: String = "com.unity.pipeline"

private const val UNITY_PACKAGES_DIRECTORY: String = "Packages"

private const val UNITY_MANIFEST_FILE: String = "manifest.json"

private val gson = Gson()

@ApiStatus.Internal
fun unityManifestPath(solution: Path): Path =
    solution.resolve(UNITY_PACKAGES_DIRECTORY).resolve(UNITY_MANIFEST_FILE)

@ApiStatus.Internal
fun unityPipelineInstallCommandLine(unity: Path, solution: Path): GeneralCommandLine =
    GeneralCommandLine(unity.toString(), "pipeline", "install", "--project-path", solution.toString())
        .withParameters("--non-interactive", "--json")
        .withWorkingDirectory(solution)

@ApiStatus.Internal
fun unityPipelineUpgradeCommandLine(unity: Path, solution: Path): GeneralCommandLine =
    GeneralCommandLine(unity.toString(), "pipeline", "upgrade", "--project-path", solution.toString())
        .withParameters("--non-interactive", "--json")
        .withWorkingDirectory(solution)

@ApiStatus.Internal
fun unityPipelineListVersionsCommandLine(unity: Path): GeneralCommandLine =
    GeneralCommandLine(unity.toString(), "pipeline", "list-versions", "--non-interactive", "--json")

@ApiStatus.Internal
fun parseUnityPipelineLatestVersion(json: String): @NlsSafe String? =
    parsePipelineJson<ListVersionsAnswer>(json)?.data?.latest?.takeIf { it.isNotBlank() }

@ApiStatus.Internal
enum class UnityMcpPackageStep {
    NONE,
    INSTALL,
    ABORT,
}

@ApiStatus.Internal
fun unityMcpPackageStep(
    operation: UnityMcpOperation,
    packagePresent: Boolean?,
    agreed: Boolean,
): UnityMcpPackageStep = when {
    operation != UnityMcpOperation.CONFIGURING -> UnityMcpPackageStep.NONE
    packagePresent == true -> UnityMcpPackageStep.NONE
    packagePresent == null -> UnityMcpPackageStep.ABORT
    agreed -> UnityMcpPackageStep.INSTALL
    else -> UnityMcpPackageStep.ABORT
}

@ApiStatus.Internal
fun readUnityPipelineManifest(content: String?, fileExists: Boolean): UnityPipelineManifest? {
    if (!fileExists) return UnityPipelineManifest(version = null)
    if (content == null) return null
    val manifest = parsePipelineJson<PackageManifest>(content) ?: return null
    return UnityPipelineManifest(version = manifest.dependencies?.get(UNITY_PIPELINE_PACKAGE_ID))
}

@ApiStatus.Internal
data class UnityPipelineManifest(val version: @NlsSafe String?) {
    val present: Boolean get() = version != null
}

@ApiStatus.Internal
fun unityPipelineInstallOutcome(output: ProcessOutput?, inManifest: Boolean?): UnityPipelineFailure? =
    unityPipelineOutcome(output, landed = inManifest == true)

@ApiStatus.Internal
fun unityPipelineUpgradeOutcome(output: ProcessOutput?, versionAfter: String?, latestVersion: String?): UnityPipelineFailure? {
    val landed = versionAfter != null && latestVersion != null &&
                 (versionAfter == latestVersion || unityPipelineHasNewerVersion(versionAfter, latestVersion))
    return unityPipelineOutcome(output, landed)
}

private fun unityPipelineOutcome(output: ProcessOutput?, landed: Boolean): UnityPipelineFailure? = when {
    output == null || output.isTimeout -> UnityPipelineFailure.LAUNCH_FAILED
    landed -> null
    output.exitCode != 0 -> UnityPipelineFailure.EXIT_CODE
    else -> UnityPipelineFailure.NOT_CONFIRMED
}

private inline fun <reified T> parsePipelineJson(text: String): T? =
    try {
        gson.fromJson(text, T::class.java)
    } catch (_: JsonSyntaxException) {
        null
    }

private data class PackageManifest(val dependencies: Map<String, String>? = null)

private data class ListVersionsAnswer(val data: ListVersionsData? = null)

private data class ListVersionsData(val latest: String? = null)
