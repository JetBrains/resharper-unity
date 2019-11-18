package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.vfs.VirtualFile
import com.intellij.util.text.CaseInsensitiveStringHashingStrategy
import gnu.trove.THashSet

private val nonEditableExtensions = getExtensions()

@Suppress("SpellCheckingInspection")
private fun getExtensions(): THashSet<String> {
    val extensions = THashSet<String>(CaseInsensitiveStringHashingStrategy.INSTANCE)
    extensions.addAll(arrayOf(
            "asset",
            "prefab",
            "unity",
            "meta",

            // From Unity's Create menu
            "anim",                 // Animation
            "brush",
            "controller",           // Animator controller
            "cubemap",
            "flare",                // Lens flare
            "fontsettings",         // Custom font
            "giparams",             // Lightmap parameters
            "guiskin",
            "mask",                 // Avatar mask
            "mat",                  // Material
            "mixer",                // Audio mixer
            "physicMaterial",
            "physicsMaterial2D",
            "playable",             // E.g. Timeline
            "overrideController",   // Animation override controller
            "renderTexture",
            "signal",               // Timeline signal
            "spriteatlas",
            "terrainlayer"
    ))
    return extensions
}

fun isNonEditableUnityFile(file: VirtualFile): Boolean {
    return isNonEditableUnityFileExtension(file.extension)
}

fun isNonEditableUnityFileExtension(extension: String?): Boolean {
    return nonEditableExtensions.contains(extension)
}

fun isGeneratedUnityFile(file: VirtualFile): Boolean {
    val extension = file.extension
    return extension.equals("csproj", true) || extension.equals("sln", true)
}

fun isUxmlFile(file: VirtualFile) = file.extension.equals("uxml", true)