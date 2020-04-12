package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.fileTypes.FileTypeRegistry
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.util.text.CaseInsensitiveStringHashingStrategy
import com.jetbrains.rider.ideaInterop.fileTypes.msbuild.CsprojFileType
import com.jetbrains.rider.ideaInterop.fileTypes.sln.SolutionFileType
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uss.UssFileType
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uxml.UxmlFileType
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

fun isNonEditableUnityFile(file: VirtualFile) = isNonEditableUnityFileExtension(file.extension)
fun isNonEditableUnityFileExtension(extension: String?) = extension != null && nonEditableExtensions.contains(extension)

fun isGeneratedUnityFile(file: VirtualFile): Boolean {
    val fileTypeRegistry = FileTypeRegistry.getInstance()
    return fileTypeRegistry.isFileOfType(file, CsprojFileType) || fileTypeRegistry.isFileOfType(file, SolutionFileType)
}

fun isUxmlFile(file: VirtualFile) = FileTypeRegistry.getInstance().isFileOfType(file, UxmlFileType)
fun isUssFile(file: VirtualFile) = FileTypeRegistry.getInstance().isFileOfType(file, UssFileType)
