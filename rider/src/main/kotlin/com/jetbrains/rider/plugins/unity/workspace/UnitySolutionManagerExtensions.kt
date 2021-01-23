package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.ide.util.PropertiesComponent
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.projectView.SolutionManagerExtensions
import java.nio.file.Path

class UnitySolutionManagerExtensions : SolutionManagerExtensions {

    companion object {
        private const val CLEANUP_USER_CONTENT_MODEL_KEY = "user.content.model.migrated"
    }

    override fun cleanupBeforeOpen(dotIdeaDir: Path): Array<Path> {
        // Cleanup indexLayout.xml one time while migrating to a new workspace model because
        //   unity plugin in versions 2020.3 and below used to write lots of generated rules in user store
        val projectDir = dotIdeaDir.parent.parent.toFile().toVirtualFile(true)
            ?: return arrayOf()
        if (!UnityProjectDiscoverer.hasUnityFileStructure(projectDir))
            return arrayOf()

        // TODO: WORKSPACE enable it after testing
//        if (PropertiesComponent.getInstance().getBoolean(CLEANUP_USER_CONTENT_MODEL_KEY))
//            return arrayOf()
//        PropertiesComponent.getInstance().setValue(CLEANUP_USER_CONTENT_MODEL_KEY, true)

        return super.cleanupBeforeOpen(dotIdeaDir)
    }
}