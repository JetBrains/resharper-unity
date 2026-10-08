package com.jetbrains.rider.plugins.unity.settings.agents

import com.google.gson.Gson
import com.google.gson.JsonSyntaxException
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessOutput
import com.intellij.openapi.util.NlsSafe
import java.nio.file.Path

const val UNITY_PIPELINE_PACKAGE_ID: String = "com.unity.pipeline"

const val UNITY_PACKAGES_DIRECTORY: String = "Packages"

const val UNITY_MANIFEST_FILE: String = "manifest.json"

private val gson = Gson()

fun unityManifestPath(solution: Path): Path =
    solution.resolve(UNITY_PACKAGES_DIRECTORY).resolve(UNITY_MANIFEST_FILE)

fun unityPipelineInstallCommandLine(unity: Path, solution: Path): GeneralCommandLine =
    GeneralCommandLine(unity.toString(), "pipeline", "install", "--project-path", solution.toString())
        .withParameters("--non-interactive", "--json")
        .withWorkingDirectory(solution)

enum class UnityMcpPackageStep {
    NONE,
    INSTALL,
    ABORT,
}

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

fun readUnityPipelineManifest(content: String?, fileExists: Boolean): UnityPipelineManifest? {
    if (!fileExists) return UnityPipelineManifest(version = null)
    if (content == null) return null
    val manifest = parsePipelineJson<PackageManifest>(content) ?: return null
    return UnityPipelineManifest(version = manifest.dependencies?.get(UNITY_PIPELINE_PACKAGE_ID))
}

data class UnityPipelineManifest(val version: @NlsSafe String?) {
    val present: Boolean get() = version != null
}

fun unityPipelineInstallOutcome(output: ProcessOutput?, inManifest: Boolean?): UnityMcpFailure? = when {
    output == null || output.isTimeout -> UnityMcpFailure.LAUNCH_FAILED
    inManifest == true -> null
    output.exitCode != 0 -> UnityMcpFailure.EXIT_CODE
    else -> UnityMcpFailure.NOT_CONFIRMED
}

private inline fun <reified T> parsePipelineJson(text: String): T? =
    try {
        gson.fromJson(text, T::class.java)
    } catch (_: JsonSyntaxException) {
        null
    }

private data class PackageManifest(val dependencies: Map<String, String>? = null)
