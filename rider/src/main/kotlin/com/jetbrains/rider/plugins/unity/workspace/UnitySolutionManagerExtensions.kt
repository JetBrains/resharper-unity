package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.ide.util.PropertiesComponent
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.projectView.SolutionManagerExtensions
import java.nio.file.Path

class UnitySolutionManagerExtensions : SolutionManagerExtensions {

    companion object {
        private const val CLEANUP_USER_CONTENT_MODEL_PREFIX = "unity.user.content.model.migrated-"
    }

    override fun cleanupBeforeOpen(dotIdeaDir: Path): Array<Path> {
        // Cleanup/delete indexLayout.xml one time while migrating to a new workspace model because
        //   unity plugin in versions 2020.3 and below used to write lots of generated rules in user store

        // Sadly, we can't save a nice "done it" flag in the project's workspace.xml, because we don't have a Project.
        // Save it to global config instead, with a key bound to the hash of the .idea path
        val key = CLEANUP_USER_CONTENT_MODEL_PREFIX + dotIdeaDir.hashCode().toString(16)
        val handledPath = PropertiesComponent.getInstance().getValue(key)
        if (handledPath == dotIdeaDir.toString()) return emptyArray()

        // <projectDir>/.idea/.idea.SolutionName/.idea
        val projectDir = dotIdeaDir.parent?.parent?.parent?.toFile() ?: return emptyArray()
        if (!UnityProjectDiscoverer.hasUnityFileStructure(projectDir)) return emptyArray()

        PropertiesComponent.getInstance().setValue(key, dotIdeaDir.toString())

        return arrayOf(dotIdeaDir.resolve("indexLayout.xml"))
    }
}