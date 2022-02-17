package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.vfs.VirtualFile
import java.util.*

class UnityVfsUtil {
    companion object {
        fun belongsToAssetDatabase(file: VirtualFile, root: VirtualFile):Boolean {
            var node: VirtualFile? = file
            while (node != null && node != root) {
                if (!isHiddenAssetRoot(node)) {
                    return false
                }
                node = node.parent
            }
            return true
        }

        fun isHiddenAssetRoot(file: VirtualFile): Boolean {
            // See https://docs.unity3d.com/Manual/SpecialFolders.html
            val extension = file.extension?.lowercase(Locale.getDefault())
            if (extension != null && UnityExplorer.IgnoredExtensions.contains(extension)) {
                return false
            }

            val name = file.nameWithoutExtension.lowercase(Locale.getDefault())
            if (name == "cvs" || file.name.startsWith(".")) {
                return false
            }

            if (file.name.endsWith("~")) {
                return false
            }

            return true
        }
    }
}