package com.jetbrains.rider.plugins.unity.settings.agents

import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.google.gson.JsonObject
import com.google.gson.JsonParser
import com.google.gson.JsonSyntaxException
import com.google.gson.annotations.SerializedName
import com.intellij.openapi.util.NlsSafe
import com.intellij.util.text.SemVer
import org.jetbrains.annotations.ApiStatus
import java.nio.file.Path

@ApiStatus.Internal
const val UNITY_PIPELINE_PACKAGE_ID: String = "com.unity.pipeline"

/** The package registry of Unity. A project manifest can name another one in its `registry` key. */
@ApiStatus.Internal
const val UNITY_PACKAGE_REGISTRY_URL: String = "https://packages.unity.com"

/**
 * The oldest Unity Editor that every published version of the package supports.
 *
 * Rider uses it until the registry answers. Then the `unity` fields of the published versions decide.
 */
@ApiStatus.Internal
const val UNITY_PIPELINE_MINIMUM_EDITOR_VERSION: String = "6000.0"

private const val UNITY_PACKAGES_DIRECTORY: String = "Packages"

private const val UNITY_MANIFEST_FILE: String = "manifest.json"

private const val UNITY_MANIFEST_DEPENDENCIES: String = "dependencies"

private const val UNITY_PROJECT_SETTINGS_DIRECTORY: String = "ProjectSettings"

private const val UNITY_PROJECT_VERSION_FILE: String = "ProjectVersion.txt"

private const val UNITY_EDITOR_VERSION_KEY: String = "m_EditorVersion:"

private val gson = Gson()

private val manifestGson = GsonBuilder().setPrettyPrinting().disableHtmlEscaping().create()

@ApiStatus.Internal
fun unityManifestPath(solution: Path): Path =
    solution.resolve(UNITY_PACKAGES_DIRECTORY).resolve(UNITY_MANIFEST_FILE)

@ApiStatus.Internal
fun unityProjectVersionPath(solution: Path): Path =
    solution.resolve(UNITY_PROJECT_SETTINGS_DIRECTORY).resolve(UNITY_PROJECT_VERSION_FILE)

/**
 * The registry document of the Unity Pipeline package.
 *
 * A [registry] that is not an http or https address counts as no registry, so Rider reads the registry of Unity.
 */
@ApiStatus.Internal
fun unityPipelineRegistryUrl(registry: String?): String {
    val base = registry?.takeIf { it.startsWith("https://") || it.startsWith("http://") } ?: UNITY_PACKAGE_REGISTRY_URL
    return "${base.trimEnd('/')}/$UNITY_PIPELINE_PACKAGE_ID"
}

/** A published version of the package, and the oldest Unity Editor that it supports. */
@ApiStatus.Internal
data class UnityPipelineRelease(val version: @NlsSafe String, val minimumEditorVersion: @NlsSafe String?)

/**
 * The package document of the registry: `dist-tags.latest`, and every published version up to it.
 *
 * [releases] holds [latest] too. When the document lists no entry for [latest], its minimum Editor is null.
 */
@ApiStatus.Internal
data class UnityPipelineRegistry(val latest: @NlsSafe String, val releases: List<UnityPipelineRelease>)

/**
 * Reads `dist-tags.latest` of a package document of the registry, and the `unity` field of each version up to it.
 *
 * A `latest` that is not a version gives null, so Rider never writes it into the manifest. Examples are a git URL and a
 * `file:` path. A version key that is not a version, or that is newer than `latest`, is left out.
 */
@ApiStatus.Internal
fun parseUnityRegistry(json: String): UnityPipelineRegistry? {
    val document = parsePipelineJson<RegistryPackage>(json) ?: return null
    val latest = document.distTags?.latest ?: return null
    val latestSemVer = SemVer.parseFromText(latest) ?: return null
    val listed = document.versions.orEmpty().entries.mapNotNull { entry ->
        val semVer = SemVer.parseFromText(entry.key) ?: return@mapNotNull null
        if (semVer > latestSemVer) return@mapNotNull null
        UnityPipelineRelease(entry.key, entry.value?.unity?.takeIf { it.isNotBlank() })
    }
    val releases = if (listed.any { it.version == latest }) listed else listed + UnityPipelineRelease(latest, null)
    return UnityPipelineRegistry(latest, releases)
}

/** Reads `m_EditorVersion` of `ProjectSettings/ProjectVersion.txt`. */
@ApiStatus.Internal
fun parseUnityEditorVersion(content: String): @NlsSafe String? =
    content.lineSequence()
        .map { it.trim() }
        .firstOrNull { it.startsWith(UNITY_EDITOR_VERSION_KEY) }
        ?.removePrefix(UNITY_EDITOR_VERSION_KEY)
        ?.trim()
        ?.takeIf { it.isNotEmpty() }

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
    return UnityPipelineManifest(
        version = manifest.dependencies?.get(UNITY_PIPELINE_PACKAGE_ID),
        registry = manifest.registry,
    )
}

@ApiStatus.Internal
data class UnityPipelineManifest(
    val version: @NlsSafe String?,
    /** The top-level `registry` of the manifest. It replaces the package registry of Unity. */
    val registry: String? = null,
) {
    val present: Boolean get() = version != null
}

/**
 * Sets the dependency [id] to [version] in the text of `Packages/manifest.json`, and keeps the order of the keys.
 *
 * The result has an indent of 2 spaces, as the Unity Editor writes it. It keeps the line separator of [content] and
 * its last line break. Returns null when [content] or its `dependencies` is not a JSON object.
 */
@ApiStatus.Internal
fun unityManifestWithDependency(content: String, id: String, version: String): String? {
    val root = parseManifestObject(content) ?: return null
    val dependencies = when (val element = root.get(UNITY_MANIFEST_DEPENDENCIES)) {
        null -> JsonObject().also { root.add(UNITY_MANIFEST_DEPENDENCIES, it) }
        is JsonObject -> element
        else -> return null
    }
    dependencies.addProperty(id, version)
    val lineSeparator = if ("\r\n" in content) "\r\n" else "\n"
    val text = manifestGson.toJson(root).replace("\n", lineSeparator)
    return if (content.endsWith("\n")) text + lineSeparator else text
}

private fun parseManifestObject(content: String): JsonObject? =
    try {
        JsonParser.parseString(content) as? JsonObject
    }
    catch (_: JsonSyntaxException) {
        null
    }

private inline fun <reified T> parsePipelineJson(text: String): T? =
    try {
        gson.fromJson(text, T::class.java)
    } catch (_: JsonSyntaxException) {
        null
    }

private data class PackageManifest(val dependencies: Map<String, String>? = null, val registry: String? = null)

private data class RegistryPackage(
    @SerializedName("dist-tags") val distTags: RegistryDistTags? = null,
    val versions: Map<String, RegistryVersion?>? = null,
)

private data class RegistryDistTags(val latest: String? = null)

private data class RegistryVersion(val unity: String? = null)
