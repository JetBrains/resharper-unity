package com.jetbrains.rider.plugins.unity.explorer

import com.google.gson.Gson
import com.intellij.ide.projectView.PresentationData
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.projectView.views.FileSystemNodeBase

class PackagesRoot(project: Project, virtualFile: VirtualFile)
    : UnityExplorerNode(project, virtualFile, listOf()) {

    override fun update(presentation: PresentationData) {
        if (!virtualFile.isValid) return
        presentation.presentableText = "Packages"
        presentation.setIcon(UnityIcons.Explorer.PackagesRoot)
    }

    // TODO: This doesn't work if there are two nodes. Doesn't even get called
    override fun isAlwaysExpand() = true

    // This means we will only show "embedded packages", which are packages that have been copied into the user's
    // project, will be compiled from source, and are user modifiable (so read/write). Any .asmdef files in the
    // package will be converted into projects, and included into the solution. Since they are already in the project
    // model, everything just works - opening files, VCS status, analysis, navigation, refactoring, locate in Unity
    // explorer, etc.
    // TODO: File and Live template scopes currently require being in the "Assets" folder
    // We do not show other kinds of packages. If the files aren't in the Packages folder, they are in a known cache
    // location and are read-only. They are not included in the project as source. Unity will create one assembly
    // for each .asmdef file in the package, compile it into an assembly, save it in Library/ScriptAssemblies and
    // add a binary reference to the project
    // The only downside to not supporting this right now is that references from .asmdef files to these packages
    // will not be resolved correctly. The tricky part about supporting them is that we will have no functionality
    // for them because they are not part of the project. We could add custom PSI modules for them, but then we need
    // to associate the binary reference to our new custom PSI module. The custom PSI module would allow us to
    // resolve the .asmdef references.
    // I also don't know what Unity are calling these kinds of packages. I'm calling them cached packages for now
    // TODO: Find out what happens with debug symbols and debugging/navigation. We should be able to have full source access
    // TODO: Resolve references to .asmdef files in cached packages
    // We also don't support local file packages or git based packages. Instead of a version number, the manifest.json
    // can contain a file: based URL, which points to a folder that is treated as a package. This URL is local to the
    // Packages folder, but can point outside of it, either via relative paths or a fully qualified path. These
    // packages are treated as source, so will be included in the project model.
    // (And git based packages aren't supported by Unity right now, either

    override fun createNode(virtualFile: VirtualFile, nestedFiles: List<VirtualFile>): FileSystemNodeBase {
        if (virtualFile.isDirectory) {
            val gson = Gson()

            try {
                val packageFile = virtualFile.findChild("package.json")
                if (packageFile?.exists() == true && !packageFile.isDirectory) {
                    val packageJson = gson.fromJson(packageFile.inputStream.reader(), PackageJson::class.java)
                    if (packageJson != null) {
                        return PackageNode(project!!, virtualFile, packageJson)
                    }
                }
            }
            catch (e: Throwable) {
                // Do nothing, drop down to file system
            }
        }
        return super.createNode(virtualFile, nestedFiles)
    }
}

class PackageNode(project: Project,
                  virtualFile: VirtualFile,
                  private val packageJson: PackageJson)
    : UnityExplorerNode(project, virtualFile, listOf()) {

    override fun update(presentation: PresentationData) {
        val name = packageJson.displayName ?: packageJson.name ?: virtualFile.name
        presentation.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation.setIcon(UnityIcons.Explorer.Package)

        // Note that this might also set the tooltip if we have too many projects underneath
        addProjects(presentation)

        // Richer tooltip
        val existingTooltip = presentation.tooltip ?: ""
        var newTooltip = ""
        if (name != virtualFile.name) {
            newTooltip = virtualFile.name + "\n"
        }
        if (packageJson.version != null && packageJson.version.isNotEmpty()) {
            newTooltip += packageJson.version
        }
        if (packageJson.description != null) {
            newTooltip += "\n${packageJson.description}"
        }
        if (existingTooltip.isNotEmpty()) {
            newTooltip += "\n" + existingTooltip
        }
        presentation.tooltip = newTooltip
    }
}

//class ManifestJson(val dependencies: Map<String, String>, val testables: Array<String>?, val registry: String?)

// Other properties are available: category, keywords, unity (supported version), author, dependencies
data class PackageJson(val name: String?, val displayName: String?, val version: String?, val description: String?)